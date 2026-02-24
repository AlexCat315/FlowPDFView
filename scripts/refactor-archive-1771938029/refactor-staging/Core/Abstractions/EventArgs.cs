namespace Flow.PDFView.Abstractions;

/// <summary>
/// 文档加载完成事件参数
/// </summary>
public class DocumentLoadedEventArgs : EventArgs
{
    /// <summary>
    /// 文档总页数
    /// </summary>
    public int PageCount { get; }

    /// <summary>
    /// 文档标题
    /// </summary>
    public string? Title { get; }

    /// <summary>
    /// 文档作者
    /// </summary>
    public string? Author { get; }

    /// <summary>
    /// 文档主题
    /// </summary>
    public string? Subject { get; }

    /// <summary>
    /// 初始化文档加载完成事件参数
    /// </summary>
    /// <param name="pageCount">总页数</param>
    /// <param name="title">标题</param>
    /// <param name="author">作者</param>
    /// <param name="subject">主题</param>
    public DocumentLoadedEventArgs(int pageCount, string? title = null, string? author = null, string? subject = null)
    {
        PageCount = pageCount;
        Title = title;
        Author = author;
        Subject = subject;
    }
}

/// <summary>
/// 页面切换事件参数
/// </summary>
public class PageChangedEventArgs : EventArgs
{
    /// <summary>
    /// 当前页面索引（从 0 开始）
    /// </summary>
    public int PageIndex { get; }

    /// <summary>
    /// 文档总页数
    /// </summary>
    public int PageCount { get; }

    /// <summary>
    /// 初始化页面切换事件参数
    /// </summary>
    /// <param name="pageIndex">当前页面索引</param>
    /// <param name="pageCount">总页数</param>
    public PageChangedEventArgs(int pageIndex, int pageCount)
    {
        PageIndex = pageIndex;
        PageCount = pageCount;
    }
}

/// <summary>
/// PDF 错误事件参数
/// </summary>
public class PdfErrorEventArgs : EventArgs
{
    /// <summary>
    /// 错误消息
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// 异常对象
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// 初始化错误事件参数
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="exception">异常对象</param>
    public PdfErrorEventArgs(string message, Exception? exception = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Exception = exception;
    }
}

/// <summary>
/// 链接点击事件参数
/// </summary>
public class LinkTappedEventArgs : EventArgs
{
    /// <summary>
    /// 链接 URI
    /// </summary>
    public string? Uri { get; }

    /// <summary>
    /// 目标页面索引
    /// </summary>
    public int? DestinationPage { get; }

    /// <summary>
    /// 是否已处理（阻止默认行为）
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// 初始化链接点击事件参数
    /// </summary>
    /// <param name="uri">链接 URI</param>
    /// <param name="destinationPage">目标页面</param>
    public LinkTappedEventArgs(string? uri, int? destinationPage = null)
    {
        Uri = uri;
        DestinationPage = destinationPage;
    }
}

/// <summary>
/// 页面点击事件参数
/// </summary>
public class PdfTappedEventArgs : EventArgs
{
    /// <summary>
    /// 页面索引（从 0 开始）
    /// </summary>
    public int PageIndex { get; }

    /// <summary>
    /// 点击 X 坐标
    /// </summary>
    public float X { get; }

    /// <summary>
    /// 点击 Y 坐标
    /// </summary>
    public float Y { get; }

    /// <summary>
    /// 初始化页面点击事件参数
    /// </summary>
    /// <param name="pageIndex">页面索引</param>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    public PdfTappedEventArgs(int pageIndex, float x, float y)
    {
        PageIndex = pageIndex;
        X = x;
        Y = y;
    }
}

/// <summary>
/// 渲染完成事件参数
/// </summary>
public class RenderedEventArgs : EventArgs
{
    /// <summary>
    /// 文档总页数
    /// </summary>
    public int PageCount { get; }

    /// <summary>
    /// 初始化渲染完成事件参数
    /// </summary>
    /// <param name="pageCount">总页数</param>
    public RenderedEventArgs(int pageCount)
    {
        PageCount = pageCount;
    }
}

/// <summary>
/// 注释点击事件参数
/// </summary>
public class AnnotationTappedEventArgs : EventArgs
{
    /// <summary>
    /// 页面索引（从 0 开始）
    /// </summary>
    public int PageIndex { get; }

    /// <summary>
    /// 注释类型
    /// </summary>
    public string? AnnotationType { get; }

    /// <summary>
    /// 注释内容
    /// </summary>
    public string? Contents { get; }

