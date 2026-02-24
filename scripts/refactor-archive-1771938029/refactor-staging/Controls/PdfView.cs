using Flow.PDFView.Abstractions;
using System.Windows.Input;

namespace Flow.PDFView
{
    /// <summary>
    /// en: PDF view control providing cross-platform PDF rendering features.
    /// zh: PDF 视图控件，提供跨平台 PDF 渲染功能。
    /// </summary>
    public class PdfView : View, IPdfView
    {
        private IPdfViewPlatformFeatures _platformFeatures = NoopPdfViewPlatformFeatures.Instance;

        /// <summary>
        /// PDF 来源属性
        /// </summary>
        public static readonly BindableProperty SourceProperty =
            BindableProperty.Create(nameof(Source), typeof(PdfSource), typeof(PdfView), null, propertyChanged: OnSourceChanged);

        /// <summary>
        /// 是否启用缩放功能
        /// </summary>
        public static readonly BindableProperty EnableZoomProperty =
            BindableProperty.Create(nameof(EnableZoom), typeof(bool), typeof(PdfView), PdfViewDefaults.EnableZoom);

        /// <summary>
        /// 是否启用滑动手势（翻页）
        /// </summary>
        public static readonly BindableProperty EnableSwipeProperty =
            BindableProperty.Create(nameof(EnableSwipe), typeof(bool), typeof(PdfView), PdfViewDefaults.EnableSwipe);

        /// <summary>
        /// 是否启用点击手势
        /// </summary>
        public static readonly BindableProperty EnableTapGesturesProperty =
            BindableProperty.Create(nameof(EnableTapGestures), typeof(bool), typeof(PdfView), PdfViewDefaults.EnableTapGestures);

        /// <summary>
        /// 是否启用链接导航
        /// </summary>
        public static readonly BindableProperty EnableLinkNavigationProperty =
            BindableProperty.Create(nameof(EnableLinkNavigation), typeof(bool), typeof(PdfView), PdfViewDefaults.EnableLinkNavigation);

        /// <summary>
        /// 当前缩放级别
        /// </summary>
        public static readonly BindableProperty ZoomProperty =
            BindableProperty.Create(nameof(Zoom), typeof(float), typeof(PdfView), PdfViewDefaults.Zoom);

        /// <summary>
        /// 最小缩放级别
        /// </summary>
        public static readonly BindableProperty MinZoomProperty =
            BindableProperty.Create(nameof(MinZoom), typeof(float), typeof(PdfView), PdfViewDefaults.MinZoom);

        /// <summary>
        /// 最大缩放级别
        /// </summary>
        public static readonly BindableProperty MaxZoomProperty =
            BindableProperty.Create(nameof(MaxZoom), typeof(float), typeof(PdfView), PdfViewDefaults.MaxZoom, propertyChanged: OnMaxZoomPropertyChanged);

        /// <summary>
        /// 页面间距（逻辑单位：Android 为 dp，iOS/macOS 为 pt）
        /// </summary>
        public static readonly BindableProperty PageSpacingProperty =
            BindableProperty.Create(nameof(PageSpacing), typeof(int), typeof(PdfView), PdfViewDefaults.PageSpacing);

        /// <summary>
        /// 页面适配策略
        /// </summary>
        public static readonly BindableProperty FitPolicyProperty =
            BindableProperty.Create(nameof(FitPolicy), typeof(FitPolicy), typeof(PdfView), PdfViewDefaults.DefaultFitPolicy);

        /// <summary>
        /// PDF 显示模式
        /// </summary>
        public static readonly BindableProperty DisplayModeProperty =
            BindableProperty.Create(nameof(DisplayMode), typeof(PdfDisplayMode), typeof(PdfView), PdfViewDefaults.DefaultDisplayMode);

        /// <summary>
        /// 滚动方向
        /// </summary>
        public static readonly BindableProperty ScrollOrientationProperty =
            BindableProperty.Create(nameof(ScrollOrientation), typeof(PdfScrollOrientation), typeof(PdfView), PdfViewDefaults.DefaultScrollOrientation);

