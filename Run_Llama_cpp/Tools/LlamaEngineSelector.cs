using Ai_Project.LlamaCpp.Utility;
using Get_Pc_Info;
using System.Diagnostics;

namespace Run_Llama_cpp.Tools
{
    public class LlamaEngineSelector
    {
        public static string _selectedEngine = string.Empty;

        private readonly static string engineDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Llama_Engine");

        private const string _radeonLlamaCppDirectory = @"Radeon_Llama_cpp";

        private const string _nvidiaLlamaCppDirectory = @"Nvidia_Llama_cpp";

        private const string _radeonLlamaCpp = $@"{_radeonLlamaCppDirectory}\llama-cli.exe";

        private const string _nvidiaLlamaCpp = $@"{_nvidiaLlamaCppDirectory}\llama-cli.exe";

        private const string _radeonLlamaArchive = @"Radeon_Llama_cpp.zip";

        private const string _nvidiaLlamaCppArchive = @"Nvidia_Llama_cpp.zip";

        public async Task<string> SelectEngine()
        {
            bool _RadeonGpu = GetPcInfo.Gpus.Any(g => g.Name.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ||
                                                      g.Name.Contains("ATI", StringComparison.OrdinalIgnoreCase));


            if (_RadeonGpu)
            {
                _selectedEngine = Path.Combine(engineDirectory, _radeonLlamaCpp);
            }
            else
            {
                _selectedEngine = Path.Combine(engineDirectory, _nvidiaLlamaCpp);
            }
            if (!File.Exists(_selectedEngine))
            {
                await EnsureLlamaEngineArchiveExists(_RadeonGpu);
            }

            return _selectedEngine;
        }

        public async Task EnsureLlamaEngineArchiveExists(bool radeonGpu)
        {
            string archivePath = radeonGpu
                ? Path.Combine(engineDirectory, _radeonLlamaArchive)
                : Path.Combine(engineDirectory, _nvidiaLlamaCppArchive);

            int retries = 2;

            while (retries-- > 0)
            {
                // If file does not exist → download
                if (!File.Exists(archivePath))
                {
                    var downloader = new GetLlamaCppEngine(engineDirectory);
                    var result = await downloader.DownloadLlamaEngineAsync(radeonGpu);

                    if (!result.IsSuccess || result.Data == null)
                        throw new Exception("Failed to download Llama engine");

                    archivePath = result.Data as string
                        ?? throw new Exception("Downloaded llama engine path missing");
                }

                string targetFolder = radeonGpu
                    ? Path.Combine(engineDirectory, _radeonLlamaCppDirectory)
                    : Path.Combine(engineDirectory, _nvidiaLlamaCppDirectory);

                // Try unzip
                if (UnzipLlamaEngineArchive(archivePath, targetFolder))
                    return; // Success
            }

            throw new Exception("Failed to extract Llama engine after retries");
        }


        private bool UnzipLlamaEngineArchive(string archivePath, string targetFolder)
        {
            // Remove existing extracted folder
            if (Directory.Exists(targetFolder))
                Directory.Delete(targetFolder, true);

            Directory.CreateDirectory(targetFolder);

            try
            {
                // Extract directly to the target folder
                System.IO.Compression.ZipFile.ExtractToDirectory(archivePath, targetFolder);
                return true; // success
            }
            catch (Exception ex)
            {
                // ZIP is corrupted or incomplete → remove it
                try
                {
                    if (File.Exists(archivePath))
                        File.Delete(archivePath);
                }
                catch { }

                // Remove partially extracted folder
                try
                {
                    if (Directory.Exists(targetFolder))
                        Directory.Delete(targetFolder, true);
                }
                catch { }

                Debug.WriteLine("ZIP extraction failed: " + ex.Message);
                return false; // indicate failure
            }
        }
    }
}
