using Microsoft.Maui;

namespace Flow.PDFView.Abstractions;

/// <summary>
/// PDF 搜索结果记录
/// </summary>
public record PdfSearchResult(
    /// <summary>
    /// 页面索引
    /// </summary>
    int PageIndex,

    /// <summary>
    /// 匹配的文本
    /// </summary>
    string Text,

    /// <summary>
    /// 文本边界
    /// </summary>
    Rect Bounds,

    /// <summary>
    /// 匹配索引
    /// </summary>
    int MatchIndex
);

/// <summary>
/// PDF 搜索选项
/// </summary>
public class PdfSearchOptions
{
    /// <summary>
    /// 是否区分大小写
    /// </summary>
    public bool MatchCase { get; set; }

    /// <summary>
    /// 是否全词匹配
    /// </summary>
    public bool WholeWord { get; set; }

    /// <summary>
    /// 是否高亮显示结果
    /// </summary>
    public bool Highlight { get; set; } = true;

    /// <summary>
    /// 是否搜索所有页面
    /// </summary>
    public bool SearchAllPages { get; set; } = true;

    /// <summary>
    /// 最大结果数
    /// </summary>
    public int MaxResults { get; set; } = 1000;
}

/// <summary>
/// PDF 页面文本
/// </summary>
public class PdfPageText
{
    /// <summary>
    /// 页面索引
    /// </summary>
    public int PageIndex { get; init; }

    /// <summary>
    /// 页面文本内容
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// 文本选择列表
    /// </summary>
    public IReadOnlyList<PdfTextSelection>? Selections { get; init; }
}

/// <summary>
/// PDF 文本选择
/// </summary>
public class PdfTextSelection
{
    /// <summary>
    /// 选中的文本
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// 文本边界
    /// </summary>
    public Rect Bounds { get; init; }
}

/// <summary>
/// PDF 大纲项（目录）
/// </summary>
public class PdfOutline
{
    /// <summary>
    /// 大纲标题
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// 目标页面索引
    /// </summary>
    public int? PageIndex { get; init; }

    /// <summary>
    /// 链接 URI
    /// </summary>
    public string? Uri { get; init; }

    /// <summary>
    /// 父大纲项
    /// </summary>
    public PdfOutline? Parent { get; init; }

    /// <summary>
    /// 子大纲列表
    /// </summary>
    public IReadOnlyList<PdfOutline>? Children { get; init; }

    /// <summary>
    /// 大纲类型
    /// </summary>
    public PdfOutlineType Type { get; init; } = PdfOutlineType.GoTo;

    /// <summary>
    /// 缩放级别
    /// </summary>
    public float? Zoom { get; init; }
}

/// <summary>
/// PDF 大纲类型
/// </summary>
public enum PdfOutlineType
{
    /// <summary>
    /// 跳转到指定页面
    /// </summary>
    GoTo,

    /// <summary>
    /// 打开外部链接
    /// </summary>
    URI,

    /// <summary>
    /// 跳转到远程 PDF
    /// </summary>
    GoToRemote,

    /// <summary>
    /// 启动外部应用
    /// </summary>
    Launch,

    /// <summary>
    /// 命名动作
    /// </summary>
    Named
}
