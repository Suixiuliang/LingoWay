# LingoWay - 技术架构与实现指南

## 一、MAUI项目结构完整说明

### 1.1 文件夹结构规划

```
LingoWay/
│
├── 📁 Presentation               # MAUI UI层 (XAML + C#)
│   ├── 📁 Views                  # XAML页面
│   │   ├── MainPage.xaml         # 主页 (Tab式导航)
│   │   ├── PlayerPage.xaml       # 播放页面 (沉浸式)
│   │   ├── BrowsePage.xaml       # 浏览/发现
│   │   ├── DownloadsPage.xaml    # 下载管理
│   │   ├── FavoritesPage.xaml    # 收藏
│   │   ├── SearchPage.xaml       # 搜索
│   │   ├── SettingsPage.xaml     # 设置
│   │   └── DetailPage.xaml       # 资源详情
│   │
│   ├── 📁 ViewModels             # MVVM ViewModels
│   │   ├── BaseViewModel.cs      # 基类 (实现INotifyPropertyChanged)
│   │   ├── MainViewModel.cs
│   │   ├── PlayerViewModel.cs
│   │   ├── BrowseViewModel.cs
│   │   ├── DownloadViewModel.cs
│   │   ├── FavoriteViewModel.cs
│   │   ├── SearchViewModel.cs
│   │   └── SettingsViewModel.cs
│   │
│   ├── 📁 Components             # 可复用组件
│   │   ├── PlayerControls.xaml   # 播放器控制栏
│   │   ├── SubtitleView.xaml     # 字幕显示组件
│   │   ├── EpisodeCard.xaml      # 剧集卡片
│   │   ├── VocabularyPopup.xaml  # 词汇弹窗
│   │   └── LoadingIndicator.xaml
│   │
│   ├── 📁 Behaviors              # 自定义行为 (Interaction)
│   │   ├── DragGestureBehavior.cs
│   │   └── TapFeedbackBehavior.cs
│   │
│   ├── 📁 Converters             # 值转换器
│   │   ├── BoolToVisibilityConverter.cs
│   │   ├── TimeSpanToStringConverter.cs
│   │   ├── DownloadProgressConverter.cs
│   │   └── VocabularyColorConverter.cs
│   │
│   └── 📁 Resources              # XAML样式/主题
│       ├── Colors.xaml           # 颜色定义
│       ├── Styles.xaml           # 全局样式
│       ├── Typography.xaml       # 字体/排版
│       ├── Icons.xaml            # 图标资源
│       └── Themes/
│           ├── LightTheme.xaml
│           └── DarkTheme.xaml
│
├── 📁 Domain                     # 业务逻辑层
│   ├── 📁 Models                 # 核心模型
│   │   ├── Episode.cs            # 剧集/资源
│   │   ├── Podcast.cs            # 播客源
│   │   ├── Subtitle.cs           # 字幕
│   │   ├── Vocabulary.cs         # 词汇
│   │   ├── Learning.cs           # 学习记录
│   │   ├── Download.cs           # 下载
│   │   └── Settings.cs           # 用户设置
│   │
│   ├── 📁 Interfaces             # 业务接口
│   │   ├── IPlaybackService.cs
│   │   ├── IDownloadService.cs
│   │   ├── ISubtitleService.cs
│   │   ├── IVocabularyService.cs
│   │   ├── ITranslationService.cs
│   │   ├── IContentProvider.cs
│   │   └── IStorageService.cs
│   │
│   └── 📁 Constants
│       ├── AppConstants.cs       # App级常量
│       ├── ColorConstants.cs     # 色彩常量
│       └── ApiConstants.cs       # API端点
│
├── 📁 Application                # 应用服务层
│   ├── 📁 Services               # 业务实现服务
│   │   ├── PlaybackService.cs    # 播放逻辑
│   │   ├── DownloadService.cs    # 下载管理
│   │   ├── VocabularyService.cs  # 词汇识别与解析
│   │   ├── SubtitleService.cs    # 字幕处理与同步
│   │   ├── TranslationService.cs # 翻译集成
│   │   ├── ContentProvider.cs    # 资源聚合
│   │   ├── SearchService.cs      # 搜索逻辑
│   │   └── SettingsService.cs    # 用户偏好
│   │
│   ├── 📁 DTOs                   # 数据传输对象
│   │   ├── EpisodeDto.cs
│   │   ├── SubtitleDto.cs
│   │   └── VocabularyDto.cs
│   │
│   ├── 📁 Mappers                # 对象映射
│   │   └── MappingProfile.cs
│   │
│   └── 📁 Behaviors              # 业务行为
│       └── PlaybackBehavior.cs
│
├── 📁 Infrastructure             # 基础设施层
│   ├── 📁 Http                   # HTTP通信
│   │   ├── ContentClient.cs      # 资源获取 (RSS/API)
│   │   ├── TranslationClient.cs  # 翻译API (腾讯/Azure)
│   │   ├── HttpClientFactory.cs  # HTTP工厂
│   │   └── RssParser.cs          # RSS解析
│   │
│   ├── 📁 Database               # 数据层
│   │   ├── AppDbContext.cs       # EF Core DbContext
│   │   ├── Migrations/           # 数据库迁移
│   │   └── Repositories/
│   │       ├── EpisodeRepository.cs
│   │       ├── VocabularyRepository.cs
│   │       ├── LearningRepository.cs
│   │       └── DownloadRepository.cs
│   │
│   ├── 📁 Storage                # 文件存储
│   │   ├── FileStorageService.cs # 文件操作
│   │   ├── CacheService.cs       # 内存缓存
│   │   ├── DatabaseCleaner.cs    # 清理策略
│   │   └── DirectoryManager.cs   # 目录管理
│   │
│   ├── 📁 Platform               # 平台特定实现
│   │   ├── iOS/
│   │   │   ├── AudioPlayer.cs
│   │   │   ├── MediaControl.cs
│   │   │   └── NativeApis.cs
│   │   ├── Android/
│   │   │   ├── AudioPlayer.cs
│   │   │   ├── ExoPlayerAdapter.cs
│   │   │   └── NativeApis.cs
│   │   ├── Windows/
│   │   │   ├── AudioPlayer.cs
│   │   │   └── NativeApis.cs
│   │   └── MacCatalyst/
│   │       ├── AudioPlayer.cs
│   │       └── NativeApis.cs
│   │
│   └── 📁 AI                     # AI/ML集成
│       ├── WhisperService.cs     # 字幕生成 (ONNX)
│       ├── VocabularyExtractor.cs # 词汇提取
│       └── Models/               # 预训练模型
│
├── 📁 Resources                  # 全局资源
│   ├── 📁 Data                   # 数据文件
│   │   ├── vocabulary.db         # 词汇SQLite数据库
│   │   ├── vocab_list.json       # 词汇列表 (高频/核心/难点)
│   │   └── common_phrases.json   # 常用短语
│   │
│   ├── 📁 Localization           # 多语言
│   │   ├── Strings.en.xaml
│   │   ├── Strings.zh-CN.xaml
│   │   └── Strings.ja.xaml
│   │
│   ├── 📁 Images                 # 图片资源
│   │   ├── logo.svg
│   │   ├── splash.svg
│   │   └── icons/
│   │
│   └── 📁 Models                 # AI模型 (可选)
│       └── whisper-tiny-zh.onnx  # Whisper模型
│
├── Platforms/                    # MAUI平台特定
│   ├── Android/
│   ├── iOS/
│   ├── MacCatalyst/
│   └── Windows/
│
├── MauiProgram.cs                # 依赖注入配置
├── AppShell.xaml                 # 导航定义
├── AppShell.xaml.cs
├── App.xaml                      # 应用定义
├── App.xaml.cs
│
├── lingoWay.csproj               # 项目文件
├── appsettings.json              # 配置文件
│
└── 📁 Tests (可选)
	├── LingoWay.Tests.Unit/      # 单元测试
	├── LingoWay.Tests.Integration/ # 集成测试
	└── LingoWay.Tests.UI/        # UI测试
```

