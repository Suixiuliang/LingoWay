# LingoWay (听途) - .NET 10 MAUI 英语学习应用

欢迎来到 LingoWay 项目！这是一个为中国打工族设计的**跨平台碎片化英语学习应用**，使用.NET 10和MAUI框架开发。

## 🎯 项目目标

- **平台**: Windows 7+、macOS 12.7.6+、Android 9+、iOS 13+
- **用户**: 中国上班族 / 通勤人士
- **核心体验**: 沉浸式、碎片化学习 - "听一听、摸一摸耳朵"
- **设计**: 简约深色模式、单手操作优化

## ✨ 核心功能

### MVP (最小可行产品)
- ✅ **音频/视频播放** - 在线/离线、倍速、进度记忆
- ✅ **内容聚合** - RSS源、播客、本地导入
- ✅ **字幕显示** - 双语字幕、实时翻译
- ✅ **词汇识别** - 难度分级、点击释义
- ✅ **离线下载** - 管理和删除
- ✅ **学习记录** - 进度追踪、统计

### 后续功能
- 🔄 AI驱动的字幕生成 (Whisper)
- 📊 学习统计仪表板
- 🔍 高级搜索和过滤
- 💬 社区笔记和分享
- ☁️ 云同步 (可选)

## 🏗️ 项目架构

```
LingoWay/
├── Domain/              # 业务逻辑 (Models、Interfaces、Constants)
├── Infrastructure/      # 数据访问 (Database、Repositories、HTTP、Storage)
├── Application/         # 应用服务 (Services、DTOs)
├── Presentation/        # UI层 (Views、ViewModels、Converters、Components)
└── Platforms/          # 平台特定代码 (iOS、Android、Windows、macOS)
```

### 技术栈

| 层 | 技术 | 版本 |
|----|----|------|
| 框架 | .NET MAUI | 10.0 |
| MVVM | CommunityToolkit.Mvvm | 8.2.2+ |
| 数据库 | SQLite + EF Core | 10.0.0 |
| 依赖注入 | Microsoft.Extensions.DependencyInjection | 10.0.0 |
| 播放器 | 平台原生 (AVPlayer/ExoPlayer) | 原生 |

## 🚀 快速开始

### 系统要求
- Visual Studio 2026 Community (或更新版本)
- .NET 10 SDK
- 平台SDK (Android、iOS、macOS、Windows)

### 安装步骤

1. **克隆仓库**
```bash
git clone https://github.com/yourusername/LingoWay.git
cd LingoWay
```

2. **恢复NuGet包**
```bash
dotnet restore
```

3. **生成项目**
```bash
dotnet build
```

4. **运行项目**
```bash
# Android
dotnet build -f net10.0-android -c Debug

# iOS
dotnet build -f net10.0-ios -c Debug

# Windows
dotnet build -f net10.0-windows -c Debug

# macOS
dotnet build -f net10.0-maccatalyst -c Debug
```

## 📁 目录结构说明

### Domain 层
- `Models/` - 业务模型 (Episode、Podcast、Vocabulary等)
- `Interfaces/` - 服务接口定义
- `Constants/` - 应用常量

### Infrastructure 层
- `Database/` - EF Core DbContext和迁移
- `Repositories/` - 数据访问对象
- `Http/` - HTTP客户端、RSS解析器
- `Storage/` - 文件和缓存管理
- `Platform/` - 平台特定实现

### Application 层
- `Services/` - 业务逻辑实现
- `DTOs/` - 数据传输对象

### Presentation 层
- `Views/` - XAML页面 (Main、Player、Browse、Download、Favorite、Settings)
- `ViewModels/` - MVVM逻辑
- `Converters/` - 值转换器
- `Components/` - 可复用组件

## 📊 性能目标

| 指标 | 目标 |
|------|------|
| 冷启动时间 | < 2s |
| 内存占用 | < 150MB (低端Android) |
| 首屏渲染 | < 1s |
| 播放卡顿率 | < 0.1% |

## 🧪 测试

```bash
# 运行单元测试
dotnet test

# 特定平台测试
dotnet test -f net10.0-android
```

## 🐛 调试

### Android
```bash
# 使用Android Studio连接模拟器
adb logcat
```

### iOS
```bash
# 使用Xcode调试
```

## 📝 提交规范

```
feat: 添加新功能
fix: 修复bug
docs: 更新文档
style: 代码格式
refactor: 重构
test: 测试
chore: 维护
```

## 🔐 隐私与安全

- ✅ 不收集用户浏览历史
- ✅ 不传输个人数据
- ✅ 本地离线处理
- ✅ 加密敏感数据

## 📄 许可证

MIT License - 详见 LICENSE 文件

## 👥 贡献

欢迎提交Issue和Pull Request！

## 📞 联系方式

- 项目主页: [GitHub/LingoWay](#)
- 文档: [Wiki](#)

---

**最后更新**: 2025-01  
**版本**: 1.0.0-alpha  
**维护者**: LingoWay Team
