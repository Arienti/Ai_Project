using Ai_Project.DTO;
using Ai_Project.Services;
using ModelsDTO;
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

        private long fileDownloaded = 0;
        private DateTime? lastUpdateTime;
        private long totalDownloadedBytes = 0;

        private HuggingFaceService huggingfaceService;

        public Action<long, long, double>? OnDownloadProgress;
        public Action<bool>? DownloadCancelled;

        public ModelManager()
        {
            AvailableModels = new List<HuggingFaceModelDTO>();
            huggingfaceService = new HuggingFaceService(this);
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

        private void ResetdownloadInfo()
        {
            fileDownloaded = 0;
            totalDownloadedBytes = 0;
            lastUpdateTime = DateTime.Now;
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
            ResetdownloadInfo();
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
                return await huggingfaceService.DownloadModel(model, rfilename, size, OnDownloadProgress!, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"Download of {rfilename} was canceled.");
                return ResultDTO.Fail("Download canceled.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error downloading {rfilename}: {ex.Message}");
                return ResultDTO.Fail($"Error downloading {rfilename}: {ex.Message}");
            }
        }


        public async Task SaveModelFileAsync(HuggingFaceModelDTO modelDTO, string rfilename, FileStream fs, byte[] buffer, int read, long size, DateTime startDateTime, CancellationToken cancellationToken, Action<long, long, double>? OnDownloadProgress)
        {
            try
            {
                //if (isCancelled)
                //{
                //    await fs.DisposeAsync();  // Make sure it's fully closed
                //    DeleteModel(modelDTO, rfilename);
                //    cancellationToken.ThrowIfCancellationRequested();
                //    return;
                //}

                await fs.WriteAsync(buffer, 0, read, cancellationToken);

                InitializeDownloadInfo(OnDownloadProgress, read, size, startDateTime);

            }
            catch (Exception ex)
            {
                // Handle file writing error
                Debug.Write($"Error saving file {rfilename}: {ex.Message}");
            }
        }

        private void InitializeDownloadInfo(Action<long, long, double>? OnDownloadProgress, int read, long size, DateTime startTime)
        {
            // Increment the number of bytes downloaded
            fileDownloaded += read;
            totalDownloadedBytes += read;

            // Calculate elapsed time since start
            TimeSpan elapsedTime = DateTime.Now - startTime;
            double elapsedSeconds = elapsedTime.TotalSeconds;

            // Calculate download speed (bytes per second)
            long downloadSpeed = (long)Math.Round(fileDownloaded / elapsedSeconds, 2);

            // If at least 1 second has passed since the last update
            if ((DateTime.Now - lastUpdateTime)?.TotalSeconds >= 1)
            {
                // Estimate the remaining time
                double estimatedTimeRemaining = (size - fileDownloaded) / downloadSpeed;

                // Invoke progress update callback if provided
                OnDownloadProgress?.Invoke(totalDownloadedBytes, downloadSpeed, estimatedTimeRemaining);

                // Update lastUpdateTime
                lastUpdateTime = DateTime.Now;
            }
        }
    }
}