using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VocabTrainer.Application.ViewModels;

namespace VocabTrainer.Presentation.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
