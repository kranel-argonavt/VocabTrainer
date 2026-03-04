using System.Collections.ObjectModel;
using VocabTrainer.Core.Entities;

namespace VocabTrainer.Application.ViewModels
{
    // Helper for XAML binding
    public static class TrainingModeValues
    {
        public static ObservableCollection<TrainingMode> All { get; } =
            new(System.Enum.GetValues<TrainingMode>());
    }
}
