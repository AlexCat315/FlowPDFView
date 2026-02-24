using Flow.PDFView.Abstractions;
using System.Windows.Input;

namespace Flow.PDFView
{
    /// <summary>
    /// en: PDF view interface defining core cross-platform PDF control capabilities.
    /// zh: PDF 视图接口，定义跨平台 PDF 控件的核心功能。
    /// </summary>
    public interface IPdfView : IView
    {
        /// <summary>
        /// en: PDF source (supports file, URI, stream, bytes, asset).
        /// zh: PDF 来源（支持文件、URI、流、字节数组、资源）。
        /// </summary>
        PdfSource? Source { get; set; }

        /// <summary>
        /// en: Current displayed page index (zero-based).
        /// zh: 当前显示的页面索引（从 0 开始）。
        /// </summary>
        int CurrentPage { get; }

        /// <summary>
        /// en: Total number of pages in the document.
        /// zh: 文档总页数。
        /// </summary>
        int PageCount { get; }

        /// <summary>
        /// en: Whether pinch/zoom is enabled.
        /// zh: 是否启用缩放功能（捏合/放大）。
        /// </summary>
        bool EnableZoom { get; set; }

        /// <summary>
        /// en: Whether swipe gestures (page flipping) are enabled.
        /// zh: 是否启用滑动手势（页面翻页）。
        /// </summary>
        bool EnableSwipe { get; set; }

        /// <summary>
        /// en: Whether link navigation is enabled.
        /// zh: 是否启用链接导航。
        /// </summary>
        bool EnableLinkNavigation { get; set; }

        /// <summary>
        /// en: Whether tap gestures are enabled.
        /// zh: 是否启用点击手势。
        /// </summary>
        bool EnableTapGestures { get; set; }

        /// <summary>
        /// en: Current zoom level.
        /// zh: 当前缩放级别。
        /// </summary>
        float Zoom { get; set; }

        /// <summary>
        /// en: Minimum allowed zoom level.
        /// zh: 最小缩放级别。
        /// </summary>
        float MinZoom { get; set; }

        /// <summary>
        /// en: Maximum allowed zoom level.
        /// zh: 最大缩放级别。
        /// </summary>
        float MaxZoom { get; set; }

        /// <summary>
        /// en: Page spacing (logical units: dp on Android, pt on iOS/macOS).
        /// zh: 页面间距（逻辑单位：Android 为 dp，iOS/macOS 为 pt）。
        /// </summary>
        int PageSpacing { get; set; }

        /// <summary>
        /// en: Page fit policy (width, height, etc.).
        /// zh: 页面适配策略（宽度、高度等）。
        /// </summary>
        FitPolicy FitPolicy { get; set; }

        /// <summary>
        /// en: Display mode for PDF (single page, continuous, etc.).
        /// zh: PDF 显示模式（单页、连续等）。
        /// </summary>
        PdfDisplayMode DisplayMode { get; set; }

        /// <summary>
        /// en: Scroll orientation (vertical/horizontal).
        /// zh: 滚动方向（垂直/水平）。
        /// </summary>
        PdfScrollOrientation ScrollOrientation { get; set; }

        /// <summary>
        /// en: Default page to display on load.
        /// zh: 默认加载时显示的页面。
        /// </summary>
        int DefaultPage { get; set; }

        /// <summary>
        /// en: Whether antialiasing is enabled (platform-specific).
        /// zh: 是否启用抗锯齿（平台相关）。
        /// </summary>
        bool EnableAntialiasing { get; set; }

        /// <summary>
        /// en: Whether to use best quality rendering.
        /// zh: 是否使用最佳质量渲染。
        /// </summary>
        bool UseBestQuality { get; set; }

        /// <summary>
        /// en: Background color for the view.
        /// zh: 视图的背景颜色。
        /// </summary>
        Color? BackgroundColor { get; set; }

