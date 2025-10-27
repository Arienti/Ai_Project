using Ai_Project.Business;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Ai_Project.Content.Controls
{
    public partial class TopicControl : UserControl
    {
        SolidColorBrush PrimaryBgHover = ((SolidColorBrush)App.Current.Resources["PrimaryBgHover"]);
        SolidColorBrush PrimaryBg = ((SolidColorBrush)App.Current.Resources["PrimaryBg"]);
        SolidColorBrush PrimaryFg = ((SolidColorBrush)App.Current.Resources["PrimaryFg"]);

        SolidColorBrush FourthyBg = ((SolidColorBrush)App.Current.Resources["FourthyBg"]);

        public bool Focused = false;

        TopicBusiness topicBusiness;
        public TopicControl(TopicBusiness topicBusiness)
        {
            this.topicBusiness = topicBusiness;
            InitializeComponent();
        }

        private void TextBlock_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (!Focused)
                {
                    ColorAnimation colorAnimation = new ColorAnimation
                    {
                        To = PrimaryBgHover.Color, //(Color)ColorConverter.ConvertFromString("#FFFFFF"),
                        Duration = TimeSpan.FromMilliseconds(300)
                    };
                    if (sender is TextBlock)
                    {
                        TopicTextBlock.Background = new SolidColorBrush(((SolidColorBrush)TopicTextBlock.Background).Color);
                        TopicTextBlock.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                    }
                    else
                    {
                        DeleteTopicBorder.Background = new SolidColorBrush(((SolidColorBrush)DeleteTopicBorder.Background).Color);
                        DeleteTopicBorder.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                    }
                }
            });
        }

        private void TextBlock_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (Focused)
                    return;
                ColorAnimation colorAnimation = new ColorAnimation
                {
                    To = PrimaryBg.Color,
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                if (sender is TextBlock)
                {
                    TopicTextBlock.Background = new SolidColorBrush(((SolidColorBrush)TopicTextBlock.Background).Color);
                    TopicTextBlock.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                }
                else
                {
                    DeleteTopicBorder.Background = new SolidColorBrush(((SolidColorBrush)DeleteTopicBorder.Background).Color);
                    DeleteTopicBorder.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                }
            });
        }

        public void SetActive()
        {
            Focused = true;
            TopicTextBlock.Background = PrimaryBgHover;
            TopicTextBlock.Foreground = FourthyBg;
        }

        public void SetInActive()
        {
            Focused = false;
            TopicTextBlock.Background = PrimaryBg;
            TopicTextBlock.Foreground = PrimaryFg;
        }
    }
}
