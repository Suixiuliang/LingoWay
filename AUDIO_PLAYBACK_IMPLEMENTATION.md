# LingoWay 音频播放和 LRC 字幕功能实现总结

## 📋 项目概述

成功在 LingoWay .NET MAUI 项目中实现了以下核心功能：

### ✅ 已完成的功能

#### 1. **本地音频播放（Windows 平台）**
   - 使用 Windows MediaPlayer API
   - 支持 MP3、WAV 等常见格式
   - 播放/暂停/停止/定位控制
   - 播放速率调整（0.5x - 2.0x）
   - 位置同步事件

#### 2. **LRC 格式字幕解析**
   - 完整的 LRC 文件解析
   - 自动分离英文和中文
   - 时间戳同步（精确到毫秒）
   - 单词级别分解
   - 支持多行时间戳

#### 3. **播放时实时字幕同步**
   - 根据播放位置自动更新当前字幕
   - 英文和中文双语显示
   - 当前句子高亮

#### 4. **单词交互功能**
   - 单词级别的交互（点击、按下）
   - 单词悬停高亮效果
   - 点击添加单词到生词本
   - 生词本显示列表

#### 5. **数据库支持**
   - LRC 行和单词的持久化存储
   - 播放状态记录
   - SQLite 数据库集成

---

## 📁 项目文件结构

### 新增文件

```
Application/Services/
├── AudioPlaybackService.cs          # 音频播放服务接口和默认实现
├── LrcParserService.cs              # LRC 文件解析服务
├── DebugHelper.cs                   # 测试数据生成和调试助手
└── Windows/
	└── WindowsAudioPlaybackService.cs   # Windows 平台实现

Domain/Models/
└── DomainModels.cs                  # 扩展：LrcLine、LrcWord、PlaybackState 模型

Platforms/Windows/Services/
└── WindowsAudioPlaybackService.cs   # Windows 媒体播放实现
```

### 修改的文件

```
Presentation/Views/
├── PlayerPage.xaml                  # 更新 UI：添加导入、字幕、单词展示区域
└── PlayerPage.xaml.cs               # 实现事件处理和交互逻辑

Presentation/ViewModels/
└── PageViewModels.cs                # 重写 PlayerViewModel 支持 LRC 和本地音频

Infrastructure/Database/
└── AppDbContext.cs                  # 添加 LrcLine、LrcWord、PlaybackState 表

MauiProgram.cs                       # 注册音频服务，平台特定实现
```

---

## 🔧 核心类和接口

### IAudioPlaybackService 接口
```csharp
public interface IAudioPlaybackService
{
	// 核心方法
	Task LoadAudioAsync(string audioPath);
	Task PlayAsync();
	Task PauseAsync();
	Task StopAsync();
	Task SeekAsync(TimeSpan position);
	void SetPlaybackRate(float rate);

	// 属性
	TimeSpan CurrentPosition { get; }
	TimeSpan Duration { get; }
	PlaybackStateEnum CurrentPlaybackState { get; }

	// 事件
	event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;
	event EventHandler<PlaybackPositionChangedEventArgs>? PositionChanged;
	event EventHandler<PlaybackErrorEventArgs>? PlaybackError;
	event EventHandler? PlaybackCompleted;
}
```

### LrcParserService 类
```csharp
public class LrcParserService
{
	// 解析 LRC 文件内容
	List<LrcLine> ParseLrc(string content, string episodeId);

	// 获取当前播放行
	LrcLine? GetCurrentLine(List<LrcLine> lines, TimeSpan currentTime);

	// 从文件加载
	Task<List<LrcLine>> LoadLrcFileAsync(string filePath, string episodeId);
}
```

### PlayerViewModel 扩展功能
```csharp
public partial class PlayerViewModel : BaseViewModel
{
	// 新增属性
	public List<LrcLine> LrcLines { get; set; }
	public LrcLine? CurrentLrcLine { get; set; }
	public PlaybackStateEnum CurrentPlaybackState { get; set; }
	public TimeSpan TotalDuration { get; set; }
	public bool IsUserSeeking { get; set; }

	// 新增方法
	Task LoadAudioAndSubtitleAsync(string audioPath, string? subtitlePath);
	Task TogglePlayPauseAsync();
	Task SkipForwardAsync();
	Task SkipBackwardAsync();
	Task SeekAsync(TimeSpan position);
	void SetPlaybackRate(float rate);
	Task AddWordToVocabularyAsync(string word);

	// 新增事件
	event EventHandler? LrcLinesUpdated;
	event EventHandler<LrcLine?>? CurrentLineChanged;
	event EventHandler? PlaybackPositionChanged;
	event EventHandler? PlaybackStateChanged;
}
```

---

## 📝 使用示例

### 1. 在 PlayerPage 中导入音频和字幕

用户点击"导入音频和字幕"按钮，会弹出文件选择器：

```csharp
// PlayerPage.xaml.cs
private async void OnImportFilesClicked(object sender, EventArgs e)
{
	var audioResult = await FilePicker.PickAsync();  // 选择音频
	var subtitleResult = await FilePicker.PickAsync();  // 选择 LRC

	await _viewModel.LoadAudioAndSubtitleAsync(
		audioResult.FullPath, 
		subtitleResult?.FullPath);
}
```

### 2. 播放音频
```csharp
await _viewModel.TogglePlayPauseAsync();  // 切换播放/暂停
await _viewModel.SkipForwardAsync();      // 快进 10 秒
await _viewModel.SkipBackwardAsync();     // 快退 10 秒
```

### 3. 调整播放速度
```csharp
_viewModel.SetPlaybackRate(1.5f);  // 1.5 倍速
```

### 4. 添加单词到生词本
```csharp
await _viewModel.AddWordToVocabularyAsync("example");
```

