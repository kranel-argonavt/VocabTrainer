using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VocabTrainer.Common;
using VocabTrainer.Core.Entities;
using VocabTrainer.Core.Interfaces;
using ClosedXML.Excel;

namespace VocabTrainer.Application.ViewModels
{
    // ── Wrapper that adds IsSelected checkbox per row ────────────────────────
    public partial class SelectableWordCard : ObservableObject
    {
        [ObservableProperty] private bool _isSelected;
        public WordCard Card { get; }
        public SelectableWordCard(WordCard card) => Card = card;
    }

    public partial class WordManagementViewModel : BaseViewModel
    {
        private readonly IWordCardRepository _repository;
        private List<WordCard> _allWords = new();

        [ObservableProperty] private ObservableCollection<SelectableWordCard> _words = new();
        [ObservableProperty] private ObservableCollection<string> _allTags = new();
        [ObservableProperty] private WordCard? _editingWord;
        [ObservableProperty] private bool _isEditing;
        [ObservableProperty] private string _searchQuery = string.Empty;
        [ObservableProperty] private string? _filterTag;
        [ObservableProperty] private string _sortMode = "German";
        [ObservableProperty] private int _selectedCount;

        public WordManagementViewModel(IWordCardRepository repository)
        {
            _repository = repository;
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            _allWords = await _repository.GetAllAsync();
            RefreshWords();
            RefreshTags();
            IsLoading = false;
        }

        partial void OnSearchQueryChanged(string value) => RefreshWords();
        partial void OnFilterTagChanged(string? value) => RefreshWords();
        partial void OnSortModeChanged(string value) => RefreshWords();

        private void RefreshWords()
        {
            // Unsubscribe old
            foreach (var s in Words) s.PropertyChanged -= OnItemSelectionChanged;

            var query = _allWords.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchQuery))
                query = query.Where(w =>
                    w.German.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    w.English.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    w.Ukrainian.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(FilterTag))
                query = query.Where(w => w.Tags.Contains(FilterTag));
            query = SortMode switch
            {
                "Difficulty"  => query.OrderBy(w => w.SuccessRate),
                "NextReview"  => query.OrderBy(w => w.NextReview),
                "ReviewCount" => query.OrderByDescending(w => w.ReviewCount),
                _             => query.OrderBy(w => w.German)
            };

