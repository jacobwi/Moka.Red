using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.SplitPane;

/// <summary>
///     A resizable two-panel layout (similar to VS Code's editor split).
///     Supports horizontal and vertical orientations with a draggable handle.
/// </summary>
public partial class MokaSplitPane : MokaComponentBase
{
	private double _currentSizePx;
	private bool _dragging;
	private double _startPointer;
	private double _startSize;

	/// <summary>Content rendered in the first (left or top) pane.</summary>
	[Parameter]
	public RenderFragment? FirstContent { get; set; }

	/// <summary>Content rendered in the second (right or bottom) pane.</summary>
	[Parameter]
	public RenderFragment? SecondContent { get; set; }

	/// <summary>Split orientation. Defaults to <see cref="MokaSplitOrientation.Horizontal" /> (side by side).</summary>
	[Parameter]
	public MokaSplitOrientation Orientation { get; set; } = MokaSplitOrientation.Horizontal;

	/// <summary>Initial size of the first pane. Accepts any CSS length (e.g., "50%", "300px"). Defaults to "50%".</summary>
	[Parameter]
	public string InitialSize { get; set; } = "50%";

	/// <summary>Minimum size for each pane. Accepts any CSS length. Defaults to "100px".</summary>
	[Parameter]
	public string MinSize { get; set; } = "100px";

	/// <summary>Whether the second pane is collapsed. Defaults to false.</summary>
	[Parameter]
	public bool Collapsed { get; set; }

	/// <summary>Whether to show the drag handle between panes. Defaults to true.</summary>
	[Parameter]
	public bool ShowHandle { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-split-pane";

	private bool IsHorizontal => Orientation == MokaSplitOrientation.Horizontal;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-split-pane--horizontal", IsHorizontal)
		.AddClass("moka-split-pane--vertical", !IsHorizontal)
		.AddClass("moka-split-pane--collapsed", Collapsed)
		.AddClass("moka-split-pane--dragging", _dragging)
		.AddClass("moka-split-pane--no-handle", !ShowHandle)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-split-size", _currentSizePx > 0 ? $"{_currentSizePx}px" : InitialSize)
		.AddStyle("--moka-split-min", MinSize)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	private void HandlePointerDown(PointerEventArgs e)
	{
		_dragging = true;
		_startPointer = IsHorizontal ? e.ClientX : e.ClientY;
		_startSize = _currentSizePx;
	}

	private async Task HandlePointerMove(PointerEventArgs e)
	{
		if (!_dragging)
		{
			return;
		}

		double current = IsHorizontal ? e.ClientX : e.ClientY;
		double delta = current - _startPointer;
		double newSize = _startSize + delta;

		if (newSize > 0)
		{
			_currentSizePx = newSize;
		}

		await Task.CompletedTask;
	}

	private void HandlePointerUp(PointerEventArgs e) => _dragging = false;

	/// <summary>
	///     Captures the initial pixel size of the first pane on first render
	///     so that drag deltas can be applied correctly.
	/// </summary>
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && _currentSizePx <= 0)
		{
			string elementId = Id ?? "split";
			string js =
				$"(function(){{ var el = document.getElementById('{elementId}'); if(!el) return 0; var f = el.querySelector('.moka-split-pane-first'); return f ? f.getBoundingClientRect().width : 0; }})()";
			double size = await SafeJsInvokeAsync<double>("eval", js);

			// If JS interop failed or returned 0, we rely on CSS initial size
			if (size > 0)
			{
				_currentSizePx = size;
				StateHasChanged();
			}
		}

		await base.OnAfterRenderAsync(firstRender);
	}
}
