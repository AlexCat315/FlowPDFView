using Android.Content;
using Android.Util;
using Android.Views;
using Flow.PDFView.Abstractions;
using Java.IO;
using Microsoft.Maui.ApplicationModel;
using AndroidRectF = Android.Graphics.RectF;
using MauiColor = Microsoft.Maui.Graphics.Color;
using Rect = Microsoft.Maui.Graphics.Rect;

namespace Flow.PDFView.Platforms.Android;

public class PdfViewAndroid : IDisposable
{
    private static readonly global::Android.Graphics.Color DefaultPageBackgroundColor =
        global::Android.Graphics.Color.Rgb(0xF1, 0xF5, 0xF9);

    private readonly Com.Blaze.Pdfviewer.PDFView _pdfView;
    private PdfSource? _source;
    private bool _enableZoom = PdfViewDefaults.EnableZoom;
    private bool _enableSwipe = PdfViewDefaults.EnableSwipe;
    private bool _enableLinkNavigation = PdfViewDefaults.EnableLinkNavigation;
    private bool _enableTapGestures = PdfViewDefaults.EnableTapGestures;
    private float _zoom = PdfViewDefaults.Zoom;
    private float _minZoom = PdfViewDefaults.MinZoom;
    private float _maxZoom = PdfViewDefaults.MaxZoom;
    private int _pageSpacing = PdfViewDefaults.PageSpacing;
    private FitPolicy _fitPolicy = PdfViewDefaults.DefaultFitPolicy;
    private PdfDisplayMode _displayMode = PdfViewDefaults.DefaultDisplayMode;
    private PdfScrollOrientation _scrollOrientation = PdfViewDefaults.DefaultScrollOrientation;
    private int _defaultPage = PdfViewDefaults.DefaultPage;
    private bool _enableAntialiasing = PdfViewDefaults.EnableAntialiasing;
    private bool _useBestQuality = PdfViewDefaults.UseBestQuality;
    private MauiColor? _backgroundColor;
    private bool _enableAnnotationRendering = PdfViewDefaults.EnableAnnotationRendering;
    private int _currentPage = PdfViewDefaults.DefaultPage;
    private int _pageCount;
    private bool _isDocumentLoading;
    private bool _pendingReload;
    private int _loadRevision;

    private TapListener? _tapListener;

    private List<PdfSearchResult> _searchResults = new();
    private List<SearchMatchNativeData> _searchMatchesNative = new();
    private int _currentSearchIndex = -1;
    private string _currentSearchQuery = "";
    private bool _highlightSearchResults = true;

    public PdfViewAndroid(Context context)
    {
        _pdfView = new Com.Blaze.Pdfviewer.PDFView(context, null);
        ApplyNativeBackgroundColor();
        ApplyPageSpacingToView();
    }

    public Com.Blaze.Pdfviewer.PDFView NativeView => _pdfView;

    public PdfSource? Source
    {
        get => _source;
        set
        {
            if (_source != value)
            {
                _source = value;
                if (_source == null)
                {
                    ResetNativeDocument();
                    return;
                }

                RequestReload();
            }
        }
    }

    public int CurrentPage => _currentPage;
    public int PageCount => _pageCount;

    public bool EnableZoom
    {
        get => _enableZoom;
        set
        {
            if (!SetField(ref _enableZoom, value))
            {
                return;
            }

            RequestReloadForConfiguration();
        }
    }

    public bool EnableSwipe
    {
        get => _enableSwipe;
        set
        {
            if (!SetField(ref _enableSwipe, value))
            {
                return;
            }

            RequestReloadForConfiguration();
        }
    }

    public bool EnableLinkNavigation
    {
        get => _enableLinkNavigation;
        set
        {
            if (!SetField(ref _enableLinkNavigation, value))
            {
                return;
            }

            RequestReloadForConfiguration();
        }
    }

