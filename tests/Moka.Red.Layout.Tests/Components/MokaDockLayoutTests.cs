using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Layout.DockLayout;

namespace Moka.Red.Layout.Tests.Components;

// The layout renders its grid before the panels inside it receive their parameters, so these tests
// change a panel's parameters through the layout's ChildContent, the way a parent component does,
// and check the grid straight after that one render.
public class MokaDockLayoutTests : BunitContext
{
	private const string DragModule = "./_content/Moka.Red.Core/moka-drag.js";

	private readonly BunitJSModuleInterop _drag;

	public MokaDockLayoutTests()
	{
		_drag = JSInterop.SetupModule(DragModule);
		_drag.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void CollapsedFromParent_UpdatesTheGridInTheSameRender()
	{
		IRenderedComponent<CountingLayout> cut = RenderLayout<CountingLayout>(new PanelSpec { Collapsed = false });
		Assert.Equal("250px 1fr", GridValue(cut, "grid-template-columns"));
		int renders = cut.Instance.Renders;

		SetPanel(cut, new PanelSpec { Collapsed = true });

		Assert.Equal("0px 1fr", GridValue(cut, "grid-template-columns"));
		// One render for the new parameters and one to apply the panel's change, then it settles.
		Assert.Equal(renders + 2, cut.Instance.Renders);

		renders = cut.Instance.Renders;
		SetPanel(cut, new PanelSpec { Collapsed = true });
		Assert.Equal(renders + 1, cut.Instance.Renders);

		SetPanel(cut, new PanelSpec { Collapsed = false });
		Assert.Equal("250px 1fr", GridValue(cut, "grid-template-columns"));
	}

	[Fact]
	public void SizeFromParent_UpdatesTheGridInTheSameRender()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { Size = "200px" });
		Assert.Equal("200px 1fr", GridValue(cut, "grid-template-columns"));

		SetPanel(cut, new PanelSpec { Size = "320px" });
		Assert.Equal("320px 1fr", GridValue(cut, "grid-template-columns"));

