namespace Ai_Project.DTO
{
    public class ResponseDTO
    {
        public string model { get; set; } = string.Empty;

        public DateTime created_at { get; set; } = DateTime.MinValue;

        public string response { get; set; } = string.Empty;

        public bool done { get; set; } = false;

        public string done_reason { get; set; } = string.Empty;

        public int[] context { get; set; } = Array.Empty<int>();

        public ulong total_duration { get; set; } = 0;

        public ulong load_duration { get; set; } = 0;

        public uint prompt_eval_count { get; set; } = 0;

        public ulong prompt_eval_duration { get; set; } = 0;

        public uint eval_count { get; set; } = 0;

        public ulong eval_duration { get; set; } = 0;

    }
}

