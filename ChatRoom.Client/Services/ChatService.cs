using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ChatRoom.Client.Services
{
    public class ChatService
    {
        private TcpClient _client;

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

                OnLog?.Invoke("Connected successfully!");
                return true;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Connection failed: {ex.Message}");
                return false;
            }
        }
    }
}