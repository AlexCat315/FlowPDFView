using Flow.PDFView.Abstractions;
using Microsoft.Maui;

namespace Flow.PDFView
{
    /// <summary>
    /// PDF 视图核心接口，定义基础功能
    /// </summary>
    public interface IPdfViewCore : IView
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
        /// 当前缩放级别
        /// </summary>
        float Zoom { get; set; }

        /// <summary>
        /// 跳转到指定页面
        /// </summary>
        /// <param name="pageIndex">页面索引（从 0 开始）</param>
        void GoToPage(int pageIndex);

        /// <summary>
        /// 在当前视口基础上平移（单位：像素）。
        /// </summary>
        /// <param name="deltaX">X 方向平移增量</param>
        /// <param name="deltaY">Y 方向平移增量</param>
        void PanBy(double deltaX, double deltaY);

        /// <summary>
        /// 以指定中心点执行相对缩放。
        /// </summary>
        /// <param name="scaleFactor">缩放倍数（例如 1.05 表示放大 5%）</param>
        /// <param name="centerX">缩放中心点 X（像素）</param>
        /// <param name="centerY">缩放中心点 Y（像素）</param>
        void ZoomBy(double scaleFactor, double centerX, double centerY);

        /// <summary>
        /// 重新加载文档
        /// </summary>
        void Reload();

        /// <summary>
        /// 文档加载完成事件
        /// </summary>
        event EventHandler<DocumentLoadedEventArgs>? DocumentLoaded;

        /// <summary>
        /// 页面切换事件
        /// </summary>
        event EventHandler<PageChangedEventArgs>? PageChanged;

        /// <summary>
        /// 视口变化事件（滚动/缩放）
        /// </summary>
        event EventHandler<ViewportChangedEventArgs>? ViewportChanged;

        /// <summary>
        /// 错误事件
        /// </summary>
        event EventHandler<PdfErrorEventArgs>? Error;
    }

    /// <summary>
    /// PDF 视图显示接口，定义显示相关功能
    /// </summary>
    public interface IPdfViewDisplay : IPdfViewCore
    {
        /// <summary>
        /// 是否启用缩放
        /// </summary>
        bool EnableZoom { get; set; }

        /// <summary>
        /// 是否启用滑动手势
        /// </summary>
        bool EnableSwipe { get; set; }

        /// <summary>
        /// 是否启用点击手势
        /// </summary>
        bool EnableTapGestures { get; set; }

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
        /// 背景颜色
        /// </summary>
        Color? BackgroundColor { get; set; }

        /// <summary>
        /// 页面点击事件
        /// </summary>
        event EventHandler<PdfTappedEventArgs>? Tapped;
    }

    /// <summary>
    /// PDF 视图注释接口，定义注释相关功能
    /// </summary>
    public interface IPdfViewAnnotation : IPdfViewDisplay
    {
        /// <summary>
        /// 是否渲染注释
        /// </summary>
        bool EnableAnnotationRendering { get; set; }

        /// <summary>
        /// 是否启用链接导航
        /// </summary>
        bool EnableLinkNavigation { get; set; }

        /// <summary>
        /// 链接点击事件
        /// </summary>
        event EventHandler<LinkTappedEventArgs>? LinkTapped;

        /// <summary>
        /// 注释点击事件
        /// </summary>
        event EventHandler<AnnotationTappedEventArgs>? AnnotationTapped;
    }

    /// <summary>
    /// PDF 视图搜索接口，定义搜索功能
    /// </summary>
    public interface IPdfViewSearch : IPdfViewAnnotation
    {
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
        /// 高亮搜索结果
        /// </summary>
        /// <param name="enable">是否启用高亮</param>
        void HighlightSearchResults(bool enable);

        /// <summary>
        /// 跳转到指定搜索结果
        /// </summary>
        /// <param name="resultIndex">结果索引</param>
        void GoToSearchResult(int resultIndex);

        /// <summary>
        /// 搜索结果找到事件
        /// </summary>
        event EventHandler<PdfSearchResultsEventArgs>? SearchResultsFound;

        /// <summary>
        /// 搜索进度事件
        /// </summary>
        event EventHandler<PdfSearchProgressEventArgs>? SearchProgress;
    }

    /// <summary>
    /// PDF 视图文本选择接口，定义文本选择功能
    /// </summary>
    public interface IPdfViewTextSelection : IPdfViewSearch
    {
        /// <summary>
        /// 获取指定页面的文本
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <returns>页面文本对象</returns>
        Task<PdfPageText?> GetPageTextAsync(int pageIndex);

        /// <summary>
        /// 获取选中的文本
        /// </summary>
        /// <returns>选中的文本</returns>
        Task<string?> GetSelectedTextAsync();

        /// <summary>
        /// 清除选择
        /// </summary>
        void ClearSelection();

        /// <summary>
        /// 复制选中的文本到剪贴板
        /// </summary>
        void CopySelection();
    }

    /// <summary>
    /// PDF 视图大纲接口，定义文档大纲功能
    /// </summary>
    public interface IPdfViewOutline : IPdfViewTextSelection
    {
        /// <summary>
        /// 获取文档大纲
        /// </summary>
        /// <returns>大纲列表</returns>
        Task<IReadOnlyList<PdfOutline>> GetOutlineAsync();

        /// <summary>
        /// 跳转到指定大纲位置
        /// </summary>
        /// <param name="outline">大纲项</param>
        void GoToOutline(PdfOutline outline);

        /// <summary>
        /// 大纲点击事件
        /// </summary>
        event EventHandler<PdfOutlineTappedEventArgs>? OutlineTapped;
    }

    /// <summary>
    /// PDF 视图缩略图接口，定义缩略图功能
    /// </summary>
    public interface IPdfViewThumbnails : IPdfViewOutline
    {
        /// <summary>
        /// 获取页面缩略图
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <param name="width">缩略图宽度</param>
        /// <param name="height">缩略图高度</param>
        /// <returns>缩略图流</returns>
        Task<Stream?> GetThumbnailAsync(int pageIndex, int width, int height);

        /// <summary>
        /// 缩略图就绪事件
        /// </summary>
        event EventHandler<PdfThumbnailReadyEventArgs>? ThumbnailReady;
    }

    /// <summary>
    /// PDF 视图旋转接口，定义页面旋转功能
    /// </summary>
    public interface IPdfViewRotation : IPdfViewThumbnails
    {
        /// <summary>
        /// 当前旋转角度（0、90、180、270）
        /// 使用 `new` 关键字隐藏可能来自上层接口的同名成员以避免警告。
        /// Current rotation angle (0, 90, 180, 270). Hides a similarly named member from a base interface.
        /// </summary>
        new int Rotation { get; set; }

        /// <summary>
        /// 异步旋转指定页面
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <param name="degrees">旋转角度</param>
        Task RotatePageAsync(int pageIndex, int degrees);
    }

    /// <summary>
    /// PDF 视图密码接口，定义密码保护文档功能
    /// </summary>
    public interface IPdfViewPassword : IPdfViewRotation
    {
        /// <summary>
        /// 是否为密码保护文档
        /// </summary>
        bool IsPasswordProtected { get; }

        /// <summary>
        /// 是否已解锁
        /// </summary>
        bool IsUnlocked { get; }

        /// <summary>
        /// 解锁文档
        /// </summary>
        /// <param name="password">密码</param>
        void Unlock(string password);

        /// <summary>
        /// 需要密码事件
        /// </summary>
        event EventHandler<PdfPasswordRequiredEventArgs>? PasswordRequired;
    }
}
