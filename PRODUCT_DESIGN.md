# LingoWay (听途) - 英语学习App 产品设计文档

## 一、产品概述

**应用名称**: LingoWay (听途)  
**目标用户**: 中国打工族 (上班族)  
**核心价值**: 碎片化学习英语 - "听一听、摸一摸耳朵" (通勤路上的沉浸式学习)  
**开发框架**: .NET 10 + MAUI  
**支持平台**: Windows 7+、macOS 12.7.6+、Android 9+、iOS 13+  

---

## 二、平台适配策略

### 2.1 多平台覆盖

| 平台 | 最低版本 | 目标版本 | 特殊优化 |
|------|--------|--------|--------|
| Windows | Win7 SP1 | Win11 | WinUI 3 风格、深色模式 |
| macOS | 12.7.6 | Sonoma | Mac Catalyst、毛玻璃效果 |
| Android | 9 (API 28) | 15 | Material Design 3、适配低端设备 |
| iOS | 13 | 18 | 原生控件、液态玻璃 (iOS 16+)、Home Bar适配 |

### 2.2 UI/UX 设计原则

- **简约设计**: 最小化交互步骤，适合单手通勤操作
- **深色模式优先**: 保护眼睛，降低电耗
- **禁止死亡科技蓝**: 采用柔和的蓝灰、深紫、深绿等配色
- **平台原生风格**:
  - iOS: 毛玻璃效果 (BottomSheet)、原生HStack/VStack
  - macOS: Mac Catalyst 原生控件
  - Android: Material Design 3、切角卡片
  - Windows: WinUI 3 丙烯酸效果

---

## 三、核心功能规格 (MVP)

### 3.1 音频/视频播放

#### 功能需求
- ✅ 在线/离线播放
- ✅ 后台播放 (锁屏控制)
- ✅ 倍速播放 (0.5x ~ 2.0x)
- ✅ 进度记忆 (自动保存播放位置)
- ✅ 循环播放 (单曲/列表/AB循环)
- ✅ 均衡器 (可选)

#### 推荐技术方案
```
Windows/macOS/iOS: MediaElement / AVPlayer 封装
Android: ExoPlayer 或 MediaPlayer3
推荐库: CommunityToolkit.MVVM + Platform-specific Implementation
```

---

### 3.2 音频资源获取方案（核心+创新）

#### 方案A: 公开API + RSS聚合 (★★★★★ 推荐)
**合规性**: ✅ 完全合法  
**稳定性**: ✅ 高  
**维护成本**: ✅ 低

##### 1. **VOA慢速英语 - RSS源**
- 源: `https://www.voaspecialenglish.com/api/` (需验证最新端点)
- 实现: RSS解析 + 自动字幕提取
- 优势: 发音标准、难度递进、原汁原味

##### 2. **BBC Learning English - RSS**
- 源: `https://www.bbc.co.uk/learningenglish/` (需提取RSS端点)
- 内容: Pronunciation、Grammar、Business English等
- 优势: 权威、多类型内容

##### 3. **TED演讲 - 官方API**
- 源: `https://www.ted.com/talks` (提供JSON API)
- 实现: 官方API爬取 + 字幕下载
- 优势: 高质量演讲、多主题、自带英文字幕

##### 4. **Podcast聚合源**
- 源: 喜马拉雅、得到、荔枝等国内平台的英语学习Podcast
- 实现: 通过用户授权接入，或爬取公开Podcast RSS
- 优势: 本地化、易获取、多讲师

##### 5. **网易公开课 - 英语专区**
- 源: 网易公开课的英语学习课程
- 实现: 爬取视频元数据 + 字幕
- 优势: 国内用户友好、资源丰富

#### 方案B: 用户导入 (★★★★☆)
**合规性**: ✅ 用户自主，应用中立  
**用途**: 个人收藏、学习资料

##### 实现方式
1. **本地文件导入**
   - 支持格式: MP3、M4A、WAV、MP4、MKV
   - UI: 文件选择器 (MAUI FilePicker)
   - 自动识别: 文件名、元数据、时长

2. **云盘链接**
   - 支持OneDrive、iCloud Drive、Google Drive（VPN）
   - 实现: WebView / REST API集成

