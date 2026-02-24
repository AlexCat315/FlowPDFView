using System.Net.Http;
using Flow.PDFView.Abstractions;
using Microsoft.Maui.Handlers;
using PdfKit;
using UIKit;
using Foundation;

namespace Flow.PDFView.Platforms.iOS;

/// <summary>
/// en: iOS/macOS handler for mapping PdfView properties to native controls.
/// zh: 用于将 PdfView 属性映射到原生控件的 iOS/macOS 处理器。
/// </summary>
public class PdfViewHandler : ViewHandler<PdfView, PdfKit.PdfView>
{
    private PdfViewiOS? _pdfViewWrapper;

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
        [nameof(PdfView.EnableAnnotationRendering)] = MapEnableAnnotationRendering
    };

    public static CommandMapper<PdfView, PdfViewHandler> CommandMapper = new(ViewCommandMapper)
    {
        [nameof(IPdfView.GoToPage)] = MapGoToPage,
        [nameof(IPdfView.Reload)] = MapReload
    };

    public PdfViewHandler() : base(Mapper, CommandMapper)
    {
    }

    public PdfViewHandler(IPropertyMapper? mapper = null, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper)
    {
    }

    protected override PdfKit.PdfView CreatePlatformView()
    {
        _pdfViewWrapper = new PdfViewiOS();

        _pdfViewWrapper.DocumentLoaded += OnDocumentLoaded;
        _pdfViewWrapper.PageChanged += OnPageChanged;
        _pdfViewWrapper.Error += OnError;
        _pdfViewWrapper.LinkTapped += OnLinkTapped;
        _pdfViewWrapper.Tapped += OnTapped;
        _pdfViewWrapper.Rendered += OnRendered;
        _pdfViewWrapper.AnnotationTapped += OnAnnotationTapped;
        _pdfViewWrapper.SearchResultsFound += OnSearchResultsFound;
        _pdfViewWrapper.SearchProgress += OnSearchProgress;

        return _pdfViewWrapper.NativeView;
    }

    protected override void ConnectHandler(PdfKit.PdfView platformView)
    {
        base.ConnectHandler(platformView);

        if (_pdfViewWrapper == null || VirtualView == null)
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

        VirtualView.SetPlatformFeatures(new IosPlatformFeatures(_pdfViewWrapper));
    }

    protected override void DisconnectHandler(PdfKit.PdfView platformView)
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
            _pdfViewWrapper.AnnotationTapped -= OnAnnotationTapped;
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

    private void OnAnnotationTapped(object? sender, AnnotationTappedEventArgs e)
    {
        VirtualView?.RaiseAnnotationTapped(e);
    }

    private void OnSearchResultsFound(object? sender, PdfSearchResultsEventArgs e)
    {
        VirtualView?.RaiseSearchResultsFound(e);
    }

    private void OnSearchProgress(object? sender, PdfSearchProgressEventArgs e)
    {
        VirtualView?.RaiseSearchProgress(e);
    }

    public static void MapSource(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && !ReferenceEquals(handler._pdfViewWrapper.Source, view.Source))
        {
            handler._pdfViewWrapper.Source = view.Source;
        }
    }

    public static void MapEnableZoom(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.EnableZoom != view.EnableZoom)
        {
            handler._pdfViewWrapper.EnableZoom = view.EnableZoom;
        }
    }

    public static void MapEnableSwipe(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.EnableSwipe != view.EnableSwipe)
        {
            handler._pdfViewWrapper.EnableSwipe = view.EnableSwipe;
        }
    }

    public static void MapEnableTapGestures(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.EnableTapGestures != view.EnableTapGestures)
        {
            handler._pdfViewWrapper.EnableTapGestures = view.EnableTapGestures;
        }
    }

    public static void MapEnableLinkNavigation(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.EnableLinkNavigation != view.EnableLinkNavigation)
        {
            handler._pdfViewWrapper.EnableLinkNavigation = view.EnableLinkNavigation;
        }
    }

    public static void MapZoom(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && Math.Abs(handler._pdfViewWrapper.Zoom - view.Zoom) > float.Epsilon)
        {
            handler._pdfViewWrapper.Zoom = view.Zoom;
        }
    }

    public static void MapMinZoom(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && Math.Abs(handler._pdfViewWrapper.MinZoom - view.MinZoom) > float.Epsilon)
        {
            handler._pdfViewWrapper.MinZoom = view.MinZoom;
        }
    }

    public static void MapMaxZoom(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && Math.Abs(handler._pdfViewWrapper.MaxZoom - view.MaxZoom) > float.Epsilon)
        {
            handler._pdfViewWrapper.MaxZoom = view.MaxZoom;
        }
    }

    public static void MapPageSpacing(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.PageSpacing != view.PageSpacing)
        {
            handler._pdfViewWrapper.PageSpacing = view.PageSpacing;
        }
    }

    public static void MapFitPolicy(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.FitPolicy != view.FitPolicy)
        {
            handler._pdfViewWrapper.FitPolicy = view.FitPolicy;
        }
    }

    public static void MapDisplayMode(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.DisplayMode != view.DisplayMode)
        {
            handler._pdfViewWrapper.DisplayMode = view.DisplayMode;
        }
    }

    public static void MapScrollOrientation(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.ScrollOrientation != view.ScrollOrientation)
        {
            handler._pdfViewWrapper.ScrollOrientation = view.ScrollOrientation;
        }
    }

    public static void MapDefaultPage(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.DefaultPage != view.DefaultPage)
        {
            handler._pdfViewWrapper.DefaultPage = view.DefaultPage;
        }
    }

    public static void MapEnableAntialiasing(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.EnableAntialiasing != view.EnableAntialiasing)
        {
            handler._pdfViewWrapper.EnableAntialiasing = view.EnableAntialiasing;
        }
    }

    public static void MapUseBestQuality(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.UseBestQuality != view.UseBestQuality)
        {
            handler._pdfViewWrapper.UseBestQuality = view.UseBestQuality;
        }
    }

    public static void MapBackgroundColor(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && !Equals(handler._pdfViewWrapper.BackgroundColor, view.BackgroundColor))
        {
            handler._pdfViewWrapper.BackgroundColor = view.BackgroundColor;
        }
    }

    public static void MapEnableAnnotationRendering(PdfViewHandler handler, PdfView view)
    {
        if (handler._pdfViewWrapper != null && handler._pdfViewWrapper.EnableAnnotationRendering != view.EnableAnnotationRendering)
        {
            handler._pdfViewWrapper.EnableAnnotationRendering = view.EnableAnnotationRendering;
        }
    }

    public static void MapGoToPage(PdfViewHandler handler, PdfView view, object? args)
    {
        if (handler._pdfViewWrapper != null && args is int pageIndex)
        {
            handler._pdfViewWrapper.GoToPage(pageIndex);
        }
    }

    public static void MapReload(PdfViewHandler handler, PdfView view, object? args)
    {
        handler._pdfViewWrapper?.Reload();
    }

    private sealed class IosPlatformFeatures : IPdfViewPlatformFeatures
    {
        private readonly PdfViewiOS _wrapper;

        public IosPlatformFeatures(PdfViewiOS wrapper)
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
