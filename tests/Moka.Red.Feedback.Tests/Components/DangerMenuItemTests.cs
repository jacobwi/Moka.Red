using AngleSharp.Dom;
using Bunit;
using Moka.Red.Feedback.Dropdown;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

// One look for a danger item in all three menus: the dropdown, the context menu and the menu bar. The row and its icon
// are in the error color, and the lit row keeps that color over the error tint. The original design uses the error
// color itself; the new design the popup look's danger color, else the error color meant for text.
public class DangerMenuItemTests : BunitContext
{
	private const string Original = "var(--moka-color-error)";
	private const string NewDesign = "var(--moka-popup-danger, var(--moka-color-error-soft))";
	private const string Tint = "var(--moka-color-error-dim)";

	private const string Dropdown = "MokaDropdownItem.razor.css";
	private const string ContextMenu = "MokaContextMenu.razor.css";
	private const string MenuBar = "MokaMenuBarItem.razor.css";

	public DangerMenuItemTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	public static TheoryData<string, string> RestingRows => new()
	{
		{ Dropdown, ".moka-dropdown-item--danger" },
		{ ContextMenu, ".moka-ctx-item--danger" },
		{ MenuBar, ".moka-menubar-item--danger" }
	};

	public static TheoryData<string, string> LitRows => new()
	{
		{ Dropdown, ".moka-dropdown-item--danger:hover:not(.moka-dropdown-item--disabled)" },
		{ Dropdown, ".moka-dropdown-item--danger:focus-visible" },
		{ ContextMenu, ".moka-ctx-menu[data-moka-pointer] .moka-ctx-item--danger:hover:not(.moka-ctx-item--disabled)" },
		{ ContextMenu, ".moka-ctx-menu:not([data-moka-pointer]) .moka-ctx-item--danger.moka-ctx-item--focused:not(.moka-ctx-item--disabled)" },
		{ MenuBar, ".moka-menubar-item--danger:hover:not(:disabled)" },
		{ MenuBar, ".moka-menubar-item--danger.moka-menubar-item--highlighted:not(:disabled)" }
	};

	public static TheoryData<string, string> NewDesignRestingRows => new()
	{
		{ Dropdown, ".moka-gate .moka-dropdown-item--danger" },
		{ Dropdown, ".moka-gate .moka-dropdown-item--danger ::deep .moka-dropdown-item-icon" },
		{ ContextMenu, ".moka-gate .moka-ctx-menu .moka-ctx-item--danger" },
		{ ContextMenu, ".moka-gate .moka-ctx-menu .moka-ctx-item--danger ::deep .moka-ctx-icon" },
		{ MenuBar, ".moka-gate .moka-menubar-item--danger" }
	};

	public static TheoryData<string, string> NewDesignLitIcons => new()
	{
		{ Dropdown, ".moka-gate .moka-dropdown-item--danger:hover:not(.moka-dropdown-item--disabled) ::deep .moka-dropdown-item-icon" },
		{ Dropdown, ".moka-gate .moka-dropdown-item--danger:focus-visible ::deep .moka-dropdown-item-icon" },
		{ ContextMenu, ".moka-gate .moka-ctx-menu[data-moka-pointer] .moka-ctx-item--danger:hover:not(.moka-ctx-item--disabled) ::deep .moka-ctx-icon" },
		{
			ContextMenu,
			".moka-gate .moka-ctx-menu:not([data-moka-pointer]) .moka-ctx-item--danger.moka-ctx-item--focused:not(.moka-ctx-item--disabled) ::deep .moka-ctx-icon"
		}
	};

	[Theory]
	[MemberData(nameof(RestingRows))]
	public void TheOriginalDesign_DrawsADangerRowInTheErrorColor(string file, string selector) =>
		Assert.Equal(Original, ScopedCss.Declarations(file, selector)["color"]);

	[Theory]
	[MemberData(nameof(LitRows))]
	public void TheOriginalDesign_TintsALitDangerRow(string file, string selector) =>
		Assert.Equal(Tint, ScopedCss.Declarations(file, selector)["background"]);

	[Theory]
	[MemberData(nameof(NewDesignRestingRows))]
	public void TheNewDesign_DrawsADangerRowInThePopupsDangerColor(string file, string selector) =>
		Assert.Equal(NewDesign, ScopedCss.Declarations(file, selector)["color"]);

	[Theory]
	[MemberData(nameof(LitRows))]
	public void TheNewDesign_KeepsTheDangerColorOnALitRow_OverTheTint(string file, string selector)
	{
		IReadOnlyDictionary<string, string> lit = ScopedCss.Declarations(file, ".moka-gate " + selector);

		Assert.Equal(Tint, lit["background"]);
		Assert.Equal(NewDesign, lit["color"]);
	}

	[Theory]
	[MemberData(nameof(NewDesignLitIcons))]
	public void TheNewDesign_KeepsTheDangerColorOnALitRowsIcon(string file, string selector) =>
		Assert.Equal(NewDesign, ScopedCss.Declarations(file, selector)["color"]);

	[Fact]
	public void ADropdownItem_IsDangerOnlyWhenAsked()
	{
		IElement plain = Render<MokaDropdownItem>(p => p.Add(x => x.Text, "Rename")).Find("[role=menuitem]");
		IElement danger = Render<MokaDropdownItem>(p => p
			.Add(x => x.Text, "Delete")
			.Add(x => x.Danger, true)).Find("[role=menuitem]");

		Assert.Equal(["moka-dropdown-item"], plain.ClassList.Where(IsOwn));
		Assert.Equal(["moka-dropdown-item", "moka-dropdown-item--danger"], danger.ClassList.Where(IsOwn));
	}

	[Fact]
	public void ADangerLink_CarriesTheClassToo() =>
		Assert.Contains("moka-dropdown-item--danger", Render<MokaDropdownItem>(p => p
			.Add(x => x.Text, "Delete")
			.Add(x => x.Href, "/files/delete")
			.Add(x => x.Danger, true)).Find("a").ClassList);

	[Fact]
	public void TheAppsClass_StillComesLast() =>
		Assert.Equal("moka-dropdown-item moka-dropdown-item--danger app-row", Render<MokaDropdownItem>(p => p
			.Add(x => x.Text, "Delete")
			.Add(x => x.Danger, true)
			.Add(x => x.Class, "app-row")).Find("[role=menuitem]").GetAttribute("class"));

	private static bool IsOwn(string name) => name.StartsWith("moka-dropdown-item", StringComparison.Ordinal);
}
