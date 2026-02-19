using System.ComponentModel;

namespace Flow.PDFView.Abstractions;

/// <summary>
/// PDF 来源抽象基类，支持从字符串、URI 隐式转换
/// </summary>
[TypeConverter(typeof(PdfSourceTypeConverter))]
public abstract class PdfSource
{
    /// <summary>
    /// PDF 密码（用于加密文档）
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 从字符串隐式转换为 PDF 来源
    /// 支持：http://、https://、asset://、file:// 前缀，或直接路径
    /// </summary>
    /// <param name="source">源字符串</param>
    /// <returns>PDF 来源实例</returns>
    public static implicit operator PdfSource?(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;

        var trimmedValue = source.Trim();

        if (trimmedValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmedValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return new UriPdfSource(new Uri(trimmedValue));
        }

        if (trimmedValue.StartsWith("asset://", StringComparison.OrdinalIgnoreCase))
        {
            return new AssetPdfSource(trimmedValue["asset://".Length..]);
        }

        if (trimmedValue.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            return new FilePdfSource(new Uri(trimmedValue).LocalPath);
        }

        if (!Path.IsPathRooted(trimmedValue) &&
            !trimmedValue.Contains(Path.DirectorySeparatorChar) &&
            !trimmedValue.Contains(Path.AltDirectorySeparatorChar))
        {
            return new AssetPdfSource(trimmedValue);
        }

        return new FilePdfSource(trimmedValue);
    }

    /// <summary>
    /// 从 URI 隐式转换为 PDF 来源
    /// </summary>
    /// <param name="uri">源 URI</param>
    /// <returns>PDF 来源实例</returns>
    public static implicit operator PdfSource?(Uri? uri)
    {
        if (uri == null)
            return null;

        return new UriPdfSource(uri);
    }

    /// <summary>
    /// 从文件路径创建 PDF 来源
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>PDF 来源实例</returns>
    public static PdfSource FromFile(string filePath)
        => new FilePdfSource(filePath);

    /// <summary>
    /// 从 URI 创建 PDF 来源
    /// </summary>
    /// <param name="uri">URI</param>
    /// <returns>PDF 来源实例</returns>
    public static PdfSource FromUri(Uri uri)
        => new UriPdfSource(uri);

    /// <summary>
    /// 从流创建 PDF 来源
    /// </summary>
    /// <param name="stream">PDF 数据流</param>
    /// <returns>PDF 来源实例</returns>
    public static PdfSource FromStream(Stream stream)
        => new StreamPdfSource(stream);

    /// <summary>
    /// 从字节数组创建 PDF 来源
    /// </summary>
    /// <param name="data">PDF 字节数组</param>
    /// <returns>PDF 来源实例</returns>
    public static PdfSource FromBytes(byte[] data)
        => new BytesPdfSource(data);

    /// <summary>
    /// 从资源名称创建 PDF 来源
    /// </summary>
    /// <param name="assetName">资源名称</param>
    /// <returns>PDF 来源实例</returns>
    public static PdfSource FromAsset(string assetName)
        => new AssetPdfSource(assetName);
}

/// <summary>
/// 本地文件 PDF 来源
/// </summary>
public sealed class FilePdfSource : PdfSource
{
    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// 初始化本地文件 PDF 来源
    /// </summary>
    /// <param name="filePath">PDF 文件路径</param>
    public FilePdfSource(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        FilePath = filePath;
    }
}

/// <summary>
/// URI PDF 来源
/// </summary>
public sealed class UriPdfSource : PdfSource
{
    /// <summary>
    /// URI
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// 初始化 URI PDF 来源
    /// </summary>
    /// <param name="uri">URI</param>
    public UriPdfSource(Uri uri)
    {
        Uri = uri ?? throw new ArgumentNullException(nameof(uri));
    }
}

/// <summary>
/// 流 PDF 来源
/// </summary>
public sealed class StreamPdfSource : PdfSource
{
    /// <summary>
    /// 数据流
    /// </summary>
    public Stream Stream { get; }

    /// <summary>
    /// 初始化流 PDF 来源
    /// </summary>
    /// <param name="stream">PDF 数据流</param>
    public StreamPdfSource(Stream stream)
    {
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }
}

/// <summary>
/// 字节数组 PDF 来源
/// </summary>
public sealed class BytesPdfSource : PdfSource
{
    /// <summary>
    /// PDF 数据
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// 初始化字节数组 PDF 来源
    /// </summary>
    /// <param name="data">PDF 字节数组</param>
    public BytesPdfSource(byte[] data)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        if (data.Length == 0)
            throw new ArgumentException("Data cannot be empty.", nameof(data));
    }
}

/// <summary>
/// 资源文件 PDF 来源
/// </summary>
public sealed class AssetPdfSource : PdfSource
{
    /// <summary>
    /// 资源名称
    /// </summary>
    public string AssetName { get; }

    /// <summary>
    /// 初始化资源文件 PDF 来源
    /// </summary>
    /// <param name="assetName">资源名称</param>
    public AssetPdfSource(string assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName))
            throw new ArgumentException("Asset name cannot be null or empty.", nameof(assetName));

        AssetName = assetName;
    }
}
