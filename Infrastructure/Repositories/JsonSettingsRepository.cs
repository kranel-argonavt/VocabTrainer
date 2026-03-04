using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using VocabTrainer.Core.Entities;
using VocabTrainer.Core.Interfaces;

namespace VocabTrainer.Infrastructure.Repositories
{
    public class JsonSettingsRepository : ISettingsRepository
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VocabTrainer", "settings.json");

        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public async Task<AppSettings> LoadAsync()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return new AppSettings();
                var json = await File.ReadAllTextAsync(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json, Options) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public async Task SaveAsync(AppSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var json = JsonSerializer.Serialize(settings, Options);
            await File.WriteAllTextAsync(SettingsPath, json);
        }
    }
}
