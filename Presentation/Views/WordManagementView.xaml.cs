using System.Windows.Controls;
using System.Windows.Input;
using VocabTrainer.Application.ViewModels;

namespace VocabTrainer.Presentation.Views
{
    public partial class WordManagementView : UserControl
    {
        public WordManagementView() => InitializeComponent();

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is WordManagementViewModel vm &&
                sender is DataGrid dg &&
                dg.SelectedItem is SelectableWordCard item)
            {
                vm.EditCommand.Execute(item);
            }
        }
    }
}
