using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.ImageCompare;

/// <summary>
///     Two images over each other with a draggable divider between them: before on one side,
///     after on the other, for screenshots and photos that changed. The divider is also a slider
///     the keyboard can move.
/// </summary>
public partial class MokaImageCompare : MokaVisualComponentBase
{
	private const string JsModulePath = "./_content/Moka.Red.Primitives/ImageCompare/MokaImageCompare.razor.js";

	private DotNetObjectReference<MokaImageCompare>? _dotNetRef;
	private ElementReference _handle;
	private IJSObjectReference? _module;
	private double _position = 50;
	private double _lastPositionParameter = 50;
	private ElementReference _root;
	private bool _compareDisposed;

	/// <summary>The image the divider reveals less of as it moves: the left (or top) one.</summary>
	[Parameter]
	public string? BeforeSrc { get; set; }

	/// <summary>The image the divider reveals more of as it moves: the right (or bottom) one.</summary>
	[Parameter]
	public string? AfterSrc { get; set; }

	/// <summary>Name of the corner label on the before image. Defaults to "Before".</summary>
	[Parameter]
	public string? BeforeLabel { get; set; }

	/// <summary>Name of the corner label on the after image. Defaults to "After".</summary>
	[Parameter]
	public string? AfterLabel { get; set; }

	/// <summary>Text the images stand for, read by assistive technology.</summary>
	[Parameter]
	public string? Alt { get; set; }

	/// <summary>
	///     Where the divider sits, as a percentage of the image, 0 to 100. Two-way bindable. A drag
	///     reports its result once, when it ends.
	/// </summary>
	[Parameter]
	public double Position { get; set; } = 50;

	/// <summary>Callback invoked when the divider moves to a new position.</summary>
	[Parameter]
	public EventCallback<double> PositionChanged { get; set; }

	/// <summary>Which way the divider runs. Defaults to top to bottom.</summary>
	[Parameter]
	public MokaImageCompareOrientation Orientation { get; set; } = MokaImageCompareOrientation.Horizontal;

	/// <inheritdoc />
	protected override string RootClass => "moka-image-compare";

	private bool IsHorizontal => Orientation == MokaImageCompareOrientation.Horizontal;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-image-compare--{SizeToKebab(Size)}")
		.AddClass(IsHorizontal ? "moka-image-compare--horizontal" : "moka-image-compare--vertical")
		.AddClass(Class)
		.Build();

	// The custom property is what the clip path and the handle read; the drag writes it directly,
	// and a render writes whatever .NET last knew, which is the same once the drag has ended.
	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-compare-pos", $"{_position.ToString("F2", CultureInfo.InvariantCulture)}%")
		.AddStyle(SpacingStyle().AddStyle(Style).Build())
		.Build();

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// The parent's value replaces what the last drag reported; a parent render repeating the
		// old one must not move a divider the user just dragged.
		if (Math.Abs(Position - _lastPositionParameter) > 0.001)
		{
			_lastPositionParameter = Position;
			_position = Math.Clamp(Position, 0, 100);
		}
	}

	// The divider's position changes with the keyboard and drags, independently of parameters.
	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			try
			{
				_module = await GetJsModuleAsync(JsModulePath);
				if (_module is not null && !_compareDisposed)
				{
					_dotNetRef ??= DotNetObjectReference.Create(this);
					await _module.InvokeVoidAsync("enableDrag", _root, _handle, IsHorizontal, _dotNetRef);
				}
			}
			catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
			{
			}
		}
	}

	/// <summary>Called from JS when a divider drag ends, with where it landed.</summary>
	/// <param name="position">The divider's new position, as a percentage of the image.</param>
	[JSInvokable]
	public async Task OnDragged(double position)
	{
		_position = Math.Clamp(position, 0, 100);
		_lastPositionParameter = _position;
		if (PositionChanged.HasDelegate)
		{
			await PositionChanged.InvokeAsync(_position);
		}

		// An interop callback has no render of its own to follow, and the style carries the position.
		StateHasChanged();
	}

	// The keyboard walks the same slider the drag does: a step at a time, Home and End to the ends.
	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		double step = e.ShiftKey ? 10 : 1;
		switch (e.Key)
		{
			case "ArrowLeft" when IsHorizontal:
			case "ArrowUp" when !IsHorizontal:
				await MoveTo(_position - step);
				break;
			case "ArrowRight" when IsHorizontal:
			case "ArrowDown" when !IsHorizontal:
				await MoveTo(_position + step);
				break;
			case "Home":
				await MoveTo(0);
				break;
			case "End":
				await MoveTo(100);
				break;
		}
	}

	private async Task MoveTo(double value)
	{
		_position = Math.Clamp(Math.Round(value, 2), 0, 100);
		_lastPositionParameter = _position;
		if (PositionChanged.HasDelegate)
		{
			await PositionChanged.InvokeAsync(_position);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		_compareDisposed = true;

		await _module.SafeInvokeVoidAsync("disableDrag", _root);
		_module = null;

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}
}
