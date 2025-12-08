using Ai_Project.DTO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Ai_Project.Content.Controls
{
    /// <summary>
    /// Interaction logic for QueryControl.xaml
    /// </summary>
    public partial class QueryControl : UserControl
    {
        SolidColorBrush PrimaryBgHover = ((SolidColorBrush)App.Current.Resources["PrimaryBgHover"]);
        public QueryControl()
        {
            InitializeComponent();
            Loaded += QueryControl_Loaded;
        }

        private void QueryControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MessagesDTO msg)
            {
                QueryTextBox.Document.Blocks.Clear();
                QueryTextBox.Document.Blocks.Add(new Paragraph(new Run(msg.Content ?? string.Empty)));
            }
            else
            {
                Debug.WriteLine($"DataContext type: {DataContext?.GetType()}"); // For debugging
            }
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            FadeIn(DateCopyGrid);
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            FadeOutAndCollapse(DateCopyGrid);
        }

        public static void FadeOutAndCollapse(UIElement element, double durationSeconds = 0.3)
        {
            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromSeconds(durationSeconds))
            {
                FillBehavior = FillBehavior.Stop
            };

            fadeOut.Completed += (s, e) =>
            {
                element.Visibility = Visibility.Collapsed;
                element.Opacity = 0.0; // ensure it's fully transparent
            };

            element.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        public static void FadeIn(UIElement element, double durationSeconds = 0.3)
        {
            element.Visibility = Visibility.Visible;

            var fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromSeconds(durationSeconds));
            element.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        private void CopyToClipboardIcon_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            CopyToClipboardIcon.Background = Brushes.Transparent;
            Clipboard.SetText(QueryTextBox.Text);
            CopyToClipboardIcon.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Check;
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2),
            };

            timer.Tick += (s, e) =>
            {
                timer.Stop();
                CopyToClipboardIcon.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.ContentCopy;
            };
            timer.Start();
        }

        private void CopyToClipboardIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            CopyToClipboardIcon.Background = PrimaryBgHover;
        }
    }
}
