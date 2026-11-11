using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.ContextMenu.Extensions;
using Moka.Red.Core.Theming;
using Moka.Red.Icons;
using static Moka.Red.ContextMenu.Tests.RadialDom;

namespace Moka.Red.ContextMenu.Tests.Components;

// The shape, look and choice style travel in the spec, so ShowRadial carries them; the host and the trigger add defaults.
public class MokaRadialMenuOptionsTests : BunitContext
{
	private const string TriggerModule = "./_content/Moka.Red.ContextMenu/MokaRadialMenuTrigger.razor.js";

	private readonly BunitJSModuleInterop _ring;

	public MokaRadialMenuOptionsTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_ring = JSInterop.SetupModule(Module);
		JSInterop.SetupModule(TriggerModule);
		JSInterop.SetupModule("./_content/Moka.Red.ContextMenu/MokaContextMenu.razor.js");
	}

	private IMokaContextMenuService Menu => Services.GetRequiredService<IMokaContextMenuService>();

	[Fact]
	public void ASpec_LeavesEveryStyleToTheMenu()
	{
		MokaRadialMenuSpec spec = Device();

		Assert.Null(spec.Shape);
		Assert.Null(spec.Look);
		Assert.Null(spec.ChoiceStyle);
		Assert.Null(spec.AdaptToEdges);
		Assert.Null(spec.BackText);
	}

	[Fact]
	public void AChoice_KeepsItsConstructor_AndTakesAnIcon()
	{
		MokaRadialChoice plain = new("10", "10 Mbit/s", true);
		MokaRadialChoice withIcon = plain with { Icon = MokaIcons.Action.Send };

		Assert.Null(plain.Icon);
		Assert.Equal(MokaIcons.Action.Send.SvgPath, withIcon.Icon!.Value.SvgPath);
		(string id, string label, bool isChecked) = plain;
		Assert.Equal(("10", "10 Mbit/s", true), (id, label, isChecked));
	}

	[Fact]
	public async Task ShowRadial_CarriesTheSpecsShapeLookAndChoices()
	{
		Services.AddMokaContextMenu();
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>();

		await cut.InvokeAsync(() => Menu.ShowRadial(150, 220, Device() with
		{
			Shape = MokaRadialShape.Pie,
			Look = MokaRadialLook.Terminal,
			ChoiceStyle = MokaRadialChoiceStyle.Flyout
		}));

		Assert.Equal(
			"moka-radial-menu moka-radial-menu--success moka-radial-menu--pie moka-radial-menu--terminal moka-radial-menu--choices-flyout",
			Ring(cut).ClassName);
	}

	[Fact]
	public async Task TheHostsDefaults_ApplyWhereTheSpecSaysNothing()
	{
		Services.AddMokaContextMenu();
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>(p => p
			.Add(h => h.RadialShape, MokaRadialShape.Labeled)
			.Add(h => h.RadialLook, MokaRadialLook.Soft)
			.Add(h => h.RadialChoiceStyle, MokaRadialChoiceStyle.Ring));

		await cut.InvokeAsync(() => Menu.ShowRadial(150, 220, Device()));
		Assert.Equal(
			"moka-radial-menu moka-radial-menu--success moka-radial-menu--labeled moka-radial-menu--soft moka-radial-menu--choices-ring",
			Ring(cut).ClassName);

		await cut.InvokeAsync(() => Menu.ShowRadial(150, 220, Device() with { Shape = MokaRadialShape.Marking, Look = MokaRadialLook.Standard }));
		Assert.Equal("moka-radial-menu moka-radial-menu--success moka-radial-menu--marking moka-radial-menu--choices-ring",
			Ring(cut).ClassName);
	}

	[Fact]
	public async Task TheHost_CanAskEveryRingToAdapt()
	{
		Services.AddMokaContextMenu();
		_ring.Setup<MokaRadialMenu.ViewportSize?>("measureViewport")
			.SetResult(new MokaRadialMenu.ViewportSize { Width = 1200, Height = 800 });
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>(p => p.Add(h => h.RadialAdaptToEdges, true));

		await cut.InvokeAsync(() => Menu.ShowRadial(600, 790, Device()));

		cut.WaitForAssertion(() => Assert.Contains("moka-radial-menu--half-ring", Ring(cut).ClassList), TimeSpan.FromSeconds(5));
	}

	[Fact]
	public async Task TheHostsRing_FollowsTheThemesPopups()
	{
		Services.AddMokaContextMenu();
		IRenderedComponent<MokaContextMenuHost> cut = Render<MokaContextMenuHost>(p =>
			p.AddCascadingValue(MokaTheme.Default.WithPopups(MokaPopupLook.Glass)));

		await cut.InvokeAsync(() => Menu.ShowRadial(150, 220, Device()));

		Assert.Contains("moka-radial-menu--glass", Ring(cut).ClassList);
	}

	[Fact]
	public async Task TheTrigger_PutsItsChoicesInTheSpecItOpens()
	{
		Services.AddMokaContextMenu();
		Render<MokaContextMenuHost>();
		IRenderedComponent<MokaRadialMenuTrigger> trigger = RenderTrigger(Device() with { Look = MokaRadialLook.Solid }, p => p
			.Add(t => t.Shape, MokaRadialShape.Pie)
			.Add(t => t.ChoiceStyle, MokaRadialChoiceStyle.Replace)
			.Add(t => t.AdaptToEdges, true));

		await trigger.Find(".moka-radial-menu-trigger").ContextMenuAsync(new MouseEventArgs { ClientX = 10, ClientY = 20, Button = 2, Detail = 1 });

		MokaRadialMenuSpec opened = Menu.Radial!.Spec;
		Assert.Equal(MokaRadialShape.Pie, opened.Shape);
		Assert.Equal(MokaRadialLook.Solid, opened.Look);
		Assert.Equal(MokaRadialChoiceStyle.Replace, opened.ChoiceStyle);
		Assert.True(opened.AdaptToEdges);
	}

	[Fact]
	public async Task TheTrigger_WithNothingOfItsOwn_OpensTheSpecItWasGiven()
	{
		Services.AddMokaContextMenu();
		Render<MokaContextMenuHost>();
		MokaRadialMenuSpec spec = Device();
		IRenderedComponent<MokaRadialMenuTrigger> trigger = RenderTrigger(spec);

		await trigger.Find(".moka-radial-menu-trigger").ContextMenuAsync(new MouseEventArgs { ClientX = 10, ClientY = 20, Button = 2, Detail = 1 });

		Assert.Same(spec, Menu.Radial!.Spec);
	}

	[Fact]
	public async Task TheTrigger_WithoutAHost_DrawsItsOwnRingInItsShape()
	{
		IRenderedComponent<MokaRadialMenuTrigger> trigger = RenderTrigger(Device(), p => p
			.Add(t => t.Shape, MokaRadialShape.Compact)
			.Add(t => t.Look, MokaRadialLook.Outline));

		await trigger.Find(".moka-radial-menu-trigger").ContextMenuAsync(new MouseEventArgs { ClientX = 10, ClientY = 20, Button = 2, Detail = 1 });

		Assert.Contains("moka-radial-menu--compact", Ring(trigger).ClassList);
		Assert.Contains("moka-radial-menu--outline", Ring(trigger).ClassList);
	}

	private IRenderedComponent<MokaRadialMenuTrigger> RenderTrigger(MokaRadialMenuSpec spec,
		Action<ComponentParameterCollectionBuilder<MokaRadialMenuTrigger>>? more = null) =>
		Render<MokaRadialMenuTrigger>(p =>
		{
			p.Add(t => t.Spec, spec).AddChildContent("Desktop");
			more?.Invoke(p);
		});
}