        /// <summary>
        /// 默认显示页面（从 0 开始）
        /// </summary>
        public static readonly BindableProperty DefaultPageProperty =
            BindableProperty.Create(nameof(DefaultPage), typeof(int), typeof(PdfView), PdfViewDefaults.DefaultPage);

        /// <summary>
        /// 是否启用抗锯齿（仅 Android 有效）
        /// </summary>
        public static readonly BindableProperty EnableAntialiasingProperty =
            BindableProperty.Create(nameof(EnableAntialiasing), typeof(bool), typeof(PdfView), PdfViewDefaults.EnableAntialiasing);

        /// <summary>
        /// 是否使用最佳质量渲染
        /// </summary>
        public static readonly BindableProperty UseBestQualityProperty =
            BindableProperty.Create(nameof(UseBestQuality), typeof(bool), typeof(PdfView), PdfViewDefaults.UseBestQuality);

        /// <summary>
        /// 是否渲染 PDF 注释
        /// </summary>
        public static readonly BindableProperty EnableAnnotationRenderingProperty =
            BindableProperty.Create(nameof(EnableAnnotationRendering), typeof(bool), typeof(PdfView), PdfViewDefaults.EnableAnnotationRendering);

        /// <summary>
        /// PDF URL 地址（兼容性属性）
        /// </summary>
        public static readonly BindableProperty UriProperty =
            BindableProperty.Create(nameof(Uri), typeof(string), typeof(PdfView), default(string));

        /// <summary>
        /// 是否水平滚动（兼容性属性）
        /// </summary>
        public static readonly BindableProperty IsHorizontalProperty =
            BindableProperty.Create(nameof(IsHorizontal), typeof(bool), typeof(PdfView), PdfViewDefaults.IsHorizontal);

        /// <summary>
        /// 页面外观设置（兼容性保留）
        /// </summary>
        public static readonly BindableProperty PageAppearanceProperty =
            BindableProperty.Create(nameof(PageAppearance), typeof(PageAppearance), typeof(PdfView), null);

        /// <summary>
        /// 页面切换命令（用于 MVVM）
        /// </summary>
        public static readonly BindableProperty PageChangedCommandProperty =
            BindableProperty.Create(nameof(PageChangedCommand), typeof(ICommand), typeof(PdfView), default(ICommand));

        /// <summary>
        /// 当前页面索引（从 0 开始）
        /// </summary>
        public static readonly BindableProperty PageIndexProperty =
            BindableProperty.Create(nameof(PageIndex), typeof(uint), typeof(PdfView), PdfViewDefaults.PageIndex, BindingMode.TwoWay);

