using System;
using System.Text.Json;
using System.Text;

namespace ChatRoom.Core
{
    public enum PacketType
    {
        Connect,
        Chat,
        Emoji,
        File,
        Disconnect
    }

    public class ChatPacket
    {
        public PacketType Type {  get; set; }
        public string? Username { get; set; }
        public string? Message { get; set; }
        public byte[]? Data { get; set; }

        public ChatPacket() { }

        public ChatPacket(PacketType type, string username, string msg)
        {
            Type = type;
            Username = username;
            Message = msg;
        }
        public byte[] Serialize()
        {
            string json = JsonSerializer.Serialize(this);
            return Encoding.UTF8.GetBytes(json);
        }

        public static ChatPacket Deserialize(byte[] data)
        {
            string json = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<ChatPacket>(json)!;
        }
    }
}
