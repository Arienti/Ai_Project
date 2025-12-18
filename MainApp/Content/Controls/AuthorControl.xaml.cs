using Ai_Project.DTO;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ai_Project.Content.Controls
{
    /// <summary>
    /// Interaction logic for AuthorControl.xaml
    /// </summary>
    public partial class AuthorControl : UserControl
    {
        public AuthorControl()
        {
            InitializeComponent();
            SetRandomBorderColor();
            this.DataContextChanged += AuthorControl_DataContextChanged;
            
        }

        private void AuthorControl_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            SetText();
        }

        private void SetRandomBorderColor()
        {
            Random random = new Random();
            Color color;

            do
            {
                // Generate random RGB values
                byte r = (byte)random.Next(0, 256);
                byte g = (byte)random.Next(0, 256);
                byte b = (byte)random.Next(0, 256);

                color = Color.FromRgb(r, g, b);
            }
            while ((color.R == 0 && color.G == 0 && color.B == 0) ||   // black
                   (color.R == 255 && color.G == 255 && color.B == 255)); // white

            LogoBorder.Background = new SolidColorBrush(color);
        }
        private void SetText()
        {
            if (this.DataContext is string)
            {
                string? author = this.DataContext as string;
                if (string.IsNullOrEmpty(author)) return;

                FirstLetterTextBlock.Text = author.Trim().Substring(0, 1).ToUpper();
                AuthorTextBlock.Text = char.ToUpper(author.Trim()[0]) + author[1..];
                this.Tag = author;
            }
        }
    }
}
