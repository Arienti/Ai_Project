using Ai_Project.Business;
using Ai_Project.Content;
using Ai_Project.Content.Controls;
using Ai_Project.DTO;
using Ai_Project.Services;
using Ai_Project.Utility;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Ai_Project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SolidColorBrush PrimaryBg = ((SolidColorBrush)App.Current.Resources["PrimaryBg"]);
        SolidColorBrush PrimaryBgHover = ((SolidColorBrush)App.Current.Resources["PrimaryBgHover"]);
        SolidColorBrush PrimaryFg = ((SolidColorBrush)App.Current.Resources["PrimaryFg"]);
        //LlamaCppService cppService = new LlamaCppService();
        
        public class NavBarControl
        {
            public TextBlock? Text { get; set; }
            public Page? Page { get; set; }
            public Grid? grid { get; set; }
            public bool isActive = false;
        }

        TopicBusiness topicBusiness;

        List<NavBarControl>? NavBarControls = null;

        ChatPage chatPage;

        ModelsPage modelsPage = new ModelsPage();

        SettingsPage settingsPage = new SettingsPage();

        public MainWindow(OllamaService ollamaService, TopicBusiness topicBusiness)
        {
            this.topicBusiness = topicBusiness;
            InitializeComponent();


            chatPage = new ChatPage();

            AddNavbarControls(NewChatGrid, NewChatTextBlock, chatPage);
            AddNavbarControls(ModelsGrid, ModelsTextBlock, modelsPage);
            AddNavbarControls(SettingsGrid, SettingsTextBlock, settingsPage);

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Ensure the executable is downloaded
            //  await llama.EnsureInstalledAsync();
            //cppService.Start();
            // Run llama.cpp with your own arguments (model, prompt, etc.)
            //string modelPath = @"D:\Ai_Project\bin\Debug\net8.0-windows\Downloads\ai21labs\AI21-Jamba-Reasoning-3B-GGUF\jamba-reasoning-3b-F16.gguf";
            //string prompt = "i need understand what C# is";
           // string prompt = "Hello";
            //var response = await cppService.AskAsync(prompt);
            //var response = await cppService.RunModelAsync(modelPath, prompt);
            //Debug.WriteLine(response);
        }

        private void AddNavbarControls(Grid grid, TextBlock textBlock, Page? page)
        {
            if (NavBarControls == null)
            {
                NavBarControls = new List<NavBarControl>();
            }
            NavBarControl NavBarControl = new NavBarControl() { grid = grid, Text = textBlock, Page = page };
            NavBarControls.Add(NavBarControl);
            NavBarControl.grid.Tag = NavBarControl;
            NavBarControl.grid.MouseLeftButtonUp += NavBarControl_MouseLeftButtonUp;
            NavBarControl.grid.MouseEnter += NavBarControl_MouseEnter;
            NavBarControl.grid.MouseLeave += NavBarControl_MouseLeave;

            if (NavBarControl.grid.Name.Equals("NewChatGrid"))
            {
                SetActiveNavBarControl(NavBarControl);
            }
        }

        private void NavBarControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            base.Dispatcher.Invoke(() =>
            {
                NavBarControl? navBarControl = (sender as Grid)?.Tag as NavBarControl;
                if (navBarControl == null)
                {
                    return;
                }

                SetActiveNavBarControl(navBarControl);
            });
        }

        private void NavBarControl_MouseEnter(object sender, MouseEventArgs e)
        {
            base.Dispatcher.Invoke(() =>
            {
                NavBarControl? navBarControl = (sender as Grid)?.Tag as NavBarControl;
                if (navBarControl != null && navBarControl.grid != null && !navBarControl.isActive)
                {
                    ColorAnimation colorAnimation = new ColorAnimation
                    {
                        To = PrimaryBgHover.Color,
                        Duration = new Duration(TimeSpan.FromMilliseconds(300))
                    };
                    navBarControl.grid.Background = new SolidColorBrush(((SolidColorBrush)navBarControl.grid.Background).Color);
                    navBarControl.grid.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                }
            });
        }

        private void NavBarControl_MouseLeave(object sender, MouseEventArgs e)
        {
            base.Dispatcher.Invoke(() =>
            {
                NavBarControl? navBarControl = (sender as Grid)?.Tag as NavBarControl;
                if (navBarControl != null && navBarControl.grid != null && !navBarControl.isActive)
                {
                    ColorAnimation colorAnimation = new ColorAnimation
                    {
                        To = PrimaryBg.Color,
                        Duration = new Duration(TimeSpan.FromMilliseconds(300))
                    };
                    navBarControl.grid.Background = new SolidColorBrush(((SolidColorBrush)navBarControl.grid.Background).Color);
                    navBarControl.grid.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                }
            });
        }

        private void SetActiveNavBarControl(NavBarControl control)
        {
            if (NavBarControls == null || control.grid == null) return;
            
            if (control.grid.Equals(NewChatGrid) && control.isActive)
            {
                if (control.Page as InitializablePage != null)
                {
                    (control.Page as InitializablePage)?.Init();
                }
                return;
            }

            if (control.isActive) return;
            foreach (var navControl in NavBarControls)
            {
                if (navControl == null || navControl.grid == null || navControl.Text == null) return;
                bool isSelected = navControl == control;
                navControl.isActive = isSelected;
                navControl.grid.Background = isSelected ? PrimaryBgHover : PrimaryBg;
                navControl.Text.Foreground = isSelected ? Brushes.White : PrimaryFg;
                navControl.Text.FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal;
            }

            MainFrame.Content = control.Page;

            if (control.Page as InitializablePage != null)
            {
                (control.Page as InitializablePage)?.Init();
            }
        }

        private void Border_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            bool navBarCollapsed = NavBarGrid.Width == 47;
            double widthTarget = navBarCollapsed ? 200 : 47;
            DoubleAnimation animation = new DoubleAnimation
            {
                From = NavBarGrid.ActualWidth,
                To = widthTarget,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            if (navBarCollapsed)
            {
                CollapseNavBarBorder.HorizontalAlignment = HorizontalAlignment.Right;
                CollapseNavBarBorder.Margin = new Thickness(0, 6, 4, 6);
                CollapseNavBarBorder.ToolTip = "Collapse";
                //HistoryGrid.Visibility =
                //    NewChatTextBlock.Visibility =
                //    ModelsTextBlock.Visibility =
                //    SettingsTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                //HistoryGrid.Visibility = Visibility.Collapsed;
                animation.Completed += (s, e) =>
                {
                    ModelsTextBlock.Visibility =
                    SettingsTextBlock.Visibility = Visibility.Collapsed;

                    CollapseNavBarBorder.HorizontalAlignment = HorizontalAlignment.Center;
                    CollapseNavBarBorder.Margin = new Thickness(0, 6, 0, 6);
                    CollapseNavBarBorder.ToolTip = "Expand";
                };
            }

            NavBarGrid.BeginAnimation(Grid.WidthProperty, animation);
        }

        //public class GridLengthAnimation : AnimationTimeline
        //{
        //    public override Type TargetPropertyType => typeof(GridLength);

        //    public double From { get; }
        //    public double To { get; }

        //    // Constructor to enforce setting From and To
        //    public GridLengthAnimation(double from, double to)
        //    {
        //        From = from;
        //        To = to;
        //    }

        //    public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        //    {
        //        double progress = animationClock.CurrentProgress ?? 0.0;
        //        double currentValue = From + (To - From) * progress;
        //        return new GridLength(currentValue, GridUnitType.Pixel);
        //    }

        //    protected override Freezable CreateInstanceCore()
        //    {
        //        return new GridLengthAnimation(From, To);
        //    }
        //}
    }
}