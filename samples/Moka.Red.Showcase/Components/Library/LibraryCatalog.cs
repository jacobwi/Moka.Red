using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;
using Moka.Red.Feedback.SystemNotification;
using Moka.Red.Icons;

namespace Moka.Red.Showcase.Components.Library;

public sealed record LibraryComponent(
	string Name,
	string Title,
	string Area,
	Type Type,
	IReadOnlyList<Type> Parts,
	Type? Demos,
	string Summary)
{
	public string Slug { get; } = LibraryCatalog.Kebab(Name);

	public string Href => $"/components/{Slug}";

	public string TypeName => ComponentParameters.DisplayName(Type);

	public string Package => Type.Assembly.GetName().Name ?? string.Empty;

	public string Namespace => Type.Namespace ?? string.Empty;

	public string? DocsHref => DocsLinks.For(Name, Slug);

	public string Line => XmlDocs.FirstSentence(Summary);

	public IEnumerable<Type> Types => Parts.Prepend(Type);

	// A page about a service has a type to name and no parameters to list.
	public IEnumerable<Type> ComponentTypes => Types.Where(type => typeof(Microsoft.AspNetCore.Components.IComponent).IsAssignableFrom(type));
}

public sealed record LibraryArea(string Name, string Slug, MokaIconDefinition Icon, string Description)
{
	public string Href => $"/{Slug}";

	public IReadOnlyList<LibraryComponent> Components => LibraryCatalog.In(Name);
}

internal sealed record LibraryPreset(string Name, MokaTheme Theme);

// Every page is built from the assemblies: a component the library adds shows up here, in its package's area, with
// its own parameter table, before anyone writes a demo for it.
internal static partial class LibraryCatalog
{
	public const string Primitives = "Primitives";
	public const string Layout = "Layout";
	public const string Forms = "Forms";
	public const string Feedback = "Feedback";
	public const string Data = "Data";
	public const string Charts = "Charts";
	public const string Navigation = "Navigation";
	public const string Media = "Media";

	private const string Prefix = "Moka.Red";
	private const string TableColumns = "TableColumns";
	private const string DemosNamespace = "Moka.Red.Showcase.Components.Demos";

	private static readonly Dictionary<string, string> PackageAreas = new(StringComparer.Ordinal)
	{
		["Moka.Red.Primitives"] = Primitives,
		["Moka.Red.Layout"] = Layout,
		["Moka.Red.Forms"] = Forms,
		["Moka.Red.Feedback"] = Feedback,
		["Moka.Red.Data"] = Data,
		["Moka.Red.Navigation"] = Navigation,
		["Moka.Red.ContextMenu"] = Feedback,
		["Moka.Red.ThemeGen"] = Forms,
		["Moka.Red.Core"] = Layout
	};

	// Components whose page sits in another area than their package's.
	private static readonly Dictionary<string, string> AreaOf = new(StringComparer.Ordinal)
	{
		["BarChart"] = Charts,
		["BoxPlot"] = Charts,
		["CandlestickChart"] = Charts,
		["ChartLegend"] = Charts,
		["DonutChart"] = Charts,
		["DotPlot"] = Charts,
		["Funnel"] = Charts,
		["GanttChart"] = Charts,
		["Heatmap"] = Charts,
		["Histogram"] = Charts,
		["LineChart"] = Charts,
		["Sankey"] = Charts,
		["ScatterChart"] = Charts,
		["Sparkline"] = Charts,
		["StateTimeline"] = Charts,
		["Treemap"] = Charts,
		["Barcode"] = Media,
		["Carousel"] = Media,
		["Chat"] = Media,
		["Countdown"] = Media,
		["Image"] = Media,
		["ImageCompare"] = Media,
		["InfiniteCarousel"] = Media,
		["Lightbox"] = Media,
		["MediaGallery"] = Media,
		["QRCode"] = Media,
		["QRScanner"] = Media,
		["VideoEmbed"] = Media,
		["Meteors"] = Layout,
		["OrbitingIcons"] = Layout,
		["Notice"] = Feedback,
		["Skeleton"] = Feedback,
		["Tooltip"] = Feedback,
		["Diagnostics"] = Feedback,
		["KanbanBoard"] = Data,
		["Hotkeys"] = Navigation,
		["Announcer"] = Feedback,
		["PageTitle"] = Navigation,
		["TabPresence"] = Navigation
	};

