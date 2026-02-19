using System.ComponentModel;
using System.Globalization;

namespace Flow.PDFView.Abstractions;

public class PdfSourceTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || sourceType == typeof(Uri);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        return value switch
        {
            string str => (PdfSource)str,
            Uri uri => (PdfSource)uri,
            _ => base.ConvertFrom(context, culture, value)
        };
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is PdfSource source && destinationType == typeof(string))
        {
            return source switch
            {
                FilePdfSource fileSource => fileSource.FilePath,
                UriPdfSource uriSource => uriSource.Uri.ToString(),
                AssetPdfSource assetSource => assetSource.AssetName,
                StreamPdfSource => throw new NotSupportedException("Cannot convert StreamPdfSource to string"),
                BytesPdfSource => throw new NotSupportedException("Cannot convert BytesPdfSource to string"),
                _ => null
            };
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
