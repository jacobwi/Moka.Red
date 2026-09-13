using AngleSharp.Dom;
using Bunit;
using Moka.Red.Feedback.BootScreen;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaBootScreenTests : BunitContext
{
	[Fact]
	public void Visible_Default_RendersRoot()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>();

		IElement root = cut.Find(".moka-boot-screen");
		Assert.Equal("status", root.GetAttribute("role"));
	}

	[Fact]
	public void Visible_False_RendersNothing()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.Visible, false));

		Assert.Empty(cut.FindAll(".moka-boot-screen"));
	}

	[Fact]
	public void Brand_Renders()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.Brand, "MOKA"));

		Assert.Contains("MOKA", cut.Find(".moka-boot-screen-mark").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void BrandSecondary_RendersDotSeparator()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.Brand, "MOKA")
			.Add(x => x.BrandSecondary, "RED"));

		Assert.NotNull(cut.Find(".moka-boot-screen-dot"));
		Assert.Contains("RED", cut.Find(".moka-boot-screen-mark").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void StatusText_RendersSubtitle()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.StatusText, "booting"));

		Assert.Contains("booting", cut.Find(".moka-boot-screen-subtitle").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void ChipsContent_Renders()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.ChipsContent, "<span id=\"chip\">engine</span>"));

		Assert.NotNull(cut.Find(".moka-boot-screen-chips").QuerySelector("#chip"));
	}

	[Fact]
	public void ShowBar_Default_RendersBar()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>();

		Assert.NotNull(cut.Find(".moka-boot-screen-bar"));
	}

	[Fact]
	public void ShowBar_False_NoBar()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.ShowBar, false));

		Assert.Empty(cut.FindAll(".moka-boot-screen-bar"));
	}

	[Fact]
	public void Default_HasGridAndScanline_NotEmbedded()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>();

		string cls = cut.Find(".moka-boot-screen").ClassName!;
		Assert.Contains("moka-boot-screen--grid", cls, StringComparison.Ordinal);
		Assert.Contains("moka-boot-screen--scanline", cls, StringComparison.Ordinal);
		Assert.DoesNotContain("moka-boot-screen--embedded", cls, StringComparison.Ordinal);
	}

	[Fact]
	public void FullScreen_False_AddsEmbedded()
	{
		IRenderedComponent<MokaBootScreen> cut = Render<MokaBootScreen>(p => p
			.Add(x => x.FullScreen, false));

		Assert.Contains("moka-boot-screen--embedded", cut.Find(".moka-boot-screen").ClassName, StringComparison.Ordinal);
	}
}