	// Child components that only make sense inside another one share its page, by their type's own short name. That
	// lets MokaToast share the page MokaToastHost names Toast.
	private static readonly Dictionary<string, string> PartOf = new(StringComparer.Ordinal)
	{
		["AccordionItem"] = "Accordion",
		["AppearanceRow"] = "AppearancePanel",
		["AvatarGroup"] = "Avatar",
		["BentoItem"] = "BentoGrid",
		["BreadcrumbItem"] = "Breadcrumb",
		["ButtonGroup"] = "Button",
		["CarouselSlide"] = "Carousel",
		["Column"] = "Table",
		["ContextMenuHost"] = "ContextMenu",
		["ContextMenuTrigger"] = "ContextMenu",
		["DialogActions"] = "Dialog",
		["DialogHost"] = "Dialog",
		["DockContent"] = "DockLayout",
		["DockPanel"] = "DockLayout",
		["DropdownItem"] = "Dropdown",
		["EditColumn"] = "Table",
		["InfiniteCarouselSlide"] = "InfiniteCarousel",
		["LightboxAction"] = "Lightbox",
		["ListItem"] = "List",
		["MenuBarDivider"] = "MenuBar",
		["MenuBarItem"] = "MenuBar",
		["MenuBarMenu"] = "MenuBar",
		["MenuDivider"] = "Menu",
		["MenuItem"] = "Menu",
		["PageTitleHost"] = "PageTitle",
		["RadialMenuTrigger"] = "RadialMenu",
		["RadioItem"] = "RadioGroup",
		["Segment"] = "SegmentedControl",
		["SelectionAction"] = "SelectionBar",
		["SettingsGroup"] = "SettingsForm",
		["SettingsLayout"] = "SettingsForm",
		["StatusBarItem"] = "StatusBar",
		["StatusBarSeparator"] = "StatusBar",
		["Step"] = "Stepper",
		["TabBadge"] = "TabStrip",
		["TabContextMenu"] = "TabStrip",
		["ThemeImportExport"] = "ThemeStudio",
		["ThemePresets"] = "ThemeStudio",
		["ThemePreview"] = "ThemeStudio",
		["TimelineItem"] = "Timeline",
		["ToggleGroupItem"] = "ToggleGroup",
		["ToolbarDivider"] = "Toolbar",
		["ToolbarGroup"] = "Toolbar",
		["TreeItem"] = "Tree",
		["WizardStep"] = "Wizard",
		["Blockquote"] = "Typography",
		["Caption"] = "Typography",
		["Code"] = "Typography",
		["Heading"] = "Typography",
		["Kbd"] = "Typography",
		["Label"] = "Typography",
		["Link"] = "Typography",
		["Mark"] = "Typography",
		["Paragraph"] = "Typography",
		["Toast"] = "Toast",
		["TablePager"] = "Table",
		["ActionsColumn"] = TableColumns,
		["BytesColumn"] = TableColumns,
		["CopyableColumn"] = TableColumns,
		["DateColumn"] = TableColumns,
		["IconTextColumn"] = TableColumns,
		["ImageColumn"] = TableColumns,
		["LinkColumn"] = TableColumns,
		["MoneyColumn"] = TableColumns,
		["NumberColumn"] = TableColumns,
		["ProgressColumn"] = TableColumns,
		["RatingColumn"] = TableColumns,
		["RowMenuColumn"] = TableColumns,
		["SparklineColumn"] = TableColumns,
		["StatusColumn"] = TableColumns,
		["YesNoColumn"] = TableColumns
	};

	// Names a page goes by when its type's name says less than the feature it draws.
	private static readonly Dictionary<string, string> PageNames = new(StringComparer.Ordinal)
	{
		["AnnouncerHost"] = "Announcer",
		["BannerHost"] = "Banner",
		["DiagnosticsOverlay"] = "Diagnostics",
		["HotkeyHost"] = "Hotkeys",
		["PersonColumn"] = TableColumns,
		["Text"] = "Typography",
		["ToastHost"] = "Toast"
	};

	// Services that are used from code and have no component of their own, by the page that shows them.
	private static readonly Dictionary<string, Type> ServicePages = new(StringComparer.Ordinal)
	{
		["SystemNotifications"] = typeof(IMokaSystemNotifier)
	};

