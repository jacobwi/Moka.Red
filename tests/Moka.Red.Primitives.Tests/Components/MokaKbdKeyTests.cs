using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Primitives.Typography;

namespace Moka.Red.Primitives.Tests.Components;

// A shortcut written "Mod+K" means Ctrl+K, or Cmd+K on a Mac, and the key caps showed the word "Mod".
public class MokaKbdKeyTests : BunitContext
{
	[Fact]
	public void AKey_IsItsName()
	{
		IElement kbd = Render<MokaKbd>(p => p.Add(x => x.Key, "Esc")).Find("kbd");

		Assert.Equal("Esc", kbd.TextContent);
		Assert.Empty(kbd.Children);
	}

	[Theory]
	[InlineData("Mod")]
	[InlineData("mod")]
	public void TheModKey_ReadsCtrl_AndCarriesCmdForAnApplePlatform(string key)
	{
		IElement kbd = Render<MokaKbd>(p => p.Add(x => x.Key, key)).Find("kbd");

		Assert.Equal("Ctrl", kbd.QuerySelector(".moka-kbd-pc")!.TextContent);
		IElement apple = kbd.QuerySelector(".moka-kbd-apple")!;
		Assert.Equal("⌘", apple.QuerySelector("[aria-hidden='true']")!.TextContent);
		Assert.Equal("Command", apple.QuerySelector(".moka-visually-hidden")!.TextContent);
	}

	[Fact]
	public void TheKey_WinsOverChildContent()
	{
		IElement kbd = Render<MokaKbd>(p => p
			.Add(x => x.Key, "K")
			.Add(x => x.ChildContent, (RenderFragment)(b => b.AddContent(0, "ignored")))).Find("kbd");

		Assert.Equal("K", kbd.TextContent);
	}

	[Fact]
	public void ChildContent_RendersAsItDid()
	{
		IElement kbd = Render<MokaKbd>(p => p.AddChildContent("Enter")).Find("kbd");

		Assert.Equal("Enter", kbd.TextContent);
	}

	[Fact]
	public void TheCmdCap_ShowsOnlyOnAPageMarkedApple()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "MokaKbd.razor.css"));

		Assert.Contains(":root[data-moka-platform=\"apple\"] .moka-kbd-apple", css, StringComparison.Ordinal);
		Assert.Contains(":root[data-moka-platform=\"apple\"] .moka-kbd-pc", css, StringComparison.Ordinal);
	}
}
