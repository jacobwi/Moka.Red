using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;
using static Moka.Red.ContextMenu.Tests.RadialDom;

namespace Moka.Red.ContextMenu.Tests.Components;

public class MokaRadialMenuLookTests : BunitContext
{
	public MokaRadialMenuLookTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule(Module);
	}

	[Theory]
	[InlineData(MokaRadialLook.Glass, "moka-radial-menu--glass")]
	[InlineData(MokaRadialLook.Solid, "moka-radial-menu--solid")]
	[InlineData(MokaRadialLook.Outline, "moka-radial-menu--outline")]
	[InlineData(MokaRadialLook.Terminal, "moka-radial-menu--terminal")]
	[InlineData(MokaRadialLook.Soft, "moka-radial-menu--soft")]
	public void ALook_IsAClassOnTheRing(MokaRadialLook look, string css)
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(look: look);

		Assert.Equal($"moka-radial-menu moka-radial-menu--success {css}", Ring(cut).ClassName);
	}

	[Fact]
	public void TheStandardLook_AddsNothing()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(look: MokaRadialLook.Standard);

		Assert.Equal("moka-radial-menu moka-radial-menu--success", Ring(cut).ClassName);
		Assert.Empty(cut.FindAll(".moka-radial-menu__text"));
	}

	[Fact]
	public void TheTerminalLook_WritesTheWordUnderEachIcon()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(look: MokaRadialLook.Terminal);

		Assert.Equal(["Open", "Limit", "Rename", "Allow", "Block", "Forget"],
			cut.FindAll(".moka-radial-menu__button > .moka-radial-menu__text").Select(t => t.TextContent));
		Assert.Equal(6, cut.FindAll(".moka-radial-menu__button > .moka-radial-menu__key").Count);
	}

	[Theory]
	[InlineData(MokaPopupLook.Glass, "moka-radial-menu--glass")]
	[InlineData(MokaPopupLook.Terminal, "moka-radial-menu--terminal")]
	[InlineData(MokaPopupLook.Standard, null)]
	[InlineData(MokaPopupLook.Elevated, null)]
	[InlineData(MokaPopupLook.Flat, null)]
	[InlineData(MokaPopupLook.AccentTint, null)]
	[InlineData(MokaPopupLook.Contrast, null)]
	public void LeftUnset_ItFollowsTheThemesPopups_WhereALookHasTheSameName(MokaPopupLook popups, string? css)
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(theme: MokaTheme.Default.WithPopups(popups));

		Assert.Equal(css is null ? "moka-radial-menu moka-radial-menu--success" : $"moka-radial-menu moka-radial-menu--success {css}",
			Ring(cut).ClassName);
	}

	[Fact]
	public void TheOriginalDesign_DoesNotFollowTheTheme()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(theme: MokaTheme.Dark with { Popups = MokaPopupLook.Glass });

		Assert.Equal("moka-radial-menu moka-radial-menu--success", Ring(cut).ClassName);
	}

	[Fact]
	public void ALookSetOnTheOriginalDesign_StillDraws()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(look: MokaRadialLook.Soft, theme: MokaTheme.Light);

		Assert.Contains("moka-radial-menu--soft", Ring(cut).ClassList);
	}

	[Fact]
	public void TheSpecsLook_WinsOverTheTheme_AndTheParameterOverTheSpec()
	{
		MokaTheme glass = MokaTheme.Default.WithPopups(MokaPopupLook.Glass);
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(Device() with { Look = MokaRadialLook.Solid }, theme: glass);
		Assert.Contains("moka-radial-menu--solid", Ring(cut).ClassList);
		Assert.DoesNotContain("moka-radial-menu--glass", Ring(cut).ClassList);

		cut.Render(p => p.Add(x => x.Look, MokaRadialLook.Outline));

		Assert.Contains("moka-radial-menu--outline", Ring(cut).ClassList);
		Assert.DoesNotContain("moka-radial-menu--solid", Ring(cut).ClassList);
	}

	[Fact]
	public void TheSpecCanAskForTheStandardLook_OverATerminalTheme()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(Device() with { Look = MokaRadialLook.Standard },
			theme: MokaTheme.Default.WithPopups(MokaPopupLook.Terminal));

		Assert.Equal("moka-radial-menu moka-radial-menu--success", Ring(cut).ClassName);
	}

	[Fact]
	public void ALookAndAShape_Combine()
	{
		IRenderedComponent<MokaRadialMenu> cut = RenderRing(Device() with { Shape = MokaRadialShape.Pie }, look: MokaRadialLook.Glass);

		IElement ring = Ring(cut);
		Assert.Contains("moka-radial-menu--pie", ring.ClassList);
		Assert.Contains("moka-radial-menu--glass", ring.ClassList);
	}

	private IRenderedComponent<MokaRadialMenu> RenderRing(MokaRadialMenuSpec? spec = null, MokaRadialLook? look = null,
		MokaTheme? theme = null) =>
		Render<MokaRadialMenu>(p =>
		{
			p.Add(m => m.Spec, spec ?? Device())
				.Add(m => m.Visible, true)
				.Add(m => m.X, 300)
				.Add(m => m.Y, 300)
				.Add(m => m.Look, look);
			if (theme is not null)
			{
				p.AddCascadingValue(theme);
			}
		});
}
