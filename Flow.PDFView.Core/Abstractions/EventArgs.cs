namespace Flow.PDFView.Abstractions;

/// <summary>
/// 文档加载完成事件参数。
/// Event arguments for when a document has finished loading.
/// </summary>
public class DocumentLoadedEventArgs : EventArgs
{
    /// <summary>
    /// 文档总页数。Total number of pages in the document.
    /// </summary>
    public int PageCount { get; }

    /// <summary>
    /// 文档标题。Document title if available.
    /// </summary>
    public string? Title { get; }

    /// <summary>
    /// 文档作者。Document author if available.
    /// </summary>
    public string? Author { get; }

    /// <summary>
    /// 文档主题。Document subject if available.
    /// </summary>
    public string? Subject { get; }

    /// <summary>
    /// 初始化文档加载完成事件参数。
    /// Initialize a new instance of DocumentLoadedEventArgs.
    /// </summary>
    /// <param name="pageCount">总页数 / total page count</param>
    /// <param name="title">标题 / title</param>
    /// <param name="author">作者 / author</param>
    /// <param name="subject">主题 / subject</param>
    public DocumentLoadedEventArgs(int pageCount, string? title = null, string? author = null, string? subject = null)
    {
        PageCount = pageCount;
        Title = title;
        Author = author;
        Subject = subject;
    }
}

/// <summary>
/// 页面切换事件参数。
/// Event arguments for when the page changes.
/// </summary>
public class PageChangedEventArgs : EventArgs
{
    /// <summary>
    /// 当前页面索引（从 0 开始）。Current page index (zero-based).
    /// </summary>
    public int PageIndex { get; }

    /// <summary>
    /// 文档总页数。Total page count.
    /// </summary>
    public int PageCount { get; }

    /// <summary>
    /// 初始化页面切换事件参数。
    /// Initialize a new instance of PageChangedEventArgs.
    /// </summary>
    /// <param name="pageIndex">当前页面索引 / current page index</param>
    /// <param name="pageCount">总页数 / total page count</param>
    public PageChangedEventArgs(int pageIndex, int pageCount)
    {
        PageIndex = pageIndex;
        PageCount = pageCount;
    }
}

/// <summary>
/// 视口变化事件参数。
/// Event arguments for viewport offset/zoom changes.
/// </summary>
public class ViewportChangedEventArgs : EventArgs
{
    /// <summary>
    /// 视口左上角相对于文档原点的 X 偏移（像素）。
    /// X offset from document origin in pixels.
    /// </summary>
    public double OffsetX { get; }

    /// <summary>
    /// 视口左上角相对于文档原点的 Y 偏移（像素）。
    /// Y offset from document origin in pixels.
    /// </summary>
    public double OffsetY { get; }

    /// <summary>
    /// 当前缩放倍率。
    /// Current zoom factor.
    /// </summary>
    public float Zoom { get; }

    /// <summary>
    /// 当前可视区域宽度（像素）。
    /// Viewport width in pixels.
    /// </summary>
    public double ViewportWidth { get; }

    /// <summary>
    /// 当前可视区域高度（像素）。
    /// Viewport height in pixels.
    /// </summary>
    public double ViewportHeight { get; }

    /// <summary>
    /// 初始化视口变化事件参数。
    /// Initialize a new instance of ViewportChangedEventArgs.
    /// </summary>
    public ViewportChangedEventArgs(double offsetX, double offsetY, float zoom, double viewportWidth, double viewportHeight)
    {
        OffsetX = offsetX;
        OffsetY = offsetY;
        Zoom = zoom <= 0f ? 1f : zoom;
        ViewportWidth = Math.Max(0d, viewportWidth);
        ViewportHeight = Math.Max(0d, viewportHeight);
    }
}

/// <summary>
/// PDF 错误事件参数。
/// Event arguments for PDF-related errors.
/// </summary>
public class PdfErrorEventArgs : EventArgs
{
    /// <summary>
    /// 错误消息。Error message describing the problem.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// 异常对象。The exception instance if available.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// 初始化错误事件参数。
    /// Initialize a new instance of PdfErrorEventArgs.
    /// </summary>
    /// <param name="message">错误消息 / error message</param>
    /// <param name="exception">异常对象 / exception (optional)</param>
    public PdfErrorEventArgs(string message, Exception? exception = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Exception = exception;
    }
}

