namespace Flow.PDFView.Abstractions;

/// <summary>
/// 页面适配策略，定义 PDF 页面在视图中的适配方式
/// </summary>
public enum FitPolicy
{
    /// <summary>
    /// 适应宽度
    /// </summary>
    Width,

    /// <summary>
    /// 适应高度
    /// </summary>
    Height,

    /// <summary>
    /// 适应全部（同时适应宽度和高度）
    /// </summary>
    Both
}
