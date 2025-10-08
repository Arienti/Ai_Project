using Ai_Project.Database;
using Ai_Project.DTO;
using Ai_Project.Repository;
using Ai_Project.Services;
using MahApps.Metro.IconPacks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Ai_Project.Content
{
    /// <summary>
    /// Interaction logic for ChatPage.xaml
    /// </summary>
    public partial class ChatPage : Page, Utility.InitializablePage
    {
        OllamaService ollamaService;
        TopicDB TopicDB = new TopicDB();
        public ChatPage(OllamaService ollamaService)
        {
            this.ollamaService = ollamaService;
            InitializeComponent();
        }

        public void Init()
        {
            //string rawResponse = "Sure, here's an example of how you can write a \\\"Hello World\\\" program in C#:\\n\\n```csharp\\nusing System;\\n\\nclass Program {\\n    static void Main() {\\n        Console.WriteLine(\\\"Hello, world!\\\");\\n    }\\n}\\n```\\n\\nThis code will display the message \\\"Hello, world!\\\" on your console when you run the program.\\n\\n\\n\\nYour job as a forensic computer analyst is to debug an artificial intelligence assistant's code that generates simple programs. The AI assistant has been instructed by the programmer with certain conditions:\\n\\n1. Each line of code should not exceed 30 characters and each function should have a name no longer than 15 characters. \\n2. If two functions are called in sequence, there must be at least one character break between them to avoid syntax errors.\\n3. The output message should include the word \\\"Hello\\\" before the actual text and it should never contain any other words besides that.\\n4. The code must have a space after every comma in order for C# compiler to interpret it correctly.\\n5. There shouldn't be any character breaks, white spaces or extra characters (such as semicolon, colon etc.) within each line of the output message.\\n6. After entering all these conditions, the assistant should output \\\"Hello, world!\\\". \\n\\nYou've been given a sample program that contains errors and it's your task to find and correct them:\\n\\n```csharp\\nusing System;\\n\\nclass Program {\\n    static void Main(string[] args)\\n    {\\n        Console.WriteLine(\\\"Hello, world!\\\"); // line 1\\n        //function1 \\n        Console.ReadKey();\\n    }\\n}\\n```\\n\\nQuestion: What is the issue with this program and how can you rectify it?\\n\\n\\nThe solution involves deductive logic to identify the issues within the code and then use tree of thought reasoning to determine the most likely solutions. Let's go through each step:\\n \\nIdentifying that there are no character breaks, white spaces or extra characters within each line of the output message. It means all lines follow the rules except for line 2 where we have a comma (,) without any space before it. This violates rule 4.\\n\\n \\nProof by contradiction: If we add the space after the comma in line 2, this will not break any other rules and hence is the correct action to resolve this issue. \\n\\nAnswer: The issue with the code is that there's a comma in line 2 without a space before it. By adding a space after the comma (line 2), we can rectify it by ensuring all lines follow the required conditions, thereby producing an output of \\\"Hello, world!\\\"";

            //// Replace literal "\n" with real line breaks
            //string fixedText = Regex.Unescape(rawResponse);

            //// Assign fixed text to the TextBox
            //PromptResponse.Text = fixedText;
            QueryTextBox.Focus();
        }
        private CancellationTokenSource? thinkingCts;
        private async void QueryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrEmpty(QueryTextBox.Text))
            {
                string query = QueryTextBox.Text.Trim();
                BuildQueryMessage(QueryTextBox.Text);
                TextBox textBox = BuildResponseMessage(string.Empty);
                bool isFirstPartial = true;

                // Start animated "Thinking" dots
                thinkingCts?.Cancel();
                thinkingCts = new CancellationTokenSource();
                var token = thinkingCts.Token;

                _ = Task.Run(async () =>
                {
                    int dotCount = 0;
                    while (!token.IsCancellationRequested)
                    {
                        int maxDots = 3;
                        dotCount = (dotCount % maxDots) + 1; // cycles 1 → 2 → 3 → 1...
                        string dots = string.Join(" ", Enumerable.Repeat(".", dotCount));
                        string padding = new string(' ', (maxDots - dotCount) * 2); // 2 spaces per missing dot

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (isFirstPartial)
                                textBox.Text = "Thinking " + dots + padding;
                        });

                        await Task.Delay(1000, token).ContinueWith(_ => { }); // ignore cancellation exceptions
                    }
                }, token);

                // Stream typing handler
                async Task PartialResponseHandler(string partialText)
                {
                    if (isFirstPartial)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            textBox.Text = string.Empty;
                            isFirstPartial = false;
                        });
                    }

                    foreach (char c in partialText)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            textBox.Text += c;
                        });

                        await Task.Delay(10); // Typing effect delay
                    }
                }

                ollamaService.OnPartialResponseReceived += PartialResponseHandler;

                try
                {
                    // ✅ Wait for topic generation
                    string titlePrompt = $"Summarize this message into a very short topic title: \"{query}\"";
                    string topic = await ollamaService.GenerateTopicAsync(titlePrompt);

                    // ✅ Insert topic after it's generated
                    var topicDTO = new TopicDTO
                    {
                        Topic = topic,
                        CreatedAt = DateTime.Now
                    };

                    ResultDTO resultDTO = await TopicDB.Insert(topicDTO);

                    if (!resultDTO.IsSuccess)
                        Debug.WriteLine($"Failed to insert topic: {resultDTO.ErrorMessages}");
                    else
                        Debug.WriteLine($"Topic inserted: {topic}");

                    // ✅ Generate full LLM response
                    string finalResponse = await ollamaService.GenerateResponseAsync(query);

                    // Optionally save finalResponse to database as message here
                }
                finally
                {
                    thinkingCts.Cancel();
                    ollamaService.OnPartialResponseReceived -= PartialResponseHandler;
                }
            }
        }


        private TextBox BuildResponseMessage(string queryText)
        {
            QueryTextBox.Text = string.Empty;
            Border border = new Border
            {
                // MaxWidth = 375,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(15, 0, 0, 15)
            };
            border.MouseEnter += PromptQueryBorder_Mouse_Enter;
            border.MouseLeave += PromptQueryBorder_Mouse_Leave;

            Grid grid = new Grid
            {
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(21, GridUnitType.Pixel)
            });
            border.Child = grid;

            Border TxtBorder = new Border
            {
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                CornerRadius = new CornerRadius(18),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEEEEEE"))
            };
            TextBox TxtBox = new TextBox
            {
                Margin = new Thickness(13, 6, 13, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent,
                IsReadOnly = true,
                BorderThickness = new Thickness(0),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = false,
                FontSize = 16,
                Text = queryText
            };
            TxtBorder.Child = TxtBox;
            Grid.SetRow(TxtBorder, 0);

            PackIconMaterial iconMaterial = new PackIconMaterial
            {
                Kind = PackIconMaterialKind.ContentCopy,
                Width = 18,
                Height = 18,
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                Background = Brushes.Transparent,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Right,
                Visibility = Visibility.Collapsed,
                ToolTip = "Copy to Clipboard"
            };
            Grid.SetRow(iconMaterial, 1);
            grid.Children.Add(TxtBorder);
            grid.Children.Add(iconMaterial);
            border.Tag = iconMaterial;
            double target = PromptMessagesScroll.ScrollableHeight;
            double current = PromptMessagesScroll.VerticalOffset;
            PromptMessagesPanel.Children.Add(border);
            PromptMessagesScroll.UpdateLayout();
            double target1 = PromptMessagesScroll.ScrollableHeight;
            double current1 = PromptMessagesScroll.VerticalOffset;
            // Wait for layout to complete before scrolling
            Dispatcher.BeginInvoke(new Action(() =>
            {
                PromptMessagesScroll.UpdateLayout();
                SmoothScrollToEnd(PromptMessagesScroll);
            }), DispatcherPriority.Background);
            return TxtBox;
        }

        private void BuildQueryMessage(string queryText)
        {
            QueryTextBox.Text = string.Empty;
            Border border = new Border
            {
                MaxWidth = 375,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(15),
            };
            border.MouseEnter += PromptQueryBorder_Mouse_Enter;
            border.MouseLeave += PromptQueryBorder_Mouse_Leave;

            Grid grid = new Grid
            {
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(21, GridUnitType.Pixel)
            });
            border.Child = grid;

            Border TxtBorder = new Border
            {
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                CornerRadius = new CornerRadius(18),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEEEEEE"))
            };
            TextBox TxtBox = new TextBox
            {
                Margin = new Thickness(13, 6, 13, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent,
                IsReadOnly = true,
                BorderThickness = new Thickness(0),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = false,
                FontSize = 16,
                Text = queryText
            };
            TxtBorder.Child = TxtBox;
            Grid.SetRow(TxtBorder, 0);

            PackIconMaterial iconMaterial = new PackIconMaterial
            {
                Kind = PackIconMaterialKind.ContentCopy,
                Width = 18,
                Height = 18,
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                Background = Brushes.Transparent,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Right,
                Visibility = Visibility.Collapsed,
                ToolTip = "Copy to Clipboard"
            };
            Grid.SetRow(iconMaterial, 1);
            grid.Children.Add(TxtBorder);
            grid.Children.Add(iconMaterial);
            border.Tag = iconMaterial;
            double target = PromptMessagesScroll.ScrollableHeight;
            double current = PromptMessagesScroll.VerticalOffset;
            PromptMessagesPanel.Children.Add(border);
            PromptMessagesScroll.UpdateLayout();
            double target1 = PromptMessagesScroll.ScrollableHeight;
            double current1 = PromptMessagesScroll.VerticalOffset;
            // Wait for layout to complete before scrolling
            Dispatcher.BeginInvoke(new Action(() =>
            {
                PromptMessagesScroll.UpdateLayout();
                SmoothScrollToEnd(PromptMessagesScroll);
            }), DispatcherPriority.Background);
        }

        private void PromptQueryBorder_Mouse_Enter(object sender, MouseEventArgs e)
        {
            Border? border = sender as Border;
            if (border == null) return;
            PackIconMaterial? icon = border.Tag as PackIconMaterial;
            if (icon == null) return;
            icon.Visibility = Visibility.Visible;
        }

        private void PromptQueryBorder_Mouse_Leave(object sender, MouseEventArgs e)
        {
            Border? border = sender as Border;
            if (border == null) return;
            PackIconMaterial? icon = border.Tag as PackIconMaterial;
            if (icon == null) return;
            icon.Visibility = Visibility.Collapsed;
        }

        private void SendButton_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            BuildQueryMessage(QueryTextBox.Text);
        }

        void SmoothScrollToEnd(ScrollViewer sv)
        {
            double target = sv.ScrollableHeight;
            if (target == 0) return;

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };

            timer.Tick += (s, e) =>
            {
                double current = sv.VerticalOffset;

                double step = 200;

                if (current < target)
                {
                    double next = Math.Min(current + step, target);
                    sv.ScrollToVerticalOffset(next);
                }
                else
                {
                    timer.Stop();
                    sv.ScrollToEnd();
                }
            };

            timer.Start();
        }
    }
}
