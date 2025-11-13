public class SiblingDTO
{
    public string rfilename { get; set; } = string.Empty;
    // size may not be present in API response, so keep nullable
    public long? size { get; set; }
}

public class SafetensorsDTO
{
    public Dictionary<string, long> parameters { get; set; } = new();
    public long total { get; set; } // total size in bytes
}

public class ModelInfoDTO
{
    public string id { get; set; } = string.Empty;
    public string author { get; set; } = string.Empty;
    public bool private_ { get; set; }
    public DateTime createdAt { get; set; }
    public string sha { get; set; }
    public List<SiblingDTO> siblings { get; set; } = new();
    public SafetensorsDTO safetensors { get; set; } = new();
    public long usedStorage { get; set; }
    // Total model size in GB from safetensors
    public double ModelSizeGb => Math.Round(safetensors.total / 1024.0 / 1024.0 / 1024.0, 2);

    // Total storage including all repo files
    public double UsedStorageGb => Math.Round((double)(siblings?.Sum(s => s.size ?? 0) ?? 0 + safetensors.total) / 1024 / 1024 / 1024, 2);
}
