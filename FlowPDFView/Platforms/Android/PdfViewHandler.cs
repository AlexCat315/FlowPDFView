using Android.Content;
using Flow.PDFView.Abstractions;
using Microsoft.Maui.Handlers;
using Com.Blaze.Pdfviewer;

namespace Flow.PDFView.Platforms.Android;

public partial class PdfViewHandler : ViewHandler<PdfView, Com.Blaze.Pdfviewer.PDFView>
{
    private PdfViewAndroid? _pdfViewWrapper;

    public static IPropertyMapper<PdfView, PdfViewHandler> Mapper = new PropertyMapper<PdfView, PdfViewHandler>(ViewMapper)
    {
        [nameof(PdfView.Source)] = MapSource,
        [nameof(PdfView.EnableZoom)] = MapEnableZoom,
        [nameof(PdfView.EnableSwipe)] = MapEnableSwipe,
        [nameof(PdfView.EnableTapGestures)] = MapEnableTapGestures,
        [nameof(PdfView.EnableLinkNavigation)] = MapEnableLinkNavigation,
        [nameof(PdfView.Zoom)] = MapZoom,
        [nameof(PdfView.MinZoom)] = MapMinZoom,
        [nameof(PdfView.MaxZoom)] = MapMaxZoom,
        [nameof(PdfView.PageSpacing)] = MapPageSpacing,
        [nameof(PdfView.FitPolicy)] = MapFitPolicy,
        [nameof(PdfView.DisplayMode)] = MapDisplayMode,
        [nameof(PdfView.ScrollOrientation)] = MapScrollOrientation,
        [nameof(PdfView.DefaultPage)] = MapDefaultPage,
        [nameof(PdfView.EnableAntialiasing)] = MapEnableAntialiasing,
        [nameof(PdfView.UseBestQuality)] = MapUseBestQuality,
        [nameof(PdfView.BackgroundColor)] = MapBackgroundColor,
        [nameof(PdfView.EnableAnnotationRendering)] = MapEnableAnnotationRendering,
    };

    public static CommandMapper<PdfView, PdfViewHandler> CommandMapper = new(ViewCommandMapper)
    {
        [nameof(IPdfView.GoToPage)] = MapGoToPage,
        [nameof(IPdfView.Reload)] = MapReload,
    };

    public PdfViewHandler() : base(Mapper, CommandMapper)
    {
    }

    public PdfViewHandler(IPropertyMapper? mapper = null, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper)
    {
    }

    protected override Com.Blaze.Pdfviewer.PDFView CreatePlatformView()
    {
        var context = Context ?? throw new InvalidOperationException("Context is null");
        _pdfViewWrapper = new PdfViewAndroid(context);

        _pdfViewWrapper.DocumentLoaded += OnDocumentLoaded;
        _pdfViewWrapper.PageChanged += OnPageChanged;
        _pdfViewWrapper.Error += OnError;
        _pdfViewWrapper.LinkTapped += OnLinkTapped;
        _pdfViewWrapper.Tapped += OnTapped;
        _pdfViewWrapper.Rendered += OnRendered;
        _pdfViewWrapper.SearchResultsFound += OnSearchResultsFound;
        _pdfViewWrapper.SearchProgress += OnSearchProgress;

        return _pdfViewWrapper.NativeView;
    }

    protected override void ConnectHandler(Com.Blaze.Pdfviewer.PDFView platformView)
    {
        base.ConnectHandler(platformView);

        if (VirtualView == null)
        {
            return;
        }

        MapEnableZoom(this, VirtualView);
        MapEnableSwipe(this, VirtualView);
        MapEnableTapGestures(this, VirtualView);
        MapEnableLinkNavigation(this, VirtualView);
        MapZoom(this, VirtualView);
        MapMinZoom(this, VirtualView);
        MapMaxZoom(this, VirtualView);
        MapPageSpacing(this, VirtualView);
        MapFitPolicy(this, VirtualView);
        MapDisplayMode(this, VirtualView);
        MapScrollOrientation(this, VirtualView);
        MapDefaultPage(this, VirtualView);
        MapEnableAntialiasing(this, VirtualView);
        MapUseBestQuality(this, VirtualView);
        MapBackgroundColor(this, VirtualView);
        MapEnableAnnotationRendering(this, VirtualView);
        MapSource(this, VirtualView);
        VirtualView.SetPlatformFeatures(_pdfViewWrapper == null
            ? null
            : new AndroidPlatformFeatures(_pdfViewWrapper));
    }

    protected override void DisconnectHandler(Com.Blaze.Pdfviewer.PDFView platformView)
    {
        VirtualView?.SetPlatformFeatures(null);

        if (_pdfViewWrapper != null)
        {
            _pdfViewWrapper.DocumentLoaded -= OnDocumentLoaded;
            _pdfViewWrapper.PageChanged -= OnPageChanged;
            _pdfViewWrapper.Error -= OnError;
            _pdfViewWrapper.LinkTapped -= OnLinkTapped;
            _pdfViewWrapper.Tapped -= OnTapped;
            _pdfViewWrapper.Rendered -= OnRendered;
            _pdfViewWrapper.SearchResultsFound -= OnSearchResultsFound;
            _pdfViewWrapper.SearchProgress -= OnSearchProgress;
            _pdfViewWrapper.Dispose();
            _pdfViewWrapper = null;
        }

        base.DisconnectHandler(platformView);
    }

    private void OnDocumentLoaded(object? sender, DocumentLoadedEventArgs e)
    {
        VirtualView?.RaiseDocumentLoaded(e);
    }

    private void OnPageChanged(object? sender, PageChangedEventArgs e)
    {
        VirtualView?.RaisePageChanged(e);
    }

