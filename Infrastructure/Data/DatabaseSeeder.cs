using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VocabTrainer.Core.Entities;

namespace VocabTrainer.Infrastructure.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(VocabDbContext context)
        {
            // Ensure ReviewHistory table exists (for databases created before this table was added)
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ReviewHistory (
                    Date TEXT NOT NULL PRIMARY KEY,
                    CardsReviewed INTEGER NOT NULL DEFAULT 0,
                    CorrectAnswers INTEGER NOT NULL DEFAULT 0
                )");

            if (await context.WordCards.AnyAsync()) return;

            var words = new[]
            {
                new WordCard { German = "Haus",     English = "house",     Ukrainian = "будинок / хата / дім", ExampleSentence = "Das Haus ist groß.",  Tags = "Nomen,Grundwortschatz" },
                new WordCard { German = "laufen",   English = "to run",    Ukrainian = "бігти",     ExampleSentence = "Ich laufe jeden Morgen.",          Tags = "Verben,Sport" },
                new WordCard { German = "schön",    English = "beautiful / lovely / nice", Ukrainian = "гарний / красивий / чудовий", ExampleSentence = "Das Wetter ist schön.", Tags = "Adjektive" },
                new WordCard { German = "Buch",     English = "book",      Ukrainian = "книга",     ExampleSentence = "Ich lese ein Buch.",               Tags = "Nomen,Bildung" },
                new WordCard { German = "arbeiten", English = "to work",   Ukrainian = "працювати", ExampleSentence = "Sie arbeitet im Büro.",            Tags = "Verben,Arbeit" },
                new WordCard { German = "Wasser",   English = "water",     Ukrainian = "вода",      ExampleSentence = "Bitte gib mir Wasser.",            Tags = "Nomen,Grundwortschatz" },
                new WordCard { German = "sprechen", English = "to speak",  Ukrainian = "говорити",  ExampleSentence = "Kannst du Deutsch sprechen?",     Tags = "Verben,Kommunikation" },
                new WordCard { German = "groß",     English = "big / large / tall",  Ukrainian = "великий / високий", ExampleSentence = "Das ist ein großes Haus.", Tags = "Adjektive" },
                new WordCard { German = "Zeit",     English = "time",      Ukrainian = "час",       ExampleSentence = "Ich habe keine Zeit.",             Tags = "Nomen,Grundwortschatz" },
                new WordCard { German = "lernen",   English = "to learn",  Ukrainian = "вчитися",   ExampleSentence = "Wir lernen zusammen Deutsch.",    Tags = "Verben,Bildung" },
            };

            // Set default SM-2 values
            foreach (var w in words)
            {
                w.EaseFactor = 2.5;
                w.IntervalDays = 1;
                w.NextReview = DateTime.UtcNow;
                w.CreatedAt = DateTime.UtcNow;
            }

            await context.WordCards.AddRangeAsync(words);
            await context.SaveChangesAsync();
        }
    }
}
