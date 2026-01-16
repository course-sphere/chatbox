using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using ChatRoom.Core; // Để dùng ChatPacket

namespace ChatRoom.Client.Services
{
    public class ChatService
    {
        private TcpClient _client;
        private NetworkStream _stream;

        public event Action<string> OnLog;
        public event Action<ChatPacket> OnMessageReceived;

        public ChatService()
        {
            _client = new TcpClient();
        }

        public async Task<bool> ConnectAsync(string ip, int port)
        {
            try
            {
                OnLog?.Invoke($"Connecting to {ip}:{port}...");
                await _client.ConnectAsync(ip, port);
                _stream = _client.GetStream();

                OnLog?.Invoke("Connected successfully!");
                return true;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Connection failed: {ex.Message}");
                return false;
            }
        }

        public async Task SendMessageAsync(string content, string username)
        {
            if (_client == null || !_client.Connected) return;

            try
            {
                var packet = new ChatPacket(PacketType.Chat, username, content);
                byte[] data = packet.Serialize();
                byte[] lengthBuffer = BitConverter.GetBytes(data.Length);
                await _stream.WriteAsync(lengthBuffer, 0, lengthBuffer.Length);
                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error sending: {ex.Message}");
            }
        }

        public async Task StartReadingLoop()
        {
            try
            {
                while (_client.Connected)
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

                        var packet = ChatPacket.Deserialize(packetBuffer);

                        OnMessageReceived?.Invoke(packet);
                    }
                }
            }
            catch
            {
            }
        }
    }
}