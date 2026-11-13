using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Navigation.Breadcrumb;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Navigation.Tests.Components;

// A trail always wrapped, and a path was always 34 px tall, so neither fit a row of small controls. Wrap="false" keeps a
// trail on one line with its end in view, and a path's height follows Size on the new design. Today's looks stay the
// defaults.
public class MokaBreadcrumbLineAndSizeTests : BunitContext
{
	private const string Css = "MokaBreadcrumb.razor.css";
	private const string SmallField = "calc(var(--moka-control-height) - var(--moka-spacing-md))";

	public MokaBreadcrumbLineAndSizeTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ATrail_WrapsByDefault_WithItsListStraightInTheNav()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Trail();

		Assert.True(cut.Instance.Wrap);
		Assert.Equal("moka-breadcrumb moka-breadcrumb--trail moka-breadcrumb--has-back", cut.Find("nav").ClassName);
		Assert.NotNull(cut.Find("nav > ol.moka-breadcrumb__list"));
		Assert.Empty(cut.FindAll(".moka-breadcrumb__scroll"));
	}

	[Fact]
	public void NoWrap_PutsTheTrailInTheScrollerAPathUses()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Trail(p => p.Add(x => x.Wrap, false));

		IElement nav = cut.Find("nav");
		Assert.Contains("moka-breadcrumb--trail", nav.ClassList);
		Assert.Contains("moka-breadcrumb--one-line", nav.ClassList);
		Assert.NotNull(cut.Find("nav > .moka-breadcrumb__scroll.moka-hide-scrollbar > ol.moka-breadcrumb__list"));
		Assert.Empty(cut.FindAll("nav > ol"));
	}

	[Fact]
	public void NoWrap_KeepsTheLevelsInOrder_AndTheCurrentPageLast()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Trail(p => p.Add(x => x.Wrap, false));

		IReadOnlyList<IElement> levels = cut.FindAll(".moka-breadcrumb__scroll li.moka-breadcrumb-item:not([aria-hidden])");
		Assert.Equal(["Home", "Network", "Devices"],
			levels.Select(level => level.QuerySelector(".moka-breadcrumb-item__link, .moka-breadcrumb-item__text")!.TextContent.Trim()));
		Assert.Equal("page", levels[^1].QuerySelector("[aria-current]")?.GetAttribute("aria-current"));
	}

	// A phone-width box still trades the trail for a link to the parent.
	[Fact]
	public void NoWrap_KeepsTheBackLink()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Trail(p => p.Add(x => x.Wrap, false));

		Assert.Equal("Network", cut.Find("nav > a.moka-breadcrumb__back").TextContent.Trim());
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-gate .moka-breadcrumb--has-back > .moka-breadcrumb__scroll")["display"]);
	}

	// A path scrolls already, so Wrap means nothing to it.
	[Fact]
	public void NoWrap_ChangesNothingOnAPath()
	{
		IRenderedComponent<MokaBreadcrumb> cut = Trail(p => p.Add(x => x.Look, MokaBreadcrumbLook.Path).Add(x => x.Wrap, false));

		Assert.Equal("moka-breadcrumb moka-breadcrumb--path", cut.Find("nav").ClassName);
	}

	[Fact]
	public void TheOneLineScroller_GivesBackTheRoomItKeepsForAFocusRing()
	{
		Assert.Equal("calc(-1 * var(--moka-spacing-xs))",
			ScopedCss.Declarations(Css, ".moka-breadcrumb--one-line > .moka-breadcrumb__scroll")["margin"]);
		Assert.Equal("var(--moka-spacing-xs)", ScopedCss.Declarations(Css, ".moka-breadcrumb__scroll")["padding"]);
		Assert.Equal("var(--moka-spacing-xs)",
			ScopedCss.Declarations(Css, ".moka-breadcrumb--one-line > .moka-breadcrumb__scroll > .moka-breadcrumb__list")["gap"]);
	}

	[Theory]
	[InlineData(MokaSize.Xs, "moka-breadcrumb--xs")]
	[InlineData(MokaSize.Sm, "moka-breadcrumb--sm")]
	[InlineData(MokaSize.Lg, "moka-breadcrumb--lg")]
	public void APath_CarriesItsSize(MokaSize size, string expected)
	{
		IRenderedComponent<MokaBreadcrumb> cut = Trail(p => p.Add(x => x.Look, MokaBreadcrumbLook.Path).Add(x => x.Size, size));

		Assert.Contains(expected, cut.Find("nav").ClassList);
	}

	[Fact]
	public void AMediumPath_AndATrail_WriteNoSize()
	{
		IRenderedComponent<MokaBreadcrumb> path = Trail(p => p.Add(x => x.Look, MokaBreadcrumbLook.Path));
		IRenderedComponent<MokaBreadcrumb> trail = Trail(p => p.Add(x => x.Size, MokaSize.Sm));

		Assert.Equal("moka-breadcrumb moka-breadcrumb--path", path.Find("nav").ClassName);
		Assert.Equal("moka-breadcrumb moka-breadcrumb--trail moka-breadcrumb--has-back", trail.Find("nav").ClassName);
	}

	[Fact]
	public void ASmallPath_IsAsTallAsASmallField_OnTheNewDesign()
	{
		IReadOnlyDictionary<string, string> small = ScopedCss.Declarations(Css, ".moka-gate .moka-breadcrumb--path.moka-breadcrumb--sm");

		Assert.Equal(SmallField, small["min-height"]);
		Assert.Equal("var(--moka-line-height-tight)", small["line-height"]);
		Assert.Equal("0", ScopedCss.Declarations(Css, ".moka-gate .moka-breadcrumb--path.moka-breadcrumb--sm ::deep .moka-breadcrumb-item__link")["padding-block"]);
		Assert.Equal("calc(var(--moka-control-height) + var(--moka-spacing-md))",
			ScopedCss.Declarations(Css, ".moka-gate .moka-breadcrumb--path.moka-breadcrumb--lg")["min-height"]);
	}

	// The original design and a medium path keep the height a path has always had.
	[Fact]
	public void APath_KeepsItsHeight_Otherwise()
	{
		Assert.Equal("2.125rem", ScopedCss.Declarations(Css, ".moka-breadcrumb--path")["min-height"]);
		Assert.False(ScopedCss.Declarations(Css, ".moka-breadcrumb--path.moka-breadcrumb--sm").ContainsKey("min-height"));
	}

	private IRenderedComponent<MokaBreadcrumb> Trail(Action<ComponentParameterCollectionBuilder<MokaBreadcrumb>>? extra = null) =>
		Render<MokaBreadcrumb>(p =>
		{
			extra?.Invoke(p);
			p.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "Home").Add(x => x.Href, "/"))
				.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "Network").Add(x => x.Href, "/network"))
				.AddChildContent<MokaBreadcrumbItem>(item => item.Add(x => x.Text, "Devices"));
		});
}
