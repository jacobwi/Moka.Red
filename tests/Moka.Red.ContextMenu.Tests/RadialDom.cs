using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Icons;

namespace Moka.Red.ContextMenu.Tests;

/// <summary>Builds radial menus for tests and reads the rendered ring.</summary>
internal static class RadialDom
{
	public const string Module = "./_content/Moka.Red.ContextMenu/MokaRadialMenu.razor.js";

	public static MokaRadialMenuSpec Device(Func<MokaRadialPick, Task>? onPick = null) => new(
		"Office desktop",
		[
			new MokaRadialItem("open", "Open", MokaIcons.Content.ExternalLink),
			new MokaRadialItem("limit", "Limit", MokaIcons.Status.Clock, Choices:
			[
				new MokaRadialChoice("10", "10 Mbit/s"),
				new MokaRadialChoice("50", "50 Mbit/s", Checked: true),
				new MokaRadialChoice("100", "100 Mbit/s")
			]),
			new MokaRadialItem("rename", "Rename", MokaIcons.Action.Edit),
			new MokaRadialItem("allow", "Allow", MokaIcons.Status.Check, MokaRadialTone.Good),
			new MokaRadialItem("block", "Block", MokaIcons.Toggle.Lock, MokaRadialTone.Danger),
			new MokaRadialItem("forget", "Forget", MokaIcons.Action.Delete, Disabled: true)
		],
		Sub: "Device",
		Icon: MokaIcons.File.Terminal,
		Color: MokaColor.Success,
		OnPick: onPick);

	public static IElement Ring<T>(IRenderedComponent<T> cut) where T : IComponent => cut.Find(".moka-radial-menu");

	public static IReadOnlyList<IElement> Slots<T>(IRenderedComponent<T> cut) where T : IComponent =>
		cut.FindAll(".moka-radial-menu > .moka-radial-menu__slot");

	public static IElement Button<T>(IRenderedComponent<T> cut, string label) where T : IComponent =>
		Assert.Single(cut.FindAll(".moka-radial-menu__button"), b => b.GetAttribute("aria-label") == label);

	public static IElement Choice<T>(IRenderedComponent<T> cut, string label) where T : IComponent =>
		Assert.Single(cut.FindAll(".moka-radial-menu__choice"),
			c => c.QuerySelector(".moka-radial-menu__choice-text")!.TextContent == label);

	/// <summary>
	///     The name of what <c>aria-activedescendant</c> points at, or null. Also checks that the same button or
	///     choice, and only that one, is drawn lit.
	/// </summary>
	public static string? Lit<T>(IRenderedComponent<T> cut) where T : IComponent
	{
		IElement ring = Ring(cut);
		string? id = ring.GetAttribute("aria-activedescendant");
		IReadOnlyList<IElement> litSlots = cut.FindAll(".moka-radial-menu__slot--active");
		if (id is null)
		{
			Assert.Empty(litSlots);
			return null;
		}

		IElement target = ring.QuerySelector($"[id='{id}']")!;
		Assert.NotNull(target);
		if (target.ClassList.Contains("moka-radial-menu__choice"))
		{
			Assert.Same(target, Assert.Single(cut.FindAll(".moka-radial-menu__choice--active")));
			return target.QuerySelector(".moka-radial-menu__choice-text")!.TextContent;
		}

		Assert.Empty(cut.FindAll(".moka-radial-menu__choice--active"));
		Assert.Same(target.ParentElement, Assert.Single(litSlots));
		return target.GetAttribute("aria-label");
	}

	public static string HubWord<T>(IRenderedComponent<T> cut) where T : IComponent =>
		cut.Find(".moka-radial-menu__word").TextContent;
}
