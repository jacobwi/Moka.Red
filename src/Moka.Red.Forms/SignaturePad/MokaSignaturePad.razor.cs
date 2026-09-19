using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.SignaturePad;

/// <summary>
///     Drawing canvas for capturing signatures. Uses JS interop for canvas drawing operations.
///     Supports undo, clear, and exports the signature as a base64 data URL. A <see cref="Value" />
///     from the parent is drawn on the canvas, and an empty one clears it.
/// </summary>
public partial class MokaSignaturePad : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Forms/SignaturePad/MokaSignaturePad.razor.js";

	private ElementReference _canvasRef;
	private DotNetObjectReference<MokaSignaturePad>? _dotNetRef;
	private bool _drawPending;
	private int _drawRequest;
	private bool _initialized;
	private string? _lastValue;

	// The value from the parent the canvas shows under the user's strokes. Undoing the last of them
	// reports no drawing, but the canvas shows this value again.
	private string? _shownValue;
	private string? _value;

	/// <summary>
	///     The signature as a base64 data URL. Two-way bindable. A value the parent passes is drawn on
	///     the canvas, scaled to fit, and a null or empty one clears it.
	/// </summary>
	[Parameter]
	public string? Value { get; set; }

	/// <summary>Callback when the signature value changes.</summary>
	[Parameter]
	public EventCallback<string?> ValueChanged { get; set; }

	/// <summary>Label text displayed above the pad.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the pad.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>Width of the signature pad. Default "100%".</summary>
	[Parameter]
	public string Width { get; set; } = "100%";

	/// <summary>Height of the signature pad. Default "200px".</summary>
	[Parameter]
	public string Height { get; set; } = "200px";

	/// <summary>Stroke color for drawing. Default "#000000".</summary>
	[Parameter]
	public string StrokeColor { get; set; } = "#000000";

	/// <summary>Stroke width in pixels. Default 2.</summary>
	[Parameter]
	public int StrokeWidth { get; set; } = 2;

	/// <summary>Background color of the canvas. Default "#ffffff".</summary>
	[Parameter]
	public string BackgroundColor { get; set; } = "#ffffff";

	/// <summary>Whether the pad is read-only.</summary>
	[Parameter]
	public bool ReadOnly { get; set; }

	/// <summary>Whether to show the clear button. Default true.</summary>
	[Parameter]
	public bool ShowClearButton { get; set; } = true;

	/// <summary>Whether to show the undo button. Default true.</summary>
	[Parameter]
	public bool ShowUndoButton { get; set; } = true;

	/// <summary>Placeholder text shown when the pad is empty. Default "Sign here".</summary>
	[Parameter]
	public string Placeholder { get; set; } = "Sign here";

	/// <inheritdoc />
	protected override string RootClass => "moka-signature-pad";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-signature-pad--disabled", Disabled)
		.AddClass("moka-signature-pad--readonly", ReadOnly)
		.AddClass(Class)
		.Build();

	// The root is the outermost element, so it takes the margin. The canvas frame draws the pad's
	// border, so it takes the padding and the radius.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("width", Width)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle(Style)
		.Build();

	private string? FrameStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	// Height was documented but never applied, so the canvas took the 3:1 shape of its drawing
	// buffer whatever the parameter said.
	private string? CanvasStyle => new StyleBuilder()
		.AddStyle("height", Height)
		.AddStyle("background-color", BackgroundColor)
		.Build();

	/// <summary>Override ShouldRender to always return true for canvas state updates.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Value only seeds the state when the parent passes a new value. Copying it on every parent
		// render put the "Sign here" placeholder back over an unbound pad's signature.
		if (!string.Equals(_lastValue, Value, StringComparison.Ordinal))
		{
			_lastValue = Value;

			// A bound parent passes back the value the pad just reported. That drawing is already on
			// the canvas, and drawing it again would throw away the strokes undo works through.
			if (!string.Equals(_value, Value, StringComparison.Ordinal))
			{
				// null and "" both mean an empty pad, so one following the other clears nothing.
				if (!string.IsNullOrEmpty(_value) || !string.IsNullOrEmpty(Value))
				{
					_drawPending = true;
				}

				_value = Value;
			}
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// The drawing buffer takes the displayed shape once, before anything is painted on it.
		if (firstRender)
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "fitCanvas", _canvasRef);
		}

		// Drawing is attached the first time the pad is editable: a pad that started read-only or
		// disabled could never be drawn on. Attaching paints the background, so a signature already
		// on the canvas is drawn again after it.
		if (!_initialized && !ReadOnly && !Disabled)
		{
			await InitializeCanvasAsync();
			if (!firstRender && !string.IsNullOrEmpty(_value))
			{
				_drawPending = true;
			}
		}

		if (_drawPending)
		{
			_drawPending = false;
			int request = ++_drawRequest;
			string? value = _value;
			bool shown = await SafeModuleInvokeAsync<bool>(ModulePath, "showSignature", _canvasRef, value,
				BackgroundColor);

			// A newer value may have been sent while the image loaded. Only the latest draw counts.
			if (request == _drawRequest)
			{
				_shownValue = shown ? value : null;
			}
		}
	}

	private async Task InitializeCanvasAsync()
	{
		if (_initialized)
		{
			return;
		}

		_dotNetRef = DotNetObjectReference.Create(this);

		// A dictionary rather than an anonymous type, which trimming can strip in WebAssembly apps.
		await SafeModuleInvokeVoidAsync(ModulePath, "initCanvasDraw", _dotNetRef, _canvasRef,
			new Dictionary<string, object?>
			{
				["strokeColor"] = StrokeColor,
				["strokeWidth"] = StrokeWidth,
				["backgroundColor"] = BackgroundColor
			});
		_initialized = true;
	}

	/// <summary>Called from JavaScript when the signature changes.</summary>
	[JSInvokable]
	public async Task OnSignatureChanged(string? signatureData)
	{
		_value = signatureData ?? _shownValue;

		// A call from JS renders nothing by itself, and without a bound parent nothing else would
		// hide the placeholder.
		StateHasChanged();

		if (ValueChanged.HasDelegate)
		{
			await ValueChanged.InvokeAsync(_value);
		}
	}

	private async Task HandleClear()
	{
		if (!_initialized)
		{
			return;
		}

		// A value still loading is not drawn over the cleared pad, in JS or here.
		_drawRequest++;
		await SafeModuleInvokeVoidAsync(ModulePath, "clearSignature", _canvasRef, BackgroundColor);
		_value = null;
		_shownValue = null;
		await ValueChanged.InvokeAsync(null);
	}

	private async Task HandleUndo()
	{
		if (!_initialized)
		{
			return;
		}

		await SafeModuleInvokeVoidAsync(ModulePath, "undoSignature", _canvasRef, StrokeColor, StrokeWidth,
			BackgroundColor);
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_initialized)
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "removeCanvasDraw", _canvasRef);
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}
}
