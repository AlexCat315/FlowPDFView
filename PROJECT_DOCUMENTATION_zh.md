# FlowPDFView 项目文档

## 1. 项目概述

**FlowPDFView** 是一个基于 **.NET MAUI** 的跨平台 PDF 渲染库，支持 Android、iOS、macOS 和 Windows 四大平台。项目采用分层架构设计，将核心抽象、控件实现和平台特定代码分离，便于维护和扩展。

### 技术栈
- **框架**: .NET MAUI 10.0
- **目标平台**: Android (API 35+), iOS 13+, macOS Catalyst 15+, Windows
- **核心语言**: C# 12
- **空值安全**: 启用 (Nullable Reference Types)

---

## 2. 项目结构

```
FlowPDFView/
├── Flow.PDFView.Core/                 # 核心抽象层（平台无关）
│   ├── Abstractions/
│   │   ├── IPdfViewCore.cs           # 核心接口定义
│   │   ├── PdfSource.cs              # PDF 来源抽象（文件/URI/流/字节/资源）
│   │   ├── EventArgs.cs              # 事件参数定义
│   │   ├── PdfModels.cs              # 数据模型（搜索结果/大纲/文本选择）
│   │   ├── FitPolicy.cs              # 页面适配策略枚举
│   │   ├── PdfDisplayMode.cs         # 显示模式枚举
│   │   ├── ScrollOrientation.cs      # 滚动方向枚举
│   │   └── PdfSourceTypeConverter.cs # 类型转换器
│   └── Class1.cs                     # 命名空间占位符
│
├── Flow.PDFView.Controls/             # 控件配置层
│   ├── PdfViewDefaults.cs             # 默认配置常量
│   └── Class1.cs                     # 命名空间占位符
│
├── FlowPDFView/                        # 主库项目
│   ├── PdfView.cs                     # 主控件实现（继承 View）
│   ├── IPdfView.cs                    # 公开接口（facade）
│   ├── PageAppearance.cs              # 页面外观配置
│   ├── AppBuilderExtensions.cs        # MAUI 扩展方法
│   │
│   ├── Interfaces/
│   │   └── IPdfViewPlatformFeatures.cs  # 平台特性接口
│   │
│   ├── DataSources/                   # PDF 来源实现（主项目）
│   │   ├── IPdfSource.cs             # 来源接口
│   │   ├── FilePdfSource.cs          # 文件来源
│   │   ├── HttpPdfSource.cs          # HTTP 来源
│   │   ├── AssetPdfSource.cs         # 资源来源
│   │   ├── ByteArrayPdfSource.cs     # 字节数组来源
│   │   └── PdfTempFileHelper.cs      # 临时文件辅助
│   │
│   ├── Helpers/
│   │   └── DesiredSizeHelper.cs      # 尺寸计算辅助
│   │
│   ├── Events/
│   │   └── PageChangedEventArgs.cs    # 页面切换事件
│   │
│   └── Platforms/                     # 平台特定实现
│       ├── Android/
│       │   ├── PdfViewAndroid.cs      # Android 封装类
│       │   ├── PdfViewHandler.cs      # MAUI Handler
│       │   ├── ScreenHelper.cs        # 屏幕密度辅助
│       │   ├── Resources/layout/
│       │   │   └── card_view.xml     # 页面卡片布局
│       │   └── Common/
│       │       ├── PdfBitmapAdapter.cs      # RecyclerView 适配器
│       │       ├── CardViewHolder.cs       # ViewHolder
│       │       ├── ZoomableRecyclerView.cs # 可缩放 RecyclerView
│       │       └── ZoomableLinearLayoutManager.cs
│       │
│       ├── iOS/
│       │   ├── PdfViewiOS.cs         # iOS 封装类
│       │   └── PdfViewHandler.cs      # MAUI Handler
│       │
│       ├── MacCatalyst/
│       │   └── PdfViewHandler.cs      # macOS Handler
│       │
│       └── Windows/
│           └── PdfViewHandler.cs      # Windows Handler
│
├── FlowPDFView.Android.Binding/        # Android 原生绑定
│   └── (自动生成的 Java 绑定代码)
│
└── ExampleMauiApp/                     # 示例应用
    ├── MainPage.xaml(.cs)             # 示例页面
    ├── App.xaml(.cs)                  # 应用入口
    ├── MauiProgram.cs                 # MAUI 程序配置
    └── Platforms/                     # 平台特定配置
```

---

## 3. 架构设计

### 3.1 分层架构