	// What a page is about, where its main type's summary speaks for one part only or there is none.
	private static readonly Dictionary<string, string> Summaries = new(StringComparer.Ordinal)
	{
		[TableColumns] = "Ready-made table columns that draw people, statuses, progress, dates, numbers, money, sizes, links, "
			+ "row menus, actions, copyable values, sparklines, ratings and images, and tell the table how to sort, filter, "
			+ "search and export them."
	};

	private static readonly Dictionary<string, string> Titles = new(StringComparer.Ordinal)
	{
		["AutoComplete"] = "Autocomplete",
		["IpAddressInput"] = "IP address input",
		["MacAddressInput"] = "MAC address input",
		["OtpInput"] = "OTP input",
		["PinInput"] = "PIN input",
		["QRCode"] = "QR code",
		["QRScanner"] = "QR scanner"
	};

	private static readonly Lazy<Snapshot> Snap = new(Load);

	public static IReadOnlyList<LibraryArea> Areas { get; } =
	[
		new(Primitives, "primitives", MokaIcons.Toggle.Star, "Buttons, tags, badges, text and the small parts every screen is made of."),
		new(Layout, "layout", MokaIcons.Navigation.Dashboard, "Cards, grids, panels and bars: the frames a page is built in."),
		new(Forms, "forms", MokaIcons.Action.Edit, "Inputs and pickers that take part in EditForm, and the settings form."),
		new(Feedback, "feedback", MokaIcons.Status.Bell, "Dialogs, toasts, menus, tooltips and progress."),
		new(Data, "data", MokaIcons.File.Database, "The table, lists that load on demand, the file browser and the board."),
		new(Charts, "charts", MokaIcons.Content.BarChart, "Lines, bars, rows, parts and states, drawn without a charting dependency."),
		new(Navigation, "navigation", MokaIcons.Navigation.Menu, "The app shell, menus, tabs, breadcrumbs and steppers."),
		new(Media, "media", MokaIcons.Content.Image, "Galleries, the lightbox, carousels, codes and the QR scanner.")
	];

	public static string Version { get; } =
		typeof(MokaTheme).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

	public static string AllHref => "/components";

	public static int PackageCount => Snap.Value.PackageCount;

	public static IReadOnlyList<LibraryComponent> Components => Snap.Value.Components;

	public static int TypeCount => Components.Sum(component => component.Parts.Count + 1);

	public static IReadOnlyList<LibraryPreset> Presets => Snap.Value.Presets;

	public static LibraryArea? Area(string slug) =>
		Areas.FirstOrDefault(area => string.Equals(area.Slug, slug, StringComparison.OrdinalIgnoreCase));

	public static IReadOnlyList<LibraryComponent> In(string area) =>
		Snap.Value.ByArea.GetValueOrDefault(area) ?? [];

	public static LibraryComponent? Find(string slug) =>
		Snap.Value.BySlug.GetValueOrDefault(slug);

	// A page by any type it shows, parts included, so "MokaMenuItem" finds the Menu page.
	public static LibraryComponent? ShowingType(string typeName) =>
		Snap.Value.ByType.GetValueOrDefault(typeName.StartsWith("Moka", StringComparison.Ordinal) ? typeName : $"Moka{typeName}");

	public static Type? TypeNamed(string typeName) => Snap.Value.Types.GetValueOrDefault(typeName);

	public static string HrefFor(string name) => (Find(Kebab(name)) ?? ShowingType(name))?.Href ?? AllHref;

	// Every word has to appear in the name, the type, a part, the package or the area. A name that starts with what
	// was typed comes first.
	public static IReadOnlyList<LibraryComponent> Search(string query)
	{
		string trimmed = query.Trim();
		if (trimmed.Length == 0)
		{
			return [];
		}

		string[] words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		return
		[
			.. Components
				.Where(component => words.All(word => Haystack(component).Contains(word, StringComparison.OrdinalIgnoreCase)))
				.OrderBy(component => Rank(component, trimmed))
				.ThenBy(component => component.Name, StringComparer.OrdinalIgnoreCase)
		];
	}

	public static string Keywords(LibraryComponent component) =>
		string.Join(' ', component.Types.Select(ComponentParameters.DisplayName).Append(component.Area));