		// New bounds clamp the current size.
		SetPanel(cut, new PanelSpec { Size = "320px", MaxSize = "280px" });
		Assert.Equal("280px 1fr", GridValue(cut, "grid-template-columns"));
	}

	[Fact]
	public void DockFromParent_MovesTheTrackInTheSameRender()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec());
		Assert.Equal("'left content'", GridValue(cut, "grid-template-areas"));

		SetPanel(cut, new PanelSpec { Dock = MokaDockPosition.Right });

		Assert.Equal("1fr 250px", GridValue(cut, "grid-template-columns"));
		Assert.Equal("'content right'", GridValue(cut, "grid-template-areas"));
	}

	[Fact]
	public void FloatingFromParent_TakesThePanelOutOfTheGridInTheSameRender()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { Floating = false });

		SetPanel(cut, new PanelSpec { Floating = true });

		Assert.Equal("1fr", GridValue(cut, "grid-template-columns"));
		Assert.Equal("'content'", GridValue(cut, "grid-template-areas"));
	}

	// A collapsed panel has no splitter, so expanding creates a new element that needs its own
	// listener. The flag the panel kept used to say "attached" and the new splitter never worked.
	[Fact]
	public void ExpandFromParent_AttachesTheNewSplitter()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { Collapsed = false });
		Assert.Single(_drag.Invocations["makeResizable"]);

		SetPanel(cut, new PanelSpec { Collapsed = true });
		Assert.Empty(cut.FindAll(".moka-dock-panel-splitter"));

		SetPanel(cut, new PanelSpec { Collapsed = false });

		Assert.Single(cut.FindAll(".moka-dock-panel-splitter"));
		IReadOnlyList<JSRuntimeInvocation> attached = _drag.Invocations["makeResizable"];
		Assert.Equal(2, attached.Count);
		ElementReference first = Assert.IsType<ElementReference>(attached[0].Arguments[2]);
		ElementReference second = Assert.IsType<ElementReference>(attached[1].Arguments[2]);
		Assert.NotEqual(first.Id, second.Id);
	}

	// The header drag belongs to floating panels. Left on a docked panel, dragging its header
	// would move it out of its grid cell.
	[Fact]
	public void DockFromParent_RemovesTheHeaderDrag()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { Floating = true, Title = "Inspector" });
		JSRuntimeInvocation drag = Assert.Single(_drag.Invocations["makeDraggable"]);

		SetPanel(cut, new PanelSpec { Floating = false, Title = "Inspector" });

		JSRuntimeInvocation removed = Assert.Single(_drag.Invocations["removeDraggable"]);
		Assert.Equal(
			Assert.IsType<ElementReference>(drag.Arguments[2]).Id,
			Assert.IsType<ElementReference>(removed.Arguments[0]).Id);
		Assert.Single(_drag.Invocations["makeResizable"]);
	}

	[Fact]
	public void Collapse_HidesTheBodyAndKeepsChildStateMounted()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec(), StatefulChild);
		cut.Find(".probe").Click();
		StatefulProbe probe = cut.FindComponent<StatefulProbe>().Instance;

		cut.Find(".moka-dock-panel-toggle[title=Collapse]").Click();

		IElement body = cut.Find(".moka-dock-panel-body");
		Assert.True(body.HasAttribute("hidden"));
		Assert.NotNull(body.QuerySelector(".probe"));
		Assert.Same(probe, cut.FindComponent<StatefulProbe>().Instance);
		Assert.Equal("0px 1fr", GridValue(cut, "grid-template-columns"));

		cut.Find(".moka-dock-panel-toggle[title=Expand]").Click();

		Assert.False(cut.Find(".moka-dock-panel-body").HasAttribute("hidden"));
		Assert.Same(probe, cut.FindComponent<StatefulProbe>().Instance);
		Assert.Equal("1", cut.Find(".probe").TextContent);
	}

	// Collapsed="@x" without a CollapsedChanged handler: the parent keeps passing its old value.
	// That used to write the button's state back over, so the collapse button seemed to do nothing.
	[Fact]
	public void CollapseButton_HoldsWhenAnUnboundParentRendersAgain()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { Collapsed = false });

		cut.Find(".moka-dock-panel-toggle[title=Collapse]").Click();
		SetPanel(cut, new PanelSpec { Collapsed = false });

		Assert.Equal("0px 1fr", GridValue(cut, "grid-template-columns"));
		Assert.True(cut.Find(".moka-dock-panel-body").HasAttribute("hidden"));

		// A value the parent really changes still wins.
		SetPanel(cut, new PanelSpec { Collapsed = true });
		SetPanel(cut, new PanelSpec { Collapsed = false });
		Assert.Equal("250px 1fr", GridValue(cut, "grid-template-columns"));
	}

	[Fact]
	public void UndockButton_HoldsWhenAnUnboundParentRendersAgain()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { Floating = false, Title = "Inspector" });

		cut.Find(".moka-dock-panel-toggle[title=Undock]").Click();
		SetPanel(cut, new PanelSpec { Floating = false, Title = "Inspector" });

		Assert.Equal("1fr", GridValue(cut, "grid-template-columns"));
		Assert.Contains("moka-dock-panel--floating", cut.Find(".moka-dock-panel").ClassList);
	}

	[Fact]
	public async Task HeaderDrag_KeepsItsPositionWhenTheParentRepeatsTheOldOne()
	{
		PanelSpec floating = new() { Floating = true, Title = "Inspector", FloatingX = 80, FloatingY = 120 };
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(floating);
		MokaDockPanel panel = cut.FindComponent<MokaDockPanel>().Instance;

		await cut.InvokeAsync(() => panel.OnFloatingMoved(400, 300));
		SetPanel(cut, floating);

		Assert.Equal("400px", StyleValue(cut.Find(".moka-dock-panel"), "left"));
		Assert.Equal("300px", StyleValue(cut.Find(".moka-dock-panel"), "top"));

		SetPanel(cut, floating with { FloatingX = 60 });
		Assert.Equal("60px", StyleValue(cut.Find(".moka-dock-panel"), "left"));
	}

	// The header button used to reset the size to Size when docking again, while a parent docking
	// the panel kept the dragged size. Both keep it now.
	[Fact]
	public async Task Redocking_KeepsTheDraggedSize_FromTheButtonAndFromTheParent()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { Title = "Inspector" });
		MokaDockPanel panel = cut.FindComponent<MokaDockPanel>().Instance;
		await cut.InvokeAsync(() => panel.OnResized(320));
		Assert.Equal("320px 1fr", GridValue(cut, "grid-template-columns"));

		await cut.Find(".moka-dock-panel-toggle[title=Undock]").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-dock-panel-toggle[title=Dock]").ClickAsync(new MouseEventArgs());
		Assert.Equal("320px 1fr", GridValue(cut, "grid-template-columns"));

		SetPanel(cut, new PanelSpec { Title = "Inspector", Floating = true });
		SetPanel(cut, new PanelSpec { Title = "Inspector", Floating = false });
		Assert.Equal("320px 1fr", GridValue(cut, "grid-template-columns"));
	}

	[Fact]
	public void CollapseButton_ReportsItsStateInAriaExpanded()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { CollapsedSize = "32px" });
		Assert.Equal("true", cut.Find(".moka-dock-panel-toggle[title=Collapse]").GetAttribute("aria-expanded"));

		cut.Find(".moka-dock-panel-toggle[title=Collapse]").Click();

		Assert.Equal("false", cut.Find(".moka-dock-panel-toggle[title=Expand]").GetAttribute("aria-expanded"));
	}

	// Collapsed to nothing, the header is clipped out of sight but its buttons could still be
	// tabbed to. A strip with a size shows its buttons, so it stays usable.
	[Theory]
	[InlineData("0px", true)]
	[InlineData("0", true)]
	[InlineData("0.0rem", true)]
	[InlineData("32px", false)]
	[InlineData("calc(0px + 2rem)", false)]
	public void CollapsedPanel_IsInertOnlyAtZeroSize(string collapsedSize, bool inert)
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { CollapsedSize = collapsedSize });
		Assert.False(cut.Find(".moka-dock-panel").HasAttribute("inert"));

		cut.Find(".moka-dock-panel-toggle[title=Collapse]").Click();

		Assert.Equal(inert, cut.Find(".moka-dock-panel").HasAttribute("inert"));
	}

	// Collapsed to a 32px strip, the header row ran past the strip's edge: the expand button was out
	// of sight but still in the tab order, and so were the undock button and the consumer's actions.
	// The strip keeps the expand button, and the actions stay mounted until it expands again.
	[Fact]
	public void CollapsedStrip_KeepsOnlyTheExpandButton_AndExpandingBringsTheRestBack()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec
		{
			CollapsedSize = "32px",
			Title = "Explorer",
			Actions = StatefulChild
		});
		cut.Find(".probe").Click();
		StatefulProbe action = cut.FindComponent<StatefulProbe>().Instance;

		cut.Find(".moka-dock-panel-toggle[title=Collapse]").Click();

		Assert.False(cut.Find(".moka-dock-panel").HasAttribute("inert"));
		Assert.Null(cut.Find(".moka-dock-panel-toggle[title=Expand]").Closest("[hidden]"));
		Assert.NotNull(cut.Find(".probe").Closest("[hidden]"));
		Assert.Empty(cut.FindAll(".moka-dock-panel-toggle[title=Undock]"));

		cut.Find(".moka-dock-panel-toggle[title=Expand]").Click();

		Assert.Null(cut.Find(".probe").Closest("[hidden]"));
		Assert.Single(cut.FindAll(".moka-dock-panel-toggle[title=Undock]"));
		Assert.Same(action, cut.FindComponent<StatefulProbe>().Instance);
		Assert.Equal("1", cut.Find(".probe").TextContent);
	}

	// An empty CollapsedSize used to drop the panel's track, so the areas no longer lined up with
	// the tracks and the content fell into the panel's column.
	[Fact]
	public void EmptyCollapsedSize_KeepsAZeroTrack()
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { CollapsedSize = "", Collapsed = true });

		Assert.Equal("0px 1fr", GridValue(cut, "grid-template-columns"));
		Assert.True(cut.Find(".moka-dock-panel").HasAttribute("inert"));
	}

	// MinSize and MaxSize used to go on the panel element as min-width and max-width. A percentage
	// there resolves against the panel's own grid area, so MaxSize="40%" shrank the panel inside its
	// track, and a bound the track broke left a gap or an overlap. They bound the track now. px on a
	// px size is still clamped up front, and a size that is not a length is left alone, since min(),
	// max() and clamp() would make the template invalid.
	[Theory]
	[InlineData("140px", "6rem", "40%", "clamp(6rem, 140px, 40%) 1fr")]
	[InlineData("140px", "6rem", null, "max(6rem, 140px) 1fr")]
	[InlineData("30%", null, "240px", "min(30%, 240px) 1fr")]
	[InlineData("320px", null, "280px", "280px 1fr")]
	[InlineData("minmax(200px, 30%)", "6rem", null, "minmax(200px, 30%) 1fr")]
	public void Bounds_ApplyToTheTrack_NotThePanel(string size, string? minSize, string? maxSize, string columns)
	{
		IRenderedComponent<MokaDockLayout> cut = RenderLayout(new PanelSpec { Size = size, MinSize = minSize, MaxSize = maxSize });

		Assert.Equal(columns, GridValue(cut, "grid-template-columns"));
		IElement panel = cut.Find(".moka-dock-panel");
		Assert.Null(StyleValue(panel, "min-width"));
		Assert.Null(StyleValue(panel, "max-width"));
	}

	// The same makeResizable serves MokaResizable, which must size its own element, so the dock
	// asks for track resizing by name instead of relying on the default.
	[Fact]
	public void Splitter_AsksForTrackResizing()
	{
		RenderLayout(new PanelSpec());

		JSRuntimeInvocation attach = Assert.Single(_drag.Invocations["makeResizable"]);
		Assert.Contains("\"target\":\"track\"", JsonSerializer.Serialize(attach.Arguments[3]), StringComparison.Ordinal);
	}

	private IRenderedComponent<MokaDockLayout> RenderLayout(PanelSpec panel, RenderFragment? body = null) =>
		RenderLayout<MokaDockLayout>(panel, body);

	private IRenderedComponent<TLayout> RenderLayout<TLayout>(PanelSpec panel, RenderFragment? body = null)
		where TLayout : MokaDockLayout =>
		Render<TLayout>(p => p.AddChildContent(Tree(panel, body)));

	private static void SetPanel<TLayout>(IRenderedComponent<TLayout> cut, PanelSpec panel, RenderFragment? body = null)
		where TLayout : MokaDockLayout =>
		cut.Render(p => p.AddChildContent(Tree(panel, body)));

	// Parameters left null are not passed at all, so the panel keeps whatever it set itself.
	private static RenderFragment Tree(PanelSpec panel, RenderFragment? body) => builder =>
	{
		builder.OpenComponent<MokaDockPanel>(0);
		builder.AddComponentParameter(1, nameof(MokaDockPanel.Dock), panel.Dock);
		builder.AddComponentParameter(2, nameof(MokaDockPanel.Size), panel.Size);
		builder.AddComponentParameter(3, nameof(MokaDockPanel.Collapsible), true);
		if (panel.Collapsed is { } collapsed)
		{
			builder.AddComponentParameter(4, nameof(MokaDockPanel.Collapsed), collapsed);
		}

		if (panel.Floating is { } floating)
		{
			builder.AddComponentParameter(5, nameof(MokaDockPanel.Floating), floating);
		}

		if (panel.MaxSize is not null)
		{
			builder.AddComponentParameter(6, nameof(MokaDockPanel.MaxSize), panel.MaxSize);
		}

		if (panel.Title is not null)
		{
			builder.AddComponentParameter(7, nameof(MokaDockPanel.Title), panel.Title);
		}

		builder.AddComponentParameter(8, nameof(MokaDockPanel.ChildContent), body ?? PlainBody);

		if (panel.CollapsedSize is not null)
		{
			builder.AddComponentParameter(9, nameof(MokaDockPanel.CollapsedSize), panel.CollapsedSize);
		}

		if (panel.FloatingX is { } floatingX)
		{
			builder.AddComponentParameter(10, nameof(MokaDockPanel.FloatingX), floatingX);
		}

		if (panel.FloatingY is { } floatingY)
		{
			builder.AddComponentParameter(11, nameof(MokaDockPanel.FloatingY), floatingY);
		}

		if (panel.MinSize is not null)
		{
			builder.AddComponentParameter(12, nameof(MokaDockPanel.MinSize), panel.MinSize);
		}

		if (panel.Actions is not null)
		{
			builder.AddComponentParameter(14, nameof(MokaDockPanel.Actions), panel.Actions);
		}

		builder.CloseComponent();

		builder.OpenComponent<MokaDockContent>(15);
		builder.CloseComponent();
	};

	private static void PlainBody(RenderTreeBuilder builder) => builder.AddContent(0, "panel body");

	private static void StatefulChild(RenderTreeBuilder builder)
	{
		builder.OpenComponent<StatefulProbe>(0);
		builder.CloseComponent();
	}

	private static string? GridValue<TLayout>(IRenderedComponent<TLayout> cut, string property)
		where TLayout : MokaDockLayout =>
		StyleValue(cut.Find(".moka-dock-layout"), property);

	private static string? StyleValue(IElement element, string property)
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
		public string Size { get; init; } = "250px";
		public string? MinSize { get; init; }
		public string? MaxSize { get; init; }
		public bool? Collapsed { get; init; }
		public string? CollapsedSize { get; init; }
		public bool? Floating { get; init; }
		public double? FloatingX { get; init; }
		public double? FloatingY { get; init; }
		public string? Title { get; init; }
		public RenderFragment? Actions { get; init; }
	}

	// Counts the layout's own renders. bUnit's RenderCount also counts every component inside it.
	private sealed class CountingLayout : MokaDockLayout
	{
		public int Renders { get; private set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			Renders++;
			base.BuildRenderTree(builder);
		}
	}

	// Its count lives only as long as the component instance, so a remount resets it.
	private sealed class StatefulProbe : ComponentBase
	{
		private int _count;

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenElement(0, "button");
			builder.AddAttribute(1, "class", "probe");
			builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, () => { _count++; }));
			builder.AddContent(3, _count);
			builder.CloseElement();
		}
	}
}
