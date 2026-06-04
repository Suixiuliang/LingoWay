# 新功能快速参考

## 设置界面（SettingsPage）

### 可配置项
| 类别 | 项目 | 类型 | 范围 | 默认值 |
|------|------|------|------|--------|
| 播放 | 播放速率 | 滑块 | 0.5x - 2.0x | 1.0x |
| 播放 | 背景播放 | 开关 | - | 开 |
| 字幕 | 启用字幕 | 开关 | - | 开 |
| 字幕 | 字幕语言 | 选择器 | 5种语言 | English |
| 字幕 | 字幕字号 | 滑块 | 10pt - 32pt | 16pt |
| 下载 | 仅WiFi下载 | 开关 | - | 关 |
| 主题 | 主题选择 | 按钮组 | 浅色/深色/系统 | - |

### 命令
- `SaveSettingsCommand` - 保存当前设置
- `ResetToDefaultCommand` - 恢复所有设置到默认值

---

## 单词本（VocabularyPage）

### 功能
- 📚 **查看单词**: 显示已学习的单词列表
- 🔍 **搜索**: 按单词或定义搜索
- 🗑️ **删除**: 从单词本中删除单词
- 📖 **复习**: 打开单词复习界面（待实现）

### 单词卡片信息
```
┌─────────────────────────────┐
│ WORD                  难度   │
│ /pə'nɛtɪk/                  │
│ 释义内容                    │
│ 中文翻译（斜体）            │
└─────────────────────────────┘
```

### 数据来源
```csharp
IVocabularyService.GetUserVocabularyAsync()
// 返回 List<Vocabulary>
```

---

## 导航结构

```
┌─ TabBar ──────────────────────────────┐
│                                       │
│ 发现更多 │ 我的收藏 │ 播放中 │ 单词本 │ 设置 │
│                                       │
└───────────────────────────────────────┘
```

---

## 代码示例

### 访问设置值
```csharp
var viewModel = new SettingsViewModel();
float playbackRate = viewModel.PlaybackRate;
bool isWifiOnly = viewModel.IsWifiOnlyDownloadEnabled;
```

### 加载单词本
```csharp
var vocabViewModel = new VocabularyViewModel(vocabularyService);
await vocabViewModel.LoadVocabularyAsync();
```

### 搜索单词
```csharp
vocabViewModel.SearchText = "hello";
await vocabViewModel.SearchCommand.ExecuteAsync(null);
```

---

## 主要 ViewModel 属性

### SettingsViewModel
```csharp
[ObservableProperty]
private float playbackRate = 1.0f;

[ObservableProperty]
private bool isBackgroundPlayEnabled = true;

[ObservableProperty]
private bool isSubtitleEnabled = true;

[ObservableProperty]
private string subtitleLanguage = "en";

[ObservableProperty]
private List<string> subtitleLanguages;

[ObservableProperty]
private int subtitleFontSize = 16;

[ObservableProperty]
private bool isWifiOnlyDownloadEnabled = false;
```

### VocabularyViewModel
```csharp
[ObservableProperty]
private List<Vocabulary> vocabularyList = [];

[ObservableProperty]
private string searchText = "";
```

---

## 关键方法

### SettingsViewModel
- `SaveSettingsAsync()` - 异步保存设置
- `ResetToDefaultAsync()` - 恢复默认值（带确认对话）

### VocabularyViewModel
- `LoadVocabularyAsync()` - 加载用户单词
- `SearchAsync()` - 搜索单词
- `ClearSearchAsync()` - 清除搜索结果
- `SelectVocabularyAsync(Vocabulary item)` - 处理单词操作

---

## 样式和颜色

### 使用的动态资源
- `BackgroundPrimary` - 主背景色
- `TextPrimary` - 主文本色
- `TextSecondary` - 次文本色
- `CardBackground` - 卡片背景
- `PrimaryBrand` - 品牌色（按钮）
- `AccentGreen` - 强调色（成功）
- `ErrorRed` - 错误色

### Frame 样式
```xaml
<Frame BorderColor="#E0E0E0" 
	   CornerRadius="10" 
	   Padding="15" 
	   HasShadow="True">
```

---

## 常见问题

**Q: 如何添加更多字幕语言？**
A: 在 SettingsViewModel 中更新 `subtitleLanguages` 列表

**Q: 单词本数据如何持久化？**
A: 需要实现本地数据库存储，推荐使用 SQLite

**Q: 如何自定义设置界面的分组？**
A: 修改 SettingsPage.xaml 中的 `<VerticalStackLayout>` 分组

**Q: 搜索支持哪些字段？**
A: 目前支持单词（Word）和定义（Definition），可扩展到其他字段
