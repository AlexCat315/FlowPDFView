using Flow.PDFView.Abstractions;
using PdfKit;
using UIKit;
using Foundation;

namespace Flow.PDFView.Platforms.iOS;

/// <summary>
/// en: iOS/macOS platform implementation of the PDF view control.
/// zh: PDF 视图控件的 iOS/macOS 平台实现。
/// </summary>
public class PdfViewiOS : IDisposable
{
    private readonly PdfKit.PdfView _pdfView;
    private PdfSource? _source;
    private bool _disposed;
    private NSObject? _pageChangedObserver;
    private NSObject? _annotationHitObserver;
    private UITapGestureRecognizer? _tapGestureRecognizer;
    private UIPanGestureRecognizer? _shiftScrollZoomRecognizer;
    private nfloat _lastShiftScrollTranslationY;
    private UIScrollView? _nativeScrollView;
    private PdfScrollOrientation _scrollOrientation = PdfViewDefaults.DefaultScrollOrientation;
    private int _defaultPage = PdfViewDefaults.DefaultPage;
    private bool _documentLoaded;
    private bool _enableAnnotationRendering = PdfViewDefaults.EnableAnnotationRendering;
    private bool _enableTapGestures = PdfViewDefaults.EnableTapGestures;
    private bool _enableZoom = PdfViewDefaults.EnableZoom;
    private bool _enableSwipe = PdfViewDefaults.EnableSwipe;
    private List<PdfSearchResult> _searchResults = new();
    private List<PdfSelection> _nativeSearchSelections = new();
    private int _currentSearchIndex = -1;
    private string _currentSearchQuery = string.Empty;
    private bool _highlightSearchResults = true;
    private float _minZoom = PdfViewDefaults.MinZoom;
    private float _maxZoom = PdfViewDefaults.MaxZoom;
    private int _pageSpacing = PdfViewDefaults.PageSpacing;

    public PdfViewiOS()
    {
        _pdfView = new PdfKit.PdfView
        {
            AutoScales = true,
            DisplayMode = PdfKit.PdfDisplayMode.SinglePageContinuous,
            DisplayDirection = PdfDisplayDirection.Vertical
        };

        _pageChangedObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            PdfKit.PdfView.PageChangedNotification,
            OnPageChangedNotification,
            _pdfView);

        _annotationHitObserver = PdfKit.PdfView.Notifications.ObserveAnnotationHit(OnAnnotationHit);

        _pdfView.WeakDelegate = new PdfViewDelegateImpl(this);

        _tapGestureRecognizer = new UITapGestureRecognizer(HandleTap);
        _pdfView.AddGestureRecognizer(_tapGestureRecognizer);

