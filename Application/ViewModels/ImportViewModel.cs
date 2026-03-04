using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using VocabTrainer.Core.Entities;
using VocabTrainer.Core.Interfaces;

namespace VocabTrainer.Application.ViewModels
{
    public partial class ImportViewModel : BaseViewModel
    {
        private readonly IImportService _importService;

        // FileType is a plain string "Excel" or "CSV" — avoids Enum parse errors
        [ObservableProperty] private bool _isExcel = true;
        [ObservableProperty] private bool _isCsv = false;
        [ObservableProperty] private string _selectedFilePath = string.Empty;
        [ObservableProperty] private bool _hasHeaderRow = true;
        [ObservableProperty] private string _csvSeparator = ",";
        [ObservableProperty] private int _germanColumn = 0;
        [ObservableProperty] private int _englishColumn = 1;
        [ObservableProperty] private int _ukrainianColumn = 2;
        [ObservableProperty] private int _exampleColumn = 3;
        [ObservableProperty] private int _tagsColumn = 4;
        [ObservableProperty] private ImportResult? _previewResult;
        [ObservableProperty] private ImportResult? _importResult;
        [ObservableProperty] private bool _previewed;
        [ObservableProperty] private ObservableCollection<WordCard> _previewWords = new();
        [ObservableProperty] private ObservableCollection<string> _errorMessages = new();
        [ObservableProperty] private bool _showResults;

        public ImportViewModel(IImportService importService)
        {
            _importService = importService;
        }

        partial void OnIsExcelChanged(bool value) { if (value) IsCsv = false; }
        partial void OnIsCsvChanged(bool value) { if (value) IsExcel = false; }

        [RelayCommand]
        private void BrowseFile()
        {
            var filter = IsExcel
                ? "Excel Files (*.xlsx)|*.xlsx"
                : "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";

            var dialog = new OpenFileDialog { Filter = filter };
            if (dialog.ShowDialog() == true)
            {
                SelectedFilePath = dialog.FileName;
                PreviewResult = null;
                ImportResult = null;
                Previewed = false;
                ShowResults = false;
                PreviewWords.Clear();
                ErrorMessages.Clear();
            }
        }

        private ColumnMapping GetMapping() => new()
        {
            GermanColumn = GermanColumn,
            EnglishColumn = EnglishColumn,
            UkrainianColumn = UkrainianColumn,
            ExampleColumn = ExampleColumn,
            TagsColumn = TagsColumn,
            HasHeaderRow = HasHeaderRow
        };

        [RelayCommand]
        private async Task Preview()
        {
            if (string.IsNullOrEmpty(SelectedFilePath)) { ShowError("Select a file first."); return; }
            IsLoading = true;

            ImportResult result;
            if (IsExcel)
                result = await _importService.PreviewExcelAsync(SelectedFilePath, GetMapping());
            else
            {
                char sep = string.IsNullOrEmpty(CsvSeparator) ? ',' : CsvSeparator[0];
                result = await _importService.PreviewCsvAsync(SelectedFilePath, sep, HasHeaderRow);
            }

            PreviewResult = result;
            PreviewWords.Clear();
            ErrorMessages.Clear();
            foreach (var w in result.PreviewCards) PreviewWords.Add(w);
            foreach (var e in result.ErrorMessages) ErrorMessages.Add(e);
            Previewed = true;
            IsLoading = false;
        }

        [RelayCommand]
        private async Task Import()
        {
            if (string.IsNullOrEmpty(SelectedFilePath)) return;
            IsLoading = true;

            ImportResult result;
            if (IsExcel)
                result = await _importService.ImportExcelAsync(SelectedFilePath, GetMapping());
            else
            {
                char sep = string.IsNullOrEmpty(CsvSeparator) ? ',' : CsvSeparator[0];
                result = await _importService.ImportCsvAsync(SelectedFilePath, sep, HasHeaderRow);
            }

            ImportResult = result;
            ShowResults = true;
            IsLoading = false;
            ShowInfo($"Import complete!\n✓ Imported: {result.Imported}\n⊘ Skipped: {result.Skipped}\n✗ Errors: {result.Errors}");
        }

        [RelayCommand]
        private void Reset()
        {
            SelectedFilePath = string.Empty;
            PreviewResult = null;
            ImportResult = null;
            Previewed = false;
            ShowResults = false;
            PreviewWords.Clear();
            ErrorMessages.Clear();
        }
    }
}
