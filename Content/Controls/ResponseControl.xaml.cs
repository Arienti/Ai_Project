using Ai_Project.DTO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Ai_Project.Content.Controls
{
    /// <summary>
    /// Interaction logic for ResponseControl.xaml
    /// </summary>
    public partial class ResponseControl : UserControl
    {
        SolidColorBrush ChatForeground = ((SolidColorBrush)App.Current.Resources["ChatForeground"]);
        SolidColorBrush TextChatBGColor = ((SolidColorBrush)App.Current.Resources["TextChatBGColor"]);
        SolidColorBrush CodeBackgroudControl = ((SolidColorBrush)App.Current.Resources["CodeBackgroudControl"]);
        string entireMessage = string.Empty;
        public ResponseControl()
        {
            InitializeComponent();
            this.DataContextChanged += ResponseControl_DataContextChanged;
        }

        private void ResponseControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is MessagesDTO message)
            {
                //message.Content = CleanMarkdown(message.Content);
                SetMessageContent(message.Content);
            }
        }

        private void CopyToClipboardIcon_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            CopyToClipboardIcon.Background = Brushes.Transparent;
            Clipboard.SetText(entireMessage);
            CopyToClipboardIcon.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Check;
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2),
            };

            timer.Tick += (s, e) =>
            {
                timer.Stop();
                CopyToClipboardIcon.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.ContentCopy;
            };
            timer.Start();

        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public void SetMessageContent(string message)
        {
            ContentPanel.Children.Clear();

            bool inCodeBlock = false;
            var codeBuilder = new StringBuilder();
            var lines = message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            entireMessage = "";

            foreach (var rawLine in lines)
            {
                string line = NormalizeInvisibleWhitespace(rawLine);

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Code block handling
                if (line.StartsWith("```"))
                {
                    inCodeBlock = !inCodeBlock;

                    if (!inCodeBlock && codeBuilder.Length > 0)
                    {
                        entireMessage += codeBuilder.ToString() + "\n";
                        ContentPanel.Children.Add(CreateCodeGrid(codeBuilder.ToString()));
                        codeBuilder.Clear();
                    }

                    continue;
                }

                if (inCodeBlock)
                {
                    codeBuilder.AppendLine(line);
                    continue;
                }

                // Bullet conversion and remove backticks
                line = line.Replace("·", "•").Replace("`", "");
                line = Regex.Replace(line, @"^(\s*)\*\s+", "$1• ");

                // Split for bold
                var parts = line.Split(new[] { "**" }, StringSplitOptions.None);

                var paragraph = new Paragraph
                {
                    Margin = new Thickness(0),
                    Padding = new Thickness(0),
                    LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                    LineHeight = 19
                };

                var paragraphTextBuilder = new StringBuilder();

                for (int i = 0; i < parts.Length; i++)
                {
                    string partText = NormalizeInvisibleWhitespace(parts[i]);

                    if (i % 2 == 0)
                        partText = partText.Replace("*", "");

                    if (string.IsNullOrWhiteSpace(partText))
                        continue;

                    var run = new Run
                    {
                        Text = partText,
                        FontWeight = (i % 2 == 1) ? FontWeights.Bold : FontWeights.Normal
                    };

                    paragraph.Inlines.Add(run);
                    paragraphTextBuilder.Append(partText);
                }

                if (paragraph.Inlines.Count > 0)
                {
                    var textBox = new Emoji.Wpf.RichTextBox
                    {
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(0),
                        Background = Brushes.Transparent,
                        Foreground = ChatForeground,
                        FontSize = 16,
                        Margin = new Thickness(0),
                        IsReadOnly = true,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                        Document = new FlowDocument()
                        {
                            PagePadding = new Thickness(0) // <-- Remove default internal padding
                        }
                    };

                    textBox.Document.Blocks.Add(paragraph);
                    ContentPanel.Children.Add(textBox);

                    entireMessage += paragraphTextBuilder.ToString() + "\n";
                }
            }

            // Flush any unclosed code block
            if (inCodeBlock && codeBuilder.Length > 0)
            {
                entireMessage += codeBuilder.ToString() + "\n";
                var codeBox = new Emoji.Wpf.RichTextBox
                {
                    BorderThickness = new Thickness(0),
                    Text = codeBuilder.ToString(),
                    FontFamily = new FontFamily("Consolas"),
                    Background = Brushes.Transparent,
                    IsReadOnly = true,
                    Foreground = Brushes.Black,
                    FontSize = 16,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                ContentPanel.Children.Add(codeBox);
            }
        }

        /// <summary>
        /// Normalize invisible whitespace: NBSP, zero-width space, BOM
        /// </summary>
        private string NormalizeInvisibleWhitespace(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Replace('\u00A0', ' ');
            s = s.Replace("\u200B", "");
            s = s.Replace("\uFEFF", "");
            return s;
        }

        private Border CreateCodeGrid(string code)
        {
            Border border = new Border
            {
                BorderThickness = new Thickness(0),
                Background = CodeBackgroudControl,
                CornerRadius = new CornerRadius(16),
                Margin = new Thickness(3, 0, 3, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.Background = Brushes.Transparent;
            var copyButton = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 0, 2),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(7, 2, 7, 2)
            };

            var txtButton = new TextBlock
            {
                Text = "Copy",
                Foreground = ChatForeground,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };

            copyButton.MouseEnter += (s, e) =>
            {
                txtButton.Foreground = Brushes.White;
            };

            copyButton.MouseLeave += (s, e) =>
            {
                txtButton.Foreground = ChatForeground;
            };
            copyButton.Child = txtButton;
            var codeBox = new Emoji.Wpf.RichTextBox
            {
                Text = code,
                FontFamily = new FontFamily("Consolas"),
                Foreground = ChatForeground,
                Background = Brushes.Transparent,
                IsReadOnly = true,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 5),
                Cursor = Cursors.Arrow,
                FontSize = 16
            };

            copyButton.PreviewMouseUp += (s, e) =>
            {
                Clipboard.SetText(codeBox.Text);
                txtButton.Text = "Copied";
                DispatcherTimer timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2),
                };

                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    txtButton.Text = "Copy";
                };
                timer.Start();
            };

            Grid.SetRow(copyButton, 0);
            Grid.SetRow(codeBox, 1);

            grid.Children.Add(copyButton);
            grid.Children.Add(codeBox);
            border.Child = grid;
            return border;
        }

        private void AddFormattedText(Emoji.Wpf.RichTextBox rtb, string text)
        {
            // Replace bullets (* ) with middle dot
            text = Regex.Replace(text, @"^\*\s+", "· ", RegexOptions.Multiline);

            // Remove inline code backticks
            text = Regex.Replace(text, @"`(.+?)`", "$1");

            rtb.Document.Blocks.Clear();
            var paragraph = new Paragraph();

            int currentIndex = 0;
            var boldRegex = new Regex(@"\*\*(.+?)\*\*");
            foreach (Match match in boldRegex.Matches(text))
            {
                // Text before bold
                if (match.Index > currentIndex)
                {
                    paragraph.Inlines.Add(new Run(text.Substring(currentIndex, match.Index - currentIndex)));
                }

                // Bold text
                paragraph.Inlines.Add(new Bold(new Run(match.Groups[1].Value)));

                currentIndex = match.Index + match.Length;
            }

            // Remaining text
            if (currentIndex < text.Length)
            {
                paragraph.Inlines.Add(new Run(text.Substring(currentIndex)));
            }

            rtb.Document.Blocks.Add(paragraph);
        }




        private string CleanMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            bool inCodeBlock = false;
            var sb = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.StartsWith("```") || line.StartsWith("···"))
                {
                    inCodeBlock = !inCodeBlock;
                    sb.AppendLine("···"); // visual separator
                    continue;
                }

                if (inCodeBlock)
                {
                    sb.AppendLine(line); // keep code formatting
                    continue;
                }

                // Handle bullets starting with *
                if (line.TrimStart().StartsWith("* "))
                {
                    sb.AppendLine("· " + line.TrimStart().Substring(2));
                    continue;
                }

                sb.AppendLine(line); // normal text or emojis
            }

            return sb.ToString();

        }

        private void ContentPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            FadeIn(DateCopyGrid);
        }

        private void ContentPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            FadeOutAndCollapse(DateCopyGrid);
        }

        public static void FadeOutAndCollapse(UIElement element, double durationSeconds = 0.3)
        {
            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromSeconds(durationSeconds))
            {
                FillBehavior = FillBehavior.Stop
            };

            fadeOut.Completed += (s, e) =>
            {
                element.Visibility = Visibility.Collapsed;
                element.Opacity = 0.0; // ensure it's fully transparent
            };

            element.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        public static void FadeIn(UIElement element, double durationSeconds = 0.3)
        {
            element.Visibility = Visibility.Visible;

            var fadeIn = new DoubleAnimation(0.0, 1.0, TimeSpan.FromSeconds(durationSeconds));
            element.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        private void CopyToClipboardIcon_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            CopyToClipboardIcon.Background = TextChatBGColor;
        }
    }
}
