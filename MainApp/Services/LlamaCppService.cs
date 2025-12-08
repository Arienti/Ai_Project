using Ai_Project.DTO;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Ai_Project
{
    public class LlamaCppService
    {
        private readonly string _rootDir;
        private readonly string _exePath;
        private readonly string _modelsDir;

        // Predefined URLs
        private readonly string _exeZipUrl = "https://github.com/ggerganov/llama.cpp/releases/download/0.1.0/llama.cpp-win64.zip";
        private readonly string _modelUrl = "https://huggingface.co/TheBloke/Llama-2-7B-GGUF/resolve/main/Llama-2-7B.gguf";
        private readonly string _modelName = "Llama-2-7B.gguf";

        public LlamaCppService()
        {
            _rootDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Service_LLaMa", "llama_cpp");
            _exePath = Path.Combine(_rootDir, "llama-cli.exe"); // Will be extracted from zip
            _modelsDir = Path.Combine(_rootDir, "models");

            Directory.CreateDirectory(_rootDir);
            Directory.CreateDirectory(_modelsDir);
        }

        /// <summary>
        /// Ensure both llama.cpp executable and model are installed
        /// </summary>
        public async Task EnsureInstalledAsync()
        {
            await EnsureExecutableAsync();
            await EnsureModelAsync();
        }

        /// <summary>
        /// Ensure prebuilt llama.cpp executable exists
        /// </summary>
        private async Task EnsureExecutableAsync()
        {
            if (File.Exists(_exePath))
            {
                Debug.WriteLine("Llama.cpp executable already exists.");
                return;
            }

            string zipPath = Path.Combine(_rootDir, "llama.zip");

            Debug.WriteLine("Downloading prebuilt llama.cpp...");
            using var client = new WebClient();
            await client.DownloadFileTaskAsync(_exeZipUrl, zipPath);

            Debug.WriteLine("Extracting llama.cpp...");
            ZipFile.ExtractToDirectory(zipPath, _rootDir, true);
            File.Delete(zipPath);

            if (!File.Exists(_exePath))
                throw new Exception("Failed to extract llama.cpp executable.");

            Debug.WriteLine("Llama.cpp ready at: " + _exePath);
        }

        /// <summary>
        /// Ensure GGUF model exists
        /// </summary>
        private async Task EnsureModelAsync()
        {
            string modelPath = Path.Combine(_modelsDir, _modelName);
            if (File.Exists(modelPath))
            {
                Debug.WriteLine("Model already exists: " + _modelName);
                return;
            }

            Debug.WriteLine("Downloading model: " + _modelName);
            using var client = new WebClient();
            await client.DownloadFileTaskAsync(_modelUrl, modelPath);
            Debug.WriteLine("Downloaded model to: " + modelPath);
        }

        /// <summary>
        /// Run the model with a prompt
        /// </summary>
        private Process? serverProcess;

        public void Start()
        {
            string exePath = @"D:\Ai_Project\bin\Debug\net8.0-windows\Service_LLaMa\llama_cpp\llama-server.exe";
            string modelFolder = @"D:\Ai_Project\bin\Debug\net8.0-windows\Downloads\ai21labs\AI21-Jamba-Reasoning-3B-GGUF";
            string modelFile = Path.Combine(modelFolder, "jamba-reasoning-3b-F16.gguf");

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"-m \"{modelFile}\" --ctx-size 4096 --port 8080",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            serverProcess = Process.Start(psi);
        }

        public void Stop()
        {
            if (serverProcess != null && !serverProcess.HasExited)
                serverProcess.Kill();
        }

        public async Task<string> AskAsync(string prompt)
        {
            var client = new HttpClient();
            RequestDTO request = new RequestDTO
            {
                prompt = prompt,
                model = "local",
                max_new_tokens = 500,
                stream = false
            };

            string json = System.Text.Json.JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            string url = "http://127.0.0.1:8080/v1/completions";
            try
            {
                var response = await client.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
                string result = await response.Content.ReadAsStringAsync();
                return result;
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine("Request timed out: " + ex.Message);
                return "Timeout";
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine("Request failed: " + ex.Message);
                return "Failed";
            }

        }

        private Process _llamaProcess;
        private readonly StringBuilder _outputBuffer = new();

        //public string RunModel(string modelPath, string prompt, int nPredict = 20, int threads = 8)
        //{
        //    string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Service_LLaMa", "llama_cpp", "llama-cli.exe");
        //    if (!File.Exists(exePath)) throw new Exception("Llama CLI executable not found.");
        //    if (!File.Exists(modelPath)) throw new Exception("Model not found: " + modelPath);

        //    var outputBuilder = new System.Text.StringBuilder();
        //    bool startCollecting = false;

        //    using (var process = new Process())
        //    {
        //        process.StartInfo = new ProcessStartInfo
        //        {
        //            FileName = exePath,
        //            Arguments = $"--model \"{modelPath}\" --n_predict {nPredict} --threads {threads} --prompt \"{prompt}\"",
        //            RedirectStandardOutput = true,
        //            UseShellExecute = false,
        //            CreateNoWindow = true,
        //            StandardOutputEncoding = System.Text.Encoding.UTF8
        //        };

        //        process.OutputDataReceived += (sender, e) =>
        //        {
        //            if (string.IsNullOrEmpty(e.Data))
        //                return;

        //            if (!startCollecting && e.Data.Trim().Equals("assistant", StringComparison.OrdinalIgnoreCase))
        //            {
        //                startCollecting = true;
        //                return;
        //            }

        //            if (startCollecting)
        //                outputBuilder.AppendLine(e.Data);
        //        };

        //        process.Start();
        //        process.BeginOutputReadLine();

        //        // Wait for process to exit
        //        process.WaitForExit();

        //        // Wait for asynchronous output reading to finish
        //        process.WaitForExit(); // optional but ensures output is flushed
        //    }

        //    return outputBuilder.ToString().Trim();
        //}


        public async Task<string> SendPromptAsync(string prompt, int timeoutMs = 100000)
        {
            if (_llamaProcess == null || _llamaProcess.HasExited)
                throw new Exception("Llama process is not running");

            var tcs = new TaskCompletionSource<string>();
            bool collecting = false;
            var tempBuffer = new StringBuilder();

            void handler(object s, DataReceivedEventArgs e)
            {
                if (e.Data == null) return;

                if (!collecting && e.Data.Trim().Equals("assistant", StringComparison.OrdinalIgnoreCase))
                {
                    collecting = true;
                    return; // skip the "assistant" line
                }

                if (collecting)
                {
                    tempBuffer.AppendLine(e.Data);
                    if (e.Data.Trim().EndsWith("<|endoftext|>"))
                    {
                        tcs.TrySetResult(tempBuffer.ToString().Replace("<|endoftext|>", "").Trim());
                    }
                }
            }

            _llamaProcess.OutputDataReceived += handler;

            await _llamaProcess.StandardInput.WriteLineAsync(prompt);

            var resultTask = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
            _llamaProcess.OutputDataReceived -= handler;

            if (resultTask == tcs.Task)
                return tcs.Task.Result;

            return "No response received in time";
        }


        public async Task<string> RunLlamaAsync(string modelPath, string prompt, int nPredict = 100, int threads = 8, int timeoutMs = 60000)
        {
            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Service_LLaMa", "llama_cpp", "llama-run.exe");
            if (!File.Exists(exePath)) throw new Exception("llama-run.exe not found");
            if (!File.Exists(modelPath)) throw new Exception("Model not found: " + modelPath);

            var outputBuilder = new StringBuilder();
            using var cts = new CancellationTokenSource(timeoutMs);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"--threads {threads} --context-size 1024 \"{modelPath}\" \"{prompt}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                },
                EnableRaisingEvents = true
            };

            var tcs = new TaskCompletionSource<string>();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            //process.ErrorDataReceived += (sender, e) =>
            //{
            //    if (!string.IsNullOrEmpty(e.Data))
            //        Debug.WriteLine("ERR: " + e.Data);
            //};

            process.Exited += (sender, e) =>
            {
                tcs.TrySetResult(outputBuilder.ToString().Trim());
                process.Dispose();
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using (cts.Token.Register(() =>
            {
                if (!process.HasExited)
                {
                    try { process.Kill(); } catch { }
                    tcs.TrySetResult(outputBuilder.ToString().Trim());
                }
            }))
            {
                return await tcs.Task;
            }
        }

        public async Task<string> RunModelAsync(string modelPath, string prompt, int nPredict = 100, int threads = 8)
        {
            string exePath = @"D:\Ai_Project\bin\Debug\net8.0-windows\Service_LLaMa\llama_cpp\llama-run.exe";
            if (!System.IO.File.Exists(exePath)) throw new Exception("llama-run.exe not found");
            if (!System.IO.File.Exists(modelPath)) throw new Exception("Model not found: " + modelPath);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"--threads {threads} --context-size 4096 \"{modelPath}\" \"{prompt}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };

            process.Start();

            // Read all output after process ends
            string stdout = await process.StandardOutput.ReadToEndAsync();
            string stderr = await process.StandardError.ReadToEndAsync();

            process.WaitForExit();
            //process.Dispose();
            string response = stdout.Trim().Replace("\u001b[0m", "");
            return response;
        }
    }
}
