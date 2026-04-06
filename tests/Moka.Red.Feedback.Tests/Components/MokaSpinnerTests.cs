using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Feedback.Loading;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaSpinnerTests : BunitContext
{
	[Fact]
	public void Renders_RoleStatus()
	{
		IRenderedComponent<MokaSpinner> cut = Render<MokaSpinner>();

		IElement el = cut.Find("[role='status']");
		Assert.NotNull(el);
	}

	[Fact]
	public void Renders_RootClass()
	{
		IRenderedComponent<MokaSpinner> cut = Render<MokaSpinner>();

		IElement el = cut.Find(".moka-spinner");
		Assert.NotNull(el);
	}

	[Fact]
	public void DefaultStyle_IsCircular()
	{
		IRenderedComponent<MokaSpinner> cut = Render<MokaSpinner>();

		IElement el = cut.Find(".moka-spinner");
		Assert.Contains("moka-spinner--circular", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void DotsStyle()
	{
		IRenderedComponent<MokaSpinner> cut = Render<MokaSpinner>(p => p
			.Add(x => x.SpinnerStyle, MokaSpinnerStyle.Dots));

		IElement el = cut.Find(".moka-spinner");
		Assert.Contains("moka-spinner--dots", el.ClassName, StringComparison.Ordinal);

		IReadOnlyList<IElement> dots = cut.FindAll(".moka-spinner-dot");
		Assert.Equal(3, dots.Count);
	}

	[Fact]
	public void Label_Renders()
	{
		IRenderedComponent<MokaSpinner> cut = Render<MokaSpinner>(p => p
			.Add(x => x.Label, "Loading..."));

		IElement label = cut.Find(".moka-spinner-label");
		Assert.Equal("Loading...", label.TextContent);
	}

	[Fact]
	public void NoLabel_NoLabelElement()
	{
		IRenderedComponent<MokaSpinner> cut = Render<MokaSpinner>();

		Assert.Empty(cut.FindAll(".moka-spinner-label"));
	}

	[Fact]
	public void DefaultAriaLabel_IsLoading()
	{
		IRenderedComponent<MokaSpinner> cut = Render<MokaSpinner>();

		IElement el = cut.Find("[role='status']");
		Assert.Equal("Loading", el.GetAttribute("aria-label"));
	}

	[Fact]
	public void CustomLabel_UsedAsAriaLabel()
	{
		IRenderedComponent<MokaSpinner> cut = Render<MokaSpinner>(p => p
			.Add(x => x.Label, "Saving..."));

		IElement el = cut.Find("[role='status']");
		Assert.Equal("Saving...", el.GetAttribute("aria-label"));
	}

	[Fact]
	public void Color_AppliedAsStyle()
	{
		IRenderedComponent<MokaSpinner> cut = Render<MokaSpinner>(p => p
			.Add(x => x.Color, MokaColor.Success));

		IElement el = cut.Find(".moka-spinner");
		string? style = el.GetAttribute("style");
		Assert.Contains("--moka-color-success", style, StringComparison.Ordinal);
	}
}
