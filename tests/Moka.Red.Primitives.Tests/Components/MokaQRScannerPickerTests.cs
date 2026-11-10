using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moka.Red.Core.Files;
using Moka.Red.Primitives.QRScanner;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// With an app's own picker the image button never opens the browser's dialog. The picked image arrives in .NET, so it
// is streamed to the browser, which reads the code as it does for its own pick.
public class MokaQRScannerPickerTests : BunitContext
{
	private const string ModulePath = "./_content/Moka.Red.Primitives/QRScanner/MokaQRScanner.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaQRScannerPickerTests()
	{
		_module = JSInterop.SetupModule(ModulePath);
		_module.Setup<string>("startScanner", _ => true).SetResult("started");
		_module.SetupVoid("stopScanner", _ => true).SetVoidResult();
		_module.SetupVoid("resumeScanner", _ => true).SetVoidResult();
		AnswerDecode("hello");
	}

	private void AnswerDecode(string? text) => _module.Setup<string?>("decodeImageStream", _ => true).SetResult(text);

	private static string Status(IRenderedComponent<MokaQRScanner> cut) => cut.Find("[role=status]").TextContent;

	private static IElement ImageButton(IRenderedComponent<MokaQRScanner> cut) => cut.Find("label.moka-qr-scanner__pick");

	private static bool Has(IElement element, string attribute) =>
		element.Attributes.Any(a => string.Equals(a.Name, attribute, StringComparison.OrdinalIgnoreCase));

	private FakeFilePicker Register(params PickedFile[] files)
	{
		FakeFilePicker picker = new() { Files = files };
		Services.AddSingleton<IMokaFilePicker>(picker);
		return picker;
	}

	[Fact]
	public void WithoutAPicker_TheButtonIsTheBrowsersAsBefore()
	{
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>();
		IElement button = ImageButton(cut);

		Assert.False(Has(button, "blazor:onclick"));
		Assert.False(Has(button, "blazor:onclick:preventDefault"));
		Assert.Empty(_module.Invocations["decodeImageStream"]);
	}

	[Fact]
	public void ARegisteredPicker_IsAskedForOneImage_AndTheBrowserReadsIt()
	{
		string? scanned = null;
		PickedFile image = new("code.png", "png bytes", "image/png");
		FakeFilePicker picker = Register(image);
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p.Add(x => x.OnScanned, text => scanned = text));
		Assert.True(Has(ImageButton(cut), "blazor:onclick:preventDefault"));

		cut.Find("input[type=file]").Click();

		Assert.Equal([new MokaFilePickRequest(false, "image/*")], picker.Requests);
		cut.WaitForAssertion(() => Assert.Equal("hello", scanned));
		JSRuntimeInvocation decode = Assert.Single(_module.Invocations["decodeImageStream"]);
		Assert.IsType<DotNetStreamReference>(decode.Arguments[0]);
		Assert.Equal("image/png", decode.Arguments[1]);
		Assert.Equal(1, image.Opened);
		Assert.Empty(_module.Invocations["decodePickedImage"]);
		Assert.Equal("Code read.", Status(cut));
	}

	[Fact]
	public void AnImageWithNoCode_SaysSo_AsBefore()
	{
		AnswerDecode(null);
		Register(new PickedFile("photo.jpg", contentType: "image/jpeg"));
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p.Add(x => x.NoCodeText, name => $"Nothing in {name}"));

		ImageButton(cut).Click();

		cut.WaitForAssertion(() => Assert.Equal("Nothing in photo.jpg", Status(cut)));
	}

	[Fact]
	public void ATurnedDownCode_SaysSo_AsBefore()
	{
		string? scanned = null;
		Register(new PickedFile("code.png", contentType: "image/png"));
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p
			.Add(x => x.Accept, text => text.StartsWith("https:", StringComparison.Ordinal))
			.Add(x => x.OnScanned, text => scanned = text));

		ImageButton(cut).Click();

		cut.WaitForAssertion(() => Assert.Equal("That QR code isn't one this app can use.", Status(cut)));
		Assert.Null(scanned);
	}

	[Fact]
	public void AnImageTooBigToStream_IsNotRead()
	{
		PickedFile huge = new("huge.png", contentType: "image/png") { SizeOverride = 64L * 1024 * 1024 };
		Register(huge);
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>();

		ImageButton(cut).Click();

		cut.WaitForAssertion(() => Assert.Equal("No QR code found in huge.png.", Status(cut)));
		Assert.Empty(_module.Invocations["decodeImageStream"]);
		Assert.Equal(0, huge.Opened);
	}

	[Fact]
	public void ACancelledPick_ChangesNothing()
	{
		FakeFilePicker picker = Register();
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>();

		ImageButton(cut).Click();
		picker.Failure = new OperationCanceledException();
		ImageButton(cut).Click();

		Assert.Equal(2, picker.Requests.Count);
		Assert.Equal("The camera is off.", Status(cut));
		Assert.Empty(_module.Invocations["decodeImageStream"]);
	}

	[Fact]
	public async Task AFailingPicker_IsNotSwallowed()
	{
		FakeFilePicker picker = Register();
		picker.Failure = new UnauthorizedAccessException("Photos are blocked.");
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>();

		UnauthorizedAccessException thrown = await Assert.ThrowsAsync<UnauthorizedAccessException>(
			() => ImageButton(cut).ClickAsync(new MouseEventArgs()));

		Assert.Equal("Photos are blocked.", thrown.Message);
	}

	[Fact]
	public async Task WhileThePickerIsOpen_AnotherClickDoesNothing()
	{
		string? scanned = null;
		FakeFilePicker picker = Register();
		picker.StaysOpen = true;
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p
			.Add(x => x.Continuous, true)
			.Add(x => x.OnScanned, text => scanned = text));

		Task first = ImageButton(cut).ClickAsync(new MouseEventArgs());
		await ImageButton(cut).ClickAsync(new MouseEventArgs());
		Assert.Single(picker.Requests);

		picker.Close(new PickedFile("code.png", contentType: "image/png"));
		await first;
		Assert.Equal("hello", scanned);

		picker.StaysOpen = false;
		await ImageButton(cut).ClickAsync(new MouseEventArgs());
		Assert.Equal(2, picker.Requests.Count);
	}

	[Fact]
	public void TheParameter_WinsOverTheRegisteredPicker()
	{
		FakeFilePicker registered = Register(new PickedFile("registered.png", contentType: "image/png"));
		FakeFilePicker own = new() { Files = [new PickedFile("own.png", contentType: "image/png")] };
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p.Add(x => x.FilePicker, own));

		ImageButton(cut).Click();

		Assert.Empty(registered.Requests);
		Assert.Single(own.Requests);
	}

	[Fact]
	public void Disabled_DoesNotOpenThePicker()
	{
		FakeFilePicker picker = Register(new PickedFile("code.png", contentType: "image/png"));
		IRenderedComponent<MokaQRScanner> cut = Render<MokaQRScanner>(p => p.Add(x => x.Disabled, true));

		ImageButton(cut).Click();

		Assert.Empty(picker.Requests);
	}
}
