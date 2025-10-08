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
        private readonly string model = "phi:latest";

        // Async event for streaming partial responses
        public event Func<string, Task>? OnPartialResponseReceived;

        public OllamaService(string baseUrl = "http://localhost:11434")
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient();
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

        public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
        {
            RequestDTO request = new RequestDTO
            {
                model = model,
                prompt = prompt,
                stream = false
            };
            //_httpClient.Timeout = TimeSpan.FromMinutes(2);
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content, cancellationToken);
            response.EnsureSuccessStatusCode();

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
                    {
                        builder.Append(chunk.Response);

                        // Send letter by letter to event subscribers
                        foreach (var ch in chunk.Response)
                        {
                            await InvokePartialResponseReceivedAsync(ch.ToString());
                        }
                    }
                }
                catch
                {
                    // You might want to log or handle errors here
                }
            }

            return builder.ToString();
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
