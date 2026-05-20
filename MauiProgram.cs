using Microsoft.Extensions.Logging;
using LingoWay.Infrastructure.Database;
using LingoWay.Infrastructure.Http;
using LingoWay.Infrastructure.Storage;
using LingoWay.Infrastructure.Repositories;
using LingoWay.Domain.Interfaces;
using LingoWay.Application.Services;
using LingoWay.Presentation.ViewModels;
using LingoWay.Views;
using Microsoft.EntityFrameworkCore;

namespace LingoWay
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // 注册Services和ViewModels
            builder.Services
                // 基础设施层
                .AddSingleton<AppDbContext>()
                .AddSingleton<FileStorageService>()
                .AddSingleton<Infrastructure.Storage.CacheService>()
                .AddSingleton<ContentClient>()
                .AddSingleton<TranslationClient>()
                .AddSingleton<RssParser>()

                // 应用层Services
                .AddSingleton<IPlaybackService, PlaybackService>()
                .AddSingleton<IDownloadService, DownloadService>()
                .AddSingleton<ISubtitleService, SubtitleService>()
                .AddSingleton<IVocabularyService, VocabularyService>()
                .AddSingleton<ITranslationService, TranslationService>()
                .AddSingleton<IContentProvider, ContentProvider>()
                .AddSingleton<ISearchService, SearchService>()
                .AddSingleton<IFavoriteService, FavoriteService>()
                .AddSingleton<ILearningService, LearningService>()

                // ViewModels
                .AddSingleton<PlayerViewModel>()
                .AddSingleton<BrowseViewModel>()
                .AddSingleton<DownloadViewModel>()
                .AddSingleton<FavoriteViewModel>()
                .AddSingleton<SearchViewModel>()
                .AddSingleton<SettingsViewModel>()

                // Pages
                .AddSingleton<MainPage>()
                .AddSingleton<PlayerPage>()
                .AddSingleton<BrowsePage>()
                .AddSingleton<DownloadsPage>()
                .AddSingleton<FavoritesPage>()
                .AddSingleton<SettingsPage>();

            // Theme service
            builder.Services.AddSingleton<IThemeService, ThemeService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // 初始化数据库
            InitializeDatabase(app.Services);

            return app;
        }

        private static void InitializeDatabase(IServiceProvider services)
        {
            try
            {
                var dbContext = services.GetRequiredService<AppDbContext>();
                dbContext.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"数据库初始化失败: {ex.Message}");
            }
        }
    }
}
