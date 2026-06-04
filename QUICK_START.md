# 快速开始指南 - 音频播放和 LRC 字幕功能

## 🎯 快速开始（5 分钟）

### 1. 准备测试文件

打开 PlayerPage 代码，在初始化时生成测试数据：

```csharp
// 在 PlayerPage.xaml.cs 的 OnAppearing 中添加
protected override void OnAppearing()
{
	base.OnAppearing();

	// 生成测试文件（仅在 DEBUG 模式）
#if DEBUG
	_ = GenerateTestFiles();
#endif
}

private async Task GenerateTestFiles()
{
	try
	{
		var lrcPath = await DebugHelper.CreateTestLrcFileAsync();
		var audioPath = await DebugHelper.CreateTestAudioFileAsync();

		System.Diagnostics.Debug.WriteLine($"Test LRC: {lrcPath}");
		System.Diagnostics.Debug.WriteLine($"Test Audio: {audioPath}");
	}
	catch (Exception ex)
	{
		System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
	}
}
```

### 2. 导入音频和字幕

1. 在 PlayerPage 中点击"导入音频和字幕"按钮
2. 选择生成的测试音频文件（或您自己的 MP3/WAV）
3. 选择生成的测试 LRC 字幕文件（或您自己的 LRC）
4. 应用会自动加载并开始显示字幕

### 3. 播放和交互

- **播放/暂停**：点击中间的播放按钮（⏯）
- **快进/快退**：点击前后按钮
- **调整速度**：点击 1.0x、1.5x 等按钮
- **拖动进度**：滑动进度条
- **添加单词**：点击字幕中的任何单词

---

## 📚 完整使用示例

### 从文件系统加载音频和字幕

```csharp
// 从 FilePicker 获取路径后
var audioPath = "/path/to/audio.mp3";
var subtitlePath = "/path/to/subtitle.lrc";

await playerViewModel.LoadAudioAndSubtitleAsync(audioPath, subtitlePath);
```

### 监听播放事件

```csharp
// 在 ViewModel 中
_viewModel.PlaybackStateChanged += (s, e) => 
{
	Debug.WriteLine($"State: {_viewModel.CurrentPlaybackState}");
};

_viewModel.CurrentLineChanged += (s, line) => 
{
	Debug.WriteLine($"Current: {line?.EnglishText}");
};

_viewModel.PlaybackPositionChanged += (s, e) => 
{
	Debug.WriteLine($"Position: {_viewModel.CurrentPosition}");
};
```

### 手动控制播放

```csharp
// 开始播放
await playerViewModel.TogglePlayPauseAsync();

// 跳到特定位置
await playerViewModel.SeekAsync(TimeSpan.FromSeconds(30));

// 调整速度
playerViewModel.SetPlaybackRate(1.5f);

// 添加单词
await playerViewModel.AddWordToVocabularyAsync("example");
```

---

## 🛠️ 常见任务

### 任务 1：自定义 LRC 文件格式

编辑 `LrcParserService.cs` 中的 `ParseLrc` 方法：

```csharp
// 当前支持的格式：
// [00:12.50]English text
// [00:12.50]中文翻译

// 如果需要修改格式（例如支持歌词标签），修改正则表达式：
var lrcRegex = new Regex(@"^\[(\d{2}):(\d{2})\.(\d{2})\](.*)$", RegexOptions.Multiline);
```

### 任务 2：修改字幕显示样式

在 `PlayerPage.xaml` 中编辑相关样式：

```xaml
<!-- 修改英文标签的字体大小和颜色 -->
<Label x:Name="EnglishTextLabel"
	   FontSize="20"  <!-- 修改这里 -->
	   TextColor="YourColor"/>
```

### 任务 3：添加更多播放控制按钮

1. 在 `PlayerPage.xaml` 中添加按钮
2. 在 `PlayerPage.xaml.cs` 中添加事件处理器
3. 在 `PlayerViewModel` 中添加相应方法

例如，添加音量控制：

```xaml
<Button Text="🔊" Clicked="OnVolumeClicked"/>
```

```csharp
private void OnVolumeClicked(object sender, EventArgs e)
{
	// 实现音量控制
}
```

