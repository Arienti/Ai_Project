using Get_Pc_Info;

namespace Run_Llama_cpp.Tools
{
    public static class Flags
    {
        [Flags]
        public enum LlamaFlags
        {
            None = 0,
            Threads = 1 << 0,
            ContextSize = 1 << 1,
            Temp = 1 << 2,
            Top_P = 1 << 3,
            Ngl = 1 << 4,
            SingleTurn = 1 << 6,
            NoPrompt = 1 << 7,
        }

        private static readonly Dictionary<LlamaFlags, Func<LLamaFlagsValuesDTO, string>> _map =
            new()
            {
                { LlamaFlags.Threads,     v => $"--threads {v.Threads}" },
                { LlamaFlags.ContextSize, v => $"--ctx-size {v.ContextSize}" },
                { LlamaFlags.Temp,        v => $"--temp {v.Temp}" },
                { LlamaFlags.Top_P,       v => $"--top-p {v.Top_p}" },
                { LlamaFlags.Ngl,         v => $"--n-gpu-layers {v.Ngl}" },
         //       { LlamaFlags.SingleTurn,  v => $"--single-turn" },
                { LlamaFlags.NoPrompt,    v => $"--no-display-prompt" }
            };

        public static string Build(string modelPath)
        {
            // --- 1. Read metadata from GGUF ---
            var meta = GgufMetadata.ReadFromFile(modelPath);

            // --- 2. Build runtime values using metadata directly ---
            LLamaFlagsValuesDTO v = BuildValues(meta);

            // --- 3. Return flags string ---
            return string.Join(" ", _map.Select(e => e.Value(v)));
        }

        private static LLamaFlagsValuesDTO BuildValues(GgufMetadata meta)
        {
            // --- Threads ---
            uint threads = 0;
            foreach (var cpu in GetPcInfo.CpuList)
                threads += (uint)cpu.PhysicalCores;
            if (threads >= 6) threads -= 2;

            // --- FType from metadata ---
            string ftype = meta.FType.ToLower();

            // --- GPU detection ---
            int vRamMb = GetPcInfo.Gpus.Sum(g => (int)(g.VRAM / 1024 / 1024));
            bool hasGpu = vRamMb > 0;

            // --- Ngl based on FType and GPU ---
            int baseNgl = ftype switch
            {
                var s when s.Contains("q2") => 32,
                var s when s.Contains("q3") => 24,
                var s when s.Contains("q4") => 16,
                var s when s.Contains("q5") => 12,
                var s when s.Contains("q6") => 8,
                var s when s.Contains("q8") => 4,
                var s when s.Contains("f16") => 48,
                var s when s.Contains("f32") => 64,
                _ => 16
            };

            if (hasGpu)
            {
                if (vRamMb < 2048) baseNgl = Math.Min(baseNgl, 8);
                else if (vRamMb < 4096) baseNgl = Math.Min(baseNgl, 16);
                else if (vRamMb < 8192) baseNgl = Math.Min(baseNgl, 32);
            }
            else
            {
                baseNgl = 0; // CPU only
            }

            // --- Temperature based on FType ---
            float temp = ftype switch
            {
                var s when s.Contains("q2") => 0.7f,
                var s when s.Contains("q3") => 0.65f,
                var s when s.Contains("q4") => 0.6f,
                var s when s.Contains("q5") => 0.55f,
                var s when s.Contains("q6") => 0.5f,
                var s when s.Contains("q8") => 0.45f,
                var s when s.Contains("f16") => 0.8f,
                var s when s.Contains("f32") => 0.85f,
                _ => 0.8f
            };

            // --- Context Size ---
            const float tokenSizeMb = 0.005f;
            float usableRamMb = GetPcInfo.AvailableRam * 0.4f;
            uint calculatedCtx = (uint)(usableRamMb / tokenSizeMb);

            // Limit context size using GGUF metadata if present
            uint modelLimit = meta.NCtx > 0 ? meta.NCtx : 4096u;
            uint contextSize = Math.Clamp(calculatedCtx, 512u, modelLimit);

            return new LLamaFlagsValuesDTO
            {
                Threads = threads,
                ContextSize = contextSize,
                Temp = temp,
                Ngl = (uint)baseNgl
            };
        }
    }
}