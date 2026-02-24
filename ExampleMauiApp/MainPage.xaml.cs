using Flow.PDFView.Abstractions;
using PdfPageChangedEventArgs = Flow.PDFView.Abstractions.PageChangedEventArgs;
#if MACCATALYST
using Foundation;
using UIKit;
#endif

namespace ExampleMauiApp;

public partial class MainPage : ContentPage
{
	private const string DefaultSampleUrl = "https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf";

	private int _currentPageIndex;
	private int _totalPageCount;
	private bool _isInitialLoadDone;
	private int _currentSearchIndex = -1;
	private IReadOnlyList<PdfSearchResult> _searchResults = Array.Empty<PdfSearchResult>();

	public MainPage()
	{
		InitializeComponent();
		InitializeControls();
		PdfViewer.SearchResultsFound += OnSearchResultsFound;
		PdfViewer.SearchProgress += OnSearchProgress;
		Loaded += OnPageLoaded;
	}

	private void InitializeControls()
	{
		UrlEntry.Text = DefaultSampleUrl;

		foreach (var item in Enum.GetNames<PdfDisplayMode>())
			DisplayModePicker.Items.Add(item);

		foreach (var item in Enum.GetNames<PdfScrollOrientation>())
			OrientationPicker.Items.Add(item);

		foreach (var item in Enum.GetNames<FitPolicy>())
			FitPolicyPicker.Items.Add(item);

		DisplayModePicker.SelectedIndex = (int)PdfDisplayMode.SinglePageContinuous;
		OrientationPicker.SelectedIndex = (int)PdfScrollOrientation.Vertical;
		FitPolicyPicker.SelectedIndex = (int)FitPolicy.Width;

		PdfViewer.EnableZoom = EnableZoomSwitch.IsToggled;
		PdfViewer.EnableSwipe = EnableSwipeSwitch.IsToggled;
		PdfViewer.EnableLinkNavigation = EnableLinkSwitch.IsToggled;
		PdfViewer.Zoom = (float)ZoomSlider.Value;
		SetToolbarVisible(false);
		UpdateSearchNavState();

		UpdatePageIndicators();
	}

	private async void OnPageLoaded(object? sender, EventArgs e)
	{
		if (_isInitialLoadDone)
			return;

		_isInitialLoadDone = true;
		await LoadFromUrlAsync(UrlEntry.Text, showAlertOnError: false);
	}

	private async void OnLoadUrlClicked(object? sender, EventArgs e)
	{
		await LoadFromUrlAsync(UrlEntry.Text, showAlertOnError: true);
	}

	private async void OnLoadSampleClicked(object? sender, EventArgs e)
	{
		UrlEntry.Text = DefaultSampleUrl;
		await LoadFromUrlAsync(DefaultSampleUrl, showAlertOnError: true);
	}

	private async Task LoadFromUrlAsync(string? input, bool showAlertOnError)
	{
		if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
		{
			SetStatus("URL 无效");
			if (showAlertOnError)
				await DisplayAlertAsync("URL 无效", "请输入完整的绝对 URL。", "确定");
			return;
		}

		PdfViewer.Source = new UriPdfSource(uri);
		SourceInfoLabel.Text = $"来源: URL ({uri.Host})";
		SetStatus("正在加载 URL PDF...");
	}

