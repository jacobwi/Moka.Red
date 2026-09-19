using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Moka.Red.ContextMenu.Tests;

/// <summary>Reads rendered menus: their rows, the highlighted row and the position.</summary>
internal static class MenuDom
{
	/// <summary>Every open menu in document order: the root first, the deepest submenu last.</summary>
	public static IReadOnlyList<IElement> Menus<T>(IRenderedComponent<T> cut) where T : IComponent =>
		cut.FindAll("[role=menu]");

	/// <summary>
	///     The rows of one menu. A submenu is a DOM child of its parent menu, so this takes direct
	///     children only.
	/// </summary>
	public static List<IElement> Rows(IElement menu) =>
		menu.Children.Where(child => child.GetAttribute("role") == "menuitem").ToList();

	public static IElement Row(IElement menu, string text) => Assert.Single(Rows(menu), row => Text(row) == text);

	public static string Text(IElement row) => row.QuerySelector(".moka-ctx-text")!.TextContent.Trim();

	/// <summary>
	///     The text of the row the menu's <c>aria-activedescendant</c> points at, or null. Also checks
	///     that the same row, and only that row, is drawn highlighted.
	/// </summary>
	public static string? Highlighted(IElement menu)
	{
		List<IElement> drawn = Rows(menu).Where(row => row.ClassList.Contains("moka-ctx-item--focused")).ToList();
		string? id = menu.GetAttribute("aria-activedescendant");
		if (id is null)
		{
			Assert.Empty(drawn);
			return null;
		}

		IElement row = Assert.Single(Rows(menu), candidate => candidate.Id == id);
		Assert.Same(row, Assert.Single(drawn));
		return Text(row);
	}

	public static void AssertAt(IElement menu, double x, double y) =>
		Assert.Contains(string.Create(CultureInfo.InvariantCulture, $"left: {x}px; top: {y}px"),
			menu.GetAttribute("style"), StringComparison.Ordinal);
}
