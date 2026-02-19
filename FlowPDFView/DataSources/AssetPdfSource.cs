using System.Reflection;
namespace Flow.PDFView.DataSources;

/// <summary>
/// 嵌入资源 PDF 来源实现
/// </summary>
public class AssetPdfSource : IPdfSource
{
    private readonly string _resourcePath;

    /// <summary>
    /// 初始化嵌入资源 PDF 来源
    /// </summary>
    /// <param name="resourcePath">嵌入资源路径（如 "Assets/document.pdf"）</param>
    public AssetPdfSource(string resourcePath)
    {
        _resourcePath = resourcePath;
    }

    /// <summary>
    /// 从嵌入资源读取 PDF 并返回本地临时路径
    /// </summary>
    /// <returns>PDF 文件的临时路径</returns>
    public async Task<string> GetFilePathAsync()
    {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly is null)
        {
            throw new InvalidOperationException("无法获取入口程序集。");
        }

        byte[] bytes;
        await using (Stream? stream = assembly.GetManifestResourceStream(_resourcePath))
        {
            if (stream is null)
            {
                throw new FileNotFoundException($"未找到嵌入资源: {_resourcePath}");
            }

            bytes = new byte[stream.Length];
            // 使用 ReadExactly 确保精确读取，避免 CA2022
            stream.ReadExactly(bytes, 0, bytes.Length);
        }

        var tempFile = PdfTempFileHelper.CreateTempPdfFilePath();
        await File.WriteAllBytesAsync(tempFile, bytes);
        return tempFile;
    }
}