	private async void OnPickFileClicked(object? sender, EventArgs e)
	{
		try
		{
#if MACCATALYST
			var picked = await PickPdfWithNativePickerAsync();
			if (picked is null)
			{
				SetStatus("已取消文件选择");
				return;
			}

			PdfViewer.Source = new BytesPdfSource(picked.Data);
			SourceInfoLabel.Text = $"来源: 本地文件 ({picked.FileName})";
			SetStatus("正在加载本地 PDF...");
			return;
#else
			var result = await FilePicker.Default.PickAsync(new PickOptions
			{
				PickerTitle = "选择一个 PDF 文件"
			});

			if (result is null)
			{
				SetStatus("已取消文件选择");
				return;
			}

			if (!string.Equals(Path.GetExtension(result.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
			{
				SetStatus("请选择 .pdf 文件");
				return;
			}

			await using var stream = await result.OpenReadAsync();
			using var memory = new MemoryStream();
			await stream.CopyToAsync(memory);
			var data = memory.ToArray();
			if (data.Length == 0)
			{
				SetStatus("选择文件失败: 文件为空");
				return;
			}

			PdfViewer.Source = new BytesPdfSource(data);
			SourceInfoLabel.Text = $"来源: 本地文件 ({result.FileName})";
			SetStatus("正在加载本地 PDF...");
#endif
		}
		catch (Exception ex)
		{
			SetStatus($"选择文件失败: {ex.Message}");
		}
	}

#if MACCATALYST
	private async Task<PickedPdfData?> PickPdfWithNativePickerAsync()
	{
		var presenter = GetTopViewController();
		if (presenter == null)
		{
			return null;
		}

		var tcs = new TaskCompletionSource<PickedPdfData?>(TaskCreationOptions.RunContinuationsAsynchronously);
		var picker = new UIDocumentPickerViewController(new[] { "com.adobe.pdf" }, UIDocumentPickerMode.Import)
		{
			AllowsMultipleSelection = false
		};

		DocumentPickerDelegate? pickerDelegate = null;
		pickerDelegate = new DocumentPickerDelegate(
			onPicked: async url =>
			{
				try
				{
					if (url == null || string.IsNullOrWhiteSpace(url.Path))
					{
						tcs.TrySetResult(null);
						return;
					}

					var fileName = Path.GetFileName(url.Path);
					var bytes = await File.ReadAllBytesAsync(url.Path);
					if (bytes.Length == 0)
					{
						tcs.TrySetResult(null);
						return;
					}

					tcs.TrySetResult(new PickedPdfData(fileName, bytes));
				}
				catch (Exception ex)
				{
					tcs.TrySetException(ex);
				}
			},
			onCancelled: () => tcs.TrySetResult(null));

		picker.Delegate = pickerDelegate;
		presenter.PresentViewController(picker, true, null);

		var result = await tcs.Task;
		picker.DismissViewController(true, null);
		picker.Delegate = null;
		pickerDelegate.Dispose();
		picker.Dispose();
		return result;
	}

	private static UIViewController? GetTopViewController()
	{
		var scene = UIApplication.SharedApplication.ConnectedScenes
			.OfType<UIWindowScene>()
			.FirstOrDefault(s => s.ActivationState == UISceneActivationState.ForegroundActive);
		var window = scene?.Windows?.FirstOrDefault(w => w.IsKeyWindow) ?? scene?.Windows?.FirstOrDefault();
		var controller = window?.RootViewController;

		while (controller?.PresentedViewController != null)
		{
			controller = controller.PresentedViewController;
		}

		return controller;
	}

	private sealed class DocumentPickerDelegate : UIDocumentPickerDelegate
	{
		private readonly Func<NSUrl?, Task> _onPicked;
		private readonly Action _onCancelled;

		public DocumentPickerDelegate(Func<NSUrl?, Task> onPicked, Action onCancelled)
		{
			_onPicked = onPicked;
			_onCancelled = onCancelled;
		}

		public override async void DidPickDocument(UIDocumentPickerViewController controller, NSUrl url)
		{
			await _onPicked(url);
		}

		public override async void DidPickDocument(UIDocumentPickerViewController controller, NSUrl[] urls)
		{
			await _onPicked(urls.FirstOrDefault());
		}

		public override void WasCancelled(UIDocumentPickerViewController controller)
		{
			_onCancelled();
		}
	}

	private sealed record PickedPdfData(string FileName, byte[] Data);
#endif

	private void OnReloadClicked(object? sender, EventArgs e)
	{
		PdfViewer.Reload();
		SetStatus("已触发重载");
	}

	private void OnToolbarToggleClicked(object? sender, EventArgs e)
	{
		SetToolbarVisible(true);
	}

	private void OnToolbarCloseClicked(object? sender, EventArgs e)
	{
		SetToolbarVisible(false);
	}

	private async void OnSearchClicked(object? sender, EventArgs e)
	{
		// 统一调用 FlowPDFView 公共搜索 API，避免平台私有反射导致的不稳定跳转。
		var query = SearchEntry.Text?.Trim();
		if (string.IsNullOrWhiteSpace(query))
		{
			SetSearchStatus("搜索: 请输入关键词");
			return;
		}

		if (!PdfViewer.IsSearchSupported)
		{
			SetSearchStatus("搜索: 当前平台暂不支持");
			return;
		}

		try
		{
			var options = new PdfSearchOptions
			{
				Highlight = SearchHighlightSwitch.IsToggled,
				SearchAllPages = true,
				MaxResults = 200
			};

			_searchResults = await PdfViewer.SearchAsync(query, options);
			_currentSearchIndex = _searchResults.Count > 0 ? 0 : -1;
			UpdateSearchNavState();

			if (_currentSearchIndex >= 0)
			{
				PdfViewer.GoToSearchResult(_currentSearchIndex);
				SetSearchStatus($"搜索: 命中 {_searchResults.Count} 项，当前 1/{_searchResults.Count}");
			}
			else
			{
				SetSearchStatus("搜索: 未找到结果");
			}
		}
		catch (Exception ex)
		{
			SetSearchStatus($"搜索失败: {ex.Message}");
		}
	}

	private void OnSearchPrevClicked(object? sender, EventArgs e)
	{
		GoToSearchResultWithOffset(-1);
	}

	private void OnSearchNextClicked(object? sender, EventArgs e)
	{
		GoToSearchResultWithOffset(1);
	}

	private void OnSearchClearClicked(object? sender, EventArgs e)
	{
		_searchResults = Array.Empty<PdfSearchResult>();
		_currentSearchIndex = -1;
		UpdateSearchNavState();
		SetSearchStatus("搜索: 已清除");
		PdfViewer.ClearSearch();
	}

	private void OnSearchHighlightToggled(object? sender, ToggledEventArgs e)
	{
		if (!PdfViewer.IsSearchSupported)
		{
			SetSearchStatus("搜索高亮: 当前平台暂不支持");
			return;
		}

		PdfViewer.HighlightSearchResults(e.Value);
		SetSearchStatus($"搜索高亮: {(e.Value ? "已开启" : "已关闭")}");
	}

	private void OnPrevPageClicked(object? sender, EventArgs e)
	{
		// 使用控件实时页码，避免依赖事件同步时序导致“下一页/上一页无反应”。
		var currentPage = Math.Clamp(PdfViewer.CurrentPage, 0, Math.Max(0, _totalPageCount - 1));
		if (currentPage <= 0)
			return;

		PdfViewer.GoToPage(currentPage - 1);
	}

	private void OnNextPageClicked(object? sender, EventArgs e)
	{
		var currentPage = Math.Clamp(PdfViewer.CurrentPage, 0, Math.Max(0, _totalPageCount - 1));
		if (currentPage + 1 >= _totalPageCount)
			return;

		PdfViewer.GoToPage(currentPage + 1);
	}

	private void OnDisplayModeChanged(object? sender, EventArgs e)
	{
		if (DisplayModePicker.SelectedIndex < 0)
			return;

		PdfViewer.DisplayMode = (PdfDisplayMode)DisplayModePicker.SelectedIndex;
		SetStatus($"显示模式: {PdfViewer.DisplayMode}");
	}

	private void OnOrientationChanged(object? sender, EventArgs e)
	{
		if (OrientationPicker.SelectedIndex < 0)
			return;

		PdfViewer.ScrollOrientation = (PdfScrollOrientation)OrientationPicker.SelectedIndex;
		SetStatus($"滚动方向: {PdfViewer.ScrollOrientation}");
	}

	private void OnFitPolicyChanged(object? sender, EventArgs e)
	{
		if (FitPolicyPicker.SelectedIndex < 0)
			return;

		PdfViewer.FitPolicy = (FitPolicy)FitPolicyPicker.SelectedIndex;
		SetStatus($"适配策略: {PdfViewer.FitPolicy}");
	}

	private void OnEnableZoomToggled(object? sender, ToggledEventArgs e)
	{
		PdfViewer.EnableZoom = e.Value;
		SetStatus($"启用缩放: {(e.Value ? "是" : "否")}");
	}

	private void OnEnableSwipeToggled(object? sender, ToggledEventArgs e)
	{
		PdfViewer.EnableSwipe = e.Value;
		SetStatus($"启用滑动: {(e.Value ? "是" : "否")}");
	}

	private void OnEnableLinkToggled(object? sender, ToggledEventArgs e)
	{
		PdfViewer.EnableLinkNavigation = e.Value;
		SetStatus($"启用链接跳转: {(e.Value ? "是" : "否")}");
	}

	private void OnZoomSliderValueChanged(object? sender, ValueChangedEventArgs e)
	{
		var zoom = MathF.Round((float)e.NewValue, 2);
		PdfViewer.Zoom = zoom;
		ZoomValueLabel.Text = $"{zoom:0.00}x";
	}

	private void OnDocumentLoaded(object? sender, DocumentLoadedEventArgs e)
	{
		_totalPageCount = e.PageCount;
		_currentPageIndex = Math.Clamp(PdfViewer.CurrentPage, 0, Math.Max(0, _totalPageCount - 1));
		UpdatePageIndicators();
		SetStatus($"文档加载完成，共 {_totalPageCount} 页");
	}

	private void OnPageChanged(object? sender, PdfPageChangedEventArgs e)
	{
		_currentPageIndex = e.PageIndex;
		_totalPageCount = e.PageCount;
		UpdatePageIndicators();
	}

	private void OnPdfError(object? sender, PdfErrorEventArgs e)
	{
		SetStatus($"加载失败: {e.Message}");
	}

	private void OnSearchResultsFound(object? sender, PdfSearchResultsEventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			_searchResults = e.Results;
			_currentSearchIndex = _searchResults.Count > 0 ? Math.Clamp(e.CurrentIndex, 0, _searchResults.Count - 1) : -1;
			UpdateSearchNavState();

			if (_searchResults.Count > 0)
			{
				SetSearchStatus($"搜索: 命中 {_searchResults.Count} 项，当前 {_currentSearchIndex + 1}/{_searchResults.Count}");
			}
			else if (!string.IsNullOrWhiteSpace(e.Query))
			{
				SetSearchStatus("搜索: 未找到结果");
			}
		});
	}

	private void OnSearchProgress(object? sender, PdfSearchProgressEventArgs e)
	{
		if (_currentSearchIndex < 0)
		{
			MainThread.BeginInvokeOnMainThread(() =>
				SetSearchStatus($"搜索中: {e.CurrentPage}/{e.TotalPages}，命中 {e.ResultCount}"));
		}
	}

	private void UpdatePageIndicators()
	{
		if (_totalPageCount <= 0)
		{
			PageInfoLabel.Text = "页码: - / -";
			PrevPageButton.IsEnabled = false;
			NextPageButton.IsEnabled = false;
			return;
		}

		// 按控件当前页刷新状态，确保按钮可用性与实际渲染页一致。
		_currentPageIndex = Math.Clamp(PdfViewer.CurrentPage, 0, Math.Max(0, _totalPageCount - 1));
		PageInfoLabel.Text = $"页码: {_currentPageIndex + 1} / {_totalPageCount}";
		PrevPageButton.IsEnabled = _currentPageIndex > 0;
		NextPageButton.IsEnabled = _currentPageIndex + 1 < _totalPageCount;
	}

	private void SetStatus(string message)
	{
		EventInfoLabel.Text = $"状态: {message}";
	}

	private void SetToolbarVisible(bool isVisible)
	{
		ToolbarPanel.IsVisible = isVisible;
		ToolbarToggleButton.IsVisible = !isVisible;
	}

	private void SetSearchStatus(string message)
	{
		SearchStatusLabel.Text = message;
	}

	private void UpdateSearchNavState()
	{
		var hasResults = _searchResults.Count > 0;
		SearchPrevButton.IsEnabled = hasResults;
		SearchNextButton.IsEnabled = hasResults;
	}

	private void GoToSearchResultWithOffset(int offset)
	{
		if (_searchResults.Count == 0)
			return;

		if (!PdfViewer.IsSearchSupported)
			return;

		_currentSearchIndex = (_currentSearchIndex + offset + _searchResults.Count) % _searchResults.Count;
		PdfViewer.GoToSearchResult(_currentSearchIndex);
		SetSearchStatus($"搜索: 命中 {_searchResults.Count} 项，当前 {_currentSearchIndex + 1}/{_searchResults.Count}");
	}
}
