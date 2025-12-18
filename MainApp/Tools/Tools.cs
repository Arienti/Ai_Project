using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Ai_Project.Tools
{
    public static class Tools
    {
        public static void AnimateBorderBackground(Border border, Color color)
        {
            // Implementation for color animation
            if (border == null) return;
            border.Dispatcher.BeginInvoke(new Action(() =>
            {
                ColorAnimation animation = new ColorAnimation
                {
                    To = color,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                border.Background = new SolidColorBrush(((SolidColorBrush)border.Background).Color);
                border.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);

            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }
}
