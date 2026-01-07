using Ai_Project.DTO;
using Ai_Project.Model_Manager;
using ModelsDTO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Ai_Project.Content.Controls.Demo
{
    public class DownloadModel : IDisposable
    {
        Grid grid;
        ProgressBar progressBar;
        Border Stopborder;
        Border _downloadBorder;
        ModelManager modelManager;
        public SiblingDTO? sibling;
        public TextBlock? DownloadingProgressTextBlock;
        public HuggingFaceModelDTO? model;
        public static bool IsDownloading = false;

        public DownloadModel(Grid grid, ProgressBar progressBar, Border _downloadBorder, Border Stopborder, ModelManager modelManager)
        {
            IsDownloading = true;
            this.modelManager = modelManager;
            this.grid = grid;
            this.progressBar = progressBar;
            this.Stopborder = Stopborder;
            this._downloadBorder = _downloadBorder;
            _downloadBorder.Visibility = Visibility.Collapsed;
            Stopborder.Visibility = Visibility.Visible;
            ShowGrid();
        }

        // Event handler for download progress updates
        void DownloadProgressChanged(long progress, long speedDownloading, double timeestimate)
        {
            if (sibling == null || DownloadingProgressTextBlock == null)
                return;

            double progressInGB = progress / 1024d / 1024d / 1024d;
            double speedInMB = speedDownloading / 1024d / 1024d;

            string speedText = speedInMB >= 1
                ? $"{speedInMB:F2} MB/s"
                : $"{speedInMB * 1024:F0} KB/s";

            DownloadingProgressTextBlock.Dispatcher.Invoke(() => DownloadingProgressTextBlock.Text =
                $"{speedText} - {progressInGB:F2} GB of {sibling._sizeGB} GB, {FormatTimeEstimate(timeestimate)}");

            progressBar.Dispatcher.Invoke(() => progressBar.Value =
                (progress / (sibling._sizeGB * 1024d * 1024d * 1024d)) * 100);
        }

        private string FormatTimeEstimate(double timeestimate)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(timeestimate);

            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays} days left";

            if (timeSpan.TotalHours > 1)
                return $"{(int)timeSpan.TotalHours} hours left";

            if (timeSpan.TotalHours == 1)
                return $"{(int)timeSpan.TotalHours} hour left";

            if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.Minutes} minutes left";

            return $"{timeSpan.Seconds} seconds left";
        }

        public async Task<ResultDTO> DownloadModelAsync()
        {
            if (sibling == null || sibling.size == null || model == null || model._modelInfoDto == null)
                return ResultDTO.Fail("There is missing info of this model");

            // Subscribe to progress events
            modelManager.OnDownloadProgress += DownloadProgressChanged;

            try
            {
                ResultDTO result = await modelManager.DownloadModelAsync(model, sibling.rfilename, (long)sibling.size);
                if (result.IsSuccess && result.Data.ToString().Equals("Download canceled by user"))
                {
                    _downloadBorder.Dispatcher.Invoke(() => _downloadBorder.Visibility = Visibility.Visible);
                }
                else
                if (!result.IsSuccess)
                {
                    if (MessageBox.Show("An error occurred during downloading.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error) == MessageBoxResult.OK)
                    {
                         _downloadBorder.Dispatcher.Invoke(() => _downloadBorder.Visibility = Visibility.Visible);
                    }
                }

                return result;
            }

            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return ResultDTO.Fail($"Exception during download: {ex.Message}");
            }
        }

        private void ShowGrid()
        {
            if (grid == null) return;
            double to = 36;
            DoubleAnimation animation = new DoubleAnimation
            {
                To = to,
                Duration = TimeSpan.FromMilliseconds(100),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            grid.Dispatcher.BeginInvoke(new Action(() =>
            {
                grid.Visibility = System.Windows.Visibility.Visible;

                grid.BeginAnimation(FrameworkElement.HeightProperty, animation);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void HideGrid()
        {
            if (grid == null) return;
            double to = 0;
            DoubleAnimation animation = new DoubleAnimation
            {
                To = to,
                Duration = TimeSpan.FromMilliseconds(100),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            grid.Dispatcher.BeginInvoke(new Action(() =>
            {
                animation.Completed += ((s, e) =>
                {
                    grid.Visibility = System.Windows.Visibility.Collapsed;
                });
                grid.BeginAnimation(FrameworkElement.HeightProperty, animation);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        public void Dispose()
        {
            HideGrid();
            IsDownloading = false;
            modelManager.OnDownloadProgress -= DownloadProgressChanged;

            if (DownloadingProgressTextBlock != null) DownloadingProgressTextBlock.Text = "Downloading";

            sibling = null;

            model = null;

            DownloadingProgressTextBlock = null;

            Stopborder.Dispatcher.Invoke(() => Stopborder.Visibility = Visibility.Collapsed);

            progressBar.Dispatcher.Invoke(() =>
            {
                progressBar.Value = 0;
            });
        }
    }
}
