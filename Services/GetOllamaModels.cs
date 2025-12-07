using Ai_Project.DTO;
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
        private readonly string _baseUrl = "https://huggingface.co/api/models?filter=gguf&full=true";

        public HuggingFaceService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = Timeout.InfiniteTimeSpan;
        }

        //public ResultDTO GetModels()
        //{
        //    var response = _httpClient.GetAsync(_baseUrl).Result;
        //    if (response.IsSuccessStatusCode)
        //    {
        //        var content = response.Content.ReadAsStringAsync().Result;
        //        var models = JsonSerializer.Deserialize<List<ModelDTO>>(content);
        //        models = models.OrderBy(t => t.modelId).ToList();

        //        List<ModelDTO> newList = new List<ModelDTO>();
        //        foreach (var model in models)
        //        {
        //            var actmodel = getModelInfo(model.modelId);
        //            if (actmodel != null)
        //            {
        //                newList.Add(model);
        //            }
        //            Thread.Sleep(1); // 1 second pause between API calls

        //        }

        //        newList = newList.OrderBy(t => t.modelId).ToList();
        //        return ResultDTO.Success(newList);
        //    }
        //    else
        //    {
        //        return ResultDTO.Fail($"Error fetching models: {response.ReasonPhrase}");
        //    }
        //}

        public async Task<ResultDTO> GetModelsAsync()
        {
            HttpResponseMessage response = await _httpClient.GetAsync(_baseUrl);
            if (!response.IsSuccessStatusCode)
                return ResultDTO.Fail($"Error fetching models: {response.ReasonPhrase}");

            string content = await response.Content.ReadAsStringAsync();
            List<HuggingFaceModelDTO>? models = JsonSerializer.Deserialize<List<HuggingFaceModelDTO>>(content);
            if (models == null)
                return ResultDTO.Fail("Failed to deserialize model list");

            List<HuggingFaceModelDTO> filterdmodels = models.Where(m => m.tags != null && m.tags
            .Any(t => t.Equals("license:apache-2.0", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(m => m.modelId).ToList();
            // Allowed file extensions
            //string[] allowedExtensions = { ".gguf" };
            //// Exclude .safetensors and .gguf unless you also have the model code to load them

            //// Filter models: license is apache or null
            //var filteredModels = new List<ModelInfoDTO>();
            //foreach (var model in filterdmodels)
            //{
            //    //bool licenseOk = model.license == null ||
            //    //                 model.license.Equals("apache-2.0", StringComparison.OrdinalIgnoreCase);

            //    //if (!licenseOk)
            //    //    continue;

            //    // Fetch detailed model info to check siblings
            //    var detailResponse = await _httpClient.GetAsync($"https://huggingface.co/api/models/{model.modelId}");
            //    if (!detailResponse.IsSuccessStatusCode)
            //        continue;

            //    var detailContent = await detailResponse.Content.ReadAsStringAsync();
            //    var detail = JsonSerializer.Deserialize<ModelInfoDTO>(detailContent);
            //    if (detail?.siblings == null || detail.siblings.Count == 0)
            //        continue;

            //    // Check if any sibling matches allowed extensions
            //    if (detail.siblings.Any(f => allowedExtensions.Any(ext => f.rfilename.EndsWith(ext, StringComparison.OrdinalIgnoreCase))))
            //    {
            //        filteredModels.Add(detail);
            //    }
            //}

            //filteredModels = filteredModels.OrderBy(m => m.id).ToList();
            return ResultDTO.Success(filterdmodels);
        }

        public ModelInfoDTO? getModelInfo(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            try
            {
                // 1. Fetch model metadata
                string url = $"https://huggingface.co/api/models/{id}";
                var response = _httpClient.GetAsync(url).Result;
                if (!response.IsSuccessStatusCode)
                    return null;

                var content = response.Content.ReadAsStringAsync().Result;
                var model = JsonSerializer.Deserialize<ModelInfoDTO>(content);

                if (model == null)
                    return null;

                // 2. Fetch safetensors index for total_size
                //string urlSize = $"https://huggingface.co/api/resolve-cache/models/{model.id}/{model.sha}/model.safetensors.index.json";
                //var responseSize = _httpClient.GetAsync(urlSize).Result;
                //if (!responseSize.IsSuccessStatusCode)
                //    return null;

                //var contentSize = responseSize.Content.ReadAsStringAsync().Result;
                //var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                //var metadata = JsonSerializer.Deserialize<MetadataDTO>(contentSize, options);

                //if (metadata == null || metadata.metadata == null)
                //    return null;

                //model.usedStorage = metadata.metadata.total_size;
                return model;
            }
            catch
            {
                return null;
            }
        }

        public class SafetensorsIndexDTO
        {
            public List<ChunkDTO> Chunks { get; set; } = new();
        }

        public class ChunkDTO
        {
            public string Name { get; set; } = string.Empty;
            public long Size { get; set; } // size in bytes
        }

        public class MetadataDTO
        {
            public MetadataInfo metadata { get; set; } = new();
        }

        public class MetadataInfo
        {
            public long total_parameters { get; set; }
            public long total_size { get; set; }
        }

        public async Task<ResultDTO> GetModelInfo(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return ResultDTO.Fail($"Model id cannot be null or empty ({nameof(id)})");

            try
            {
                // 1. Fetch model metadata
                string url = $"https://huggingface.co/api/models/{id}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return ResultDTO.Fail($"Error fetching model info. Status code: {response.StatusCode}");

                var content = await response.Content.ReadAsStringAsync();
                var model = JsonSerializer.Deserialize<ModelInfoDTO>(content);

                // 2. Fetch the safetensors index to get total_size
                //string urlSize = $"https://huggingface.co/api/resolve-cache/models/{model.id}/{model.sha}/model.safetensors.index.json";
                //var responseSize = await _httpClient.GetAsync(urlSize);
                //if (!responseSize.IsSuccessStatusCode)
                //    return ResultDTO.Fail($"Error fetching model size. Status code: {responseSize.StatusCode}");

                //var contentSize = await responseSize.Content.ReadAsStringAsync();
                //var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                //var metadata = JsonSerializer.Deserialize<MetadataDTO>(contentSize, options);

                //if (metadata == null || metadata.metadata == null)
                //    return ResultDTO.Fail("Failed to deserialize metadata");

                //model.usedStorage = model.usedStorage;

                return ResultDTO.Success(model);
            }
            catch (Exception ex)
            {
                return ResultDTO.Fail($"Exception fetching model info: {ex.Message}");
            }
        }


        public async Task<ResultDTO> DownloadModelFilesAsync(string modelId, string folderPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                return ResultDTO.Fail("Model ID cannot be null or empty");

            folderPath = Path.Combine("D:\\Ai_Project\\bin\\Debug\\net8.0-windows\\Downloads", modelId);
            Directory.CreateDirectory(folderPath);

            var modelInfoResult = await GetModelInfo(modelId);
            if (!modelInfoResult.IsSuccess || modelInfoResult.Data is not ModelInfoDTO modelInfo)
                return ResultDTO.Fail("Failed to get model info");

            if (modelInfo.siblings == null || modelInfo.siblings.Count == 0)
                return ResultDTO.Fail("No files found in model");

            int count = 0;
            long totalModelBytes = modelInfo.siblings.Sum(f => f.size ?? 0);
            long totalDownloadedBytes = 0;

            foreach (var file in modelInfo.siblings)
            {
                string url = $"https://huggingface.co/{modelId}/resolve/main/{file.rfilename}";
                string path = Path.Combine(folderPath, file.rfilename);

                // Skip if already fully downloaded
                if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    if (fileInfo.Length == (file.size ?? 0))
                    {
                        Debug.WriteLine($"{file.rfilename} already downloaded. Skipping...");
                        totalDownloadedBytes += fileInfo.Length;
                        continue;
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                try
                {
                    using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    long totalBytes = response.Content.Headers.ContentLength ?? -1;

                    using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

                    byte[] buffer = new byte[81920];
                    long fileDownloaded = 0;
                    int read;

                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await fs.WriteAsync(buffer, 0, read, cancellationToken);
                        fileDownloaded += read;
                        totalDownloadedBytes += read;

                        double fileMb = fileDownloaded / 1024.0 / 1024.0;
                        double totalMb = totalDownloadedBytes / 1024.0 / 1024.0;

                        Debug.WriteLine($"{file.rfilename}: {fileMb:F2} MB downloaded (total: {totalMb:F2} MB)");
                    }

                    count++;
                    Debug.WriteLine($"Completed {file.rfilename} ({fileDownloaded / 1024.0 / 1024.0:F2} MB)");
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine($"Download canceled for {file.rfilename}");
                    return ResultDTO.Fail("Download canceled");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error downloading {file.rfilename}: {ex.Message}");
                    // Continue to next file
                }
            }

            return ResultDTO.Success($"Downloaded {count} files to {folderPath}");
        }
    }
}