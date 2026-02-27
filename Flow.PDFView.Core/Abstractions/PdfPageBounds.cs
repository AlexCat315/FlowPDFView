namespace Flow.PDFView.Abstractions;

/// <summary>
/// Document-space bounds of a PDF page at zoom 1.0.
/// </summary>
/// <param name="X">Left position in document coordinates.</param>
/// <param name="Y">Top position in document coordinates.</param>
/// <param name="Width">Page width in document coordinates.</param>
/// <param name="Height">Page height in document coordinates.</param>
public readonly record struct PdfPageBounds(double X, double Y, double Width, double Height);
