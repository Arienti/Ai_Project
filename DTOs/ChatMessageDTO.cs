using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ai_Project.DTOs
{
    public class ChatDTO
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "New Chat";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastModified { get; set; } = DateTime.Now;
        public List<ChatMessageDTO> Messages { get; set; } = new();
    }

    public class ChatMessageDTO
    {
        public string Sender { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
