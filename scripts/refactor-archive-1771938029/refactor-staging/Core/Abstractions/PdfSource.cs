using System.ComponentModel;

namespace Flow.PDFView.Abstractions;

/// <summary>
/// en: Abstract base for PDF sources. Supports implicit conversions from string and URI.
/// zh: PDF 来源抽象基类，支持从字符串和 URI 的隐式转换。
/// </summary>
[TypeConverter(typeof(PdfSourceTypeConverter))]
public abstract class PdfSource
{
    /// <summary>
    /// en: PDF password used for encrypted documents.
    /// zh: PDF 密码（用于加密文档）。
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// en: Implicit conversion from string to PdfSource. Supports http(s)://, asset://, file://, or local paths.
    /// zh: 从字符串隐式转换为 PdfSource。支持 http(s)://、asset://、file:// 前缀或本地路径。
    /// </summary>
    /// <param name="source">en: Source string. zh: 源字符串。</param>
    /// <returns>en: PdfSource instance. zh: PdfSource 实例。</returns>
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
    /// en: Implicit conversion from URI to PdfSource.
    /// zh: 从 URI 隐式转换为 PdfSource。
    /// </summary>
    /// <param name="uri">en: Source URI. zh: 源 URI。</param>
    /// <returns>en: PdfSource instance. zh: PdfSource 实例。</returns>
    public static implicit operator PdfSource?(Uri? uri)
    {
        if (uri == null)
            return null;

        return new UriPdfSource(uri);
    }

    /// <summary>
    /// en: Create PdfSource from a file path.
    /// zh: 从文件路径创建 PdfSource。
    /// </summary>
    /// <param name="filePath">en: File path. zh: 文件路径。</param>
    /// <returns>en: PdfSource instance. zh: PdfSource 实例。</returns>
    public static PdfSource FromFile(string filePath)
        => new FilePdfSource(filePath);

    /// <summary>
    /// en: Create PdfSource from a URI.
    /// zh: 从 URI 创建 PdfSource。
    /// </summary>
    /// <param name="uri">en: URI. zh: URI。</param>
    /// <returns>en: PdfSource instance. zh: PdfSource 实例。</returns>
    public static PdfSource FromUri(Uri uri)
        => new UriPdfSource(uri);

    /// <summary>
    /// en: Create PdfSource from a stream.
    /// zh: 从流创建 PdfSource。
    /// </summary>
    /// <param name="stream">en: PDF data stream. zh: PDF 数据流。</param>
    /// <returns>en: PdfSource instance. zh: PdfSource 实例。</returns>
    public static PdfSource FromStream(Stream stream)
        => new StreamPdfSource(stream);

    /// <summary>
    /// en: Create PdfSource from a byte array.
    /// zh: 从字节数组创建 PdfSource。
    /// </summary>
    /// <param name="data">en: PDF byte array. zh: PDF 字节数组。</param>
    /// <returns>en: PdfSource instance. zh: PdfSource 实例。</returns>
    public static PdfSource FromBytes(byte[] data)
        => new BytesPdfSource(data);

    /// <summary>
    /// en: Create PdfSource from an embedded asset/resource name.
    /// zh: 从资源名称创建 PdfSource（嵌入资源/资产）。
    /// </summary>
    /// <param name="assetName">en: Asset name. zh: 资源名称。</param>
    /// <returns>en: PdfSource instance. zh: PdfSource 实例。</returns>
    public static PdfSource FromAsset(string assetName)
        => new AssetPdfSource(assetName);
}

/// <summary>
/// en: Local file PDF source.
/// zh: 本地文件 PDF 来源。
/// </summary>
public sealed class FilePdfSource : PdfSource
{
    /// <summary>
    /// en: File path to the PDF.
    /// zh: PDF 文件路径。
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// en: Initialize a file-based PdfSource.
    /// zh: 初始化基于文件的 PdfSource。
    /// </summary>
    /// <param name="filePath">en: PDF file path. zh: PDF 文件路径。</param>
    public FilePdfSource(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        FilePath = filePath;
    }
}

/// <summary>
/// en: URI-based PDF source.
/// zh: 基于 URI 的 PDF 来源。
/// </summary>
public sealed class UriPdfSource : PdfSource
{
    /// <summary>
    /// en: Source URI.
    /// zh: 源 URI。
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// en: Initialize a URI-based PdfSource.
    /// zh: 初始化基于 URI 的 PdfSource。
    /// </summary>
    /// <param name="uri">en: URI. zh: URI。</param>
    public UriPdfSource(Uri uri)
    {
        Uri = uri ?? throw new ArgumentNullException(nameof(uri));
    }
}

/// <summary>
/// en: Stream-based PDF source.
/// zh: 基于流的 PDF 来源。
/// </summary>
public sealed class StreamPdfSource : PdfSource
{
    /// <summary>
    /// en: PDF data stream.
    /// zh: PDF 数据流。
    /// </summary>
    public Stream Stream { get; }

    /// <summary>
    /// en: Initialize a stream-based PdfSource.
    /// zh: 初始化基于流的 PdfSource。
    /// </summary>
    /// <param name="stream">en: PDF data stream. zh: PDF 数据流。</param>
    public StreamPdfSource(Stream stream)
    {
        Stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }
}

/// <summary>
/// en: Byte-array based PDF source.
/// zh: 基于字节数组的 PDF 来源。
/// </summary>
public sealed class BytesPdfSource : PdfSource
{
    /// <summary>
    /// en: PDF binary data.
    /// zh: PDF 二进制数据。
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// en: Initialize a byte-array based PdfSource.
    /// zh: 初始化基于字节数组的 PdfSource。
    /// </summary>
    /// <param name="data">en: PDF byte array. zh: PDF 字节数组。</param>
    public BytesPdfSource(byte[] data)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        if (data.Length == 0)
            throw new ArgumentException("Data cannot be empty.", nameof(data));
    }
}

/// <summary>
/// en: Asset/resource-based PDF source.
/// zh: 基于资源/资产的 PDF 来源。
/// </summary>
public sealed class AssetPdfSource : PdfSource
{
    /// <summary>
    /// en: Asset/resource name.
    /// zh: 资源/资产名称。
    /// </summary>
    public string AssetName { get; }

    /// <summary>
    /// en: Initialize an asset-based PdfSource.
    /// zh: 初始化基于资源的 PdfSource。
    /// </summary>
    /// <param name="assetName">en: Asset name. zh: 资源名称。</param>
    public AssetPdfSource(string assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName))
            throw new ArgumentException("Asset name cannot be null or empty.", nameof(assetName));

        AssetName = assetName;
    }
}
