using Flow.PDFView.Abstractions;
using System.IO;

namespace Flow.PDFView;

internal interface IPdfViewPlatformFeatures
{
    bool IsSearchSupported { get; }

    bool IsPointOnDocument(double viewX, double viewY);

    Task<IReadOnlyList<PdfSearchResult>> SearchAsync(string query, PdfSearchOptions? options = null);

    void ClearSearch();

    void HighlightSearchResults(bool enable);

    void GoToSearchResult(int resultIndex);

    Task<Stream?> GetThumbnailAsync(int pageIndex, int width, int height);

    Task<PdfPageBounds?> GetPageBoundsAsync(int pageIndex);
}

internal sealed class NoopPdfViewPlatformFeatures : IPdfViewPlatformFeatures
{
    public static readonly NoopPdfViewPlatformFeatures Instance = new();

    private NoopPdfViewPlatformFeatures()
    {
    }

    public bool IsSearchSupported => false;

    public bool IsPointOnDocument(double viewX, double viewY)
    {
        return true;
    }

    public Task<IReadOnlyList<PdfSearchResult>> SearchAsync(string query, PdfSearchOptions? options = null)
    {
        return Task.FromResult<IReadOnlyList<PdfSearchResult>>(Array.Empty<PdfSearchResult>());
    }

    public void ClearSearch()
    {
    }

    public void HighlightSearchResults(bool enable)
    {
    }

    public void GoToSearchResult(int resultIndex)
    {
    }

    public Task<Stream?> GetThumbnailAsync(int pageIndex, int width, int height)
    {
        return Task.FromResult<Stream?>(null);
    }

    public Task<PdfPageBounds?> GetPageBoundsAsync(int pageIndex)
    {
        return Task.FromResult<PdfPageBounds?>(null);
    }
}
