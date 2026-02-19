namespace Flow.PDFView.DataSources;

/// <summary>
/// PDF 来源接口，定义获取 PDF 文件路径的方法
/// </summary>
public interface IPdfSource
{
    /// <summary>
    /// 异步获取 PDF 文件的本地临时路径
    /// </summary>
    /// <returns>PDF 文件的临时路径</returns>
    Task<string> GetFilePathAsync();
}