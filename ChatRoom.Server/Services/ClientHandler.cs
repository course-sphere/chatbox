using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using ChatRoom.Core;

namespace ChatRoom.Server.Services
{
    public class ClientHandler
    {
        public TcpClient ClientSocket { get; set; }
        public string Username { get; set; }
        public string UID { get; set; }

        private NetworkStream _stream;
        private Action<string> _logAction;
        private ServerListener _server;

        public ClientHandler(TcpClient client, Action<string> logAction, ServerListener server)
        {
            ClientSocket = client;
            _stream = client.GetStream();
            _logAction = logAction;
            _server = server;
            UID = Guid.NewGuid().ToString();
            Username = "Unknown";
        }

        public async Task StartReadingLoop()
        {
            try
            {
                while (ClientSocket.Connected)
                {
                    byte[] lengthBuffer = new byte[4];
                    int bytesRead = await _stream.ReadAsync(lengthBuffer, 0, 4);

                    if (bytesRead == 0) break;

                    int packetLength = BitConverter.ToInt32(lengthBuffer, 0);

                    if (packetLength > 0)
                    {
                        byte[] packetBuffer = new byte[packetLength];
                        int totalRead = 0;

                        while (totalRead < packetLength)
                        {
                            int read = await _stream.ReadAsync(packetBuffer, totalRead, packetLength - totalRead);
                            if (read == 0) break;
                            totalRead += read;
                        }

                        ChatPacket packet = ChatPacket.Deserialize(packetBuffer);
                        this.Username = packet.Username;

                        _logAction?.Invoke($"[{packet.Username}]: {packet.Message}");

                        _server.BroadcastPacket(packet);
                    }
                }
            }
            catch (Exception ex)
            {
                _logAction?.Invoke($"Error handling client {UID}: {ex.Message}");
            }
            finally
            {
                ClientSocket.Close();
                _logAction?.Invoke($"Client {UID} disconnected.");
            }
        }

        public async Task SendPacketAsync(ChatPacket packet)
        {
            try
            {
                byte[] data = packet.Serialize();
                byte[] lengthBuffer = BitConverter.GetBytes(data.Length);
                await _stream.WriteAsync(lengthBuffer, 0, lengthBuffer.Length);

                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
            }
            catch
            {
            }
        }
    }
}