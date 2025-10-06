using Ai_Project.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ai_Project.Services
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string model = "llama2:latest" ;

        public OllamaService(string baseUrl = "http://localhost:11434")
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient();
        }

        public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
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
            while (!reader.EndOfStream)
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
