using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.SignaturePad;

/// <summary>
///     Drawing canvas for capturing signatures. Uses JS interop for canvas drawing operations.
///     Supports undo, clear, and exports the signature as a base64 data URL.
/// </summary>
public partial class MokaSignaturePad : MokaVisualComponentBase
{
	private ElementReference _canvasRef;
	private DotNetObjectReference<MokaSignaturePad>? _dotNetRef;
	private bool _initialized;
	private IJSObjectReference? _module;

	/// <summary>The signature as a base64 data URL. Two-way bindable.</summary>
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

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("width", Width)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	/// <summary>Override ShouldRender to always return true for canvas state updates.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && !ReadOnly && !Disabled)
		{
			await InitializeCanvasAsync();
		}
	}

	private async Task InitializeCanvasAsync()
	{
		if (_initialized)
		{
			return;
		}

		_dotNetRef = DotNetObjectReference.Create(this);
		_module = await GetJsModuleAsync("./_content/Moka.Red.Core/moka-drag.js");
		await _module.InvokeVoidAsync("initCanvasDraw", _dotNetRef, _canvasRef, new
		{
			strokeColor = StrokeColor,
			strokeWidth = StrokeWidth,
			backgroundColor = BackgroundColor
		});
		_initialized = true;
	}

	/// <summary>Called from JavaScript when the signature changes.</summary>
	[JSInvokable]
	public async Task OnSignatureChanged(string? signatureData)
	{
		Value = signatureData;
		if (ValueChanged.HasDelegate)
		{
			await ValueChanged.InvokeAsync(signatureData);
		}
	}

	private async Task HandleClear()
	{
		if (_module is null)
		{
			return;
		}

		await _module.InvokeVoidAsync("clearCanvas", _canvasRef, BackgroundColor);
		Value = null;
		await ValueChanged.InvokeAsync(null);
	}

	private async Task HandleUndo()
	{
		if (_module is null)
		{
			return;
		}

		await _module.InvokeVoidAsync("undoCanvas", _canvasRef, StrokeColor, StrokeWidth, BackgroundColor);
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_module is not null)
		{
			try
			{
				await _module.InvokeVoidAsync("removeCanvasDraw", _canvasRef);
			}
			catch (JSDisconnectedException)
			{
				// Circuit disconnected
			}
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}
}