---

## 二、MVVM 模式实现

### 2.1 基础 ViewModel 类

所有ViewModel继承自 `ObservableObject` (CommunityToolkit.Mvvm)

```csharp
// 示例结构
public partial class PlayerViewModel : ObservableObject
{
	[ObservableProperty]
	private string currentEpisodeTitle = "";

	[ObservableProperty]
	private TimeSpan currentPosition;

	[ObservableProperty]
	private bool isPlaying;

	[RelayCommand]
	private async Task Play() { /* 实现 */ }

	[RelayCommand]
	private async Task Pause() { /* 实现 */ }
}
```

### 2.2 依赖注入配置

在 `MauiProgram.cs` 中注册所有服务

```csharp
public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder()
			.UseMauiApp<App>()
			.ConfigureFonts(fonts => /* ... */)
			// 注册Services
			.Services
				.AddSingleton<IPlaybackService, PlaybackService>()
				.AddSingleton<IDownloadService, DownloadService>()
				.AddSingleton<ISubtitleService, SubtitleService>()
				.AddSingleton<IVocabularyService, VocabularyService>()
				.AddSingleton<IContentProvider, ContentProvider>()
				.AddSingleton<AppDbContext>()
				// 注册ViewModels
				.AddSingleton<PlayerViewModel>()
				.AddSingleton<BrowseViewModel>()
				.AddSingleton<DownloadViewModel>()
				// 注册Pages
				.AddSingleton<PlayerPage>()
				.AddSingleton<BrowsePage>();

		return builder.Build();
	}
}
```