	public static string? PresetName(MokaTheme? theme)
	{
		if (theme is null)
		{
			return null;
		}

		LibraryPreset? same = Presets.FirstOrDefault(preset => ReferenceEquals(preset.Theme, theme));
		if (same is not null)
		{
			return same.Name;
		}

		// The style choices make a new theme, so a preset is found by its colors. Gate and Crisp share their colors,
		// and what else still matches tells them apart.
		return Presets
			.Where(preset => preset.Theme.Look == theme.Look
				&& preset.Theme.IsDark == theme.IsDark
				&& Equals(preset.Theme.Palette, theme.Palette))
			.OrderByDescending(preset => Likeness(preset.Theme, theme))
			.FirstOrDefault()?.Name;
	}

	[SuppressMessage("Globalization", "CA1308", Justification = "A slug is lower case by design; it is not normalized for comparison.")]
	public static string Kebab(string name) => Words().Replace(name, "-").ToLowerInvariant();

	private static string Haystack(LibraryComponent component) =>
		$"{component.Name} {component.Title} {Keywords(component)} {component.Package}";

	// Matrix and Ember share their colors too, and Ember's own choices tell them apart.
	private static int Likeness(MokaTheme preset, MokaTheme theme) =>
		(Equals(preset.Typography, theme.Typography) ? 2 : 0)
		+ (preset.Selection == theme.Selection ? 1 : 0)
		+ (Equals(preset.Spacing, theme.Spacing) ? 1 : 0)
		+ (preset.Elevation == theme.Elevation ? 1 : 0)
		+ (preset.Callouts == theme.Callouts ? 1 : 0);