3. **字幕导入**
   - 格式支持: SRT、VTT、ASS
   - UI: 拖拽或手动关联

#### 方案C: 爬取类内容源 (★★★☆☆ 谨慎)
**合规性**: ⚠️ 灰色地带，需风险告知  
**目标平台**: 国内可访问、有英语学习内容的平台

##### 1. **B站英语学习频道**
- 目标频道: VOA、BBC精选、TED演讲、英语新闻、口语对话等
- **合规考量**: 
  - B站用户协议禁止爬取
  - 版权问题：大多数内容为搬运（本身可能侵权）
  - 风险: 封IP、法律风险
- **建议**: 不作为主要方案，如需使用需在APP中明确警告用户

##### 2. **优酷、腾讯视频 - 英语专区**
- 状态: 同样存在版权风险
- 建议: 不推荐爬取，改为合作方案

##### 3. **喜马拉雅 - 英语学习Podcast**
- 状态: 提供RSS，可合法聚合
- 实现: RSS订阅 + 用户授权

#### 方案D: 合作/官方渠道 (★★★★☆ 长期)
- 与教育平台合作 (新东方、英孚等)
- 接入国家公开课资源
- 教材配套音频授权

#### 方案E: 自制内容 (★★☆☆☆ 后期)
- 邀请发音教师录制
- 社区用户投稿
- 众包字幕翻译

---

### 3.3 字幕和翻译

#### 自动字幕生成
- **离线方案**: OpenAI Whisper (中文模型 + 英文模型)
  - 集成方式: C# 封装 + ONNX Runtime (离线推理)
  - 优势: 隐私、无网依赖、支持方言识别
  - 劣势: 模型大 (1-2GB)

- **在线方案**: Azure Speech-to-Text API
  - 优势: 准确率高、实时处理
  - 缺点: 需要联网、有费用

#### 翻译方案
- **离线翻译**: 
  - LibreTranslate 私有部署
  - Ollama + Mistral (开源小模型)

- **在线翻译**:
  - 腾讯翻君 API (便宜、国内优化)
  - Azure Translator
  - 百度翻译API

#### 字幕高亮跟随
- 实时字幕区块高亮
- 句子同步滚动
- 双语对照模式

---

### 3.4 智能词汇识别

#### 词汇分类
1. **高频词** (前1000个基础单词)
   - 来源: Google/COCA语料库
   - 标记: 灰色/隐藏 (用户可选显示)

2. **核心词** (中等难度)
   - 来源: 词汇表数据库
   - 标记: 绿色高亮

3. **难点词** (生僻/复杂)
   - 来源: Academic Word List
   - 标记: 红色/橙色高亮

#### 点击词汇展示
- 英文释义 (简明)
- 中文翻译 (多义)
- 发音 (TTS)
- 词根词缀分解
- 例句 (来自数据库)
- 同义词/反义词

#### 数据来源
- 离线数据库: SQLite 词典 (CC0许可 或 自建)
- 在线补充: MDict、Youdao API

---

### 3.5 基础功能

- ✅ **收藏/书签**: 保存喜欢的资源、笔记
- ✅ **下载管理**: 离线下载、进度管理、清理
- ✅ **学习记录**: 播放时长、学习天数、完成度
- ✅ **搜索**: 按标题、标签、讲师、难度搜索
- ✅ **标签/分类**: 自定义分类、难度级别、题材
- ✅ **推荐系统** (后期): 基于学习历史推荐

---

## 四、用户体验 (UX) 要求

### 4.1 核心原则
1. **一屏一功能**: 通勤单手操作，最小化导航层级
2. **深色优先**: 系统深色模式、AMOLED优化
3. **离线 First**: 所有关键功能支持离线
4. **快速启动**: 冷启动 < 2s、热启动 < 500ms
5. **低内存**: 目标 < 150MB (低端Android)

### 4.2 界面布局
```
主界面 (Tab式 或 Drawer)
├─ 浏览 (Browse/Discover) - 展示推荐资源
├─ 播放 (Now Playing) - 全屏播放器
├─ 下载 (Downloads) - 离线资源管理
├─ 收藏 (Favorites) - 个人书签
└─ 设置 (Settings) - 偏好设置

播放界面 (沉浸式)
├─ 大封面 + 播放进度
├─ 字幕展示 (大字号、高对比)
├─ 控制栏 (进度条、音量、倍速、列表)
└─ 词汇悬浮按钮
```

