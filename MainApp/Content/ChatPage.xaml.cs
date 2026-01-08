using Ai_Project.Business;
using Ai_Project.Content.Controls;
using Ai_Project.DTO;
using Ai_Project.Services;
using MahApps.Metro.IconPacks;
using ModelsDTO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Run_LlamaSharp;
using Run_LlamaSharp.DTOs;

namespace Ai_Project.Content
{
    /// <summary>
    /// Interaction logic for ChatPage.xaml
    /// </summary>
    public partial class ChatPage : Page, Utility.InitializablePage
    {
        private CancellationTokenSource? thinkingCts;

        private TopicDTO? selectedTopicDTO;

        private MessagesBusiness messagesBusiness;

        private const string messageAi = "Ai";

        private const string messageUser = "User";

        TopicBusiness topicBusiness;

        DispatcherTimer? _holdTimer;
        PackIconMaterial? _pressedIcon;

        List<TopicDTO>? topics = new List<TopicDTO>();

        private List<TopicControl> _topicControls = new List<TopicControl>();

        SolidColorBrush ThirdHoverBg = (SolidColorBrush)Application.Current.Resources["ThirdHoverBg"];
        SolidColorBrush ThirdBg = (SolidColorBrush)Application.Current.Resources["ThirdBg"];
        SolidColorBrush SecondaryHoverBg = (SolidColorBrush)Application.Current.Resources["SecondaryHoverBg"];
        SolidColorBrush SecondaryBg = (SolidColorBrush)Application.Current.Resources["SecondaryBg"];

        bool _allHictory = true;
        bool _favoritesHictory = false;
        RunLlamaSharp? runLlamaCpp;
        public ChatPage()
        {
            topicBusiness = new TopicBusiness();
            //this.ollamaService = new OllamaService();
            messagesBusiness = new MessagesBusiness();
            InitializeComponent();

            ModelDTO model = new ModelDTO
            {
                _id = "qwen-3-8b-q4_k_m",
                id = "Qwen/Qwen-3-8B-Q4_K_M",
                path = @"D:\Ai_Project\MainApp\bin\Debug\net8.0-windows\Models\bartowski\cognitivecomputations_Dolphin-Mistral-24B-Venice-Edition-GGUF\cognitivecomputations_Dolphin-Mistral-24B-Venice-Edition-IQ2_M.gguf"
            };
            string modelPath = @"C:\Users\Wizard\Downloads\Qwen3-8B-Q4_K_M.gguf";
           // runLlamaCpp = new RunLlamaSharp();
           // runLlamaCpp.InitializeAsync(model).Wait();
        }

        public void Init()
        {
            PromptMessagesPanel.Children.Clear();
            WelcomeAiBorder.Visibility = Visibility.Visible;
            selectedTopicDTO = null;
            UpdateControls();
        }

