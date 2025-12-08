namespace Ai_Project.DTO
{
    public class HuggingFaceModelDTO
    {
        public string modelId { get; set; } = string.Empty;
        
        public string author { get; set; } = string.Empty;
        
        public const bool gated = false;
        
        public string pipeline_tag { get; set; } = string.Empty;
        
        public DateTime? createdAt { get; set; }
        
        public string[] tags { get; set; } = new string[0];
        
        public string? license { get; set; } = null;
        
        public DateTime? lastModified { get; set; }
        
        public string sha { get; set; } = string.Empty;
        
        public ulong downloads { get; set; } = 0;
    }

    public class SiblingDTO
    {
        public string rfilename { get; set; } = string.Empty;
        // size may not be present in API response, so keep nullable
        public long? size { get; set; }
    }

    public class ModelInfoDTO
    {
        public string id { get; set; } = string.Empty;
        public string author { get; set; } = string.Empty;
        public bool private_ { get; set; }
        public DateTime createdAt { get; set; }
        public string sha { get; set; }
        public List<SiblingDTO> siblings { get; set; } = new();
        public long usedStorage { get; set; }
        public Gguf gguf { get; set; }
        // Total model size in GB from safetensors
    }
    public class Gguf
    {
        public ulong total { get; set; }
        public string architecture { get; set; } = string.Empty;
        public uint context_length { get; set; }

        public double model_size
        {
            get
            {
                return Math.Round(total / 1024.0 / 1024.0 / 1024.0, 2);
            }
        } 
    }
}