---

## 三、平台特定代码处理

### 3.1 使用 Conditional Compilation

```csharp
using System.Runtime.InteropServices;

public class AudioPlayer
{
#if IOS
	private AVAudioPlayer? player;

	public void Initialize()
	{
		// iOS特定初始化
	}
#elif ANDROID
	private MediaPlayer? player;

	public void Initialize()
	{
		// Android特定初始化
	}
#elif WINDOWS
	private MediaPlayerElement? player;

	public void Initialize()
	{
		// Windows特定初始化
	}
#elif MACCATALYST
	private AVAudioPlayer? player;

	public void Initialize()
	{
		// macOS特定初始化
	}
#endif
}
```

### 3.2 使用 Dependency Service (推荐)

```csharp
// 接口定义 (Domain)
public interface INativeAudioPlayer
{
	void Play(string filePath);
	void Pause();
}

// iOS实现
[assembly: Dependency(typeof(LingoWay.Platforms.iOS.NativeAudioPlayer))]
namespace LingoWay.Platforms.iOS;

public class NativeAudioPlayer : INativeAudioPlayer
{
	private AVAudioPlayer? player;

	public void Play(string filePath)
	{
		// iOS AVAudioPlayer
	}

	public void Pause()
	{
		player?.Pause();
	}
}

// 在ViewModel中使用
var audioPlayer = ServiceHelper.GetService<INativeAudioPlayer>();
audioPlayer.Play(episodePath);
```

---

## 四、关键技术实现方案

### 4.1 音频播放

#### 推荐库
- **CommunityToolkit.MVVM**: 轻量级MVVM框架 ✅
- **MediaManager.MAUI**: 跨平台媒体管理 (如果需要高级功能)
- **Platform-specific**: AVPlayer (iOS/macOS), ExoPlayer (Android), MediaPlayerElement (Windows)

#### 实现方案
```
基础架构:
├─ IPlaybackService (接口)
├─ PlaybackServiceImpl (共通逻辑)
└─ Platform-specific实现
   ├─ iOS: AVAudioPlayer + AVPlayer
   ├─ Android: ExoPlayer (推荐) / MediaPlayer3
   ├─ Windows: MediaPlayerElement
   └─ macOS: AVAudioPlayer
```

### 4.2 字幕和翻译

#### 离线字幕 (Whisper集成)
1. **使用 Whisper.NET**
   ```
   NuGet: Whisper.net
   依赖: ONNX Runtime
   模型文件: whisper-tiny-zh.onnx (仅80MB)
   ```

2. **集成步骤**
   - 下载ONNX模型，打包到Resources/Models
   - 创建WhisperService包装
   - 异步后台处理 (不阻塞UI)
   - 支持进度回调

#### 在线翻译
1. **腾讯翻君 API** (国内最优)
   ```
   优势: 便宜、快速、针对中英优化
   价格: ~50元/百万字
   ```

2. **Azure Translator**
   ```
   优势: 稳定、支持多种语言
   需要: Azure订阅
   ```

### 4.3 词汇识别

#### 数据准备
1. **高频词表** (前1000)
   - 来源: BNC/COCA语料库
   - 格式: JSON或SQLite

2. **词汇数据库**
   - 使用SQLite存储
   - 包含: 单词、词性、释义、例句、音标、词根
   - 预设 + 用户收藏