### 任务 4：修改单词高亮颜色

在 `PlayerPage.xaml.cs` 的 `UpdateWordButtons` 方法中：

```csharp
var brandColor = (Color)(MauiApp.Current?.Resources["PrimaryBrand"] ?? Colors.Blue);
// 修改 Colors.Blue 为您想要的颜色
```

---

## 🐛 故障排除

### 问题 1：音频文件无法加载

**症状**：点击播放后没有反应

**解决方案**：
1. 检查文件路径是否正确
2. 确保文件格式受支持（MP3、WAV）
3. 查看调试输出中的错误消息

```csharp
System.Diagnostics.Debug.WriteLine($"Loading: {audioPath}");
```

### 问题 2：字幕显示不正确

**症状**：字幕为空或时间不同步

**解决方案**：
1. 验证 LRC 文件格式
2. 检查时间戳是否正确
3. 使用 LRC 编辑器验证文件

```text
正确格式：[mm:ss.ms]内容
错误格式：[m:ss]内容 或 [hh:mm:ss]内容
```

### 问题 3：单词添加失败

**症状**：点击单词后没有反应

**解决方案**：
1. 检查是否已加载 LRC 文件
2. 验证单词解析是否正确
3. 检查数据库连接

```csharp
Debug.WriteLine($"Words: {currentLine?.Words?.Count ?? 0}");
```

---

## 📱 平台特定配置

### Windows

需要在 Package.appxmanifest 中声明以下功能：
- 文件系统访问
- 媒体播放

```xml
<Capabilities>
	<Capability Name="documentsLibrary"/>
	<Capability Name="musicLibrary"/>
</Capabilities>
```

### Android（未来支持）

需要在 AndroidManifest.xml 中添加：
```xml
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE"/>
```

### iOS（未来支持）

需要在 Info.plist 中添加：
```xml
<key>NSMediaLibraryUsageDescription</key>
<string>允许访问音乐库</string>
```

---

## 📊 调试技巧

### 启用详细日志

```csharp
System.Diagnostics.Debug.WriteLine($"[LingoWay.Debug] Message");
```

### 检查 LRC 解析结果

```csharp
var lrcLines = await lrcParserService.LoadLrcFileAsync(path, episodeId);
Debug.WriteLine($"Loaded {lrcLines.Count} lines");

foreach (var line in lrcLines)
{
	Debug.WriteLine($"[{line.StartTime}] {line.EnglishText}");
}
```

### 监控播放进度

```csharp
// 在 ViewModel 中添加
Debug.WriteLine($"Position: {CurrentPosition}, Duration: {TotalDuration}");
Debug.WriteLine($"Current Line: {CurrentLrcLine?.EnglishText}");
```

---

## 📈 性能优化建议

### 1. 大型 LRC 文件处理

```csharp
// 分批加载而不是一次性加载
const int batchSize = 100;
for (int i = 0; i < lines.Count; i += batchSize)
{
	var batch = lines.Skip(i).Take(batchSize);
	await SaveToDatabase(batch);
}
```

### 2. 内存管理

```csharp
// 及时释放资源
protected override void OnDisappearing()
{
	base.OnDisappearing();
	_viewModel?.Cleanup();  // 调用 Cleanup 方法
}
```

### 3. UI 更新优化

```csharp
// 使用 MainThread 确保 UI 操作在主线程上
MainThread.BeginInvokeOnMainThread(() =>
{
	UpdateUI();
});
```

---

## 🚀 下一步

1. **集成到已有的播放器**
   - 在现有播放器页面中集成本功能
   - 保持 UI 一致性

2. **扩展功能**
   - 添加更多字幕格式支持（SRT、ASS）
   - 实现播放列表功能
   - 添加章节标记

3. **优化体验**
   - 改进搜索功能
   - 优化字幕显示
   - 添加快捷键支持

---

## 📞 获取帮助

- 查看 `AUDIO_PLAYBACK_IMPLEMENTATION.md` 获取详细文档
- 查看源代码中的注释
- 运行测试用例
- 提交 Issue

---

**版本**：1.0.0  
**最后更新**：2024年  
**作者**：LingoWay Team
