using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Layout.DockLayout;

namespace Moka.Red.Layout.Tests.Components;

// Today's one-panel-per-side markup still works: each MokaDockPanel with Dock is a group of one at that edge, around
// MokaDockContent. These tests change a panel's parameters through the layout's ChildContent, the way a parent does.
public class MokaDockLayoutTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Layout/DockLayout/MokaDockLayout.razor.js";

	public MokaDockLayoutTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule(Module).Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void APanelDockedLeft_IsATrackBesideTheContent()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec());

		Assert.Equal("250px var(--_dock-gutter) minmax(0px, 1fr)", Track(cut));
		Assert.Contains("moka-dock-panel--left", cut.Find("[data-moka-dock-group='left']").ClassList);
	}

	// Two panels on one side used to overlap in the same grid cell. They are tabs of one group now.
	[Fact]
	public void TwoPanelsOnOneSide_ShareAGroupAsTabs()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.AddChildContent(DockKit.Parts(
			DockKit.Panel("a", "Alpha", extra: Dock(MokaDockPosition.Left)),
			DockKit.Panel("b", "Beta", extra: Dock(MokaDockPosition.Left)),
			DockKit.Content())));

		IElement group = cut.Find("[data-moka-dock-group='left']");
		Assert.Equal(2, group.QuerySelectorAll("[role=tab]").Length);
		Assert.Equal("250px var(--_dock-gutter) minmax(0px, 1fr)", Track(cut));
		Assert.False(cut.Find("#a").HasAttribute("hidden"));
		Assert.True(cut.Find("#b").HasAttribute("hidden"));
	}

	[Fact]
	public void PanelsOnEverySide_SurroundTheContent()
	{
		IRenderedComponent<MokaDockLayout> cut = Render<MokaDockLayout>(p => p.AddChildContent(DockKit.Parts(
			DockKit.Panel("l", extra: Dock(MokaDockPosition.Left)),
			DockKit.Panel("r", extra: Dock(MokaDockPosition.Right)),
			DockKit.Panel("t", extra: Dock(MokaDockPosition.Top)),
			DockKit.Panel("b", extra: Dock(MokaDockPosition.Bottom)),
			DockKit.Content())));

		IElement row = cut.Find(".moka-dock-main > .moka-dock-split");
		Assert.Equal("row", row.GetAttribute("data-moka-dock-dir"));
		IElement column = row.QuerySelector(".moka-dock-split")!;
		Assert.Equal("column", column.GetAttribute("data-moka-dock-dir"));
		Assert.Equal("250px var(--_dock-gutter) minmax(0px, 1fr) var(--_dock-gutter) 250px",
			StyleValue(row, "grid-template-columns"));
		Assert.Equal("250px var(--_dock-gutter) minmax(0px, 1fr) var(--_dock-gutter) 250px",
			StyleValue(column, "grid-template-rows"));
	}

	[Fact]
	public void SizeFromParent_UpdatesTheTrack_AndBoundsClampIt()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { Size = "200px" });
		Assert.StartsWith("200px ", Track(cut), StringComparison.Ordinal);

		SetPanel(cut, new PanelSpec { Size = "320px" });
		Assert.StartsWith("320px ", Track(cut), StringComparison.Ordinal);

		SetPanel(cut, new PanelSpec { Size = "320px", MaxSize = "280px" });
		Assert.StartsWith("280px ", Track(cut), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("140px", "6rem", "40%", "clamp(6rem, 140px, 40%)")]
	[InlineData("140px", "6rem", null, "max(6rem, 140px)")]
	[InlineData("30%", null, "240px", "min(30%, 240px)")]
	[InlineData("320px", null, "280px", "280px")]
	public void Bounds_ApplyToTheTrack(string size, string? minSize, string? maxSize, string track)
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { Size = size, MinSize = minSize, MaxSize = maxSize });

		Assert.StartsWith(track + " ", Track(cut), StringComparison.Ordinal);
	}

	// SizeChanged was an EventCallback<double> beside a string Size, so @bind-Size didn't compile. Both are strings.
	[Fact]
	public async Task SizeChanged_ReportsTheDraggedSize_AsALength()
	{
		string? reported = null;
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec
		{
			SizeChanged = EventCallback.Factory.Create<string>(this, size => reported = size)
		});
		string split = cut.Find("[data-moka-dock-split]").GetAttribute("data-moka-dock-split")!;

		await cut.InvokeAsync(() => cut.Instance.OnSplitResized(split, [320, 700]));

		Assert.Equal("320px", reported);
		Assert.StartsWith("320px ", Track(cut), StringComparison.Ordinal);
	}

	[Fact]
	public void CollapsedFromParent_FoldsTheTrack_AndKeepsTheBodyMounted()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { Collapsed = false }, DockKit.Probe("p"));
		cut.Find(".probe").Click();
		DockProbe probe = cut.FindComponent<DockProbe>().Instance;

		SetPanel(cut, new PanelSpec { Collapsed = true }, DockKit.Probe("p"));

		Assert.StartsWith("0px ", Track(cut), StringComparison.Ordinal);
		Assert.True(cut.Find("#side").HasAttribute("hidden"));
		Assert.Same(probe, cut.FindComponent<DockProbe>().Instance);

		SetPanel(cut, new PanelSpec { Collapsed = false }, DockKit.Probe("p"));
		Assert.StartsWith("250px ", Track(cut), StringComparison.Ordinal);
		Assert.Equal("1", cut.Find(".probe").TextContent);
	}

	// Collapsed="@x" without CollapsedChanged: the parent keeps passing its old value, which must not undo a click.
	[Fact]
	public void CollapseButton_HoldsWhenAnUnboundParentRendersAgain()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { Collapsed = false });

		cut.Find("button[aria-label=Collapse]").Click();
		SetPanel(cut, new PanelSpec { Collapsed = false });

		Assert.StartsWith("0px ", Track(cut), StringComparison.Ordinal);

		SetPanel(cut, new PanelSpec { Collapsed = true });
		SetPanel(cut, new PanelSpec { Collapsed = false });
		Assert.StartsWith("250px ", Track(cut), StringComparison.Ordinal);
	}

	[Fact]
	public void CollapseButton_ReportsItsState()
	{
		bool? reported = null;
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec
		{
			CollapsedSize = "32px",
			CollapsedChanged = EventCallback.Factory.Create<bool>(this, value => reported = value)
		});
		Assert.Equal("true", cut.Find("button[aria-label=Collapse]").GetAttribute("aria-expanded"));

		cut.Find("button[aria-label=Collapse]").Click();

		Assert.True(reported);
		Assert.Equal("false", cut.Find("button[aria-label=Expand]").GetAttribute("aria-expanded"));
		Assert.Single(cut.FindAll("[data-moka-dock-group='left'] .moka-dock-panel-actions button"));
	}

	[Fact]
	public void FloatingFromParent_TakesThePanelOutOfTheTree_AndDocksItBackAtItsSize()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { Floating = false, Size = "300px" });

		SetPanel(cut, new PanelSpec { Floating = true, Size = "300px" });

		Assert.Empty(cut.FindAll("[data-moka-dock-split]"));
		IElement window = cut.Find("[data-moka-dock-float]");
		Assert.Contains("left: clamp(0px, 100px", window.GetAttribute("style"), StringComparison.Ordinal);

		SetPanel(cut, new PanelSpec { Floating = false, Size = "300px" });
		Assert.StartsWith("300px ", Track(cut), StringComparison.Ordinal);
	}

	[Fact]
	public void UndockButton_HoldsWhenAnUnboundParentRendersAgain()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { Floating = false });

		cut.Find("button[aria-label=Undock]").Click();
		SetPanel(cut, new PanelSpec { Floating = false });

		Assert.Single(cut.FindAll("[data-moka-dock-float]"));
	}

	// A floating panel's moves reached only private fields, and a resize was never reported. Both bind now.
	[Fact]
	public async Task FloatingWindowMoves_AreReportedBack()
	{
		(double X, double Y, string? W, string? H) reported = default;
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec
		{
			Floating = true,
			FloatingXChanged = EventCallback.Factory.Create<double>(this, x => reported.X = x),
			FloatingYChanged = EventCallback.Factory.Create<double>(this, y => reported.Y = y),
			FloatingWidthChanged = EventCallback.Factory.Create<string>(this, w => reported.W = w),
			FloatingHeightChanged = EventCallback.Factory.Create<string>(this, h => reported.H = h)
		});
		string group = cut.Find("[data-moka-dock-float]").GetAttribute("data-moka-dock-float")!;

		await cut.InvokeAsync(() => cut.Instance.OnFloatMoved(group, 400, 300, 500, 350));

		Assert.Equal((400d, 300d, "500px", "350px"), reported);
		Assert.Contains("width: min(500px, 100%)", cut.Find("[data-moka-dock-float]").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void FloatingFromParentPosition_MovesTheWindow()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { Floating = true, FloatingX = 80 });

		SetPanel(cut, new PanelSpec { Floating = true, FloatingX = 60 });

		Assert.Contains("left: clamp(0px, 60px", cut.Find("[data-moka-dock-float]").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("0px", true)]
	[InlineData("32px", false)]
	public void ACollapsedStrip_KeepsOnlyItsExpandButton(string collapsedSize, bool zero)
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { CollapsedSize = collapsedSize, Title = "Explorer" });

		cut.Find("button[aria-label=Collapse]").Click();

		Assert.StartsWith(collapsedSize + " ", Track(cut), StringComparison.Ordinal);
		Assert.Empty(cut.FindAll("button[aria-label=Undock]"));
		Assert.Single(cut.FindAll("button[aria-label=Expand]"));
		Assert.Equal(zero, Track(cut).StartsWith("0px", StringComparison.Ordinal));
	}

	[Fact]
	public void TheBody_ScrollsUnlessToldNotTo()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec());
		Assert.DoesNotContain("moka-dock-panel-body--no-scroll", cut.Find("#side").ClassList);
	}

	private IRenderedComponent<MokaDockLayout> RenderLayout(PanelSpec panel, RenderFragment? body = null) =>
		Render<MokaDockLayout>(p => p.AddChildContent(Tree(panel, body)));

	private static void SetPanel(IRenderedComponent<MokaDockLayout> cut, PanelSpec panel, RenderFragment? body = null) =>
		cut.Render(p => p.AddChildContent(Tree(panel, body)));

	// Parameters left null are not passed at all, so the panel keeps whatever it set itself.
	private static RenderFragment Tree(PanelSpec panel, RenderFragment? body) => builder =>
	{
		builder.OpenComponent<MokaDockPanel>(0);
		builder.AddComponentParameter(1, nameof(MokaDockPanel.Id), "side");
		builder.AddComponentParameter(2, nameof(MokaDockPanel.Dock), panel.Dock);
		builder.AddComponentParameter(3, nameof(MokaDockPanel.Size), panel.Size);
		builder.AddComponentParameter(4, nameof(MokaDockPanel.Collapsible), true);
		Optional(builder, 5, nameof(MokaDockPanel.Collapsed), panel.Collapsed);
		Optional(builder, 6, nameof(MokaDockPanel.Floating), panel.Floating);
		Optional(builder, 7, nameof(MokaDockPanel.MaxSize), panel.MaxSize);
		Optional(builder, 8, nameof(MokaDockPanel.MinSize), panel.MinSize);
		Optional(builder, 9, nameof(MokaDockPanel.Title), panel.Title);
		Optional(builder, 10, nameof(MokaDockPanel.CollapsedSize), panel.CollapsedSize);
		Optional(builder, 11, nameof(MokaDockPanel.FloatingX), panel.FloatingX);
		Optional(builder, 12, nameof(MokaDockPanel.SizeChanged), panel.SizeChanged);
		Optional(builder, 13, nameof(MokaDockPanel.CollapsedChanged), panel.CollapsedChanged);
		Optional(builder, 14, nameof(MokaDockPanel.FloatingXChanged), panel.FloatingXChanged);
		Optional(builder, 15, nameof(MokaDockPanel.FloatingYChanged), panel.FloatingYChanged);
		Optional(builder, 16, nameof(MokaDockPanel.FloatingWidthChanged), panel.FloatingWidthChanged);
		Optional(builder, 17, nameof(MokaDockPanel.FloatingHeightChanged), panel.FloatingHeightChanged);
		builder.AddComponentParameter(18, nameof(MokaDockPanel.ChildContent), body ?? DockKit.Text("panel body"));
		builder.CloseComponent();

		builder.OpenComponent<MokaDockContent>(19);
		builder.CloseComponent();
	};

	private static void Optional(RenderTreeBuilder builder, int sequence, string name, object? value)
	{
		if (value is not null)
		{
			builder.AddComponentParameter(sequence, name, value);
		}
	}

	private static Dictionary<string, object?> Dock(MokaDockPosition side) => new() { [nameof(MokaDockPanel.Dock)] = side };

	private static string Track(IRenderedComponent<MokaDockLayout> cut) =>
		StyleValue(cut.Find(".moka-dock-main > .moka-dock-split"), "grid-template-columns") ?? "";

	internal static string? StyleValue(IElement element, string property)
	{
		string style = element.GetAttribute("style") ?? "";
		foreach (string declaration in style.Split(';'))
		{
			int colon = declaration.IndexOf(':', StringComparison.Ordinal);
			if (colon > 0 && string.Equals(declaration[..colon].Trim(), property, StringComparison.Ordinal))
			{
				return declaration[(colon + 1)..].Trim();
			}
		}

		return null;
	}

	private sealed record PanelSpec
	{
		public MokaDockPosition Dock { get; init; } = MokaDockPosition.Left;
		public string? Size { get; init; } = "250px";
		public string? MinSize { get; init; }
		public string? MaxSize { get; init; }
		public bool? Collapsed { get; init; }
		public string? CollapsedSize { get; init; }
		public bool? Floating { get; init; }
		public double? FloatingX { get; init; }
		public string? Title { get; init; }
		public EventCallback<string>? SizeChanged { get; init; }
		public EventCallback<bool>? CollapsedChanged { get; init; }
		public EventCallback<double>? FloatingXChanged { get; init; }
		public EventCallback<double>? FloatingYChanged { get; init; }
		public EventCallback<string>? FloatingWidthChanged { get; init; }
		public EventCallback<string>? FloatingHeightChanged { get; init; }
	}
}
