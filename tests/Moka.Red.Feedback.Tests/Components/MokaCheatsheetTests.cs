using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Feedback.Cheatsheet;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaCheatsheetTests : BunitContext
{
	private static IReadOnlyList<MokaCheatsheetGroup> SampleGroups =>
	[
		new("General",
		[
			new MokaCheatsheetItem("Command palette", ["Ctrl", "K"]),
			new MokaCheatsheetItem("Close", ["Esc"])
		]),
		new("Create", [new MokaCheatsheetItem("New", ["Ctrl", "N"])])
	];

	[Fact]
	public void Closed_RendersNothing()
	{
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p
			.Add(x => x.Open, false)
			.Add(x => x.Groups, SampleGroups));

		Assert.Empty(cut.FindAll(".moka-cheatsheet"));
	}

	[Fact]
	public void Open_RendersBackdropAndDialog()
	{
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Groups, SampleGroups));

		Assert.NotNull(cut.Find(".moka-cheatsheet-backdrop"));
		IElement dialog = cut.Find(".moka-cheatsheet");
		Assert.Equal("dialog", dialog.GetAttribute("role"));
		Assert.Equal("true", dialog.GetAttribute("aria-modal"));
	}

	[Fact]
	public void Title_Default()
	{
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p
			.Add(x => x.Open, true));

		Assert.Equal("Keyboard Shortcuts", cut.Find(".moka-cheatsheet-title").TextContent);
	}

	[Fact]
	public void Groups_RenderWithRowsAndKbd()
	{
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Groups, SampleGroups));

		Assert.Equal(2, cut.FindAll(".moka-cheatsheet-group").Count);
		Assert.Equal(3, cut.FindAll(".moka-cheatsheet-row").Count);
		// Ctrl+K, Esc, Ctrl+N = 5 kbd chips
		Assert.Equal(5, cut.FindAll(".moka-cheatsheet-keys .moka-kbd").Count);
	}

	[Fact]
	public void FooterContent_Renders()
	{
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.FooterContent, "<span id=\"foot\">hint</span>"));

		Assert.NotNull(cut.Find(".moka-cheatsheet-footer").QuerySelector("#foot"));
	}

	[Fact]
	public void CloseButton_InvokesOpenChanged()
	{
		var newState = true;
		IRenderedComponent<MokaCheatsheet> cut = Render<MokaCheatsheet>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.OpenChanged, EventCallback.Factory.Create<bool>(this, v => newState = v)));

		cut.Find(".moka-cheatsheet-close").Click();

		Assert.False(newState);
	}
}
