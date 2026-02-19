namespace Flow.PDFView.DataSources;

/// <summary>
/// HTTP URL PDF 来源实现
/// </summary>
public class HttpPdfSource : IPdfSource
{
    private readonly string _url;

    /// <summary>
    /// 初始化 HTTP PDF 来源
    /// </summary>
    /// <param name="url">PDF 文件 URL（支持 http/https）</param>
    public HttpPdfSource(string url)
    {
        _url = url;
    }

    /// <summary>
    /// 从 URL 下载 PDF 并返回本地临时路径
    /// </summary>
    /// <returns>PDF 文件的临时路径</returns>
    public async Task<string> GetFilePathAsync()
    {
        var tempFile = PdfTempFileHelper.CreateTempPdfFilePath();

        // 创建自定义 HttpClientHandler
        var handler = new HttpClientHandler
        {
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12
                         | System.Security.Authentication.SslProtocols.Tls13,

            // 生产环境不要跳过证书验证
            // ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        using var client = new HttpClient(handler);

        try
        {
            var stream = await client.GetStreamAsync(_url);
            await using var fileStream = File.Create(tempFile);
            await stream.CopyToAsync(fileStream);
            return tempFile;
        }
        catch (HttpRequestException ex)
        {
            if (ex.InnerException is System.Security.Authentication.AuthenticationException)
            {
                // 专门处理 TLS 错误
                throw new Exception("TLS 连接失败，请检查服务器SSL配置", ex);
            }
            throw;
        }
    }
}
