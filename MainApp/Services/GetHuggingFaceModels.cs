using Ai_Project.DTO;
using ModelsDTO;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace Ai_Project.Services
{
    public class HuggingFaceService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://huggingface.co/api/models?filter=gguf&pipeline_tag=text-generation&limit=20";

        public HuggingFaceService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = Timeout.InfiniteTimeSpan;
        }

        public async Task<ResultDTO> GetModelsAsync()
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
                    m._modelInfoDto = await GetModelInfoAsync(m.id);
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

        public async Task<ModelInfoDTO?> GetModelInfoAsync(string id)
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

        //public async Task<ResultDTO> DownloadModelFilesAsync(string modelId, string folderPath, CancellationToken cancellationToken = default)
        //{
        //    if (string.IsNullOrWhiteSpace(modelId))
        //        return ResultDTO.Fail("Model ID cannot be null or empty");

        //    folderPath = Path.Combine("D:\\Ai_Project\\bin\\Debug\\net8.0-windows\\Downloads", modelId);
        //    Directory.CreateDirectory(folderPath);

        //    var modelInfoResult = await GetModelInfo(modelId);
        //    if (!modelInfoResult.IsSuccess || modelInfoResult.Data is not ModelInfoDTO modelInfo)
        //        return ResultDTO.Fail("Failed to get model info");

        //    if (modelInfo.siblings == null || modelInfo.siblings.Count == 0)
        //        return ResultDTO.Fail("No files found in model");

        //    int count = 0;
        //    long totalModelBytes = modelInfo.siblings.Sum(f => f.size ?? 0);
        //    long totalDownloadedBytes = 0;

        //    foreach (var file in modelInfo.siblings)
        //    {
        //        string url = $"https://huggingface.co/{modelId}/resolve/main/{file.rfilename}";
        //        string path = Path.Combine(folderPath, file.rfilename);

        //        // Skip if already fully downloaded
        //        if (File.Exists(path))
        //        {
        //            var fileInfo = new FileInfo(path);
        //            if (fileInfo.Length == (file.size ?? 0))
        //            {
        //                Debug.WriteLine($"{file.rfilename} already downloaded. Skipping...");
        //                totalDownloadedBytes += fileInfo.Length;
        //                continue;
        //            }
        //        }

        //        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        //        try
        //        {
        //            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        //            response.EnsureSuccessStatusCode();

        //            long totalBytes = response.Content.Headers.ContentLength ?? -1;

        //            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        //            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

        //            byte[] buffer = new byte[81920];
        //            long fileDownloaded = 0;
        //            int read;

        //            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        //            {
        //                await fs.WriteAsync(buffer, 0, read, cancellationToken);
        //                fileDownloaded += read;
        //                totalDownloadedBytes += read;

        //                double fileMb = fileDownloaded / 1024.0 / 1024.0;
        //                double totalMb = totalDownloadedBytes / 1024.0 / 1024.0;

        //                Debug.WriteLine($"{file.rfilename}: {fileMb:F2} MB downloaded (total: {totalMb:F2} MB)");
        //            }

        //            count++;
        //            Debug.WriteLine($"Completed {file.rfilename} ({fileDownloaded / 1024.0 / 1024.0:F2} MB)");
        //        }
        //        catch (OperationCanceledException)
        //        {
        //            Debug.WriteLine($"Download canceled for {file.rfilename}");
        //            return ResultDTO.Fail("Download canceled");
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine($"Error downloading {file.rfilename}: {ex.Message}");
        //            // Continue to next file
        //        }
        //    }

        //    return ResultDTO.Success($"Downloaded {count} files to {folderPath}");
        //}
    }
}