### 4.3 深色模式配色方案
```
背景: #0A0E27 (深紫黑)
次级背景: #1A1F3A (深蓝灰)
卡片: #242A45 (深灰蓝)
主文本: #E8E9ED (亮灰)
辅助文本: #A0A5B3 (中灰)
主色: #6366F1 (靛蓝，柔和)
强调色: #10B981 (翠绿，用于CTA)
危险: #EF4444 (柔和红)
```

---

## 五、技术栈与架构

### 5.1 项目架构
```
LingoWay/
├─ Presentation/               # MAUI UI层
│  ├─ Views/                   # XAML 页面
│  │  ├─ MainPage.xaml
│  │  ├─ PlayerPage.xaml
│  │  ├─ BrowsePage.xaml
│  │  └─ ...
│  ├─ ViewModels/              # ViewModel (CommunityToolkit)
│  │  ├─ PlayerViewModel.cs
│  │  ├─ BrowseViewModel.cs
│  │  └─ ...
│  ├─ Behaviors/               # 自定义行为
│  └─ Converters/              # 值转换器
│
├─ Domain/                      # 业务逻辑层
│  ├─ Models/                  # 数据模型
│  │  ├─ Episode.cs
│  │  ├─ Subtitle.cs
│  │  ├─ Vocabulary.cs
│  │  └─ ...
│  ├─ Interfaces/              # 抽象接口
│  │  ├─ IAudioPlaybackService.cs
│  │  ├─ ISubtitleService.cs
│  │  └─ ...
│  └─ Constants/               # 常量定义
│
├─ Application/                # 应用服务层
│  ├─ Services/                # 业务服务
│  │  ├─ PlaybackService.cs    # 播放管理
│  │  ├─ DownloadService.cs    # 下载管理
│  │  ├─ VocabularyService.cs  # 词汇识别
│  │  ├─ SubtitleService.cs    # 字幕处理
│  │  └─ ...
│  ├─ DTOs/                    # 数据传输对象
│  └─ Mappers/                 # 模型映射
│
├─ Infrastructure/             # 基础设施层
│  ├─ Http/                    # HTTP客户端
│  │  ├─ ContentClient.cs      # 资源获取
│  │  └─ TranslationClient.cs  # 翻译API
│  ├─ Database/                # 数据访问
│  │  ├─ AppDbContext.cs       # SQLite ORM
│  │  └─ Repositories/
│  ├─ Storage/                 # 本地存储
│  │  ├─ FileStorageService.cs
│  │  └─ CacheService.cs
│  └─ Platform/                # 平台特定代码
│     ├─ iOS/
│     ├─ Android/
│     ├─ Windows/
│     └─ MacCatalyst/
│
└─ Resources/                  # 资源文件
   ├─ Styles/                  # XAML样式
   ├─ Icons/                   # SVG图标
   ├─ Data/                    # 词典/数据库
   └─ Localization/            # 多语言资源
```

### 5.2 关键技术栈

| 层级 | 技术 | 说明 |
|------|------|------|
| UI | MAUI | 跨平台UI框架 |
| MVVM | CommunityToolkit.Mvvm | 官方推荐 |
| 数据库 | SQLite + Entity Framework Core | 轻量级本地存储 |
| HTTP | HttpClientFactory | 官方标准 |
| 依赖注入 | Microsoft.Extensions.DependencyInjection | MAUI原生支持 |
| 日志 | Microsoft.Extensions.Logging | 轻量级日志 |
| 播放 | 平台特定 (AVPlayer/ExoPlayer/MediaPlayerElement) | 原生性能 |
| 字幕 | Whisper.net (ONNX) 或 API | 离线/在线混合 |
| 翻译 | 腾讯翻君/Azure Translator | 云端方案 |

---

## 六、合规性与风险管理

### 6.1 版权和许可

