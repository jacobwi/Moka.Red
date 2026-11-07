using AngleSharp.Dom;
using Bunit;
using Moka.Red.Primitives.QRScanner;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaQRScannerTests : BunitContext
{
	private const string ModulePath = "./_content/Moka.Red.Primitives/QRScanner/MokaQRScanner.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaQRScannerTests()
	{
		_module = JSInterop.SetupModule(ModulePath);
		AnswerStart("started");
		_module.SetupVoid("stopScanner", _ => true).SetVoidResult();
		_module.SetupVoid("resumeScanner", _ => true).SetVoidResult();
	}

	// The latest handler set up for a call is the one that answers it.
	private void AnswerStart(string answer) => _module.Setup<string>("startScanner", _ => true).SetResult(answer);

	private static string Status(IRenderedComponent<MokaQRScanner> cut) => cut.Find("[role=status]").TextContent;

	[Fact]
	public void Off_TheCameraIsNotAskedFor()
	{
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>();

		Assert.Equal("The camera is off.", Status(cut));
		Assert.Empty(_module.Invocations["startScanner"]);
		Assert.Empty(cut.FindAll(".moka-qr-scanner__beam"));
		Assert.Equal(MokaQRScannerState.Off, cut.Instance.State);
	}

	[Fact]
	public void Active_StartsTheCamera_AndScans()
	{
		List<MokaQRScannerState> states = [];
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p
			.Add(x => x.Active, true)
			.Add(x => x.OnStateChanged, state => states.Add(state)));

		JSRuntimeInvocation start = Assert.Single(_module.Invocations["startScanner"]);
		Dictionary<string, object?> options = Assert.IsType<Dictionary<string, object?>>(start.Arguments[2]);
		Assert.Equal(720, options["maxSide"]);
		cut.WaitForAssertion(() => Assert.Equal("Point the camera at a QR code.", Status(cut)));
		Assert.Single(cut.FindAll(".moka-qr-scanner__beam"));
		Assert.Contains("moka-qr-scanner--scanning", cut.Find(".moka-qr-scanner").ClassList);
		Assert.Equal([MokaQRScannerState.Starting, MokaQRScannerState.Scanning], states);
	}

	[Theory]
	[InlineData("denied", MokaQRScannerState.Denied, "Camera access was blocked.")]
	[InlineData("missing", MokaQRScannerState.NoCamera, "No camera was found.")]
	[InlineData("busy", MokaQRScannerState.CameraBusy, "The camera is in use by another app.")]
	[InlineData("insecure", MokaQRScannerState.Unsupported, "This browser can't use the camera here.")]
	[InlineData("failed", MokaQRScannerState.Failed, "The camera couldn't start.")]
	public void ACameraThatWontStart_SaysWhy_AndOffersAnImage(string answer, MokaQRScannerState state, string text)
	{
		AnswerStart(answer);
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p.Add(x => x.Active, true));

		cut.WaitForAssertion(() => Assert.Equal(state, cut.Instance.State));
		Assert.Equal($"{text} You can pick an image of the code instead.", Status(cut));
		Assert.Contains("moka-qr-scanner__cover--problem", cut.Find(".moka-qr-scanner__cover").ClassList);
	}

	[Fact]
	public void WithoutTheImagePicker_AProblemOffersNothingElse()
	{
		AnswerStart("denied");
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p
			.Add(x => x.Active, true)
			.Add(x => x.ShowImagePicker, false));

		cut.WaitForAssertion(() => Assert.Equal("Camera access was blocked.", Status(cut)));
		Assert.Empty(cut.FindAll("input[type=file]"));
	}

	[Fact]
	public void ACode_EndsTheScan_AndBoundActiveFollows()
	{
		string? scanned = null;
		bool? active = null;
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p
			.Add(x => x.Active, true)
			.Add(x => x.ActiveChanged, value => active = value)
			.Add(x => x.OnScanned, text => scanned = text));
		cut.WaitForAssertion(() => Assert.Equal(MokaQRScannerState.Scanning, cut.Instance.State));

		cut.InvokeAsync(() => cut.Instance.OnCodeFound("hello"));

		cut.WaitForAssertion(() => Assert.Equal("hello", scanned));
		Assert.False(active);
		Assert.Single(_module.Invocations["stopScanner"]);
		Assert.Equal("Code read.", Status(cut));
		Assert.Equal(MokaQRScannerState.Off, cut.Instance.State);
	}

	// Without a binding the parent keeps passing Active="true", and that mustn't turn the camera back on.
	[Fact]
	public void AfterAScan_AnUnboundActiveDoesNotRestartIt()
	{
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p.Add(x => x.Active, true));
		cut.WaitForAssertion(() => Assert.Equal(MokaQRScannerState.Scanning, cut.Instance.State));
		cut.InvokeAsync(() => cut.Instance.OnCodeFound("hello"));
		cut.WaitForAssertion(() => Assert.Equal(MokaQRScannerState.Off, cut.Instance.State));

		cut.Render(p => p.Add(x => x.Active, true));

		Assert.Single(_module.Invocations["startScanner"]);
	}

	[Fact]
	public void ACodeTheAppTurnsDown_SaysSo_AndScanningGoesOn()
	{
		string? scanned = null;
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p
			.Add(x => x.Active, true)
			.Add(x => x.Accept, text => text.StartsWith("ok:", StringComparison.Ordinal))
			.Add(x => x.OnScanned, text => scanned = text));
		cut.WaitForAssertion(() => Assert.Equal(MokaQRScannerState.Scanning, cut.Instance.State));

		cut.InvokeAsync(() => cut.Instance.OnCodeFound("nope"));

		cut.WaitForAssertion(() => Assert.Single(_module.Invocations["resumeScanner"]));
		Assert.Null(scanned);
		Assert.Equal("That QR code isn't one this app can use.", Status(cut));
		Assert.Empty(_module.Invocations["stopScanner"]);
	}

	[Fact]
	public void Continuous_ReportsEachCode_AndKeepsScanning()
	{
		List<string> scanned = [];
		bool? active = null;
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p
			.Add(x => x.Active, true)
			.Add(x => x.Continuous, true)
			.Add(x => x.ActiveChanged, value => active = value)
			.Add(x => x.OnScanned, text => scanned.Add(text)));
		cut.WaitForAssertion(() => Assert.Equal(MokaQRScannerState.Scanning, cut.Instance.State));

		cut.InvokeAsync(() => cut.Instance.OnCodeFound("one"));
		cut.InvokeAsync(() => cut.Instance.OnCodeFound("two"));

		cut.WaitForAssertion(() => Assert.Equal(["one", "two"], scanned));
		Assert.Equal(2, _module.Invocations["resumeScanner"].Count);
		Assert.Null(active);
		Assert.Equal(MokaQRScannerState.Scanning, cut.Instance.State);
	}

	[Fact]
	public void TurningActiveOff_StopsTheCamera()
	{
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p.Add(x => x.Active, true));
		cut.WaitForAssertion(() => Assert.Equal(MokaQRScannerState.Scanning, cut.Instance.State));

		cut.Render(p => p.Add(x => x.Active, false));

		Assert.Single(_module.Invocations["stopScanner"]);
		Assert.Equal("The camera is off.", Status(cut));
	}

	[Fact]
	public void Disabled_StopsTheCamera_AndThePicker()
	{
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p.Add(x => x.Active, true));
		cut.WaitForAssertion(() => Assert.Equal(MokaQRScannerState.Scanning, cut.Instance.State));

		cut.Render(p => p.Add(x => x.Disabled, true));

		Assert.Single(_module.Invocations["stopScanner"]);
		Assert.True(cut.Find("input[type=file]").HasAttribute("disabled"));
	}

	[Fact]
	public async Task Disposing_StopsTheCamera()
	{
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p.Add(x => x.Active, true));
		cut.WaitForAssertion(() => Assert.Equal(MokaQRScannerState.Scanning, cut.Instance.State));

		await DisposeComponentsAsync();

		Assert.Single(_module.Invocations["stopScanner"]);
	}

	// A start the person hasn't answered yet, overtaken by turning it off: its late answer mustn't switch the
	// scanner back to scanning.
	[Fact]
	public void AStartOvertakenByAStop_IsIgnored()
	{
		JSRuntimeInvocationHandler<string> pending = _module.Setup<string>("startScanner", _ => true);
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p.Add(x => x.Active, true));
		Assert.Equal("Starting the camera.", Status(cut));

		cut.Render(p => p.Add(x => x.Active, false));
		pending.SetResult("started");

		cut.WaitForAssertion(() => Assert.Equal("The camera is off.", Status(cut)));
		Assert.Equal(MokaQRScannerState.Off, cut.Instance.State);
	}

	[Fact]
	public void APickedImage_IsDecodedInTheBrowser_AndScanned()
	{
		_module.Setup<string?[]>("decodePickedImage", _ => true).SetResult(["code.png", "hello"]);
		string? scanned = null;
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p.Add(x => x.OnScanned, text => scanned = text));

		cut.Find("input[type=file]").Change("C:\\fakepath\\code.png");

		cut.WaitForAssertion(() => Assert.Equal("hello", scanned));
		Assert.Equal("Code read.", Status(cut));
	}

	[Fact]
	public void AnImageWithNoCode_SaysSo()
	{
		_module.Setup<string?[]>("decodePickedImage", _ => true).SetResult(["photo.jpg", null]);
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>();

		cut.Find("input[type=file]").Change("C:\\fakepath\\photo.jpg");
		cut.WaitForAssertion(() => Assert.Equal("No QR code found in photo.jpg.", Status(cut)));

		cut.Render(p => p.Add(x => x.NoCodeText, name => $"Nothing in {name}"));
		cut.Find("input[type=file]").Change("C:\\fakepath\\photo.jpg");
		cut.WaitForAssertion(() => Assert.Equal("Nothing in photo.jpg", Status(cut)));
	}

	// The picker is a label around the file input, so it reads as its text and a click anywhere on it opens the dialog.
	[Fact]
	public void TheImagePicker_IsALabelledFileInput()
	{
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p.Add(x => x.PickImageText, "Pick a photo"));

		IElement label = cut.Find("label.moka-qr-scanner__pick");
		Assert.Contains("Pick a photo", label.TextContent, StringComparison.Ordinal);
		Assert.Equal("image/*", label.QuerySelector("input[type=file]")!.GetAttribute("accept"));
	}
}
