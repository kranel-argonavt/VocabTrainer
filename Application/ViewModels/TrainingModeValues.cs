using CommunityToolkit.Mvvm.ComponentModel;

namespace VocabTrainer.Application.ViewModels
{
    public partial class MultipleChoiceOption : ObservableObject
    {
        [ObservableProperty] private string _text = string.Empty;
        [ObservableProperty] private bool _isCorrect;
        [ObservableProperty] private bool _isSelected;
        [ObservableProperty] private bool _showResult;
        public VocabTrainer.Core.Entities.WordCard Card { get; set; } = null!;
    }

    public partial class TagItem : ObservableObject
    {
        public string Name { get; }

        [ObservableProperty] private bool _isSelected;
        [ObservableProperty] private bool _isVisible = true;

        public TagItem(string name) => Name = name;
    }
}
