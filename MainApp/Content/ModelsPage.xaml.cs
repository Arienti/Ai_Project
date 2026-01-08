using Ai_Project.Content.Controls;
using Ai_Project.DTO;
using Ai_Project.Model_Manager;
using Ai_Project.Tools;
using Get_Pc_Info;
using ModelsDTO;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

namespace Ai_Project.Content
{
    /// <summary>
    /// Interaction logic for ModelsPage.xaml
    /// </summary>
    public partial class ModelsPage : Page, Utility.InitializablePage
    {
        List<HuggingFaceModelDTO>? models;
        ModelManager modelManager;
        public ModelsPage()
        {
            modelManager = new ModelManager();
            InitializeComponent();
        }
        public void Init()
        {
            // GetSize().GetAwaiter();
            GetPacInfo();
            LoadModels().GetAwaiter();
        }

        private void GetPacInfo()
        {
            // RAM
            RamTextBox.Text = $"Available ram: {GetPcInfo.AvailableRam / 1024 / 1024 / 1024} GB";

            // Disk
            DiskSpaceTextBox.Text = $"Disk available: {GetPcInfo.DiskFree / 1024 / 1024 / 1024} GB";

            // GPUs
            var gpu = GetPcInfo.Gpus.FirstOrDefault();
            if (gpu.Name != null)
            {
                VRamTextBox.Text = $"VRAM: {gpu.VRAM / 1024 / 1024} MB";
            }

            // All CPUs (multi CPU systems)
            var cpu = GetPcInfo.CpuList.FirstOrDefault();

            CpuTextBox.Text = $"CPU: {cpu.Name}  Threads: {cpu.PhysicalCores}";
        }

        private async Task GetSize()
        {
            // string size = await HuggingFaceService.GetFileSize();
        }

        private async Task LoadModels()
        {
            using (LoadAnimation loadAnimation = new LoadAnimation(LoadingProgressBar, this))
            {
                if (models != null && models.Count > 0) return;

                models = new List<HuggingFaceModelDTO>();
                // Implementation for loading models goes here
                ResultDTO result = await modelManager.GetModelsAsync();
                int authors = 0;
                if (result.IsSuccess && result.Data is List<HuggingFaceModelDTO> fetchedModels)
                {
                    models = fetchedModels;
                    // 1. Group models by author
                    var groupedByAuthor = fetchedModels
                                        .GroupBy(m => m._modelInfoDto!.author.Trim().ToLower())
                                        .Select(g => new
                                        {
                                            Author = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(g.Key),
                                            Models = g.ToList()
                                        })
                                        .ToList();

                    foreach (var authorGroup in groupedByAuthor)
                    {
                        authors++;
                        AuthorControl authorControl = new AuthorControl
                        {
                            DataContext = authorGroup.Author,
                        };
                        authorControl.PreviewMouseLeftButtonUp += AuthorControl_PreviewMouseLeftButtonUp;
                        AuthorsPanel.Children.Add(authorControl);
                    }
                }
                AuthorsCountTextBlock.Text = $"({authors})";
            }
        }

        private void AuthorControl_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            int count = 0;
            string author = (sender as AuthorControl)?.Tag as string ?? string.Empty;
            if (string.IsNullOrEmpty(author))
            {
                return;
            }
            ModelsPanel.Children.Clear();
            var authorModels = models?
                               .Where(m => m._modelInfoDto != null && m._modelInfoDto.author.Trim().Equals(author.Trim(), StringComparison.OrdinalIgnoreCase))
                               .ToList();
            if (authorModels == null || authorModels.Count == 0) return;

            foreach (var model in authorModels)
            {
                count++;
                ModelControl modelControl = new ModelControl
                {
                    DataContext = model,
                };
                modelControl.PreviewMouseLeftButtonUp += ModelControl_PreviewMouseLeftButtonUp;
                ModelsPanel.Children.Add(modelControl);
            }
            ModelsCountTextBlock.Text = $"({count})";
        }

        private async void ModelControl_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ModelsDetailsPanel.Children.Clear();
            ModelControl? modelControl = sender as ModelControl;
            if (modelControl == null) return;
            HuggingFaceModelDTO? model = modelControl.Tag as HuggingFaceModelDTO;
            if (model == null || model._modelInfoDto == null || model._modelInfoDto.siblings == null)
                return;
            ModelsDetailsScroll.Visibility = System.Windows.Visibility.Collapsed;
            PanelModelNameTextBlock.Text = modelControl.ModelNameTextBlock.Text;