#### 实现流程
```csharp
1. 获取字幕文本 -> 分词 -> 标记词性
2. 查询本地词汇库 -> 返回释义
3. 根据难度等级着色显示
4. 点击词汇 -> 显示详情弹窗
5. 可选: 保存到生词本
```

### 4.4 下载与离线管理

#### 下载队列
```csharp
public interface IDownloadService
{
	Task<Download> EnqueueAsync(Episode episode);
	IAsyncEnumerable<DownloadProgress> DownloadAsync(Download download);
	Task DeleteAsync(Download download);
}
```

#### 离线存储结构
```
AppData/
├─ Episodes/              # 离线视频/音频
│  ├─ episode_001/
│  │  ├─ audio.m4a
│  │  ├─ subtitles.vtt
│  │  └─ metadata.json
│  └─ ...
├─ Cache/                 # 缓存
├─ Database/
│  └─ app.db             # SQLite数据库
└─ Temp/                 # 临时文件
```

---

## 五、UI/UX 实现细节

### 5.1 主题系统

```xaml
<!-- Resources/Styles/Colors.xaml -->
<Color x:Key="BackgroundPrimary">#0A0E27</Color>
<Color x:Key="BackgroundSecondary">#1A1F3A</Color>
<Color x:Key="CardBackground">#242A45</Color>
<Color x:Key="TextPrimary">#E8E9ED</Color>
<Color x:Key="TextSecondary">#A0A5B3</Color>
<Color x:Key="PrimaryBrand">#6366F1</Color>
<Color x:Key="AccentGreen">#10B981</Color>
<Color x:Key="ErrorRed">#EF4444</Color>
```

### 5.2 平台特定UI组件

#### iOS - 毛玻璃效果
```xaml
<Grid BackgroundColor="Transparent">
	<GraphicsView x:Name="GlassmorphismView"
				  CornerRadius="20"/>
</Grid>
```

#### Android - Material Design 3
```xaml
<Grid StyleClass="material3-surface">
	<!-- Material Design 3组件 -->
</Grid>
```

#### Windows - WinUI 3 风格
```xaml
<Grid Background="Transparent">
	<!-- Acrylic background -->
</Grid>
```

### 5.3 深色模式

MAUI原生支持系统深色模式：
```csharp
// App.xaml.cs
public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		// 跟随系统主题
		UserAppTheme = AppTheme.Dark; // 或 Unspecified
	}
}
```

---

## 六、数据库设计 (SQLite + EF Core)

### 6.1 Entity 模型

```csharp
public class Episode
{
	[Key]
	public string Id { get; set; } = Guid.NewGuid().ToString();

	public string Title { get; set; } = "";
	public string Description { get; set; } = "";
	public string SourceUrl { get; set; } = "";
	public DateTime PublishedDate { get; set; }
	public TimeSpan Duration { get; set; }
	public string CoverUrl { get; set; } = "";

	// 关系
	public string PodcastId { get; set; } = "";
	public virtual Podcast? Podcast { get; set; }

	public virtual ICollection<Subtitle> Subtitles { get; set; } = [];
	public virtual ICollection<Download> Downloads { get; set; } = [];
	public virtual ICollection<LearningRecord> LearningRecords { get; set; } = [];
}

public class Subtitle
{
	[Key]
	public int Id { get; set; }

	public string EpisodeId { get; set; } = "";
	public virtual Episode? Episode { get; set; }

	public TimeSpan StartTime { get; set; }
	public TimeSpan EndTime { get; set; }
	public string EnglishText { get; set; } = "";
	public string ChineseText { get; set; } = "";

	public virtual ICollection<VocabularyMention> Vocabulary { get; set; } = [];
}

public class Vocabulary
{
	[Key]
	public string Word { get; set; } = "";

	public string PartOfSpeech { get; set; } = "";
	public string Definition { get; set; } = "";
	public string ChineseTranslation { get; set; } = "";
	public string Phonetic { get; set; } = "";
	public string WordRoot { get; set; } = "";

	public DifficultyLevel Difficulty { get; set; } // High/Medium/Low

	public virtual ICollection<VocabularyMention> Mentions { get; set; } = [];
}

public class Download
{
	[Key]
	public string Id { get; set; } = Guid.NewGuid().ToString();

	public string EpisodeId { get; set; } = "";
	public virtual Episode? Episode { get; set; }

	public string LocalPath { get; set; } = "";
	public long TotalBytes { get; set; }
	public long DownloadedBytes { get; set; }

	public DownloadStatus Status { get; set; } // Pending/Downloading/Completed/Failed
	public DateTime CreatedDate { get; set; }
}

public class LearningRecord
{
	[Key]
	public string Id { get; set; } = Guid.NewGuid().ToString();

	public string EpisodeId { get; set; } = "";
	public virtual Episode? Episode { get; set; }

	public TimeSpan ListenedDuration { get; set; }
	public DateTime LastPlayedTime { get; set; }
	public int PlayCount { get; set; }
}
```

