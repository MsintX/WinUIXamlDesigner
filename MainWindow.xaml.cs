using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System.Collections.ObjectModel;
using WinUIXamlDesigner.Models;
using WinUIXamlDesigner.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace WinUIXamlDesigner;

public sealed partial class MainWindow : Window {
	public ObservableCollection<ToolboxItem> ToolboxItems { get; } = new()
	{
		new ToolboxItem("Button", "Button"), new ToolboxItem("HyperlinkButton", "HyperlinkButton"), new ToolboxItem("ToggleButton", "ToggleButton"),
		new ToolboxItem("RepeatButton", "RepeatButton"), new ToolboxItem("CheckBox", "CheckBox"), new ToolboxItem("RadioButton", "RadioButton"),
		new ToolboxItem("ToggleSwitch", "ToggleSwitch"), new ToolboxItem("TextBlock", "TextBlock"), new ToolboxItem("TextBox", "TextBox"),
		new ToolboxItem("PasswordBox", "PasswordBox"), new ToolboxItem("AutoSuggestBox", "AutoSuggestBox"), new ToolboxItem("NumberBox", "NumberBox"),
		new ToolboxItem("RichEditBox", "RichEditBox"), new ToolboxItem("ComboBox", "ComboBox"), new ToolboxItem("ListView", "ListView"),
		new ToolboxItem("ListBox", "ListBox"), new ToolboxItem("TreeView", "TreeView"), new ToolboxItem("ItemsRepeater", "ItemsRepeater"),
		new ToolboxItem("Slider", "Slider"), new ToolboxItem("ProgressBar", "ProgressBar"), new ToolboxItem("ProgressRing", "ProgressRing"),
		new ToolboxItem("Image", "Image"), new ToolboxItem("Icon", "Icon"), new ToolboxItem("FontIcon", "FontIcon"),
		new ToolboxItem("DatePicker", "DatePicker"), new ToolboxItem("CalendarDatePicker", "CalendarDatePicker"), new ToolboxItem("TimePicker", "TimePicker"),
		new ToolboxItem("CalendarView", "CalendarView"), new ToolboxItem("Expander", "Expander"), new ToolboxItem("InfoBar", "InfoBar"),
		new ToolboxItem("RatingControl", "RatingControl"), new ToolboxItem("NavigationView", "NavigationView"), new ToolboxItem("Pivot", "Pivot"),
		new ToolboxItem("TabView", "TabView"), new ToolboxItem("ScrollViewer", "ScrollViewer"), new ToolboxItem("CommandBar", "CommandBar")
	};

	private readonly XamlDocumentService _xaml = new();
	private readonly CSharpEventService _csharp = new();
	private GridDocument? _document;
	private GridDocument? _savedSnapshot;
	private ControlNode? _selected;
	private bool _windowSelected = true;
	private ToolboxItem? _pendingTool;
	private bool _ignorePropertyChanges;
	private bool _dragging;
	private readonly Stack<GridDocument> _undo = new();
	private readonly Stack<GridDocument> _redo = new();
	private int _dragOriginalRow;
	private int _dragOriginalColumn;
	private int _dragOriginalRowSpan;
	private int _dragOriginalColumnSpan;
	private int _hoverRow;
	private int _hoverColumn;
	private bool _hasHoverCell;

	private const double DesignPreviewBaseWidth = 900;
	private const double DesignPreviewBaseHeight = 662;
	private const double MinDesignZoom = 0.25;
	private const double MaxDesignZoom = 3.00;
	private double _designZoom = 1.0;

	private readonly string[] _tips =
	{
		"直接从工具箱拖到画布，无需先选中控件。",
		"单击画布空白区域即可选择 Window（窗口）。",
		"Ctrl+S 会先提交当前正在编辑的属性，再保存到 XAML。",
		"单行输入框可以按 Enter 提交；多行输入框会保留换行行为。",
		"拖动控件时只更新内存模型，松手后才保存文件。",
		"x:Name（控件名称）可以自动派生 Click 或 Tapped 事件。",
		"Visual Studio 检测到外部 XAML 修改后，可以选择重新加载。",
		"Grid（网格布局）目前优先使用相对布局，每个单元格按 * 均分。",
		"不支持的复杂 XAML 结构会尽量保留，而不是静默删除。",
		"属性面板会根据当前控件类型自动启用或禁用相关编辑项。",
		"删除行或列前，Designer 会检查是否会影响现有控件。",
		"窗口标题栏预览会跟随 Window 的 Title（标题）属性更新。",
		"双击画布空白区域可以为根内容元素添加 Loaded 事件订阅。",
		"拖动控件时，按住 Shift 可以锁定水平或垂直方向。",
		"拖动控件时，按住 Ctrl 可以复制控件而不是移动。",
		"拖动控件时，按住 Alt 可以在网格中自由移动，而不受单元格限制。",
		"拖动控件时，按住 Ctrl+Shift 可以复制并锁定方向。",
		"在属性面板中，输入框支持粘贴 XAML 片段来设置复杂属性。",
		"在属性面板中，ComboBox 下拉列表会根据控件类型显示可选值。",
		"WinUI XAML Designer仍在Demo（演示）阶段！可能还有很多Bug没修！",
		"如果你喜欢这个工具，请考虑给MsintX点个Star，或者在GitHub上提交Bug和建议。",
		"你知道吗？MsintX是一名初一生！还tm寄宿！",
		"Hello Coder!",
		"项目立项于2026.9.19！",
		"其实这个软件的作者MsintX是不仅是音游入，还是名wmc！",
		"学好数理化，走遍天下都不怕！",
		"你醒啦？请你复习一下数轴、相反数、绝对值、倒数、乘方、有理数、有理数的加 减 乘 除还有三元一次方程吧！",
		"我要网暴这个C#，回来吧面向过程，我最骄傲的信仰↑↑",
		"C++ ×\nCNM √",
		"闹吃vs古振兴，谁才是赢家？",
		"xxx xxx xxxxxxx",
		"x x xxx",
		"VS自动补全别捣乱行不",
		"我要的面向var编程哪去了，为什么我写var(var)var会报错",
		"请投入硬币，要开始了哟，欢迎回来！",
		"我是臀萌 + 句号。",
		"UWP好看？跟我的SandBox、生命周期、Store分发、旁加载说去吧",
		"没人觉得Metro Design(Modern UI)很好看嘛",
		"WinUI 3的生命周期和UWP的生命周期不一样，WinUI 3的生命周期是Win32的生命周期",
		"前面忘了，中间忘了，后面忘了",
		"我把春、观沧海、次北固山下、闻王昌龄左迁龙标遥有此寄、天净沙·秋思都背完了！！",
		"Visual Studio自动补全那么牛逼能不能帮我把2000+ error的报错补全成not error found啊",
		"BiliBili关注MsintX谢谢喵，YouTube订阅MsintX谢谢喵",
		"我想要一个全是“awmc”的评论区",
		"A：你这tip怎么内嵌在cs里面啊\nQ：json多难写，解析json的NuGet引用多麻烦，你就忍忍呗（手动doge",
		"像素方块的硬核才是王道你的卡通画风根本没技巧\n萌趣的世界才受大众喜爱你的硬核玩法早就被时代落败",
		"雷军！金凡！",
		"7月份才想起澎湃解bl通道在1月份就关了，喂我花生喂我花生",
		"这种粉丝少的up整活最狠了",
		"中秋节当天，有人在吃月饼，有人在赏月，而我就不一样了，我在Phigros 4.0.0 Update",
		"“难道没人觉得一段文字加上双引号和英文句号会很高级吗.”",
		"VS的IntelliSense错误列表就是lj",
		"截至目前，WinUI XAML Designer已经有超过0人的下载量了！",
		"各位Watcher们能不能帮我写完作业，能写完的自动获得美国核弹发射权",
		"你知道吗？WinUI XAML Designer的第一个Release预计在中秋发布！"
	};

	private int _lastTipIndex = -1;
	private DispatcherQueueTimer? _tipTimer;

