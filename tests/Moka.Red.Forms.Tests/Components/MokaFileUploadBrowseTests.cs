using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Files;
using Moka.Red.Forms.FileUpload;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// BrowseLook Button draws Browse as an outlined button in the drop zone. It is only a picture: the file input still
// covers the zone and takes the click, the keyboard and the drop, so nothing about picking changes.
public partial class MokaFileUploadBrowseTests : BunitContext
{
	private const string Css = "MokaFileUpload.razor.css";
	private const string Button = "moka-fileupload-browse-button";

	public MokaFileUploadBrowseTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<MokaFileUpload> Upload(MokaFileUploadBrowseLook browse, MokaFileUploadLook look = MokaFileUploadLook.Standard,
		bool compact = false) =>
		Render<MokaFileUpload>(p => p
			.Add(x => x.Id, "files")
			.Add(x => x.Look, look)
			.Add(x => x.Compact, compact)
			.Add(x => x.BrowseLook, browse));

	[Fact]
	public void TheDefault_IsTheLink()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>();

		Assert.Equal(MokaFileUploadBrowseLook.Link, cut.Instance.BrowseLook);

		Assert.Equal("moka-fileupload-browse", cut.Find("label[for]").ClassName);
		Assert.Empty(cut.FindAll($".{Button}"));
	}

	// The link sits inside the card list's sentence, as it always has.
	[Fact]
	public void TheCardListsLink_StaysInItsSentence()
	{
		IRenderedComponent<MokaFileUpload> cut = Upload(MokaFileUploadBrowseLook.Link, MokaFileUploadLook.CardList);

		IElement text = cut.Find(".moka-fileupload-text");
		Assert.Equal("Drag files here or browse", text.TextContent.Trim());
		Assert.Equal("moka-fileupload-browse", text.QuerySelector("label")!.ClassName);
		Assert.Equal(2, cut.Find(".moka-fileupload-dropzone-content--row").Children.Length);
	}

	[Fact]
	public void Button_InTheStandardZone_IsAnOutlinedLabelForTheInput()
	{
		IRenderedComponent<MokaFileUpload> cut = Upload(MokaFileUploadBrowseLook.Button);

		IElement label = cut.Find(".moka-fileupload-dropzone-content > label");
		Assert.Equal(Button, label.ClassName);
		Assert.Equal("files", label.GetAttribute("for"));
		Assert.Equal("Browse", label.TextContent);
		Assert.Empty(cut.FindAll(".moka-fileupload-browse"));
		Assert.Equal("Drag files here or", cut.Find(".moka-fileupload-text").TextContent);
	}

	[Fact]
	public void Button_InACardList_EndsTheRow()
	{
		IRenderedComponent<MokaFileUpload> cut = Upload(MokaFileUploadBrowseLook.Button, MokaFileUploadLook.CardList);

		IElement row = cut.Find(".moka-fileupload-dropzone-content--row");
		IElement label = row.Children[^1];
		Assert.Equal($"{Button} {Button}--end", label.ClassName);
		Assert.Equal("files", label.GetAttribute("for"));
		Assert.Equal("Browse", label.TextContent);
		Assert.Equal("Drag files here or", cut.Find(".moka-fileupload-text").TextContent.Trim());
		Assert.Empty(cut.FindAll(".moka-fileupload-text label"));
	}

	[Fact]
	public void Button_InACompactZone_IsTheOneButton()
	{
		IRenderedComponent<MokaFileUpload> cut = Upload(MokaFileUploadBrowseLook.Button, compact: true);

		IElement label = cut.Find(".moka-fileupload-dropzone > label");
		Assert.Equal(Button, label.ClassName);
		Assert.Equal("Choose file(s)", label.TextContent);
	}

	[Fact]
	public void TheWords_AreTheUploadsOwn()
	{
		IRenderedComponent<MokaFileUpload> cut = Render<MokaFileUpload>(p => p
			.Add(x => x.BrowseLook, MokaFileUploadBrowseLook.Button)
			.Add(x => x.Text, new MokaFileUploadText { Browse = "Bläddra" }));

		Assert.Equal("Bläddra", cut.Find($".{Button}").TextContent);
	}

	// Only the label changes: the zone keeps its attributes, and the input still covers it and comes last.
	[Theory]
	[InlineData(MokaFileUploadLook.Standard, false)]
	[InlineData(MokaFileUploadLook.Standard, true)]
	[InlineData(MokaFileUploadLook.CardList, false)]
	public void Button_LeavesTheZoneAndTheInputAsTheyAre(MokaFileUploadLook look, bool compact)
	{
		IElement link = Upload(MokaFileUploadBrowseLook.Link, look, compact).Find(".moka-fileupload-dropzone");
		IElement button = Upload(MokaFileUploadBrowseLook.Button, look, compact).Find(".moka-fileupload-dropzone");

		Assert.Equal(Attributes(link), Attributes(button));
		Assert.Equal("input", button.Children[^1].LocalName);
		Assert.Equal(Attributes(link.Children[^1]), Attributes(button.Children[^1]));
	}

	[Theory]
	[InlineData(MokaFileUploadLook.Field)]
	[InlineData(MokaFileUploadLook.Quiet)]
	[InlineData(MokaFileUploadLook.Slot)]
	[InlineData(MokaFileUploadLook.Terminal)]
	public void TheOtherLooks_IgnoreIt(MokaFileUploadLook look) =>
		Assert.Equal(Normalized(Upload(MokaFileUploadBrowseLook.Link, look).Markup),
			Normalized(Upload(MokaFileUploadBrowseLook.Button, look).Markup));

	[Fact]
	public void WithAPicker_TheButtonOpensIt_AsTheLinkDid()
	{
		FakeFilePicker picker = new() { Files = [new PickedFile("a.txt")] };
		Services.AddSingleton<IMokaFilePicker>(picker);
		IRenderedComponent<MokaFileUpload> cut = Upload(MokaFileUploadBrowseLook.Button);

		cut.Find($".{Button}").Click();

		Assert.Single(picker.Requests);
		Assert.Equal(["a.txt"], cut.FindAll(".moka-fileupload-filename").Select(name => name.TextContent));
	}

	[Fact]
	public void TheButton_IsOutlinedOnBothDesigns_AndLightsUpWithTheZone()
	{
		IReadOnlyDictionary<string, string> matrix = ScopedCss.Declarations(Css, $".{Button}");
		Assert.Equal("var(--moka-border-width) solid var(--moka-color-primary)", matrix["border"]);
		Assert.Equal("transparent", matrix["background-color"]);
		Assert.Equal("uppercase", matrix["text-transform"]);
		Assert.Equal("var(--moka-color-primary-glow)",
			ScopedCss.Declarations(Css, $".moka-fileupload-dropzone:hover .{Button}")["background-color"]);

		IReadOnlyDictionary<string, string> gate = ScopedCss.Declarations(Css, $".moka-gate .{Button}");
		Assert.Equal("var(--moka-color-outline-strong)", gate["border-color"]);
		Assert.Equal("var(--moka-color-surface)", gate["background-color"]);
		Assert.Equal("var(--moka-control-case)", gate["text-transform"]);
		Assert.Equal("var(--moka-button-height-sm)", gate["min-height"]);
		Assert.Equal("var(--moka-color-surface-hover)",
			ScopedCss.Declarations(Css, $".moka-gate .moka-fileupload-dropzone:hover .{Button}")["background-color"]);
	}

	// The check while dragging takes the compact zone's place, its one button included.
	[Fact]
	public void TheCheckWhileDragging_HidesACompactZonesButton() =>
		Assert.Equal("none", ScopedCss.Declarations(Css, $".moka-fileupload-dropzone[data-moka-drag-check] > .{Button}")["display"]);

	// Two renders number their event handlers and element references apart, so only the names of those count.
	private static string[] Attributes(IElement element) =>
		element.Attributes
			.Select(a => a.Name.StartsWith("blazor:", StringComparison.Ordinal) ? a.Name : $"{a.Name}={a.Value}")
			.Order(StringComparer.Ordinal)
			.ToArray();

	private static string Normalized(string markup) => BlazorAttribute().Replace(markup, "$1");

	[GeneratedRegex("(blazor:[a-zA-Z:]+)=\"[^\"]*\"")]
	private static partial Regex BlazorAttribute();
}