            Words.Clear();
            foreach (var w in query)
            {
                var sw = new SelectableWordCard(w);
                sw.PropertyChanged += OnItemSelectionChanged;
                Words.Add(sw);
            }
            UpdateSelectedCount();
        }

        private void OnItemSelectionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectableWordCard.IsSelected))
                UpdateSelectedCount();
        }

        private void UpdateSelectedCount() =>
            SelectedCount = Words.Count(w => w.IsSelected);

        private void RefreshTags()
        {
            var tags = _allWords
                .SelectMany(w => w.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct().OrderBy(t => t).ToList();
            AllTags.Clear();
            AllTags.Add("");
            foreach (var t in tags) AllTags.Add(t);
        }

        // ── Selection commands ───────────────────────────────────────────────

        [RelayCommand]
        private void SelectAll()
        {
            foreach (var w in Words) w.IsSelected = true;
        }

        [RelayCommand]
        private void ClearSelection()
        {
            foreach (var w in Words) w.IsSelected = false;
        }

        [RelayCommand]
        private void InvertSelection()
        {
            foreach (var w in Words) w.IsSelected = !w.IsSelected;
        }

        // ── CRUD ─────────────────────────────────────────────────────────────

        [RelayCommand]
        private void AddNew()
        {
            EditingWord = new WordCard
            {
                EaseFactor = 2.5, IntervalDays = 1,
                NextReview = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
            };
            IsEditing = true;
        }

        [RelayCommand]
        private void Edit(SelectableWordCard? item)
        {
            if (item == null) return;
            var card = item.Card;
            EditingWord = new WordCard
            {
                Id = card.Id, German = card.German, English = card.English,
                Ukrainian = card.Ukrainian, ExampleSentence = card.ExampleSentence,
                Tags = card.Tags, CorrectAnswers = card.CorrectAnswers,
                WrongAnswers = card.WrongAnswers, LastReviewed = card.LastReviewed,
                NextReview = card.NextReview, IntervalDays = card.IntervalDays,
                EaseFactor = card.EaseFactor, ReviewCount = card.ReviewCount,
                CreatedAt = card.CreatedAt
            };
            IsEditing = true;
        }

        [RelayCommand]
        private async Task Save()
        {
            if (EditingWord == null || string.IsNullOrWhiteSpace(EditingWord.German))
            {
                ShowError("German field is required.");
                return;
            }
            if (EditingWord.Id == 0)
                await _repository.AddAsync(EditingWord);
            else
                await _repository.UpdateAsync(EditingWord);
            await LoadAsync();
            IsEditing = false;
        }

        [RelayCommand]
        private void CancelEdit() => IsEditing = false;

        // ── Delete single (row button) ───────────────────────────────────────
        [RelayCommand]
        private async Task Delete(SelectableWordCard? item)
        {
            if (item == null) return;
            var result = MessageBox.Show(
                $"Delete \"{item.Card.German}\"?", "VocabTrainer",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                await _repository.DeleteAsync(item.Card.Id);
                _allWords.Remove(item.Card);
                RefreshWords();
                if (IsEditing && EditingWord?.Id == item.Card.Id)
                    IsEditing = false;
            }
        }

        // ── Delete selected (bulk) ───────────────────────────────────────────
        [RelayCommand]
        private async Task DeleteSelected()
        {
            var selected = Words.Where(w => w.IsSelected).ToList();
            if (selected.Count == 0) return;

            var result = MessageBox.Show(
                $"Delete {selected.Count} selected word(s)?", "VocabTrainer",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            foreach (var item in selected)
            {
                await _repository.DeleteAsync(item.Card.Id);
                _allWords.Remove(item.Card);
                if (IsEditing && EditingWord?.Id == item.Card.Id)
                    IsEditing = false;
            }
            RefreshWords();
            ShowInfo($"Deleted {selected.Count} word(s).");
        }

        // ── Reset stats ──────────────────────────────────────────────────────
        [RelayCommand]
        private async Task ResetStats(SelectableWordCard? item)
        {
            if (item == null) return;
            var card = item.Card;
            card.CorrectAnswers = 0; card.WrongAnswers = 0;
            card.ReviewCount = 0; card.IntervalDays = 1;
            card.EaseFactor = 2.5; card.NextReview = DateTime.UtcNow;
            card.LastReviewed = null;
            await _repository.UpdateAsync(card);
            await LoadAsync();
        }

        // ── Export ───────────────────────────────────────────────────────────
        [RelayCommand]
        private async Task ExportExcel()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Export to Excel",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"VocabTrainer_Export_{DateTime.Now:yyyy-MM-dd}.xlsx"
            };
            if (dialog.ShowDialog() != true) return;
            IsLoading = true;
            try
            {
                var words = await _repository.GetAllAsync();
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Words");
                var headers = new[] { "German", "English", "Ukrainian", "Example", "Tags",
                                      "Reviews", "CorrectAnswers", "WrongAnswers",
                                      "SuccessRate%", "IntervalDays", "EaseFactor",
                                      "NextReview", "CreatedAt" };
                for (int c = 0; c < headers.Length; c++)
                {
                    var cell = ws.Cell(1, c + 1);
                    cell.Value = headers[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4A90D9");
                    cell.Style.Font.FontColor = XLColor.White;
                }
                for (int r = 0; r < words.Count; r++)
                {
                    var w = words[r];
                    int row = r + 2;
                    ws.Cell(row, 1).Value  = w.German;
                    ws.Cell(row, 2).Value  = w.English;
                    ws.Cell(row, 3).Value  = w.Ukrainian;
                    ws.Cell(row, 4).Value  = w.ExampleSentence;
                    ws.Cell(row, 5).Value  = w.Tags;
                    ws.Cell(row, 6).Value  = w.ReviewCount;
                    ws.Cell(row, 7).Value  = w.CorrectAnswers;
                    ws.Cell(row, 8).Value  = w.WrongAnswers;
                    ws.Cell(row, 9).Value  = Math.Round(w.SuccessRate, 1);
                    ws.Cell(row, 10).Value = w.IntervalDays;
                    ws.Cell(row, 11).Value = Math.Round(w.EaseFactor, 3);
                    ws.Cell(row, 12).Value = w.NextReview.ToString("yyyy-MM-dd");
                    ws.Cell(row, 13).Value = w.CreatedAt.ToString("yyyy-MM-dd");
                    if (r % 2 == 1)
                        ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F8FC");
                }
                ws.Columns().AdjustToContents();
                wb.SaveAs(dialog.FileName);
                ShowInfo($"Exported {words.Count} words to Excel.");
            }
            catch (Exception ex) { ShowError($"Export failed: {ex.Message}"); }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        private async Task ExportCsv()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Export to CSV",
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = $"VocabTrainer_Export_{DateTime.Now:yyyy-MM-dd}.csv"
            };
            if (dialog.ShowDialog() != true) return;
            IsLoading = true;
            try
            {
                var words = await _repository.GetAllAsync();
                var sb = new StringBuilder();
                sb.AppendLine("German,English,Ukrainian,Example,Tags");
                foreach (var w in words)
                    sb.AppendLine(string.Join(",",
                        CsvEscape(w.German), CsvEscape(w.English),
                        CsvEscape(w.Ukrainian), CsvEscape(w.ExampleSentence),
                        CsvEscape(w.Tags)));
                await File.WriteAllTextAsync(dialog.FileName, sb.ToString(), Encoding.UTF8);
                ShowInfo($"Exported {words.Count} words to CSV.");
            }
            catch (Exception ex) { ShowError($"Export failed: {ex.Message}"); }
            finally { IsLoading = false; }
        }

        private static string CsvEscape(string? value) =>
            string.IsNullOrEmpty(value) ? "\"\"" : $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