	public MainWindow() {
		InitializeComponent();
		ToolboxList.ItemsSource = ToolboxItems;
		_windowSelected = true;
		ShowRandomTip();
		_tipTimer = DispatcherQueue.GetForCurrentThread()?.CreateTimer();
		if (_tipTimer is not null) {
			_tipTimer.Interval = TimeSpan.FromMinutes(1);
			_tipTimer.Tick += TipTimer_Tick;
			_tipTimer.Start();
		}
		Closed += MainWindow_Closed;
		UpdateUi();
	}

	private void TipTimer_Tick(DispatcherQueueTimer sender, object args) {
		ShowRandomTip();
	}

	private void ShowRandomTip() {
		if (_tips.Length == 0) return;

		var next = _tips.Length == 1
			? 0
			: Random.Shared.Next(_tips.Length);

		if (_tips.Length > 1) {
			while (next == _lastTipIndex)
				next = Random.Shared.Next(_tips.Length);
		}

		_lastTipIndex = next;
		if (TipText is not null)
			TipText.Text = $"Tip: {_tips[next]}";
	}

	private void MainWindow_Closed(object sender, WindowEventArgs args) {
		if (_tipTimer is not null) {
			_tipTimer.Stop();
			_tipTimer.Tick -= TipTimer_Tick;
		}
	}

	private async void OpenXaml_Click(object sender, RoutedEventArgs e) {
		var picker = new FileOpenPicker();
		picker.FileTypeFilter.Add(".xaml");
		InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
		var file = await picker.PickSingleFileAsync();
		if (file is null) return;
		await LoadDocumentAsync(file.Path);
	}

	private async void OpenProject_Click(object sender, RoutedEventArgs e) {
		var picker = new FileOpenPicker();
		picker.FileTypeFilter.Add(".csproj");
		InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
		var project = await picker.PickSingleFileAsync();
		if (project is null) return;

		var files = ProjectLocator.FindXamlFiles(project.Path);
		if (files.Count == 0) {
			await ShowErrorAsync("项目中没有可编辑的 XAML 文件。App.xaml 以及 bin/obj 下文件已排除。");
			return;
		}

		var combo = new ComboBox { ItemsSource = files, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
		var dialog = CreateDialog(
			title: "选择要编辑的 XAML",
			content: combo,
			primaryButtonText: "打开",
			closeButtonText: "取消",
			defaultButton: ContentDialogButton.Primary);
		if (await dialog.ShowAsync() == ContentDialogResult.Primary && combo.SelectedItem is string path)
			await LoadDocumentAsync(path);
	}

	private async Task LoadDocumentAsync(string path) {
		try {
			if (XamlDocumentService.IsBlankFile(path)) {
				var createDialog = CreateDialog(
					title: "XAML 文件为空",
					content: "这个 XAML 文件目前没有任何内容。\n\n要为它创建一个默认的 1 × 1 Grid（网格布局）吗？",
					primaryButtonText: "创建默认 Grid",
					closeButtonText: "取消",
					defaultButton: ContentDialogButton.Primary);

				if (await createDialog.ShowAsync() != ContentDialogResult.Primary) {
					StatusText.Text = "已取消打开空白 XAML。";
					return;
				}

				await File.WriteAllTextAsync(path, XamlDocumentService.DefaultGridXaml);
			}

			_document = _xaml.Load(path);
			_document.CodeBehindPath = ProjectLocator.FindCodeBehind(path);
			_savedSnapshot = _document.Clone();
			_selected = null;
			_windowSelected = true;
			_pendingTool = null;
			_hasHoverCell = false;
			_undo.Clear();
			_redo.Clear();
			RenderCanvas();
			UpdateUi();
			var ignoredHint = _document.IgnoredElementCount > 0
				? $"；有 {_document.IgnoredElementCount} 个非设计控件/容器已保留但不参与编辑"
				: "";
			StatusText.Text = _document.CodeBehindPath is null
				? $"已打开：{path}；未找到同名 .xaml.cs{ignoredHint}"
				: $"已打开：{path}{ignoredHint}";
		} catch (Exception ex) {
			await ShowErrorAsync(ex.Message);
		}
	}

	private async void Save_Click(object sender, RoutedEventArgs e) => await SaveDocumentAsync();

	private async Task SaveDocumentAsync() {
		if (_document is null) return;
		try {
			await CommitFocusedEditorAsync();
			_document.Normalize();
			await new WriteBackService(_xaml, _csharp).WriteAsync(_document, _savedSnapshot);
			_savedSnapshot = _document.Clone();
			StatusText.Text = $"已保存：{_document.FilePath}；Visual Studio 可重载外部修改。";
			UpdateUi();
		} catch (Exception ex) {
			await ShowErrorAsync($"保存失败：{ex.Message}");
		}
	}

	private async void Reload_Click(object sender, RoutedEventArgs e) {
		if (_document is null) return;
		if (_document.IsDirty) {
			var dialog = CreateDialog(
				title: "重新加载",
				content: "当前有未保存的内存修改，重新加载会丢弃这些修改。",
				primaryButtonText: "重新加载",
				closeButtonText: "取消",
				defaultButton: ContentDialogButton.Close);
			if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
		}
		await LoadDocumentAsync(_document.FilePath);
	}

	private async void About_Click(object sender, RoutedEventArgs e) {
		var content = new StackPanel { Spacing = 12, MaxWidth = 620 };
		content.Children.Add(new TextBlock {
			Text = "WinUI XAML Designer",
			FontSize = 28,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
		});
		content.Children.Add(new TextBlock {
			Text = "独立于 Visual Studio 的轻量级 WinUI XAML 可视化编辑器。",
			TextWrapping = TextWrapping.Wrap,
			Opacity = 0.8
		});
		content.Children.Add(new TextBlock {
			Text = "作者：MisntX\n辅助开发：ChatGPT",
			FontSize = 16
		});
		content.Children.Add(new TextBlock {
			Text = "Includes small AIGC",
			FontSize = 18,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold
		});
		content.Children.Add(new TextBlock {
			Text = "技术栈（Technology Stack）：WinUI（WinUI 3）、.NET 8、Windows App SDK、Roslyn（C# 语法分析与代码编辑）",
			TextWrapping = TextWrapping.Wrap
		});
		content.Children.Add(new TextBlock {
			Text = "如果你喜欢这个工具，请考虑给颗star，谢谢喵！",
			TextWrapping = TextWrapping.Wrap
		});

		var dialog = CreateDialog(
			title: "关于我们",
			content: content,
			closeButtonText: "确定",
			defaultButton: ContentDialogButton.Close);
		await dialog.ShowAsync();
	}

	private void Toolbox_DragItemsStarting(object sender, DragItemsStartingEventArgs e) {
		if (e.Items.FirstOrDefault() is not ToolboxItem item)
			return;

		e.Data.SetText(item.TypeName);
		e.Data.Properties.Title = item.DisplayName;
		e.Data.Properties.Description = "拖到设计画布中以放置控件";
		_pendingTool = item;
		_hasHoverCell = false;
		StatusText.Text = $"正在拖动 {item.DisplayName}：松手放置到目标格。";
		RenderCanvas();
	}

	private void Toolbox_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args) {
		if (args.DropResult == DataPackageOperation.None) {
			_pendingTool = null;
			_hasHoverCell = false;
			StatusText.Text = "已取消拖动。";
			RenderCanvas();
		}
	}

	private void DesignCanvas_DragOver(object sender, DragEventArgs e) {
		if (_document is null || !e.DataView.Contains(StandardDataFormats.Text)) {
			e.AcceptedOperation = DataPackageOperation.None;
			return;
		}

		e.AcceptedOperation = DataPackageOperation.Copy;
		var point = e.GetPosition(DesignCanvas);
		(_hoverRow, _hoverColumn) = PointToCell(point);
		_hasHoverCell = true;
		RenderCanvas();
		e.Handled = true;
	}

	private void DesignCanvas_DragLeave(object sender, DragEventArgs e) {
		if (_dragging) return;
		_hasHoverCell = false;
		RenderCanvas();
	}

