namespace Flow.PDFView
{
    /// <summary>
    /// MAUI 应用构建器扩展方法类
    /// </summary>
    public static class AppBuilderExtensions
    {
        /// <summary>
        /// 注册 PDF 视图处理器到 MAUI 应用
        /// </summary>
        /// <param name="builder">MAUI 应用构建器</param>
        /// <returns>MAUI 应用构建器</returns>
        public static MauiAppBuilder UseMauiPdfView(this MauiAppBuilder builder)
        {
            builder.ConfigureMauiHandlers((handlers) =>
            {
#if ANDROID
                handlers.AddHandler(typeof(PdfView), typeof(Platforms.Android.PdfViewHandler));
#elif IOS
                handlers.AddHandler(typeof(PdfView), typeof(Platforms.iOS.PdfViewHandler));
#elif MACCATALYST
                handlers.AddHandler(typeof(PdfView), typeof(Platforms.MacCatalyst.PdfViewHandler));
#elif WINDOWS
                handlers.AddHandler(typeof(PdfView), typeof(Platforms.Windows.PdfViewHandler));
#endif
            });
            return builder;
        }
    }
}
