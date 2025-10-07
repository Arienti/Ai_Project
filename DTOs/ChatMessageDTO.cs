using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ai_Project.DTOs
{
    public class ChatMessageDTO
    {
        public string Sender { get; set; } = string.Empty; // "User" or "AI"
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