/// <summary>
/// 链接点击事件参数。
/// Event arguments for link taps.
/// </summary>
public class LinkTappedEventArgs : EventArgs
{
    /// <summary>
    /// 链接 URI。The tapped link URI.
    /// </summary>
    public string? Uri { get; }

    /// <summary>
    /// 目标页面索引（可选）。Destination page index if the link targets a page.
    /// </summary>
    public int? DestinationPage { get; }

    /// <summary>
    /// 是否已处理（阻止默认行为）。Whether the tap has been handled.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// 初始化链接点击事件参数。
    /// Initialize a new instance of LinkTappedEventArgs.
    /// </summary>
    /// <param name="uri">链接 URI / link URI</param>
    /// <param name="destinationPage">目标页面 / destination page index (optional)</param>
    public LinkTappedEventArgs(string? uri, int? destinationPage = null)
    {
        Uri = uri;
        DestinationPage = destinationPage;
    }
}

/// <summary>
/// 页面点击事件参数。
/// Event arguments when the page is tapped.
/// </summary>
public class PdfTappedEventArgs : EventArgs
{
    /// <summary>
    /// 页面索引（从 0 开始）。Page index (zero-based).
    /// </summary>
    public int PageIndex { get; }

    /// <summary>
    /// 点击 X 坐标。X coordinate of the tap.
    /// </summary>
    public float X { get; }

    /// <summary>
    /// 点击 Y 坐标。Y coordinate of the tap.
    /// </summary>
    public float Y { get; }

    /// <summary>
    /// 初始化页面点击事件参数。
    /// Initialize a new instance of PdfTappedEventArgs.
    /// </summary>
    /// <param name="pageIndex">页面索引 / page index</param>
    /// <param name="x">X 坐标 / x coordinate</param>
    /// <param name="y">Y 坐标 / y coordinate</param>
    public PdfTappedEventArgs(int pageIndex, float x, float y)
    {
        PageIndex = pageIndex;
        X = x;
        Y = y;
    }
}

/// <summary>
/// 渲染完成事件参数。
/// Event arguments when rendering completes.
/// </summary>
public class RenderedEventArgs : EventArgs
{
    /// <summary>
    /// 文档总页数。Total page count.
    /// </summary>
    public int PageCount { get; }

    /// <summary>
    /// 初始化渲染完成事件参数。
    /// Initialize a new instance of RenderedEventArgs.
    /// </summary>
    /// <param name="pageCount">总页数 / total page count</param>
    public RenderedEventArgs(int pageCount)
    {
        PageCount = pageCount;
    }
}

/// <summary>
/// 注释点击事件参数。
/// Event arguments for annotation taps.
/// </summary>
public class AnnotationTappedEventArgs : EventArgs
{
    /// <summary>
    /// 页面索引（从 0 开始）。Page index (zero-based).
    /// </summary>
    public int PageIndex { get; }

    /// <summary>
    /// 注释类型。Type of the annotation.
    /// </summary>
    public string? AnnotationType { get; }

    /// <summary>
    /// 注释内容。Contents of the annotation.
    /// </summary>
    public string? Contents { get; }

    /// <summary>
    /// 注释边界。Bounds of the annotation.
    /// </summary>
    public Rect Bounds { get; }

    /// <summary>
    /// 是否已处理（阻止默认行为）。Whether handled.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// 初始化注释点击事件参数。
    /// Initialize a new instance of AnnotationTappedEventArgs.
    /// </summary>
    /// <param name="pageIndex">页面索引 / page index</param>
    /// <param name="annotationType">注释类型 / annotation type</param>
    /// <param name="contents">注释内容 / annotation contents</param>
    /// <param name="bounds">注释边界 / annotation bounds</param>
    public AnnotationTappedEventArgs(int pageIndex, string? annotationType, string? contents, Rect bounds)
    {
        PageIndex = pageIndex;
        AnnotationType = annotationType;
        Contents = contents;
        Bounds = bounds;
    }
}

/// <summary>
/// 搜索结果事件参数。
/// Event arguments for search results.
/// </summary>
public class PdfSearchResultsEventArgs : EventArgs
{
    /// <summary>
    /// 搜索关键词。Search query used.
    /// </summary>
    public string Query { get; }

