using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
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
		builder.CloseComponent();

		builder.OpenComponent<MokaDockContent>(9);
		builder.CloseComponent();
	};

	private static void PlainBody(RenderTreeBuilder builder) => builder.AddContent(0, "panel body");

	private static void StatefulChild(RenderTreeBuilder builder)
	{
		builder.OpenComponent<StatefulProbe>(0);
		builder.CloseComponent();
	}

	private static string? GridValue<TLayout>(IRenderedComponent<TLayout> cut, string property)
		where TLayout : MokaDockLayout
	{
		string style = cut.Find(".moka-dock-layout").GetAttribute("style") ?? "";
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
		public string? MaxSize { get; init; }
		public bool? Collapsed { get; init; }
		public bool? Floating { get; init; }
		public string? Title { get; init; }
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
