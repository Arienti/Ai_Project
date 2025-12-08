using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ai_Project.Content.Controls
{
    /// <summary>
    /// Interaction logic for TypinganimationControl.xaml
    /// </summary>
    public partial class TypinganimationControl : UserControl
    {
        private Storyboard _storyboard;

        public TypinganimationControl()
        {
            InitializeComponent();
            Loaded += TypinganimationControl_Loaded;
        }

        private void TypinganimationControl_Loaded(object sender, RoutedEventArgs e)
        {
            StartTyping();
        }

        public void StartTyping()
        {
            _storyboard = new Storyboard();

            AddDotAnimation(Dot1, 0.0);
            AddDotAnimation(Dot2, 0.2);
            AddDotAnimation(Dot3, 0.4);

            _storyboard.RepeatBehavior = RepeatBehavior.Forever;
            _storyboard.Begin();
        }

        private void AddDotAnimation(Ellipse dot, double beginTime)
        {
            var anim = new DoubleAnimationUsingKeyFrames();
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0)));
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(-5, KeyTime.FromPercent(0.5)));
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1)));

            anim.Duration = TimeSpan.FromSeconds(0.6);
            anim.BeginTime = TimeSpan.FromSeconds(beginTime);
            Storyboard.SetTarget(anim, dot);
            Storyboard.SetTargetProperty(anim, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            // Make sure the dot has a RenderTransform
            if (dot.RenderTransform == null)
                dot.RenderTransform = new TranslateTransform();

            _storyboard.Children.Add(anim);
        }

        public void StopTyping()
        {
            _storyboard?.Stop();
        }
    }
}