    /// <summary>
    /// 注释边界
    /// </summary>
    public Rect Bounds { get; }

    /// <summary>
    /// 是否已处理（阻止默认行为）
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// 初始化注释点击事件参数
    /// </summary>
    /// <param name="pageIndex">页面索引</param>
    /// <param name="annotationType">注释类型</param>
    /// <param name="contents">注释内容</param>
    /// <param name="bounds">注释边界</param>
    public AnnotationTappedEventArgs(int pageIndex, string? annotationType, string? contents, Rect bounds)
    {
        PageIndex = pageIndex;
        AnnotationType = annotationType;
        Contents = contents;
        Bounds = bounds;
    }
}

/// <summary>
/// 搜索结果事件参数
/// </summary>
public class PdfSearchResultsEventArgs : EventArgs
{
    /// <summary>
    /// 搜索关键词
    /// </summary>
    public string Query { get; }

    /// <summary>
    /// 搜索结果列表
    /// </summary>
    public IReadOnlyList<PdfSearchResult> Results { get; }

    /// <summary>
    /// 当前结果索引
    /// </summary>
    public int CurrentIndex { get; }

    /// <summary>
    /// 是否搜索完成
    /// </summary>
    public bool IsComplete { get; }

    /// <summary>
    /// 初始化搜索结果事件参数
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <param name="results">搜索结果</param>
    /// <param name="currentIndex">当前索引</param>
    /// <param name="isComplete">是否完成</param>
    public PdfSearchResultsEventArgs(string query, IReadOnlyList<PdfSearchResult> results, int currentIndex = 0, bool isComplete = true)
    {
        Query = query;
        Results = results;
        CurrentIndex = currentIndex;
        IsComplete = isComplete;
    }
}

/// <summary>
/// 搜索进度事件参数
/// </summary>
public class PdfSearchProgressEventArgs : EventArgs
{
    /// <summary>
    /// 搜索关键词
    /// </summary>
    public string Query { get; }

    /// <summary>
    /// 当前搜索页面
    /// </summary>
    public int CurrentPage { get; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages { get; }

    /// <summary>
    /// 结果数量
    /// </summary>
    public int ResultCount { get; }

    /// <summary>
    /// 初始化搜索进度事件参数
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <param name="currentPage">当前页面</param>
    /// <param name="totalPages">总页数</param>
    /// <param name="resultCount">结果数量</param>
    public PdfSearchProgressEventArgs(string query, int currentPage, int totalPages, int resultCount)
    {
        Query = query;
        CurrentPage = currentPage;
        TotalPages = totalPages;
        ResultCount = resultCount;
    }
}

/// <summary>
/// 需要密码事件参数
/// </summary>
public class PdfPasswordRequiredEventArgs : EventArgs
{
    /// <summary>
    /// 密码是否正确
    /// </summary>
    public bool IsCorrect { get; set; }

    /// <summary>
    /// 尝试次数
    /// </summary>
    public int AttemptCount { get; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 初始化需要密码事件参数
    /// </summary>
    /// <param name="attemptCount">尝试次数</param>
    public PdfPasswordRequiredEventArgs(int attemptCount = 0)
    {
        AttemptCount = attemptCount;
    }
}

/// <summary>
/// 大纲点击事件参数
/// </summary>
public class PdfOutlineTappedEventArgs : EventArgs
{
    /// <summary>
    /// 大纲项
    /// </summary>
    public PdfOutline Outline { get; }

    /// <summary>
    /// 是否已处理（阻止默认行为）
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// 初始化大纲点击事件参数
    /// </summary>
    /// <param name="outline">大纲项</param>
    public PdfOutlineTappedEventArgs(PdfOutline outline)
    {
        Outline = outline;
    }
}

/// <summary>
/// 缩略图就绪事件参数
/// </summary>
public class PdfThumbnailReadyEventArgs : EventArgs
{
    /// <summary>
    /// 页面索引（从 0 开始）
    /// </summary>
    public int PageIndex { get; }

    /// <summary>
    /// 缩略图流
    /// </summary>
    public Stream? ThumbnailStream { get; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// 初始化缩略图就绪事件参数
    /// </summary>
    /// <param name="pageIndex">页面索引</param>
    /// <param name="thumbnailStream">缩略图流</param>
    /// <param name="success">是否成功</param>
    public PdfThumbnailReadyEventArgs(int pageIndex, Stream? thumbnailStream, bool success)
    {
        PageIndex = pageIndex;
        ThumbnailStream = thumbnailStream;
        Success = success;
    }
}
