using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Icons;

namespace Moka.Red.Primitives.QRScanner;

/// <summary>
///     Reads a QR code from the camera, or from an image the person picks. Decoding happens in the browser (its own
///     detector where it has one, else the bundled jsQR, loaded only once something is scanned), so frames never
///     reach .NET. An inline component: put it in a <c>MokaDialog</c> for a scan dialog. The camera runs only while
///     <see cref="Active" /> is on, and stops when it goes off, when the component is disposed, and after a scan.
/// </summary>
public partial class MokaQRScanner : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Primitives/QRScanner/MokaQRScanner.razor.js";
	private const int FrameMaxSide = 720;
	private const int FrameInterval = 200;

	private ElementReference _video;
	private ElementReference _picker;
	private DotNetObjectReference<MokaQRScanner>? _dotNetRef;
	private MokaQRScannerState _state = MokaQRScannerState.Off;
	private MokaQRScannerState _reportedState = MokaQRScannerState.Off;
	private string? _message;
	private bool _lastActive;
	private bool _lastDisabled;
	private bool _cameraOn;
	private bool _startPending;
	private bool _stopPending;

	// Each start and stop takes a number, so the answer to a start that a later start or stop overtook is ignored.
	private int _generation;

	/// <summary>
	///     Whether the camera is on. After a scan the scanner turns itself off and raises <see cref="ActiveChanged" />
	///     with false, so <c>@bind-Active</c> follows it. Without <see cref="Continuous" /> one code ends the scan.
	/// </summary>
	[Parameter]
	public bool Active { get; set; }

	/// <summary>Raised when the scanner turns itself off after a scan.</summary>
	[Parameter]
	public EventCallback<bool> ActiveChanged { get; set; }

	/// <summary>The text of a code that was read and passed <see cref="Accept" />.</summary>
	[Parameter]
	public EventCallback<string> OnScanned { get; set; }

	/// <summary>
	///     Whether a code is one the app can use. A code it turns down shows <see cref="RejectedText" /> and the scan
	///     goes on. Null takes every code.
	/// </summary>
	[Parameter]
	public Func<string, bool>? Accept { get; set; }

	/// <summary>
	///     Keeps scanning after a code is read, reporting each new one. A code is reported once while it stays in view.
	///     Default false: one code ends the scan.
	/// </summary>
	[Parameter]
	public bool Continuous { get; set; }

	/// <summary>Raised whenever the state changes, including when the camera can't start and why.</summary>
	[Parameter]
	public EventCallback<MokaQRScannerState> OnStateChanged { get; set; }

	/// <summary>Whether a button lets the person pick an image of a code instead. Default true.</summary>
	[Parameter]
	public bool ShowImagePicker { get; set; } = true;

	/// <summary>The image button's text. Default "Use an image".</summary>
	[Parameter]
	public string PickImageText { get; set; } = "Use an image";

	/// <summary>Said while the camera is off. Default "The camera is off."</summary>
	[Parameter]
	public string OffText { get; set; } = "The camera is off.";

	/// <summary>Said while the camera starts. Default "Starting the camera."</summary>
	[Parameter]
	public string StartingText { get; set; } = "Starting the camera.";

	/// <summary>Said while scanning. Default "Point the camera at a QR code."</summary>
	[Parameter]
	public string ScanningText { get; set; } = "Point the camera at a QR code.";

	/// <summary>Said once a code is read and the scan is over. Default "Code read."</summary>
	[Parameter]
	public string FoundText { get; set; } = "Code read.";

	/// <summary>Said when <see cref="Accept" /> turns a code down. Default "That QR code isn't one this app can use."</summary>
	[Parameter]
	public string RejectedText { get; set; } = "That QR code isn't one this app can use.";

	/// <summary>Said when the camera was refused. Default "Camera access was blocked."</summary>
	[Parameter]
	public string DeniedText { get; set; } = "Camera access was blocked.";

	/// <summary>Said when there is no camera. Default "No camera was found."</summary>
	[Parameter]
	public string NoCameraText { get; set; } = "No camera was found.";

	/// <summary>Said when another app holds the camera. Default "The camera is in use by another app."</summary>
	[Parameter]
	public string CameraBusyText { get; set; } = "The camera is in use by another app.";

	/// <summary>Said when the browser can't use a camera here. Default "This browser can't use the camera here."</summary>
	[Parameter]
	public string UnsupportedText { get; set; } = "This browser can't use the camera here.";

	/// <summary>Said when the camera failed to start for another reason. Default "The camera couldn't start."</summary>
	[Parameter]
	public string FailedText { get; set; } = "The camera couldn't start.";

	/// <summary>
	///     Added after a camera problem while the image button shows. Default "You can pick an image of the code instead."
	/// </summary>
	[Parameter]
	public string ImageFallbackText { get; set; } = "You can pick an image of the code instead.";

	/// <summary>Said when a picked image holds no code, given the file's name. Null says "No QR code found in {name}."</summary>
	[Parameter]
	public Func<string, string>? NoCodeText { get; set; }

	/// <summary>Where the scanner is now.</summary>
	public MokaQRScannerState State => _state;

	/// <inheritdoc />
	protected override string RootClass => "moka-qr-scanner";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-qr-scanner--scanning", _state == MokaQRScannerState.Scanning)
		.AddClass("moka-qr-scanner--disabled", Disabled)
		.AddClass(Class)
		.Build();

	// The viewfinder draws the box, so it takes the padding and the radius; the root only stacks it over the bar.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle(Style)
		.Build();

	private string? ViewStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private bool HasProblem => _state is MokaQRScannerState.Denied or MokaQRScannerState.NoCamera
		or MokaQRScannerState.CameraBusy or MokaQRScannerState.Unsupported or MokaQRScannerState.Failed;

	private string CoverCssClass => new CssBuilder("moka-qr-scanner__cover")
		.AddClass("moka-qr-scanner__cover--problem", HasProblem)
		.Build();

	private string PickCssClass => new CssBuilder("moka-qr-scanner__pick")
		.AddClass("moka-qr-scanner__pick--disabled", Disabled)
		.Build();

	private MokaIconDefinition CoverIcon => _state == MokaQRScannerState.Off ? MokaIcons.Content.QrCode : MokaIcons.Content.Camera;

	private string StatusText
	{
		get
		{
			if (_message is not null)
			{
				return _message;
			}

			string text = _state switch
			{
				MokaQRScannerState.Starting => StartingText,
				MokaQRScannerState.Scanning => ScanningText,
				MokaQRScannerState.Denied => DeniedText,
				MokaQRScannerState.NoCamera => NoCameraText,
				MokaQRScannerState.CameraBusy => CameraBusyText,
				MokaQRScannerState.Unsupported => UnsupportedText,
				MokaQRScannerState.Failed => FailedText,
				_ => OffText
			};

			return HasProblem && ShowImagePicker && !string.IsNullOrEmpty(ImageFallbackText) ? $"{text} {ImageFallbackText}" : text;
		}
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Only a change of the parameter counts, so a scanner that turned itself off stays off through the parent's
		// renders until the parent turns it off and on again.
		if (Active == _lastActive && Disabled == _lastDisabled)
		{
			return;
		}

		_lastActive = Active;
		_lastDisabled = Disabled;
		bool wanted = Active && !Disabled;

		if (wanted && !_cameraOn && !_startPending)
		{
			_startPending = true;
			_stopPending = false;
			_message = null;
			_state = MokaQRScannerState.Starting;
		}
		else if (!wanted && (_cameraOn || _startPending || _state == MokaQRScannerState.Starting))
		{
			_startPending = false;
			_stopPending = true;
			_message = null;
			_state = MokaQRScannerState.Off;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (_stopPending)
		{
			_stopPending = false;
			await StopCameraAsync();
			await RaiseStateAsync();
		}

		if (_startPending)
		{
			_startPending = false;
			await RaiseStateAsync();
			await StartCameraAsync();
		}
	}

	/// <summary>Called by the module with each new code it reads. The scan waits until this decides.</summary>
	/// <param name="text">The code's text.</param>
	[JSInvokable]
	public void OnCodeFound(string text) => RunDetached(() => HandleCodeAsync(text));

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_cameraOn || _state == MokaQRScannerState.Starting)
		{
			_generation++;
			_cameraOn = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "stopScanner", _video);
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}

	private async Task StartCameraAsync()
	{
		int generation = ++_generation;
		_cameraOn = true;
		_dotNetRef ??= DotNetObjectReference.Create(this);

		Dictionary<string, object?> options = new()
		{
			["maxSide"] = FrameMaxSide,
			["interval"] = FrameInterval
		};

		string? result = await SafeModuleInvokeAsync<string>(ModulePath, "startScanner", _video, _dotNetRef, options);
		if (generation != _generation || result is null or "stopped")
		{
			return;
		}

		_state = result switch
		{
			"started" => MokaQRScannerState.Scanning,
			"denied" => MokaQRScannerState.Denied,
			"missing" => MokaQRScannerState.NoCamera,
			"busy" => MokaQRScannerState.CameraBusy,
			"insecure" or "unsupported" => MokaQRScannerState.Unsupported,
			_ => MokaQRScannerState.Failed
		};

		_cameraOn = _state == MokaQRScannerState.Scanning;
		StateHasChanged();
		await RaiseStateAsync();
	}

	private async Task StopCameraAsync()
	{
		_generation++;
		if (!_cameraOn)
		{
			return;
		}

		_cameraOn = false;
		await SafeModuleInvokeVoidAsync(ModulePath, "stopScanner", _video);
	}

	private async Task HandleCodeAsync(string text)
	{
		if (!_cameraOn || string.IsNullOrEmpty(text))
		{
			return;
		}

		if (Accept is not null && !Accept(text))
		{
			_message = RejectedText;
			StateHasChanged();
			await SafeModuleInvokeVoidAsync(ModulePath, "resumeScanner", _video);
			return;
		}

		if (Continuous)
		{
			_message = null;
			await OnScanned.InvokeAsync(text);
			await SafeModuleInvokeVoidAsync(ModulePath, "resumeScanner", _video);
			return;
		}

		await FinishAsync(text);
	}

	private async Task HandleImagePickedAsync(ChangeEventArgs args)
	{
		if (Disabled)
		{
			return;
		}

		string?[]? picked = await SafeModuleInvokeAsync<string?[]>(ModulePath, "decodePickedImage", _picker);
		if (picked is not { Length: 2 })
		{
			return;
		}

		string name = picked[0] ?? string.Empty;
		string? text = picked[1];
		if (string.IsNullOrEmpty(text))
		{
			_message = NoCodeText?.Invoke(name) ?? $"No QR code found in {name}.";
			return;
		}

		if (Accept is not null && !Accept(text))
		{
			_message = RejectedText;
			return;
		}

		if (Continuous)
		{
			_message = null;
			await OnScanned.InvokeAsync(text);
			return;
		}

		await FinishAsync(text);
	}

	// One code ends the scan: the camera goes off, the app hears the code, and a bound Active follows.
	private async Task FinishAsync(string text)
	{
		bool wasOn = _cameraOn || _state != MokaQRScannerState.Off;
		await StopCameraAsync();
		_state = MokaQRScannerState.Off;
		_message = FoundText;
		StateHasChanged();
		await RaiseStateAsync();
		await OnScanned.InvokeAsync(text);
		if (wasOn && Active)
		{
			await ActiveChanged.InvokeAsync(false);
		}
	}

	private Task RaiseStateAsync()
	{
		if (_state == _reportedState)
		{
			return Task.CompletedTask;
		}

		_reportedState = _state;
		return OnStateChanged.InvokeAsync(_state);
	}
}
