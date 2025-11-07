using System.Collections.ObjectModel;

namespace Ai_Project.DTO
{
    public class TopicDTO
    {
        public string Topic { get; set; } = string.Empty;
        public uint ID { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool isFavorite { get; set; } = false;
        public List<MessagesDTO> Messages { get; set; } = new List<MessagesDTO>();
    }
}