---

## 🧪 测试和调试

### 使用 DebugHelper 生成测试数据

```csharp
// 生成测试 LRC 文件
var lrcPath = await DebugHelper.CreateTestLrcFileAsync();

// 生成测试音频文件
var audioPath = await DebugHelper.CreateTestAudioFileAsync();

// 记录调试信息
DebugHelper.LogDebugInfo("测试消息");

// 获取所有测试文件
var testFiles = DebugHelper.GetTestFiles();

// 清理测试文件
DebugHelper.CleanupTestFiles();
```

### 示例 LRC 文件格式
```
[00:00.00]Welcome to LingoWay
[00:00.00]欢迎来到 LingoWay
[00:05.50]This is a test episode
[00:05.50]这是一个测试剧集
[00:10.00]Learn English words step by step
[00:10.00]一步步学习英文单词
```

---

## 🚀 Windows 平台特定实现

### WindowsAudioPlaybackService

使用 Windows Runtime 的 `MediaPlayer` 和 `MediaSource`：

```csharp
public class WindowsAudioPlaybackService : IAudioPlaybackService
{
	private MediaPlayer? _mediaPlayer;

	private void InitializeMediaPlayer()
	{
		_mediaPlayer = new MediaPlayer { AutoPlay = false };
		_mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
		_mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
		// ...
	}

	public async Task LoadAudioAsync(string audioPath)
	{
		var mediaSource = MediaSource.CreateFromUri(new Uri($"file:///{audioPath}"));
		_mediaPlayer?.Source = mediaSource;
	}
}
```

---

## 📊 数据模型

### LrcLine（LRC 行）
```csharp
public class LrcLine
{
	public int Id { get; set; }
	public string EpisodeId { get; set; }
	public TimeSpan StartTime { get; set; }
	public TimeSpan? EndTime { get; set; }
	public string EnglishText { get; set; }
	public string ChineseText { get; set; }
	public ICollection<LrcWord> Words { get; set; }  // 单词列表
	public int LineNumber { get; set; }
}
```

### LrcWord（LRC 中的单词）
```csharp
public class LrcWord
{
	public int Id { get; set; }
	public int LrcLineId { get; set; }
	public string Word { get; set; }
	public int PositionInLine { get; set; }
	public string? VocabularyWord { get; set; }  // 关联到生词表
}
```

### PlaybackState（播放状态）
```csharp
public class PlaybackState
{
	public string Id { get; set; }
	public string EpisodeId { get; set; }
	public TimeSpan CurrentPosition { get; set; }
	public int? CurrentLrcLineId { get; set; }
	public int? CurrentHighlightedWordId { get; set; }
	public DateTime LastUpdatedTime { get; set; }
}
```

---

## ✨ 主要特性

### 1. **跨平台设计**
- 接口驱动的架构
- Windows 平台特定实现
- 易于扩展到其他平台（Android、iOS）

### 2. **实时同步**
- 播放位置自动更新字幕
- 字幕行和单词完全同步
- 毫秒级精度

### 3. **用户交互**
- 直观的播放控制
- 单词级别的学习功能
- 生词本快速添加

### 4. **数据持久化**
- SQLite 数据库存储
- 播放进度保存
- 生词本管理

---

## 🔜 后续扩展方向

### 短期计划
1. **更完善的 UI**
   - 美化字幕显示区域
   - 添加播放进度条拖动
   - 音量控制

2. **功能增强**
   - 支持字幕调整（时间偏移）
   - 支持多语言字幕
   - 词频统计

### 中期计划
1. **跨平台支持**
   - Android 音频播放
   - iOS 音频播放
   - 平台统一 API

2. **高级学习功能**
   - 发音检查（使用 Speech API）
   - 单词复习算法（SRS）
   - 学习统计和报告

### 长期计划
1. **云同步**
   - 学习进度云备份
   - 生词本云同步
   - 跨设备学习

2. **AI 增强**
   - 自动字幕生成
   - 发音纠正
   - 个性化学习路径

---

## 📌 注意事项

### 1. 权限要求（Windows）
- 文件访问权限
- 媒体播放权限

### 2. 音频格式支持
- 当前支持：MP3、WAV
- 可扩展支持：M4A、OGG 等

### 3. LRC 文件规范
- 标准 LRC 格式：`[mm:ss.ms]内容`
- 自动识别英文/中文
- 支持多行时间戳

### 4. 性能优化
- 大型 LRC 文件的加载优化
- 音频流式播放
- 内存管理

---

## 🔗 相关资源

### 参考文档
- [MAUI 官方文档](https://learn.microsoft.com/maui)
- [Windows MediaPlayer API](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/)
- [LRC 格式规范](https://en.wikipedia.org/wiki/LRC_(file_format))

### 测试文件
使用 `DebugHelper` 类生成的测试数据位于应用数据目录：
```
C:\Users\{username}\AppData\Local\Packages\{AppId}\LocalState\
```

---

## 💡 开发者贡献指南

### 添加新的播放功能

1. **在 PlayerViewModel 中添加方法**
```csharp
[RelayCommand]
public async Task MyNewFeatureAsync() { ... }
```

2. **在 PlayerPage.xaml 中添加 UI 元素**
```xaml
<Button Text="功能" Clicked="OnMyFeatureClicked"/>
```

3. **在 PlayerPage.xaml.cs 中处理事件**
```csharp
private async void OnMyFeatureClicked(object sender, EventArgs e)
{
	await _viewModel.MyNewFeatureAsync();
}
```

---

## 📞 支持和反馈

如有任何问题或建议，请提交 Issue 或 Pull Request。

---

**最后更新**：2024年
**项目版本**：1.0.0
**维护者**：LingoWay Team