```
┌─────────────────────────────────────────────────────────────┐
│                      使用者 (App Developer)                 │
├─────────────────────────────────────────────────────────────┤
│  FlowPDFView (主库)                                       │
│  ├── PdfView.cs - MAUI 控件，定义 BindableProperty        │
│  ├── IPdfView.cs - 公开接口 facade                        │
│  └── DataSources/ - PDF 来源实现                          │
├─────────────────────────────────────────────────────────────┤
│  Flow.PDFView.Controls                                   │
│  └── PdfViewDefaults.cs - 默认配置常量                    │
├─────────────────────────────────────────────────────────────┤
│  Flow.PDFView.Core (核心抽象)                             │
│  ├── IPdfViewCore.cs - 核心接口                           │
│  ├── PdfSource.cs - PDF 来源抽象 + 隐式转换               │
│  ├── EventArgs.cs - 事件参数                              │
│  ├── PdfModels.cs - 数据模型                              │
│  └── 枚举 (FitPolicy, PdfDisplayMode, ScrollOrientation) │
├─────────────────────────────────────────────────────────────┤
│  Platforms (平台实现)                                      │
│  ├── Android/ - PdfViewAndroid + Handler + 组件          │
│  ├── iOS/    - PdfViewiOS + Handler                     │
│  ├── MacCatalyst/ - Handler                             │
│  └── Windows/ - Handler + PdfPig 库                      │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 MAUI Handler 架构

项目使用 **MAUI Handlers** 实现跨平台：

1. **PdfView** (MAUI Controls) - 定义跨平台属性
2. **PdfViewHandler** (Platform) - 将属性映射到原生控件
3. **PdfViewiOS/PdfViewAndroid** (Platform Wrapper) - 封装原生 PDF 渲染

```
XAML: <pdf:PdfView Source="document.pdf" />
         ↓
MAUI Handler 映射
         ↓
平台特定实现:
  - Android: PdfKit.Android (Blaze PDF viewer binding)
  - iOS/macOS: PdfKit (原生框架)
  - Windows: PdfPig (第三方库)
```

---

## 4. 核心功能

### 4.1 PDF 来源 (PdfSource)

支持多种 PDF 来源，通过静态工厂方法和隐式转换：

```csharp
// 方式 1: 字符串隐式转换
pdfView.Source = "https://example.com/doc.pdf";
pdfView.Source = "file:///storage/doc.pdf";
pdfView.Source = "asset://pdfs/document.pdf";
pdfView.Source = "document.pdf";  // 自动识别为 asset

// 方式 2: 工厂方法
pdfView.Source = PdfSource.FromFile("/path/to/doc.pdf");
pdfView.Source = PdfSource.FromUri(new Uri("https://..."));
pdfView.Source = PdfSource.FromStream(stream);
pdfView.Source = PdfSource.FromBytes(byteArray);
pdfView.Source = PdfSource.FromAsset("document.pdf");
```

### 4.2 核心属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| Source | PdfSource? | null | PDF 来源 |
| EnableZoom | bool | true | 启用缩放 |
| EnableSwipe | bool | true | 启用滑动手势翻页 |
| EnableTapGestures | bool | false | 启用点击手势 |
| EnableLinkNavigation | bool | true | 启用链接导航 |
| Zoom | float | 1.0 | 当前缩放 |
| MinZoom | float | 1.0 | 最小缩放 |
| MaxZoom | float | 10.0 | 最大缩放 |
| PageSpacing | int | 10 | 页面间距 |
| FitPolicy | FitPolicy | Width | 页面适配策略 |
| DisplayMode | PdfDisplayMode | SinglePageContinuous | 显示模式 |
| ScrollOrientation | PdfScrollOrientation | Vertical | 滚动方向 |
| DefaultPage | int | 0 | 默认显示页面 |
| EnableAntialiasing | bool | true | 抗锯齿 (Android) |
| UseBestQuality | bool | true | 最佳质量 |
| EnableAnnotationRendering | bool | true | 渲染注释 |

### 4.3 事件

| 事件 | 参数 | 说明 |
|------|------|------|
| DocumentLoaded | DocumentLoadedEventArgs | 文档加载完成 |
| PageChanged | PageChangedEventArgs | 页面切换 |
| Error | PdfErrorEventArgs | 错误发生 |
| LinkTapped | LinkTappedEventArgs | 链接点击 |
| Tapped | PdfTappedEventArgs | 页面点击 |
| Rendered | RenderedEventArgs | 渲染完成 |
| AnnotationTapped | AnnotationTappedEventArgs | 注释点击 |
| SearchResultsFound | Pdf | 搜索结果SearchResultsEventArgs |
| SearchProgress | PdfSearchProgressEventArgs | 搜索进度 |

### 4.4 方法

```csharp
// 跳转到指定页面
pdfView.GoToPage(5);

// 重新加载
pdfView.Reload();

