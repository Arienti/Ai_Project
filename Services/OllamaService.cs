using Ai_Project.DTO;
using Ai_Project.DTOs;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Ai_Project.Services
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string model = "gemma3:4b";

        // Async event for streaming partial responses
        public event Func<string, Task>? OnPartialResponseReceived;

        public OllamaService(string baseUrl = "http://localhost:11434")
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient();
            _httpClient.Timeout = Timeout.InfiniteTimeSpan; // No timeout for long-running requests
        }

        // Helper to invoke all async event handlers properly
        private async Task InvokePartialResponseReceivedAsync(string partial)
        {
            if (OnPartialResponseReceived == null)
                return;

            var invocationList = OnPartialResponseReceived.GetInvocationList();

            foreach (Func<string, Task> handler in invocationList)
            {
                await handler(partial);
            }
        }

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                var request = new RequestDTO
                {
                    model = model,
                    prompt = prompt,
                    max_new_tokens = 500,
                    stream = false
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/generate")
                {
                    Content = content
                };

                // Infinite timeout (wait as long as needed)

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                using var response = await _httpClient.SendAsync(
                    requestMessage,
                    HttpCompletionOption.ResponseHeadersRead,
                    cts.Token
                );

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"Status: {response.StatusCode}, Body: {error}");
                }

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                var builder = new StringBuilder();

                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Handle “model is busy” message
                    if (line.Contains("model is currently busy", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception("Ollama model is currently busy. Try again later.");
                    }

                    try
                    {
                        var chunk = JsonSerializer.Deserialize<OllamaChunkDTO>(line);
                        if (chunk?.Response != null)
                        {
                            builder.Append(chunk.Response);

                            // Fire partial response event if needed
                            foreach (var ch in chunk.Response)
                                await InvokePartialResponseReceivedAsync(ch.ToString());
                        }
                    }
                    catch
                    {
                        // Ignore malformed JSON
                    }
                }

                return builder.ToString();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // Propagate user cancellation
            }
            finally
            {
                _semaphore.Release();
                //_httpClient.Timeout = TimeSpan.FromMilliseconds(1); // Restore (optional)
            }
        }

        public async Task<string> GenerateTopicAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var request = new
            {
                model = model,
                prompt
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Ollama streams line-delimited JSON — so we read it as a stream
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            var builder = new StringBuilder();
            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var chunk = JsonSerializer.Deserialize<OllamaChunkDTO>(line);
                    if (chunk?.Response != null)
                        builder.Append(chunk.Response);
                }
                catch
                {
                    // Optionally log invalid JSON lines
                }
            }

            return builder.ToString();
        }
    }
}
