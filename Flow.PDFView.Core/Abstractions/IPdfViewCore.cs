using Flow.PDFView.Abstractions;
using System.Windows.Input;

namespace Flow.PDFView.Abstractions
{
    /// <summary>
    /// Core interface (pilot) extracted to Flow.PDFView.Core.
    /// 用于增量提取的核心接口示例。
    /// </summary>
    public interface IPdfViewCore
    {
        PdfSource? Source { get; set; }
        int CurrentPage { get; }
        int PageCount { get; }
        bool EnableZoom { get; set; }
        bool EnableSwipe { get; set; }
        bool EnableLinkNavigation { get; set; }
        bool EnableTapGestures { get; set; }
        float Zoom { get; set; }
        float MinZoom { get; set; }
        float MaxZoom { get; set; }
        int PageSpacing { get; set; }
        FitPolicy FitPolicy { get; set; }
        PdfDisplayMode DisplayMode { get; set; }
        PdfScrollOrientation ScrollOrientation { get; set; }
        int DefaultPage { get; set; }
        bool EnableAntialiasing { get; set; }
        bool UseBestQuality { get; set; }
        Color? BackgroundColor { get; set; }
        bool EnableAnnotationRendering { get; set; }

        event EventHandler<DocumentLoadedEventArgs>? DocumentLoaded;
        event EventHandler<PageChangedEventArgs>? PageChanged;
        event EventHandler<PdfErrorEventArgs>? Error;
        event EventHandler<LinkTappedEventArgs>? LinkTapped;
        event EventHandler<PdfTappedEventArgs>? Tapped;
        event EventHandler<RenderedEventArgs>? Rendered;
        event EventHandler<AnnotationTappedEventArgs>? AnnotationTapped;

        void GoToPage(int pageIndex);
        void Reload();
        bool IsSearchSupported { get; }
        Task<IReadOnlyList<PdfSearchResult>> SearchAsync(string query, PdfSearchOptions? options = null);
        void ClearSearch();
        void HighlightSearchResults(bool enable);
        void GoToSearchResult(int resultIndex);

        event EventHandler<PdfSearchResultsEventArgs>? SearchResultsFound;
        event EventHandler<PdfSearchProgressEventArgs>? SearchProgress;
    }
}
