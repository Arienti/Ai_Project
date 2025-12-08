namespace Run_Llama_cpp
{
    public class LLamaFlagsValuesDTO
    {
        public uint Threads { get; set; }

        public uint ContextSize { get; set; } = 1024; //default context size

        public float Temp { get; set; }

        public float Top_p { get; set; } = (float)0.9;

        public uint Ngl { get; set; }

        public uint max_token { get; set; } = 128; //default max tokens to predict
    }
}
