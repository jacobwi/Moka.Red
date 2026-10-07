using AngleSharp.Dom;
using Bunit;
using Moka.Red.Forms.SearchInput;

namespace Moka.Red.Forms.Tests.Components;

// The "/" hint was a span styled to look like a key, drawn from the search input's own CSS. It is a
// MokaKbd now, so it is the same cap the command palette and the context menu draw.
public class MokaSearchInputShortcutTests : BunitContext
{
	[Fact]
	public void TheHintIsAKeyCap()
	{
		IRenderedComponent<MokaSearchInput> cut = Render<MokaSearchInput>(p => p.Add(x => x.ShowShortcut, true));

		IElement cap = cut.Find("kbd");
		Assert.Equal("/", cap.TextContent.Trim());
		Assert.Contains("moka-search-shortcut", cap.ClassList);
		// Decorative: the input itself is what a screen reader announces.
		Assert.Equal("true", cap.GetAttribute("aria-hidden"));
	}

	[Fact]
	public void NoHintWithoutShowShortcut()
	{
		IRenderedComponent<MokaSearchInput> cut = Render<MokaSearchInput>();

		Assert.Empty(cut.FindAll("kbd"));
	}

	// The hint gives way to the clear button once there is something to clear.
	[Fact]
	public void TypingReplacesTheHint()
	{
		IRenderedComponent<MokaSearchInput> cut = Render<MokaSearchInput>(p => p
			.Add(x => x.ShowShortcut, true)
			.Add(x => x.Clearable, true)
			.Add(x => x.Value, "report"));

		Assert.Empty(cut.FindAll("kbd"));
		Assert.NotNull(cut.Find(".moka-search-clear"));
	}
}
