using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.ContextMenu.Extensions;
using static Moka.Red.ContextMenu.Tests.MenuDom;
using static Moka.Red.ContextMenu.Tests.RadialDom;

namespace Moka.Red.ContextMenu.Tests.Components;

// An item runs from the menu's own click, so before owners only the menu redrew and a page that drew what the item
// changed kept showing the old value until something else redrew it. MokaArk wrapped every item in an EventCallback.
public class MokaContextMenuOwnerTests : BunitContext
{
	private const string MenuModule = "./_content/Moka.Red.ContextMenu/MokaContextMenu.razor.js";
	private const string TriggerModule = "./_content/Moka.Red.ContextMenu/MokaContextMenuTrigger.razor.js";

	public MokaContextMenuOwnerTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule(MenuModule);
		JSInterop.SetupModule(TriggerModule);
		JSInterop.SetupModule(Module);
		JSInterop.SetupModule("./_content/Moka.Red.ContextMenu/MokaRadialMenuTrigger.razor.js");
		Services.AddMokaContextMenu();
	}

	private IMokaContextMenuService Menu => Services.GetRequiredService<IMokaContextMenuService>();

	private static string Shown<T>(IRenderedComponent<T> cut, string selector = "output") where T : IComponent =>
		cut.Find(selector).TextContent;

	[Fact]
	public async Task WithoutAnOwner_AnItemRedrawsOnlyTheMenu()
	{
		IRenderedComponent<Board> board = Render<Board>();
		IRenderedComponent<MokaContextMenuHost> host = Render<MokaContextMenuHost>();
		await host.InvokeAsync(() => Menu.Show(new MouseEventArgs { Detail = 1 }, [Rename(board.Instance)]));

		await Row(host.Find("[role=menu]"), "Rename").ClickAsync(new MouseEventArgs());

		Assert.Equal("Renamed", board.Instance.Name);
		Assert.Equal("Report", Shown(board));
	}

	[Fact]
	public async Task AnItem_RedrawsTheOwnerItWasShownWith()
	{
		IRenderedComponent<Board> board = Render<Board>();
		IRenderedComponent<MokaContextMenuHost> host = Render<MokaContextMenuHost>();
		await host.InvokeAsync(() => Menu.Show(new MouseEventArgs { Detail = 1 }, [Rename(board.Instance)], board.Instance));

		await Row(host.Find("[role=menu]"), "Rename").ClickAsync(new MouseEventArgs());

		Assert.Equal("Renamed", Shown(board));
		Assert.False(Menu.Visible);
	}

	[Fact]
	public async Task AnAsyncItem_RedrawsTheOwnerOnceItFinishes()
	{
		IRenderedComponent<Board> board = Render<Board>();
		IRenderedComponent<MokaContextMenuHost> host = Render<MokaContextMenuHost>();
		MokaContextMenuItem save = new()
		{
			Text = "Save",
			OnClick = async () =>
			{
				board.Instance.Name = "Saving";
				await Task.Yield();
				board.Instance.Name = "Saved";
			}
		};
		await host.InvokeAsync(() => Menu.Show(120, 80, [save], board.Instance));

		await Row(host.Find("[role=menu]"), "Save").ClickAsync(new MouseEventArgs());

		board.WaitForAssertion(() => Assert.Equal("Saved", Shown(board)));
	}

	[Fact]
	public async Task TheKeyboard_RunsAnItemThroughTheOwnerToo()
	{
		IRenderedComponent<Board> board = Render<Board>();
		IRenderedComponent<MokaContextMenuHost> host = Render<MokaContextMenuHost>();
		await host.InvokeAsync(() => Menu.Show(10, 10, [Rename(board.Instance)], board.Instance, openedFromKeyboard: true));

		await host.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal("Renamed", Shown(board));
	}

	[Fact]
	public async Task ASubmenuItem_RunsThroughTheOwner()
	{
		IRenderedComponent<Board> board = Render<Board>();
		IRenderedComponent<MokaContextMenuHost> host = Render<MokaContextMenuHost>();
		MokaContextMenuItem color = new()
		{
			Text = "Color",
			Children = [new MokaContextMenuItem { Text = "Red", OnClickSync = () => board.Instance.Name = "Red" }]
		};
		await host.InvokeAsync(() => Menu.Show(10, 10, [color], board.Instance, openedFromKeyboard: true));

		await host.Find("[role=menu]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight" });
		IElement submenu = host.WaitForElement(".moka-ctx-menu--submenu");
		await submenu.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal("Red", Shown(board));
	}

	[Fact]
	public async Task AnOwnerGoneBeforeTheClick_DoesNotStopTheItem()
	{
		IRenderedComponent<Holder> holder = Render<Holder>(p => p.Add(x => x.ShowBoard, true));
		Board gone = holder.FindComponent<Board>().Instance;
		IRenderedComponent<MokaContextMenuHost> host = Render<MokaContextMenuHost>();
		await host.InvokeAsync(() => Menu.Show(10, 10, [Rename(gone)], gone));
		holder.Render(p => p.Add(x => x.ShowBoard, false));
		Assert.Empty(holder.FindComponents<Board>());

		await Row(host.Find("[role=menu]"), "Rename").ClickAsync(new MouseEventArgs());

		Assert.Equal("Renamed", gone.Name);
		Assert.False(Menu.Visible);
	}

	[Fact]
	public async Task APick_RedrawsTheOwnerTheRingWasShownWith()
	{
		IRenderedComponent<Board> board = Render<Board>();
		IRenderedComponent<MokaContextMenuHost> host = Render<MokaContextMenuHost>();
		MokaRadialMenuSpec spec = Device(pick =>
		{
			board.Instance.Name = pick.Answer;
			return Task.CompletedTask;
		});
		await host.InvokeAsync(() => Menu.ShowRadial(new MouseEventArgs { Detail = 1, Button = 2 }, spec, board.Instance));

		await Button(host, "Allow").ClickAsync(new MouseEventArgs());

		Assert.Equal("allow", Shown(board));
	}

	[Fact]
	public async Task APick_WithoutAnOwner_RedrawsOnlyTheRing()
	{
		IRenderedComponent<Board> board = Render<Board>();
		IRenderedComponent<MokaContextMenuHost> host = Render<MokaContextMenuHost>();
		MokaRadialMenuSpec spec = Device(pick =>
		{
			board.Instance.Name = pick.Answer;
			return Task.CompletedTask;
		});
		await host.InvokeAsync(() => Menu.ShowRadial(150, 220, spec));

		await Button(host, "Allow").ClickAsync(new MouseEventArgs());

		Assert.Equal("Report", Shown(board));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ATriggersOwner_RedrawsEverythingItDraws(bool sharedMenu)
	{
		IRenderedComponent<MokaContextMenuHost>? host = sharedMenu ? Render<MokaContextMenuHost>() : null;
		IRenderedComponent<Explorer> explorer = Render<Explorer>(p => p.Add(x => x.PassOwner, true));

		await explorer.Find(".moka-ctx-trigger").ContextMenuAsync(new MouseEventArgs { ClientX = 20, ClientY = 20, Button = 2 });
		IElement menu = sharedMenu ? host!.Find("[role=menu]") : explorer.Find("[role=menu]");
		await Row(menu, "Rename").ClickAsync(new MouseEventArgs());

		Assert.Equal("Renamed", Shown(explorer, ".outside"));
		Assert.Equal("Renamed", Shown(explorer, ".inside"));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task WithoutAnOwner_ATriggerRedrawsTheContentInsideIt(bool sharedMenu)
	{
		IRenderedComponent<MokaContextMenuHost>? host = sharedMenu ? Render<MokaContextMenuHost>() : null;
		IRenderedComponent<Explorer> explorer = Render<Explorer>();

		await explorer.Find(".moka-ctx-trigger").ContextMenuAsync(new MouseEventArgs { ClientX = 20, ClientY = 20, Button = 2 });
		IElement menu = sharedMenu ? host!.Find("[role=menu]") : explorer.Find("[role=menu]");
		await Row(menu, "Rename").ClickAsync(new MouseEventArgs());

		Assert.Equal("Renamed", Shown(explorer, ".inside"));
		Assert.Equal("Report", Shown(explorer, ".outside"));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ARadialTriggersOwner_RedrawsEverythingItDraws(bool sharedMenu)
	{
		IRenderedComponent<MokaContextMenuHost>? host = sharedMenu ? Render<MokaContextMenuHost>() : null;
		IRenderedComponent<Explorer> explorer = Render<Explorer>(p => p
			.Add(x => x.Radial, true)
			.Add(x => x.PassOwner, true));

		await explorer.Find(".moka-radial-menu-trigger").ContextMenuAsync(new MouseEventArgs { ClientX = 200, ClientY = 200, Detail = 1, Button = 2 });
		await (sharedMenu ? Button(host!, "Allow") : Button(explorer, "Allow")).ClickAsync(new MouseEventArgs());

		Assert.Equal("allow", Shown(explorer, ".outside"));
		Assert.Equal("allow", Shown(explorer, ".inside"));
	}

	[Fact]
	public async Task WithoutAnOwner_ARadialTriggerRedrawsTheContentInsideIt()
	{
		IRenderedComponent<MokaContextMenuHost> host = Render<MokaContextMenuHost>();
		IRenderedComponent<Explorer> explorer = Render<Explorer>(p => p.Add(x => x.Radial, true));

		await explorer.Find(".moka-radial-menu-trigger").ContextMenuAsync(new MouseEventArgs { ClientX = 200, ClientY = 200, Detail = 1, Button = 2 });
		await Button(host, "Allow").ClickAsync(new MouseEventArgs());

		Assert.Equal("allow", Shown(explorer, ".inside"));
		Assert.Equal("Report", Shown(explorer, ".outside"));
	}

	[Fact]
	public void TheService_HoldsTheOwner_UntilTheMenuCloses()
	{
		MokaContextMenuService service = new();
		Board owner = new();

		service.Show(new MouseEventArgs(), [new MokaContextMenuItem { Text = "Copy" }], owner);
		Assert.Same(owner, service.Owner);

		service.Close();
		Assert.Null(service.Owner);

		service.Show(10, 10, [new MokaContextMenuItem { Text = "Copy" }], owner, openedFromKeyboard: true, anchorTop: 4);
		Assert.Same(owner, service.Owner);
		Assert.True(service.OpenedFromKeyboard);
		Assert.Equal(4, service.AnchorTop);

		service.Show(10, 10, [new MokaContextMenuItem { Text = "Copy" }]);
		Assert.Null(service.Owner);
	}

	[Fact]
	public void TheService_GivesARingItsOwner_AndAListMenuForgetsIt()
	{
		MokaContextMenuService service = new();
		Board owner = new();

		service.Show(10, 10, [new MokaContextMenuItem { Text = "Copy" }], owner);
		service.ShowRadial(new MouseEventArgs { Button = 2, Detail = 1 }, Device(), owner);

		Assert.Same(owner, service.Radial!.Owner);
		Assert.Null(service.Owner);

		service.ShowRadial(150, 220, Device(), owner, openedFromKeyboard: true);
		Assert.Same(owner, service.Radial!.Owner);
		Assert.True(service.Radial.OpenedFromKeyboard);

		service.ShowRadial(150, 220, Device());
		Assert.Null(service.Radial!.Owner);
	}

	// An implementation written before owners still compiles, and the new overloads fall back to the old ones.
	[Fact]
	public void TheOwnerOverloads_FallBack_InOlderImplementations()
	{
		OlderService older = new();
		IMokaContextMenuService service = older;
		MokaContextMenuItem[] items = [new() { Text = "Copy" }];

		service.Show(new MouseEventArgs { ClientX = 12, ClientY = 34 }, items, new Board());
		Assert.Equal((12d, 34d), (service.X, service.Y));

		service.Show(40, 50, items, new Board(), openedFromKeyboard: true);
		Assert.Equal((40d, 50d), (service.X, service.Y));
		Assert.Null(service.Owner);
		Assert.True(service.Visible);
	}

	private static MokaContextMenuItem Rename(Board board) => new() { Text = "Rename", OnClickSync = () => board.Name = "Renamed" };

	/// <summary>Draws a name an item changes, the way a page draws what its menu acts on.</summary>
	public sealed class Board : ComponentBase
	{
		public string Name { get; set; } = "Report";

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenElement(0, "output");
			builder.AddContent(1, Name);
			builder.CloseElement();
		}
	}

	/// <summary>Draws a <see cref="Board" /> until told not to, which disposes it.</summary>
	public sealed class Holder : ComponentBase
	{
		[Parameter] public bool ShowBoard { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			if (ShowBoard)
			{
				builder.OpenComponent<Board>(0);
				builder.CloseComponent();
			}
		}
	}

	/// <summary>Writes a trigger around one copy of its name and draws another outside it.</summary>
	public sealed class Explorer : ComponentBase
	{
		[Parameter] public bool PassOwner { get; set; }

		[Parameter] public bool Radial { get; set; }

		public string Name { get; private set; } = "Report";

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			builder.OpenElement(0, "p");
			builder.AddAttribute(1, "class", "outside");
			builder.AddContent(2, Name);
			builder.CloseElement();

			RenderFragment inside = content =>
			{
				content.OpenElement(0, "span");
				content.AddAttribute(1, "class", "inside");
				content.AddContent(2, Name);
				content.CloseElement();
			};

			if (Radial)
			{
				builder.OpenComponent<MokaRadialMenuTrigger>(3);
				builder.AddComponentParameter(4, nameof(MokaRadialMenuTrigger.Spec), Device(pick =>
				{
					Name = pick.Answer;
					return Task.CompletedTask;
				}));
			}
			else
			{
				builder.OpenComponent<MokaContextMenuTrigger>(3);
				builder.AddComponentParameter(4, nameof(MokaContextMenuTrigger.Items),
					new MokaContextMenuItem[] { new() { Text = "Rename", OnClickSync = () => Name = "Renamed" } });
			}

			if (PassOwner)
			{
				builder.AddComponentParameter(5, "Owner", this);
			}

			builder.AddComponentParameter(6, "ChildContent", inside);
			builder.CloseComponent();
		}
	}

	/// <summary>Implements only what the interface asked for before owners.</summary>
	private sealed class OlderService : IMokaContextMenuService
	{
		public event Action? OnChanged;

		public bool Visible { get; private set; }

		public double X { get; private set; }

		public double Y { get; private set; }

		public IReadOnlyList<MokaContextMenuItem> Items { get; private set; } = [];

		public void Show(double x, double y, IReadOnlyList<MokaContextMenuItem> items)
		{
			(X, Y, Items, Visible) = (x, y, items, true);
			OnChanged?.Invoke();
		}

		public void Show(MouseEventArgs mouseEvent, IReadOnlyList<MokaContextMenuItem> items) =>
			Show(mouseEvent.ClientX, mouseEvent.ClientY, items);

		public void Close() => Visible = false;
	}
}