### 6.2 DbContext

```csharp
public class AppDbContext : DbContext
{
	public DbSet<Episode> Episodes { get; set; }
	public DbSet<Podcast> Podcasts { get; set; }
	public DbSet<Subtitle> Subtitles { get; set; }
	public DbSet<Vocabulary> Vocabulary { get; set; }
	public DbSet<Download> Downloads { get; set; }
	public DbSet<LearningRecord> LearningRecords { get; set; }

	protected override void OnConfiguring(DbContextOptionsBuilder options)
	{
		options.UseSqlite($"Filename={GetDbPath()}");
	}

	private string GetDbPath()
	{
		var dbPath = Path.Combine(
			FileSystem.AppDataDirectory,
			"lingoWay.db"
		);
		return dbPath;
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// 配置关系
		modelBuilder.Entity<Episode>()
			.HasMany(e => e.Subtitles)
			.WithOne(s => s.Episode);

		modelBuilder.Entity<Podcast>()
			.HasMany(p => p.Episodes)
			.WithOne(e => e.Podcast);
	}
}
```

---

## 七、RSS聚合实现

### 7.1 RSS源列表

```json
{
  "sources": [
	{
	  "name": "VOA慢速英语",
	  "url": "https://www.voaspecialenglish.com/rss",
	  "type": "rss",
	  "language": "en",
	  "category": "learning"
	},
	{
	  "name": "BBC Learning English",
	  "url": "https://www.bbc.co.uk/learningenglish/feed",
	  "type": "rss",
	  "language": "en",
	  "category": "learning"
	}
  ]
}
```

### 7.2 RSS Parser

```csharp
public class RssParser
{
	public async Task<List<Episode>> ParseAsync(string rssUrl)
	{
		using var client = new HttpClient();
		var content = await client.GetStringAsync(rssUrl);

		var doc = XDocument.Parse(content);
		var items = doc.Descendants("item");

		return items.Select(item => new Episode
		{
			Title = item.Element("title")?.Value ?? "",
			Description = item.Element("description")?.Value ?? "",
			SourceUrl = item.Element("link")?.Value ?? "",
			PublishedDate = DateTime.Parse(item.Element("pubDate")?.Value ?? ""),
			Duration = ParseDuration(item.Element("duration")?.Value)
		}).ToList();
	}
}
```

---

## 八、性能优化策略

### 8.1 启动优化
- ✅ 延迟加载：先展示主页面，后台加载数据
- ✅ 资源预加载：缓存常用数据
- ✅ 代码优化：移除不必要的初始化

### 8.2 内存优化
- ✅ 虚拟滚动 (ItemsView with VirtualizingStackLayout)
- ✅ 图片缓存 + 压缩
- ✅ 流式播放（不加载完整文件）

### 8.3 网络优化
- ✅ HTTP缓存头 (Cache-Control)
- ✅ 请求合并 + 速率限制
- ✅ 压缩传输 (Gzip)

---

## 九、多版本iOS支持

**当前状态**: iOS 13+ 使用MAUI统一代码

**iOS 6兼容版本**: 暂不推荐
- MAUI最低支持iOS 13
- iOS 6已停止维护 (2014年)
- 建议用户升级或使用网页版

---

## 十、参考资源

- [MAUI官方文档](https://learn.microsoft.com/dotnet/maui/)
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/MVVM-Samples)
- [Entity Framework Core文档](https://learn.microsoft.com/ef/core/)
- [ExoPlayer Android](https://exoplayer.dev/)
- [AVFoundation iOS](https://developer.apple.com/av-foundation/)

---

*技术指南 v1.0 | 2025-01*
