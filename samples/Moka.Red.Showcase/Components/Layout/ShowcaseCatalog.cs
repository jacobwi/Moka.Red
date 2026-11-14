using Moka.Red.Core.Icons;
using Moka.Red.Icons;
using Moka.Red.Navigation.AppShell;
using Moka.Red.Primitives.Badge;
using Moka.Red.Showcase.Components.Library;

namespace Moka.Red.Showcase.Components.Layout;

internal sealed record ShowcasePage(string Group, string Label, string Route, MokaIconDefinition Icon, string Description, string Keywords = "")
{
	// The release a page's subject first ships in, while it is still to come. The menu marks it New.
	public string? NewIn { get; init; }

	public bool Bottom { get; init; }
}

internal static class ShowcaseCatalog
{
	public const string ComponentsGroup = "Components";
	public const string PatternsGroup = "Patterns";
	public const string ThemeGroup = "Theme";
	public const string ToolsGroup = "Tools";

	public static ShowcasePage Overview { get; } =
		new(string.Empty, "Overview", "/", MokaIcons.Navigation.Home, "The library at a glance.", "home start");

	public static ShowcasePage AllComponents { get; } =
		new(string.Empty, "All components", "/components", MokaIcons.Navigation.Grid, "Every component, by area.", "catalog index");

	public static IReadOnlyList<ShowcasePage> Patterns { get; } =
	[
		new(PatternsGroup, "A whole page", "/patterns/whole-page", MokaIcons.File.Document,
			"A detail page from header to empty state, made of library parts only.", "PageHeader Tabs Alert Card Copyable KeyValue Stat"),
		new(PatternsGroup, "Settings page", "/patterns/settings-page", MokaIcons.Action.Settings,
			"The settings frame: a side list, groups, a save bar and a guard before leaving.", "SettingsForm SettingsLayout SettingsGroup ActionBar PageTitle"),
		new(PatternsGroup, "App shell", "/patterns/app-shell", MokaIcons.Content.Columns,
			"The frame around every page: menu, rail, phone bar and account card.", "AppShell NavEntry AccountCard Sidebar HotkeyHost"),
		new(PatternsGroup, "Board customizing", "/patterns/board", MokaIcons.Action.GripVertical,
			"A board of cards people arrange, resize and hide.", "BentoGrid BentoItem BoardCard"),
		new(PatternsGroup, "File browser", "/patterns/file-browser", MokaIcons.File.Folder,
			"A full file manager: folders, selection, rename, drag, upload and a cut and paste.", "FileBrowser Files Explorer"),
		new(PatternsGroup, "Dock layout", "/patterns/dock-layout", MokaIcons.Navigation.Dock,
			"An IDE-like frame of panels that resize, collapse and float.", "DockLayout DockPanel DockContent"),
		new(PatternsGroup, "Boot screen", "/patterns/boot-screen", MokaIcons.Status.Loading,
			"The screen an app shows while it starts, in every variant and state.", "BootScreen BootStep")
	];

	public static IReadOnlyList<ShowcasePage> Theme { get; } =
	[
		new(ThemeGroup, "Theme generator", "/theme/generator", MokaIcons.Content.EyeDropper,
			"Build a theme from seeds, pin tokens, check contrast and export it.", "ThemeStudio seeds accent neutral contrast tokens export"),
		new(ThemeGroup, "Tokens", "/theme/tokens", MokaIcons.File.Braces,
			"Every CSS custom property the theme writes, with its value.", "tokens CSS variables colors spacing radius shadow"),
		new(ThemeGroup, "Looks", "/theme/looks", MokaIcons.Content.Paintbrush,
			"The component looks a theme picks for toasts, popups, lists, dialogs and buttons.", "Toast Notification Popup List Dialog Button looks Component style"),
		new(ThemeGroup, "Label colors", "/theme/label-colors", MokaIcons.Content.Tag,
			"The seventeen label hues and their tones.", "label colors hues tags dots swatches"),
		new(ThemeGroup, "Chart looks", "/theme/chart-looks", MokaIcons.Content.BarChart,
			"Chart looks, palettes, textures and print.", "Chart look palette Reference AccentFirst Muted hatch texture print forced colors ramps")
	];

	public static IReadOnlyList<ShowcasePage> Tools { get; } =
	[
		new(ToolsGroup, "Diagnostics", "/components/diagnostics", MokaIcons.Status.Activity,
			"The diagnostics overlay, and its full window in a tab of its own.", "Diagnostics overlay Ctrl+Shift+D render performance"),
		new(ToolsGroup, "Appearance", "/appearance", MokaIcons.Action.Sliders,
			"This app's own look: colors, style and where the look controls sit.", "settings look colors style dock drawer popover") { Bottom = true }
	];

	public static IEnumerable<ShowcasePage> Pages => Patterns.Concat(Theme).Concat(Tools).Prepend(AllComponents).Prepend(Overview);

	// The name the page bar shows for a path.
	public static string? PageName(string path)
	{
		string wanted = path.Split('?', '#')[0].Trim('/');
		if (wanted.StartsWith("components/", StringComparison.OrdinalIgnoreCase))
		{
			return LibraryCatalog.Find(wanted["components/".Length..])?.Title;
		}

		if (LibraryCatalog.Area(wanted) is { } area)
		{
			return area.Name;
		}

		return Pages.FirstOrDefault(page => string.Equals(page.Route.Trim('/'), wanted, StringComparison.OrdinalIgnoreCase))?.Label;
	}

	// One menu: each area is a page and a group of its components, and the shell opens the area being looked at.
	public static IReadOnlyList<MokaNavEntry> Menu { get; } = Build();

	private static List<MokaNavEntry> Build()
	{
		List<MokaNavEntry> entries = [Entry(Overview), Entry(AllComponents)];
		foreach (LibraryArea area in LibraryCatalog.Areas)
		{
			int count = area.Components.Count;
			entries.Add(new MokaNavEntry(ComponentsGroup, area.Name, area.Href, area.Icon, area.Description,
				new MokaNavPill(count, MokaBadgeTone.Count, count == 1 ? "1 component" : $"{count} components"))
			{
				Children = [.. area.Components.Select(component =>
					new MokaNavEntry(ComponentsGroup, component.Title, component.Href, MokaIcons.Content.Hash, LibraryCatalog.Keywords(component)))]
			});
		}

		entries.AddRange(Patterns.Concat(Theme).Concat(Tools).Select(Entry));
		return entries;
	}

	private static MokaNavEntry Entry(ShowcasePage page) =>
		new(page.Group, page.Label, page.Route, page.Icon, page.Keywords, NewPill(page)) { Bottom = page.Bottom };

	// The menu draws the word New over a Ready pill, since a pill carries a number and no words.
	private static MokaNavPill? NewPill(ShowcasePage page) =>
		page.NewIn is { } version ? new MokaNavPill(1, MokaBadgeTone.Ready, $"New in {version}") : null;
}
