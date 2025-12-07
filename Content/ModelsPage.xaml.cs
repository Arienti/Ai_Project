using Ai_Project.DTO;
using Ai_Project.Services;
using ModelsDTO;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Ai_Project.Content
{
    /// <summary>
    /// Interaction logic for ModelsPage.xaml
    /// </summary>
    public partial class ModelsPage : Page, Utility.InitializablePage
    {
        List<HuggingFaceModelDTO> models;
        HuggingFaceService HuggingFaceService;
        public ModelsPage()
        {
            models = new List<HuggingFaceModelDTO>();
            HuggingFaceService = new HuggingFaceService();
            InitializeComponent();
        }
        public void Init()
        {
            LoadModels().GetAwaiter();
        }
        private async Task LoadModels()
        {
            // Implementation for loading models goes here
            ResultDTO result = await HuggingFaceService.GetModelsAsync();
            if (result.IsSuccess && result.Data is List<HuggingFaceModelDTO> fetchedModels)
            {
                models = fetchedModels;
                ModelsListBox.ItemsSource = models;
            }
            ModelsCountTextblock.Text = $"Models {models.Count}";
        }

        private async void ModelsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModelsListBox.SelectedItem == null)
                return;

            // If your items are strings (like "maya-research/maya1")
            HuggingFaceModelDTO? m = ModelsListBox.SelectedItem as HuggingFaceModelDTO;
            if (m == null)
                return;
            // OR if your items are objects:
            // var model = ModelsListBox.SelectedItem as ModelDTO;
            // string modelId = model?.Id;

            try
            {
                ResultDTO result = await HuggingFaceService.GetModelInfo(m.modelId);
                if (result.IsSuccess)
                {
                    ModelInfoDTO? model = result.Data as ModelInfoDTO;
                    if (model == null)
                        return;

                    ModelInfoTextBlock.Text = $"Created: {model.createdAt.ToString("G")}";
                    ModelInfoSizeTextBlock.Text = $"Size: {model.gguf.model_size.ToString()}Gb";
                }
                else
                {
                    MessageBox.Show($"Failed to get model info: {result.ErrorMessages}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching model info: {ex.Message}");
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            HuggingFaceModelDTO? m = ModelsListBox.SelectedItem as HuggingFaceModelDTO;
            if (m == null)
                return;
            ResultDTO result = await HuggingFaceService.GetModelInfo(m.modelId);
            if (result.IsSuccess)
            {
                ModelInfoDTO? model = result.Data as ModelInfoDTO;
                if (model == null)
                    return;
                // Application root
                string appRoot = AppContext.BaseDirectory;

                // Create "Downloads" folder under root
                string downloadsFolder = Path.Combine(appRoot, "Downloads");

                await HuggingFaceService.DownloadModelFilesAsync(model.id, appRoot);
            }
            else
            {
                MessageBox.Show($"Failed to get model info: {result.ErrorMessages}");
            }
        }
    }
}