        /// <summary>
        /// PDF 来源，支持 URL、本地文件、资源文件等多种来源
        /// </summary>
        public PdfSource? Source
        {
            get => (PdfSource?)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// 是否启用缩放功能
        /// </summary>
        public bool EnableZoom
        {
            get => (bool)GetValue(EnableZoomProperty);
            set => SetValue(EnableZoomProperty, value);
        }

        /// <summary>
        /// 是否启用滑动手势（翻页）
        /// </summary>
        public bool EnableSwipe
        {
            get => (bool)GetValue(EnableSwipeProperty);
            set => SetValue(EnableSwipeProperty, value);
        }

        /// <summary>
        /// 是否启用点击手势
        /// </summary>
        public bool EnableTapGestures
        {
            get => (bool)GetValue(EnableTapGesturesProperty);
            set => SetValue(EnableTapGesturesProperty, value);
        }

        /// <summary>
        /// 是否启用链接导航
        /// </summary>
        public bool EnableLinkNavigation
        {
            get => (bool)GetValue(EnableLinkNavigationProperty);
            set => SetValue(EnableLinkNavigationProperty, value);
        }

        /// <summary>
        /// 当前缩放级别
        /// </summary>
        public float Zoom
        {
            get => (float)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        /// <summary>
        /// 最小缩放级别
        /// </summary>
        public float MinZoom
        {
            get => (float)GetValue(MinZoomProperty);
            set => SetValue(MinZoomProperty, value);
        }

        /// <summary>
        /// 最大缩放级别
        /// </summary>
        public float MaxZoom
        {
            get => (float)GetValue(MaxZoomProperty);
            set => SetValue(MaxZoomProperty, value);
        }

        /// <summary>
        /// 页面间距（逻辑单位：Android 为 dp，iOS/macOS 为 pt）
        /// </summary>
        public int PageSpacing
        {
            get => (int)GetValue(PageSpacingProperty);
            set => SetValue(PageSpacingProperty, value);
        }

        /// <summary>
        /// 页面适配策略
        /// </summary>
        public FitPolicy FitPolicy
        {
            get => (FitPolicy)GetValue(FitPolicyProperty);
            set => SetValue(FitPolicyProperty, value);
        }

        /// <summary>
        /// PDF 显示模式
        /// </summary>
        public PdfDisplayMode DisplayMode
        {
            get => (PdfDisplayMode)GetValue(DisplayModeProperty);
            set => SetValue(DisplayModeProperty, value);
        }

        /// <summary>
        /// 滚动方向
        /// </summary>
        public PdfScrollOrientation ScrollOrientation
        {
            get => (PdfScrollOrientation)GetValue(ScrollOrientationProperty);
            set => SetValue(ScrollOrientationProperty, value);
        }

        /// <summary>
        /// 默认显示页面（从 0 开始）
        /// </summary>
        public int DefaultPage
        {
            get => (int)GetValue(DefaultPageProperty);
            set => SetValue(DefaultPageProperty, value);
        }

        /// <summary>
        /// 是否启用抗锯齿（仅 Android 有效）
        /// </summary>
        public bool EnableAntialiasing
        {
            get => (bool)GetValue(EnableAntialiasingProperty);
            set => SetValue(EnableAntialiasingProperty, value);
        }

        /// <summary>
        /// 是否使用最佳质量渲染
        /// </summary>
        public bool UseBestQuality
        {
            get => (bool)GetValue(UseBestQualityProperty);
            set => SetValue(UseBestQualityProperty, value);
        }

        /// <summary>
        /// 背景颜色
        /// </summary>
        public new Color? BackgroundColor
        {
            get => (Color?)GetValue(BackgroundColorProperty);
            set => SetValue(BackgroundColorProperty, value);
        }

        /// <summary>
        /// 是否渲染 PDF 注释
        /// </summary>
        public bool EnableAnnotationRendering
        {
            get => (bool)GetValue(EnableAnnotationRenderingProperty);
            set => SetValue(EnableAnnotationRenderingProperty, value);
        }

        /// <summary>
        /// 当前显示的页面（从 0 开始）
        /// </summary>
        public int CurrentPage { get; internal set; }

        /// <summary>
        /// PDF 文档总页数
        /// </summary>
        public int PageCount { get; internal set; }

        /// <summary>
        /// PDF URL 地址（兼容性属性）
        /// </summary>
        public string? Uri
        {
            get => (string?)GetValue(UriProperty);
            set => SetValue(UriProperty, value);
        }

        /// <summary>
        /// 是否水平滚动（兼容性属性）
        /// </summary>
        public bool IsHorizontal
        {
            get => (bool)GetValue(IsHorizontalProperty);
            set => SetValue(IsHorizontalProperty, value);
        }

        /// <summary>
        /// 页面外观设置（兼容性保留）
        /// </summary>
        public PageAppearance? PageAppearance
        {
            get => (PageAppearance?)GetValue(PageAppearanceProperty);
            set => SetValue(PageAppearanceProperty, value);
        }

        /// <summary>
        /// 页面切换命令（用于 MVVM）
        /// </summary>
        public ICommand PageChangedCommand
        {
            get => (ICommand)GetValue(PageChangedCommandProperty);
            set => SetValue(PageChangedCommandProperty, value);
        }

        /// <summary>
        /// 当前页面索引（从 0 开始）
        /// </summary>
        public uint PageIndex
        {
            get => (uint)GetValue(PageIndexProperty);
            set => SetValue(PageIndexProperty, value);
        }

        /// <summary>
        /// 文档加载完成事件
        /// </summary>
        public event EventHandler<DocumentLoadedEventArgs>? DocumentLoaded;

        /// <summary>
        /// 页面切换事件
        /// </summary>
        public event EventHandler<PageChangedEventArgs>? PageChanged;

        /// <summary>
        /// 错误事件
        /// </summary>
        public event EventHandler<PdfErrorEventArgs>? Error;

        /// <summary>
        /// 链接点击事件
        /// </summary>
        public event EventHandler<LinkTappedEventArgs>? LinkTapped;

        /// <summary>
        /// 页面点击事件
        /// </summary>
        public event EventHandler<PdfTappedEventArgs>? Tapped;

        /// <summary>
        /// 渲染完成事件
        /// </summary>
        public event EventHandler<RenderedEventArgs>? Rendered;

        /// <summary>
        /// 注释点击事件（仅 iOS）
        /// </summary>
        public event EventHandler<AnnotationTappedEventArgs>? AnnotationTapped;

        /// <summary>
        /// 搜索结果事件
        /// </summary>
        public event EventHandler<PdfSearchResultsEventArgs>? SearchResultsFound;

        /// <summary>
        /// 搜索进度事件
        /// </summary>
        public event EventHandler<PdfSearchProgressEventArgs>? SearchProgress;

        /// <summary>
        /// 当前平台是否支持搜索
        /// </summary>
        public bool IsSearchSupported => _platformFeatures.IsSearchSupported;

        /// <summary>
        /// 跳转到指定页面
        /// en: Navigate to the specified page index (zero-based).
        /// zh: 跳转到指定页面索引（从 0 开始）。
        /// </summary>
        /// <param name="pageIndex">页面索引（从 0 开始）</param>
        public void GoToPage(int pageIndex)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(pageIndex));

            // Minimal navigation implementation: set PageIndex and raise PageChanged
            PageIndex = (uint)pageIndex;
            PageChanged?.Invoke(this, new PageChangedEventArgs(pageIndex, PageCount));
        }

