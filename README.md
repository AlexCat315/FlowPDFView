# Flow.PDFView

`Flow.PDFView` 是一个跨平台 .NET MAUI PDF 控件，支持 Android、iOS、MacCatalyst（Windows 仍为基础实现）。

## 主要能力

- 原生 PDF 渲染（非 WebView）
- 多数据源加载：`Uri` / 本地文件 / Asset / 字节数组 / Stream
- 页面控制：缩放、滑动方向、显示模式、跳转页
- 事件：文档加载、页码变化、错误、点击、链接点击
- 搜索 API（统一入口）
  - `SearchAsync`
  - `GoToSearchResult`
  - `ClearSearch`
  - `HighlightSearchResults`
  - `SearchResultsFound` / `SearchProgress`

## 平台功能矩阵

| 功能 | Android | iOS | MacCatalyst | Windows |
|---|---|---|---|---|
| 基础渲染/翻页/缩放 | ✅ | ✅ | ✅ | ✅ |
| 统一搜索 API | ✅ | ✅ | ✅ | ✅ |
| 搜索结果跳转 | ✅ | ✅ | ✅ | ✅ |
| 搜索高亮 | ✅ | ✅ | ✅ | ✅ |

## 仓库结构

```text
FlowPDFView/
├── FlowPDFView/                  # 核心 MAUI 控件库
├── FlowPDFView.Android.Binding/  # Android 原生绑定层
├── BlazePdfium/                  # Android PDF 引擎源码与构建
└── ExampleMauiApp/               # 演示应用
```

## 本地接入（ProjectReference）

在你的 MAUI 项目中直接引用本地库项目：

```xml
<ItemGroup>
  <ProjectReference Include="..\FlowPDFView\Flow.PDFView.csproj" />
</ItemGroup>
```

并在 `MauiProgram.cs` 注册：

```csharp
builder
    .UseMauiApp<App>()
    .UseMauiPdfView();
```

## 本地 Android 依赖（BlazePdfium）

如果你修改了 `BlazePdfium` 版本或源码，请重新构建并确保绑定工程使用最新产物：

1. 在 `BlazePdfium/` 执行 `./gradlew buildAll`
2. 该任务会自动清理旧 Blaze AAR、复制新版 AAR 到 `FlowPDFView.Android.Binding/Jars/` 并完成校验
3. 重新构建：

```bash
cd BlazePdfium
./gradlew buildAll
cd ..
dotnet build ExampleMauiApp/ExampleMauiApp.csproj -f net10.0-android
```

## 快速使用

```xml
<pdf:PdfView x:Name="PdfViewer"
             EnableZoom="True"
             EnableSwipe="True"
             DisplayMode="SinglePageContinuous"
             ScrollOrientation="Vertical" />
```

```csharp
PdfViewer.Source = new UriPdfSource(new Uri("https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf"));
PdfViewer.GoToPage(3);

var results = await PdfViewer.SearchAsync("pdf", new PdfSearchOptions
{
    Highlight = true,
    SearchAllPages = true,
    MaxResults = 200
});

if (results.Count > 0)
{
    PdfViewer.GoToSearchResult(0);
}
```

## 演示应用

请参考 `ExampleMauiApp/README.md`：

- URL / 本地文件加载
- 翻页与缩放
- 搜索 + 上一条/下一条跳转
- 工具栏浮层交互

## 构建验证（本仓库）

```bash
dotnet build FlowPDFView/Flow.PDFView.csproj -f net10.0-android
dotnet build FlowPDFView/Flow.PDFView.csproj -f net10.0-ios
dotnet build FlowPDFView/Flow.PDFView.csproj -f net10.0-maccatalyst
dotnet build ExampleMauiApp/ExampleMauiApp.csproj -f net10.0-android
```
