using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using VocabTrainer.Application.ViewModels;
using VocabTrainer.Common;
using VocabTrainer.Core.Algorithms;
using VocabTrainer.Core.Interfaces;
using VocabTrainer.Infrastructure.Data;
using VocabTrainer.Infrastructure.Repositories;
using VocabTrainer.Infrastructure.Services;
using VocabTrainer.Presentation.Views;

namespace VocabTrainer
{
    public partial class App : System.Windows.Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            // Init DB
            var factory = Services.GetRequiredService<IDbContextFactory<VocabDbContext>>();
            await using (var ctx = await factory.CreateDbContextAsync())
            {
                await ctx.Database.EnsureCreatedAsync();
                await DatabaseSeeder.SeedAsync(ctx);
            }

            // Apply saved settings (language + theme) before UI appears
            var settingsRepo = Services.GetRequiredService<ISettingsRepository>();
            var settings = await settingsRepo.LoadAsync();
            LocalizationService.Instance.Language = settings.InterfaceLanguage;
            if (settings.DarkTheme)
                SettingsViewModel.ApplyTheme(true);

            var mainWindow = Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();

            if (mainWindow.DataContext is MainViewModel vm)
                await vm.InitializeAsync();
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            var dbPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VocabTrainer", "vocab.db");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);

            services.AddDbContextFactory<VocabDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            services.AddSingleton<IWordCardRepository, WordCardRepository>();
            services.AddSingleton<ISettingsRepository, JsonSettingsRepository>();
            services.AddSingleton<ISpacedRepetitionService, Sm2Algorithm>();
            services.AddSingleton<ITrainingService, TrainingService>();
            services.AddSingleton<IImportService, ImportService>();
            services.AddSingleton<IStatisticsService, StatisticsService>();

            services.AddTransient<TrainingViewModel>();
            services.AddTransient<StatisticsViewModel>();
            services.AddTransient<WordManagementViewModel>();
            services.AddTransient<ImportViewModel>();
            services.AddSingleton<SettingsViewModel>();

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
        }
    }
}