    private void OnError(object? sender, PdfErrorEventArgs e)
    {
        VirtualView?.RaiseError(e);
    }

    private void OnLinkTapped(object? sender, LinkTappedEventArgs e)
    {
        VirtualView?.RaiseLinkTapped(e);
    }

    private void OnTapped(object? sender, PdfTappedEventArgs e)
    {
        VirtualView?.RaiseTapped(e);
    }

    private void OnRendered(object? sender, RenderedEventArgs e)
    {
        VirtualView?.RaiseRendered(e);
    }

    private void OnSearchResultsFound(object? sender, PdfSearchResultsEventArgs e)
    {
        VirtualView?.RaiseSearchResultsFound(e);
    }

    private void OnSearchProgress(object? sender, PdfSearchProgressEventArgs e)
    {
        VirtualView?.RaiseSearchProgress(e);
    }

    private static void MapSource(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && !ReferenceEquals(handler._pdfViewWrapper.Source, view.Source))
        {
            handler._pdfViewWrapper.Source = view.Source;
        }
    }

    private static void MapEnableZoom(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.EnableZoom != view.EnableZoom)
        {
            handler._pdfViewWrapper.EnableZoom = view.EnableZoom;
        }
    }

    private static void MapEnableSwipe(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.EnableSwipe != view.EnableSwipe)
        {
            handler._pdfViewWrapper.EnableSwipe = view.EnableSwipe;
        }
    }

    private static void MapEnableTapGestures(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.EnableTapGestures != view.EnableTapGestures)
        {
            handler._pdfViewWrapper.EnableTapGestures = view.EnableTapGestures;
        }
    }

    private static void MapEnableLinkNavigation(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.EnableLinkNavigation != view.EnableLinkNavigation)
        {
            handler._pdfViewWrapper.EnableLinkNavigation = view.EnableLinkNavigation;
        }
    }

    private static void MapZoom(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && Math.Abs(handler._pdfViewWrapper.Zoom - view.Zoom) > float.Epsilon)
        {
            handler._pdfViewWrapper.Zoom = view.Zoom;
        }
    }

    private static void MapMinZoom(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && Math.Abs(handler._pdfViewWrapper.MinZoom - view.MinZoom) > float.Epsilon)
        {
            handler._pdfViewWrapper.MinZoom = view.MinZoom;
        }
    }

    private static void MapMaxZoom(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && Math.Abs(handler._pdfViewWrapper.MaxZoom - view.MaxZoom) > float.Epsilon)
        {
            handler._pdfViewWrapper.MaxZoom = view.MaxZoom;
        }
    }

    private static void MapPageSpacing(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.PageSpacing != view.PageSpacing)
        {
            handler._pdfViewWrapper.PageSpacing = view.PageSpacing;
        }
    }

    private static void MapFitPolicy(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.FitPolicy != view.FitPolicy)
        {
            handler._pdfViewWrapper.FitPolicy = view.FitPolicy;
        }
    }

    private static void MapDisplayMode(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.DisplayMode != view.DisplayMode)
        {
            handler._pdfViewWrapper.DisplayMode = view.DisplayMode;
        }
    }

    private static void MapScrollOrientation(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.ScrollOrientation != view.ScrollOrientation)
        {
            handler._pdfViewWrapper.ScrollOrientation = view.ScrollOrientation;
        }
    }

    private static void MapDefaultPage(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.DefaultPage != view.DefaultPage)
        {
            handler._pdfViewWrapper.DefaultPage = view.DefaultPage;
        }
    }

    private static void MapEnableAntialiasing(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.EnableAntialiasing != view.EnableAntialiasing)
        {
            handler._pdfViewWrapper.EnableAntialiasing = view.EnableAntialiasing;
        }
    }

    private static void MapUseBestQuality(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.UseBestQuality != view.UseBestQuality)
        {
            handler._pdfViewWrapper.UseBestQuality = view.UseBestQuality;
        }
    }

    private static void MapBackgroundColor(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && !Equals(handler._pdfViewWrapper.BackgroundColor, view.BackgroundColor))
        {
            handler._pdfViewWrapper.BackgroundColor = view.BackgroundColor;
        }
    }

    private static void MapEnableAnnotationRendering(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.EnableAnnotationRendering != view.EnableAnnotationRendering)
        {
            handler._pdfViewWrapper.EnableAnnotationRendering = view.EnableAnnotationRendering;
        }
    }

    private static void MapGoToPage(PdfViewHandler handler, PdfView view, object? args)
    {
        if (handler._pdfViewWrapper != null && args is int pageIndex)
        {
            handler._pdfViewWrapper.GoToPage(pageIndex);
        }
    }

    private static void MapReload(PdfViewHandler handler, PdfView view, object? args)
    {
        handler._pdfViewWrapper?.Reload();
    }

    private sealed class AndroidPlatformFeatures : IPdfViewPlatformFeatures
    {
        private readonly PdfViewAndroid _wrapper;

        public AndroidPlatformFeatures(PdfViewAndroid wrapper)
        {
            _wrapper = wrapper;
        }

        public bool IsSearchSupported => true;

        public Task<IReadOnlyList<PdfSearchResult>> SearchAsync(string query, PdfSearchOptions? options = null)
        {
            return _wrapper.SearchAsync(query, options);
        }

        public void ClearSearch()
        {
            _wrapper.ClearSearch();
        }

        public void HighlightSearchResults(bool enable)
        {
            _wrapper.HighlightSearchResults(enable);
        }

        public void GoToSearchResult(int resultIndex)
        {
            _wrapper.GoToSearchResult(resultIndex);
        }
    }
}
