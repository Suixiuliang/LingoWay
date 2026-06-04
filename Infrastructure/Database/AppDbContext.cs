namespace LingoWay.Infrastructure.Database;

using LingoWay.Domain.Models;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// LingoWay数据库上下文
/// </summary>
public class AppDbContext : DbContext
{
    public DbSet<Podcast> Podcasts { get; set; } = null!;
    public DbSet<Episode> Episodes { get; set; } = null!;
    public DbSet<Subtitle> Subtitles { get; set; } = null!;
    public DbSet<Vocabulary> Vocabularies { get; set; } = null!;
    public DbSet<VocabularyMention> VocabularyMentions { get; set; } = null!;
    public DbSet<Download> Downloads { get; set; } = null!;
    public DbSet<LearningRecord> LearningRecords { get; set; } = null!;
    public DbSet<Favorite> Favorites { get; set; } = null!;
    public DbSet<UserVocabulary> UserVocabularies { get; set; } = null!;
    public DbSet<UserSettings> UserSettings { get; set; } = null!;

    // LRC 字幕相关
    public DbSet<LrcLine> LrcLines { get; set; } = null!;
    public DbSet<LrcWord> LrcWords { get; set; } = null!;
    public DbSet<PlaybackState> PlaybackStates { get; set; } = null!;

    public AppDbContext()
    {
        // 配置DbContext选项
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            var dbPath = GetDatabasePath();
            options.UseSqlite($"Filename={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置Podcast
        modelBuilder.Entity<Podcast>()
            .HasKey(p => p.Id);
        modelBuilder.Entity<Podcast>()
            .HasMany(p => p.Episodes)
            .WithOne(e => e.Podcast)
            .HasForeignKey(e => e.PodcastId)
            .OnDelete(DeleteBehavior.Cascade);

        // 配置Episode
        modelBuilder.Entity<Episode>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<Episode>()
            .HasMany(e => e.Subtitles)
            .WithOne(s => s.Episode)
            .HasForeignKey(s => s.EpisodeId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Episode>()
            .HasMany(e => e.LrcLines)
            .WithOne(l => l.Episode)
            .HasForeignKey(l => l.EpisodeId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Episode>()
            .HasMany(e => e.Downloads)
            .WithOne(d => d.Episode)
            .HasForeignKey(d => d.EpisodeId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Episode>()
            .HasMany(e => e.LearningRecords)
            .WithOne(l => l.Episode)
            .HasForeignKey(l => l.EpisodeId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Episode>()
            .HasMany(e => e.Favorites)
            .WithOne(f => f.Episode)
            .HasForeignKey(f => f.EpisodeId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Episode>()
            .HasMany(e => e.PlaybackStates)
            .WithOne(ps => ps.Episode)
            .HasForeignKey(ps => ps.EpisodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // 配置Subtitle
        modelBuilder.Entity<Subtitle>()
            .HasKey(s => s.Id);
        modelBuilder.Entity<Subtitle>()
            .HasMany(s => s.VocabularyMentions)
            .WithOne(vm => vm.Subtitle)
            .HasForeignKey(vm => vm.SubtitleId)
            .OnDelete(DeleteBehavior.Cascade);

        // 配置Vocabulary
        modelBuilder.Entity<Vocabulary>()
            .HasKey(v => v.Word);
        modelBuilder.Entity<Vocabulary>()
            .HasMany(v => v.Mentions)
            .WithOne(vm => vm.Vocabulary)
            .HasForeignKey(vm => vm.Word)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Vocabulary>()
            .HasMany(v => v.UserVocabularies)
            .WithOne(uv => uv.Vocabulary)
            .HasForeignKey(uv => uv.Word)
            .OnDelete(DeleteBehavior.Cascade);

        // 配置VocabularyMention
        modelBuilder.Entity<VocabularyMention>()
            .HasKey(vm => vm.Id);

        // 配置Download
        modelBuilder.Entity<Download>()
            .HasKey(d => d.Id);

        // 配置LearningRecord
        modelBuilder.Entity<LearningRecord>()
            .HasKey(l => l.Id);

        // 配置Favorite
        modelBuilder.Entity<Favorite>()
            .HasKey(f => f.Id);

        // 配置UserVocabulary
        modelBuilder.Entity<UserVocabulary>()
            .HasKey(uv => uv.Id);

        // 配置UserSettings
        modelBuilder.Entity<UserSettings>()
            .HasKey(us => us.Id);

        // 配置LrcLine
        modelBuilder.Entity<LrcLine>()
            .HasKey(ll => ll.Id);
        modelBuilder.Entity<LrcLine>()
            .HasMany(ll => ll.Words)
            .WithOne(w => w.LrcLine)
            .HasForeignKey(w => w.LrcLineId)
            .OnDelete(DeleteBehavior.Cascade);

        // 配置LrcWord
        modelBuilder.Entity<LrcWord>()
            .HasKey(w => w.Id);
        modelBuilder.Entity<LrcWord>()
            .HasOne(w => w.Vocabulary)
            .WithMany()
            .HasForeignKey(w => w.VocabularyWord)
            .OnDelete(DeleteBehavior.SetNull);

        // 配置PlaybackState
        modelBuilder.Entity<PlaybackState>()
            .HasKey(ps => ps.Id);

        // 创建索引以提高查询性能
        modelBuilder.Entity<Episode>()
            .HasIndex(e => e.PodcastId);
        modelBuilder.Entity<Episode>()
            .HasIndex(e => e.PublishedDate);

        modelBuilder.Entity<LrcLine>()
            .HasIndex(ll => ll.EpisodeId);
        modelBuilder.Entity<LrcLine>()
            .HasIndex(ll => ll.StartTime);

        modelBuilder.Entity<PlaybackState>()
            .HasIndex(ps => ps.EpisodeId);

        modelBuilder.Entity<Download>()
            .HasIndex(d => d.Status);
        modelBuilder.Entity<Download>()
            .HasIndex(d => d.CreatedDate);

        modelBuilder.Entity<LearningRecord>()
            .HasIndex(l => l.LastPlayedTime);

        modelBuilder.Entity<UserVocabulary>()
            .HasIndex(uv => uv.LastReviewedDate);
    }

    /// <summary>
    /// 获取数据库文件路径
    /// </summary>
    private static string GetDatabasePath()
    {
        var appDataDir = FileSystem.AppDataDirectory;
        var dbPath = Path.Combine(appDataDir, "lingoWay.db");
        return dbPath;
    }
}
