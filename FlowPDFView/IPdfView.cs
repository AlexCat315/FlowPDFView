using Flow.PDFView.Abstractions;
using System.Windows.Input;

namespace Flow.PDFView
{
    /// <summary>
    /// PDF 视图接口，定义跨平台 PDF 控件的核心功能
    /// </summary>
    public interface IPdfView : IView
    {
        /// <summary>
        /// PDF 来源
        /// </summary>
        PdfSource? Source { get; set; }

        /// <summary>
        /// 当前页面（从 0 开始）
        /// </summary>
        int CurrentPage { get; }

        /// <summary>
        /// 文档总页数
        /// </summary>
        int PageCount { get; }

        /// <summary>
        /// 是否启用缩放
        /// </summary>
        bool EnableZoom { get; set; }

        /// <summary>
        /// 是否启用滑动手势
        /// </summary>
        bool EnableSwipe { get; set; }

        /// <summary>
        /// 是否启用链接导航
        /// </summary>
        bool EnableLinkNavigation { get; set; }

        /// <summary>
        /// 是否启用点击手势
        /// </summary>
        bool EnableTapGestures { get; set; }

        /// <summary>
        /// 当前缩放级别
        /// </summary>
        float Zoom { get; set; }

        /// <summary>
        /// 最小缩放级别
        /// </summary>
        float MinZoom { get; set; }

        /// <summary>
        /// 最大缩放级别
        /// </summary>
        float MaxZoom { get; set; }

        /// <summary>
        /// 页面间距（逻辑单位：Android 为 dp，iOS/macOS 为 pt）
        /// </summary>
        int PageSpacing { get; set; }

        /// <summary>
        /// 页面适配策略
        /// </summary>
        FitPolicy FitPolicy { get; set; }

        /// <summary>
        /// 显示模式
        /// </summary>
        PdfDisplayMode DisplayMode { get; set; }

        /// <summary>
        /// 滚动方向
        /// </summary>
        PdfScrollOrientation ScrollOrientation { get; set; }

        /// <summary>
        /// 默认显示页面
        /// </summary>
        int DefaultPage { get; set; }

        /// <summary>
        /// 是否启用抗锯齿
        /// </summary>
        bool EnableAntialiasing { get; set; }

        /// <summary>
        /// 是否使用最佳质量
        /// </summary>
        bool UseBestQuality { get; set; }

        /// <summary>
        /// 背景颜色
        /// </summary>
        Color? BackgroundColor { get; set; }

        /// <summary>
        /// 是否渲染注释
        /// </summary>
        bool EnableAnnotationRendering { get; set; }

        /// <summary>
        /// 文档加载完成事件
        /// </summary>
        event EventHandler<DocumentLoadedEventArgs>? DocumentLoaded;

        /// <summary>
        /// 页面切换事件
        /// </summary>
        event EventHandler<PageChangedEventArgs>? PageChanged;

        /// <summary>
        /// 错误事件
        /// </summary>
        event EventHandler<PdfErrorEventArgs>? Error;

        /// <summary>
        /// 链接点击事件
        /// </summary>
        event EventHandler<LinkTappedEventArgs>? LinkTapped;

        /// <summary>
        /// 页面点击事件
        /// </summary>
        event EventHandler<PdfTappedEventArgs>? Tapped;

        /// <summary>
        /// 渲染完成事件
        /// </summary>
        event EventHandler<RenderedEventArgs>? Rendered;

        /// <summary>
        /// 注释点击事件
        /// </summary>
        event EventHandler<AnnotationTappedEventArgs>? AnnotationTapped;

        /// <summary>
        /// 跳转到指定页面
        /// </summary>
        /// <param name="pageIndex">页面索引（从 0 开始）</param>
        void GoToPage(int pageIndex);

        /// <summary>
        /// 重新加载文档
        /// </summary>
        void Reload();

        /// <summary>
        /// 当前平台是否支持搜索
        /// </summary>
        bool IsSearchSupported { get; }

        /// <summary>
        /// 异步搜索文本
        /// </summary>
        /// <param name="query">搜索关键词</param>
        /// <param name="options">搜索选项</param>
        /// <returns>搜索结果列表</returns>
        Task<IReadOnlyList<PdfSearchResult>> SearchAsync(string query, PdfSearchOptions? options = null);

        /// <summary>
        /// 清除搜索结果
        /// </summary>
        void ClearSearch();

        /// <summary>
        /// 设置是否高亮搜索结果
        /// </summary>
        /// <param name="enable">是否启用高亮</param>
        void HighlightSearchResults(bool enable);

        /// <summary>
        /// 跳转到指定搜索结果
        /// </summary>
        /// <param name="resultIndex">搜索结果索引</param>
        void GoToSearchResult(int resultIndex);

        /// <summary>
        /// 搜索结果事件
        /// </summary>
        event EventHandler<PdfSearchResultsEventArgs>? SearchResultsFound;

        /// <summary>
        /// 搜索进度事件
        /// </summary>
        event EventHandler<PdfSearchProgressEventArgs>? SearchProgress;
    }
}
