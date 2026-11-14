using System.Text;
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

	// A generous ceiling on a PNG data URL of the drawing, which has two pixels for every CSS pixel of the pad.
	// A dense signature on a 900 by 200 pad comes to about 70 KB.
	private const long MaxSignatureBytes = 16 * 1024 * 1024;

	private ElementReference _canvasRef;
	private DotNetObjectReference<MokaSignaturePad>? _dotNetRef;
	private bool _drawPending;
	private int _drawRequest;
	private bool _fitted;
	private bool _initialized;
	private int _reportCount;

	// The drawing the pad holds, apart from Value: a stroke shows before the parent answers, and a parent that doesn't
	// own the value can't put the placeholder back over it by rendering again.
	private readonly MokaInputValue<string?> _input;

	// The value from the parent the canvas shows under the user's strokes. Undoing the last of them
	// reports no drawing, but the canvas shows this value again.
	private string? _shownValue;

	/// <summary>Creates the pad. It shows its own drawing until the parent passes a value.</summary>
	public MokaSignaturePad() => _input = new MokaInputValue<string?>(DrawParentValue, DispatchExceptionAsync);

	/// <summary>
	///     The signature as a base64 PNG data URL. Two-way bindable. The image has two pixels for every
	///     CSS pixel of the pad on any screen, so it comes out the same wherever it was drawn and stays
	///     sharp on a high density screen. A value the parent passes is drawn on the canvas, scaled to
	///     fit, and a null or empty one clears it.
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

	/// <summary>Stroke width in CSS pixels. Default 2.</summary>
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

	private string? Drawing => _input.Shown;

	/// <inheritdoc />
	public override Task SetParametersAsync(ParameterView parameters)
	{
		// A bound parent passes back the value the pad just reported. That drawing is already on the canvas, and drawing
		// it again would throw away the strokes undo works through. null and "" both mean an empty pad, so one following
		// the other clears nothing.
		bool wasEmpty = string.IsNullOrEmpty(Drawing);
		if (_input.Take(parameters, nameof(Value)) && !(wasEmpty && string.IsNullOrEmpty(Drawing)))
		{
			_drawPending = true;
		}

		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		base.OnAfterRender(firstRender);
		_input.AfterRender();
	}

	// A drawing the parent didn't take goes, with its strokes, and the parent's value is drawn again.
	private void DrawParentValue()
	{
		_drawPending = true;
		StateHasChanged();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// The module sizes the drawing buffer to the screen's pixels before anything is painted on it, and
		// keeps it so as the pad resizes or moves to a screen of another density. A buffer of a fixed size
		// is stretched to fit by the browser, which blurred the strokes.
		if (firstRender)
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "fitCanvas", _canvasRef,
				new Dictionary<string, object?> { ["backgroundColor"] = BackgroundColor });
			_fitted = true;
		}

		// Drawing is attached the first time the pad is editable: a pad that started read-only or
		// disabled could never be drawn on. Attaching paints the background, so a signature already
		// on the canvas is drawn again after it.
		if (!_initialized && !ReadOnly && !Disabled)
		{
			await InitializeCanvasAsync();
			if (!firstRender && !string.IsNullOrEmpty(Drawing))
			{
				_drawPending = true;
			}
		}

		if (_drawPending)
		{
			_drawPending = false;
			int request = ++_drawRequest;
			string? value = Drawing;
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

	/// <summary>Called from JavaScript with a new drawing, as a stream of its data URL.</summary>
	/// <param name="signature">The data URL, as ASCII bytes.</param>
	[JSInvokable]
	public async Task OnSignatureStream(IJSStreamReference signature)
	{
		ArgumentNullException.ThrowIfNull(signature);

		int report = ++_reportCount;
		string dataUrl;
		try
		{
			await using (signature)
			{
				await using Stream stream = await signature.OpenReadStreamAsync(MaxSignatureBytes);
				using StreamReader reader = new(stream, Encoding.ASCII);
				dataUrl = await reader.ReadToEndAsync();
			}
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
			return;
		}

		// A later stroke, undo or clear got here while this one was being read, and it's the one on the canvas.
		if (report == _reportCount)
		{
			ApplySignature(dataUrl);
		}
	}

	/// <summary>Called from JavaScript when the last stroke is undone, which leaves no drawing to send.</summary>
	/// <param name="signatureData">The signature, or null for no strokes left.</param>
	[JSInvokable]
	public Task OnSignatureChanged(string? signatureData)
	{
		_reportCount++;
		ApplySignature(signatureData);
		return Task.CompletedTask;
	}

	// A call from JS renders nothing by itself, and without a bound parent nothing else would hide the placeholder or
	// show the parent's value again after a drawing it didn't take, so the pad renders after reporting.
	private void ApplySignature(string? signatureData)
	{
		_input.Pick(signatureData ?? _shownValue, ValueChanged);
		StateHasChanged();
	}

	private async Task HandleClear()
	{
		if (!_initialized)
		{
			return;
		}

		// A value still loading is not drawn over the cleared pad, in JS or here, and a stroke still being
		// read doesn't bring the drawing back.
		_drawRequest++;
		_reportCount++;
		await SafeModuleInvokeVoidAsync(ModulePath, "clearSignature", _canvasRef, BackgroundColor);
		_shownValue = null;
		_input.Pick(null, ValueChanged);
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
		// A read-only pad never attaches drawing, but its canvas is still watched for size.
		if (_fitted)
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "removeCanvasDraw", _canvasRef);
		}

		_input.Stop();
		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();

	}
}
