using Flow.PDFView.Abstractions;

namespace Flow.PDFView;

internal interface IPdfViewPlatformFeatures
{
    bool IsSearchSupported { get; }

    Task<IReadOnlyList<PdfSearchResult>> SearchAsync(string query, PdfSearchOptions? options = null);

    void ClearSearch();

    void HighlightSearchResults(bool enable);

    void GoToSearchResult(int resultIndex);
}

internal sealed class NoopPdfViewPlatformFeatures : IPdfViewPlatformFeatures
{
    public static readonly NoopPdfViewPlatformFeatures Instance = new();

    private NoopPdfViewPlatformFeatures()
    {
    }

    public bool IsSearchSupported => false;

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
}
