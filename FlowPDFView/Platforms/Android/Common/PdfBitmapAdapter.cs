using Android.Graphics;
using Android.Graphics.Pdf;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.CardView.Widget;
using AndroidX.RecyclerView.Widget;
using Java.IO;
using Microsoft.Maui.Platform;
using ParcelFileDescriptor = Android.OS.ParcelFileDescriptor;

namespace Flow.PDFView.Platforms.Android.Common
{
    /// <summary>
    /// en: RecyclerView adapter for rendering PDF pages as bitmaps.
    /// zh: 用于将 PDF 页面渲染为位图的 RecyclerView 适配器。
    /// </summary>
    internal class PdfBitmapAdapter : RecyclerView.Adapter
    {
        private readonly PageAppearance? _pageAppearance;
        private readonly string _fileName;
        private readonly Func<int, (int Width, int Height)> _getPageSize;
        private readonly Dictionary<int, Bitmap> _pageCache = new();
        private readonly object _cacheLock = new();
        private readonly object _rendererLock = new();
        
        private int _pageCount;
        private bool _isDisposed;
        
        private PdfRenderer? _pdfRenderer;
        private ParcelFileDescriptor? _fileDescriptor;
        private bool _isRendererInitialized;
        
        private const int MaxCacheSize = 20;

        public PdfBitmapAdapter(
            string fileName,
            PageAppearance? pageAppearance,
            Func<int, (int Width, int Height)> getPageSize,
            int pageCount)
        {
            _fileName = fileName;
            _pageAppearance = pageAppearance;
            _getPageSize = getPageSize;
            _pageCount = pageCount;
        }

        public int PageCount => _pageCount;

        private bool TryInitializeRenderer()
        {
            if (_isRendererInitialized)
                return _pdfRenderer != null;
            
            lock (_rendererLock)
            {
                if (_isRendererInitialized)
                    return _pdfRenderer != null;
                
                try
                {
                    if (!string.IsNullOrEmpty(_fileName) && System.IO.File.Exists(_fileName))
                    {
                        _fileDescriptor = ParcelFileDescriptor.Open(new Java.IO.File(_fileName), ParcelFileMode.ReadOnly);
                        if (_fileDescriptor != null)
                        {
                            _pdfRenderer = new PdfRenderer(_fileDescriptor);
                            _isRendererInitialized = true;
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error initializing PdfRenderer: {ex.Message}");
                }
                
                _isRendererInitialized = true;
                return false;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            global::Android.Views.View? itemView = LayoutInflater.From(parent.Context)?.Inflate(Resource.Layout.card_view, parent, false);

            if (itemView == null)
            {
                throw new InvalidOperationException("Failed to inflate card_view layout");
            }

            if (itemView is CardView cardView && _pageAppearance != null)
            {
                cardView.Elevation = _pageAppearance.ShadowEnabled ? 4 : 0;
                
                if (cardView.LayoutParameters is ViewGroup.MarginLayoutParams layoutParams)
                {
                    layoutParams.SetMargins(
                        (int)_pageAppearance.Margin.Left,
                        (int)_pageAppearance.Margin.Top,
                        (int)_pageAppearance.Margin.Right,
                        (int)_pageAppearance.Margin.Bottom);
                    cardView.LayoutParameters = layoutParams;
                }
            }

            return new CardViewHolder(itemView);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            CardViewHolder vh = (CardViewHolder)holder;
            
            if (vh.Image == null)
                return;

            Bitmap? bitmap = null;
            lock (_cacheLock)
            {
                if (_pageCache.TryGetValue(position, out var cachedBitmap) && !cachedBitmap.IsRecycled)
                {
                    bitmap = cachedBitmap;
                }
            }

            if (bitmap != null && !bitmap.IsRecycled)
            {
                vh.Image.SetImageBitmap(bitmap);
            }
            else
            {
                vh.Image.SetImageBitmap(null);
                RenderPageAsync(position, vh.Image);
            }
        }

        private async void RenderPageAsync(int position, ImageView imageView)
        {
            try
            {
                var (width, height) = _getPageSize(position);
                if (width <= 0 || height <= 0)
                {
                    return;
                }

                var bitmap = await Task.Run(() =>
                {
                    if (!TryInitializeRenderer() || _pdfRenderer == null)
                        return null;

                    if (position >= _pdfRenderer.PageCount)
                        return null;

                    PdfRenderer.Page? page = null;
                    Bitmap? pageBitmap = null;

                    try
                    {
                        page = _pdfRenderer.OpenPage(position);
                        if (page == null)
                            return null;

                        var config = Bitmap.Config.Argb8888;
                        pageBitmap = config != null ? Bitmap.CreateBitmap(width, height, config) : null;
                        if (pageBitmap == null)
                            return null;
                        
                        var crop = _pageAppearance?.Crop ?? Microsoft.Maui.Thickness.Zero;
                        var matrix = GetCropMatrix(page, pageBitmap, crop);
                        page.Render(pageBitmap, null, matrix, PdfRenderMode.ForDisplay);
                    }
                    finally
                    {
                        page?.Close();
                    }

                    return pageBitmap;
                });

                if (bitmap != null && !_isDisposed && !bitmap.IsRecycled)
                {
                    lock (_cacheLock)
                    {
                        if (_pageCache.Count >= MaxCacheSize)
                        {
                            var keysToRemove = _pageCache.Keys.OrderBy(k => k).Take(5).ToList();
                            foreach (var key in keysToRemove)
                            {
                                if (_pageCache.TryGetValue(key, out var oldBitmap))
                                {
                                    try { oldBitmap?.Recycle(); } catch { }
                                    _pageCache.Remove(key);
                                }
                            }
                        }
                        
                        if (!_pageCache.ContainsKey(position))
                        {
                            _pageCache[position] = bitmap;
                        }
                    }
                    
                    imageView.Post(() =>
                    {
                        try
                        {
                            if (!_isDisposed)
                                imageView.SetImageBitmap(bitmap);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error setting bitmap: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rendering page {position}: {ex.Message}");
            }
        }

        private static Matrix? GetCropMatrix(PdfRenderer.Page page, Bitmap bitmap, Microsoft.Maui.Thickness bounds)
        {
            if (bounds.IsEmpty)
                return null;
            
            int pageWidth = page.Width;
            int pageHeight = page.Height;
                
            var cropLeft = (int)bounds.Left;
            int cropTop = (int)bounds.Top;
            int cropRight = pageWidth - (int)bounds.Right;
            int cropBottom = pageHeight - (int)bounds.Bottom;

            Matrix matrix = new Matrix();
            float scaleX = (float)bitmap.Width / (cropRight - cropLeft);
            float scaleY = (float)bitmap.Height / (cropBottom - cropTop);
            matrix.SetScale(scaleX, scaleY);
            matrix.PostTranslate(-cropLeft * scaleX, -cropTop * scaleY);

            return matrix;
        }

        public void ClearCache()
        {
            lock (_cacheLock)
            {
                foreach (var bitmap in _pageCache.Values)
                {
                    try { bitmap?.Recycle(); } catch { }
                }
                _pageCache.Clear();
            }
        }

        public override int ItemCount => _pageCount;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isDisposed = true;
                ClearCache();
                
                lock (_rendererLock)
                {
                    _pdfRenderer?.Close();
                    _pdfRenderer = null;
                    _fileDescriptor?.Close();
                    _fileDescriptor = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
