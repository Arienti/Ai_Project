using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Ai_Project.Model_Manager.Helper
{
    internal class TextBoxWriter : TextWriter
    {
        private readonly TextBox _textBox;
        private readonly Dispatcher _dispatcher;

        public TextBoxWriter(TextBox textBox)
        {
            _textBox = textBox;
            _dispatcher = textBox.Dispatcher;
        }

        public override void Write(char value)
        {
            _dispatcher.Invoke(() => _textBox.AppendText(value.ToString()));
        }

        public override void Write(string value)
        {
            _dispatcher.Invoke(() => _textBox.AppendText(value));
        }

        public override Encoding Encoding => Encoding.UTF8;
    }
}
