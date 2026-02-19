using System.Net.Http;
using System.Numerics;
using Flow.PDFView.Abstractions;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.System;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;
using UglyToad.PdfPig.Content;
using PigPdfDocument = UglyToad.PdfPig.PdfDocument;

namespace Flow.PDFView.Platforms.Windows;

public class PdfViewHandler : ViewHandler<PdfView, ScrollViewer>
{
    public static readonly PropertyMapper<PdfView, PdfViewHandler> Mapper = new(ViewMapper)
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

        // Compatibility mappings
        [nameof(PdfView.Uri)] = MapUri,
        [nameof(PdfView.IsHorizontal)] = MapIsHorizontal,
        [nameof(PdfView.PageAppearance)] = MapPageAppearance,
        [nameof(PdfView.PageIndex)] = MapPageIndex,
    };

    public static readonly CommandMapper<PdfView, PdfViewHandler> CommandMapper = new(ViewCommandMapper)
    {
        [nameof(IPdfView.GoToPage)] = MapGoToPage,
        [nameof(IPdfView.Reload)] = MapReload,
    };

    private ScrollViewer _scrollViewer = null!;
    private StackPanel _stack = null!;
    private string? _fileName;

    private PageAppearance _pageAppearance = new();

    private bool _isScrolling;
    private bool _isPageIndexLocked;

    private PdfScrollOrientation _scrollOrientation = PdfViewDefaults.DefaultScrollOrientation;
    private bool _enableZoom = PdfViewDefaults.EnableZoom;
    private bool _enableSwipe = PdfViewDefaults.EnableSwipe;
    private bool _enableTapGestures = PdfViewDefaults.EnableTapGestures;
    private bool _enableLinkNavigation = PdfViewDefaults.EnableLinkNavigation;
    private float _zoom = PdfViewDefaults.Zoom;
    private float _minZoom = PdfViewDefaults.MinZoom;
    private float _maxZoom = PdfViewDefaults.WindowsMaxZoom;
    private int _pageSpacing = PdfViewDefaults.PageSpacing;
    private FitPolicy _fitPolicy = PdfViewDefaults.DefaultFitPolicy;
    private PdfDisplayMode _displayMode = PdfViewDefaults.DefaultDisplayMode;
    private int _defaultPage = PdfViewDefaults.DefaultPage;
    private bool _enableAntialiasing = PdfViewDefaults.EnableAntialiasing;
    private bool _useBestQuality = PdfViewDefaults.UseBestQuality;
    private bool _enableAnnotationRendering = PdfViewDefaults.EnableAnnotationRendering;

    private List<PdfSearchResult> _searchResults = new();
    private List<SearchHighlightRegion> _searchHighlightRegions = new();
    private int _currentSearchIndex = -1;
    private string _currentSearchQuery = string.Empty;
    private bool _highlightSearchResults = true;
    private readonly Dictionary<int, Canvas> _pageHighlightCanvases = new();
    private readonly Dictionary<int, Image> _pageImages = new();

    public PdfViewHandler() : base(Mapper, CommandMapper)
    {
    }

    protected override ScrollViewer CreatePlatformView()
    {
        _scrollViewer = new ScrollViewer
        {
            ZoomMode = ZoomMode.Enabled,
            MinZoomFactor = PdfViewDefaults.MinZoom,
            MaxZoomFactor = PdfViewDefaults.WindowsMaxZoom,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };

        _scrollViewer.ViewChanged += OnScrollViewerViewChanged;
        _scrollViewer.PointerWheelChanged += OnScrollViewerPointerWheelChanged;

        _stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
        };

        _scrollViewer.Content = _stack;
        return _scrollViewer;
    }

    protected override void ConnectHandler(ScrollViewer platformView)
    {
        base.ConnectHandler(platformView);

        if (VirtualView == null)
        {
            return;
        }

        VirtualView.SetPlatformFeatures(new WindowsPlatformFeatures(this));

        MapEnableZoom(this, VirtualView);
        MapEnableSwipe(this, VirtualView);
        MapEnableTapGestures(this, VirtualView);
        MapEnableLinkNavigation(this, VirtualView);
        MapMinZoom(this, VirtualView);
        MapMaxZoom(this, VirtualView);
        MapZoom(this, VirtualView);
        MapPageSpacing(this, VirtualView);
        MapFitPolicy(this, VirtualView);
        MapDisplayMode(this, VirtualView);
        MapScrollOrientation(this, VirtualView);
        MapDefaultPage(this, VirtualView);
        MapEnableAntialiasing(this, VirtualView);
        MapUseBestQuality(this, VirtualView);
        MapBackgroundColor(this, VirtualView);
        MapEnableAnnotationRendering(this, VirtualView);
        MapPageAppearance(this, VirtualView);
        MapSource(this, VirtualView);
        MapUri(this, VirtualView);
    }

    protected override void DisconnectHandler(ScrollViewer platformView)
    {
        VirtualView?.SetPlatformFeatures(null);

        if (_scrollViewer != null)
        {
            _scrollViewer.ViewChanged -= OnScrollViewerViewChanged;
            _scrollViewer.PointerWheelChanged -= OnScrollViewerPointerWheelChanged;
        }

        base.DisconnectHandler(platformView);
    }

    private static void MapSource(PdfViewHandler handler, PdfView pdfView)
    {
        if (pdfView.Source == null)
        {
            handler._fileName = null;
            handler._stack.Children.Clear();
            handler._pageHighlightCanvases.Clear();
            handler._pageImages.Clear();
            handler.ClearSearchInternal();
            return;
        }

        _ = handler.LoadSourceAsync(pdfView.Source);
    }

    private static void MapUri(PdfViewHandler handler, PdfView pdfView)
    {
        if (pdfView.Source != null)
        {
            return;
        }

        var uri = pdfView.Uri;
        if (string.IsNullOrWhiteSpace(uri))
        {
            return;
        }

        _ = handler.LoadFromUriAsync(uri);
    }

    private static void MapIsHorizontal(PdfViewHandler handler, PdfView pdfView)
    {
        handler._scrollOrientation = pdfView.IsHorizontal
            ? PdfScrollOrientation.Horizontal
            : PdfScrollOrientation.Vertical;
        handler.ApplyScrollSettings();
    }

    private static void MapScrollOrientation(PdfViewHandler handler, PdfView pdfView)
    {
        handler._scrollOrientation = pdfView.ScrollOrientation;
        handler.ApplyScrollSettings();
    }

    private static void MapEnableZoom(PdfViewHandler handler, PdfView pdfView)
    {
        handler._enableZoom = pdfView.EnableZoom;
        handler.ApplyZoomSettings();
    }

    private static void MapEnableSwipe(PdfViewHandler handler, PdfView pdfView)
    {
        handler._enableSwipe = pdfView.EnableSwipe;
        handler.ApplyScrollSettings();
    }

    private static void MapEnableTapGestures(PdfViewHandler handler, PdfView pdfView)
    {
        handler._enableTapGestures = pdfView.EnableTapGestures;
    }

    private static void MapEnableLinkNavigation(PdfViewHandler handler, PdfView pdfView)
    {
        handler._enableLinkNavigation = pdfView.EnableLinkNavigation;
    }

    private static void MapZoom(PdfViewHandler handler, PdfView pdfView)
    {
        handler._zoom = pdfView.Zoom;
        handler.ApplyZoomSettings();
    }

    private static void MapMinZoom(PdfViewHandler handler, PdfView pdfView)
    {
        handler._minZoom = pdfView.MinZoom;
        handler.ApplyZoomSettings();
    }

    private static void MapMaxZoom(PdfViewHandler handler, PdfView pdfView)
    {
        handler._maxZoom = pdfView.MaxZoom;
        handler.ApplyZoomSettings();
    }

    private static void MapPageSpacing(PdfViewHandler handler, PdfView pdfView)
    {
        handler._pageSpacing = Math.Max(0, pdfView.PageSpacing);
        handler.ApplyPageSpacing();
    }

    private static void MapFitPolicy(PdfViewHandler handler, PdfView pdfView)
    {
        handler._fitPolicy = pdfView.FitPolicy;
        handler.ApplyFitPolicy();
    }

    private static void MapDisplayMode(PdfViewHandler handler, PdfView pdfView)
    {
        handler._displayMode = pdfView.DisplayMode;
    }

    private static void MapDefaultPage(PdfViewHandler handler, PdfView pdfView)
    {
        handler._defaultPage = pdfView.DefaultPage;
    }

    private static void MapEnableAntialiasing(PdfViewHandler handler, PdfView pdfView)
    {
        handler._enableAntialiasing = pdfView.EnableAntialiasing;
    }

    private static void MapUseBestQuality(PdfViewHandler handler, PdfView pdfView)
    {
        handler._useBestQuality = pdfView.UseBestQuality;
    }

    private static void MapBackgroundColor(PdfViewHandler handler, PdfView pdfView)
    {
        if (pdfView.BackgroundColor == null)
        {
            return;
        }

        handler._scrollViewer.Background = new SolidColorBrush(ToNativeColor(pdfView.BackgroundColor));
    }

    private static void MapEnableAnnotationRendering(PdfViewHandler handler, PdfView pdfView)
    {
        handler._enableAnnotationRendering = pdfView.EnableAnnotationRendering;
    }

    private static void MapPageAppearance(PdfViewHandler handler, PdfView pdfView)
    {
        handler._pageAppearance = pdfView.PageAppearance ?? new PageAppearance();
        handler.ApplyPageSpacing();
    }

    private static void MapPageIndex(PdfViewHandler handler, PdfView pdfView)
    {
        handler.GotoPage(pdfView.PageIndex);
    }

    private static void MapGoToPage(PdfViewHandler handler, PdfView pdfView, object? args)
    {
        if (args is int pageIndex)
        {
            handler.GotoPage((uint)Math.Max(pageIndex, 0));
        }
    }

    private static void MapReload(PdfViewHandler handler, PdfView pdfView, object? args)
    {
        if (pdfView.Source != null)
        {
            _ = handler.LoadSourceAsync(pdfView.Source);
            return;
        }

        if (!string.IsNullOrWhiteSpace(pdfView.Uri))
        {
            _ = handler.LoadFromUriAsync(pdfView.Uri);
        }
    }

    private static Microsoft.UI.Color ToNativeColor(Color color)
    {
        return Microsoft.UI.Color.FromArgb(
            (byte)Math.Clamp(color.Alpha * 255, 0, 255),
            (byte)Math.Clamp(color.Red * 255, 0, 255),
            (byte)Math.Clamp(color.Green * 255, 0, 255),
            (byte)Math.Clamp(color.Blue * 255, 0, 255));
    }

    private async Task LoadSourceAsync(PdfSource source)
    {
        try
        {
            switch (source)
            {
                case FilePdfSource fileSource:
                    _fileName = fileSource.FilePath;
                    break;
                case UriPdfSource uriSource:
                    if (uriSource.Uri == null)
                    {
                        RaiseError("PDF URI 为空");
                        return;
                    }

                    await LoadFromUriAsync(uriSource.Uri.AbsoluteUri);
                    return;
                case AssetPdfSource assetSource:
                    await using (var assetStream = await FileSystem.Current.OpenAppPackageFileAsync(assetSource.AssetName))
                    {
                        _fileName = await WriteTempFileAsync(assetStream);
                    }
                    break;
                case BytesPdfSource bytesSource:
                    _fileName = await WriteTempFileAsync(new MemoryStream(bytesSource.Data));
                    break;
                case StreamPdfSource streamSource:
                    _fileName = await WriteTempFileAsync(streamSource.Stream);
                    break;
                default:
                    RaiseError($"不支持的数据源类型: {source.GetType().Name}");
                    return;
            }

            await RenderPagesAsync();
        }
        catch (Exception ex)
        {
            RaiseError($"加载 PDF 失败: {ex.Message}", ex);
        }
    }

    private async Task LoadFromUriAsync(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            _fileName = null;
            _stack.Children.Clear();
            _pageHighlightCanvases.Clear();
            _pageImages.Clear();
            ClearSearchInternal();
            return;
        }

        if (uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var client = new HttpClient();
                await using var stream = await client.GetStreamAsync(uri);
                _fileName = await WriteTempFileAsync(stream);
                await RenderPagesAsync();
            }
            catch (Exception ex)
            {
                RaiseError($"下载 PDF 失败: {ex.Message}", ex);
            }

            return;
        }

        _fileName = uri;
        await RenderPagesAsync();
    }

    private static async Task<string> WriteTempFileAsync(Stream source)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"pdf_{Guid.NewGuid():N}.pdf");
        await using var file = File.Create(tempFile);

        if (source.CanSeek)
        {
            source.Seek(0, SeekOrigin.Begin);
        }

        await source.CopyToAsync(file);
        await file.FlushAsync();

        return tempFile;
    }

    private async Task RenderPagesAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            _stack.Children.Clear();
            _pageHighlightCanvases.Clear();
            _pageImages.Clear();
            _scrollViewer.ZoomToFactor(1f);
        });

        if (string.IsNullOrWhiteSpace(_fileName))
        {
            return;
        }

        if (!File.Exists(_fileName))
        {
            RaiseError($"PDF 文件不存在: {_fileName}");
            return;
        }

        try
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(_fileName);
            var pdfDoc = await PdfDocument.LoadFromFileAsync(storageFile);

            var renderedPages = new List<UIElement>();

            for (uint i = 0; i < pdfDoc.PageCount; i++)
            {
                using var page = pdfDoc.GetPage(i);
                using var stream = new InMemoryRandomAccessStream();

                var crop = _pageAppearance.Crop;
                var logicalDpi = DeviceDisplay.MainDisplayInfo.Density * 96;
                var scale = logicalDpi / 72.0;

                var sourceRect = new Windows.Foundation.Rect(
                    Math.Max(0, crop.Left * scale),
                    Math.Max(0, crop.Top * scale),
                    Math.Max(1, page.Size.Width - ((crop.Right + crop.Left) * scale)),
                    Math.Max(1, page.Size.Height - ((crop.Bottom + crop.Top) * scale)));

                var renderOptions = new PdfPageRenderOptions
                {
                    SourceRect = sourceRect,
                };

                var applyAntialiasing = _enableAntialiasing;
                var includeAnnotations = _enableAnnotationRendering;
                _ = applyAntialiasing;
                _ = includeAnnotations;

                if (_useBestQuality)
                {
                    renderOptions.DestinationWidth = (uint)Math.Max(1, sourceRect.Width * scale);
                    renderOptions.DestinationHeight = (uint)Math.Max(1, sourceRect.Height * scale);
                }

                await page.RenderToStreamAsync(stream, renderOptions);

                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);
                renderedPages.Add(MakePage(bitmap, i));
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _stack.Children.Clear();
                foreach (var pageElement in renderedPages)
                {
                    _stack.Children.Add(pageElement);
                }

                ApplyScrollSettings();
                ApplyZoomSettings();
                ApplyPageSpacing();
                ApplyFitPolicy();
                ApplySearchHighlightsVisual();

                var pageCount = (int)pdfDoc.PageCount;
                VirtualView?.RaiseDocumentLoaded(new DocumentLoadedEventArgs(pageCount));
                VirtualView?.RaiseRendered(new RenderedEventArgs(pageCount));

                var startPage = Math.Clamp(
                    Math.Max(_defaultPage, 0),
                    0,
                    Math.Max(0, pageCount - 1));

                if (pageCount > 0)
                {
                    if (startPage > 0)
                    {
                        GotoPage((uint)startPage);
                    }

                    VirtualView?.RaisePageChanged(new Abstractions.PageChangedEventArgs(startPage, pageCount));
                }
            });
        }
        catch (Exception ex)
        {
            RaiseError($"渲染 PDF 失败: {ex.Message}", ex);
        }
    }

    private Grid MakePage(BitmapImage image, uint pageIndex)
    {
        var border = new Grid
        {
            Margin = BuildPageMargin(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var imageControl = new Image
        {
            Source = image,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var overlayCanvas = new Canvas
        {
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var pageIndexInt = (int)pageIndex;
        _pageImages[pageIndexInt] = imageControl;
        _pageHighlightCanvases[pageIndexInt] = overlayCanvas;

        imageControl.SizeChanged += (_, _) =>
        {
            overlayCanvas.Width = imageControl.ActualWidth;
            overlayCanvas.Height = imageControl.ActualHeight;
            RenderHighlightsForPage(pageIndexInt);
        };

        border.Children.Add(imageControl);
        border.Children.Add(overlayCanvas);

        if (_pageAppearance.ShadowEnabled)
        {
            AddShadow(border, 24);
        }

        border.Tapped += (_, args) =>
        {
            if (!_enableTapGestures)
            {
                return;
            }

            if (!_enableLinkNavigation)
            {
                // Windows 当前位图渲染链路没有链接元数据，仅触发点击事件。
            }

            var point = args.GetPosition(border);
            VirtualView?.RaiseTapped(new PdfTappedEventArgs((int)pageIndex, (float)point.X, (float)point.Y));
        };

        return border;
    }

    private Thickness BuildPageMargin()
    {
        var appearanceMargin = _pageAppearance.Margin;
        var spacing = _pageSpacing / 2.0;

        return new Thickness(
            appearanceMargin.Left + spacing,
            appearanceMargin.Top + spacing,
            appearanceMargin.Right + spacing,
            appearanceMargin.Bottom + spacing);
    }

    private void ApplySearchHighlightsVisual()
    {
        foreach (var pageIndex in _pageHighlightCanvases.Keys)
        {
            RenderHighlightsForPage(pageIndex);
        }
    }

    private void ClearSearchHighlightsVisual()
    {
        foreach (var canvas in _pageHighlightCanvases.Values)
        {
            canvas.Children.Clear();
        }
    }

    private void RenderHighlightsForPage(int pageIndex)
    {
        if (!_pageHighlightCanvases.TryGetValue(pageIndex, out var canvas) ||
            !_pageImages.TryGetValue(pageIndex, out var imageControl))
        {
            return;
        }

        canvas.Children.Clear();
        if (!_highlightSearchResults)
        {
            return;
        }

        var imageWidth = imageControl.ActualWidth;
        var imageHeight = imageControl.ActualHeight;
        if (imageWidth <= 0 || imageHeight <= 0)
        {
            return;
        }

        canvas.Width = imageWidth;
        canvas.Height = imageHeight;

        foreach (var region in _searchHighlightRegions.Where(r => r.PageIndex == pageIndex))
        {
            var width = Math.Max(2d, region.WidthNormalized * imageWidth);
            var height = Math.Max(2d, region.HeightNormalized * imageHeight);
            var left = Math.Clamp(region.XNormalized * imageWidth, 0d, Math.Max(0d, imageWidth - width));
            var top = Math.Clamp(region.YNormalized * imageHeight, 0d, Math.Max(0d, imageHeight - height));

            var isCurrent = region.ResultIndex == _currentSearchIndex;
            var rect = new Rectangle
            {
                Width = width,
                Height = height,
                RadiusX = 2,
                RadiusY = 2,
                IsHitTestVisible = false,
                Fill = new SolidColorBrush(isCurrent
                    ? Microsoft.UI.Color.FromArgb(170, 255, 143, 20)
                    : Microsoft.UI.Color.FromArgb(125, 255, 235, 60)),
                Stroke = new SolidColorBrush(isCurrent
                    ? Microsoft.UI.Color.FromArgb(220, 216, 96, 0)
                    : Microsoft.UI.Color.FromArgb(180, 180, 150, 0)),
                StrokeThickness = 1,
            };

            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);
            canvas.Children.Add(rect);
        }
    }

    public static void AddShadow(Grid target, int zDepth)
    {
        var shadowReceiver = new Border { Margin = new Thickness(-zDepth) };
        target.Children.Insert(0, shadowReceiver);

        var sharedShadow = new ThemeShadow();
        target.Shadow = sharedShadow;
        sharedShadow.Receivers.Add(shadowReceiver);

        target.Translation += new Vector3(0, 0, zDepth);
    }

    private void ApplyPageSpacing()
    {
        var margin = BuildPageMargin();
        foreach (var child in _stack.Children)
        {
            if (child is Grid grid)
            {
                grid.Margin = margin;
            }
        }
    }

    private void ApplyFitPolicy()
    {
        foreach (var child in _stack.Children)
        {
            if (child is not Grid grid)
            {
                continue;
            }

            var image = grid.Children.OfType<Image>().FirstOrDefault();
            if (image == null)
            {
                continue;
            }

            image.Stretch = _fitPolicy switch
            {
                FitPolicy.Both => Stretch.Fill,
                _ => Stretch.Uniform,
            };
        }
    }

    private void ApplyScrollSettings()
    {
        _stack.Orientation = _scrollOrientation == PdfScrollOrientation.Horizontal
            ? Orientation.Horizontal
            : Orientation.Vertical;

        if (_displayMode == PdfDisplayMode.SinglePage)
        {
            // Windows 当前位图渲染链路先保持连续容器，单页视觉优化后续补充。
        }

        if (_enableSwipe)
        {
            _scrollViewer.HorizontalScrollMode = ScrollMode.Enabled;
            _scrollViewer.VerticalScrollMode = ScrollMode.Enabled;
            _scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }
        else
        {
            _scrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
            _scrollViewer.VerticalScrollMode = ScrollMode.Disabled;
            _scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        }
    }

    private void ApplyZoomSettings()
    {
        var min = Math.Max(0.1f, _minZoom);
        var max = Math.Max(min, _maxZoom);

        _scrollViewer.MinZoomFactor = min;
        _scrollViewer.MaxZoomFactor = max;
        _scrollViewer.ZoomMode = _enableZoom ? ZoomMode.Enabled : ZoomMode.Disabled;

        var target = _enableZoom ? Math.Clamp(_zoom, min, max) : 1f;
        _scrollViewer.ZoomToFactor(target);
    }

    private void GotoPage(uint pageIndex)
    {
        if (_isScrolling || pageIndex >= _stack.Children.Count)
        {
            return;
        }

        if (_stack.Children[(int)pageIndex] is not FrameworkElement child)
        {
            return;
        }

        _isPageIndexLocked = true;

        if (_scrollOrientation == PdfScrollOrientation.Horizontal)
        {
            _scrollViewer.ChangeView(_scrollViewer.ZoomFactor * child.ActualOffset.X, null, null);
        }
        else
        {
            _scrollViewer.ChangeView(null, _scrollViewer.ZoomFactor * child.ActualOffset.Y, null);
        }
    }

    private void OnScrollViewerViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_stack.Children.Count == 0)
        {
            return;
        }

        var currentPage = -1;
        var maxVisibleSize = 0.0;

        for (var i = 0; i < _stack.Children.Count; i++)
        {
            if (_stack.Children[i] is not FrameworkElement child)
            {
                continue;
            }

            var transform = child.TransformToVisual(_scrollViewer);
            var position = transform.TransformBounds(new Windows.Foundation.Rect(0, 0, child.ActualWidth, child.ActualHeight));

            var isVisible = _scrollOrientation == PdfScrollOrientation.Horizontal
                ? (position.Right >= 0 && position.Left <= _scrollViewer.ViewportWidth)
                : (position.Bottom >= 0 && position.Top <= _scrollViewer.ViewportHeight);

            if (!isVisible)
            {
                continue;
            }

            var visibleSize = _scrollOrientation == PdfScrollOrientation.Horizontal
                ? Math.Min(position.Right, _scrollViewer.ViewportWidth) - Math.Max(position.Left, 0)
                : Math.Min(position.Bottom, _scrollViewer.ViewportHeight) - Math.Max(position.Top, 0);

            if (visibleSize > maxVisibleSize)
            {
                maxVisibleSize = visibleSize;
                currentPage = i;
            }
        }

        if (currentPage < 0)
        {
            return;
        }

        var newPageIndex = (uint)currentPage;
        if (_isPageIndexLocked)
        {
            _isPageIndexLocked = false;
        }
        else if (VirtualView != null && VirtualView.PageIndex != newPageIndex)
        {
            _isScrolling = true;
            VirtualView.PageIndex = newPageIndex;
            _isScrolling = false;
        }

        VirtualView?.RaisePageChanged(new Abstractions.PageChangedEventArgs(currentPage, _stack.Children.Count));

        if (VirtualView?.PageChangedCommand?.CanExecute(null) == true)
        {
            VirtualView.PageChangedCommand.Execute(new Flow.PDFView.Events.PageChangedEventArgs(currentPage, _stack.Children.Count));
        }
    }

    private void OnScrollViewerPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (!_enableZoom)
        {
            return;
        }

        var keyState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
        if ((keyState & Windows.UI.Core.CoreVirtualKeyStates.Down) == 0)
        {
            return;
        }

        var delta = e.GetCurrentPoint(_scrollViewer).Properties.MouseWheelDelta;
        if (delta == 0)
        {
            return;
        }

        var step = delta > 0 ? 1.1f : 0.9f;
        var min = Math.Max(0.1f, _minZoom);
        var max = Math.Max(min, _maxZoom);
        var targetZoom = Math.Clamp(_scrollViewer.ZoomFactor * step, min, max);

        _scrollViewer.ChangeView(null, null, targetZoom);
        e.Handled = true;
    }

    private Task<IReadOnlyList<PdfSearchResult>> SearchAsyncInternal(string query, PdfSearchOptions? options = null)
    {
        options ??= new PdfSearchOptions();
        var normalizedQuery = query?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            ClearSearchInternal();
            return Task.FromResult<IReadOnlyList<PdfSearchResult>>(Array.Empty<PdfSearchResult>());
        }

        if (string.IsNullOrWhiteSpace(_fileName) || !File.Exists(_fileName))
        {
            return Task.FromResult<IReadOnlyList<PdfSearchResult>>(Array.Empty<PdfSearchResult>());
        }

        return Task.Run<IReadOnlyList<PdfSearchResult>>(() =>
        {
            var results = new List<PdfSearchResult>();
            var regions = new List<SearchHighlightRegion>();
            var maxResults = Math.Max(1, options.MaxResults);

            using var document = PigPdfDocument.Open(_fileName);
            var totalPages = document.NumberOfPages;

            for (var pageNumber = 1; pageNumber <= totalPages; pageNumber++)
            {
                var page = document.GetPage(pageNumber);
                var pageMatches = FindMatchesOnPage(
                    page,
                    normalizedQuery,
                    options.MatchCase,
                    options.WholeWord,
                    maxResults - results.Count);

                foreach (var match in pageMatches)
                {
                    var pageIndex = pageNumber - 1;
                    var width = Math.Max(0.5, match.Right - match.Left);
                    var height = Math.Max(0.5, match.Top - match.Bottom);
                    var resultIndex = results.Count;

                    results.Add(new PdfSearchResult(
                        pageIndex,
                        match.Text,
                        new Rect(match.Left, match.Bottom, width, height),
                        resultIndex));
                    regions.Add(CreateSearchHighlightRegion(
                        resultIndex,
                        pageIndex,
                        page.Width,
                        page.Height,
                        match.Left,
                        match.Top,
                        width,
                        height));

                    if (results.Count >= maxResults)
                    {
                        break;
                    }
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    VirtualView?.RaiseSearchProgress(new PdfSearchProgressEventArgs(
                        normalizedQuery,
                        pageNumber,
                        totalPages,
                        results.Count));
                });

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
            _searchHighlightRegions = regions;
            _currentSearchQuery = normalizedQuery;
            _currentSearchIndex = results.Count > 0 ? 0 : -1;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_highlightSearchResults && options.Highlight)
                {
                    ApplySearchHighlightsVisual();
                }
                else
                {
                    ClearSearchHighlightsVisual();
                }

                if (_currentSearchIndex >= 0)
                {
                    GotoPage((uint)_searchResults[_currentSearchIndex].PageIndex);
                }

                VirtualView?.RaiseSearchResultsFound(new PdfSearchResultsEventArgs(
                    normalizedQuery,
                    _searchResults.AsReadOnly(),
                    Math.Max(0, _currentSearchIndex),
                    true));
            });

            return (IReadOnlyList<PdfSearchResult>)_searchResults.AsReadOnly();
        });
    }

    private void ClearSearchInternal()
    {
        _searchResults.Clear();
        _searchHighlightRegions.Clear();
        _currentSearchIndex = -1;
        _currentSearchQuery = string.Empty;
        ClearSearchHighlightsVisual();
    }

    private void HighlightSearchInternal(bool enable)
    {
        _highlightSearchResults = enable;
        if (_highlightSearchResults)
        {
            ApplySearchHighlightsVisual();
        }
        else
        {
            ClearSearchHighlightsVisual();
        }
    }

    private void GoToSearchResultInternal(int resultIndex)
    {
        if (resultIndex < 0 || resultIndex >= _searchResults.Count)
        {
            return;
        }

        _currentSearchIndex = resultIndex;
        GotoPage((uint)_searchResults[resultIndex].PageIndex);
        ApplySearchHighlightsVisual();

        VirtualView?.RaiseSearchResultsFound(new PdfSearchResultsEventArgs(
            _currentSearchQuery,
            _searchResults.AsReadOnly(),
            _currentSearchIndex,
            true));
    }

    private static IReadOnlyList<SearchWordMatch> FindMatchesOnPage(
        Page page,
        string query,
        bool matchCase,
        bool wholeWord,
        int maxCount)
    {
        if (maxCount <= 0)
        {
            return Array.Empty<SearchWordMatch>();
        }

        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var words = page.GetWords()
            .Where(w => !string.IsNullOrWhiteSpace(w.Text))
            .ToList();

        if (words.Count == 0)
        {
            return Array.Empty<SearchWordMatch>();
        }

        var queryTokens = query
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var matches = new List<SearchWordMatch>();

        if (queryTokens.Length > 1)
        {
            for (var start = 0; start <= words.Count - queryTokens.Length; start++)
            {
                var matched = true;
                for (var tokenIndex = 0; tokenIndex < queryTokens.Length; tokenIndex++)
                {
                    if (!string.Equals(words[start + tokenIndex].Text, queryTokens[tokenIndex], comparison))
                    {
                        matched = false;
                        break;
                    }
                }

                if (!matched)
                {
                    continue;
                }

                var phraseWords = words.Skip(start).Take(queryTokens.Length).ToList();
                var left = phraseWords.Min(w => w.BoundingBox.Left);
                var right = phraseWords.Max(w => w.BoundingBox.Right);
                var top = phraseWords.Max(w => w.BoundingBox.Top);
                var bottom = phraseWords.Min(w => w.BoundingBox.Bottom);
                matches.Add(new SearchWordMatch(string.Join(" ", phraseWords.Select(w => w.Text)), left, top, right, bottom));
                if (matches.Count >= maxCount)
                {
                    break;
                }

                start += queryTokens.Length - 1;
            }

            return matches;
        }

        foreach (var word in words)
        {
            var wordText = word.Text.Trim();
            var isMatch = wholeWord
                ? string.Equals(wordText, query, comparison)
                : wordText.IndexOf(query, comparison) >= 0;

            if (!isMatch)
            {
                continue;
            }

            matches.Add(new SearchWordMatch(
                wordText,
                word.BoundingBox.Left,
                word.BoundingBox.Top,
                word.BoundingBox.Right,
                word.BoundingBox.Bottom));

            if (matches.Count >= maxCount)
            {
                break;
            }
        }

        return matches;
    }

    private static SearchHighlightRegion CreateSearchHighlightRegion(
        int resultIndex,
        int pageIndex,
        double pageWidth,
        double pageHeight,
        double left,
        double top,
        double width,
        double height)
    {
        var safePageWidth = Math.Max(1, pageWidth);
        var safePageHeight = Math.Max(1, pageHeight);

        var xNorm = Math.Clamp(left / safePageWidth, 0d, 1d);
        var yNorm = Math.Clamp((safePageHeight - top) / safePageHeight, 0d, 1d);
        var widthNorm = Math.Clamp(width / safePageWidth, 0.001d, 1d);
        var heightNorm = Math.Clamp(height / safePageHeight, 0.001d, 1d);

        return new SearchHighlightRegion(resultIndex, pageIndex, xNorm, yNorm, widthNorm, heightNorm);
    }

    private sealed record SearchWordMatch(string Text, double Left, double Top, double Right, double Bottom);

    private sealed record SearchHighlightRegion(
        int ResultIndex,
        int PageIndex,
        double XNormalized,
        double YNormalized,
        double WidthNormalized,
        double HeightNormalized);

    private void RaiseError(string message, Exception? exception = null)
    {
        VirtualView?.RaiseError(new PdfErrorEventArgs(message, exception));
    }

    private sealed class WindowsPlatformFeatures : IPdfViewPlatformFeatures
    {
        private readonly PdfViewHandler _handler;

        public WindowsPlatformFeatures(PdfViewHandler handler)
        {
            _handler = handler;
        }

        public bool IsSearchSupported => true;

        public Task<IReadOnlyList<PdfSearchResult>> SearchAsync(string query, PdfSearchOptions? options = null)
        {
            return _handler.SearchAsyncInternal(query, options);
        }

        public void ClearSearch()
        {
            _handler.ClearSearchInternal();
        }

        public void HighlightSearchResults(bool enable)
        {
            _handler.HighlightSearchInternal(enable);
        }

        public void GoToSearchResult(int resultIndex)
        {
            _handler.GoToSearchResultInternal(resultIndex);
        }
    }
}
