using Android.Graphics.Pdf;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;

namespace Flow.PDFView.Platforms.Android
{
    internal class ScreenHelper
    {
        private int _widthPixels;
        private int _heightPixels;
        private float _density;
        private float _scaledDensity;

        public void Invalidate()
        {
            IWindowManager windowManager = global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.WindowService).JavaCast<IWindowManager>();

            // 使用 .NET 提供的跨平台版本检查 API
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var bounds = windowManager.CurrentWindowMetrics.Bounds;
                _widthPixels = bounds.Width();
                _heightPixels = bounds.Height();
                var displayMetrics = global::Android.App.Application.Context.Resources.DisplayMetrics;
                _density = displayMetrics.Density;
                _scaledDensity = displayMetrics.ScaledDensity;
                return;
            }

            // 低版本路径
            var metrics = new DisplayMetrics();
#pragma warning disable CA1422 // 抑制特定过时警告
            windowManager.DefaultDisplay.GetMetrics(metrics);
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
