using Moka.Red.Core.Icons;

namespace Moka.Red.Navigation.AppShell;

/// <summary>
///     One screen of an app. A plain value, so the same list can feed the menu, the phone's title, a page's eyebrow and
///     the command palette. When a pill's number changes, build the entry again with <c>with { Pill = ... }</c>. With
///     <see cref="Children" /> it's a page that also holds a group of screens.
/// </summary>
/// <param name="Group">
///     The group it's listed under. Empty puts it above the named groups, with no heading. A child's group isn't read.
/// </param>
/// <param name="Label">The screen's name.</param>
/// <param name="Route">The page's route, like <c>/devices</c>.</param>
/// <param name="Icon">Its icon, which is all the rail shows of it.</param>
/// <param name="Keywords">More words a search finds it by. The menu doesn't read them.</param>
/// <param name="Pill">The number it shows right now, if any.</param>
public sealed record MokaNavEntry(
	string Group,
	string Label,
	string Route,
	MokaIconDefinition Icon,
	string Keywords = "",
	MokaNavPill? Pill = null)
{
	/// <summary>
	///     Listed at the bottom of the menu, under the list that scrolls and above the account card, such as Settings. Its
	///     <see cref="Group" /> isn't shown there. A child's isn't read.
	/// </summary>
	public bool Bottom { get; init; }

	/// <summary>
	///     The screens listed under this one, in order. The entry stays a page of its own: in the menu its name is a link to
	///     <see cref="Route" />, and an arrow beside it opens and closes the children. On the rail its icon opens them beside
	///     it, under a link to its own page. Children can hold children. Empty by default.
	/// </summary>
	public IReadOnlyList<MokaNavEntry> Children { get; init; } = [];

	/// <summary>
	///     A picture to show in place of <see cref="Icon" />, such as a plugin's own icon: a URL or a <c>data:image/...</c>
	///     URI. <see cref="Icon" /> stays the fallback, shown when this is null, isn't an address a picture can come from, or
	///     fails to load.
	/// </summary>
	public string? ImageSource { get; init; }

	/// <summary>The route as a link relative to the page's base address, so an app hosted under a path keeps working.</summary>
	public string Href
	{
		get
		{
			string relative = Route.TrimStart('/');
			return relative.Length == 0 ? "./" : relative;
		}
	}

	/// <summary>
	///     The entry a path belongs to, children included: <c>devices/tv</c> is Devices. The longest route wins, and a query
	///     or fragment doesn't count. Between two equally long routes the one listed first wins, a parent before its
	///     children. Null when the list has no such screen.
	/// </summary>
	/// <param name="entries">The menu.</param>
	/// <param name="path">A path, with or without its leading slash.</param>
	/// <returns>The entry, or null.</returns>
	public static MokaNavEntry? Find(IEnumerable<MokaNavEntry> entries, string path)
	{
		ArgumentNullException.ThrowIfNull(entries);
		ArgumentNullException.ThrowIfNull(path);

		string wanted = path.Split('?', '#')[0].Trim('/');
		MokaNavEntry? best = null;
		int bestLength = -1;
		foreach (MokaNavEntry entry in Flatten(entries))
		{
			string route = entry.Route.Trim('/');
			if (route.Length > bestLength && IsUnder(wanted, route))
			{
				best = entry;
				bestLength = route.Length;
			}
		}

		return best;
	}

	/// <summary>
	///     Every entry in the list and under it, in the menu's order: each entry, then its children. For whatever lists
	///     every screen, such as the command palette.
	/// </summary>
	/// <param name="entries">The menu.</param>
	/// <returns>The entries, children included.</returns>
	public static IEnumerable<MokaNavEntry> Flatten(IEnumerable<MokaNavEntry> entries)
	{
		ArgumentNullException.ThrowIfNull(entries);
		return Walk(entries);

		static IEnumerable<MokaNavEntry> Walk(IEnumerable<MokaNavEntry> level)
		{
			foreach (MokaNavEntry entry in level)
			{
				yield return entry;
				foreach (MokaNavEntry child in Walk(entry.Children))
				{
					yield return child;
				}
			}
		}
	}

	// An empty route is the home page, which matches only itself: every path starts with nothing.
	private static bool IsUnder(string path, string route) =>
		route.Length == 0
			? path.Length == 0
			: string.Equals(path, route, StringComparison.OrdinalIgnoreCase)
			  || path.StartsWith(route + "/", StringComparison.OrdinalIgnoreCase);
}
