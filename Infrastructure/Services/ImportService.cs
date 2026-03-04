using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using VocabTrainer.Core.Entities;
using VocabTrainer.Core.Interfaces;

namespace VocabTrainer.Infrastructure.Services
{
    public class ImportService : IImportService
    {
        private readonly IWordCardRepository _repository;
        public ImportService(IWordCardRepository repository) => _repository = repository;

        public Task<ImportResult> PreviewExcelAsync(string filePath, ColumnMapping mapping) =>
            ParseExcelAsync(filePath, mapping, dryRun: true);

        public Task<ImportResult> ImportExcelAsync(string filePath, ColumnMapping mapping) =>
            ParseExcelAsync(filePath, mapping, dryRun: false);

        private async Task<ImportResult> ParseExcelAsync(string filePath, ColumnMapping mapping, bool dryRun)
        {
            var result = new ImportResult();
            var toImport = new List<WordCard>();
            try
            {
                using var workbook = new XLWorkbook(filePath);
                var sheet = workbook.Worksheets.First();
                var rows = sheet.RowsUsed().ToList();
                int startRow = mapping.HasHeaderRow ? 1 : 0;
                for (int i = startRow; i < rows.Count; i++)
                {
                    var row = rows[i];
                    try
                    {
                        var german = GetCell(row, mapping.GermanColumn + 1);
                        if (string.IsNullOrWhiteSpace(german)) { result.Errors++; result.ErrorMessages.Add($"Row {i + 1}: German is empty."); continue; }
                        if (await _repository.ExistsAsync(german)) { result.Skipped++; continue; }
                        var card = new WordCard
                        {
                            German = german,
                            English = GetCell(row, mapping.EnglishColumn + 1),
                            Ukrainian = GetCell(row, mapping.UkrainianColumn + 1),
                            ExampleSentence = GetCell(row, mapping.ExampleColumn + 1),
                            Tags = GetCell(row, mapping.TagsColumn + 1),
                            EaseFactor = 2.5, IntervalDays = 1, NextReview = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
                        };
                        toImport.Add(card);
                        result.PreviewCards.Add(card);
                    }
                    catch (Exception ex) { result.Errors++; result.ErrorMessages.Add($"Row {i + 1}: {ex.Message}"); }
                }
                if (!dryRun && toImport.Any()) { await _repository.AddRangeAsync(toImport); result.Imported = toImport.Count; }
                else if (dryRun) result.Imported = toImport.Count;
            }
            catch (Exception ex) { result.Errors++; result.ErrorMessages.Add($"File error: {ex.Message}"); }
            return result;
        }

        public Task<ImportResult> PreviewCsvAsync(string filePath, char separator, bool hasHeader) =>
            ParseCsvAsync(filePath, separator, hasHeader, dryRun: true);

        public Task<ImportResult> ImportCsvAsync(string filePath, char separator, bool hasHeader) =>
            ParseCsvAsync(filePath, separator, hasHeader, dryRun: false);

        private async Task<ImportResult> ParseCsvAsync(string filePath, char separator, bool hasHeader, bool dryRun)
        {
            var result = new ImportResult();
            var toImport = new List<WordCard>();
            try
            {
                var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
                int start = hasHeader ? 1 : 0;
                for (int i = start; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var p = line.Split(separator);
                        var german = p.Length > 0 ? p[0].Trim().Trim('"') : string.Empty;
                        if (string.IsNullOrWhiteSpace(german)) { result.Errors++; result.ErrorMessages.Add($"Line {i + 1}: German is empty."); continue; }
                        if (await _repository.ExistsAsync(german)) { result.Skipped++; continue; }
                        var card = new WordCard
                        {
                            German = german,
                            English = p.Length > 1 ? p[1].Trim().Trim('"') : string.Empty,
                            Ukrainian = p.Length > 2 ? p[2].Trim().Trim('"') : string.Empty,
                            ExampleSentence = p.Length > 3 ? p[3].Trim().Trim('"') : string.Empty,
                            Tags = p.Length > 4 ? p[4].Trim().Trim('"') : string.Empty,
                            EaseFactor = 2.5, IntervalDays = 1, NextReview = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
                        };
                        toImport.Add(card);
                        result.PreviewCards.Add(card);
                    }
                    catch (Exception ex) { result.Errors++; result.ErrorMessages.Add($"Line {i + 1}: {ex.Message}"); }
                }
                if (!dryRun && toImport.Any()) { await _repository.AddRangeAsync(toImport); result.Imported = toImport.Count; }
                else if (dryRun) result.Imported = toImport.Count;
            }
            catch (Exception ex) { result.Errors++; result.ErrorMessages.Add($"File error: {ex.Message}"); }
            return result;
        }

        private static string GetCell(IXLRow row, int col) { try { return row.Cell(col).GetString()?.Trim() ?? string.Empty; } catch { return string.Empty; } }
    }
}