        /// <summary>
        /// en: Whether PDF annotations should be rendered.
        /// zh: 是否渲染 PDF 注释。
        /// </summary>
        bool EnableAnnotationRendering { get; set; }

        /// <summary>
        /// en: Event raised when the document has finished loading.
        /// zh: 文档加载完成时触发的事件。
        /// </summary>
        event EventHandler<DocumentLoadedEventArgs>? DocumentLoaded;

        /// <summary>
        /// en: Event raised when the current page changes.
        /// zh: 页面切换时触发的事件。
        /// </summary>
        event EventHandler<PageChangedEventArgs>? PageChanged;

        /// <summary>
        /// en: Event raised when an error occurs.
        /// zh: 出现错误时触发的事件。
        /// </summary>
        event EventHandler<PdfErrorEventArgs>? Error;

        /// <summary>
        /// en: Event raised when a link in the PDF is tapped.
        /// zh: 点击 PDF 内链接时触发的事件。
        /// </summary>
        event EventHandler<LinkTappedEventArgs>? LinkTapped;

        /// <summary>
        /// en: Event raised when the PDF page is tapped.
        /// zh: 页面被点击时触发的事件。
        /// </summary>
        event EventHandler<PdfTappedEventArgs>? Tapped;

        /// <summary>
        /// en: Event raised after a render pass completes.
        /// zh: 渲染完成后触发的事件。
        /// </summary>
        event EventHandler<RenderedEventArgs>? Rendered;

        /// <summary>
        /// en: Event raised when an annotation is tapped.
        /// zh: 注释被点击时触发的事件。
        /// </summary>
        event EventHandler<AnnotationTappedEventArgs>? AnnotationTapped;

        /// <summary>
        /// en: Navigate to the specified page index (zero-based).
        /// zh: 跳转到指定页面索引（从 0 开始）。
        /// </summary>
        /// <param name="pageIndex">en: Page index (zero-based). zh: 页面索引（从 0 开始）。</param>
        void GoToPage(int pageIndex);

        /// <summary>
        /// en: Reload the current document source.
        /// zh: 重新加载当前文档来源。
        /// </summary>
        void Reload();

        /// <summary>
        /// en: Whether the current platform implementation supports text search.
        /// zh: 当前平台实现是否支持文本搜索。
        /// </summary>
        bool IsSearchSupported { get; }

        /// <summary>
        /// en: Asynchronously search text in the document.
        /// zh: 异步在文档中搜索文本。
        /// </summary>
        /// <param name="query">en: Search keyword. zh: 搜索关键词。</param>
        /// <param name="options">en: Search options. zh: 搜索选项。</param>
        /// <returns>en: List of search results. zh: 搜索结果列表。</returns>
        Task<IReadOnlyList<PdfSearchResult>> SearchAsync(string query, PdfSearchOptions? options = null);

        /// <summary>
        /// en: Clear current search results/highlights.
        /// zh: 清除当前搜索结果/高亮。
        /// </summary>
        void ClearSearch();

        /// <summary>
        /// en: Enable or disable highlighting of search results.
        /// zh: 设置是否高亮显示搜索结果。
        /// </summary>
        /// <param name="enable">en: Whether to enable highlights. zh: 是否启用高亮。</param>
        void HighlightSearchResults(bool enable);

        /// <summary>
        /// en: Navigate to a specific search result index.
        /// zh: 跳转到指定的搜索结果索引。
        /// </summary>
        /// <param name="resultIndex">en: Search result index. zh: 搜索结果索引。</param>
        void GoToSearchResult(int resultIndex);

        /// <summary>
        /// en: Event raised when search results are found.
        /// zh: 搜索结果找到时触发的事件。
        /// </summary>
        event EventHandler<PdfSearchResultsEventArgs>? SearchResultsFound;

        /// <summary>
        /// en: Event providing search progress updates.
        /// zh: 提供搜索进度更新的事件。
        /// </summary>
        event EventHandler<PdfSearchProgressEventArgs>? SearchProgress;
    }
}