    public bool EnableTapGestures
    {
        get => _enableTapGestures;
        set
        {
            if (!SetField(ref _enableTapGestures, value))
            {
                return;
            }

            RequestReloadForConfiguration();
        }
    }

    public float Zoom
    {
        get => _zoom;
        set
        {
            if (_zoom != value)
            {
                _zoom = Math.Clamp(value, _minZoom, _maxZoom);
                _pdfView.ZoomTo(_zoom);
            }
        }
    }

    public float MinZoom
    {
        get => _minZoom;
        set
        {
            var normalizedMin = Math.Max(0.1f, value);
            if (Math.Abs(_minZoom - normalizedMin) <= float.Epsilon)
            {
                return;
            }

            _minZoom = normalizedMin;
            if (_maxZoom < _minZoom)
            {
                _maxZoom = _minZoom;
            }

            _zoom = Math.Clamp(_zoom, _minZoom, _maxZoom);
            if (_pageCount > 0 && _enableZoom)
            {
                _pdfView.ZoomTo(_zoom);
            }
        }
    }

    public float MaxZoom
    {
        get => _maxZoom;
        set
        {
            var normalizedMax = Math.Max(Math.Max(0.1f, _minZoom), value);
            if (Math.Abs(_maxZoom - normalizedMax) <= float.Epsilon)
            {
                return;
            }

            _maxZoom = normalizedMax;
            _zoom = Math.Clamp(_zoom, _minZoom, _maxZoom);
            if (_pageCount > 0 && _enableZoom)
            {
                _pdfView.ZoomTo(_zoom);
            }
        }
    }

    public int PageSpacing
    {
        get => _pageSpacing;
        set
        {
            var spacing = Math.Max(0, value);
            if (!SetField(ref _pageSpacing, spacing))
            {
                return;
            }

            ApplyPageSpacingToView();
            RequestReloadForConfiguration();
        }
    }

    public FitPolicy FitPolicy
    {
        get => _fitPolicy;
        set
        {
            if (SetField(ref _fitPolicy, value))
            {
                RequestReloadForConfiguration();
            }
        }
    }

    public PdfDisplayMode DisplayMode
    {
        get => _displayMode;
        set
        {
            if (SetField(ref _displayMode, value))
            {
                RequestReloadForConfiguration();
            }
        }
    }

    public PdfScrollOrientation ScrollOrientation
    {
        get => _scrollOrientation;
        set
        {
            if (SetField(ref _scrollOrientation, value))
            {
                RequestReloadForConfiguration();
            }
        }
    }

    public int DefaultPage
    {
        get => _defaultPage;
        set => _defaultPage = value;
    }

    public bool EnableAntialiasing
    {
        get => _enableAntialiasing;
        set
        {
            if (SetField(ref _enableAntialiasing, value))
            {
                RequestReloadForConfiguration();
            }
        }
    }

    public bool UseBestQuality
    {
        get => _useBestQuality;
        set
        {
            if (SetField(ref _useBestQuality, value))
            {
                RequestReloadForConfiguration();
            }
        }
    }