        /// <summary>
        /// en: Reload the current document source.
        /// zh: 重新加载当前文档来源。
        /// </summary>
        public void Reload()
        {
            var current = Source;
            Source = null;
            Source = current;
        }

        /// <summary>
        /// en: Asynchronously search text in the document (minimal stub).
        /// zh: 异步在文档中搜索文本（最小实现的存根）。
        /// </summary>
        public Task<IReadOnlyList<PdfSearchResult>> SearchAsync(string query, PdfSearchOptions? options = null)
        {
            // Minimal implementation: no-op, return empty result set.
            return Task.FromResult((IReadOnlyList<PdfSearchResult>)Array.Empty<PdfSearchResult>());
        }

        /// <summary>
        /// en: Clear current search results/highlights.
        /// zh: 清除当前搜索结果/高亮。
        /// </summary>
        public void ClearSearch()
        {
            // no-op minimal implementation
        }

        /// <summary>
        /// en: Enable or disable highlighting of search results.
        /// zh: 设置是否高亮显示搜索结果。
        /// </summary>
        public void HighlightSearchResults(bool enable)
        {
            // no-op minimal implementation
        }

        /// <summary>
        /// en: Navigate to a specific search result index (minimal stub).
        /// zh: 跳转到指定的搜索结果索引（最小实现）。
        /// </summary>
        public void GoToSearchResult(int resultIndex)
        {
            // no-op minimal implementation
        }
    }
}
