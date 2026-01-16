using System;
using System.IO;
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

        private FileStream? _activeFileStream;
        private string _storagePath;

        public ClientHandler(TcpClient client, Action<string> logAction, ServerListener server)
        {
            ClientSocket = client;
            _stream = client.GetStream();
            _logAction = logAction;
            _server = server;
            UID = Guid.NewGuid().ToString();
            Username = "Unknown";

            _storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServerFiles");
            if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
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

                        switch (packet.Type)
                        {
                            case PacketType.Chat:
                                this.Username = packet.Username!;
                                _logAction?.Invoke($"[{packet.Username}]: {packet.Message}");
                                _server.BroadcastPacket(packet);
                                break;

                            // --- MỚI THÊM: XỬ LÝ TYPING ---
                            case PacketType.Typing:
                                // Cập nhật tên nếu có (để đảm bảo danh tính)
                                if (!string.IsNullOrEmpty(packet.Username)) this.Username = packet.Username;
                                // Chuyển tiếp ngay cho mọi người biết
                                _server.BroadcastPacket(packet);
                                break;
                            // ------------------------------

                            case PacketType.FileHeader:
                                if (!string.IsNullOrEmpty(packet.Username))
                                {
                                    this.Username = packet.Username;
                                }

                                string cleanFileName = Path.GetFileName(packet.Message!);
                                string savePath = Path.Combine(_storagePath, cleanFileName);
                                _logAction?.Invoke($"Receiving file: {cleanFileName} from {this.Username}...");
                                _activeFileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
                                break;

                            case PacketType.FileChunk:
                                if (_activeFileStream != null && packet.Data != null)
                                {
                                    if (packet.Data.Length == 0) // Hết file
                                    {
                                        _activeFileStream.Close();
                                        _activeFileStream = null;
                                        _logAction?.Invoke($"File saved: {packet.Message}");

                                        var notiPacket = new ChatPacket(PacketType.Chat, this.Username, $"[FILE_UPLOADED]|{packet.Message}");
                                        _server.BroadcastPacket(notiPacket);
                                    }
                                    else
                                    {
                                        await _activeFileStream.WriteAsync(packet.Data, 0, packet.Data.Length);
                                    }
                                }
                                break;

                            case PacketType.FileReq:
                                string reqFileName = packet.Message!;
                                string filePath = Path.Combine(_storagePath, reqFileName);

                                if (File.Exists(filePath))
                                {
                                    _logAction?.Invoke($"Sending file {reqFileName} to {this.Username}...");
                                    _ = SendFileToClient(filePath);
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logAction?.Invoke($"Error: {ex.Message}");
                _activeFileStream?.Close();
            }
            finally
            {
                ClientSocket.Close();
                _activeFileStream?.Close();
                _logAction?.Invoke($"Client {UID} disconnected.");
            }
        }

        private async Task SendFileToClient(string filePath)
        {
            string fileName = Path.GetFileName(filePath);

            var header = new ChatPacket(PacketType.FileHeader, "Server", fileName);
            await SendPacketAsync(header);

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[1024 * 20];
                int bytesRead;
                while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] chunkData = new byte[bytesRead];
                    Array.Copy(buffer, chunkData, bytesRead);

                    var chunk = new ChatPacket(PacketType.FileChunk, "Server", fileName, chunkData);
                    await SendPacketAsync(chunk);
                }
            }

            var endPacket = new ChatPacket(PacketType.FileChunk, "Server", fileName, new byte[0]);
            await SendPacketAsync(endPacket);
            _logAction?.Invoke($"Sent file {fileName} to {Username} successfully.");
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
            catch { }
        }
    }
}