using Flow.PDFView.Abstractions;
using System.Windows.Input;

namespace Flow.PDFView
{
    /// <summary>
    /// PDF 视图接口（外层）。继承 `IPdfViewCore` 以使用 Core 中的契约。
    /// Outer PDF view interface that inherits the core contract from `IPdfViewCore`.
    /// </summary>
    public interface IPdfView : IView, IPdfViewCore
    {
        // 保留此接口作为对外的稳定表面层（facade）。
        // Keep this interface as the public facade surface for consumers.
    }
}
