namespace Flow.PDFView.DataSources;

/// <summary>
/// 字节数组 PDF 来源实现
/// </summary>
public class ByteArrayPdfSource : IPdfSource
{
    private readonly byte[] _pdfBytes;

    /// <summary>
    /// 初始化字节数组 PDF 来源
    /// </summary>
    /// <param name="pdfBytes">PDF 文件的字节数组</param>
    public ByteArrayPdfSource(byte[] pdfBytes)
    {
        _pdfBytes = pdfBytes;
    }

    /// <summary>
    /// 将字节数组写入临时文件并返回路径
    /// </summary>
    /// <returns>PDF 文件的临时路径</returns>
    public async Task<string> GetFilePathAsync()
    {
        var tempFile = PdfTempFileHelper.CreateTempPdfFilePath();
        await File.WriteAllBytesAsync(tempFile, _pdfBytes);
        return tempFile;
    }
}