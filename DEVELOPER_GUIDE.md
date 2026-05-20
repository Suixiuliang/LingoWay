# LingoWay 开发者指南

## 项目初始化检查清单

### ✅ 已完成的工作

#### 1. Domain 层
- [x] Models (Episode、Podcast、Subtitle、Vocabulary等)
- [x] Interfaces (Service接口)
- [x] Constants (AppConstants、ColorConstants、ApiConstants)

#### 2. Infrastructure 层
- [x] Database (AppDbContext、EF Core配置)
- [x] Repositories (所有Entity的CRUD)
- [x] Http (RssParser、ContentClient、TranslationClient)
- [x] Storage (文件和缓存管理)

#### 3. Application 层
- [x] Services (ContentProvider、SearchService、PlaybackService等)
- [x] DownloadService、LearningService、VocabularyService

#### 4. Presentation 层
- [x] ViewModels (PlayerViewModel、BrowseViewModel等)
- [x] Pages (MainPage、PlayerPage、BrowsePage等)
- [x] Converters (值转换器)
- [x] Components (EpisodeCard)

#### 5. Configuration
- [x] MauiProgram 依赖注入
- [x] AppShell 导航
- [x] App.xaml 全局资源
- [x] 项目文件 (NuGet包引用)

---

## 下一步工作

### 🔨 立即需要做的

1. **编译项目验证**
```bash
cd D:\Administrator\Documents\GitHub\LingoWay\
dotnet build
```

2. **解决编译错误**
   - 检查命名空间导入
   - 验证NuGet包版本
   - 修复XAML绑定错误

3. **数据库初始化**
   - 首次启动时自动创建SQLite数据库
   - 导入初始数据 (词汇库、播客列表)

### 📱 平台特定工作

#### Android
- [ ] 配置AndroidManifest.xml权限 (INTERNET、READ_EXTERNAL_STORAGE)
- [ ] 实现ExoPlayer音频播放
- [ ] 测试低端设备 (3GB RAM)

#### iOS
- [ ] 配置Info.plist (Privacy、Capabilities)
- [ ] 实现AVPlayer音频播放
- [ ] 配置毛玻璃效果 (iOS 16+)

#### Windows
- [ ] 配置包清单
- [ ] 测试WinUI 3风格
- [ ] 实现MediaPlayerElement

#### macOS
- [ ] 配置Entitlements
- [ ] 实现Mac Catalyst支持
- [ ] 测试触控板手势

### 🎨 UI完善

1. **设计细节**
   - [ ] 实现平台特定的导航栏样式
   - [ ] 添加Loading动画
   - [ ] 优化列表虚拟滚动

2. **响应式布局**
   - [ ] 平板优化 (横屏模式)
   - [ ] 不同屏幕尺寸适配
   - [ ] 字体大小响应式

### 🔌 集成第三方服务

1. **RSS源测试**
   - [ ] VOA解析验证
   - [ ] BBC解析验证
   - [ ] 错误处理

2. **翻译API**
   - [ ] 腾讯翻君集成
   - [ ] Azure Translator集成
   - [ ] 本地翻译缓存

3. **字幕生成**
   - [ ] Whisper.NET集成
   - [ ] 模型下载机制
   - [ ] 进度回调

---

## 文件修改速查

### 快速修改列表

| 目的 | 文件 | 说明 |
|------|------|------|
| 添加新ViewModel | `Presentation/ViewModels/*.cs` | 继承BaseViewModel |
| 添加新Page | `Presentation/Views/*.xaml` | 在AppShell中注册 |
| 添加Service | `Application/Services/*.cs` | 在MauiProgram中注册 |
| 添加Model | `Domain/Models/DomainModels.cs` | 在DbContext中配置 |
| 调整颜色 | `Domain/Constants/ColorConstants.cs` | 或Resources/Styles/Colors.xaml |
| 数据库操作 | `Infrastructure/Repositories/*.cs` | 扩展BaseRepository |

---

## 常见开发任务

### 1. 添加新功能页面

```csharp
// 1. 创建ViewModel
public partial class NewFeatureViewModel : BaseViewModel
{
	[ObservableProperty]
	private string title = "";

	[RelayCommand]
	public async Task LoadDataAsync() { }
}

// 2. 创建XAML页面
// Presentation/Views/NewFeaturePage.xaml

// 3. 在AppShell中注册
// 在MauiProgram.cs中添加
.AddSingleton<NewFeatureViewModel>()
.AddSingleton<NewFeaturePage>()

// 4. 在AppShell.xaml中添加标签
<ShellContent Title="新功能" 
			 ContentTemplate="{DataTemplate views:NewFeaturePage}" />
```

