using System;

namespace ChatRoom.Client.Models
{
    public class ChatMessage
    {
        public string? Sender { get; set; }
        public string? Content { get; set; }
        public DateTime Time { get; set; }
        public bool IsMe { get; set; }
        public string? Color { get; set; }

        public bool IsFile { get; set; }
    }
}