// 搜索功能
var results = await pdfView.SearchAsync("keyword");
pdfView.HighlightSearchResults(true);
pdfView.GoToSearchResult(0);
pdfView.ClearSearch();
```

---

## 5. 平台实现细节

### 5.1 Android

- **原生库**: Blaze PDF Viewer (通过 Java 绑定)
- **渲染**: `Com.Blaze.Pdfviewer.PDFView`
- **列表**: `RecyclerView` + `PdfBitmapAdapter`
- **缩放**: 自定义 `ZoomableRecyclerView`
- **Handler**: `PdfViewHandler` (部分类)

### 5.2 iOS/macOS

- **原生框架**: PdfKit
- **渲染**: `PdfKit.PdfView`
- **手势**: `UITapGestureRecognizer`, `UIPanGestureRecognizer`
- **搜索**: 使用 `PdfDocument.GetSelection()` API

### 5.3 Windows

- **渲染库**: PdfPig (UglyToad.PdfPig)
- **显示**: `ScrollViewer` + `Image` 列表
- **文本提取**: PdfPig 内置文本提取

---

## 6. 使用示例

### 6.1 XAML 使用

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:pdf="clr-namespace:Flow.PDFView;assembly=Flow.PDFView">
    <pdf:PdfView x:Name="PdfViewer"
                 Source="https://example.com/document.pdf"
                 EnableZoom="True"
                 EnableSwipe="True"
                 PageSpacing="10"
                 FitPolicy="Width" />
</ContentPage>
```

### 6.2 C# 代码使用

```csharp
public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        
        // 监听事件
        PdfViewer.DocumentLoaded += OnDocumentLoaded;
        PdfViewer.PageChanged += OnPageChanged;
        PdfViewer.Error += OnError;
        
        // 设置来源
        PdfViewer.Source = "asset://pdfs/sample.pdf";
        
        // 或使用代码
        PdfViewer.Source = PdfSource.FromFile(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "document.pdf"));
    }
    
    private void OnDocumentLoaded(object sender, DocumentLoadedEventArgs e)
    {
        Console.WriteLine($"文档加载完成，共 {e.PageCount} 页");
    }
}
```

---

## 7. 构建与发布

### 7.1 项目引用关系

```
Flow.PDFView (主库)
    ├── Flow.PDFView.Core
    ├── Flow.PDFView.Controls
    └── FlowPDFView.Android.Binding (仅 Android)
```

### 7.2 目标框架

- `net10.0` - 默认/共享代码
- `net10.0-android` - Android 特定
- `net10.0-ios` - iOS 特定
- `net10.0-maccatalyst` - macOS Catalyst
- `net10.0-windows` - Windows 特定 (额外依赖: PdfPig)

### 7.3 构建命令

```bash
# 构建所有平台
dotnet build

# 构建特定平台
dotnet build -f net10.0-android
dotnet build -f net10.0-ios

# 发布
dotnet publish -f net10.0-android
dotnet publish -f net10.0-ios
```

---

## 8. 关键设计决策

### 8.1 为什么使用 PdfSource 抽象？

- **类型安全**: 不同的来源需要不同的处理方式
- **延迟加载**: 流和字节数组需要不同生命周期管理
- **平台适配**: 不同平台对来源的处理不同

### 8.2 为什么使用 Handler 而不是 Renderer？

MAUI Handlers 是新的推荐方式，相比旧的 Renderer：
- 更轻量
- API 更简洁
- 更容易维护

### 8.3 默认值管理

- `PdfViewDefaults.cs` 集中管理默认值
- 便于统一修改和文档化
- 支持 Controls 模块独立发布

---

## 9. 注意事项

### 9.1 平台要求

| 平台 | 最低版本 | 说明 |
|------|----------|------|
| Android | API 35 | 需要 Blaze PDF 绑定 |
| iOS | 13.0 | 使用 PdfKit |
| macOS Catalyst | 15.0 | 使用 PdfKit |
| Windows | 10+ | 使用 PdfPig |

### 9.2 权限要求

**Android** (`AndroidManifest.xml`):
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
```

### 9.3 已知限制

- Windows 搜索功能实现不完整
- 部分过时 API 使用 disable` 抑制了 `#pragma warning警告

---

## 10. 扩展开发

### 添加新属性步骤

1. 在 `IPdfViewCore.cs` 添加属性定义
2. 在 `PdfView.cs` 添加 `BindableProperty` 和属性包装
3. 在各平台 `PdfViewHandler.cs` 添加映射方法
4. 在平台实现类中实现逻辑
5. 在 `PdfViewDefaults.cs` 添加默认值（如适用）

### 添加新平台步骤

1. 创建 `Platforms/<Platform>/` 目录
2. 创建 `<Platform>PdfView.cs` 封装类
3. 创建 `PdfViewHandler.cs` 处理器
4. 在主项目 csproj 添加条件引用
