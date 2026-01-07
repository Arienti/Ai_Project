using Ai_Project.DTO;
using Ai_Project.Services;
using ModelsDTO;
using Run_LlamaSharp;
using Run_LlamaSharp.DTOs;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Ai_Project.Model_Manager
{
    public class ModelManager
    {
        public List<HuggingFaceModelDTO> AvailableModels { get; set; }
        protected string? AssemblyPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private const string ModelsFolder = "Models";

        long totalSize = 0;
        long fileDownloaded = 0;
        DateTime startTime;
        DateTime lastUpdateTime;
        DateTime lastDataReceived;
        private bool isDownloading = false;

        private HuggingFaceService huggingfaceService;

        public Action<long, long, double>? OnDownloadProgress;
        public Action<bool>? DownloadCancelled;

        public FileStream _fileStream { get; set; }

        public ModelManager()
        {
            AvailableModels = new List<HuggingFaceModelDTO>();
            huggingfaceService = new HuggingFaceService(this);
        }

        public async Task RunModel(ModelDTO model)
        {
            SelectLlama selectLlama = new SelectLlama();
            selectLlama.UnLoadModel();
            await selectLlama.InitializeAsync(model);
        }

        public List<HuggingFaceModelDTO>? GetModels()
        {
            return AvailableModels;
        }

        public async Task<ResultDTO> GetModelsAsync()
        {
            try
            {
                ResultDTO result = await huggingfaceService.GetModels();
                if (result.IsSuccess && result.Data is List<HuggingFaceModelDTO> models)
                {
                    foreach (var model in models)
                    {
                        AvailableModels.Add(model);
                    }
                    return ResultDTO.Success(AvailableModels);
                }
                return ResultDTO.Fail("Failed to fetch models");
            }
            catch (Exception ex)
            {
                return ResultDTO.Fail($"Exception fetching models: {ex.Message}");
            }
        }

        public async Task<long> GetFileSize(string id, string rFilename)
        {
            return await huggingfaceService.GetFileSize(id, rFilename);
        }

        public void StartDownloading(long size)
        {
            totalSize = size;
            fileDownloaded = 0;
            startTime = DateTime.UtcNow;
            lastUpdateTime = startTime;
            lastDataReceived = startTime;
            isDownloading = true;
        }

        public bool CheckFileExists(HuggingFaceModelDTO model, string rfilename)
        {
            if (AssemblyPath == null || model == null)
                return false;

            string path = Path.Combine(AssemblyPath, ModelsFolder, model.id, rfilename);

            try
            {
                if (File.Exists(path))
                {
                    FileInfo fileData = new FileInfo(path);

                    var sizeInBytes = model._modelInfoDto?.siblings?.FirstOrDefault(s => s.rfilename.Equals(rfilename))?.size ?? 0;

                    return fileData.Length == sizeInBytes; // Check if file size matches
                }
            }
            catch (IOException ex)
            {
                // Log error or handle exception
                Debug.WriteLine($"Error checking file: {ex.Message}");
            }

            return false;
        }

        public string? CreateModelPath(HuggingFaceModelDTO model, string rfilename)
        {
            if (AssemblyPath == null || model == null)
                return null;

            string folderPath = Path.Combine(AssemblyPath, ModelsFolder, model.id);

            try
            {
                EnsureDirectoryExists(folderPath);
                string filePath = Path.Combine(folderPath, rfilename);

                return filePath;
            }
            catch (IOException ex)
            {
                // Log error or handle exception
                Debug.WriteLine($"Error creating model path: {ex.Message}");
                return null;
            }
        }

        public string? GetModelPath(HuggingFaceModelDTO model, string rfilename)
        {
            if (AssemblyPath == null || model == null)
                return null;
            string path = Path.Combine(AssemblyPath, ModelsFolder, model.id, rfilename);
            path = path.Replace("/", "\\");
            return path;
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void DeleteModel(HuggingFaceModelDTO model, string rfilename)
        {
            try
            {
                if (_fileStream != null)
                {
                    _fileStream.Dispose();
                }

                string modelPath = Path.Combine(AssemblyPath!, ModelsFolder, model.id, rfilename);

                if (File.Exists(modelPath))
                {
                    File.Delete(modelPath); // true to delete recursively
                }
                string? directoryPath = Path.GetDirectoryName(modelPath);

                if (directoryPath != null && Directory.Exists(directoryPath) && !Directory.EnumerateFileSystemEntries(directoryPath).Any())
                {
                    Directory.Delete(directoryPath); // Delete the directory if empty

                    string? lastParentDirectory = Directory.GetParent(directoryPath)?.FullName;

                    if (lastParentDirectory != null && Directory.Exists(lastParentDirectory) && !Directory.EnumerateFileSystemEntries(lastParentDirectory).Any())
                    {
                        Directory.Delete(lastParentDirectory);  // Delete the last parent directory if it is empty
                    }
                }

            }
            catch (IOException ex)
            {
                // Log error or handle exception
                Debug.WriteLine($"Error deleting model path: {ex.Message}");
            }
        }

        public async Task<ResultDTO> DownloadModelAsync(HuggingFaceModelDTO model, string rfilename, long size, CancellationToken cancellationToken = default)
        {
            StartDownloading(size);

            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            DownloadCancelled += (isCancelled) =>
            {
                if (isCancelled)
                {
                    cancellationTokenSource.Cancel();
                    DeleteModel(model, rfilename);
                }
            };

            try
            {
                ResultDTO result = await huggingfaceService.DownloadModel(model, rfilename, size, cancellationTokenSource.Token);
                string data = string.Empty;

                if (result.Data != null)
                {
                    data = result.Data.ToString() ?? string.Empty;

                    bool isDownloadFailed = !result.IsSuccess ||
                            data.Equals("Download canceled by user");
                    if (isDownloadFailed)
                    {
                        DeleteModel(model, rfilename);
                    }
                }
                return result;
            }

            catch (Exception ex)
            {
                Debug.WriteLine($"Error downloading {rfilename}: {ex.Message}");
                return ResultDTO.Fail($"Error downloading {rfilename}: {ex.Message}");
            }
        }


        public async Task SaveModelFileAsync(byte[] buffer, int read, CancellationToken cancellationToken)
        {
            await _fileStream.WriteAsync(buffer, 0, read, cancellationToken);

            Interlocked.Add(ref fileDownloaded, read);
            lastDataReceived = DateTime.UtcNow;
        }

        public void StopDownloading()
        {
            isDownloading = false;
        }

        public async Task InitializeDownloadInfo(CancellationToken cancellationToken)
        {
            while (isDownloading && !cancellationToken.IsCancellationRequested)
            {
                UpdateProgress();
                await Task.Delay(1000, cancellationToken);
            }
        }

        private void UpdateProgress()
        {
            var now = DateTime.UtcNow;

            if ((now - lastUpdateTime).TotalSeconds < 1)
                return;

            long downloaded = Interlocked.Read(ref fileDownloaded);
            double elapsedSeconds = (now - startTime).TotalSeconds;

            if (elapsedSeconds <= 0)
                return;

            double speed = downloaded / elapsedSeconds;
            double remaining =
                speed > 0 ? (totalSize - downloaded) / speed : double.PositiveInfinity;

            OnDownloadProgress?.Invoke(
                downloaded,
                (long)speed,
                remaining);

            lastUpdateTime = now;
        }
    }
}