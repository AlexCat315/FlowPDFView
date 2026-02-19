namespace Flow.PDFView.DataSources;

/// <summary>
/// 本地文件 PDF 来源实现
/// </summary>
public class FilePdfSource : IPdfSource
{
    private readonly string _filePath;

    /// <summary>
    /// 初始化本地文件 PDF 来源
    /// </summary>
    /// <param name="filePath">PDF 文件路径</param>
    public FilePdfSource(string filePath)
    {
        _filePath = filePath;
    }

    /// <summary>
    /// 获取 PDF 文件路径
    /// </summary>
    /// <returns>PDF 文件路径</returns>
    public Task<string> GetFilePathAsync()
    {
        if (!File.Exists(_filePath))
            throw new FileNotFoundException("File not found", _filePath);
        return Task.FromResult(_filePath);
    }
}