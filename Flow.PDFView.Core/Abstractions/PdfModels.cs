using Microsoft.Maui;
using System.Collections.Generic;

namespace Flow.PDFView.Abstractions
{
    /// <summary>
    /// PDF 搜索结果记录。
    /// Represents a single PDF search result.
    /// </summary>
    public record PdfSearchResult(
        int PageIndex,
        string Text,
        Rect Bounds,
        int MatchIndex
    );

    /// <summary>
    /// PDF 搜索选项。
    /// Options for controlling search behavior.
    /// </summary>
    public class PdfSearchOptions
    {
        /// <summary>是否区分大小写 / match case</summary>
        public bool MatchCase { get; set; }

        /// <summary>是否全词匹配 / whole word match</summary>
        public bool WholeWord { get; set; }

        /// <summary>是否高亮显示结果 / highlight results</summary>
        public bool Highlight { get; set; } = true;

        /// <summary>是否搜索所有页面 / search all pages</summary>
        public bool SearchAllPages { get; set; } = true;

        /// <summary>最大结果数 / maximum results</summary>
        public int MaxResults { get; set; } = 1000;
    }

    /// <summary>
    /// PDF 页面文本。
    /// Represents the extracted text for a page.
    /// </summary>
    public class PdfPageText
    {
        /// <summary>页面索引 / page index</summary>
        public int PageIndex { get; init; }

        /// <summary>页面文本内容 / page text content</summary>
        public string Text { get; init; } = string.Empty;

        /// <summary>文本选择列表 / text selections</summary>
        public IReadOnlyList<PdfTextSelection>? Selections { get; init; }
    }

    /// <summary>
    /// PDF 文本选择。
    /// Represents a text selection within a page.
    /// </summary>
    public class PdfTextSelection
    {
        /// <summary>选中的文本 / selected text</summary>
        public string Text { get; init; } = string.Empty;

        /// <summary>文本边界 / bounds of the selection</summary>
        public Rect Bounds { get; init; }
    }

    /// <summary>
    /// PDF 大纲项（目录）。
    /// Represents an outline (bookmark) entry.
    /// </summary>
    public class PdfOutline
    {
        /// <summary>大纲标题 / outline title</summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>目标页面索引 / target page index</summary>
        public int? PageIndex { get; init; }

        /// <summary>链接 URI / link URI</summary>
        public string? Uri { get; init; }

        /// <summary>父大纲项 / parent outline</summary>
        public PdfOutline? Parent { get; init; }

        /// <summary>子大纲列表 / child outlines</summary>
        public IReadOnlyList<PdfOutline>? Children { get; init; }

        /// <summary>大纲类型 / outline type</summary>
        public PdfOutlineType Type { get; init; } = PdfOutlineType.GoTo;

        /// <summary>缩放级别 / zoom level</summary>
        public float? Zoom { get; init; }
    }

    /// <summary>
    /// PDF 大纲类型。
    /// Types of outline actions.
    /// </summary>
    public enum PdfOutlineType
    {
        /// <summary>跳转到指定页面 / go to page</summary>
        GoTo,

        /// <summary>打开外部链接 / open external URI</summary>
        URI,

        /// <summary>跳转到远程 PDF / go to remote PDF</summary>
        GoToRemote,

        /// <summary>启动外部应用 / launch external application</summary>
        Launch,

        /// <summary>命名动作 / named action</summary>
        Named
    }
}
