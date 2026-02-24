using Android.Graphics.Pdf;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;

namespace Flow.PDFView.Platforms.Android
{
    /// <summary>
    /// en: Helper class for screen metrics and density calculations on Android.
    /// zh: Android 屏幕指标和密度计算的辅助类。
    /// </summary>
    internal class ScreenHelper
    {
        private int _widthPixels;
        private int _heightPixels;
        private float _density;
        private float _scaledDensity;

        public void Invalidate()
        {
            var windowService = global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.WindowService);
            if (windowService == null)
                return;

            IWindowManager windowManager = windowService.JavaCast<IWindowManager>();
            if (windowManager == null)
                return;

            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var windowMetrics = windowManager.CurrentWindowMetrics;
                if (windowMetrics == null)
                    return;
                    
                var bounds = windowMetrics.Bounds;
                _widthPixels = bounds.Width();
                _heightPixels = bounds.Height();
                
                var resources = global::Android.App.Application.Context.Resources;
                if (resources == null)
                {
                    _density = 1.0f;
                    _scaledDensity = 1.0f;
                    return;
                }
                    
                var displayMetrics = resources.DisplayMetrics;
                _density = displayMetrics?.Density ?? 1.0f;
#pragma warning disable CA1422
                _scaledDensity = displayMetrics?.ScaledDensity ?? 1.0f;
#pragma warning restore CA1422
                return;
            }

            var metrics = new DisplayMetrics();
#pragma warning disable CA1422
            var display = windowManager.DefaultDisplay;
            if (display != null)
                display.GetMetrics(metrics);
#pragma warning restore CA1422
            _widthPixels = metrics.WidthPixels;
            _heightPixels = metrics.HeightPixels;
            _density = metrics.Density;
            _scaledDensity = metrics.ScaledDensity;
        }

        public float Density => _density;
        public float ScaledDensity => _scaledDensity;

        public (int Width, int Height) GetImageWidthAndHeight(bool isVertival, PdfRenderer.Page page)
        {
            int width;
            int height;
            float ratio;

            float scaleFactor = _scaledDensity > 0 ? _scaledDensity : _density;
            scaleFactor = Math.Clamp(scaleFactor, 1.5f, 3.0f);

            if (isVertival)
            {
                width = (int)(_widthPixels * scaleFactor);
                ratio = (float)page.Height / page.Width;
                height = (int)(width * ratio);
            }
            else
            {
                height = (int)(_heightPixels * scaleFactor);
                ratio = (float)page.Width / page.Height;
                width = (int)(height * ratio);
            }

            return (width, height);
        }
    }
}