        if (OperatingSystem.IsIOSVersionAtLeast(13, 4))
        {
            _shiftScrollZoomRecognizer = new UIPanGestureRecognizer(HandleShiftScrollZoom)
            {
                AllowedScrollTypesMask = UIScrollTypeMask.All,
                CancelsTouchesInView = false,
                Delegate = new SimultaneousGestureDelegate(),
            };
            _pdfView.AddGestureRecognizer(_shiftScrollZoomRecognizer);
        }
    }

    public PdfKit.PdfView NativeView => _pdfView;

    public PdfSource? Source
    {
        get => _source;
        set
        {
            if (ReferenceEquals(_source, value))
                return;

            _source = value;
            LoadDocument();
        }
    }

    public int CurrentPage => _pdfView.Document != null && _pdfView.CurrentPage != null
        ? (int)_pdfView.Document.GetPageIndex(_pdfView.CurrentPage)
        : 0;

    public int PageCount => _pdfView.Document != null ? (int)_pdfView.Document.PageCount : 0;

    public bool EnableZoom
    {
        get => _enableZoom;
        set
        {
            _enableZoom = value;
            if (value)
            {
                ApplyZoomBounds();
            }
            else
            {
                var currentScale = _pdfView.ScaleFactor;
                _pdfView.MinScaleFactor = currentScale;
                _pdfView.MaxScaleFactor = currentScale;
            }
        }
    }

    public bool EnableSwipe
    {
        get => _enableSwipe;
        set
        {
            if (_enableSwipe == value)
                return;

            _enableSwipe = value;
            UpdateScrollInteraction();
        }
    }

    public bool EnableTapGestures
    {
        get => _enableTapGestures;
        set
        {
            if (_enableTapGestures == value)
                return;

            _enableTapGestures = value;

            if (_tapGestureRecognizer != null)
            {
                if (_enableTapGestures && !_pdfView.GestureRecognizers.Contains(_tapGestureRecognizer))
                {
                    _pdfView.AddGestureRecognizer(_tapGestureRecognizer);
                }
                else if (!_enableTapGestures && _pdfView.GestureRecognizers.Contains(_tapGestureRecognizer))
                {
                    _pdfView.RemoveGestureRecognizer(_tapGestureRecognizer);
                }
            }
        }
    }

    public bool EnableLinkNavigation
    {
        get
        {
#pragma warning disable CA1422
            return _pdfView.EnableDataDetectors;
#pragma warning restore CA1422
        }
        set
        {
#pragma warning disable CA1422
            _pdfView.EnableDataDetectors = value;
#pragma warning restore CA1422
        }
    }

    public float Zoom
    {
        get => (float)_pdfView.ScaleFactor;
        set
        {
            var clamped = Math.Clamp(value, MinZoom, MaxZoom);
            _pdfView.ScaleFactor = clamped;
        }
    }

    public float MinZoom
    {
        get => _minZoom;
        set
        {
            _minZoom = value;
            ApplyZoomBounds();
        }
    }

    public float MaxZoom
    {
        get => _maxZoom;
        set
        {
            _maxZoom = value;
            ApplyZoomBounds();
        }
    }

    public int PageSpacing
    {
        get => _pageSpacing;
        set
        {
            var spacing = Math.Max(0, value);
            if (_pageSpacing == spacing)
            {
                return;
            }

            _pageSpacing = spacing;
            _pdfView.PageBreakMargins = new UIEdgeInsets(spacing, spacing, spacing, spacing);
        }
    }

    public FitPolicy FitPolicy
    {
        get => _pdfView.AutoScales ? FitPolicy.Width : FitPolicy.Both;
        set
        {
            switch (value)
            {
                case FitPolicy.Width:
                    _pdfView.AutoScales = true;
                    _pdfView.DisplayMode = PdfKit.PdfDisplayMode.SinglePageContinuous;
                    break;
                case FitPolicy.Height:
                    _pdfView.AutoScales = true;
                    _pdfView.DisplayMode = PdfKit.PdfDisplayMode.SinglePage;
                    break;
                case FitPolicy.Both:
                    _pdfView.AutoScales = false;
                    break;
            }
        }
    }

    public Abstractions.PdfDisplayMode DisplayMode
    {
        get
        {
            return _pdfView.DisplayMode switch
            {
                PdfKit.PdfDisplayMode.SinglePage => Abstractions.PdfDisplayMode.SinglePage,
                PdfKit.PdfDisplayMode.SinglePageContinuous => Abstractions.PdfDisplayMode.SinglePageContinuous,
                _ => Abstractions.PdfDisplayMode.SinglePageContinuous
            };
        }
        set
        {
            _pdfView.DisplayMode = value switch
            {
                Abstractions.PdfDisplayMode.SinglePage => PdfKit.PdfDisplayMode.SinglePage,
                Abstractions.PdfDisplayMode.SinglePageContinuous => PdfKit.PdfDisplayMode.SinglePageContinuous,
                _ => PdfKit.PdfDisplayMode.SinglePageContinuous
            };
        }
    }

    public PdfScrollOrientation ScrollOrientation
    {
        get => _scrollOrientation;
        set
        {
            _scrollOrientation = value;
            _pdfView.DisplayDirection = value == PdfScrollOrientation.Horizontal
                ? PdfDisplayDirection.Horizontal
                : PdfDisplayDirection.Vertical;
        }
    }

    public int DefaultPage
    {
        get => _defaultPage;
        set => _defaultPage = value;
    }

    public bool EnableAntialiasing { get; set; } = PdfViewDefaults.EnableAntialiasing;
    public bool UseBestQuality { get; set; } = PdfViewDefaults.UseBestQuality;

    public Color? BackgroundColor
    {
        get => _pdfView.BackgroundColor != null
            ? Color.FromRgba(
                _pdfView.BackgroundColor.CGColor.Components[0],
                _pdfView.BackgroundColor.CGColor.Components[1],
                _pdfView.BackgroundColor.CGColor.Components[2],
                _pdfView.BackgroundColor.CGColor.Alpha)
            : null;
        set
        {
            if (value != null)
            {
                _pdfView.BackgroundColor = UIColor.FromRGBA(
                    (float)value.Red,
                    (float)value.Green,
                    (float)value.Blue,
                    (float)value.Alpha);
            }
        }
    }

    public bool EnableAnnotationRendering
    {
        get => _enableAnnotationRendering;
        set
        {
            if (_enableAnnotationRendering != value)
            {
                _enableAnnotationRendering = value;
                UpdateAnnotationVisibility();
            }
        }
    }

    public event EventHandler<DocumentLoadedEventArgs>? DocumentLoaded;
    public event EventHandler<PageChangedEventArgs>? PageChanged;
    public event EventHandler<ViewportChangedEventArgs>? ViewportChanged;
    public event EventHandler<PdfErrorEventArgs>? Error;
    public event EventHandler<LinkTappedEventArgs>? LinkTapped;
    public event EventHandler<PdfTappedEventArgs>? Tapped;
    public event EventHandler<RenderedEventArgs>? Rendered;
    public event EventHandler<AnnotationTappedEventArgs>? AnnotationTapped;
    public event EventHandler<PdfSearchResultsEventArgs>? SearchResultsFound;
    public event EventHandler<PdfSearchProgressEventArgs>? SearchProgress;

    public void GoToPage(int pageIndex)
    {
        if (_pdfView.Document == null)
            return;

        if (pageIndex < 0 || pageIndex >= PageCount)
            return;

        var page = _pdfView.Document.GetPage((nint)pageIndex);
        if (page != null)
        {
            _pdfView.GoToPage(page);
            EmitViewportChanged();
        }
    }

    public void Reload()
    {
        LoadDocument();
    }

    public Task<IReadOnlyList<PdfSearchResult>> SearchAsync(string query, PdfSearchOptions? options = null)
    {
        options ??= new PdfSearchOptions();
        var normalizedQuery = query?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            ClearSearch();
            return Task.FromResult<IReadOnlyList<PdfSearchResult>>(Array.Empty<PdfSearchResult>());
        }

        var document = _pdfView.Document;
        if (document == null || document.PageCount <= 0)
        {
            return Task.FromResult<IReadOnlyList<PdfSearchResult>>(Array.Empty<PdfSearchResult>());
        }

        var results = new List<PdfSearchResult>();
        var nativeSelections = new List<PdfSelection>();
        var totalPages = (int)document.PageCount;
        var maxResults = Math.Max(1, options.MaxResults);
        var compareOptions = options.MatchCase
            ? NSStringCompareOptions.LiteralSearch
            : NSStringCompareOptions.CaseInsensitiveSearch;
        var foundSelections = document.Find(normalizedQuery, compareOptions) ?? Array.Empty<PdfSelection>();
        var matchesByPage = BuildSelectionMatchesByPage(document, foundSelections, normalizedQuery, options);
        int? firstMatchedPage = null;

        for (var pageIndex = 0; pageIndex < totalPages; pageIndex++)
        {
            if (matchesByPage.TryGetValue(pageIndex, out var pageMatches))
            {
                if (!options.SearchAllPages)
                {
                    firstMatchedPage ??= pageIndex;
                    if (pageIndex != firstMatchedPage.Value)
                    {
                        SearchProgress?.Invoke(this, new PdfSearchProgressEventArgs(
                            normalizedQuery,
                            pageIndex + 1,
                            totalPages,
                            results.Count));
                        continue;
                    }
                }

                foreach (var match in pageMatches)
                {
                    results.Add(new PdfSearchResult(
                        pageIndex,
                        match.Selection.Text?.Trim() ?? normalizedQuery,
                        match.Bounds,
                        results.Count));
                    nativeSelections.Add(match.Selection);

                    if (results.Count >= maxResults)
                    {
                        break;
                    }
                }
            }

            SearchProgress?.Invoke(this, new PdfSearchProgressEventArgs(
                normalizedQuery,
                pageIndex + 1,
                totalPages,
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
        _nativeSearchSelections = nativeSelections;
        _currentSearchQuery = normalizedQuery;
        _currentSearchIndex = results.Count > 0 ? 0 : -1;
        if (_highlightSearchResults && options.Highlight)
        {
            ApplySearchHighlights();
        }
        else
        {
            ClearSearchHighlights();
        }

        if (_currentSearchIndex >= 0)
        {
            FocusSearchResult(_currentSearchIndex);
        }

        SearchResultsFound?.Invoke(this, new PdfSearchResultsEventArgs(
            normalizedQuery,
            _searchResults.AsReadOnly(),
            Math.Max(0, _currentSearchIndex),
            true));

        return Task.FromResult<IReadOnlyList<PdfSearchResult>>(_searchResults.AsReadOnly());
    }

    public void ClearSearch()
    {
        _searchResults.Clear();
        _nativeSearchSelections.Clear();
        _currentSearchIndex = -1;
        _currentSearchQuery = string.Empty;
        ClearSearchHighlights();
    }

    public void HighlightSearchResults(bool enable)
    {
        _highlightSearchResults = enable;
        if (_highlightSearchResults)
        {
            ApplySearchHighlights();
        }
        else
        {
            ClearSearchHighlights();
        }
    }

    public void GoToSearchResult(int resultIndex)
    {
        if (resultIndex < 0 || resultIndex >= _searchResults.Count)
        {
            return;
        }

        _currentSearchIndex = resultIndex;
        FocusSearchResult(resultIndex);
        if (_highlightSearchResults)
        {
            ApplySearchHighlights();
        }

        SearchResultsFound?.Invoke(this, new PdfSearchResultsEventArgs(
            _currentSearchQuery,
            _searchResults.AsReadOnly(),
            _currentSearchIndex,
            true));
    }

    private void LoadDocument()
    {
        if (_source == null)
        {
            ClearSearch();
            _pdfView.Document = null;
            return;
        }

        try
        {
            PdfDocument? document = null;

            switch (_source)
            {
                case FilePdfSource fileSource:
                    var fileUrl = NSUrl.FromFilename(fileSource.FilePath);
                    document = new PdfDocument(fileUrl);
                    break;

                case UriPdfSource uriSource:
                    var uri = uriSource.Uri;
                    if (uri != null && (uri.Scheme == "http" || uri.Scheme == "https"))
                    {
                        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"pdf_{Guid.NewGuid()}.pdf");
                        try
                        {
                            var client = new System.Net.Http.HttpClient();
                            var bytes = client.GetByteArrayAsync(uri).GetAwaiter().GetResult();
                            System.IO.File.WriteAllBytes(tempFile, bytes);
                            var fileUrl2 = NSUrl.FromFilename(tempFile);
                            document = new PdfDocument(fileUrl2);
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
                        var url = new NSUrl(uriSource.Uri?.AbsoluteUri ?? "");
                        document = new PdfDocument(url);
                    }
                    break;

                case StreamPdfSource streamSource:
                    var streamData = NSData.FromStream(streamSource.Stream);
                    if (streamData != null)
                        document = new PdfDocument(streamData);
                    break;

                case BytesPdfSource bytesSource:
                    var bytesData = NSData.FromArray(bytesSource.Data);
                    if (bytesData != null)
                        document = new PdfDocument(bytesData);
                    break;

                case AssetPdfSource assetSource:
                    var assetPath = Path.Combine(NSBundle.MainBundle.BundlePath, assetSource.AssetName);
                    if (File.Exists(assetPath))
                    {
                        var assetUrl = NSUrl.FromFilename(assetPath);
                        document = new PdfDocument(assetUrl);
                    }
                    else
                    {
                        var resourcePath = NSBundle.MainBundle.PathForResource(
                            Path.GetFileNameWithoutExtension(assetSource.AssetName),
                            Path.GetExtension(assetSource.AssetName));

                        if (!string.IsNullOrEmpty(resourcePath))
                        {
                            var resourceUrl = NSUrl.FromFilename(resourcePath);
                            document = new PdfDocument(resourceUrl);
                        }
                    }
                    break;
            }

            if (document != null)
            {
                if (document.IsLocked)
                {
                    if (!string.IsNullOrEmpty(_source.Password))
                    {
                        bool unlocked = document.Unlock(_source.Password);
                        if (!unlocked)
                        {
                            OnError(new PdfErrorEventArgs("Failed to unlock PDF: incorrect password"));
                            return;
                        }
                    }
                    else
                    {
                        OnError(new PdfErrorEventArgs("PDF is password-protected but no password was provided"));
                        return;
                    }
                }

                MainThread.BeginInvokeOnMainThread(() => _pdfView.Document = document);
                ApplyZoomBounds();
                UpdateScrollInteraction();
                ClearSearch();

                var pageCount = (int)document.PageCount;
                var title = document.DocumentAttributes?["Title"]?.ToString();
                var author = document.DocumentAttributes?["Author"]?.ToString();
                var subject = document.DocumentAttributes?["Subject"]?.ToString();

                DocumentLoaded?.Invoke(this, new DocumentLoadedEventArgs(pageCount, title, author, subject));

                if (_defaultPage > 0 && _defaultPage < pageCount)
                {
                    var page = document.GetPage((nint)_defaultPage);
                    if (page != null)
                    {
                        _pdfView.GoToPage(page);
                    }
                }

                var currentPageIndex = _defaultPage > 0 && _defaultPage < pageCount ? _defaultPage : 0;
                PageChanged?.Invoke(this, new PageChangedEventArgs(currentPageIndex, pageCount));

                UpdateAnnotationVisibility();

                if (!_documentLoaded)
                {
                    _documentLoaded = true;
                    Task.Delay(100).ContinueWith(_ =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Rendered?.Invoke(this, new RenderedEventArgs(pageCount));
                        });
                    });
                }
            }
            else
            {
                Error?.Invoke(this, new PdfErrorEventArgs("Failed to load PDF document", null));
            }
        }
        catch (Exception ex)
        {
            Error?.Invoke(this, new PdfErrorEventArgs($"Error loading PDF: {ex.Message}", ex));
        }
    }

    private sealed record SelectionMatch(PdfSelection Selection, Rect Bounds);

    private static Dictionary<int, List<SelectionMatch>> BuildSelectionMatchesByPage(
        PdfDocument document,
        IReadOnlyList<PdfSelection> selections,
        string query,
        PdfSearchOptions options)
    {
        var lookup = new Dictionary<int, List<SelectionMatch>>();
        var comparison = options.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        foreach (var selection in selections)
        {
            var page = selection.Pages?.FirstOrDefault();
            if (page == null)
            {
                continue;
            }

            var pageIndex = (int)document.GetPageIndex(page);
            if (pageIndex < 0)
            {
                continue;
            }

            var text = selection.Text?.Trim() ?? string.Empty;
            if (text.Length == 0)
            {
                continue;
            }

            if (options.WholeWord)
            {
                if (!string.Equals(text, query, comparison))
                {
                    continue;
                }
            }
            else if (text.IndexOf(query, comparison) < 0)
            {
                continue;
            }

            var bounds = selection.GetBoundsForPage(page);
            var rect = new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height);

            if (!lookup.TryGetValue(pageIndex, out var list))
            {
                list = new List<SelectionMatch>();
                lookup[pageIndex] = list;
            }

            list.Add(new SelectionMatch(selection, rect));
        }

        return lookup;
    }

    private void FocusSearchResult(int resultIndex)
    {
        if (resultIndex < 0 || resultIndex >= _nativeSearchSelections.Count)
        {
            return;
        }

        var selection = _nativeSearchSelections[resultIndex];
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _pdfView.SetCurrentSelection(selection, true);
            _pdfView.GoToSelection(selection);
        });
    }

    private void ApplySearchHighlights()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_nativeSearchSelections.Count == 0)
            {
                _pdfView.HighlightedSelections = Array.Empty<PdfSelection>();
                return;
            }

            for (var i = 0; i < _nativeSearchSelections.Count; i++)
            {
                _nativeSearchSelections[i].Color = i == _currentSearchIndex
                    ? UIColor.FromRGBA(1f, 0.56f, 0.08f, 0.45f)
                    : UIColor.FromRGBA(1f, 0.92f, 0.23f, 0.35f);
            }

            _pdfView.HighlightedSelections = _nativeSearchSelections.ToArray();
        });
    }

    private void ClearSearchHighlights()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _pdfView.HighlightedSelections = Array.Empty<PdfSelection>();
        });
    }

    private void UpdateAnnotationVisibility()
    {
        if (_pdfView.Document == null)
            return;

        for (nint i = 0; i < _pdfView.Document.PageCount; i++)
        {
            var page = _pdfView.Document.GetPage(i);
            if (page?.Annotations != null)
            {
                foreach (var annotation in page.Annotations)
                {
                    annotation.ShouldDisplay = _enableAnnotationRendering;
                }
            }
        }

        MainThread.BeginInvokeOnMainThread(() => _pdfView.SetNeedsDisplay());
    }

    private void ApplyZoomBounds()
    {
        if (!_enableZoom)
        {
            return;
        }

        var min = Math.Max(0.1f, _minZoom);
        var max = Math.Max(min, _maxZoom);
        _pdfView.MinScaleFactor = min;
        _pdfView.MaxScaleFactor = max;
        _pdfView.ScaleFactor = Math.Clamp((float)_pdfView.ScaleFactor, min, max);
    }

    private void UpdateScrollInteraction()
    {
        AttachScrollViewEvents();
        if (_nativeScrollView == null)
        {
            return;
        }

        _nativeScrollView.ScrollEnabled = _enableSwipe;
        _nativeScrollView.Bounces = _enableSwipe;
        EmitViewportChanged();
    }

    private void AttachScrollViewEvents()
    {
        var scrollView = FindScrollView(_pdfView);
        if (ReferenceEquals(scrollView, _nativeScrollView))
        {
            return;
        }

        if (_nativeScrollView != null)
        {
            _nativeScrollView.Scrolled -= OnNativeScrollViewScrolled;
        }

        _nativeScrollView = scrollView;
        if (_nativeScrollView != null)
        {
            _nativeScrollView.Scrolled += OnNativeScrollViewScrolled;
        }
    }

    private void OnNativeScrollViewScrolled(object? sender, EventArgs e)
    {
        EmitViewportChanged();
    }

    private void EmitViewportChanged()
    {
        var scrollView = _nativeScrollView ?? FindScrollView(_pdfView);
        if (scrollView == null)
        {
            return;
        }

        var zoom = (float)Math.Max(0.1, _pdfView.ScaleFactor);
        ViewportChanged?.Invoke(this, new ViewportChangedEventArgs(
            scrollView.ContentOffset.X,
            scrollView.ContentOffset.Y,
            zoom,
            scrollView.Bounds.Width,
            scrollView.Bounds.Height));
    }

    private static UIScrollView? FindScrollView(UIView root)
    {
        if (root is UIScrollView scrollView)
        {
            return scrollView;
        }

        foreach (var child in root.Subviews)
        {
            var nested = FindScrollView(child);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private void HandleShiftScrollZoom(UIPanGestureRecognizer recognizer)
    {
        if (!OperatingSystem.IsIOSVersionAtLeast(13, 4))
        {
            return;
        }

        if (!EnableZoom)
        {
            return;
        }

        var translationY = recognizer.TranslationInView(_pdfView).Y;

        if (recognizer.State == UIGestureRecognizerState.Began)
        {
            _lastShiftScrollTranslationY = translationY;
            return;
        }

        if (recognizer.State != UIGestureRecognizerState.Changed)
        {
            return;
        }

        if (!recognizer.ModifierFlags.HasFlag(UIKeyModifierFlags.Shift))
        {
            _lastShiftScrollTranslationY = translationY;
            return;
        }

        var deltaY = translationY - _lastShiftScrollTranslationY;
        _lastShiftScrollTranslationY = translationY;
        if (Math.Abs((float)deltaY) < 0.01f)
        {
            return;
        }

        var factor = (float)Math.Exp(-deltaY * 0.015f);
        Zoom = Math.Clamp(Zoom * factor, MinZoom, MaxZoom);
        EmitViewportChanged();
    }

    private void OnPageChangedNotification(NSNotification notification)
    {
        if (_pdfView.Document != null && _pdfView.CurrentPage != null)
        {
            var pageIndex = (int)_pdfView.Document.GetPageIndex(_pdfView.CurrentPage);
            var pageCount = (int)_pdfView.Document.PageCount;

            PageChanged?.Invoke(this, new PageChangedEventArgs(pageIndex, pageCount));
            EmitViewportChanged();
        }
    }

    private void OnAnnotationHit(object? sender, PdfViewAnnotationHitEventArgs e)
    {
        if (_pdfView.Document == null)
            return;

        var userInfo = e.Notification.UserInfo;
        if (userInfo == null)
            return;

        var annotationKey = new NSString("PDFAnnotationHit");
        if (!userInfo.ContainsKey(annotationKey))
            return;

        var annotationObject = userInfo[annotationKey];
        if (annotationObject is PdfAnnotation annotation)
        {
            var page = annotation.Page;
            if (page == null)
                return;

            var pageIndex = (int)_pdfView.Document.GetPageIndex(page);

            string annotationType;
            try
            {
                annotationType = annotation.AnnotationType.ToString();
            }
            catch (NotSupportedException)
            {
                var runtimeName = annotation.GetType()?.Name;
                annotationType = !string.IsNullOrEmpty(runtimeName) ? $"Custom({runtimeName})" : "Unknown";
            }
            var contents = annotation.Contents ?? string.Empty;
            var bounds = annotation.Bounds;

            var args = new AnnotationTappedEventArgs(
                pageIndex,
                annotationType,
                contents,
                new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height)
            );

            AnnotationTapped?.Invoke(this, args);
        }
    }

    private void OnError(PdfErrorEventArgs args)
    {
        Error?.Invoke(this, args);
    }

    private void HandleTap(UITapGestureRecognizer recognizer)
    {
        var location = recognizer.LocationInView(_pdfView);
        var pageIndex = CurrentPage;

        var page = _pdfView.CurrentPage;
        if (page != null)
        {
            var pagePoint = _pdfView.ConvertPointToPage(location, page);
            Tapped?.Invoke(this, new PdfTappedEventArgs(pageIndex, (float)pagePoint.X, (float)pagePoint.Y));
        }
        else
        {
            Tapped?.Invoke(this, new PdfTappedEventArgs(pageIndex, (float)location.X, (float)location.Y));
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_pageChangedObserver != null)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(_pageChangedObserver);
            _pageChangedObserver?.Dispose();
            _pageChangedObserver = null;
        }

        if (_annotationHitObserver != null)
        {
            _annotationHitObserver?.Dispose();
            _annotationHitObserver = null;
        }

        if (_tapGestureRecognizer != null)
        {
            _pdfView.RemoveGestureRecognizer(_tapGestureRecognizer);
            _tapGestureRecognizer?.Dispose();
            _tapGestureRecognizer = null;
        }

        if (_shiftScrollZoomRecognizer != null)
        {
            _pdfView.RemoveGestureRecognizer(_shiftScrollZoomRecognizer);
            _shiftScrollZoomRecognizer.Dispose();
            _shiftScrollZoomRecognizer = null;
        }

        if (_nativeScrollView != null)
        {
            _nativeScrollView.Scrolled -= OnNativeScrollViewScrolled;
            _nativeScrollView = null;
        }

        _pdfView.WeakDelegate = null;
        _pdfView?.Dispose();
    }

    private sealed class SimultaneousGestureDelegate : UIGestureRecognizerDelegate
    {
        public override bool ShouldRecognizeSimultaneously(UIGestureRecognizer gestureRecognizer, UIGestureRecognizer otherGestureRecognizer)
        {
            return true;
        }
    }

    private class PdfViewDelegateImpl : PdfViewDelegate
    {
        private readonly WeakReference<PdfViewiOS> _owner;

        public PdfViewDelegateImpl(PdfViewiOS owner)
        {
            _owner = new WeakReference<PdfViewiOS>(owner);
        }

        [Export("PDFViewWillClickOnLink:withURL:")]
        public override void WillClickOnLink(PdfKit.PdfView sender, NSUrl url)
        {
            if (!_owner.TryGetTarget(out var owner))
                return;

            var args = new LinkTappedEventArgs(url.AbsoluteString, null);
            owner.LinkTapped?.Invoke(owner, args);

            if (!args.Handled && owner.EnableLinkNavigation)
            {
                UIKit.UIApplication.SharedApplication.OpenUrl(url, new UIApplicationOpenUrlOptions(), null);
            }
        }
    }
}
