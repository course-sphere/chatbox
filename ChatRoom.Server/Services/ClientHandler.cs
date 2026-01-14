using System.Net.Sockets;

namespace ChatRoom.Server.Services
{
    public class ClientHandler
    {
        public TcpClient ClientSocket { get; set; }
        public string Username { get; set; } 
        public string UID { get; set; }       

        public ClientHandler(TcpClient client)
        {
            ClientSocket = client;
            UID = System.Guid.NewGuid().ToString();
            Username = "Unknown";
        }
    }
}