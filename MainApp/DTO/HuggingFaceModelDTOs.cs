namespace Ai_Project.DTO
{
    public class HuggingFaceModelDTO
    {
        public string id { get; set; } = string.Empty;
        
        public string pipeline_tag { get; set; } = string.Empty;
        
        public ulong downloads { get; set; } = 0;

        public string[]? tags { get; set; }

        public ModelInfoDTO? _modelInfoDto { get; set; } = null;
    }

    public class ModelInfoDTO
    {
        public string _id { get; set; } = string.Empty;
        public string id { get; set; } = string.Empty;
        public string author { get; set; } = string.Empty;
        public DateTime createdAt { get; set; }
        public string? sha { get; set; }
        public DateTime? lastModified { get; set; }
        public List<SiblingDTO>? siblings { get; set; }
        public Gguf? gguf { get; set; }
    }

    public class Gguf
    {
        public string architecture { get; set; } = string.Empty;
        public string? chat_template { get; set; } = null;
    }

    public class SiblingDTO
    {
        public string rfilename { get; set; } = string.Empty;
        public long? size { get; set; }
        public double _sizeGB
        {
            get
            {
                return size.HasValue ? Math.Round(size.Value / 1024.0 / 1024.0 / 1024.0, 2) : 0;
            }
        }
        //public bool _isDownloaded { get; set; } = false;
    }
}