### 2. 添加新的数据模型

```csharp
// 1. 在Domain/Models中定义
public class NewModel
{
	public string Id { get; set; }
	// 属性...
}

// 2. 添加到DbContext
public DbSet<NewModel> NewModels { get; set; }

// 3. 创建Repository
public class NewModelRepository : BaseRepository<NewModel> { }

// 4. 配置关系和索引
modelBuilder.Entity<NewModel>()
	.HasKey(m => m.Id);
```

### 3. 调用Service

```csharp
// 在ViewModel中注入
public class MyViewModel : BaseViewModel
{
	private readonly IPlaybackService playbackService;

	public MyViewModel(IPlaybackService playbackService)
	{
		this.playbackService = playbackService;
	}

	[RelayCommand]
	public async Task PlayAsync(Episode episode)
	{
		await playbackService.PlayAsync(episode);
	}
}
```

---

## 调试技巧

### Visual Studio调试

```csharp
// 在需要的地方打断点
Debug.WriteLine($"Debug: {value}");

// 条件断点
if (value > 10)
{
	System.Diagnostics.Debugger.Break();
}
```

### 日志输出

```csharp
// 通过依赖注入获取Logger (如需要)
ILogger<ClassName> logger;

// 输出日志
System.Diagnostics.Debug.WriteLine("消息");
```

### 平台特定调试

**Android**:
```bash
adb logcat | grep LingoWay
```

**iOS**:
使用 Xcode 控制台

**Windows**:
Visual Studio 输出窗口

---

## 性能优化检查清单

- [ ] 使用CollectionView代替ListView (虚拟滚动)
- [ ] 延迟加载 (LazyLoading)
- [ ] 缓存网络响应
- [ ] 图片缓存和压缩
- [ ] 异步操作 (不阻塞UI线程)
- [ ] 数据库索引优化

---

## 编码规范

### 命名约定

```csharp
// ViewModel - xxxViewModel
public class PlayerViewModel { }

// Service - IxxxService / xxxService
public interface IPlaybackService { }
public class PlaybackService { }

// Repository - xxxRepository
public class EpisodeRepository { }

// Model - 单数形式
public class Episode { }
public class Podcast { }

// 常量 - PascalCase
public const string AppName = "LingoWay";
```

### MVVM 模式

```csharp
// 属性使用[ObservableProperty]
[ObservableProperty]
private string title = "";

// 命令使用[RelayCommand]
[RelayCommand]
public async Task LoadAsync() { }
```

### 异步编程

```csharp
// 总是使用async/await
public async Task DoSomethingAsync()
{
	var result = await GetDataAsync();
	return result;
}

// 不要使用.Result (会导致死锁)
// ❌ var result = GetDataAsync().Result;
// ✅ var result = await GetDataAsync();
```

---

## 故障排除

### 常见问题

**问题**: NuGet包还原失败
```bash
# 解决方案
dotnet nuget locals all --clear
dotnet restore
```

**问题**: 编译错误 "找不到命名空间"
```csharp
// 确保添加了using
using LingoWay.Domain.Models;
using LingoWay.Application.Services;
```

**问题**: XAML绑定失败
```xaml
<!-- 检查BindingContext -->
<ContentPage.BindingContext>
	<local:PlayerViewModel />
</ContentPage.BindingContext>

<!-- 或在Code-Behind设置 -->
BindingContext = viewModel;
```

**问题**: 数据库操作异常
```csharp
try
{
	await dbContext.SaveChangesAsync();
}
catch (DbUpdateException ex)
{
	Debug.WriteLine($"数据库错误: {ex.Message}");
}
```

---

## 项目发展路线图

### Phase 1 (当前) - MVP
- [x] 基础架构搭建
- [x] 核心Models和Services
- [ ] 编译验证
- [ ] 基础功能测试

### Phase 2 - 功能完善 (2-4周)
- [ ] 平台特定代码实现
- [ ] 播放器功能测试
- [ ] 字幕显示优化
- [ ] 词汇识别完善

### Phase 3 - 优化与发布 (4-6周)
- [ ] 性能优化
- [ ] UI/UX打磨
- [ ] 多语言支持
- [ ] App Store/Play Store上架

---

## 资源和参考

- [MAUI官方文档](https://learn.microsoft.com/dotnet/maui/)
- [CommunityToolkit.Mvvm文档](https://github.com/CommunityToolkit/MVVM-Samples)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- [.NET 10文档](https://learn.microsoft.com/dotnet/)

---

**更新日期**: 2025-01  
**版本**: 1.0.0-alpha