	private async void DesignCanvas_Drop(object sender, DragEventArgs e) {
		if (_document is null || !e.DataView.Contains(StandardDataFormats.Text))
			return;

		try {
			var typeName = await e.DataView.GetTextAsync();
			var item = ToolboxItems.FirstOrDefault(t => t.TypeName == typeName);
			if (item is null)
				return;

			CommitHistory();
			var point = e.GetPosition(DesignCanvas);
			var (row, col) = PointToCell(point);
			var node = new ControlNode {
				TypeName = item.TypeName,
				Content = DefaultContent(item.TypeName),
				Row = row,
				Column = col,
				XName = MakeUniqueName(item.TypeName),
				EventName = null,
				EventKind = null,
				EventWasAutoGenerated = false
			};
			_document.Nodes.Add(node);
			_document.IsDirty = true;
			_pendingTool = null;
			_hasHoverCell = false;
			Select(node);
			await AutoWriteAsync();
			e.AcceptedOperation = DataPackageOperation.Copy;
			e.Handled = true;
		} catch (Exception ex) {
			_pendingTool = null;
			_hasHoverCell = false;
			await ShowErrorAsync($"放置控件失败：{ex.Message}");
		} finally {
			RenderCanvas();
		}
	}

	private void DesignCanvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e) {
		if (_document is null)
			return;

		var point = e.GetCurrentPoint(DesignPreviewScrollViewer);
		var ctrlDown = InputKeyboardSource
			.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
			.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

		if (!ctrlDown || point.Properties.MouseWheelDelta == 0)
			return;

		var oldZoom = _designZoom;
		var step = point.Properties.MouseWheelDelta > 0 ? 1.1 : 1.0 / 1.1;
		_designZoom = Math.Clamp(_designZoom * step, MinDesignZoom, MaxDesignZoom);

		if (Math.Abs(_designZoom - oldZoom) < 0.0001) {
			e.Handled = true;
			return;
		}

		UpdateDesignZoom();
		e.Handled = true;
	}

	private void DesignCanvas_PointerPressed(object sender, PointerRoutedEventArgs e) {
		if (_document is null) return;
		DesignCanvas.Focus(FocusState.Pointer);
		var point = e.GetCurrentPoint(DesignCanvas).Position;

		if (_pendingTool is not null) {
			CommitHistory();
			var (row, col) = PointToCell(point);
			var node = new ControlNode {
				TypeName = _pendingTool.TypeName,
				Content = DefaultContent(_pendingTool.TypeName),
				Row = row,
				Column = col,
				XName = MakeUniqueName(_pendingTool.TypeName),
				EventName = null,
				EventKind = null,
				EventWasAutoGenerated = false
			};
			_document.Nodes.Add(node);
			_pendingTool = null;
			_document.IsDirty = true;
			Select(node);
			_ = AutoWriteAsync();
			e.Handled = true;
			return;
		}

		var hit = FindNodeAt(point);
		if (hit is null) {
			_selected = null;
			_windowSelected = true;
			RenderCanvas();
			UpdateUi();
			return;
		}

		Select(hit);
		_dragOriginalRow = hit.Row;
		_dragOriginalColumn = hit.Column;
		_dragOriginalRowSpan = hit.RowSpan;
		_dragOriginalColumnSpan = hit.ColumnSpan;
		CommitHistory();
		_dragging = true;
		DesignCanvas.CapturePointer(e.Pointer);
		e.Handled = true;
	}

	private void DesignCanvas_PointerMoved(object sender, PointerRoutedEventArgs e) {
		if (_document is null) return;
		var point = e.GetCurrentPoint(DesignCanvas).Position;
		var (row, col) = PointToCell(point);
		_hoverRow = row;
		_hoverColumn = col;
		_hasHoverCell = true;

		if (_dragging && _selected is not null) {
			row = Math.Clamp(row, 0, Math.Max(0, _document.Rows - _selected.RowSpan));
			col = Math.Clamp(col, 0, Math.Max(0, _document.Columns - _selected.ColumnSpan));
			_selected.Row = row;
			_selected.Column = col;
			_document.IsDirty = true;
		}

		RenderCanvas();
	}

	private async void DesignCanvas_PointerReleased(object sender, PointerRoutedEventArgs e) {
		if (!_dragging || _selected is null || _document is null) return;
		_dragging = false;
		DesignCanvas.ReleasePointerCapture(e.Pointer);

		if (_selected.Row == _dragOriginalRow && _selected.Column == _dragOriginalColumn &&
			_selected.RowSpan == _dragOriginalRowSpan && _selected.ColumnSpan == _dragOriginalColumnSpan) {
			if (_undo.Count > 0) _undo.Pop();
		}

		await AutoWriteAsync();
		StatusText.Text = _document.IsDirty ? "有未保存变更" : "已同步";
		RenderCanvas();
	}

	private void DesignCanvas_PointerCanceled(object sender, PointerRoutedEventArgs e) {
		if (!_dragging || _selected is null) return;
		_selected.Row = _dragOriginalRow;
		_selected.Column = _dragOriginalColumn;
		_selected.RowSpan = _dragOriginalRowSpan;
		_selected.ColumnSpan = _dragOriginalColumnSpan;
		_dragging = false;
		DesignCanvas.ReleasePointerCapture(e.Pointer);
		RenderCanvas();
		UpdateUi();
	}

	private void DesignCanvas_PointerExited(object sender, PointerRoutedEventArgs e) {
		if (_dragging || _pendingTool is not null) return;
		_hasHoverCell = false;
		RenderCanvas();
	}

	private async void DesignCanvas_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
		if (_document is null || FindNodeAt(e.GetPosition(DesignCanvas)) is not null) return;
		if (_document.CodeBehindPath is null || !File.Exists(_document.CodeBehindPath)) {
			await ShowErrorAsync("没有找到对应的 .xaml.cs，无法生成 Loaded。\nWinUI 3 的 Window 没有 Loaded；此功能会给根内容元素添加 Loaded 订阅。");
			return;
		}

		try {
			var before = _document.Clone();
			CommitHistory();
			if (string.IsNullOrWhiteSpace(_document.RootXName)) _document.RootXName = "ContentRoot";
			var cs = await File.ReadAllTextAsync(_document.CodeBehindPath);
			var updated = _csharp.EnsureLoadedHandler(cs, _document.RootXName, "Window_Loaded");
			var xamlText = _xaml.Serialize(_document);
			await WriteBackService.CommitRawPairAsync(_document.FilePath, xamlText, _document.CodeBehindPath, updated);
			_document.OriginalXamlText = xamlText;
			_document.IsDirty = false;
			_savedSnapshot = _document.Clone();
			StatusText.Text = "已生成 Loaded；Visual Studio 可重载外部修改。";
			RenderCanvas();
			UpdateUi();
		} catch (Exception ex) {
			if (_document is not null && _savedSnapshot is not null)
				_document.CopyFrom(_savedSnapshot);
			RenderCanvas();
			UpdateUi();
			await ShowErrorAsync($"生成 Loaded 失败，已回滚：{ex.Message}");
		}
	}

	private void DesignCanvas_RightTapped(object sender, RightTappedRoutedEventArgs e) {
		_pendingTool = null;
		RenderCanvas();
		StatusText.Text = "已取消放置/拖动。";
	}

	private void DesignCanvas_KeyDown(object sender, KeyRoutedEventArgs e) {
		if (e.Key == Windows.System.VirtualKey.Escape) {
			_pendingTool = null;
			RenderCanvas();
			StatusText.Text = "已取消放置/拖动。";
			e.Handled = true;
		} else if (e.Key == Windows.System.VirtualKey.Delete && _selected is not null) {
			DeleteSelected();
			e.Handled = true;
		}
	}

	private async void Editor_KeyDown(object sender, KeyRoutedEventArgs e) {
		if (e.Key != Windows.System.VirtualKey.Enter || sender is not TextBox box || box.AcceptsReturn)
			return;

		e.Handled = true;
		if (box == NameBox)
			await CommitNameAsync();
		else
			await CommitPropertyTextBoxAsync(box);
	}

	private async void SaveKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) {
		args.Handled = true;
		await SaveDocumentAsync();
	}

	private async Task CommitFocusedEditorAsync() {
		if (_ignorePropertyChanges)
			return;

		var xamlRoot = Content.XamlRoot;
		if (xamlRoot is null)
			return;

		if (FocusManager.GetFocusedElement(xamlRoot) is TextBox box) {
			if (box == NameBox)
				await CommitNameAsync();
			else
				await CommitPropertyTextBoxAsync(box);
		}
	}

	private void NameBox_LostFocus(object sender, RoutedEventArgs e) => _ = CommitNameAsync();

	private async Task CommitNameAsync() {
		if (_ignorePropertyChanges || _selected is null || _document is null) return;
		var newName = NameBox.Text.Trim();
		var oldName = _selected.XName;
		if (string.Equals(newName, oldName, StringComparison.Ordinal)) return;
		if (!string.IsNullOrWhiteSpace(newName) && _document.Nodes.Any(n => !ReferenceEquals(n, _selected) && n.XName == newName)) {
			await ShowErrorAsync($"x:Name 已存在：{newName}");
			RefreshProperties();
			return;
		}

		var before = _document.Clone();
		var beforeNode = _selected.Clone();
		CommitHistory();

		if (string.IsNullOrWhiteSpace(newName)) {
			_selected.XName = null;
			if (_selected.EventWasAutoGenerated) {
				_selected.EventName = null;
				_selected.EventKind = null;
				_selected.EventWasAutoGenerated = false;
			}
		} else {
			_selected.XName = newName;
			if (!beforeNode.EventWasAutoGenerated && !string.IsNullOrWhiteSpace(beforeNode.EventName))
				StatusText.Text = $"已保留用户手写事件：{beforeNode.EventName}";
			if (beforeNode.EventName is null || beforeNode.EventWasAutoGenerated) {
				_selected.EventName = _selected.DerivedEventName;
				_selected.EventKind = _selected.DerivedEventKind;
				_selected.EventWasAutoGenerated = true;
			}
		}

		_document.IsDirty = true;
		RenderCanvas();
		UpdateUi();

		if (AutoWriteToggle.IsChecked == true) {
			try {
				await new WriteBackService(_xaml, _csharp).CommitRenameAsync(before, _document, beforeNode, _selected.Clone());
				_savedSnapshot = _document.Clone();
				UpdateUi();
			} catch (Exception ex) {
				_document.CopyFrom(before);
				_selected = _document.Nodes.FirstOrDefault(n => n.Id == beforeNode.Id);
				RenderCanvas();
				UpdateUi();
				await ShowErrorAsync($"重命名事务已回滚：{ex.Message}");
			}
		}
	}

	private async void PropertyBox_LostFocus(object sender, RoutedEventArgs e) {
		if (sender is not TextBox box) return;
		await CommitPropertyTextBoxAsync(box);
	}

	private async Task CommitPropertyTextBoxAsync(TextBox box) {
		if (_ignorePropertyChanges || _document is null || box.Tag is not string key)
			return;

		if (_windowSelected && _selected is null) {
			var value = string.IsNullOrWhiteSpace(box.Text) ? null : box.Text.Trim();
			_document.RootProperties.TryGetValue(key, out var rootOldValue);
			if (string.Equals(rootOldValue, value, StringComparison.Ordinal)) return;
			CommitHistory();
			if (value is null) _document.RootProperties.Remove(key); else _document.RootProperties[key] = value;
			_document.IsDirty = true;
			UpdateUi();
			await AutoWriteAsync();
			return;
		}

		if (_selected is null) return;

		var propertyKey = key;
		var valueText = string.IsNullOrWhiteSpace(box.Text) ? null : box.Text.Trim();
		string? oldText;
		if (propertyKey == "Width") oldText = _selected.Width;
		else if (propertyKey == "Height") oldText = _selected.Height;
		else if (propertyKey == "ContentOrText") oldText = _selected.Content;
		else oldText = _selected.Properties.TryGetValue(propertyKey, out var current) ? current : null;
		if (string.Equals(oldText, valueText, StringComparison.Ordinal)) return;

		CommitHistory();
		switch (propertyKey) {
			case "Width": _selected.Width = valueText; break;
			case "Height": _selected.Height = valueText; break;
			case "ContentOrText": _selected.Content = valueText; break;
			default:
				_selected.Properties[propertyKey] = valueText ?? string.Empty;
				break;
		}
		_document.IsDirty = true;
		RenderCanvas();
		UpdateUi();
		await AutoWriteAsync();
	}

	private void PropertyCombo_Changed(object sender, SelectionChangedEventArgs e) {
		if (_ignorePropertyChanges || _document is null || sender is not ComboBox combo || combo.Tag is not string key)
			return;
		var value = (combo.SelectedItem as ComboBoxItem)?.Content?.ToString();
		value = string.IsNullOrWhiteSpace(value) ? null : value;

		if (_windowSelected && _selected is null) {
			_document.RootProperties.TryGetValue(key, out var rootOldValue);
			if (string.Equals(rootOldValue, value, StringComparison.Ordinal)) return;
			CommitHistory();
			if (value is null) _document.RootProperties.Remove(key); else _document.RootProperties[key] = value;
			_document.IsDirty = true;
			UpdateUi();
			_ = AutoWriteAsync();
			return;
		}

		if (_selected is null) return;
		string? oldValue;
		if (key == "HorizontalAlignment") oldValue = _selected.HorizontalAlignment;
		else if (key == "VerticalAlignment") oldValue = _selected.VerticalAlignment;
		else oldValue = _selected.Properties.TryGetValue(key, out var current) ? current : null;
		if (string.Equals(oldValue, value, StringComparison.Ordinal)) return;
		CommitHistory();
		switch (key) {
			case "HorizontalAlignment": _selected.HorizontalAlignment = value; break;
			case "VerticalAlignment": _selected.VerticalAlignment = value; break;
			default:
				_selected.Properties[key] = value ?? string.Empty;
				break;
		}
		_document.IsDirty = true;
		RenderCanvas();
		UpdateUi();
		_ = AutoWriteAsync();
	}

	private void GridValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) {
		if (_ignorePropertyChanges || _selected is null || _document is null || double.IsNaN(args.NewValue)) return;
		CommitHistory();
		var value = Math.Max(0, (int)args.NewValue);
		if (sender == RowBox) _selected.Row = Math.Min(value, _document.Rows - 1);
		else if (sender == ColumnBox) _selected.Column = Math.Min(value, _document.Columns - 1);
		else if (sender == RowSpanBox) _selected.RowSpan = Math.Max(1, value);
		else if (sender == ColumnSpanBox) _selected.ColumnSpan = Math.Max(1, value);
		_document.Normalize();
		_document.IsDirty = true;
		RenderCanvas();
		_ = AutoWriteAsync();
	}

	private void DeleteSelected_Click(object sender, RoutedEventArgs e) => DeleteSelected();

	private void DeleteSelected() {
		if (_document is null || _selected is null) return;
		CommitHistory();
		_document.Nodes.Remove(_selected);
		_selected = null;
		_document.IsDirty = true;
		RenderCanvas();
		UpdateUi();
		_ = AutoWriteAsync();
	}

	private void AddRow_Click(object sender, RoutedEventArgs e) => ChangeGrid(+1, 0);
	private void RemoveRow_Click(object sender, RoutedEventArgs e) => ChangeGrid(-1, 0);
	private void AddColumn_Click(object sender, RoutedEventArgs e) => ChangeGrid(0, +1);
	private void RemoveColumn_Click(object sender, RoutedEventArgs e) => ChangeGrid(0, -1);

	private void ChangeGrid(int rowDelta, int colDelta) {
		if (_document is null) return;
		if (!_document.GridSizingSupported) {
			StatusText.Text = "当前 Grid 使用 Auto 或固定尺寸，行列编辑已禁用以避免静默改变原布局。";
			return;
		}
		var rows = Math.Max(1, _document.Rows + rowDelta);
		var cols = Math.Max(1, _document.Columns + colDelta);
		if (rows == _document.Rows && cols == _document.Columns) return;

		if (rows < _document.Rows) {
			var blocked = _document.Nodes.FirstOrDefault(n => n.Row >= rows || n.Row + n.RowSpan > rows);
			if (blocked is not null) {
				StatusText.Text = $"无法删除行：{blocked.TypeName} 位于第 {blocked.Row + 1} 行或跨越目标区域。请先移动/缩小控件。";
				return;
			}
		}
		if (cols < _document.Columns) {
			var blocked = _document.Nodes.FirstOrDefault(n => n.Column >= cols || n.Column + n.ColumnSpan > cols);
			if (blocked is not null) {
				StatusText.Text = $"无法删除列：{blocked.TypeName} 位于第 {blocked.Column + 1} 列或跨越目标区域。请先移动/缩小控件。";
				return;
			}
		}

		CommitHistory();
		_document.Rows = rows;
		_document.Columns = cols;
		_document.Normalize();
		_document.IsDirty = true;
		RenderCanvas();
		UpdateUi();
		_ = AutoWriteAsync();
	}

	private void Undo_Click(object sender, RoutedEventArgs e) {
		if (_document is null || _undo.Count == 0) return;
		_redo.Push(_document.Clone());
		_document.CopyFrom(_undo.Pop());
		_document.IsDirty = true;
		RestoreSelection();
		RenderCanvas();
		UpdateUi();
	}

	private void Redo_Click(object sender, RoutedEventArgs e) {
		if (_document is null || _redo.Count == 0) return;
		_undo.Push(_document.Clone());
		_document.CopyFrom(_redo.Pop());
		_document.IsDirty = true;
		RestoreSelection();
		RenderCanvas();
		UpdateUi();
	}

	private void CommitHistory() {
		if (_document is null) return;
		_undo.Push(_document.Clone());
		_redo.Clear();
		if (_undo.Count > 100) _undo.Pop();
	}

	private void RestoreSelection() {
		if (_selected is null || _document is null) return;
		_selected = _document.Nodes.FirstOrDefault(n => n.Id == _selected.Id);
	}

	private async Task AutoWriteAsync() {
		if (_document is null || AutoWriteToggle.IsChecked != true) {
			UpdateUi();
			return;
		}
		try {
			await new WriteBackService(_xaml, _csharp).WriteAsync(_document, _savedSnapshot);
			_savedSnapshot = _document.Clone();
			UpdateUi();
		} catch (Exception ex) {
			await ShowErrorAsync($"自动保存失败（变更仍保留在内存）：{ex.Message}");
		}
	}

	private void Select(ControlNode node) {
		_selected = node;
		_windowSelected = false;
		RefreshProperties();
		RenderCanvas();
		UpdateUi();
	}

	private void SelectWindow() {
		_selected = null;
		_windowSelected = true;
		RefreshProperties();
		RenderCanvas();
		UpdateUi();
	}

	private void UpdateDesignZoom() {
		DesignPreviewViewbox.Width = DesignPreviewBaseWidth * _designZoom;
		DesignPreviewViewbox.Height = DesignPreviewBaseHeight * _designZoom;
	}

	private void RenderCanvas() {
		DesignCanvas.Children.Clear();
		if (_document is null) return;

		UpdateDesignZoom();

		var cellWidth = DesignCanvas.Width / _document.Columns;
		var cellHeight = DesignCanvas.Height / _document.Rows;
		var gridBrush = ThemeBrush("DesignerCanvasGridBrush");
		var accentBrush = ThemeBrush("DesignerAccentBrush");

		for (var c = 1; c < _document.Columns; c++) AddLine(c * cellWidth, 0, 1, DesignCanvas.Height, gridBrush);
		for (var r = 1; r < _document.Rows; r++) AddLine(0, r * cellHeight, DesignCanvas.Width, 1, gridBrush);

		if (_hasHoverCell && (_pendingTool is not null || _dragging)) {
			var spanRows = _dragging && _selected is not null ? _selected.RowSpan : 1;
			var spanCols = _dragging && _selected is not null ? _selected.ColumnSpan : 1;
			var target = new Border {
				Width = Math.Max(8, cellWidth * spanCols - 6),
				Height = Math.Max(8, cellHeight * spanRows - 6),
				CornerRadius = new CornerRadius(8),
				BorderThickness = new Thickness(2),
				BorderBrush = accentBrush,
				Background = new SolidColorBrush(PreviewColor(35, 0, 120, 215)),
				IsHitTestVisible = false
			};
			Canvas.SetLeft(target, _hoverColumn * cellWidth + 3);
			Canvas.SetTop(target, _hoverRow * cellHeight + 3);
			DesignCanvas.Children.Add(target);
			AddLine((_hoverColumn + 0.5) * cellWidth, 0, 1, DesignCanvas.Height, accentBrush, 0.18);
			AddLine(0, (_hoverRow + 0.5) * cellHeight, DesignCanvas.Width, 1, accentBrush, 0.18);
		}

		foreach (var node in _document.Nodes) {
			var maxWidth = Math.Max(60, cellWidth * node.ColumnSpan - 10);
			var maxHeight = Math.Max(44, cellHeight * node.RowSpan - 10);
			var width = Math.Min(maxWidth, ParseSize(node.Width, Math.Min(maxWidth, Math.Max(150, cellWidth * 0.62))));
			var height = Math.Min(maxHeight, ParseSize(node.Height, Math.Min(maxHeight, 62)));
			var selected = ReferenceEquals(node, _selected);

			var host = new Border {
				Width = width,
				Height = height,
				CornerRadius = new CornerRadius(7),
				BorderThickness = new Thickness(selected ? 2 : 1),
				BorderBrush = selected ? ThemeBrush("DesignerControlSelectedBorderBrush") : ThemeBrush("DesignerControlBorderBrush"),
				Background = selected ? ThemeBrush("DesignerControlSelectedBrush") : ThemeBrush("DesignerControlBackgroundBrush"),
				Padding = new Thickness(8),
				Tag = node,
				IsHitTestVisible = true,
				Child = BuildPreview(node, width - 16, height - 16)
			};
			ToolTipService.SetToolTip(host, $"{node.TypeName}" + (string.IsNullOrWhiteSpace(node.XName) ? "" : $" - {node.XName}"));
			host.PointerPressed += Node_PointerPressed;
			Canvas.SetLeft(host, node.Column * cellWidth + 5);
			Canvas.SetTop(host, node.Row * cellHeight + 5);
			DesignCanvas.Children.Add(host);
		}
	}

	private FrameworkElement BuildPreview(ControlNode node, double width, double height) {
		var type = node.TypeName;
		var label = string.IsNullOrWhiteSpace(node.Content) ? type : node.Content!;
		var surface = ThemeBrush("DesignerPreviewSurfaceBrush");
		var accentBrush = ThemeBrush("DesignerAccentBrush");
		var foreground = new SolidColorBrush(PreviewColor(225, 255, 255, 255));

		if (type == "Button" || type == "HyperlinkButton" || type == "ToggleButton" || type == "RepeatButton") {
			return new Border {
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Center,
				CornerRadius = new CornerRadius(5),
				Height = Math.Min(34, height),
				Background = surface,
				BorderBrush = ThemeBrush("DesignerControlBorderBrush"),
				BorderThickness = new Thickness(1),
				Child = CenterText(label)
			};
		}

		if (type == "TextBox" || type == "PasswordBox" || type == "AutoSuggestBox" || type == "NumberBox" || type == "RichEditBox") {
			var panel = new Grid();
			panel.Children.Add(new Border {
				Background = new SolidColorBrush(PreviewColor(45, 128, 128, 128)),
				BorderBrush = ThemeBrush("DesignerControlBorderBrush"),
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(4)
			});
			panel.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(node.Content) ? type : node.Content, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(9, 0, 9, 0), Opacity = 0.78 });
			return panel;
		}

		if (type == "Image") {
			var panel = new Grid();
			panel.Children.Add(new TextBlock { Text = "▧", FontSize = Math.Min(28, height), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.72 });
			var source = node.Properties.TryGetValue("Source", out var sourceValue) ? sourceValue : null;
			panel.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(source) ? "Image" : source, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, FontSize = 11, Opacity = 0.65, TextTrimming = TextTrimming.CharacterEllipsis });
			return panel;
		}

		if (type == "CheckBox" || type == "RadioButton") {
			var grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
			grid.ColumnDefinitions.Add(new ColumnDefinition());
			var mark = new Border { Width = 16, Height = 16, CornerRadius = new CornerRadius(type == "RadioButton" ? 9 : 3), BorderThickness = new Thickness(1.5), BorderBrush = ThemeBrush("DesignerControlBorderBrush"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
			grid.Children.Add(mark);
			var text = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(4, 0, 0, 0) };
			Grid.SetColumn(text, 1); grid.Children.Add(text); return grid;
		}

		if (type == "ToggleSwitch") {
			var grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
			grid.ColumnDefinitions.Add(new ColumnDefinition());
			var pill = new Border { Width = 34, Height = 18, CornerRadius = new CornerRadius(9), Background = new SolidColorBrush(PreviewColor(90, 150, 150, 150)), BorderBrush = ThemeBrush("DesignerControlBorderBrush"), BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
			pill.Child = new Border { Width = 14, Height = 14, CornerRadius = new CornerRadius(7), Background = foreground, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2) };
			grid.Children.Add(pill);
			var onContent = node.Properties.TryGetValue("OnContent", out var onContentValue) ? onContentValue : null;
			var text = new TextBlock { Text = string.IsNullOrWhiteSpace(onContent) ? "ToggleSwitch" : onContent, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.8, TextTrimming = TextTrimming.CharacterEllipsis };
			Grid.SetColumn(text, 1); grid.Children.Add(text); return grid;
		}

		if (type == "Slider" || type == "ProgressBar") {
			var stack = new StackPanel { Spacing = 5, VerticalAlignment = VerticalAlignment.Center };
			stack.Children.Add(new TextBlock { Text = type, FontSize = 11, Opacity = 0.65 });
			var track = new Grid { Height = 8 };
			track.Children.Add(new Border { Height = 4, VerticalAlignment = VerticalAlignment.Center, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush(PreviewColor(55, 140, 140, 140)) });
			track.Children.Add(new Border { Width = type == "ProgressBar" ? width * 0.58 : 4, Height = 4, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, CornerRadius = new CornerRadius(2), Background = accentBrush });
			if (type == "Slider") track.Children.Add(new Border { Width = 14, Height = 14, CornerRadius = new CornerRadius(7), Background = accentBrush, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(Math.Max(0, width * 0.52), 0, 0, 0) });
			stack.Children.Add(track); return stack;
		}

		if (type == "ProgressRing" || type == "RatingControl")
			return new TextBlock { Text = type == "ProgressRing" ? "◌" : "★★★★★", FontSize = Math.Min(24, height), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.78 };

		if (type == "ComboBox" || type == "DatePicker" || type == "CalendarDatePicker" || type == "TimePicker")
			return new Border { Height = Math.Min(34, height), VerticalAlignment = VerticalAlignment.Center, CornerRadius = new CornerRadius(4), Background = surface, BorderBrush = ThemeBrush("DesignerControlBorderBrush"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = label + "   ▾", Margin = new Thickness(9, 0, 9, 0), VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis } };

		if (type == "ListView" || type == "ListBox" || type == "TreeView" || type == "ItemsRepeater" || type == "NavigationView" || type == "Pivot" || type == "TabView") {
			var stack = new StackPanel { Spacing = 3, Margin = new Thickness(3) };
			for (var i = 0; i < 3; i++) stack.Children.Add(new Border { Height = Math.Max(12, (height - 12) / 3), CornerRadius = new CornerRadius(3), Background = new SolidColorBrush(PreviewColor((byte)(i == 0 ? 50 : 28), 140, 140, 140)), Child = new TextBlock { Text = i == 0 ? label : $"{type} item", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(7, 0, 7, 0), TextTrimming = TextTrimming.CharacterEllipsis } });
			return stack;
		}

		return new Grid {
			Children = { new TextBlock { Text = label, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis } }
		};
	}

	private static TextBlock CenterText(string text) => new() { Text = text, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, FontSize = 13 };

	private static bool IsDesignableControl(string typeName) => XamlDocumentService.DesignableControlTypes.Contains(typeName);

	private static Windows.UI.Color PreviewColor(byte a, byte r, byte g, byte b) => Windows.UI.Color.FromArgb(a, r, g, b);

	private static Brush ThemeBrush(string key) => Application.Current.Resources[key] as Brush ?? new SolidColorBrush(PreviewColor(255, 128, 128, 128));

	private void AddLine(double left, double top, double width, double height, Brush brush, double opacity = 0.35) {
		var line = new Rectangle { Width = width, Height = height, Fill = brush, Opacity = opacity, IsHitTestVisible = false };
		Canvas.SetLeft(line, left); Canvas.SetTop(line, top); DesignCanvas.Children.Add(line);
	}

	private void Node_PointerPressed(object sender, PointerRoutedEventArgs e) {
		if (sender is Border border && border.Tag is ControlNode node) {
			Select(node);
			_dragOriginalRow = node.Row;
			_dragOriginalColumn = node.Column;
			_dragOriginalRowSpan = node.RowSpan;
			_dragOriginalColumnSpan = node.ColumnSpan;
			CommitHistory();
			_dragging = true;
			DesignCanvas.CapturePointer(e.Pointer);
			e.Handled = true;
		}
	}

	private ControlNode? FindNodeAt(Point point) {
		if (_document is null) return null;
		var (row, col) = PointToCell(point);
		return _document.Nodes.LastOrDefault(n => row >= n.Row && row < n.Row + n.RowSpan && col >= n.Column && col < n.Column + n.ColumnSpan);
	}

	private (int row, int col) PointToCell(Point point) {
		if (_document is null) return (0, 0);
		var cellHeight = DesignCanvas.Height / _document.Rows;
		var cellWidth = DesignCanvas.Width / _document.Columns;
		return (
			Math.Clamp((int)(point.Y / cellHeight), 0, _document.Rows - 1),
			Math.Clamp((int)(point.X / cellWidth), 0, _document.Columns - 1));
	}

	private void RefreshProperties() {
		_ignorePropertyChanges = true;
		try {
			var n = _selected;
			var isControl = n is not null;
			var isWindow = _document is not null && _windowSelected && n is null;

			SelectedTypeText.Text = isControl
				? $"{n!.TypeName}（控件）"
				: isWindow
					? $"{_document!.RootTypeName}（根容器）"
					: "未选择对象";

			// Root/window editors.
			WindowTitleBox.Text = RootValue("Title");
			WindowWidthBox.Text = RootValue("Width");
			WindowHeightBox.Text = RootValue("Height");
			WindowMinWidthBox.Text = RootValue("MinWidth");
			WindowMinHeightBox.Text = RootValue("MinHeight");
			WindowMaxWidthBox.Text = RootValue("MaxWidth");
			WindowMaxHeightBox.Text = RootValue("MaxHeight");
			SelectCombo(WindowThemeBox, RootValue("RequestedTheme"));
			SelectCombo(WindowExtendTitleBarBox, RootValue("ExtendsContentIntoTitleBar"));

			foreach (var control in WindowPropertyControls()) control.IsEnabled = isWindow;
			if (_document is not null) {
				WindowTitleBox.IsEnabled = isWindow && string.Equals(_document.RootTypeName, "Window", StringComparison.Ordinal);
				WindowWidthBox.IsEnabled = isWindow && string.Equals(_document.RootTypeName, "Window", StringComparison.Ordinal);
				WindowHeightBox.IsEnabled = isWindow && string.Equals(_document.RootTypeName, "Window", StringComparison.Ordinal);
				WindowMinWidthBox.IsEnabled = isWindow && string.Equals(_document.RootTypeName, "Window", StringComparison.Ordinal);
				WindowMinHeightBox.IsEnabled = isWindow && string.Equals(_document.RootTypeName, "Window", StringComparison.Ordinal);
				WindowMaxWidthBox.IsEnabled = isWindow && string.Equals(_document.RootTypeName, "Window", StringComparison.Ordinal);
				WindowMaxHeightBox.IsEnabled = isWindow && string.Equals(_document.RootTypeName, "Window", StringComparison.Ordinal);
				WindowExtendTitleBarBox.IsEnabled = isWindow;
			}

			// Core control fields.
			NameBox.Text = n?.XName ?? "";
			ContentBox.Text = n?.Content ?? "";
			RowBox.Value = n?.Row ?? 0;
			ColumnBox.Value = n?.Column ?? 0;
			RowSpanBox.Value = n?.RowSpan ?? 1;
			ColumnSpanBox.Value = n?.ColumnSpan ?? 1;
			WidthBox.Text = n?.Width ?? "";
			HeightBox.Text = n?.Height ?? "";
			SelectCombo(HorizontalAlignmentBox, n?.HorizontalAlignment);
			SelectCombo(VerticalAlignmentBox, n?.VerticalAlignment);

			SetText(PropertyBox("MinWidth"), PropertyValue(n, "MinWidth"));
			SetText(PropertyBox("MaxWidth"), PropertyValue(n, "MaxWidth"));
			SetText(PropertyBox("MinHeight"), PropertyValue(n, "MinHeight"));
			SetText(PropertyBox("MaxHeight"), PropertyValue(n, "MaxHeight"));
			SetText(PropertyBox("Margin"), PropertyValue(n, "Margin"));
			SetText(PropertyBox("Padding"), PropertyValue(n, "Padding"));
			SetText(PropertyBox("FontSize"), PropertyValue(n, "FontSize"));
			SelectCombo(FontWeightBox, PropertyValue(n, "FontWeight"));
			SetText(PropertyBox("CharacterSpacing"), PropertyValue(n, "CharacterSpacing"));
			SetText(PropertyBox("Foreground"), PropertyValue(n, "Foreground"));
			SetText(PropertyBox("Background"), PropertyValue(n, "Background"));
			SetText(PropertyBox("BorderBrush"), PropertyValue(n, "BorderBrush"));
			SetText(PropertyBox("BorderThickness"), PropertyValue(n, "BorderThickness"));
			SetText(PropertyBox("CornerRadius"), PropertyValue(n, "CornerRadius"));
			SetText(PropertyBox("Opacity"), PropertyValue(n, "Opacity"));
			SelectCombo(VisibilityBox, PropertyValue(n, "Visibility"));
			SelectCombo(IsEnabledBox, PropertyValue(n, "IsEnabled"));
			SelectCombo(IsTabStopBox, PropertyValue(n, "IsTabStop"));
			SelectCombo(IsHitTestVisibleBox, PropertyValue(n, "IsHitTestVisible"));
			SelectCombo(HorizontalContentAlignmentBox, PropertyValue(n, "HorizontalContentAlignment"));
			SelectCombo(VerticalContentAlignmentBox, PropertyValue(n, "VerticalContentAlignment"));
			SetText(ToolTipBox, PropertyValue(n, "ToolTip"));
			SetText(PropertyBox("PlaceholderText"), PropertyValue(n, "PlaceholderText"));
			SetText(PropertyBox("Header"), PropertyValue(n, "Header"));
			SetText(SourceBox, PropertyValue(n, "Source"));
			SelectCombo(StretchBox, PropertyValue(n, "Stretch"));
			SelectCombo(TextWrappingBox, PropertyValue(n, "TextWrapping"));
			SetText(PropertyBox("MaxLength"), PropertyValue(n, "MaxLength"));
			SetText(PropertyBox("SelectedIndex"), PropertyValue(n, "SelectedIndex"));
			SetText(PropertyBox("Minimum"), PropertyValue(n, "Minimum"));
			SetText(PropertyBox("Maximum"), PropertyValue(n, "Maximum"));
			SetText(PropertyBox("Value"), PropertyValue(n, "Value"));
			SelectCombo(OrientationBox, PropertyValue(n, "Orientation"));
			SelectCombo(IsCheckedBox, PropertyValue(n, "IsChecked"));
			SelectCombo(IsOnBox, PropertyValue(n, "IsOn"));
			SetText(OnContentBox, PropertyValue(n, "OnContent"));
			SetText(OffContentBox, PropertyValue(n, "OffContent"));
			SetText(GroupNameBox, PropertyValue(n, "GroupName"));
			SelectCombo(IsEditableBox, PropertyValue(n, "IsEditable"));
			SelectCombo(AcceptsReturnBox, PropertyValue(n, "AcceptsReturn"));
			SelectCombo(IsReadOnlyBox, PropertyValue(n, "IsReadOnly"));
			SetText(FontFamilyBox, PropertyValue(n, "FontFamily"));
			SelectCombo(FontStyleBox, PropertyValue(n, "FontStyle"));
			SelectCombo(TextAlignmentBox, PropertyValue(n, "TextAlignment"));
			SelectCombo(FlowDirectionBox, PropertyValue(n, "FlowDirection"));
			SetText(TabIndexBox, PropertyValue(n, "TabIndex"));
			SelectCombo(UseSystemFocusVisualsBox, PropertyValue(n, "UseSystemFocusVisuals"));
			SetText(CommandBox, PropertyValue(n, "Command"));
			SetText(CommandParameterBox, PropertyValue(n, "CommandParameter"));
			SelectCombo(IsDefaultBox, PropertyValue(n, "IsDefault"));
			SelectCombo(IsCancelBox, PropertyValue(n, "IsCancel"));
			SelectCombo(ClickModeBox, PropertyValue(n, "ClickMode"));
			SelectCombo(IsIndeterminateBox, PropertyValue(n, "IsIndeterminate"));
			SetText(DateFormatBox, PropertyValue(n, "DateFormat"));
			SetText(NumberFormatBox, PropertyValue(n, "NumberFormat"));
			SetText(LanguageBox, PropertyValue(n, "Language"));
			SetText(TagBox, PropertyValue(n, "Tag"));

			foreach (var editor in ControlPropertyEditors()) editor.IsEnabled = false;
			if (n is not null) {
				foreach (var pair in ControlPropertyEditorMap())
					pair.Value.IsEnabled = n.IsPropertyEnabled(pair.Key);
				NameBox.IsEnabled = true;
				ContentBox.IsEnabled = SupportsContentText(n);
				RowBox.IsEnabled = true;
				ColumnBox.IsEnabled = true;
				RowSpanBox.IsEnabled = true;
				ColumnSpanBox.IsEnabled = true;
				DeleteButton.IsEnabled = true;
			} else {
				NameBox.IsEnabled = false;
				ContentBox.IsEnabled = false;
				RowBox.IsEnabled = false;
				ColumnBox.IsEnabled = false;
				RowSpanBox.IsEnabled = false;
				ColumnSpanBox.IsEnabled = false;
				DeleteButton.IsEnabled = false;
			}

			ContentBox.AcceptsReturn = n is not null &&
				(n.TypeName == "TextBox" || n.TypeName == "RichEditBox");

			PreviewWindowTitleText.Text = GetPreviewWindowTitle();

			EventText.Text = n?.EventName is null
				? (n is null ? "选择控件后可查看事件" : "未生成事件")
				: $"{n.EventKind ?? n.DerivedEventKind} → {n.EventName}" + (n.EventWasAutoGenerated ? "（自动）" : "（用户手写）");
		} finally {
			_ignorePropertyChanges = false;
		}
	}

	private static bool SupportsContentText(ControlNode n) {
		return n.TypeName == "Button" || n.TypeName == "HyperlinkButton" || n.TypeName == "ToggleButton" ||
			   n.TypeName == "RepeatButton" || n.TypeName == "CheckBox" || n.TypeName == "RadioButton" ||
			   n.TypeName == "Expander" || n.TypeName == "TextBlock" || n.TypeName == "TextBox" ||
			   n.TypeName == "PasswordBox" || n.TypeName == "AutoSuggestBox" || n.TypeName == "RichEditBox";
	}

	private TextBox PropertyBox(string key) {
		if (key == "MinWidth") return MinWidthBox;
		if (key == "MaxWidth") return MaxWidthBox;
		if (key == "MinHeight") return MinHeightBox;
		if (key == "MaxHeight") return MaxHeightBox;
		if (key == "Margin") return MarginBox;
		if (key == "Padding") return PaddingBox;
		if (key == "FontSize") return FontSizeBox;
		if (key == "CharacterSpacing") return CharacterSpacingBox;
		if (key == "Foreground") return ForegroundBox;
		if (key == "Background") return BackgroundBox;
		if (key == "BorderBrush") return BorderBrushBox;
		if (key == "BorderThickness") return BorderThicknessBox;
		if (key == "CornerRadius") return CornerRadiusBox;
		if (key == "Opacity") return OpacityBox;
		if (key == "ToolTip") return ToolTipBox;
		if (key == "PlaceholderText") return PlaceholderTextBox;
		if (key == "Header") return HeaderBox;
		if (key == "MaxLength") return MaxLengthBox;
		if (key == "SelectedIndex") return SelectedIndexBox;
		if (key == "Minimum") return MinimumBox;
		if (key == "Maximum") return MaximumBox;
		if (key == "Value") return ValueBox;
		throw new KeyNotFoundException(key);
	}

	private IReadOnlyDictionary<string, Control> ControlPropertyEditorMap() => new Dictionary<string, Control>(StringComparer.Ordinal) {
		["ToolTip"] = ToolTipBox,
		["PlaceholderText"] = PlaceholderTextBox,
		["Header"] = HeaderBox,
		["Source"] = SourceBox,
		["Stretch"] = StretchBox,
		["MinWidth"] = MinWidthBox, ["MaxWidth"] = MaxWidthBox,
		["MinHeight"] = MinHeightBox, ["MaxHeight"] = MaxHeightBox,
		["Margin"] = MarginBox, ["Padding"] = PaddingBox,
		["HorizontalContentAlignment"] = HorizontalContentAlignmentBox,
		["VerticalContentAlignment"] = VerticalContentAlignmentBox,
		["FontSize"] = FontSizeBox, ["FontWeight"] = FontWeightBox, ["CharacterSpacing"] = CharacterSpacingBox,
		["Foreground"] = ForegroundBox, ["Background"] = BackgroundBox,
		["BorderBrush"] = BorderBrushBox, ["BorderThickness"] = BorderThicknessBox, ["CornerRadius"] = CornerRadiusBox,
		["Opacity"] = OpacityBox, ["Visibility"] = VisibilityBox, ["IsEnabled"] = IsEnabledBox,
		["IsTabStop"] = IsTabStopBox, ["IsHitTestVisible"] = IsHitTestVisibleBox,
		["TextWrapping"] = TextWrappingBox, ["AcceptsReturn"] = AcceptsReturnBox, ["IsReadOnly"] = IsReadOnlyBox,
		["MaxLength"] = MaxLengthBox, ["SelectedIndex"] = SelectedIndexBox, ["Minimum"] = MinimumBox,
		["Maximum"] = MaximumBox, ["Value"] = ValueBox, ["Orientation"] = OrientationBox,
		["IsChecked"] = IsCheckedBox, ["IsOn"] = IsOnBox, ["OnContent"] = OnContentBox, ["OffContent"] = OffContentBox,
		["GroupName"] = GroupNameBox, ["IsEditable"] = IsEditableBox,
		["FontFamily"] = FontFamilyBox, ["FontStyle"] = FontStyleBox, ["TextAlignment"] = TextAlignmentBox,
		["FlowDirection"] = FlowDirectionBox, ["TabIndex"] = TabIndexBox, ["UseSystemFocusVisuals"] = UseSystemFocusVisualsBox,
		["Command"] = CommandBox, ["CommandParameter"] = CommandParameterBox, ["IsDefault"] = IsDefaultBox,
		["IsCancel"] = IsCancelBox, ["ClickMode"] = ClickModeBox, ["IsIndeterminate"] = IsIndeterminateBox,
		["DateFormat"] = DateFormatBox, ["NumberFormat"] = NumberFormatBox, ["Language"] = LanguageBox, ["Tag"] = TagBox
	};

	private IEnumerable<Control> ControlPropertyEditors() => ControlPropertyEditorMap().Values;
	private IEnumerable<Control> WindowPropertyControls() => new Control[]
	{
		WindowTitleBox, WindowWidthBox, WindowHeightBox, WindowMinWidthBox, WindowMinHeightBox,
		WindowMaxWidthBox, WindowMaxHeightBox, WindowThemeBox, WindowExtendTitleBarBox
	};

	private static void SetText(TextBox? box, string? value) {
		if (box is not null) box.Text = value ?? "";
	}

	private static string? PropertyValue(ControlNode? node, string key) => node is not null && node.Properties.TryGetValue(key, out var value) ? value : null;
	private string? RootValue(string key) => _document is not null && _document.RootProperties.TryGetValue(key, out var value) ? value : null;

	private static void SelectCombo(ComboBox combo, string? value) {
		combo.SelectedIndex = 0;
		if (string.IsNullOrWhiteSpace(value)) return;
		for (var i = 0; i < combo.Items.Count; i++)
			if ((combo.Items[i] as ComboBoxItem)?.Content?.ToString() == value) {
				combo.SelectedIndex = i;
				break;
			}
	}

	private string GetPreviewWindowTitle() {
		var configured = RootValue("Title");
		if (!string.IsNullOrWhiteSpace(configured))
			return configured!;
		if (_document is not null) {
			var name = System.IO.Path.GetFileNameWithoutExtension(_document.FilePath);
			if (!string.IsNullOrWhiteSpace(name)) return name;
			if (!string.IsNullOrWhiteSpace(_document.RootTypeName)) return _document.RootTypeName;
		}
		return "Window";
	}

	private void UpdateUi() {
		StatusText.Text = _document is null ? "未打开文件" : $"{_document.FilePath}{(_document.IsDirty ? "  •  未保存" : "  •  已同步")}";
		GridStatusText.Text = _document is null
			? "Grid 1 × 1"
			: _document.GridSizingSupported
				? $"Grid {_document.Rows} × {_document.Columns}"
				: $"Grid {_document.Rows} × {_document.Columns} - Auto/固定值仅预览";
		UndoButton.IsEnabled = _undo.Count > 0;
		RedoButton.IsEnabled = _redo.Count > 0;
		var gridEditingEnabled = _document is not null && _document.GridSizingSupported;
		AddRowButton.IsEnabled = gridEditingEnabled;
		RemoveRowButton.IsEnabled = gridEditingEnabled && _document!.Rows > 1;
		AddColumnButton.IsEnabled = gridEditingEnabled;
		RemoveColumnButton.IsEnabled = gridEditingEnabled && _document!.Columns > 1;
		HintText.Text = _document is not null && !_document.GridSizingSupported
			? $"当前 Grid 含 Auto/固定尺寸，设计器保持原布局并仅提供预览/控件编辑（Ctrl+滚轮缩放 {_designZoom * 100:0}%）"
			: $"WinUI XAML Designer by MsintX（Ctrl+滚轮缩放 {_designZoom * 100:0}%）";
		RefreshProperties();
	}

	private string MakeUniqueName(string typeName) {
		var i = 1;
		while (_document?.Nodes.Any(n => n.XName == typeName + i) == true) i++;
		return typeName + i;
	}

	private static string DefaultContent(string typeName) {
		if (typeName == "Button") return "Button";
		if (typeName == "TextBlock") return "TextBlock";
		if (typeName == "CheckBox") return "CheckBox";
		if (typeName == "RadioButton") return "RadioButton";
		if (typeName == "ComboBox") return "ComboBox";
		return typeName;
	}

	private static double DefaultHeight(string typeName) => typeName == "TextBlock" ? 28 : 42;
	private static double ParseSize(string? value, double fallback) => double.TryParse(value, out var d) && d > 0 ? d : fallback;

	private async Task ShowErrorAsync(string message) {
		var dialog = CreateDialog(
			title: "发现错误",
			content: message,
			closeButtonText: "确定",
			defaultButton: ContentDialogButton.Close);
		await dialog.ShowAsync();
	}

	private ContentDialog CreateDialog(
		string title,
		object content,
		string? primaryButtonText = null,
		string? closeButtonText = null,
		ContentDialogButton defaultButton = ContentDialogButton.None) {
		var style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
		return new ContentDialog {
			XamlRoot = Content.XamlRoot,
			Style = style,
			Title = title,
			Content = content,
			PrimaryButtonText = primaryButtonText,
			CloseButtonText = closeButtonText,
			DefaultButton = defaultButton
		};
	}

}

