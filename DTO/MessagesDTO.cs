using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ai_Project.DTO
{
    public class MessagesDTO
    {
        [ForeignKey("TopicDTO")]
        public uint TopicId { get; set; }
        public TopicDTO TopicDTO { get; set; } = new TopicDTO();
        public uint ID { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
