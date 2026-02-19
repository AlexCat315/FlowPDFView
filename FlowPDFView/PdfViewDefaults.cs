using Flow.PDFView.Abstractions;

namespace Flow.PDFView;

internal static class PdfViewDefaults
{
    public const bool EnableZoom = true;
    public const bool EnableSwipe = true;
    public const bool EnableTapGestures = false;
    public const bool EnableLinkNavigation = true;

    public const float Zoom = 1.0f;
    public const float MinZoom = 1.0f;
    public const float MaxZoom = 10.0f;
    public const float WindowsMaxZoom = 4.0f;

    public const int PageSpacing = 10;
    public const bool AutoSpacing = false;
    public const bool FitEachPage = false;
    public const bool NightMode = false;
    public const FitPolicy DefaultFitPolicy = FitPolicy.Width;
    public const PdfDisplayMode DefaultDisplayMode = PdfDisplayMode.SinglePageContinuous;
    public const PdfScrollOrientation DefaultScrollOrientation = PdfScrollOrientation.Vertical;
    public const int DefaultPage = 0;

    public const bool EnableAntialiasing = true;
    public const bool UseBestQuality = true;
    public const bool EnableAnnotationRendering = true;

    public const bool IsHorizontal = false;
    public const uint PageIndex = 0;
}
