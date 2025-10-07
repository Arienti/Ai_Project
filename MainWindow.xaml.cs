using Ai_Project.DTOs;
using Ai_Project.Services;
using MahApps.Metro.IconPacks;
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
        private readonly ChatStorageService _chatStorageService;
        private readonly List<NavBarItem> _navBarItems = new();
        private ChatDTO _currentChat;
        private readonly string welcomeAIMessage = "Welcome to chat! Waiting for your questions..";

        // =====================================================
        public MainWindow(OllamaService ollamaService)
        {
            InitializeComponent();
            _ollamaService = ollamaService;
            _chatStorageService = new ChatStorageService();

            // Initialize navbar
            AddNavbarItem(NewChatGrid, NewChatTextBlock);
            AddNavbarItem(ModelsGrid, ModelsTextBlock);
            AddNavbarItem(SettingsGrid, SettingsTextBlock);

            // Load chat history
            LoadChatHistory();

            // Start new chat
            StartNewChat();
        }

        // =====================================================
        #region Chat Management

        private void StartNewChat()
        {
            // Save current chat if it has messages
            if (_currentChat != null && _currentChat.Messages.Count > 1) // More than just welcome message
            {
                SaveCurrentChat();
            }

            // Create new chat
            _currentChat = new ChatDTO
            {
                Title = "New Chat",
                CreatedAt = DateTime.Now,
                LastModified = DateTime.Now
            };

            // Clear UI
            PromptMessagesPanel.Children.Clear();

            // Add welcome message
            AddChatMessage("AI", welcomeAIMessage, saveToHistory: false);
            _currentChat.Messages.Add(new ChatMessageDTO
            {
                Sender = "AI",
                Text = welcomeAIMessage,
                Timestamp = DateTime.Now
            });

            // Refresh history sidebar
            LoadChatHistory();
        }

        private void SaveCurrentChat()
        {
            if (_currentChat == null || _currentChat.Messages.Count <= 1)
                return;

            // Generate title from first user message if still "New Chat"
            if (_currentChat.Title == "New Chat")
            {
                var firstUserMessage = _currentChat.Messages.FirstOrDefault(m => m.Sender == "User");
                if (firstUserMessage != null)
                {
                    _currentChat.Title = firstUserMessage.Text.Length > 50
                        ? firstUserMessage.Text.Substring(0, 47) + "..."
                        : firstUserMessage.Text;
                }
            }

            _chatStorageService.SaveChat(_currentChat);
        }

        private void LoadChat(ChatDTO chat)
        {
            // Save current chat before switching
            if (_currentChat != null && _currentChat.Messages.Count > 1)
            {
                SaveCurrentChat();
            }

            _currentChat = chat;

            // Clear UI
            PromptMessagesPanel.Children.Clear();

            // Load messages
            foreach (var message in _currentChat.Messages)
            {
                AddChatMessage(message.Sender, message.Text, saveToHistory: false);
            }

            PromptMessagesScroll.ScrollToEnd();
        }

        private void LoadChatHistory()
        {
            HistoryGrid.Children.Clear();

            var chats = _chatStorageService.LoadAllChats();

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            foreach (var chat in chats)
            {
                var chatItem = CreateChatHistoryItem(chat);
                stackPanel.Children.Add(chatItem);
            }

            HistoryGrid.Children.Add(stackPanel);
        }

        private Border CreateChatHistoryItem(ChatDTO chat)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(5, 2, 5, 2),
                Padding = new Thickness(8, 6, 8, 6),
                Cursor = Cursors.Hand,
                Tag = chat
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleBlock = new TextBlock
            {
                Text = chat.Title,
                Foreground = Brushes.White,
                FontSize = 13,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleBlock, 0);

            var deleteIcon = new PackIconMaterial
            {
                Kind = PackIconMaterialKind.Delete,
                Width = 14,
                Height = 14,
                Foreground = Brushes.Gray,
                Cursor = Cursors.Hand,
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(deleteIcon, 1);

            grid.Children.Add(titleBlock);
            grid.Children.Add(deleteIcon);
            border.Child = grid;

            // Event handlers
            border.MouseLeftButtonUp += (s, e) =>
            {
                if (e.OriginalSource is PackIconMaterial)
                    return; // Don't load chat if delete icon was clicked

                LoadChat(chat);
            };

            border.MouseEnter += (s, e) =>
            {
                border.Background = new SolidColorBrush(Color.FromRgb(60, 60, 65));
                deleteIcon.Visibility = Visibility.Visible;
            };

            border.MouseLeave += (s, e) =>
            {
                border.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));
                deleteIcon.Visibility = Visibility.Collapsed;
            };

            deleteIcon.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                var result = MessageBox.Show(
                    "Are you sure you want to delete this chat?",
                    "Delete Chat",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    _chatStorageService.DeleteChat(chat.Id);
                    LoadChatHistory();

                    // Start new chat if deleted current chat
                    if (_currentChat?.Id == chat.Id)
                    {
                        StartNewChat();
                    }
                }
            };

            return border;
        }

        #endregion

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

            // Handle "New Chat" action
            if (grid == NewChatGrid)
            {
                StartNewChat();
            }

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

                // Update last message in history and UI
                if (_currentChat.Messages.Count > 0)
                {
                    _currentChat.Messages[^1].Text = response;
                }

                PromptMessagesPanel.Children.RemoveAt(PromptMessagesPanel.Children.Count - 1);
                AddChatMessage("AI", response, saveToHistory: false);

                // Auto-save after each exchange
                SaveCurrentChat();
                LoadChatHistory();
            }
            catch (Exception ex)
            {
                string errorMsg = $"⚠️ Error: {ex.Message}";

                if (_currentChat.Messages.Count > 0)
                {
                    _currentChat.Messages[^1].Text = errorMsg;
                }

                PromptMessagesPanel.Children.RemoveAt(PromptMessagesPanel.Children.Count - 1);
                AddChatMessage("AI", errorMsg, saveToHistory: false);
            }
        }

        #endregion

        // =====================================================
        #region UI Message Display

        private void AddChatMessage(string sender, string text, bool saveToHistory = true)
        {
            // Save to history
            if (saveToHistory)
            {
                var message = new ChatMessageDTO
                {
                    Sender = sender,
                    Text = text,
                    Timestamp = DateTime.Now
                };
                _currentChat.Messages.Add(message);
            }

            // Check last group
            var lastGroup = PromptMessagesPanel.Children.Count > 0
                ? PromptMessagesPanel.Children[^1] as StackPanel
                : null;

            // Determine if we can append to the last group
            bool sameSenderGroup = false;
            if (lastGroup != null && lastGroup.Tag is string lastSender)
                sameSenderGroup = lastSender == sender;

            StackPanel messageGroup;
            if (!sameSenderGroup)
            {
                // Create new group container
                messageGroup = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = sender == "User" ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                    Margin = new Thickness(0, 8, 0, 8),
                    Tag = sender
                };

                // Header (Sender + timestamp)
                var headerPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = sender == "User" ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                    Margin = new Thickness(8, 0, 8, 2)
                };

                var senderText = new TextBlock
                {
                    Text = sender,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.DimGray,
                    FontSize = 12
                };

                var timeText = new TextBlock
                {
                    Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                    Foreground = Brushes.Gray,
                    FontSize = 11,
                    Margin = new Thickness(6, 0, 0, 0)
                };

                headerPanel.Children.Add(senderText);
                headerPanel.Children.Add(timeText);
                messageGroup.Children.Add(headerPanel);

                PromptMessagesPanel.Children.Add(messageGroup);
            }
            else
            {
                messageGroup = lastGroup!;
            }

            // Create message bubble
            var border = new Border
            {
                Background = sender == "User"
                    ? new SolidColorBrush(Color.FromRgb(180, 220, 255))
                    : new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8),
                Margin = new Thickness(8, 2, 8, 2),
                MaxWidth = 600
            };

            var tb = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14
            };
            border.Child = tb;

            messageGroup.Children.Add(border);

            // Auto-scroll
            PromptMessagesScroll.ScrollToEnd();
        }

        #endregion

        // =====================================================
        #region Window Events

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Save current chat on exit
            SaveCurrentChat();
            base.OnClosing(e);
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