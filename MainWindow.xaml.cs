using Ai_Project.DTOs;
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
        private readonly List<ChatMessageDTO> _chatHistory = new();

        // =====================================================
        public MainWindow(OllamaService ollamaService)
        {
            InitializeComponent();
            _ollamaService = ollamaService;

            // Initialize navbar
            AddNavbarItem(NewChatGrid, NewChatTextBlock);
            AddNavbarItem(ModelsGrid, ModelsTextBlock);
            AddNavbarItem(SettingsGrid, SettingsTextBlock);
            PromptResponse.Text = "AI answer will placed here....";
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

            string userQuery = QueryTextBox.Text.Trim();
            QueryTextBox.Clear();

            // Add user message
            AddChatMessage("User", userQuery);

            // Show placeholder for AI
            var aiPlaceholder = "Thinking...";
            AddChatMessage("AI", aiPlaceholder);

            try
            {
                string response = await _ollamaService.GenerateResponseAsync(userQuery);
                // Replace AI placeholder with actual response
                _chatHistory[^1].Text = response; // update last AI message in history
                PromptMessagesPanel.Children.RemoveAt(PromptMessagesPanel.Children.Count - 1);
                AddChatMessage("AI", response);
            }
            catch (Exception ex)
            {
                _chatHistory[^1].Text = $"⚠️ Error: {ex.Message}";
                PromptMessagesPanel.Children.RemoveAt(PromptMessagesPanel.Children.Count - 1);
                AddChatMessage("AI", $"⚠️ Error: {ex.Message}");
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

        private void AddChatMessage(string sender, string text)
        {
            // Save to history
            var message = new ChatMessageDTO { Sender = sender, Text = text };
            _chatHistory.Add(message);

            // Display in UI
            var border = new Border
            {
                Background = sender == "User" ? Brushes.LightBlue : Brushes.LightGray,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8),
                Margin = new Thickness(4, 2, 4, 2),
                MaxWidth = 600,
            };

            var tb = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14
            };

            border.Child = tb;

            var container = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = sender == "User" ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            container.Children.Add(new TextBlock
            {
                Text = sender,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Gray,
                Margin = new Thickness(4, 0, 4, 2),
                FontSize = 12
            });

            container.Children.Add(border);

            PromptMessagesPanel.Children.Add(container);
            PromptMessagesScroll.ScrollToEnd();
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