    /// <summary>
    /// 搜索结果列表。List of search results.
    /// </summary>
    public IReadOnlyList<PdfSearchResult> Results { get; }

    /// <summary>
    /// 当前结果索引。Current result index.
    /// </summary>
    public int CurrentIndex { get; }

    /// <summary>
    /// 是否搜索完成。Whether search is complete.
    /// </summary>
    public bool IsComplete { get; }

    /// <summary>
    /// 初始化搜索结果事件参数。
    /// Initialize a new instance of PdfSearchResultsEventArgs.
    /// </summary>
    public PdfSearchResultsEventArgs(string query, IReadOnlyList<PdfSearchResult> results, int currentIndex = 0, bool isComplete = true)
    {
        Query = query;
        Results = results;
        CurrentIndex = currentIndex;
        IsComplete = isComplete;
    }
}

/// <summary>
/// 搜索进度事件参数。
/// Event arguments for search progress.
/// </summary>
public class PdfSearchProgressEventArgs : EventArgs
{
    /// <summary>
    /// 搜索关键词。Search query used.
    /// </summary>
    public string Query { get; }

    /// <summary>
    /// 当前搜索页面。Current page being searched.
    /// </summary>
    public int CurrentPage { get; }

    /// <summary>
    /// 总页数。Total pages.
    /// </summary>
    public int TotalPages { get; }

    /// <summary>
    /// 结果数量。Number of results found so far.
    /// </summary>
    public int ResultCount { get; }

    /// <summary>
    /// 初始化搜索进度事件参数。
    /// Initialize a new instance of PdfSearchProgressEventArgs.
    /// </summary>
    public PdfSearchProgressEventArgs(string query, int currentPage, int totalPages, int resultCount)
    {
        Query = query;
        CurrentPage = currentPage;
        TotalPages = totalPages;
        ResultCount = resultCount;
    }
}

/// <summary>
/// 需要密码事件参数。
/// Event arguments when a password is required.
/// </summary>
public class PdfPasswordRequiredEventArgs : EventArgs
{
    /// <summary>
    /// 密码是否正确。Whether the provided password is correct.
    /// </summary>
    public bool IsCorrect { get; set; }

    /// <summary>
    /// 尝试次数。Number of attempts.
    /// </summary>
    public int AttemptCount { get; }

    /// <summary>
    /// 错误消息。Error message if available.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 初始化需要密码事件参数。
    /// Initialize a new instance of PdfPasswordRequiredEventArgs.
    /// </summary>
    public PdfPasswordRequiredEventArgs(int attemptCount = 0)
    {
        AttemptCount = attemptCount;
    }
}

/// <summary>
/// 大纲点击事件参数。
/// Event arguments for outline (bookmark) taps.
/// </summary>
public class PdfOutlineTappedEventArgs : EventArgs
{
    /// <summary>
    /// 大纲项。The outline item.
    /// </summary>
    public PdfOutline Outline { get; }

    /// <summary>
    /// 是否已处理（阻止默认行为）。Whether handled.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// 初始化大纲点击事件参数。
    /// Initialize a new instance of PdfOutlineTappedEventArgs.
    /// </summary>
    public PdfOutlineTappedEventArgs(PdfOutline outline)
    {
        Outline = outline;
    }
}

/// <summary>
/// 缩略图就绪事件参数。
/// Event arguments when a thumbnail is ready.
/// </summary>
public class PdfThumbnailReadyEventArgs : EventArgs
{
    /// <summary>
    /// 页面索引（从 0 开始）。Page index (zero-based).
    /// </summary>
    public int PageIndex { get; }

    /// <summary>
    /// 缩略图流。Thumbnail stream if available.
    /// </summary>
    public Stream? ThumbnailStream { get; }

    /// <summary>
    /// 是否成功。Whether thumbnail generation succeeded.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// 初始化缩略图就绪事件参数。
    /// Initialize a new instance of PdfThumbnailReadyEventArgs.
    /// </summary>
    public PdfThumbnailReadyEventArgs(int pageIndex, Stream? thumbnailStream, bool success)
    {
        PageIndex = pageIndex;
        ThumbnailStream = thumbnailStream;
        Success = success;
    }
}
