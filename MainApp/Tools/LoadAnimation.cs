using System.Windows;
using System.Windows.Controls;

namespace Ai_Project.Tools
{
    public class LoadAnimation : IDisposable
    {
        ProgressBar progressBar;
        Page page;
        public LoadAnimation(ProgressBar progressBar, Page page)
        {
            this.page = page;
            this.progressBar = progressBar;
            page.IsEnabled = false;
            progressBar.Visibility = Visibility.Visible;
        }

        public void Dispose()
        {
            page.IsEnabled = true;
            progressBar.Visibility = Visibility.Collapsed;
        }
    }
}
