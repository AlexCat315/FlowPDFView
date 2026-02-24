using System.Net.Http;
using Foundation;
using Flow.PDFView.Abstractions;
using Flow.PDFView.Helpers;
using Microsoft.Maui.Handlers;
using PdfKit;
using UIKit;

namespace Flow.PDFView.Platforms.MacCatalyst;

/// <summary>
/// en: MacCatalyst handler for mapping PdfView properties to native controls.
/// zh: 用于将 PdfView 属性映射到原生控件的 MacCatalyst 处理器。
/// </summary>
public class PdfViewHandler : ViewHandler<PdfView, PdfKit.PdfView>
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

    private string? _fileName;
    private readonly DesiredSizeHelper _sizeHelper = new();
    private PageAppearance? _appearance;
    private NSObject? _pageChangedObserver;
    private NSObject? _annotationHitObserver;
    private UITapGestureRecognizer? _tapGestureRecognizer;
    private UIPanGestureRecognizer? _shiftScrollZoomRecognizer;
    private UIView? _shiftScrollZoomGestureHost;
    private nfloat _lastShiftScrollTranslationX;
    private nfloat _lastShiftScrollTranslationY;

    private bool _isScrolling;
    private bool _isPageIndexLocked;

    private bool _enableZoom = PdfViewDefaults.EnableZoom;
    private bool _enableSwipe = PdfViewDefaults.EnableSwipe;
    private bool _enableTapGestures = PdfViewDefaults.EnableTapGestures;
    private bool _enableLinkNavigation = PdfViewDefaults.EnableLinkNavigation;
    private float _zoom = PdfViewDefaults.Zoom;
    private float _minZoom = PdfViewDefaults.MinZoom;
    private float _maxZoom = PdfViewDefaults.MaxZoom;
    private int _pageSpacing = PdfViewDefaults.PageSpacing;
    private FitPolicy _fitPolicy = PdfViewDefaults.DefaultFitPolicy;
    private bool _enableAntialiasing = PdfViewDefaults.EnableAntialiasing;
    private bool _useBestQuality = PdfViewDefaults.UseBestQuality;
    private bool _enableAnnotationRendering = PdfViewDefaults.EnableAnnotationRendering;

    private List<PdfSearchResult> _searchResults = new();
    private List<PdfSelection> _nativeSearchSelections = new();
    private int _currentSearchIndex = -1;
    private string _currentSearchQuery = string.Empty;
    private bool _highlightSearchResults = true;

    public PdfViewHandler() : base(Mapper, CommandMapper)
    {
    }

    protected override PdfKit.PdfView CreatePlatformView()
    {
        var pdfView = new PdfKit.PdfView
        {
            AutoScales = true,
            DisplayMode = PdfKit.PdfDisplayMode.SinglePageContinuous,
            DisplayDirection = PdfDisplayDirection.Vertical,
            DisplaysPageBreaks = true,
        };

        _pageChangedObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            PdfKit.PdfView.PageChangedNotification,
            PageChangedNotificationHandler,
            pdfView);

        _annotationHitObserver = PdfKit.PdfView.Notifications.ObserveAnnotationHit(OnAnnotationHit);

        pdfView.WeakDelegate = new PdfViewDelegateImpl(this);

        _tapGestureRecognizer = new UITapGestureRecognizer(HandleTap);
        pdfView.AddGestureRecognizer(_tapGestureRecognizer);

        _shiftScrollZoomRecognizer = new UIPanGestureRecognizer(HandleShiftScrollZoom)
        {
            AllowedScrollTypesMask = UIScrollTypeMask.All,
            CancelsTouchesInView = false,
            Delegate = new SimultaneousGestureDelegate(),
        };
        AttachShiftScrollZoomRecognizer(pdfView);

        return pdfView;
    }

    protected override void ConnectHandler(PdfKit.PdfView platformView)
    {
        base.ConnectHandler(platformView);

        if (VirtualView == null)
        {
            return;
        }

        VirtualView.SetPlatformFeatures(new MacCatalystPlatformFeatures(this));

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
        MapPageAppearance(this, VirtualView);
        MapSource(this, VirtualView);
        MapUri(this, VirtualView);
    }

    protected override void DisconnectHandler(PdfKit.PdfView platformView)
    {
        VirtualView?.SetPlatformFeatures(null);

        if (_pageChangedObserver != null)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(_pageChangedObserver);
            _pageChangedObserver.Dispose();
            _pageChangedObserver = null;
        }

        if (_annotationHitObserver != null)
        {
            _annotationHitObserver.Dispose();
            _annotationHitObserver = null;
        }

        if (_tapGestureRecognizer != null)
        {
            platformView.RemoveGestureRecognizer(_tapGestureRecognizer);
            _tapGestureRecognizer.Dispose();
            _tapGestureRecognizer = null;
        }

        if (_shiftScrollZoomRecognizer != null)
        {
            _shiftScrollZoomGestureHost?.RemoveGestureRecognizer(_shiftScrollZoomRecognizer);
            _shiftScrollZoomGestureHost = null;
            _shiftScrollZoomRecognizer.Dispose();
            _shiftScrollZoomRecognizer = null;
        }

        platformView.WeakDelegate = null;

        base.DisconnectHandler(platformView);
    }

    public override Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        if (_sizeHelper.UpdateSize(widthConstraint, heightConstraint) && !string.IsNullOrWhiteSpace(_fileName))
        {
            _ = RenderPagesAsync();
        }

        return base.GetDesiredSize(widthConstraint, heightConstraint);
    }

    private static void MapSource(PdfViewHandler handler, PdfView pdfView)
    {
        if (pdfView.Source == null)
        {
            handler._fileName = null;
            handler.PlatformView.Document = null;
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
        handler.PlatformView.DisplayDirection = pdfView.IsHorizontal
            ? PdfDisplayDirection.Horizontal
            : PdfDisplayDirection.Vertical;
        handler.ApplyPageAppearance();
    }

    private static void MapEnableZoom(PdfViewHandler handler, PdfView pdfView)
    {
        handler._enableZoom = pdfView.EnableZoom;
        handler.ApplyZoomSettings();
    }

    private static void MapEnableSwipe(PdfViewHandler handler, PdfView pdfView)
    {
        handler._enableSwipe = pdfView.EnableSwipe;
        handler.ApplySwipeSettings();
    }

    private static void MapEnableTapGestures(PdfViewHandler handler, PdfView pdfView)
    {
        handler._enableTapGestures = pdfView.EnableTapGestures;
        if (handler._tapGestureRecognizer != null)
        {
            handler._tapGestureRecognizer.Enabled = handler._enableTapGestures;
        }
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
        handler.ApplyPageAppearance();
    }

    private static void MapFitPolicy(PdfViewHandler handler, PdfView pdfView)
    {
        handler._fitPolicy = pdfView.FitPolicy;
        handler.ApplyFitPolicy();
    }

    private static void MapScrollOrientation(PdfViewHandler handler, PdfView pdfView)
    {
        handler.PlatformView.DisplayDirection = pdfView.ScrollOrientation == PdfScrollOrientation.Horizontal
            ? PdfDisplayDirection.Horizontal
            : PdfDisplayDirection.Vertical;
        handler.ApplyPageAppearance();
    }

    private static void MapDisplayMode(PdfViewHandler handler, PdfView pdfView)
    {
        handler.PlatformView.DisplayMode = pdfView.DisplayMode switch
        {
            Flow.PDFView.Abstractions.PdfDisplayMode.SinglePage => PdfKit.PdfDisplayMode.SinglePage,
            Flow.PDFView.Abstractions.PdfDisplayMode.SinglePageContinuous => PdfKit.PdfDisplayMode.SinglePageContinuous,
            _ => PdfKit.PdfDisplayMode.SinglePageContinuous,
        };
    }

    private static void MapPageAppearance(PdfViewHandler handler, PdfView pdfView)
    {
        handler._appearance = pdfView.PageAppearance;
        handler.ApplyPageAppearance();
    }

    private static void MapPageIndex(PdfViewHandler handler, PdfView pdfView)
    {
        handler.GotoPage(pdfView.PageIndex);
    }

    private static void MapDefaultPage(PdfViewHandler handler, PdfView pdfView)
    {
        if (pdfView.DefaultPage >= 0)
        {
            handler.GotoPage((uint)pdfView.DefaultPage);
        }
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

        handler.PlatformView.BackgroundColor = UIColor.FromRGBA(
            (float)pdfView.BackgroundColor.Red,
            (float)pdfView.BackgroundColor.Green,
            (float)pdfView.BackgroundColor.Blue,
            (float)pdfView.BackgroundColor.Alpha);
    }

    private static void MapEnableAnnotationRendering(PdfViewHandler handler, PdfView pdfView)
    {
        handler._enableAnnotationRendering = pdfView.EnableAnnotationRendering;
        handler.UpdateAnnotationVisibility();
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
                    _fileName = ResolveAssetPath(assetSource.AssetName);
                    if (string.IsNullOrWhiteSpace(_fileName))
                    {
                        RaiseError($"未找到资源文件: {assetSource.AssetName}");
                        return;
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
            PlatformView.Document = null;
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

    private static string? ResolveAssetPath(string assetName)
    {
        var directPath = Path.Combine(NSBundle.MainBundle.BundlePath, assetName);
        if (File.Exists(directPath))
        {
            return directPath;
        }

        var name = Path.GetFileNameWithoutExtension(assetName);
        var extension = Path.GetExtension(assetName).TrimStart('.');
        var resolved = NSBundle.MainBundle.PathForResource(name, extension);
        return string.IsNullOrWhiteSpace(resolved) ? null : resolved;
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

    private Task RenderPagesAsync()
    {
        return Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(_fileName) || !File.Exists(_fileName))
            {
                MainThread.BeginInvokeOnMainThread(() => PlatformView.Document = null);
                return;
            }

            try
            {
                var data = NSData.FromFile(_fileName);
                if (data == null || data.Length == 0)
                {
                    MainThread.BeginInvokeOnMainThread(() => PlatformView.Document = null);
                    RaiseError($"读取 PDF 失败: {_fileName}");
                    return;
                }

                var document = new PdfDocument(data);
                if (document == null)
                {
                    MainThread.BeginInvokeOnMainThread(() => PlatformView.Document = null);
                    RaiseError($"创建 PDF 文档失败: {_fileName}");
                    return;
                }

                if (document.IsLocked)
                {
                    var password = VirtualView?.Source?.Password;
                    if (string.IsNullOrEmpty(password) || !document.Unlock(password))
                    {
                        MainThread.BeginInvokeOnMainThread(() => PlatformView.Document = null);
                        RaiseError("PDF 已加密且密码无效或未提供");
                        return;
                    }
                }

                var pageCount = (int)document.PageCount;
                var title = document.DocumentAttributes?["Title"]?.ToString();
                var author = document.DocumentAttributes?["Author"]?.ToString();
                var subject = document.DocumentAttributes?["Subject"]?.ToString();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PlatformView.Document = document;
                    AttachShiftScrollZoomRecognizer(PlatformView);
                    ApplyFitPolicy();
                    ApplyPageAppearance();
                    ApplyZoomSettings();
                    ApplySwipeSettings();
                    UpdateAnnotationVisibility();
                    ClearSearchInternal();

                    VirtualView?.RaiseDocumentLoaded(new DocumentLoadedEventArgs(pageCount, title, author, subject));
                    VirtualView?.RaiseRendered(new RenderedEventArgs(pageCount));

                    if (VirtualView != null && VirtualView.DefaultPage >= 0 && VirtualView.DefaultPage < pageCount)
                    {
                        GotoPage((uint)VirtualView.DefaultPage);
                    }
                    else
                    {
                        VirtualView?.RaisePageChanged(new Abstractions.PageChangedEventArgs(0, pageCount));
                    }
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() => PlatformView.Document = null);
                RaiseError($"渲染 PDF 失败: {ex.Message}", ex);
            }
        });
    }

    private void ApplyPageAppearance()
    {
        var appearance = _appearance;
        if (OperatingSystem.IsIOSVersionAtLeast(12, 0))
        {
            PlatformView.PageShadowsEnabled = appearance?.ShadowEnabled ?? true;
        }

        var margin = appearance?.Margin ?? Thickness.Zero;
        PlatformView.PageBreakMargins = new UIEdgeInsets(
            (nfloat)(margin.Top + _pageSpacing),
            (nfloat)(margin.Left + _pageSpacing),
            (nfloat)(margin.Bottom + _pageSpacing),
            (nfloat)(margin.Right + _pageSpacing));
    }

    private void ApplyFitPolicy()
    {
        PlatformView.AutoScales = _fitPolicy != FitPolicy.Both;
    }

    private void ApplyZoomSettings()
    {
        if (!_enableZoom)
        {
            var fixedScale = PlatformView.ScaleFactor <= 0 ? 1f : PlatformView.ScaleFactor;
            PlatformView.MinScaleFactor = fixedScale;
            PlatformView.MaxScaleFactor = fixedScale;
            return;
        }

        var min = Math.Max(0.1f, _minZoom);
        var max = Math.Max(min, _maxZoom);
        var clamped = Math.Clamp(_zoom, min, max);

        PlatformView.MinScaleFactor = min;
        PlatformView.MaxScaleFactor = max;

        if (Math.Abs((float)PlatformView.ScaleFactor - clamped) > float.Epsilon)
        {
            PlatformView.ScaleFactor = clamped;
        }
    }

    private void ApplySwipeSettings()
    {
        var scrollView = FindScrollView(PlatformView);
        if (scrollView == null)
        {
            return;
        }

        scrollView.ScrollEnabled = _enableSwipe;
        scrollView.Bounces = _enableSwipe;
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

    private void AttachShiftScrollZoomRecognizer(PdfKit.PdfView pdfView)
    {
        if (_shiftScrollZoomRecognizer == null)
        {
            return;
        }

        var targetView = (UIView?)FindScrollView(pdfView) ?? pdfView;
        if (ReferenceEquals(targetView, _shiftScrollZoomGestureHost))
        {
            return;
        }

        _shiftScrollZoomGestureHost?.RemoveGestureRecognizer(_shiftScrollZoomRecognizer);
        targetView.AddGestureRecognizer(_shiftScrollZoomRecognizer);
        _shiftScrollZoomGestureHost = targetView;
    }

    private static bool IsShiftPressed(UIPanGestureRecognizer recognizer)
    {
        return recognizer.ModifierFlags.HasFlag(UIKeyModifierFlags.Shift);
    }

    private void UpdateAnnotationVisibility()
    {
        var document = PlatformView.Document;
        if (document == null)
        {
            return;
        }

        for (nint i = 0; i < document.PageCount; i++)
        {
            var page = document.GetPage(i);
            if (page?.Annotations == null)
            {
                continue;
            }

            foreach (var annotation in page.Annotations)
            {
                annotation.ShouldDisplay = _enableAnnotationRendering;
            }
        }

        PlatformView.SetNeedsDisplay();
    }

    private void GotoPage(uint pageIndex)
    {
        if (_isScrolling)
        {
            return;
        }

        var document = PlatformView.Document;
        if (document == null || pageIndex >= document.PageCount)
        {
            return;
        }

        var page = document.GetPage((nint)pageIndex);
        if (page == null)
        {
            return;
        }

        _isPageIndexLocked = true;
        PlatformView.GoToPage(page);
    }

    private void PageChangedNotificationHandler(NSNotification notification)
    {
        var currentPage = PlatformView.CurrentPage;
        var document = PlatformView.Document;
        if (currentPage == null || document == null)
        {
            return;
        }

        var newPageIndex = (uint)document.GetPageIndex(currentPage);
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

        VirtualView?.RaisePageChanged(new Abstractions.PageChangedEventArgs((int)newPageIndex, (int)document.PageCount));

        if (VirtualView?.PageChangedCommand?.CanExecute(null) == true)
        {
            VirtualView.PageChangedCommand.Execute(new Flow.PDFView.Events.PageChangedEventArgs((int)newPageIndex, (int)document.PageCount));
        }
    }

    private void OnAnnotationHit(object? sender, PdfViewAnnotationHitEventArgs e)
    {
        var document = PlatformView.Document;
        if (document == null || !_enableAnnotationRendering)
        {
            return;
        }

        var userInfo = e.Notification.UserInfo;
        if (userInfo == null)
        {
            return;
        }

        var annotationKey = new NSString("PDFAnnotationHit");
        if (!userInfo.ContainsKey(annotationKey))
        {
            return;
        }

        if (userInfo[annotationKey] is not PdfAnnotation annotation)
        {
            return;
        }

        var page = annotation.Page;
        if (page == null)
        {
            return;
        }

        var pageIndex = (int)document.GetPageIndex(page);

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

        var bounds = annotation.Bounds;
        var args = new AnnotationTappedEventArgs(
            pageIndex,
            annotationType,
            annotation.Contents ?? string.Empty,
            new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height));

        VirtualView?.RaiseAnnotationTapped(args);
    }

    private void HandleTap(UITapGestureRecognizer recognizer)
    {
        if (!_enableTapGestures)
        {
            return;
        }

        var location = recognizer.LocationInView(PlatformView);
        var pageIndex = 0;

        var document = PlatformView.Document;
        var currentPage = PlatformView.CurrentPage;
        if (document != null && currentPage != null)
        {
            pageIndex = (int)document.GetPageIndex(currentPage);
            var pagePoint = PlatformView.ConvertPointToPage(location, currentPage);
            VirtualView?.RaiseTapped(new PdfTappedEventArgs(pageIndex, (float)pagePoint.X, (float)pagePoint.Y));
            return;
        }

        VirtualView?.RaiseTapped(new PdfTappedEventArgs(pageIndex, (float)location.X, (float)location.Y));
    }

    private void HandleShiftScrollZoom(UIPanGestureRecognizer recognizer)
    {
        if (!_enableZoom)
        {
            return;
        }

        var hostView = recognizer.View ?? PlatformView;
        var translation = recognizer.TranslationInView(hostView);
        var translationX = translation.X;
        var translationY = translation.Y;

        if (recognizer.State == UIGestureRecognizerState.Began)
        {
            _lastShiftScrollTranslationX = translationX;
            _lastShiftScrollTranslationY = translationY;
            return;
        }

        if (recognizer.State != UIGestureRecognizerState.Changed)
        {
            return;
        }

        var hasShiftModifier = IsShiftPressed(recognizer);

        var deltaX = translationX - _lastShiftScrollTranslationX;
        var deltaY = translationY - _lastShiftScrollTranslationY;
        _lastShiftScrollTranslationX = translationX;
        _lastShiftScrollTranslationY = translationY;
        var dominantHorizontal = Math.Abs((float)deltaX) > Math.Abs((float)deltaY) * 1.2f;
        var shouldZoom = hasShiftModifier || dominantHorizontal;
        if (!shouldZoom)
        {
            return;
        }

        var delta = dominantHorizontal ? deltaX : deltaY;
        if (Math.Abs((float)delta) < 0.01f)
        {
            return;
        }

        var factor = (float)Math.Exp(-delta * 0.015f);
        var min = Math.Max(0.1f, _minZoom);
        var max = Math.Max(min, _maxZoom);
        var nextZoom = Math.Clamp((float)PlatformView.ScaleFactor * factor, min, max);
        PlatformView.ScaleFactor = nextZoom;
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

        var document = PlatformView.Document;
        if (document == null || document.PageCount <= 0)
        {
            return Task.FromResult<IReadOnlyList<PdfSearchResult>>(Array.Empty<PdfSearchResult>());
        }

        var results = new List<PdfSearchResult>();
        var nativeSelections = new List<PdfSelection>();
        var maxResults = Math.Max(1, options.MaxResults);
        var compareOptions = options.MatchCase
            ? NSStringCompareOptions.LiteralSearch
            : NSStringCompareOptions.CaseInsensitiveSearch;
        var foundSelections = document.Find(normalizedQuery, compareOptions) ?? Array.Empty<PdfSelection>();
        var matchesByPage = BuildSelectionMatchesByPage(document, foundSelections, normalizedQuery, options);
        int? firstMatchedPage = null;

        for (var pageIndex = 0; pageIndex < document.PageCount; pageIndex++)
        {
            if (matchesByPage.TryGetValue((int)pageIndex, out var pageMatches))
            {
                if (!options.SearchAllPages)
                {
                    firstMatchedPage ??= (int)pageIndex;
                    if ((int)pageIndex != firstMatchedPage.Value)
                    {
                        VirtualView?.RaiseSearchProgress(new PdfSearchProgressEventArgs(
                            normalizedQuery,
                            (int)pageIndex + 1,
                            (int)document.PageCount,
                            results.Count));
                        continue;
                    }
                }

                foreach (var match in pageMatches)
                {
                    results.Add(new PdfSearchResult(
                        (int)pageIndex,
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

            VirtualView?.RaiseSearchProgress(new PdfSearchProgressEventArgs(
                normalizedQuery,
                (int)pageIndex + 1,
                (int)document.PageCount,
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

        VirtualView?.RaiseSearchResultsFound(new PdfSearchResultsEventArgs(
            normalizedQuery,
            _searchResults.AsReadOnly(),
            Math.Max(0, _currentSearchIndex),
            true));

        return Task.FromResult<IReadOnlyList<PdfSearchResult>>(_searchResults.AsReadOnly());
    }

    private void ClearSearchInternal()
    {
        _searchResults.Clear();
        _nativeSearchSelections.Clear();
        _currentSearchIndex = -1;
        _currentSearchQuery = string.Empty;
        ClearSearchHighlights();
    }

    private void HighlightSearchInternal(bool enable)
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

    private void GoToSearchResultInternal(int resultIndex)
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

        VirtualView?.RaiseSearchResultsFound(new PdfSearchResultsEventArgs(
            _currentSearchQuery,
            _searchResults.AsReadOnly(),
            _currentSearchIndex,
            true));
    }

    private void RaiseError(string message, Exception? exception = null)
    {
        VirtualView?.RaiseError(new PdfErrorEventArgs(message, exception));
    }

    private void HandleLinkTapped(NSUrl url)
    {
        var args = new LinkTappedEventArgs(url.AbsoluteString, null);
        VirtualView?.RaiseLinkTapped(args);

        if (!args.Handled && _enableLinkNavigation)
        {
            UIApplication.SharedApplication.OpenUrl(url, new UIApplicationOpenUrlOptions(), null);
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
        PlatformView.SetCurrentSelection(selection, true);
        PlatformView.GoToSelection(selection);
    }

    private void ApplySearchHighlights()
    {
        if (_nativeSearchSelections.Count == 0)
        {
            PlatformView.HighlightedSelections = Array.Empty<PdfSelection>();
            return;
        }

        for (var i = 0; i < _nativeSearchSelections.Count; i++)
        {
            _nativeSearchSelections[i].Color = i == _currentSearchIndex
                ? UIColor.FromRGBA(1f, 0.56f, 0.08f, 0.45f)
                : UIColor.FromRGBA(1f, 0.92f, 0.23f, 0.35f);
        }

        PlatformView.HighlightedSelections = _nativeSearchSelections.ToArray();
    }

    private void ClearSearchHighlights()
    {
        PlatformView.HighlightedSelections = Array.Empty<PdfSelection>();
    }

    private sealed class PdfViewDelegateImpl : PdfViewDelegate
    {
        private readonly WeakReference<PdfViewHandler> _owner;

        public PdfViewDelegateImpl(PdfViewHandler owner)
        {
            _owner = new WeakReference<PdfViewHandler>(owner);
        }

        [Export("PDFViewWillClickOnLink:withURL:")]
        public override void WillClickOnLink(PdfKit.PdfView sender, NSUrl url)
        {
            if (_owner.TryGetTarget(out var owner))
            {
                owner.HandleLinkTapped(url);
            }
        }
    }

    private sealed class SimultaneousGestureDelegate : UIGestureRecognizerDelegate
    {
        public override bool ShouldRecognizeSimultaneously(UIGestureRecognizer gestureRecognizer, UIGestureRecognizer otherGestureRecognizer)
        {
            return true;
        }
    }

    private sealed class MacCatalystPlatformFeatures : IPdfViewPlatformFeatures
    {
        private readonly PdfViewHandler _handler;

        public MacCatalystPlatformFeatures(PdfViewHandler handler)
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