	private static int Rank(LibraryComponent component, string query)
	{
		string name = query.StartsWith("Moka", StringComparison.OrdinalIgnoreCase) ? component.TypeName : component.Name;
		if (string.Equals(name, query, StringComparison.OrdinalIgnoreCase))
		{
			return 0;
		}

		if (name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
		{
			return 1;
		}

		return name.Contains(query, StringComparison.OrdinalIgnoreCase) ? 2 : 3;
	}

	private static Snapshot Load()
	{
		Assembly[] assemblies = LibraryAssemblies();
		Dictionary<string, Type> byName = new(StringComparer.Ordinal);
		foreach (Type type in assemblies.SelectMany(ComponentTypes))
		{
			byName.TryAdd(ShortName(type), type);
		}

		Dictionary<string, Type> demos = typeof(LibraryCatalog).Assembly.GetTypes()
			.Where(type => type.Namespace?.StartsWith(DemosNamespace, StringComparison.Ordinal) == true
				&& type.Name.EndsWith("Demos", StringComparison.Ordinal)
				&& typeof(Microsoft.AspNetCore.Components.IComponent).IsAssignableFrom(type))
			.ToDictionary(type => type.Name[..^"Demos".Length], StringComparer.Ordinal);

		Dictionary<string, List<Type>> parts = new(StringComparer.Ordinal);
		List<(string Name, Type Type)> wholes = [];
		foreach ((string shortName, Type type) in byName)
		{
			if (PartOf.TryGetValue(shortName, out string? whole))
			{
				(parts.TryGetValue(whole, out List<Type>? list) ? list : parts[whole] = []).Add(type);
				continue;
			}

			wholes.Add((PageNames.GetValueOrDefault(shortName, shortName), type));
		}

		wholes.AddRange(ServicePages.Select(page => (page.Key, page.Value)));

		LibraryComponent[] components =
		[
			.. wholes
				.Select(whole => new LibraryComponent(
					whole.Name,
					Titles.GetValueOrDefault(whole.Name) ?? Humanize(whole.Name),
					AreaOf.GetValueOrDefault(whole.Name) ?? PackageAreas.GetValueOrDefault(whole.Type.Assembly.GetName().Name ?? string.Empty, Primitives),
					whole.Type,
					[.. (parts.GetValueOrDefault(whole.Name) ?? []).OrderBy(part => part.Name, StringComparer.Ordinal)],
					demos.GetValueOrDefault(whole.Name),
					Summaries.GetValueOrDefault(whole.Name) ?? XmlDocs.Summary(whole.Type)))
				.OrderBy(component => component.Title, StringComparer.OrdinalIgnoreCase)
		];

		return new Snapshot(
			assemblies.Length,
			components,
			components.GroupBy(component => component.Area).ToDictionary(group => group.Key, group => (IReadOnlyList<LibraryComponent>)[.. group]),
			components.ToDictionary(component => component.Slug, StringComparer.OrdinalIgnoreCase),
			components.SelectMany(component => component.Types.Select(type => (WithoutArity(type.Name), component)))
				.ToDictionary(pair => pair.Item1, pair => pair.component, StringComparer.Ordinal),
			components.SelectMany(component => component.Types).ToDictionary(type => WithoutArity(type.Name), StringComparer.Ordinal),
			LoadPresets());
	}

	// The build copies every referenced project next to the app, so the folder is the list of packages: the app's own
	// references leave out a package it never names in code.
	private static Assembly[] LibraryAssemblies()
	{
		string own = typeof(LibraryCatalog).Assembly.GetName().Name ?? string.Empty;
		return
		[
			.. Directory.EnumerateFiles(AppContext.BaseDirectory, $"{Prefix}*.dll")
				.Select(AssemblyName.GetAssemblyName)
				.Where(name => name.Name is { } n
					&& (string.Equals(n, Prefix, StringComparison.Ordinal) || n.StartsWith($"{Prefix}.", StringComparison.Ordinal))
					&& !string.Equals(n, own, StringComparison.Ordinal))
				.Select(Assembly.Load)
		];
	}

	// Chart chrome parts are hidden from IntelliSense because apps never write them, and the diagnostics package is a
	// tool whose panels only its overlay draws.
	private static IEnumerable<Type> ComponentTypes(Assembly assembly) =>
		assembly.GetExportedTypes()
			.Where(type => type is { IsClass: true, IsAbstract: false }
				&& type.IsSubclassOf(typeof(ComponentBase))
				&& type.Name.StartsWith("Moka", StringComparison.Ordinal)
				&& type.Namespace?.StartsWith($"{Prefix}.", StringComparison.Ordinal) == true
				&& type.GetCustomAttribute<EditorBrowsableAttribute>()?.State != EditorBrowsableState.Never
				&& (assembly.GetName().Name != "Moka.Red.Diagnostics" || type.Name == "MokaDiagnosticsOverlay"));

	private static string ShortName(Type type) => WithoutArity(type.Name)[4..];

	private static string WithoutArity(string name)
	{
		int tick = name.IndexOf('`', StringComparison.Ordinal);
		return tick < 0 ? name : name[..tick];
	}

	[SuppressMessage("Globalization", "CA1308", Justification = "Sentence case lowers every word after the first; nothing compares it.")]
	private static string Humanize(string name)
	{
		string spaced = Words().Replace(name, " ");
		return spaced[..1] + spaced[1..].ToLowerInvariant();
	}

	private static LibraryPreset[] LoadPresets()
	{
		IEnumerable<(string Name, MokaTheme Theme)> found = typeof(MokaTheme)
			.GetProperties(BindingFlags.Public | BindingFlags.Static)
			.Where(property => property.PropertyType == typeof(MokaTheme) && property.GetIndexParameters().Length == 0)
			.Select(property => (property.Name, Theme: (MokaTheme)property.GetValue(null)!));

		List<LibraryPreset> presets = [];
		foreach ((string name, MokaTheme theme) in found)
		{
			// Default and DefaultLight hand back Red and RedLight, so they are names, not presets.
			if (presets.Any(preset => ReferenceEquals(preset.Theme, theme)))
			{
				continue;
			}

			presets.Add(new LibraryPreset(name, theme));
		}

		return [.. presets];
	}

	// A word starts at a capital after a lower case letter or digit, or at the last capital of a run ("QRCode").
	[GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
	private static partial Regex Words();

	private sealed record Snapshot(
		int PackageCount,
		IReadOnlyList<LibraryComponent> Components,
		IReadOnlyDictionary<string, IReadOnlyList<LibraryComponent>> ByArea,
		IReadOnlyDictionary<string, LibraryComponent> BySlug,
		IReadOnlyDictionary<string, LibraryComponent> ByType,
		IReadOnlyDictionary<string, Type> Types,
		IReadOnlyList<LibraryPreset> Presets);
}
