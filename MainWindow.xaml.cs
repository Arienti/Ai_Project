using Ai_Project.Services;
using MahApps.Metro.IconPacks;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Ai_Project
{
    public partial class MainWindow : Window
    {
        // ========== Colors from App.xaml Resources ==========
        private readonly SolidColorBrush PrimaryBg = (SolidColorBrush)Application.Current.Resources["PrimaryBg"];
        private readonly SolidColorBrush PrimaryBgHover = (SolidColorBrush)Application.Current.Resources["PrimaryBgHover"];
        private readonly SolidColorBrush PrimaryFg = (SolidColorBrush)Application.Current.Resources["PrimaryFg"];

        private readonly OllamaService _ollamaService;
        private readonly List<NavBarItem> _navBarItems = new();

        // =====================================================
        public MainWindow(OllamaService ollamaService)
        {
            InitializeComponent();
            _ollamaService = ollamaService;

            // Initialize navbar
            AddNavbarItem(NewChatGrid, NewChatTextBlock);
            AddNavbarItem(ModelsGrid, ModelsTextBlock);
            AddNavbarItem(SettingsGrid, SettingsTextBlock);

            // Example initialization text (unescaped)
            string rawResponse = "Sure, here's an example...\\n\\n```csharp\\nConsole.WriteLine(\"Hello\");```";
            PromptResponse.Text = Regex.Unescape(rawResponse);
        }

        // =====================================================
        #region Navbar Logic

        private void AddNavbarItem(Grid grid, TextBlock textBlock)
        {
            var item = new NavBarItem(grid, textBlock);
            _navBarItems.Add(item);

            grid.Tag = item;
            grid.MouseLeftButtonUp += NavBar_MouseLeftButtonUp;
            grid.MouseEnter += NavBar_MouseEnter;
            grid.MouseLeave += NavBar_MouseLeave;
        }

        private void NavBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Grid grid || grid.Tag is not NavBarItem clicked) return;
            SetActiveNavBar(clicked);
        }

        private void NavBar_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is not Grid grid || grid.Tag is not NavBarItem item) return;
            if (!item.IsActive)
                AnimateBackground(item.Grid, PrimaryBgHover.Color);
        }

        private void NavBar_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is not Grid grid || grid.Tag is not NavBarItem item) return;
            if (!item.IsActive)
                AnimateBackground(item.Grid, PrimaryBg.Color);
        }

        private void SetActiveNavBar(NavBarItem activeItem)
        {
            foreach (var item in _navBarItems)
            {
                item.IsActive = item == activeItem;
                item.Grid.Background = new SolidColorBrush(item.IsActive ? PrimaryBgHover.Color : PrimaryBg.Color);
                item.Text.Foreground = item.IsActive ? Brushes.White : PrimaryFg;
                item.Text.FontWeight = item.IsActive ? FontWeights.SemiBold : FontWeights.Normal;
            }
        }

        private static void AnimateBackground(Grid grid, Color targetColor)
        {
            var currentColor = ((SolidColorBrush)grid.Background).Color;
            var animation = new ColorAnimation
            {
                From = currentColor,
                To = targetColor,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase()
            };

            var animatedBrush = new SolidColorBrush(currentColor);
            grid.Background = animatedBrush;
            animatedBrush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }

        #endregion

        // =====================================================
        #region Ollama Query Handling

        private async void QueryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || string.IsNullOrWhiteSpace(QueryTextBox.Text))
                return;

            string query = QueryTextBox.Text.Trim();
            QueryTextBox.Clear();
            PromptResponse.Text = "Thinking...";

            try
            {
                string response = await _ollamaService.GenerateResponseAsync(query);
                PromptResponse.Text = Regex.Unescape(response);
            }
            catch (Exception ex)
            {
                PromptResponse.Text = $"⚠️ Error: {ex.Message}";
            }
        }

        #endregion

        // =====================================================
        #region Hover Effects

        private void PromptQueryBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.Tag is PackIconMaterial icon)
                icon.Visibility = Visibility.Visible;
        }

        private void PromptQueryBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.Tag is PackIconMaterial icon)
                icon.Visibility = Visibility.Collapsed;
        }

        #endregion

        // =====================================================
        #region Smooth Scroll

        private void SmoothScrollToEnd(ScrollViewer scroll)
        {
            if (scroll.ScrollableHeight == 0) return;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };

            timer.Tick += (s, e) =>
            {
                double current = scroll.VerticalOffset;
                double target = scroll.ScrollableHeight;
                double next = Math.Min(current + 200, target);

                scroll.ScrollToVerticalOffset(next);

                if (next >= target)
                {
                    timer.Stop();
                    scroll.ScrollToEnd();
                }
            };

            timer.Start();
        }

        #endregion
    }

    // =====================================================
    public class NavBarItem
    {
        public Grid Grid { get; }
        public TextBlock Text { get; }
        public bool IsActive { get; set; }

        public NavBarItem(Grid grid, TextBlock text)
        {
            Grid = grid;
            Text = text;
        }
    }
}
