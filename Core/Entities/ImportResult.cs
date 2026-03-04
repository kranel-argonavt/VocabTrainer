using System.Collections.Generic;

namespace VocabTrainer.Core.Entities
{
    public class ImportResult
    {
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public int Errors { get; set; }
        public List<string> ErrorMessages { get; set; } = new();
        public List<WordCard> PreviewCards { get; set; } = new();
    }

    public class ColumnMapping
    {
        public int GermanColumn { get; set; } = 0;
        public int EnglishColumn { get; set; } = 1;
        public int UkrainianColumn { get; set; } = 2;
        public int ExampleColumn { get; set; } = 3;
        public int TagsColumn { get; set; } = 4;
        public bool HasHeaderRow { get; set; } = true;
    }
}
