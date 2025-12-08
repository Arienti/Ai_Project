namespace Run_LlamaSharp
{
    public class FlagsValuesDTO
    {
        public uint Threads { get; set; }

        public uint ContextSize { get; set; } = 2048; //default context size

        public float Temp { get; set; } = (float)0.7;

        public float Top_p { get; set; } = (float)0.9;

        public uint Ngl { get; set; } = 0;
    }
}
