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

        public ClientHandler(TcpClient client, Action<string> logAction)
        {
            ClientSocket = client;
            _stream = client.GetStream();
            _logAction = logAction;
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

                        _logAction?.Invoke($"[{packet.Username}]: {packet.Message}");
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
    }
}