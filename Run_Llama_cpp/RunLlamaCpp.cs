using Run_Llama_cpp.Tools;
using System.Diagnostics;
using System.Text;

namespace Run_Llama_cpp
{
    public class RunLlamaCpp
    {
        private LlamaEngineSelector _engineSelector;

        public RunLlamaCpp()
        {
            _engineSelector = new LlamaEngineSelector();
        }
        public async Task<string> RunLlama(string modelPath, string prompt, CancellationToken cancellationToken = default)
        {
            string exePath = await _engineSelector.SelectEngine();

            if (!File.Exists(exePath))
                throw new FileNotFoundException("llama-cli.exe not found", exePath);

            if (!File.Exists(modelPath))
                throw new FileNotFoundException("Model not found", modelPath);

            var outputBuilder = new StringBuilder();

            // Build arguments: all flags from Flags.Build + model + prompt + safe non-interactive options
            string arguments = $@"{Flags.Build(modelPath)} -m ""{modelPath}"" -p ""{prompt}""";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };

            // Capture stdout
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    // Trim to remove leading/trailing whitespace
                    string line = e.Data.Trim();
                    line = line.Replace("[end of text]", "")
                               .Replace("??", "");
                    // Skip [end of text]

                    outputBuilder.AppendLine(line);
                }
            };

            // Capture stderr but only for debugging
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.WriteLine("[llama-cli ERR] " + e.Data);
            };

            try
            {
                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait until the process exits or cancellation is requested
                await process.WaitForExitAsync(cancellationToken);

                string response = outputBuilder.ToString().Trim();
                return response;
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    try { process.Kill(); } catch { }
                }
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to run llama-cli: " + ex.Message);
                throw;
            }
        }
    }
}