            foreach (var sibling in model._modelInfoDto.siblings)
            {
                sibling.size = await modelManager.GetFileSize(model._modelInfoDto.id, sibling.rfilename);
                if (!sibling.rfilename.Contains("matrix"))
                {
                    FileControl fileControl = new FileControl(model, modelManager, LogsTextBox, LogsScrollViewer)
                    {
                        DataContext = sibling,
                        Tag = model
                    };

                    ModelsDetailsPanel.Children.Add(fileControl);
                }
            }
            ModelsDetailsScroll.Visibility = System.Windows.Visibility.Visible;
        }

        private void ClearLogsTextBox_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LogsTextBox.Document.Blocks.Clear();
        }

        private void LogsTextBox_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            double delta = LogsTextBox.ViewportHeight * 0.8;
            switch (e.Key)
            {
                case System.Windows.Input.Key.Up:
                case System.Windows.Input.Key.Down:
                case System.Windows.Input.Key.Left:
                case System.Windows.Input.Key.Right:
                case System.Windows.Input.Key.Home:
                case System.Windows.Input.Key.End:
                    return; // allow scrolling/navigation
            }

            e.Handled = true; // block everything else
        }

        private void LogsTextBox_PreviewKey(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void Scroll_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer)
                return;

            // Get the vertical scrollbar
            if (scrollViewer.Template.FindName("PART_VerticalScrollBar", scrollViewer) is ScrollBar verticalScrollBar)
            {
                verticalScrollBar.ApplyTemplate(); // ensure template is applied

                // Get the border inside the scrollbar template
                if (verticalScrollBar.Template.FindName("ScrollBarBorder", verticalScrollBar) is Border border)
                {
                    double toValue = 10;
                    var animation = new DoubleAnimation(toValue, TimeSpan.FromMilliseconds(100));
                    border.BeginAnimation(Border.WidthProperty, animation);
                }
            }
        }

        private void Scroll_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer)
                return;

            // Get the vertical scrollbar
            if (scrollViewer.Template.FindName("PART_VerticalScrollBar", scrollViewer) is ScrollBar verticalScrollBar)
            {
                verticalScrollBar.ApplyTemplate(); // ensure template is applied

                // Get the border inside the scrollbar template
                if (verticalScrollBar.Template.FindName("ScrollBarBorder", verticalScrollBar) is Border border)
                {
                    double toValue = 0;
                    var animation = new DoubleAnimation(toValue, TimeSpan.FromMilliseconds(100));
                    border.BeginAnimation(Border.WidthProperty, animation);
                }
            }
        }


        //private async void ModelsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (ModelsListBox.SelectedItem == null)
        //        return;

        //    // If your items are strings (like "maya-research/maya1")
        //    HuggingFaceModelDTO? m = ModelsListBox.SelectedItem as HuggingFaceModelDTO;
        //    if (m == null)
        //        return;
        //    // OR if your items are objects:
        //    // var model = ModelsListBox.SelectedItem as ModelDTO;
        //    // string modelId = model?.Id;

        //    try
        //    {
        //        ResultDTO result = await HuggingFaceService.GetModelInfo(m.modelId);
        //        if (result.IsSuccess)
        //        {
        //            ModelInfoDTO? model = result.Data as ModelInfoDTO;
        //            if (model == null)
        //                return;

        //            ModelInfoTextBlock.Text = $"Created: {model.createdAt.ToString("G")}";
        //            ModelInfoSizeTextBlock.Text = $"Size: {model.gguf.model_size.ToString()}Gb";
        //        }
        //        else
        //        {
        //            MessageBox.Show($"Failed to get model info: {result.ErrorMessages}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Error fetching model info: {ex.Message}");
        //    }
        //}

        //private async void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    HuggingFaceModelDTO? m = ModelsListBox.SelectedItem as HuggingFaceModelDTO;
        //    if (m == null)
        //        return;
        //    ResultDTO result = await HuggingFaceService.GetModelInfo(m.modelId);
        //    if (result.IsSuccess)
        //    {
        //        ModelInfoDTO? model = result.Data as ModelInfoDTO;
        //        if (model == null)
        //            return;
        //        // Application root
        //        string appRoot = AppContext.BaseDirectory;

        //        // Create "Downloads" folder under root
        //        string downloadsFolder = Path.Combine(appRoot, "Downloads");

        //        await HuggingFaceService.DownloadModelFilesAsync(model.id, appRoot);
        //    }
        //    else
        //    {
        //        MessageBox.Show($"Failed to get model info: {result.ErrorMessages}");
        //    }
        //}
    }
}