    public MauiColor? BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            _backgroundColor = value;
            ApplyNativeBackgroundColor();
        }
    }

    public bool EnableAnnotationRendering
    {
        get => _enableAnnotationRendering;
        set
        {
            if (SetField(ref _enableAnnotationRendering, value))
            {
                RequestReloadForConfiguration();
            }
        }
    }

    public event EventHandler<DocumentLoadedEventArgs>? DocumentLoaded;
    public event EventHandler<PageChangedEventArgs>? PageChanged;
    public event EventHandler<PdfErrorEventArgs>? Error;
    public event EventHandler<LinkTappedEventArgs>? LinkTapped;
    public event EventHandler<PdfTappedEventArgs>? Tapped;
    public event EventHandler<RenderedEventArgs>? Rendered;

    public event EventHandler<AnnotationTappedEventArgs>? AnnotationTapped;

    public event EventHandler<PdfSearchResultsEventArgs>? SearchResultsFound;
    public event EventHandler<PdfSearchProgressEventArgs>? SearchProgress;

    public void GoToPage(int pageIndex)
    {
        if (pageIndex >= 0 && pageIndex < _pageCount)
        {
            _pdfView.JumpTo(pageIndex);
        }
    }

    public void Reload()
    {
        RequestReload();
    }

    public async Task<IReadOnlyList<PdfSearchResult>> SearchAsync(string query, PdfSearchOptions? options = null)
    {
        options ??= new PdfSearchOptions();
        var normalizedQuery = query?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            ClearSearch();
            return Array.Empty<PdfSearchResult>();
        }

        if (_pageCount <= 0)
        {
            return Array.Empty<PdfSearchResult>();
        }

        return await Task.Run(() =>
        {
            var results = new List<PdfSearchResult>();
            var nativeMatches = new List<SearchMatchNativeData>();
            var flags = BuildSearchFlags(options);
            var maxResults = Math.Max(1, options.MaxResults);

            for (var pageIndex = 0; pageIndex < _pageCount; pageIndex++)
            {
                _pdfView.OpenPage(pageIndex);
                using var textPage = _pdfView.OpenTextPage(pageIndex);
                if (textPage == null)
                {
                    continue;
                }

                using var findResult = textPage.StartTextSearch(normalizedQuery, flags, 0);
                if (findResult == null)
                {
                    continue;
                }

                while (findResult.FindNext())
                {
                    var startIndex = findResult.SchResultIndex;
                    var length = findResult.SchCount;
                    if (startIndex < 0 || length <= 0)
                    {
                        continue;
                    }

                    var rects = textPage.GetTextRangeRects(new[] { startIndex, length });
                    var androidRects = rects?
                        .Select(r => r.Rect)
                        .Where(r => r != null)
                        .Select(r => new AndroidRectF(r!))
                        .ToList() ?? new List<AndroidRectF>();

                    var firstRect = androidRects.FirstOrDefault();
                    var bounds = firstRect == null
                        ? Rect.Zero
                        : new Rect(firstRect.Left, firstRect.Top, firstRect.Width(), firstRect.Height());

                    var text = textPage.GetText(startIndex, length) ?? normalizedQuery;
                    results.Add(new PdfSearchResult(pageIndex, text, bounds, results.Count));
                    nativeMatches.Add(new SearchMatchNativeData(pageIndex, androidRects));

                    if (results.Count >= maxResults)
                    {
                        break;
                    }
                }

                RaiseSearchProgressOnMainThread(new PdfSearchProgressEventArgs(
                    normalizedQuery,
                    pageIndex + 1,
                    _pageCount,
                    results.Count));

                if (results.Count >= maxResults)
                {
                    break;
                }

                if (!options.SearchAllPages && results.Count > 0)
                {
                    break;
                }
            }

            _searchResults = results;
            _searchMatchesNative = nativeMatches;
            _currentSearchQuery = normalizedQuery;
            _currentSearchIndex = results.Count > 0 ? 0 : -1;

            if (_highlightSearchResults && options.Highlight)
            {
                SyncSearchHighlights();
            }
            else
            {
                _pdfView.ClearSearchHighlights();
            }

            if (_currentSearchIndex >= 0)
            {
                FocusSearchResult(_currentSearchIndex, withAnimation: true);
            }

            RaiseSearchResultsFoundOnMainThread(new PdfSearchResultsEventArgs(
                normalizedQuery,
                _searchResults,
                _currentSearchIndex >= 0 ? _currentSearchIndex : 0,
                true));

            return (IReadOnlyList<PdfSearchResult>)_searchResults.AsReadOnly();
        }).ConfigureAwait(false);
    }

    public void ClearSearch()
    {
        _searchResults.Clear();
        _searchMatchesNative.Clear();
        _currentSearchIndex = -1;
        _currentSearchQuery = "";
        RunOnMainThread(() => _pdfView.ClearSearchHighlights());
    }

    public void HighlightSearchResults(bool enable)
    {
        _highlightSearchResults = enable;
        if (_highlightSearchResults)
        {
            SyncSearchHighlights();
        }
        else
        {
            _pdfView.ClearSearchHighlights();
        }
    }

    public void GoToSearchResult(int resultIndex)
    {
        if (resultIndex >= 0 && resultIndex < _searchResults.Count)
        {
            _currentSearchIndex = resultIndex;
            FocusSearchResult(resultIndex, withAnimation: true);
            SyncSearchHighlights();
            
            RaiseSearchResultsFoundOnMainThread(new PdfSearchResultsEventArgs(
                _currentSearchQuery, 
                _searchResults, 
                resultIndex, 
                true
            ));
        }
    }

    public async Task<PdfPageText?> GetPageTextAsync(int pageIndex)
    {
        return null;
    }

    public Task<string?> GetSelectedTextAsync()
    {
        return Task.FromResult<string?>(null);
    }

    public void ClearSelection()
    {
    }

    public void CopySelection()
    {
    }

    public async Task<IReadOnlyList<PdfOutline>> GetOutlineAsync()
    {
        return Array.Empty<PdfOutline>();
    }

    private List<PdfOutline> ParseOutline(Java.Lang.Object? outline, PdfOutline? parent)
    {
        var result = new List<PdfOutline>();
        return result;
    }

    public void GoToOutline(PdfOutline outline)
    {
        if (outline.PageIndex.HasValue)
        {
            GoToPage(outline.PageIndex.Value);
        }
    }

    public async Task<Stream?> GetThumbnailAsync(int pageIndex, int width, int height)
    {
        return null;
    }

    public int Rotation { get; set; }

    public Task RotatePageAsync(int pageIndex, int degrees)
    {
        return Task.CompletedTask;
    }

    public bool IsPasswordProtected => false;
    public bool IsUnlocked { get; private set; }

    public void Unlock(string password)
    {
        IsUnlocked = true;
    }

    private static HashSet<Com.Blaze.Pdfium.FindFlags> BuildSearchFlags(PdfSearchOptions options)
    {
        var flags = new HashSet<Com.Blaze.Pdfium.FindFlags>();
        if (Com.Blaze.Pdfium.FindFlags.None != null)
        {
            flags.Add(Com.Blaze.Pdfium.FindFlags.None!);
        }
        if (options.MatchCase && Com.Blaze.Pdfium.FindFlags.MatchCase != null)
        {
            flags.Add(Com.Blaze.Pdfium.FindFlags.MatchCase!);
        }
        if (options.WholeWord && Com.Blaze.Pdfium.FindFlags.MatchWholeWord != null)
        {
            flags.Add(Com.Blaze.Pdfium.FindFlags.MatchWholeWord!);
        }
        return flags;
    }

    private void SyncSearchHighlights()
    {
        RunOnMainThread(() =>
        {
            if (!_highlightSearchResults || _searchMatchesNative.Count == 0)
            {
                _pdfView.ClearSearchHighlights();
                return;
            }

            var highlights = _searchMatchesNative
                .Select(match => new Com.Blaze.Pdfviewer.PDFView.SearchHighlight(match.PageIndex, match.Rects.ToList()))
                .ToList();
            _pdfView.SetSearchHighlights(highlights, _currentSearchIndex);
        });
    }

    private void RaiseSearchResultsFoundOnMainThread(PdfSearchResultsEventArgs args)
    {
        if (MainThread.IsMainThread)
        {
            SearchResultsFound?.Invoke(this, args);
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => SearchResultsFound?.Invoke(this, args));
    }

    private void RaiseSearchProgressOnMainThread(PdfSearchProgressEventArgs args)
    {
        if (MainThread.IsMainThread)
        {
            SearchProgress?.Invoke(this, args);
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => SearchProgress?.Invoke(this, args));
    }

    private void FocusSearchResult(int resultIndex, bool withAnimation)
    {
        if (resultIndex < 0 || resultIndex >= _searchMatchesNative.Count)
        {
            return;
        }

        var match = _searchMatchesNative[resultIndex];
        var anchor = match.Rects.FirstOrDefault();
        RunOnMainThread(() =>
        {
            _pdfView.JumpTo(match.PageIndex, withAnimation);

            if (anchor == null)
            {
                return;
            }

            var zoom = Math.Max(0.1f, _pdfView.Zoom);
            var viewWidth = Math.Max(1f, _pdfView.Width);
            var viewHeight = Math.Max(1f, _pdfView.Height);
            var targetX = anchor.CenterX() * zoom;
            var targetY = anchor.CenterY() * zoom;
            var desiredX = viewWidth * 0.35f;
            var desiredY = viewHeight * 0.35f;

            _pdfView.Post(() =>
            {
                if (_scrollOrientation == PdfScrollOrientation.Vertical)
                {
                    _pdfView.MoveRelativeTo(0f, -(targetY - desiredY));
                }
                else
                {
                    _pdfView.MoveRelativeTo(-(targetX - desiredX), 0f);
                }
            });
        });
    }

    private static void RunOnMainThread(Action action)
    {
        if (MainThread.IsMainThread)
        {
            action();
            return;
        }

        MainThread.BeginInvokeOnMainThread(action);
    }

    private void RequestReloadForConfiguration()
    {
        if (_source == null)
        {
            return;
        }

        RequestReload();
    }

    private void RequestReload()
    {
        if (_source == null)
        {
            ResetNativeDocument();
            return;
        }

        if (_isDocumentLoading)
        {
            _pendingReload = true;
            return;
        }

        LoadDocumentCore(_source);
    }

    private void ResetNativeDocument()
    {
        _isDocumentLoading = false;
        _pendingReload = false;
        _pageCount = 0;
        _currentPage = PdfViewDefaults.DefaultPage;
        ClearSearch();
        _pdfView.PostDelayed(() => _pdfView.Recycle(), 100);
    }

    private void LoadDocumentCore(PdfSource source)
    {
        var pageToRestore = _currentPage;
        var loadRevision = unchecked(++_loadRevision);
        _isDocumentLoading = true;
        _pendingReload = false;

        try
        {
            var configurator = source switch
            {
                FilePdfSource fileSource => _pdfView.FromFile(new Java.IO.File(fileSource.FilePath)),
                UriPdfSource uriSource => LoadFromUri(uriSource),
                StreamPdfSource streamSource => _pdfView.FromStream(streamSource.Stream),
                BytesPdfSource bytesSource => _pdfView.FromBytes(bytesSource.Data),
                AssetPdfSource assetSource => _pdfView.FromAsset(assetSource.AssetName),
                _ => throw new NotSupportedException($"PDF source type {source.GetType().Name} is not supported.")
            };

            ConfigureAndLoad(configurator, loadRevision, pageToRestore);
        }
        catch (Exception ex)
        {
            CompleteLoad(loadRevision);
            OnError(new PdfErrorEventArgs($"Failed to load PDF document: {ex.Message}", ex));
        }
    }

    private Com.Blaze.Pdfviewer.PDFView.Configurator LoadFromUri(UriPdfSource uriSource)
    {
        var uri = uriSource.Uri;
        if (uri == null)
        {
            throw new ArgumentException("URI cannot be null");
        }

        if (uri.Scheme == "http" || uri.Scheme == "https")
        {
            var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"pdf_{Guid.NewGuid()}.pdf");
            try
            {
                using var client = new System.Net.Http.HttpClient();
                var bytes = client.GetByteArrayAsync(uri).GetAwaiter().GetResult();
                System.IO.File.WriteAllBytes(tempFile, bytes);
                return _pdfView.FromFile(new Java.IO.File(tempFile));
            }
            catch
            {
                if (System.IO.File.Exists(tempFile))
                {
                    System.IO.File.Delete(tempFile);
                }
                throw;
            }
        }
        else
        {
            return _pdfView.FromUri(global::Android.Net.Uri.Parse(uri.ToString()));
        }
    }

    private void ConfigureAndLoad(Com.Blaze.Pdfviewer.PDFView.Configurator configurator, int loadRevision, int pageToRestore = -1)
    {
        bool enablePageSnap = _displayMode == PdfDisplayMode.SinglePage;
        bool enablePageFling = _displayMode == PdfDisplayMode.SinglePage;
        int spacing = Math.Max(0, _pageSpacing);
        var nativeFitPolicy = ToNativeFitPolicy(_fitPolicy);

        _pdfView.BestQuality = _useBestQuality;
        _pdfView.Antialiasing = _enableAntialiasing;

        if (!string.IsNullOrEmpty(_source?.Password))
        {
            configurator.Password(_source.Password);
        }

        configurator
            .EnableSwipe(_enableSwipe)
            .EnableDoubleTap(_enableZoom)
            .SwipeHorizontal(_scrollOrientation == PdfScrollOrientation.Horizontal)
            .DefaultPage(pageToRestore >= 0 ? pageToRestore : _defaultPage)
            .PageFitPolicy(nativeFitPolicy)
            .AutoSpacing(PdfViewDefaults.AutoSpacing)
            .Spacing(spacing)
            .PageSnap(enablePageSnap)
            .PageFling(enablePageFling)
            .NightMode(PdfViewDefaults.NightMode)
            .FitEachPage(PdfViewDefaults.FitEachPage)
            .EnableAntialiasing(_enableAntialiasing)
            .EnableAnnotationRendering(_enableAnnotationRendering)
            .OnLoad(new LoadCompleteListener(this, loadRevision, pageToRestore))
            .OnPageChange(new PageChangeListener(this))
            .OnError(new ErrorListener(this, loadRevision))
            .OnTap(_enableTapGestures ? _tapListener ??= new TapListener(this) : null)
            .OnRender(new RenderListener(this, loadRevision));

        if (_enableLinkNavigation)
        {
            configurator.LinkHandler(new LinkHandlerImpl(this));
        }

        configurator.Load();
    }

    private void OnDocumentLoaded(int pageCount, int loadRevision)
    {
        if (!IsCurrentLoad(loadRevision) || _source == null)
        {
            return;
        }

        _pageCount = pageCount;
        ApplyConfiguredZoom();
        DocumentLoaded?.Invoke(this, new DocumentLoadedEventArgs(pageCount));
        CompleteLoad(loadRevision);
    }

    private void OnDocumentLoadedWithPageRestore(int pageCount, int pageToRestore, int loadRevision)
    {
        if (!IsCurrentLoad(loadRevision) || _source == null)
        {
            return;
        }

        _pageCount = pageCount;

        if (pageToRestore >= 0 && pageToRestore < pageCount)
        {
            _pdfView.JumpTo(pageToRestore);
        }

        ApplyConfiguredZoom();
        DocumentLoaded?.Invoke(this, new DocumentLoadedEventArgs(pageCount));
        CompleteLoad(loadRevision);
    }

    private void ApplyConfiguredZoom()
    {
        var min = Math.Max(0.1f, _minZoom);
        var max = Math.Max(min, _maxZoom);
        _minZoom = min;
        _maxZoom = max;
        _zoom = Math.Clamp(_zoom, min, max);

        RunOnMainThread(() =>
        {
            var fixedZoom = _enableZoom ? _zoom : (float)Math.Max(0.1f, _pdfView.Zoom);
            _pdfView.MinZoom = _enableZoom ? _minZoom : fixedZoom;
            _pdfView.MaxZoom = _enableZoom ? _maxZoom : fixedZoom;
            _pdfView.ZoomTo(fixedZoom);
        });
    }

    private void OnPageChanged(int pageIndex, int pageCount)
    {
        _currentPage = pageIndex;
        _pageCount = pageCount;
        PageChanged?.Invoke(this, new PageChangedEventArgs(pageIndex, pageCount));
    }

    private void OnError(PdfErrorEventArgs args, int? loadRevision = null)
    {
        if (loadRevision.HasValue)
        {
            if (!IsCurrentLoad(loadRevision.Value))
            {
                return;
            }

            CompleteLoad(loadRevision.Value);
        }

        Error?.Invoke(this, args);
    }

    private void OnLinkTapped(LinkTappedEventArgs args)
    {
        LinkTapped?.Invoke(this, args);
    }

    private void OnTapped(int pageIndex, float x, float y)
    {
        Tapped?.Invoke(this, new PdfTappedEventArgs(pageIndex, x, y));
    }

    private void OnRendered(int pageCount, int loadRevision)
    {
        if (!IsCurrentLoad(loadRevision) || _source == null)
        {
            return;
        }

        Rendered?.Invoke(this, new RenderedEventArgs(pageCount));
    }

    private static bool SetField<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        return true;
    }

    private bool IsCurrentLoad(int loadRevision)
    {
        return loadRevision == _loadRevision;
    }

    private void CompleteLoad(int loadRevision)
    {
        if (!IsCurrentLoad(loadRevision))
        {
            return;
        }

        _isDocumentLoading = false;
        if (!_pendingReload || _source == null)
        {
            return;
        }

        _pendingReload = false;
        LoadDocumentCore(_source);
    }

    private void ApplyPageSpacingToView()
    {
        var metrics = _pdfView.Resources?.DisplayMetrics
            ?? global::Android.Content.Res.Resources.System?.DisplayMetrics
            ?? new DisplayMetrics();
        var spacingPx = (int)Math.Round(TypedValue.ApplyDimension(ComplexUnitType.Dip, _pageSpacing, metrics));
        RunOnMainThread(() =>
        {
            _pdfView.SetPadding(spacingPx, spacingPx, spacingPx, spacingPx);
            _pdfView.SetClipToPadding(false);
            _pdfView.RequestLayout();
            _pdfView.Invalidate();
        });
    }

    private void ApplyNativeBackgroundColor()
    {
        RunOnMainThread(() =>
        {
            var color = _backgroundColor == null
                ? DefaultPageBackgroundColor
                : global::Android.Graphics.Color.Argb(
                    (int)(_backgroundColor.Alpha * 255),
                    (int)(_backgroundColor.Red * 255),
                    (int)(_backgroundColor.Green * 255),
                    (int)(_backgroundColor.Blue * 255));

            _pdfView.SetBackgroundColor(color);
        });
    }

    private static Com.Blaze.Pdfviewer.Util.FitPolicy ToNativeFitPolicy(FitPolicy fitPolicy)
    {
        return fitPolicy switch
        {
            FitPolicy.Height => Com.Blaze.Pdfviewer.Util.FitPolicy.Height!,
            FitPolicy.Both => Com.Blaze.Pdfviewer.Util.FitPolicy.Both!,
            _ => Com.Blaze.Pdfviewer.Util.FitPolicy.Width!,
        };
    }

    public void Dispose()
    {
        if (_tapListener != null)
        {
            _tapListener.Dispose();
            _tapListener = null;
        }

        _pdfView?.Dispose();
    }

    private sealed record SearchMatchNativeData(int PageIndex, IReadOnlyList<AndroidRectF> Rects);

    private class LoadCompleteListener : Java.Lang.Object, Com.Blaze.Pdfviewer.Listener.IOnLoadCompleteListener
    {
        private readonly WeakReference<PdfViewAndroid> _viewRef;
        private readonly int _loadRevision;
        private readonly int _pageToRestore;

        public LoadCompleteListener(PdfViewAndroid view, int loadRevision, int pageToRestore = -1)
        {
            _viewRef = new WeakReference<PdfViewAndroid>(view);
            _loadRevision = loadRevision;
            _pageToRestore = pageToRestore;
        }

        public void LoadComplete(int nbPages)
        {
            if (_viewRef.TryGetTarget(out var view))
            {
                if (_pageToRestore >= 0)
                {
                    view.OnDocumentLoadedWithPageRestore(nbPages, _pageToRestore, _loadRevision);
                }
                else
                {
                    view.OnDocumentLoaded(nbPages, _loadRevision);
                }
            }
        }
    }

    private class PageChangeListener : Java.Lang.Object, Com.Blaze.Pdfviewer.Listener.IOnPageChangeListener
    {
        private readonly WeakReference<PdfViewAndroid> _viewRef;

        public PageChangeListener(PdfViewAndroid view)
        {
            _viewRef = new WeakReference<PdfViewAndroid>(view);
        }

        public void OnPageChanged(int page, int pageCount)
        {
            if (_viewRef.TryGetTarget(out var view))
            {
                view.OnPageChanged(page, pageCount);
            }
        }
    }

    private class ErrorListener : Java.Lang.Object, Com.Blaze.Pdfviewer.Listener.IOnErrorListener
    {
        private readonly WeakReference<PdfViewAndroid> _viewRef;
        private readonly int _loadRevision;

        public ErrorListener(PdfViewAndroid view, int loadRevision)
        {
            _viewRef = new WeakReference<PdfViewAndroid>(view);
            _loadRevision = loadRevision;
        }

        public void OnError(Java.Lang.Throwable? t)
        {
            if (_viewRef.TryGetTarget(out var view))
            {
                var message = t?.Message ?? "Unknown error occurred";
                view.OnError(new PdfErrorEventArgs(message), _loadRevision);
            }
        }
    }

    private class LinkHandlerImpl : Java.Lang.Object, Com.Blaze.Pdfviewer.Link.ILinkHandler
    {
        private readonly WeakReference<PdfViewAndroid> _viewRef;

        public LinkHandlerImpl(PdfViewAndroid view)
        {
            _viewRef = new WeakReference<PdfViewAndroid>(view);
        }

        public void HandleLinkEvent(Com.Blaze.Pdfviewer.Model.LinkTapEvent? linkTapEvent)
        {
            if (_viewRef.TryGetTarget(out var view) && linkTapEvent != null)
            {
                var link = linkTapEvent.Link;
                var args = new LinkTappedEventArgs(link?.Uri, null);

                view.OnLinkTapped(args);

                if (!args.Handled)
                {
                    new Com.Blaze.Pdfviewer.Link.DefaultLinkHandler(view._pdfView).HandleLinkEvent(linkTapEvent);
                }
            }
        }
    }

    private class TapListener : Java.Lang.Object, Com.Blaze.Pdfviewer.Listener.IOnTapListener
    {
        private readonly WeakReference<PdfViewAndroid> _viewRef;

        public TapListener(PdfViewAndroid view)
        {
            _viewRef = new WeakReference<PdfViewAndroid>(view);
        }

        public bool OnTap(MotionEvent? e)
        {
            if (_viewRef.TryGetTarget(out var view) && e != null)
            {
                view.OnTapped(view.CurrentPage, e.GetX(), e.GetY());
            }

            return false;
        }
    }

    private class RenderListener : Java.Lang.Object, Com.Blaze.Pdfviewer.Listener.IOnRenderListener
    {
        private readonly WeakReference<PdfViewAndroid> _viewRef;
        private readonly int _loadRevision;

        public RenderListener(PdfViewAndroid view, int loadRevision)
        {
            _viewRef = new WeakReference<PdfViewAndroid>(view);
            _loadRevision = loadRevision;
        }

        public void OnInitiallyRendered(int nbPages)
        {
            if (_viewRef.TryGetTarget(out var view))
            {
                view.OnRendered(nbPages, _loadRevision);
            }
        }
    }
}
