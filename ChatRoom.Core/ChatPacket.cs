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
        FileHeader,   
        FileChunk,   
        FileReq,     
        Typing,
        Disconnect
    }

    public class ChatPacket
    {
        public PacketType Type { get; set; }
        public string? Username { get; set; }
        public string? Message { get; set; }
        public byte[]? Data { get; set; }

        public ChatPacket() { }

        public ChatPacket(PacketType type, string? username, string? msg, byte[]? data = null)
        {
            Type = type;
            Username = username;
            Message = msg;
            Data = data;
        }

        public byte[] Serialize()
        {
            return JsonSerializer.SerializeToUtf8Bytes(this);
        }

        public static ChatPacket Deserialize(byte[] data)
        {
            return JsonSerializer.Deserialize<ChatPacket>(data)!;
        }
    }
}