| 内容源 | 许可 | 合规性 | 风险 |
|--------|------|--------|------|
| VOA/BBC RSS | Public Domain | ✅ 安全 | 无 |
| TED 官方API | CC BY-NC-ND | ✅ 安全 (非商用) | 无 |
| 国内Podcast RSS | 平台授权 | ⚠️ 需协议 | 平台政策变化 |
| B站爬取 | 违反ToS | ❌ 高风险 | 封IP、法律风险 |
| 用户导入 | 用户自主 | ✅ 中立 | 用户自负 |

### 6.2 用户隐私

- ✅ 不收集用户浏览历史 (本地存储)
- ✅ 不传输用户个人数据
- ✅ 翻译/字幕处理可本地离线完成
- ⚠️ 若使用云端翻译/字幕，需明确用户同意

### 6.3 应用审核

**App Store (iOS)**
- 需要隐私政策页面
- 如涉及第三方API，需列出所有集成服务
- 不能包含自动下载侵权内容的功能

**Google Play (Android)**
- 内容政策: 不能推广侵权内容
- 爬取功能需提示用户风险
- 提供举报机制

---

## 七、开发路线图

### Phase 1: MVP (8-12周)
```
Week 1-2:   项目结构搭建、MAUI配置、UI基础框架
Week 3-4:   播放器实现 (基础功能)、平台特定代码
Week 5-6:   本地数据库、文件管理、下载功能
Week 7-8:   RSS聚合、资源获取、列表展示
Week 9-10:  字幕显示、基础翻译集成
Week 11-12: 词汇识别、UI磨光、测试、打包
```

### Phase 2: 增强 (6-8周)
```
- 离线Whisper集成
- 高级播放功能 (AB循环、均衡器)
- 学习统计面板
- 词汇本/复习功能
- 社区功能 (分享笔记)
```

### Phase 3: 优化 (4-6周)
```
- 推荐算法
- 云端同步 (可选)
- 性能优化
- 多语言支持
- 第三方平台集成
```

---

## 八、开发优先级 (MoSCoW)

### Must Have (MVP必须)
- [x] 基础播放器 (音频/视频)
- [x] RSS资源聚合
- [x] 字幕显示
- [x] 离线下载
- [x] 深色主题UI

### Should Have (8-12周内)
- [x] 词汇识别和释义
- [x] 基础翻译
- [x] 学习记录
- [x] 搜索和过滤

### Could Have (后期可选)
- [ ] 离线Whisper字幕生成
- [ ] 高级推荐系统
- [ ] 云端同步
- [ ] 社区互动

### Won't Have (暂不考虑)
- [ ] B站爬取 (合规风险)
- [ ] 直播功能
- [ ] 社交媒体集成
- [ ] 电商功能

---

## 九、性能目标

| 指标 | 目标 | 实现方案 |
|------|------|--------|
| 冷启动时间 | < 2s | 延迟加载、资源优化 |
| 热启动时间 | < 500ms | 缓存、后台预加载 |
| 内存占用 | < 150MB | 流式播放、缓存限制 |
| 磁盘占用 | < 500MB (基础) | 选择性下载、清理策略 |
| 首次渲染 | < 1s | 虚拟滚动、精简布局 |
| 播放卡顿率 | < 0.1% | ExoPlayer/AVPlayer + 缓冲预加载 |

---

## 十、成功指标 (KPI)

### 用户体验
- ✅ 通勤场景下单手操作完成度 > 90%
- ✅ 深色模式下续航时间提升 > 15%
- ✅ 用户反馈评分 > 4.5/5

### 功能完整度
- ✅ 支持 4 个以上官方资源源
- ✅ 字幕准确率 > 95%
- ✅ 词汇识别覆盖率 > 90%

### 合规性
- ✅ 0 个版权投诉
- ✅ App Store / Google Play 审核通过率 100%
- ✅ 隐私政策完整度 100%

---

## 总结

LingoWay定位为**合规、高效、用户友好**的碎片化英语学习工具。核心竞争力在于：
1. **合法合规**: 以官方API和用户导入为主，避免爬取风险
2. **离线体验**: 强离线支持，适合中国网络环境
3. **极简交互**: 专为通勤场景优化
4. **原生体验**: 充分利用各平台特性（iOS毛玻璃、Android Material 3等）

---

*文档版本: v1.0 | 更新日期: 2025-01*
