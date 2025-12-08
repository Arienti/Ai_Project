using Ai_Project.Business;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Ai_Project.Content.Controls
{
    public partial class TopicControl : UserControl
    {
        SolidColorBrush TopicFocusBg = ((SolidColorBrush)App.Current.Resources["TopicFocusBg"]);
        SolidColorBrush TopicHoverBg = ((SolidColorBrush)App.Current.Resources["TopicHoverBg"]);
        SolidColorBrush TopicBg = ((SolidColorBrush)App.Current.Resources["TopicBg"]);

        public bool Focused = false;

        TopicBusiness topicBusiness;

        public bool _deleteButtonIsPressed = false;

        public bool _addToFavoritesButtonIsPressed = false;

        public TopicControl(TopicBusiness topicBusiness)
        {
            this.topicBusiness = topicBusiness;
            InitializeComponent();
        }

        public void SetActive()
        {
            Focused = true;
            Dispatcher.Invoke(() =>
            {
                ColorAnimation colorAnimation = new ColorAnimation
                {
                    To = TopicFocusBg.Color,
                    Duration = TimeSpan.FromMilliseconds(300)
                };

                border.Background = new SolidColorBrush(((SolidColorBrush)border.Background).Color);
                border.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            });
        }

        public void SetInActive()
        {
            Focused = false;
            Dispatcher.Invoke(() =>
            {
                if (Focused)
                    return;
                ColorAnimation colorAnimation = new ColorAnimation
                {
                    To = TopicBg.Color,
                    Duration = TimeSpan.FromMilliseconds(300)
                };

                border.Background = new SolidColorBrush(((SolidColorBrush)border.Background).Color);
                border.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            });
        }

        private void UserControl_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (Focused)
                    return;
                ColorAnimation colorAnimation = new ColorAnimation
                {
                    To = TopicHoverBg.Color,
                    Duration = TimeSpan.FromMilliseconds(300)
                };

                border.Background = new SolidColorBrush(((SolidColorBrush)border.Background).Color);
                border.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            });
        }

        private void UserControl_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (Focused)
                    return;
                ColorAnimation colorAnimation = new ColorAnimation
                {
                    To = TopicBg.Color,
                    Duration = TimeSpan.FromMilliseconds(300)
                };

                border.Background = new SolidColorBrush(((SolidColorBrush)border.Background).Color);
                border.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            });
        }
    }
}
