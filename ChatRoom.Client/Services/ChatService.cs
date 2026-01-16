using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using ChatRoom.Core; // Bắt buộc có dòng này để dùng ChatPacket
using System.IO;

namespace ChatRoom.Client.Services
{
    public class ChatService
    {
        private TcpClient _client;
        private NetworkStream _stream;

        // Sự kiện bắn log ra ngoài UI
        public event Action<string>? OnLog;

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
                var packet = new ChatPacket()
                {
                    Type = PacketType.Chat,
                    Username = username,
                    Message = content
                };

                byte[] data = packet.Serialize();

                byte[] lengthBuffer = BitConverter.GetBytes(data.Length);
                await _stream.WriteAsync(lengthBuffer, 0, lengthBuffer.Length);

                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error sending message: {ex.Message}");
            }
        }
    }
}