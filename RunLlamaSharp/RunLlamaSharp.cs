using Get_Pc_Info;
using LLama;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using ModelsDTO;
using Run_LlamaSharp.DTOs;
using Run_LlamaSharp.Tools;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Run_LlamaSharp
{
    public class RunLlamaSharp
    {
        private NativeLogConfig.LLamaLogCallback? _llamaCallback;
        protected LLamaWeights? modelWeights;
        protected LLamaContext? context;
        protected StatelessExecutor? executor;
        protected InferenceParams? inferenceParams;

        private protected ModelDTO? SelectedModel { get; set; } = null;

        public Action<string, string>? OnLog;

        public virtual ModelDTO GetModelSelected()
        {
            return SelectedModel!;
        }

        public void UnloadModel()
        {
            SelectedModel = null;
            modelWeights = null;
            context = null;
            executor = null;
            inferenceParams = null;
        }

        public virtual async Task<ResultDTO> InitializeAsync(ModelDTO model)
        {
            SelectedModel = model;
            return await LoadModelAsync();
        }

        private int CalculateMaxContext(GgufMetadata meta)
        {
            // 1. Total and usable RAM (leave 1 GB for system)
            double totalRamMb = GetPcInfo.TotalRam / 1024.0 / 1024.0;
            double reservedSystemMb = 1024; // 1 GB
            double usableRamMb = Math.Max(totalRamMb - reservedSystemMb, 512);

            // 2. Estimate model memory in MB
            // Model memory ≈ NLayers * Dim^2 * bytes per parameter
            double bytesPerParam = meta.FType.ToLower() switch
            {
                var q when q.Contains("q4") => 0.5, // 4-bit ≈ 0.5 byte
                var f when f.Contains("f16") || f.Contains("bf16") => 2.0, // 16-bit float
                var f when f.Contains("f32") => 4.0, // 32-bit float
                _ => 2.0
            };

            double modelMemoryMb = meta.NLayers * Math.Pow(meta.Dim, 2) * bytesPerParam / (1024 * 1024);

            // 3. Memory left for context
            double memoryForContextMb = Math.Max(usableRamMb - modelMemoryMb, 256); // safety floor

            // 4. Estimate memory per token (adjust for quantization & model size)
            double baselineModelB = 13.0; // reference model
            double modelSizeB = meta.ModelName.Contains("B") ?
                                double.Parse(Regex.Match(meta.ModelName, @"(\d+(\.\d+)?)b", RegexOptions.IgnoreCase).Groups[1].Value) : 13.0;
            double factor = meta.FType.ToLower() switch
            {
                var q when q.Contains("q4") => 0.125,
                var f when f.Contains("f16") || f.Contains("bf16") => 0.5,
                var f when f.Contains("f32") => 1.0,
                _ => 0.5
            };

            double memPerTokenMb = factor * (modelSizeB / baselineModelB);

            // 5. Calculate max context tokens
            int maxContext = (int)(memoryForContextMb / memPerTokenMb);

            // 6. Do not exceed model default context
            return (int)Math.Min(maxContext, meta.NCtx);
        }


        private async Task<ResultDTO> LoadModelAsync()
        {
            ModelDTO model = GetModelSelected();

            if (model == null)
                throw new InvalidOperationException("No model selected.");
            _llamaCallback = new NativeLogConfig.LLamaLogCallback(LogHandler);
           
            NativeLogConfig.llama_log_set(_llamaCallback);
            
            var ggufMetadata = GgufMetadata.ReadFromFile(model.path);

            int threads = GetPcInfo.CpuList.Sum(t => t.PhysicalCores);
            int gpuCount = GetPcInfo.Gpus.Count;


            var modelParams = new ModelParams(model.path)
            {
                Threads = threads > 4 ? threads - 2 : threads,
                ContextSize = (uint)CalculateMaxContext(ggufMetadata),
                UseMemorymap = true,
                GpuLayerCount = (int)ggufMetadata.NLayers,
                MainGpu = gpuCount > 0 ? 0 : -1,

            };
            try
            {
                modelWeights = LLamaWeights.LoadFromFile(modelParams);
                context = modelWeights.CreateContext(modelParams);

                executor = new StatelessExecutor(modelWeights, modelParams);

                inferenceParams = new InferenceParams
                {
                    SamplingPipeline = new DefaultSamplingPipeline
                    {
                        Temperature = 0.85f,
                        TopP = 0.95f,
                        TopK = 40,
                        RepeatPenalty = 1.1f
                    },
                    DecodeSpecialTokens = true,
                    AntiPrompts = new List<string> { "[END_OF_RESPONSE]" }
                };

                await Task.CompletedTask;

                return ResultDTO.Success("Model loaded successfully.");
            }
            catch (Exception ex)
            {
                return ResultDTO.Fail($"Failed to load model: {ex.Message}");
            }
        }

        private void LogHandler(LLamaLogLevel level, string message)
        {
            // Do whatever you want with the log
            OnLog?.Invoke($"[{level}]", $"{message}"); // or append to a TextBox in WPF
        }

        private readonly SemaphoreSlim _inferLock = new(1, 1);

        private async Task<string> GetResponse(string prompt)
        {
            if (executor == null)
                throw new InvalidOperationException("Model not initialized.");

            var sb = new StringBuilder();

            // Wait asynchronously to enter the "lock"
            await _inferLock.WaitAsync();
            try
            {
                await foreach (var t in executor.InferAsync(prompt, inferenceParams!)
                    .WithCancellation(CancellationToken.None)
                    .ConfigureAwait(false))
                {
                    sb.Append(t);
                }
            }
            finally
            {
                _inferLock.Release(); // release for next call
            }

            string response = Regex.Replace(sb.ToString(), "<think>.*?</think>", "", RegexOptions.Singleline)
                                   .Replace("[END_OF_RESPONSE]", "")
                                   .Replace("��", "")
                                   .Trim();

            return response;
        }


        public async Task<string> GenerateResponse(List<(string role, string content)> conversation)
        {
            // Get the latest user message
            string query = conversation.LastOrDefault(msg => msg.role == "user").content;
            if (string.IsNullOrEmpty(query))
                throw new ArgumentException("Prompt cannot be null or empty.", nameof(query));

            if (context == null)
                throw new InvalidOperationException("context is null.");
            if (modelWeights == null)
                throw new InvalidOperationException("model is not loaded");
            // Reserve tokens for generation
            int modelGeneration = context.ContextSize <= 2048 ? 1200 : 1000;
            int tokenLimit = (int)context.ContextSize - modelGeneration; // tokens available for history

            var trimmedConversation = new List<(string role, string content)>();
            int totalTokens = 0;

            // iterate from last to first to keep most recent messages
            for (int i = 0; i < conversation.Count; i++)
            {
                var msg = conversation[i];
                var tokens = modelWeights.Tokenize(msg.content, false, false, Encoding.UTF8);

                if (totalTokens + tokens.Length > tokenLimit)
                    continue; // skip older messages if over limit

                trimmedConversation.Add(msg);
                totalTokens += tokens.Length;
            }

            // Ensure the latest user message is included
            var lastUserMsg = conversation.LastOrDefault(m => m.role == "user");
            if (lastUserMsg != default && !trimmedConversation.Contains(lastUserMsg))
                trimmedConversation.Add(lastUserMsg);

            // Generate prompt from truncated conversation
            string prompt = GenerateTemplate(trimmedConversation);

            // Run model inference asynchronously
            return await Task.Run(() => GetResponse(prompt));
        }

        private string GenerateTemplate(List<(string role, string content)> conversation)
        {
            string m = SelectedModel!.path.ToLower();

            // Detect model type by filename
            bool isBase = m.Contains("base");
            bool isInstruct = m.Contains("inst"); // check if is instruct model
            bool isReasoning = m.Contains("reason");
            bool isThinking = m.Contains("think");
            bool isCode = m.Contains("code");
            bool isMath = m.Contains("math");

            // Chat is the default category
            bool isChat = !(isBase || isInstruct || isReasoning || isThinking || isCode || isMath);

            var sb = new StringBuilder();

            // ============================
            // 1) INSTRUCT TEMPLATE
            // ============================
            if (isInstruct)
            {
                string systemInstruction =
                    "You are a helpful assistant. Do NOT generate <think>, notes, meta comments, analysis, or reasoning explanations. Only answer the user. End all responses with [END_OF_RESPONSE].";

                sb.AppendLine("[INST] <<SYS>>");
                sb.AppendLine(systemInstruction);
                sb.AppendLine("<</SYS>>");

                foreach (var msg in conversation)
                {
                    if (msg.role == "user")
                        sb.AppendLine($"[INST] {msg.content} [/INST]");
                    else if (msg.role == "assistant")
                        sb.AppendLine(msg.content);
                }

                sb.Append("[INST] "); // prepare next instruction
                return sb.ToString();
            }

            // ============================
            // 2) BASE MODEL — plain prompt
            // ============================
            if (isBase)
            {
                sb.AppendLine("You are a helpful assistant. Answer clearly. End all responses with [END_OF_RESPONSE].");
                sb.AppendLine();

                foreach (var msg in conversation)
                    sb.AppendLine($"{msg.role}: {msg.content}");

                sb.Append("assistant: ");
                return sb.ToString();
            }

            // ============================
            // 3) CHAT TEMPLATE (default)
            // ============================
            string systemText;

            if (isThinking)
            {
                systemText =
                    "Provide your reasoning inside <think> tags. You may include step-by-step thoughts. End all responses with [END_OF_RESPONSE].";
            }
            else if (isCode)
            {
                systemText =
                    "You are a helpful coding assistant. Provide code when needed. Do NOT output <think> unless explicitly required. End all responses with [END_OF_RESPONSE].";
            }
            else if (isReasoning || isChat)
            {
                systemText =
                    "You are a helpful assistant. Never output <think>, chain-of-thought, internal reasoning, or explanations of your thinking. End all responses with [END_OF_RESPONSE].";
            }
            else if (isMath)
            {
                systemText =
                    "You are a helpful math assistant. Explain briefly without internal reasoning. End all responses with [END_OF_RESPONSE].";
            }
            else
            {
                systemText = "Complete the prompt as best as possible. End all responses with [END_OF_RESPONSE].";
            }

            // === LLaMA 3 Chat Format ===
            sb.AppendLine("<|im_start|>system");
            sb.AppendLine(systemText);
            sb.AppendLine("<|im_end|>");

            foreach (var msg in conversation)
            {
                sb.AppendLine($"<|im_start|>{msg.role}");
                sb.AppendLine(msg.content);
                sb.AppendLine("<|im_end|>");
            }

            sb.Append("<|im_start|>assistant\n");

            return sb.ToString();
        }
    }
}