        private async void UpdateControls()
        {
            HistoryList.Children.Clear();
            _topicControls.Clear();

            topics = await topicBusiness.GetAll();
            if (topics == null) return;

            List<TopicDTO>? list = list = new List<TopicDTO>();
            if (_favoritesHictory)
            {
                list = topics.Where(t => t.isFavorite).ToList();
            }
            else
            {
                list = topics;
            }
            foreach (var topic in list)
            {
                var control = new TopicControl(topicBusiness)
                {
                    DataContext = topic
                };

                // Set favorite icon
                control.addToFavoritesIcon.Kind = topic.isFavorite
                    ? MahApps.Metro.IconPacks.PackIconMaterialKind.Star
                    : MahApps.Metro.IconPacks.PackIconMaterialKind.StarOutline;

                HistoryList.Children.Add(control);

                // Local handler to reset pressed flags
                void ResetPressedFlags()
                {
                    control._deleteButtonIsPressed = false;
                    control._addToFavoritesButtonIsPressed = false;
                }

                // Delete button
                control.DeleteTopicBorder.PreviewMouseUp += async (_, __) =>
                {
                    control._deleteButtonIsPressed = true;
                    if (MessageBox.Show("Are you sure to delete this topic?", "AI", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        await topicBusiness.Delete(topic.ID);
                        if (selectedTopicDTO != null && selectedTopicDTO.ID == topic.ID)
                        {
                            PromptMessagesPanel.Children.Clear();
                            WelcomeAiBorder.Visibility = Visibility.Visible;
                            selectedTopicDTO = null;
                        }
                        UpdateControls();
                    }
                };

                // Add to favorites button
                control.addToFavoritesBorder.PreviewMouseUp += async (_, __) =>
                {
                    control._addToFavoritesButtonIsPressed = true;
                    topic.isFavorite = !topic.isFavorite;
                    await topicBusiness.Update(topic);
                    UpdateControls();
                };

                // Topic selection
                control.MouseUp += async (_, __) =>
                {
                    if (control.Focused || control._deleteButtonIsPressed || control._addToFavoritesButtonIsPressed)
                    {
                        ResetPressedFlags();
                        return;
                    }

                    setActiveControl(control);
                    await Task.Delay(50);
                    TopicSelectedChanged(control);
                };

                _topicControls.Add(control);
                Dispatcher.Invoke(() => QueryTextBox.Focus());
            }
        }

        private void setActiveControl(TopicControl control)
        {
            foreach (var c in _topicControls)
            {
                if (c == control)
                {
                    c.SetActive();
                }
                else
                {
                    c.SetInActive();
                }
            }
        }

        private TopicControl? GetActiveControl()
        {
            foreach (TopicControl c in _topicControls)
            {
                if (c.Focused)
                {
                    return c;
                }
            }
            return null;
        }

        public async Task<TopicDTO?> OnAdd(TopicDTO topic)
        {
            if (_favoritesHictory)
            {
                topic.isFavorite = true;
            }
            ResultDTO result = await topicBusiness.Insert(topic);
            if (result != null && result.IsSuccess)
            {
                UpdateControls();

                TopicDTO? topicDTO = result.Data as TopicDTO;
                if (topicDTO != null)
                {
                    TopicControl? control = _topicControls
                        .FirstOrDefault(c => (c.DataContext as TopicDTO)?.ID == topicDTO.ID);
                    if (control != null)
                        setActiveControl(control);
                }
            }
            return topic;
        }

        private async void TopicSelectedChanged(TopicControl control)
        {
            PromptMessagesPanel.Children.Clear();
            if (control?.DataContext is TopicDTO topic)
            {
                selectedTopicDTO = topic;

                List<MessagesDTO>? messages = await messagesBusiness.GetMessagesByTopicId(topic.ID);
                if (messages != null)
                {
                    selectedTopicDTO.Messages = messages;

                    foreach (MessagesDTO message in messages)
                    {
                        if (message.Sender.Equals(messageUser))
                        {
                            QueryControl queryControl = new QueryControl();
                            queryControl.DataContext = message;
                            PromptMessagesPanel.Children.Add(queryControl);
                        }
                        else
                        {
                            ResponseControl responseControl = new ResponseControl();
                            responseControl.DataContext = message;
                            PromptMessagesPanel.Children.Add(responseControl);
                        }
                    }
                }
                WelcomeAiBorder.Visibility = Visibility.Collapsed;

                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    PromptMessagesScroll.UpdateLayout();
                    PromptMessagesScroll.ScrollToEnd();
                    QueryTextBox.Focus();
                }), DispatcherPriority.Background);
            }
        }

        private List<(string role, string message)> Prompt(MessagesDTO currentMessage, TopicDTO topic)
        {
            List<(string role, string content)> conversation = new();
            if (topic == null || string.IsNullOrEmpty(currentMessage.Content))
                return conversation;
            foreach (var message in topic.Messages)
            {
                if (message.Sender == messageUser)
                {
                    conversation.Add(("user", message.Content));
                }
                else
                    conversation.Add(("assistant", message.Content.Replace("*", "")
                                                                  .Replace("`", "")
                                                                  .Replace("<", "")
                                                                  .Replace(">", "")));
            }
            conversation.Add(("user", currentMessage.Content));
            return conversation;
        }

        private async void SendMessage(MessagesDTO message, TypinganimationControl typinganimation)
        {
            TopicDTO topicDTO;
            if (selectedTopicDTO == null)
            {
                WelcomeAiBorder.Visibility = Visibility.Collapsed;
                topicDTO = new TopicDTO { Topic = message.Content };
            }
            else
            {
                topicDTO = selectedTopicDTO;
            }
            thinkingCts?.Cancel();
            thinkingCts = null;

            // Generate prompt string
            //string prompt = BuildPrompt(message, topicDTO);
            List<(string role, string message)> conversation = Prompt(message, topicDTO);
            // Generate AI response
            //string finalResponse = await ollamaService.GenerateResponseAsync(prompt);
            // string finalResponse = await runLlamaCpp.RunLlama(modelPath, prompt);

            string finalResponse = await runLlamaCpp.GenerateResponse(conversation);
            // Stop typing animation
            MessagesDTO aiMessage = new MessagesDTO
            {
                TopicId = 0,
                CreatedAt = DateTime.Now,
                Sender = messageAi,
                Content = finalResponse
            };
            typinganimation.StopTyping();
            PromptMessagesPanel.Children.Remove(typinganimation);
            PromptMessagesPanel.Children.Add(new ResponseControl { DataContext = aiMessage });
            // Insert topic if new
            if (selectedTopicDTO == null)
            {
                TopicDTO? newTopic = await OnAdd(topicDTO);
                if (newTopic == null || newTopic.ID <= 0) return;
                selectedTopicDTO = newTopic;
            }

            message.TopicId = selectedTopicDTO.ID;
            aiMessage.TopicId = selectedTopicDTO.ID;

            await messagesBusiness.Insert(message);
            await messagesBusiness.Insert(aiMessage);

            await Dispatcher.BeginInvoke(() =>
            {
                PromptMessagesScroll.UpdateLayout();
                PromptMessagesScroll.ScrollToEnd();
                QueryTextBox.Focus();
            }, DispatcherPriority.Background);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentMessage"></param>
        /// <param name="topic"></param>
        /// <returns></returns>
        private string BuildPrompt(MessagesDTO currentMessage, TopicDTO topic)
        {
            if (topic == null || topic.Messages == null || topic.Messages.Count == 0)
                return currentMessage.Content;

            // Take last 30 user + last 30 AI messages
            var lastUser = topic.Messages.Where(m => m.Sender == messageUser).TakeLast(30);
            var lastAi = topic.Messages.Where(m => m.Sender == messageAi).TakeLast(30);

            List<MessagesDTO> context = new List<MessagesDTO>();
            context.AddRange(lastUser);
            context.AddRange(lastAi);
            context.Add(currentMessage);

            // Build the prompt string
            return string.Join("\n", context.Select(m => $"{m.Sender}: {m.Content}"));
        }


        private void QueryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrEmpty(QueryTextBox.Text))
            {
                e.Handled = true;

                MessagesDTO messageDTO = new MessagesDTO
                {
                    Content = QueryTextBox.Text.Trim(),
                    Sender = messageUser,
                    CreatedAt = DateTime.Now
                };

                QueryTextBox.Text = string.Empty;

                QueryControl queryControl = new QueryControl();
                queryControl.DataContext = messageDTO;
                PromptMessagesPanel.Children.Add(queryControl);

                TypinganimationControl typinganimation = new TypinganimationControl();
                PromptMessagesPanel.Children.Add(typinganimation);
                PromptMessagesScroll.UpdateLayout();
                PromptMessagesScroll.ScrollToEnd();

                SendMessage(messageDTO, typinganimation);
            }
        }

        private void SendButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            // Simply call KeyDown handler manually
            QueryTextBox_KeyDown(sender, new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Enter)
            { RoutedEvent = Keyboard.KeyDownEvent });
        }

        private void searchButton_GotFocus(object sender, RoutedEventArgs e)
        {
            if (searchButton.IsFocused && searchBorder.Visibility == Visibility.Visible)
                return;

            double to = 43;
            searchBorder.Visibility = Visibility.Visible;
            DoubleAnimation animation = new DoubleAnimation
            {
                To = to,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            searchBorder.BeginAnimation(FrameworkElement.HeightProperty, animation);
            searchTextBox.Focus();
        }


        private void searchButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            searchButton.Focus();
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (searchButton.IsFocused)
                    return;

                double to = 0;
                DoubleAnimation animation = new DoubleAnimation
                {
                    To = to,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                animation.Completed += (s, _) =>
                {
                    searchBorder.Visibility = Visibility.Collapsed;
                };

                searchBorder.BeginAnimation(FrameworkElement.HeightProperty, animation);

            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            Border? border = sender as Border;
            if (border == null || (border.Equals(AllTopicButton) && _allHictory) || (border.Equals(FavoritesTopicButton) && _favoritesHictory))
            {
                return;
            }
            Color to = border.Equals(AllTopicButton) ? ThirdBg.Color : SecondaryBg.Color;
            Tools.Tools.AnimateBorderBackground(border, to);
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            Border? border = sender as Border;
            if (border == null || (border.Equals(AllTopicButton) && _allHictory) || (border.Equals(FavoritesTopicButton) && _favoritesHictory))
            {
                return;
            }
            Color to = border.Equals(AllTopicButton) ? ThirdHoverBg.Color : SecondaryHoverBg.Color;
            Tools.Tools.AnimateBorderBackground(border, to);
        }

        private void All_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            PromptMessagesPanel.Children.Clear();
            WelcomeAiBorder.Visibility = Visibility.Visible;
            selectedTopicDTO = null;
            Border? border = sender as Border;
            if (border == null) return;
            if (border.Name.Equals(AllTopicButton.Name))
            {
                _allHictory = true;
                _favoritesHictory = false;
                Tools.Tools.AnimateBorderBackground(FavoritesTopicButton, SecondaryHoverBg.Color);
            }
            else
            {
                _allHictory = false;
                _favoritesHictory = true;
                Tools.Tools.AnimateBorderBackground(AllTopicButton, ThirdHoverBg.Color);
            }
            UpdateControls();
        }

        private async void deleteHistoryBtn_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (topics == null) return;
            if (MessageBox.Show("Are you sure you want clear the history?", "AI", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                foreach (TopicDTO topic in topics)
                {
                    await topicBusiness.Delete(topic.ID);
                }
                HistoryList.Children.Clear();
                PromptMessagesPanel.Children.Clear();
                WelcomeAiBorder.Visibility = Visibility.Visible;
                selectedTopicDTO = null;
            }
        }

        private void PackIconMaterial_PreviewLeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            _pressedIcon = sender as PackIconMaterial;
            if (_pressedIcon == null) return;

            _pressedIcon.Background = Brushes.LightGray;

            // Apply immediately once
            ChangeValue(_pressedIcon);

            // Start timer with initial delay
            _holdTimer = new DispatcherTimer();
            _holdTimer.Interval = TimeSpan.FromMilliseconds(500); // Initial delay 500ms
            _holdTimer.Tick += HoldTimer_Tick;
            _holdTimer.Start();
        }

        private void HoldTimer_Tick(object? sender, EventArgs e)
        {
            if (_pressedIcon == null) return;

            // After first tick, shorten interval for faster repeating
            _holdTimer!.Interval = TimeSpan.FromMilliseconds(20);
            ChangeValue(_pressedIcon);
        }

        private void IconsButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StopHold();
        }

        private void PackIconMaterial_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            StopHold();
        }

        private void StopHold()
        {
            if (_holdTimer != null)
            {
                _holdTimer.Stop();
                _holdTimer = null;
            }

            if (_pressedIcon != null)
            {
                _pressedIcon.Background = Brushes.Transparent;
                _pressedIcon = null;
            }
        }

        private void ChangeValue(PackIconMaterial icon)
        {
            double temp = double.Parse(TempTextBox.Text);
            int token = int.Parse(MaxTokenTextBox.Text);

            switch (icon)
            {
                case var _ when icon == PlusTempButton:
                    if (temp >= 2.0) return;
                    temp = Math.Round(temp + 0.1, 1);
                    TempTextBox.Text = temp.ToString();
                    break;

                case var _ when icon == MinusTempButton:
                    if (temp <= 0.1) return;
                    temp = Math.Round(temp - 0.1, 1);
                    TempTextBox.Text = temp.ToString();
                    break;

                case var _ when icon == PlusTokenButton:
                    if (token >= 500) return;
                    token++;
                    MaxTokenTextBox.Text = token.ToString();
                    break;

                case var _ when icon == MinusTokenButton:
                    if (token <= 0) return;
                    token--;
                    MaxTokenTextBox.Text = token.ToString();
                    break;
            }
        }
    }
}