using AngleSharp.Dom;
using Bunit;
using Moka.Red.Feedback.Loading;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaLoadingOverlayTests : BunitContext
{
	private const string Content = "<button id=\"save\">Save</button>";

	// The layer only stopped the mouse: Tab still reached the controls under it.
	[Fact]
	public void Loading_MakesTheContentInertAndBusy()
	{
		IRenderedComponent<MokaLoadingOverlay> cut = Render<MokaLoadingOverlay>(p => p
			.Add(x => x.Loading, true)
			.AddChildContent(Content));

		IElement content = cut.Find(".moka-loading-content");
		Assert.True(content.HasAttribute("inert"));
		Assert.Equal("true", content.GetAttribute("aria-busy"));
	}

	[Fact]
	public void Loaded_ContentIsReachableAgain()
	{
		IRenderedComponent<MokaLoadingOverlay> cut = Render<MokaLoadingOverlay>(p => p
			.Add(x => x.Loading, true)
			.AddChildContent(Content));

		cut.Render(p => p.Add(x => x.Loading, false));

		IElement content = cut.Find(".moka-loading-content");
		Assert.False(content.HasAttribute("inert"));
		Assert.Equal("false", content.GetAttribute("aria-busy"));
		Assert.Empty(cut.FindAll(".moka-loading-overlay"));
	}

	// The busy state belongs to the content being loaded. On the alert itself it told screen
	// readers to wait, so the alert could go unannounced.
	[Fact]
	public void TheAlert_IsNotMarkedBusy()
	{
		IRenderedComponent<MokaLoadingOverlay> cut = Render<MokaLoadingOverlay>(p => p
			.Add(x => x.Loading, true)
			.Add(x => x.Message, "Loading orders"));

		IElement alert = cut.Find("[role=alert]");
		Assert.False(alert.HasAttribute("aria-busy"));
		Assert.Equal("Loading orders", alert.GetAttribute("aria-label"));
	}

	// These shapes are sized in percentages of the indicator, which shrank to fit them and left
	// them no width. Only the circle, with its own size, showed.
	[Theory]
	[InlineData(MokaSkeletonShape.Text)]
	[InlineData(MokaSkeletonShape.Rectangle)]
	[InlineData(MokaSkeletonShape.Card)]
	public void PercentageSkeletons_SpanTheOverlay(MokaSkeletonShape shape)
	{
		IRenderedComponent<MokaLoadingOverlay> cut = Render<MokaLoadingOverlay>(p => p
			.Add(x => x.Loading, true)
			.Add(x => x.ShowSkeleton, true)
			.Add(x => x.SkeletonShape, shape));

		Assert.Contains("moka-loading-indicator--fill", cut.Find(".moka-loading-indicator").ClassList);
		Assert.Single(cut.FindAll(".moka-skeleton"));
	}

	[Fact]
	public void CircleSkeletonAndSpinner_StayCentred()
	{
		IRenderedComponent<MokaLoadingOverlay> cut = Render<MokaLoadingOverlay>(p => p
			.Add(x => x.Loading, true)
			.Add(x => x.ShowSkeleton, true)
			.Add(x => x.SkeletonShape, MokaSkeletonShape.Circle));
		Assert.DoesNotContain("moka-loading-indicator--fill", cut.Find(".moka-loading-indicator").ClassList);

		cut.Render(p => p.Add(x => x.ShowSkeleton, false));

		Assert.DoesNotContain("moka-loading-indicator--fill", cut.Find(".moka-loading-indicator").ClassList);
		Assert.Single(cut.FindAll(".moka-spinner"));
	}

	[Fact]
	public void Blur_BlursTheContentOnlyWhileLoading()
	{
		IRenderedComponent<MokaLoadingOverlay> cut = Render<MokaLoadingOverlay>(p => p
			.Add(x => x.Loading, true)
			.Add(x => x.Blur, true)
			.Add(x => x.BlurAmount, "6px"));

		IElement content = cut.Find(".moka-loading-content");
		Assert.Contains("moka-loading-content--blur", content.ClassList);
		Assert.Contains("filter: blur(6px)", content.GetAttribute("style"), StringComparison.Ordinal);

		cut.Render(p => p.Add(x => x.Loading, false));

		content = cut.Find(".moka-loading-content");
		Assert.DoesNotContain("moka-loading-content--blur", content.ClassList);
		Assert.False(content.HasAttribute("style"));
	}

	[Fact]
	public void FullScreen_FixesTheLayerToTheViewport()
	{
		IRenderedComponent<MokaLoadingOverlay> cut = Render<MokaLoadingOverlay>(p => p
			.Add(x => x.Loading, true)
			.Add(x => x.FullScreen, true));

		Assert.Contains("moka-loading-overlay--fullscreen", cut.Find(".moka-loading-overlay").ClassList);
	}

	// The overlay never changes Loading or Message itself, so change callbacks for them could
	// never be raised.
	[Theory]
	[InlineData("LoadingChanged")]
	[InlineData("MessageChanged")]
	public void HasNoChangeCallbacks(string name) =>
		Assert.Null(typeof(MokaLoadingOverlay).GetProperty(name));
}
