using Ai_Project.DTO;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace Ai_Project.Content.Controls
{
    /// <summary>
    /// Interaction logic for ModelControl.xaml
    /// </summary>
    public partial class ModelControl : UserControl
    {
        private const string _defaultModelName = "Chat-Ai";
        public ModelControl()
        {
            InitializeComponent();
            this.DataContextChanged += AuthorControl_DataContextChanged;
        }
        private void AuthorControl_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            SetName();
        }
        private void SetName()
        {
            if (this.DataContext is HuggingFaceModelDTO)
            {
                HuggingFaceModelDTO? model = this.DataContext as HuggingFaceModelDTO;
                if (model == null) return;

                // Find the first occurrence of "/"
                int firstSlashIndex = model.id.IndexOf('/');

                // Extract the substring after the first "/"
                string substringAfterSlash = model.id.Substring(firstSlashIndex + 1);

                // Define a regular expression to match the first "-<number>B-" pattern
                string pattern = @"-(\d+(\.\d+)?(_\d+)?(x\d+)?[bB])-";

                // Match the first occurrence of "-<number>B-" in the substring
                Match match = Regex.Match(substringAfterSlash, pattern);

                if (match.Success)
                {
                    // Extract the model name between the first "/" and the first "-<number>B-"
                    string modelName = substringAfterSlash.Substring(0, match.Index);
                    ModelNameTextBlock.Text = modelName;
                }
                else
                {
                    ModelNameTextBlock.Text = _defaultModelName;
                }
                this.Tag = model;
            }
        }
    }
}
