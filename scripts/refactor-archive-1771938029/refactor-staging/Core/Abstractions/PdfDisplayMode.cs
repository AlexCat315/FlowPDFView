namespace Flow.PDFView.Abstractions;

/// <summary>
/// PDF 显示模式
/// </summary>
public enum PdfDisplayMode
{
    /// <summary>
    /// 单页模式（每次显示一页，需手动翻页）
    /// </summary>
    SinglePage,

    /// <summary>
    /// 单页连续滚动模式（默认）
    /// </summary>
    SinglePageContinuous
}
