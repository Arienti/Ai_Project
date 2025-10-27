using Ai_Project.Business;
using Ai_Project.Content.Controls;
using Ai_Project.DTO;
using Ai_Project.Services;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Ai_Project.Content
{
    /// <summary>
    /// Interaction logic for ChatPage.xaml
    /// </summary>
    public partial class ChatPage : Page, Utility.InitializablePage
    {
        OllamaService ollamaService;
        private CancellationTokenSource? thinkingCts;
        private MainWindow? mainWindow = null;
        private TopicDTO? selectedTopicDTO;

        private MessagesBusiness messagesBusiness;

        private const string messageAi = "Ai";

        private const string messageUser = "User";

        public ChatPage()
        {
            this.ollamaService = new OllamaService();
            messagesBusiness = new MessagesBusiness();
            InitializeComponent();

            if (mainWindow == null)
            {
                mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.TopicSelectedChanged += TopicSelectedChanged;
                    mainWindow.NewChat += NewChat_Event;
                    mainWindow.DeleteTopic += DeleteTopic;
                }
            }
        }

        private void DeleteTopic(TopicControl obj)
        {
            if(obj.Focused)
            {
                NewChat_Event();
            }
        }

        public void Init()
        {
            QueryTextBox.Focus();
        }

        private void NewChat_Event()
        {
            // We no longer manually clear StackPanel, ItemsControl will auto-update
            selectedTopicDTO = null;
            PromptMessagesPanel.Children.Clear();
            WelcomeAiBorder.Visibility = Visibility.Visible;
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
                // Optional: auto-scroll
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    PromptMessagesScroll.UpdateLayout();
                    PromptMessagesScroll.ScrollToEnd();
                }), DispatcherPriority.Background);
            }
        }

        //private async Task<ResultDTO> AddTopic(TopicDTO topic)
        //{
        //    ResultDTO result = await topicBusiness.Insert(topic);
        //    if (!result.IsSuccess)
        //    {
        //        Debug.WriteLine($"Failed to add topic: {result.ErrorMessages}");
        //    }
        //    return result;
        //}

        private async Task<MessagesDTO> AddMessage(MessagesDTO message)
        {
            if (selectedTopicDTO == null) return message;

            ResultDTO result = await messagesBusiness.Insert(message);
            if (!result.IsSuccess)
            {
                Debug.WriteLine($"Failed to add message: {result.ErrorMessages}");
            }
            return message;
        }

        private async void SendMessage(MessagesDTO message, TypinganimationControl typinganimation)
        {
            bool isNewTopic = selectedTopicDTO == null;

            // Add topic if new
            if (isNewTopic)
            {
                WelcomeAiBorder.Visibility = Visibility.Collapsed;
                // Generate topic
                string titlePrompt = $"Summarize this message into two words topic title: \"{message.Content}\"";
                string topic = await ollamaService.GenerateTopicAsync(titlePrompt);
                TopicDTO topicDTO = new TopicDTO
                {
                    Topic = topic
                };
                if (mainWindow != null)
                {
                    TopicDTO? newTopic = await mainWindow.OnAdd(topicDTO);
                    if (newTopic != null && newTopic.ID > 0)
                    {
                        selectedTopicDTO = newTopic;
                    }
                    else
                    {
                        selectedTopicDTO = null;
                    }
                }
            }

            if (selectedTopicDTO == null) return;

            thinkingCts?.Cancel();
            thinkingCts = null;
            message.TopicId = selectedTopicDTO.ID;

            // Generate final AI response
            string finalResponse = await ollamaService.GenerateResponseAsync(message.Content);

            typinganimation.StopTyping();
            PromptMessagesPanel.Children.Remove(typinganimation);

            MessagesDTO aiMessage = new MessagesDTO
            {
                TopicId = selectedTopicDTO.ID,
                CreatedAt = DateTime.Now,
                Sender = messageAi,
                Content = finalResponse
            };

            Dispatcher.Invoke(() =>
            {
                ResponseControl responseControl = new ResponseControl();
                responseControl.DataContext = aiMessage;
                PromptMessagesPanel.Children.Add(responseControl);
            });
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                PromptMessagesScroll.UpdateLayout();
                PromptMessagesScroll.ScrollToEnd();
            }), DispatcherPriority.Background);

            await messagesBusiness.Insert(message);

            await messagesBusiness.Insert(aiMessage);
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
    }
}