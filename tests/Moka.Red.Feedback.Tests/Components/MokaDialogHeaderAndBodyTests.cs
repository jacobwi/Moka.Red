using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Feedback.Dialog;
using Moka.Red.Icons;

namespace Moka.Red.Feedback.Tests.Components;

// The header took a plain string and nothing else, so an app that wanted an icon, its own title
// markup or a button up there wrapped every dialog in a component of its own. The body always had
// padding and its own scrollbar, and there was no size between Large and FullScreen.
public class MokaDialogHeaderAndBodyTests : BunitContext
{
	private const string DialogModule = "./_content/Moka.Red.Feedback/moka-dialog.js";
	private const string DragModule = "./_content/Moka.Red.Core/moka-drag.js";

	public MokaDialogHeaderAndBodyTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule(DialogModule);
		JSInterop.SetupModule(DragModule);
	}

	[Fact]
	public void TheHeaderTakesAnIcon()
	{
		IRenderedComponent<MokaDialog> cut = Open(p => p.Add(x => x.Icon, MokaIcons.Action.Save));

		Assert.NotNull(cut.Find(".moka-dialog-header .moka-dialog-icon"));
	}

	// TitleContent goes inside the heading, which is what names the dialog.
	[Fact]
	public void TitleContentNamesTheDialog()
	{
		IRenderedComponent<MokaDialog> cut = Open(p => p
			.Add(x => x.TitleContent, Title));

		IElement heading = cut.Find("h2.moka-dialog-title");
		Assert.Equal("Pane 2", heading.TextContent.Trim());
		Assert.Equal(heading.Id, cut.Find("[role=dialog]").GetAttribute("aria-labelledby"));
	}

	// Header buttons sit before the close button, which stays last.
	[Fact]
	public void HeaderActionsComeBeforeTheCloseButton()
	{
		IRenderedComponent<MokaDialog> cut = Open(p => p
			.Add(x => x.Title, "Logs")
			.Add(x => x.HeaderActions, PinButton));

		IElement header = cut.Find(".moka-dialog-header");
		List<string> order = header.Children.Select(child => child.ClassName ?? "").ToList();
		Assert.Equal(order.FindIndex(c => c.Contains("header-actions", StringComparison.Ordinal)) + 1,
			order.FindIndex(c => c.Contains("moka-dialog-close", StringComparison.Ordinal)));
	}

	// A header of only buttons still renders, and the dialog is not named by a heading that is not there.
	[Fact]
	public void AHeaderWithoutATitle_StillRenders()
	{
		IRenderedComponent<MokaDialog> cut = Open(p => p.Add(x => x.Icon, MokaIcons.Action.Save));

		Assert.NotNull(cut.Find(".moka-dialog-header"));
		Assert.Empty(cut.FindAll("h2.moka-dialog-title"));
		Assert.False(cut.Find("[role=dialog]").HasAttribute("aria-labelledby"));
	}

	[Fact]
	public void NoHeaderAtAllWhenThereIsNothingToPutInIt()
	{
		IRenderedComponent<MokaDialog> cut = Open(p => p.Add(x => x.ShowCloseButton, false));

		Assert.Empty(cut.FindAll(".moka-dialog-header"));
	}

	[Fact]
	public void TheBodyPadsAndScrollsByDefault()
	{
		IRenderedComponent<MokaDialog> cut = Open(_ => { });

		Assert.Equal(["moka-dialog-body"], cut.Find(".moka-dialog-body").ClassList);
	}

	[Theory]
	[InlineData(false, true, "moka-dialog-body--flush")]
	[InlineData(true, false, "moka-dialog-body--no-scroll")]
	public void TheBodyCanGiveUpItsPaddingAndItsScrollbar(bool padding, bool scroll, string expected)
	{
		IRenderedComponent<MokaDialog> cut = Open(p => p
			.Add(x => x.BodyPadding, padding)
			.Add(x => x.ScrollBody, scroll));

		Assert.Contains(expected, cut.Find(".moka-dialog-body").ClassList);
	}

	[Fact]
	public void TheExtraLargeSizeHasItsOwnClass()
	{
		IRenderedComponent<MokaDialog> cut = Open(p => p.Add(x => x.DialogSize, MokaDialogSize.ExtraLarge));

		Assert.Contains("moka-dialog--xl", cut.Find("[role=dialog]").ClassList);
	}

	// FullScreen keeps its value, so a size stored as a number still means what it did.
	[Fact]
	public void ExtraLargeWasAddedWithoutMovingFullScreen() =>
		Assert.Equal(3, (int)MokaDialogSize.FullScreen);

	[Fact]
	public void TheSizeCanBeGivenOutright()
	{
		IRenderedComponent<MokaDialog> cut = Open(p => p
			.Add(x => x.Width, "60rem")
			.Add(x => x.MaxWidth, "90vw")
			.Add(x => x.Height, "40rem"));

		string style = cut.Find("[role=dialog]").GetAttribute("style") ?? "";
		Assert.Contains("width: 60rem", style, StringComparison.Ordinal);
		Assert.Contains("max-width: 90vw", style, StringComparison.Ordinal);
		Assert.Contains("height: 40rem", style, StringComparison.Ordinal);
	}

	// Every string that reaches a style goes through StyleBuilder, which drops a value that could end
	// its own declaration.
	[Fact]
	public void AWidthThatCouldEndItsDeclaration_IsDropped()
	{
		IRenderedComponent<MokaDialog> cut = Open(p => p.Add(x => x.Width, "1px; position: fixed"));

		Assert.DoesNotContain("position: fixed", cut.Find("[role=dialog]").GetAttribute("style") ?? "",
			StringComparison.Ordinal);
	}

	private static void Title(RenderTreeBuilder builder) => builder.AddMarkupContent(0, "<span>Pane 2</span>");

	private static void PinButton(RenderTreeBuilder builder) =>
		builder.AddMarkupContent(0, "<button type=\"button\" id=\"pin\">Pin</button>");

	private IRenderedComponent<MokaDialog> Open(Action<ComponentParameterCollectionBuilder<MokaDialog>> parameters) =>
		Render<MokaDialog>(p =>
		{
			p.Add(x => x.Open, true);
			parameters(p);
		});
}
