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
                // Nouns
                new WordCard { German = "der Hund",      English = "dog",              Ukrainian = "собака / пес",             ExampleSentence = "Der Hund liegt auf dem Sofa.",         Tags = "Noun,Animals" },
                new WordCard { German = "die Katze",     English = "cat",              Ukrainian = "кіт / кішка",             ExampleSentence = "Die Katze schläft den ganzen Tag.",    Tags = "Noun,Animals" },
                new WordCard { German = "das Auto",      English = "car",              Ukrainian = "автомобіль / машина",      ExampleSentence = "Mein Auto ist kaputt.",                Tags = "Noun,Transport" },
                new WordCard { German = "die Stadt",     English = "city / town",      Ukrainian = "місто",                   ExampleSentence = "Berlin ist eine große Stadt.",         Tags = "Noun,Places" },
                new WordCard { German = "das Geld",      English = "money",            Ukrainian = "гроші",                   ExampleSentence = "Ich habe kein Geld dabei.",            Tags = "Noun,Everyday" },
                new WordCard { German = "die Arbeit",    English = "work / job",       Ukrainian = "робота / праця",           ExampleSentence = "Die Arbeit beginnt um acht Uhr.",      Tags = "Noun,Work" },
                new WordCard { German = "das Kind",      English = "child",            Ukrainian = "дитина",                  ExampleSentence = "Das Kind spielt im Garten.",           Tags = "Noun,Family" },
                new WordCard { German = "die Frau",      English = "woman / wife",     Ukrainian = "жінка / дружина",          ExampleSentence = "Die Frau liest ein Buch.",             Tags = "Noun,Family" },
                new WordCard { German = "der Mann",      English = "man / husband",    Ukrainian = "чоловік",                 ExampleSentence = "Der Mann kocht heute Abend.",          Tags = "Noun,Family" },
                new WordCard { German = "das Essen",     English = "food / meal",      Ukrainian = "їжа / страва",             ExampleSentence = "Das Essen schmeckt sehr gut.",         Tags = "Noun,Food" },

                // Verbs
                new WordCard { German = "gehen",         English = "to go / to walk",  Ukrainian = "іти / ходити",             ExampleSentence = "Wir gehen morgen ins Kino.",           Tags = "Verb,Movement" },
                new WordCard { German = "kommen",        English = "to come",          Ukrainian = "приходити / приїжджати",   ExampleSentence = "Wann kommst du nach Hause?",           Tags = "Verb,Movement" },
                new WordCard { German = "sehen",         English = "to see / to watch",Ukrainian = "бачити / дивитися",        ExampleSentence = "Ich sehe jeden Abend fern.",           Tags = "Verb,Perception" },
                new WordCard { German = "kaufen",        English = "to buy",           Ukrainian = "купувати",                ExampleSentence = "Er kauft jeden Tag Brot.",             Tags = "Verb,Everyday" },
                new WordCard { German = "schreiben",     English = "to write",         Ukrainian = "писати",                  ExampleSentence = "Sie schreibt einen Brief.",            Tags = "Verb,Communication" },

                // Adjectives
                new WordCard { German = "schnell",       English = "fast / quick",     Ukrainian = "швидкий / швидко",         ExampleSentence = "Der Zug ist sehr schnell.",            Tags = "Adjective" },
                new WordCard { German = "alt",           English = "old",              Ukrainian = "старий",                  ExampleSentence = "Das Haus ist sehr alt.",               Tags = "Adjective" },
                new WordCard { German = "neu",           English = "new",              Ukrainian = "новий",                   ExampleSentence = "Ich habe ein neues Handy.",            Tags = "Adjective" },
                new WordCard { German = "wichtig",       English = "important",        Ukrainian = "важливий",                ExampleSentence = "Das ist eine wichtige Entscheidung.",  Tags = "Adjective" },
                new WordCard { German = "einfach",       English = "simple / easy",    Ukrainian = "простий / легкий",         ExampleSentence = "Die Aufgabe ist ganz einfach.",        Tags = "Adjective" },
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
