using ModelsDTO;

namespace Ai_Project.LlamaCpp.Utility
{
    public class GetLlamaCppEngine
    {
        private string engineDirectory = string.Empty;

        public GetLlamaCppEngine(string engineDirectory)
        {
            this.engineDirectory = engineDirectory;
        }

        public async Task<ResultDTO> DownloadLlamaEngineAsync(bool isRadeon, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(engineDirectory);

            string RadeonUrl = "https://github.com/ggml-org/llama.cpp/releases/download/b7157/llama-b7157-bin-win-hip-radeon-x64.zip";
            string NvidiaUrl = "https://github.com/ggml-org/llama.cpp/releases/download/b7157/llama-b7157-bin-win-cuda-12.4-x64.zip";

            string url = isRadeon
                ? RadeonUrl
                : NvidiaUrl;

            string finalFileName = isRadeon ? "Radeon_Llama_cpp.zip" : "Nvidia_Llama_cpp.zip";

            string finalPath = Path.Combine(engineDirectory, finalFileName);

            using HttpClient client = new();

            try
            {
                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                // ---- Determine filename from server or fallback ----
                string downloadedName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                                        ?? "download.zip";

                string tempPath = Path.Combine(engineDirectory, downloadedName);

                // ---- Download to temp file ----
                using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
                using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                {
                    byte[] buffer = new byte[81920];
                    int read;

                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await fs.WriteAsync(buffer, 0, read, cancellationToken);
                    }
                }

                // ---- Rename (overwrite allowed) ----
                if (File.Exists(finalPath))
                    File.Delete(finalPath);

                File.Move(tempPath, finalPath);

                return ResultDTO.Success(finalPath);
            }
            catch (Exception ex)
            {
                return ResultDTO.Fail("Error downloading llama engine: " + ex.Message);
            }
        }

    }
}
