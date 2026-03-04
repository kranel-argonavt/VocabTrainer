using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

namespace VocabTrainer.Application.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string? _statusMessage;

        protected void ShowError(string message)
        {
            StatusMessage = "⚠ " + message;
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected void ShowInfo(string message)
        {
            StatusMessage = "✓ " + message;
            // Auto-clear after 4 seconds
            Task.Delay(4000).ContinueWith(_ =>
            {
                if (StatusMessage?.EndsWith(message) == true)
                    System.Windows.Application.Current.Dispatcher.Invoke(() => StatusMessage = null);
            });
        }
    }
}
