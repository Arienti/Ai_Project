using Ai_Project.DTO;
using Ai_Project.Model_Manager;
using ModelsDTO;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace Ai_Project.Services
{
    public class HuggingFaceService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://huggingface.co/api/models?filter=gguf&pipeline_tag=text-generation&limit=20";

        private ModelManager modelManager;

        public HuggingFaceService(ModelManager modelManager)
        {
            this.modelManager = modelManager;
            _httpClient = new HttpClient();
            _httpClient.Timeout = Timeout.InfiniteTimeSpan;
        }

        public async Task<ResultDTO> GetModels()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(_baseUrl);
                if (!response.IsSuccessStatusCode)
                    return ResultDTO.Fail($"Error fetching models: {response.ReasonPhrase}");

                string content = await response.Content.ReadAsStringAsync();
                List<HuggingFaceModelDTO>? models = JsonSerializer.Deserialize<List<HuggingFaceModelDTO>>(content);
                if (models == null)
                    return ResultDTO.Fail("Failed to deserialize model list");

                string[] blockedLicenses = { "gemma" };

                List<HuggingFaceModelDTO> filteredModels =
                    models
                        .Where(m =>
                        {
                            // Normalize ID dashes
                            string normalizedId = m.id
                                .Replace("−", "-")
                                .Replace("–", "-")
                                .Replace("—", "-");

                            if (!normalizedId.Contains("b-", StringComparison.OrdinalIgnoreCase))
                                return false;

                            // Extract license tag
                            string? licenseTag = m.tags?
                                .FirstOrDefault(t => t.StartsWith("license:", StringComparison.OrdinalIgnoreCase));

                            if (licenseTag == null)
                                return true; // No license => allow

                            string license = licenseTag["license:".Length..].Trim();

                            // Check blocked licenses
                            return !blockedLicenses.Any(b =>
                                license.Contains(b, StringComparison.OrdinalIgnoreCase));
                        })
                        .OrderBy(m => m.id)
                        .ToList();


                if (filteredModels == null || filteredModels.Count <= 0)
                    return ResultDTO.Fail("No models found after filtering");


                List<HuggingFaceModelDTO> finalmodels = new List<HuggingFaceModelDTO>();

                foreach (var m in filteredModels)
                {
                    m._modelInfoDto = await GetModelInfo(m.id);
                    if (m._modelInfoDto != null)
                    {
                        finalmodels.Add(m);
                    }
                }

                if (finalmodels.Count <= 0)
                    return ResultDTO.Fail("no model was loaded");
                return ResultDTO.Success(finalmodels);
            }
            catch (Exception ex)
            {
                return ResultDTO.Fail($"Exception fetching models: {ex.Message}");
            }
        }

        public async Task<ModelInfoDTO?> GetModelInfo(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            try
            {
                string url = $"https://huggingface.co/api/models/{id}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                var content = await response.Content.ReadAsStringAsync();
                var model = JsonSerializer.Deserialize<ModelInfoDTO>(content);
                if (model == null || model.siblings == null) return null;

                model.siblings = model.siblings
                                .Where(s => s.rfilename.Contains(".gguf", StringComparison.OrdinalIgnoreCase))
                                .ToList();

                return model?.gguf != null ? model : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<long> GetFileSize(string id, string rFilename)
        {
            string fileUrl = $"https://huggingface.co/{id}/resolve/main/{rFilename}";

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, fileUrl);
                using var response = await _httpClient.SendAsync(request);

                response.EnsureSuccessStatusCode();

                if (response.Content.Headers.ContentLength.HasValue)
                {
                    long sizeBytes = response.Content.Headers.ContentLength.Value;
                    return sizeBytes; // returns size in bytes
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 0;
            }
        }

        public async Task<ResultDTO> DownloadModel(HuggingFaceModelDTO model, string rfilename, long size, Action<long, long, double> OnDownloadProgress, CancellationToken cancellationToken = default)
        {
            if (model == null)
                return ResultDTO.Fail("Model is null");

            if (modelManager.CheckFileExists(model, rfilename))
            {
                return ResultDTO.Success($"File {rfilename} already exists and is up-to-date.");
            }

            string? path = modelManager.CreateModelPath(model, rfilename);
            if (path == null)
                return ResultDTO.Fail("Failed to create model path");

            string url = $"https://huggingface.co/{model.id}/resolve/main/{rfilename}";

            try
            {
                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                byte[] buffer = new byte[81920];
                long fileDownloaded = 0;
                int read;
                using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

                DateTime startTime = DateTime.Now;

                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return ResultDTO.Fail("Download cancelled by user");
                    }
                    await modelManager.SaveModelFileAsync(model, rfilename, fs, buffer, read, size, startTime, cancellationToken, OnDownloadProgress);
                }

                Debug.WriteLine($"Completed {rfilename} ({fileDownloaded / 1024.0 / 1024.0:F2} MB)");

                return ResultDTO.Success($"{rfilename}");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"Download canceled for {rfilename}");
                return ResultDTO.Fail("Download canceled");
            }
            catch (Exception ex)
            {
                modelManager.DeleteModel(model, rfilename);

                Debug.WriteLine($"Error downloading {rfilename}: {ex.Message}");
                return ResultDTO.Fail($"Error downloading {rfilename}: {ex.Message}");
            }
        }
    }
}