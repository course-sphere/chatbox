using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using ChatRoom.Core;

namespace ChatRoom.Client.Services
{
    public class ChatService
    {
        private TcpClient _client;
        private NetworkStream _stream;

        public event Action<string> OnLog;
        public event Action<ChatPacket> OnMessageReceived;

        private FileStream? _downloadStream;
        public string DownloadFolderPath { get; set; } = "";

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
            await SendPacketInternalAsync(new ChatPacket(PacketType.Chat, username, content));
        }

        // --- HÀM MỚI: GỬI TÍN HIỆU ĐANG GÕ PHÍM ---
        public async Task SendTypingAsync(string username)
        {
            // Gửi gói tin Typing với nội dung rỗng (không cần thiết)
            await SendPacketInternalAsync(new ChatPacket(PacketType.Typing, username, ""));
        }
        // ------------------------------------------

        public async Task UploadFileAsync(string filePath, string username)
        {
            string fileName = Path.GetFileName(filePath);
            try
            {
                await SendPacketInternalAsync(new ChatPacket(PacketType.FileHeader, username, fileName));

                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[1024 * 20];
                    int bytesRead;
                    while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        byte[] chunkData = new byte[bytesRead];
                        Array.Copy(buffer, chunkData, bytesRead);
                        await SendPacketInternalAsync(new ChatPacket(PacketType.FileChunk, username, fileName, chunkData));
                    }
                }
                await SendPacketInternalAsync(new ChatPacket(PacketType.FileChunk, username, fileName, new byte[0]));
                OnLog?.Invoke($"Uploaded file: {fileName}");
            }
            catch (Exception ex) { OnLog?.Invoke($"Upload error: {ex.Message}"); }
        }

        public async Task RequestDownloadAsync(string fileName)
        {
            await SendPacketInternalAsync(new ChatPacket(PacketType.FileReq, "Me", fileName));
        }

        private async Task SendPacketInternalAsync(ChatPacket packet)
        {
            if (_client == null || !_client.Connected) return;
            byte[] data = packet.Serialize();
            byte[] lengthBuffer = BitConverter.GetBytes(data.Length);
            await _stream.WriteAsync(lengthBuffer, 0, lengthBuffer.Length);
            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();
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

                        if (packet.Type == PacketType.FileHeader)
                        {
                            string savePath = Path.Combine(DownloadFolderPath, packet.Message!);
                            _downloadStream = new FileStream(savePath, FileMode.Create, FileAccess.Write);
                            OnLog?.Invoke($"Downloading {packet.Message}...");
                        }
                        else if (packet.Type == PacketType.FileChunk)
                        {
                            if (_downloadStream != null && packet.Data != null)
                            {
                                if (packet.Data.Length == 0)
                                {
                                    _downloadStream.Close();
                                    _downloadStream = null;
                                    OnLog?.Invoke($"Download finished: {packet.Message}");
                                }
                                else
                                {
                                    await _downloadStream.WriteAsync(packet.Data, 0, packet.Data.Length);
                                }
                            }
                        }
                        else
                        {
                            OnMessageReceived?.Invoke(packet);
                        }
                    }
                }
            }
            catch { }
        }
    }
}