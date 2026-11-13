using System.Collections.Concurrent;
using Moka.Red.Core.Icons;
using Moka.Red.Icons;
using Moka.Red.Navigation.AppShell;
using Moka.Red.Primitives.Badge;

namespace Moka.Red.DevApp.Components.Layout;

internal enum DevAppPageKind
{
	Home,
	Showcase,
	Demo,
	Pattern,
	Tool
}

internal sealed record DevAppPage(string Area, string Label, string Route, MokaIconDefinition Icon, DevAppPageKind Kind, string Keywords = "")
{
	// The release the page's subject first shipped in, when that was a 0.3 point release or is still to come.
	public string? NewIn { get; init; }

	public string? ImageSource { get; init; }

	public bool Bottom { get; init; }

	public string Path => Route.Trim('/');
}

internal static class DevAppCatalog
{
	public const string ThemeArea = "Theme and tokens";
	public const string PrimitivesArea = "Primitives";
	public const string LayoutArea = "Layout";
	public const string FormsArea = "Forms";
	public const string FeedbackArea = "Feedback";
	public const string DataArea = "Data";
	public const string NavigationArea = "Navigation";
	public const string MediaArea = "Media";
	public const string ToolsGroup = "Tools";
	public const string PatternsGroup = "Patterns";
	public const string ComponentsGroup = "Components";
	public const string NextRelease = "next";

	private const string SelectionAreaPicture =
		"data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnIHZpZXdCb3g9JzAgMCAxNiAxNic+PHJlY3Qgd2lkdGg9JzE2JyBoZWlnaHQ9JzE2JyByeD0nNCcgZmlsbD0nI2VmNTM1MCcvPjxyZWN0IHg9JzQnIHk9JzQnIHdpZHRoPSc4JyBoZWlnaHQ9JzgnIHJ4PScxJyBmaWxsPSdub25lJyBzdHJva2U9JyNmZmYnIHN0cm9rZS13aWR0aD0nMS41JyBzdHJva2UtZGFzaGFycmF5PScyIDEuNScvPjwvc3ZnPg==";

	private static readonly ConcurrentDictionary<(DevAppMenu Menu, string? Area), IReadOnlyList<MokaNavEntry>> Menus = new();

	// The misc page holds a few things that belong to an area of their own.
	private static readonly Dictionary<string, string> AreaOverrides = new(StringComparer.Ordinal)
	{
		["ButtonGroup"] = PrimitivesArea,
		["List"] = PrimitivesArea,
		["Ribbon"] = PrimitivesArea,
		["StatusBar"] = LayoutArea,
		["Toolbar"] = LayoutArea,
		["Diagnostics"] = ToolsGroup
	};

	public static IReadOnlyList<string> Areas { get; } =
		[ThemeArea, PrimitivesArea, LayoutArea, FormsArea, FeedbackArea, DataArea, NavigationArea, MediaArea];

	public static IReadOnlyList<(string Name, string Href)> Components { get; } =
	[
		("AccountCard", "demo-shell"),
		("Accordion", "layout#Accordion"),
		("ActionBar", "demo-frame"),
		("Alert", "feedback#Alert"),
		("AppBar", "layout#AppBar"),
		("AppShell", "demo-shell"),
		("AspectRatio", "layout#AspectRatio"),
		("Attribute", "primitives#Attribute"),
		("AutoComplete", "forms#AutoComplete"),
		("AppearancePanel", "demo/appearance-panel"),
		("Avatar", "primitives#Avatar"),
		("Badge", "primitives#Badge"),
		("BarChart", "data#BarChart"),
		("Barcode", "media#Barcode"),
		("BentoGrid", "layout#BentoGrid"),
		("BoardCustomizing", "demo-board"),
		("BootScreen", "feedback#BootScreen"),
		("BottomSheet", "feedback#BottomSheet"),
		("Breadcrumb", "navigation#Breadcrumb"),
		("FileBrowser", "demo/file-browser"),
		("FilePicker", "demo/file-picker"),
		("InlineEdit", "demo/inline-edit"),
		("JobList", "demo/job-list"),
		("Button", "primitives#Button"),
		("ButtonGroup", "misc#ButtonGroup"),
		("Calendar", "forms#Calendar"),
		("Callout", "primitives#Callout"),
		("Card", "layout#Card"),
		("Carousel", "media#Carousel"),
		("Cascader", "forms#Cascader"),
		("Chat", "media#Chat"),
		("Cheatsheet", "feedback#Cheatsheet"),
		("Checkbox", "forms#Checkbox"),
		("Chip", "primitives#Chip"),
		("CodeBlock", "primitives#CodeBlock"),
		("CodeEditor", "forms#CodeEditor"),
		("ColorInput", "forms#ColorInput"),
		("ColorPicker", "forms#ColorPicker"),
		("ColorSwatch", "primitives#ColorSwatch"),
		("ColorWheel", "forms#ColorWheel"),
		("CommandBar", "navigation#CommandBar"),
		("CommandPalette", "feedback#CommandPalette"),
		("Confetti", "primitives#Confetti"),
		("Container", "layout#Container"),
		("ChainCheck", "demo-chain-job"),
		("ContextMenu", "context-menu#ContextMenu"),
		("CookieConsent", "feedback#CookieConsent"),
		("Copyable", "primitives#Copyable"),
		("Countdown", "media#Countdown"),
		("CreditCardInput", "forms#CreditCardInput"),
		("CurrencyInput", "forms#CurrencyInput"),
		("DataList", "primitives#DataList"),
		("DatePicker", "forms#DatePicker"),
		("DateRangePicker", "forms#DateRangePicker"),
		("Diagnostics", "misc#Diagnostics"),
		("Dialog", "feedback#Dialog"),
		("DiffViewer", "primitives#DiffViewer"),
		("Divider", "layout#Divider"),
		("DockLayout", "dock"),
		("DonutChart", "data#DonutChart"),
		("Drawer", "feedback#Drawer"),
		("Dropdown", "feedback#Dropdown"),
		("DropOverlay", "forms#DropOverlay"),
		("EmptyState", "feedback#EmptyState"),
		("ErrorBoundary", "demo-frame"),
		("FadeIn", "primitives#FadeIn"),
		("FileUpload", "forms#FileUpload"),
		("Flag", "primitives#Flag"),
		("Flexbox", "layout#Flexbox"),
		("FloatingActionButton", "primitives#FloatingActionButton"),
		("Footer", "layout#Footer"),
		("FormBuilder", "forms#FormBuilder"),
		("Gauge", "primitives#Gauge"),
		("GlassCard", "layout#GlassCard"),
		("Grid", "layout#Grid"),
		("GridBackground", "layout#GridBackground"),
		("Heatmap", "data#Heatmap"),
		("HoverCard", "feedback#HoverCard"),
		("Icon", "primitives#Icon"),
		("Identicon", "primitives#Identicon"),
		("ImageCompare", "primitives#ImageCompare"),
		("InfiniteCarousel", "primitives#InfiniteCarousel"),
		("InfiniteScroll", "data#InfiniteScroll"),
		("InputGroup", "forms#InputGroup"),
		("IpAddressInput", "forms#IpAddressInput"),
		("JobProgress", "demo-chain-job"),
		("KeyValue", "primitives#KeyValue"),
		("Knob", "forms#Knob"),
		("Lightbox", "media#Lightbox"),
		("LineChart", "data#LineChart"),
		("LineChartLive", "data#LineChartLive"),
		("List", "misc#List"),
		("LoadingOverlay", "feedback#LoadingOverlay"),
		("LogViewer", "primitives#LogViewer"),
		("MacAddressInput", "forms#MacAddressInput"),
		("Marquee", "primitives#Marquee"),
		("MediaGallery", "media#MediaGallery"),
		("Menu", "navigation#Menu"),
		("MenuBar", "navigation#MenuBar"),
		("Meteors", "layout#Meteors"),
		("Meter", "primitives#Meter"),
		("NativeSelect", "demo-frame"),
		("NotificationBell", "feedback#NotificationBell"),
		("NotificationCenter", "feedback#NotificationCenter"),
		("NumberTicker", "primitives#NumberTicker"),
		("NumericField", "forms#NumericField"),
		("Onboarding", "feedback#Onboarding"),
		("OrbitingIcons", "layout#OrbitingIcons"),
		("OrganizationChart", "primitives#OrganizationChart"),
		("OtpInput", "forms#OtpInput"),
		("PageHeader", "layout#PageHeader"),
		("PageTitle", "demo-frame"),
		("Pagination", "data#Pagination"),
		("Panel", "layout#Panel"),
		("Paper", "layout#Paper"),
		("Parallax", "primitives#Parallax"),
		("PasswordField", "forms#PasswordField"),
		("PasswordStrength", "forms#PasswordStrength"),
		("PhoneInput", "forms#PhoneInput"),
		("PinInput", "forms#PinInput"),
		("Popover", "feedback#Popover"),
		("PriceDisplay", "forms#PriceDisplay"),
		("Progress", "feedback#Progress"),
		("QRCode", "media#QRCode"),
		("QRScanner", "media#QRScanner"),
		("RadialMenu", "demo-radial"),
		("RadioGroup", "forms#RadioGroup"),
		("RangeBar", "demo-range-bar"),
		("RangeSlider", "forms#RangeSlider"),
		("Rating", "forms#Rating"),
		("Resizable", "layout#Resizable"),
		("RetroGrid", "layout#RetroGrid"),
		("Reveal", "primitives#Reveal"),
		("Ribbon", "misc#Ribbon"),
		("ScaleIn", "primitives#ScaleIn"),
		("SchedulePicker", "forms#SchedulePicker"),
		("ScrollArea", "layout#ScrollArea"),
		("SearchInput", "forms#SearchInput"),
		("SegmentedControl", "primitives#SegmentedControl"),
		("SelectField", "forms#SelectField"),
		("SelectionArea", "demo/selection-area"),
		("SelectionBar", "primitives#SelectionBar"),
		("SelectionAction", "primitives/variants#SelectionBar"),
		("SettingsForm", "demo-frame"),
		("Sidebar", "demo-shell"),
		("SignaturePad", "forms#SignaturePad"),
		("Skeleton", "feedback#Skeleton"),
		("SlashMenu", "feedback#SlashMenu"),
		("SlideIn", "primitives#SlideIn"),
		("Slider", "forms#Slider"),
		("Sortable", "primitives#Sortable"),
		("Spacer", "layout#Spacer"),
		("Sparkline", "data#Sparkline"),
		("Spinner", "feedback#Spinner"),
		("SplitButton", "primitives#SplitButton"),
		("SplitPane", "layout#SplitPane"),
		("Stagger", "primitives#Stagger"),
		("Stat", "page-demo"),
		("StatusBar", "misc#StatusBar"),
		("StatusDot", "primitives#StatusDot"),
		("Stepper", "navigation#Stepper"),
		("Steps", "primitives#Steps"),
		("Sticky", "layout#Sticky"),
		("SwipeActions", "primitives#SwipeActions"),
		("Switch", "forms#Switch"),
		("TabContainer", "navigation#Tabs"),
		("Table", "data#Table"),
		("Tabs", "navigation#PageTabs"),
		("TabPresence", "demo-frame"),
		("Tag", "primitives#Tag"),
		("TagInput", "forms#TagInput"),
		("Terminal", "primitives#Terminal"),
		("TextArea", "forms#TextArea"),
		("TextField", "forms#TextField"),
		("ThemeEditor", "theme-editor"),
		("ThemeSwitcher", "primitives#ThemeSwitcher"),
		("TimePicker", "forms#TimePicker"),
		("Toast", "feedback#Toast"),
		("ToggleGroup", "forms#ToggleGroup"),
		("Toolbar", "misc#Toolbar"),
		("Tooltip", "feedback#Tooltip"),
		("TransferList", "primitives#TransferList"),
		("Tree", "primitives#Tree"),
		("TreeSelect", "forms#TreeSelect"),
		("Typewriter", "primitives#Typewriter"),
		("Typography", "primitives#Typography"),
		("VideoEmbed", "media#VideoEmbed"),
		("VirtualList", "data#VirtualList"),
		("Watermark", "layout#Watermark"),
		("Wizard", "feedback#Wizard")
	];

	// In the By area order: each area's showcase page first, then the demo pages for its parts.
	public static IReadOnlyList<DevAppPage> Pages { get; } =
	[
		new("", "Overview", "/", MokaIcons.Navigation.Home, DevAppPageKind.Home, "GridBackground BentoGrid Attribute"),
		new("", "All components", "/components", MokaIcons.Navigation.Grid, DevAppPageKind.Home, "catalog index every component"),

		new(ThemeArea, "Theme", "/theme", MokaIcons.Action.Sun, DevAppPageKind.Showcase, "ThemeProvider tokens colors typography spacing"),
		new(ThemeArea, "Tokens", "/tokens", MokaIcons.File.Braces, DevAppPageKind.Demo, "tokens CSS variables colors spacing radius shadow") { NewIn = NextRelease },
		new(ThemeArea, "Looks", "/demo/looks", MokaIcons.Content.Paintbrush, DevAppPageKind.Demo, "Toast Notification Popup List Dialog Button looks Component style") { NewIn = "0.3.21" },
		new(ThemeArea, "Label colors", "/demo/labels", MokaIcons.Content.Tag, DevAppPageKind.Demo, "label colors hues tags dots swatches") { NewIn = NextRelease },
		new(ThemeArea, "Chart looks", "/charts/looks", MokaIcons.Content.BarChart, DevAppPageKind.Demo, "Chart look palette Reference AccentFirst Muted hatch texture print forced colors ramps") { NewIn = NextRelease },

		new(PrimitivesArea, "Primitives", "/primitives", MokaIcons.Toggle.Star, DevAppPageKind.Showcase),
		new(PrimitivesArea, "Selection area", "/demo/selection-area", MokaIcons.Content.Cursor, DevAppPageKind.Demo, "BoxSelect Marquee Box selection")
		{
			NewIn = "0.3.19",
			ImageSource = SelectionAreaPicture
		},
		new(PrimitivesArea, "Chain check, job progress", "/demo-chain-job", MokaIcons.Content.Link, DevAppPageKind.Demo, "ChainCheck ChainStep JobProgress"),
		new(PrimitivesArea, "Emoji and reactions", "/demo/emoji", MokaIcons.Content.Smile, DevAppPageKind.Demo, "EmojiPanel EmojiPicker Reactions EmojiTypeahead emoji") { NewIn = NextRelease },
		new(PrimitivesArea, "Data list", "/demo/data-list", MokaIcons.Content.List, DevAppPageKind.Demo, "DataList term value copy columns") { NewIn = NextRelease },
		new(PrimitivesArea, "Audit variants", "/primitives/variants", MokaIcons.Content.Wand, DevAppPageKind.Demo, "ContextMenu ItemStyle Quiet FocusRing SelectionBar Overflow More menu SelectionAction List RowStyle Tree sidebar KeyValue Columns Mono variants") { NewIn = NextRelease },
		new(PrimitivesArea, "Wrap, grade, facts, browse, view", "/demo/polish-data", MokaIcons.Toggle.Eye, DevAppPageKind.Demo, "Table Column NoWrap Gauge Display grade DataList Inline facts FileUpload BrowseLook Button Reveal OnEnter OnLeave lazy thumbnail") { NewIn = NextRelease },

		new(LayoutArea, "Layout", "/layout", MokaIcons.Navigation.Dashboard, DevAppPageKind.Showcase),
		new(LayoutArea, "A whole page", "/page-demo", MokaIcons.File.Document, DevAppPageKind.Pattern, "PageHeader Tabs Alert Card Copyable KeyValue EmptyState Progress"),
		new(LayoutArea, "Page frame", "/demo-frame", MokaIcons.Action.Save, DevAppPageKind.Pattern, "PageTitleHost SettingsGroup SettingsLayout Settings page"),
		new(LayoutArea, "Board customizing", "/demo-board", MokaIcons.Action.GripVertical, DevAppPageKind.Pattern, "BentoGrid BentoItem BoardCard"),
		new(LayoutArea, "Toolbar overflow", "/demo/toolbar-overflow", MokaIcons.Navigation.MoreHorizontal, DevAppPageKind.Demo, "Toolbar ToolbarGroup ToolbarDivider Expandable") { NewIn = "0.3.16" },
		new(LayoutArea, "Grid backgrounds", "/demo/grid-looks", MokaIcons.Navigation.SplitGrid, DevAppPageKind.Demo, "GridBackground RetroGrid pattern looks") { NewIn = NextRelease },
		new(LayoutArea, "Dock layout", "/dock", MokaIcons.Navigation.Dock, DevAppPageKind.Demo, "DockPanel DockContent"),

		new(FormsArea, "Forms", "/forms", MokaIcons.Action.Edit, DevAppPageKind.Showcase),
		new(FormsArea, "Range bar", "/demo-range-bar", MokaIcons.Content.Filter, DevAppPageKind.Demo, "RangeBar Range"),
		new(FormsArea, "Inline edit", "/demo/inline-edit", MokaIcons.Action.Pencil, DevAppPageKind.Demo, "InlineEdit Rename") { NewIn = "0.3.16" },
		new(FormsArea, "File picker", "/demo/file-picker", MokaIcons.Action.Upload, DevAppPageKind.Demo, "FilePicker Upload MAUI Native dialog") { NewIn = "0.3.20" },
		new(FormsArea, "Upload and drop", "/demo/upload-looks", MokaIcons.Content.CloudUpload, DevAppPageKind.Demo, "FileUpload DropOverlay drop zone looks") { NewIn = NextRelease },
		new(FormsArea, "Color inputs", "/demo/color-looks", MokaIcons.Content.Droplet, DevAppPageKind.Demo, "ColorInput ColorPicker ColorWheel ColorSwatch looks") { NewIn = NextRelease },
		new(FormsArea, "Country select", "/demo/country-select", MokaIcons.Content.Globe, DevAppPageKind.Demo, "CountrySelect Flag Country PhoneInput prefix dialing code") { NewIn = NextRelease },
		new(FormsArea, "Color swatch", "/demo/color-swatch", MokaIcons.Content.Palette, DevAppPageKind.Demo, "ColorSwatch swatches palette groups no color") { NewIn = NextRelease },

		new(FeedbackArea, "Feedback", "/feedback", MokaIcons.Status.Bell, DevAppPageKind.Showcase),
		new(FeedbackArea, "Context menu", "/context-menu", MokaIcons.Navigation.MoreVertical, DevAppPageKind.Demo),
		new(FeedbackArea, "Radial menu", "/demo-radial", MokaIcons.Content.PieChart, DevAppPageKind.Demo, "RadialMenu ContextMenu"),
		new(FeedbackArea, "Menu owners and colors", "/demo/menus", MokaIcons.Content.Palette, DevAppPageKind.Demo, "ContextMenu Owner redraw swatch Label ColorValue RadialMenu RelativeTime TimeZone tooltip zone") { NewIn = NextRelease },
		new(FeedbackArea, "Radial menu looks", "/demo/radial-looks", MokaIcons.Content.Shapes, DevAppPageKind.Demo, "RadialMenu ring looks") { NewIn = NextRelease },
		new(FeedbackArea, "Boot screen", "/demo-boot-screen", MokaIcons.Status.Loading, DevAppPageKind.Pattern, "BootScreen BootStep"),
		new(FeedbackArea, "Slash menu", "/demo-slash-menu", MokaIcons.File.Code, DevAppPageKind.Demo, "SlashMenu Kbd"),
		new(FeedbackArea, "Job list", "/demo/job-list", MokaIcons.Action.Download, DevAppPageKind.Pattern, "JobList Job Transfer Queue") { NewIn = "0.3.16" },
		new(FeedbackArea, "Gauge, wizard, terminal", "/demo-leftovers", MokaIcons.File.Terminal, DevAppPageKind.Demo, "Gauge Wizard Terminal"),

		new(DataArea, "Data", "/data", MokaIcons.File.Database, DevAppPageKind.Showcase),
		new(DataArea, "Table additions", "/demo-table", MokaIcons.Content.RowsComfortable, DevAppPageKind.Demo, "Table TableView Column Copyable EmptyState"),
		new(DataArea, "File browser", "/demo/file-browser", MokaIcons.File.Folder, DevAppPageKind.Pattern, "FileBrowser Files Explorer") { NewIn = "0.3.16" },
		new(DataArea, "File browser looks", "/demo/file-browser-looks", MokaIcons.File.FolderTree, DevAppPageKind.Demo, "FileBrowser list tiles looks") { NewIn = NextRelease },
		new(DataArea, "Line charts", "/charts/lines", MokaIcons.Content.LineChart, DevAppPageKind.Demo, "line chart stacked area band step slope emphasis highlight small multiples split indexed direct labels points area live pause scale million downsample") { NewIn = NextRelease },
		new(DataArea, "Bars, histograms and candles", "/charts/bars", MokaIcons.Content.BarChart, DevAppPageKind.Demo, "bar chart horizontal grouped stacked percent diverging target band emphasis histogram percentile candlestick ohlc volume merge virtualize rows") { NewIn = NextRelease },
		new(DataArea, "Chart frame", "/charts/frame", MokaIcons.Navigation.PanelTop, DevAppPageKind.Demo, "chart toolbar legend toggles export png svg csv table view loading empty error range strip presets") { NewIn = NextRelease },
		new(DataArea, "Chart interaction", "/charts/interaction", MokaIcons.Content.Hand, DevAppPageKind.Demo, "chart tooltip crosshair keyboard pin sync zoom brush wheel live hold readout") { NewIn = NextRelease },
		new(DataArea, "Chart axes", "/charts/axes", MokaIcons.Content.Ruler, DevAppPageKind.Demo, "chart axis axes labels rotate stagger wrap truncate scroll time log units grid threshold band event window annotation rows") { NewIn = NextRelease },
		new(DataArea, "Small charts", "/charts/small", MokaIcons.Content.PieChart, DevAppPageKind.Demo, "donut ring bar waffle treemap sankey heatmap hour weekday bullet meter list kpi stat sparkline") { NewIn = NextRelease },
		new(DataArea, "Rows and points", "/charts/rows", MokaIcons.Content.BarChart, DevAppPageKind.Demo, "dot plot lollipop dumbbell box plot whiskers scatter bubble gantt jobs schedule funnel conversion rows virtualize 10000 100000 points") { NewIn = NextRelease },
		new(DataArea, "State timeline", "/charts/states", MokaIcons.Status.Activity, DevAppPageKind.Demo, "state timeline uptime status up down degraded maintenance unknown monitoring services devices links incidents availability rows virtualize") { NewIn = NextRelease },

		new(NavigationArea, "Navigation", "/navigation", MokaIcons.Navigation.Menu, DevAppPageKind.Showcase),
		new(NavigationArea, "App shell", "/demo-shell", MokaIcons.Content.Columns, DevAppPageKind.Pattern, "NavEntry HotkeyHost"),
		new(NavigationArea, "Breadcrumb", "/demo-breadcrumb", MokaIcons.Navigation.ChevronRight, DevAppPageKind.Demo, "Breadcrumb BreadcrumbItem"),
		new(NavigationArea, "Tabs overflow", "/demo/tabs-overflow", MokaIcons.Navigation.PanelTop, DevAppPageKind.Demo, "Tabs Overflow Menu") { NewIn = "0.3.16" },
		new(NavigationArea, "Shell polish", "/demo/shell-polish", MokaIcons.Content.Columns, DevAppPageKind.Demo, "SegmentedControl MatchControlHeight Breadcrumb Wrap Path Size GridBackground FillContent Justify Align EmptyState TextWrap AccountCard HeadingDetail HeadingContent Tabs IconOnly AppShell Framed") { NewIn = NextRelease },

		new(MediaArea, "Media", "/media", MokaIcons.Content.Image, DevAppPageKind.Showcase),
		new(MediaArea, "Gallery captions and actions", "/demo/gallery-actions", MokaIcons.Action.Download, DevAppPageKind.Demo, "MediaGallery TileCaption Lightbox LightboxAction Actions Download share theme elevation") { NewIn = NextRelease },
		new(MediaArea, "Misc", "/misc", MokaIcons.Navigation.MoreHorizontal, DevAppPageKind.Demo),

		new(ToolsGroup, "Theme editor", "/theme-editor", MokaIcons.Content.EyeDropper, DevAppPageKind.Tool),
		new(ToolsGroup, "Appearance panel", "/demo/appearance-panel", MokaIcons.Content.Palette, DevAppPageKind.Tool, "AppearancePanel Appearance Palette Accent") { NewIn = "0.3.16" },
		new(ToolsGroup, "Diagnostics", "/moka-diagnostics", MokaIcons.Status.Activity, DevAppPageKind.Tool, "Diagnostics overlay Ctrl+Shift+D render performance"),
		new(ToolsGroup, "Appearance", "/appearance", MokaIcons.Action.Sliders, DevAppPageKind.Tool, "settings look colors style menu dock drawer popover") { Bottom = true }
	];

	// Every page, grouped by area as the By area menu lists them. The palette, the page bar's title and All components
	// read this list whichever menu is showing.
	public static IReadOnlyList<MokaNavEntry> Entries { get; } = [.. Pages.Select(page => EntryFor(page, page.Area))];

	public static string PageName(string href) => MokaNavEntry.Find(Entries, href)?.Label ?? string.Empty;

	public static string AreaOf((string Name, string Href) component) =>
		AreaOverrides.TryGetValue(component.Name, out string? area)
			? area
			: PageAt(PathOf(component.Href))?.Area ?? string.Empty;

	public static int CountIn(string area) => Components.Count(component => AreaOf(component) == area);

	public static IReadOnlyList<MokaNavEntry> Menu(DevAppMenu menu, string path)
	{
		if (menu == DevAppMenu.ByArea)
		{
			return Entries;
		}

		string? open = OpenArea(path);
		return Menus.GetOrAdd((menu, open), key => key.Menu == DevAppMenu.TwoLevel ? TwoLevel(key.Area) : PatternsApart(key.Area));
	}

	// The area a path belongs to, when it's one a nested menu can open.
	private static string? OpenArea(string path)
	{
		MokaNavEntry? entry = MokaNavEntry.Find(Entries, path);
		DevAppPage? page = entry is null ? null : PageAt(entry.Route.Trim('/'));
		return page is { Kind: DevAppPageKind.Showcase or DevAppPageKind.Demo or DevAppPageKind.Pattern } && page.Area != ThemeArea
			? page.Area
			: null;
	}

	private static List<MokaNavEntry> TwoLevel(string? open)
	{
		List<MokaNavEntry> entries = [.. Leading()];
		foreach (string area in Areas.Skip(1))
		{
			entries.Add(AreaEntry(area));
			if (area != open)
			{
				continue;
			}

			entries.AddRange(Pages
				.Where(page => page.Area == area && page.Kind is DevAppPageKind.Demo or DevAppPageKind.Pattern)
				.Select(page => EntryFor(page, ComponentsGroup)));
			entries.AddRange(Components
				.Where(component => AreaOf(component) == area)
				.OrderBy(component => component.Name, StringComparer.OrdinalIgnoreCase)
				.Select(ComponentEntry));
		}

		entries.AddRange(Trailing());
		return entries;
	}

	private static List<MokaNavEntry> PatternsApart(string? open)
	{
		List<MokaNavEntry> entries = [.. Leading()];
		entries.AddRange(Pages.Where(page => page.Kind == DevAppPageKind.Pattern).Select(page => EntryFor(page, PatternsGroup)));
		foreach (string area in Areas.Skip(1))
		{
			entries.Add(AreaEntry(area));
			if (area == open)
			{
				entries.AddRange(Pages
					.Where(page => page.Area == area && page.Kind == DevAppPageKind.Demo)
					.Select(page => EntryFor(page, ComponentsGroup)));
			}
		}

		entries.AddRange(Trailing());
		return entries;
	}

	private static IEnumerable<MokaNavEntry> Leading() =>
		Pages.Where(page => page.Kind == DevAppPageKind.Home || page.Area == ThemeArea).Select(page => EntryFor(page, page.Area));

	private static IEnumerable<MokaNavEntry> Trailing() =>
		Pages.Where(page => page.Kind == DevAppPageKind.Tool).Select(page => EntryFor(page, page.Area));

	// An area is its showcase page, with how many components it holds.
	private static MokaNavEntry AreaEntry(string area)
	{
		DevAppPage showcase = Pages.First(page => page.Area == area && page.Kind == DevAppPageKind.Showcase);
		int count = CountIn(area);
		return EntryFor(showcase, ComponentsGroup) with
		{
			Pill = new MokaNavPill(count, MokaBadgeTone.Count, count == 1 ? "1 component" : $"{count} components")
		};
	}

	// A component goes to its section. One shown on a demo page of its own gets a fragment anyway, so no component
	// entry ever matches the address: the page's own entry is the one marked as current.
	private static MokaNavEntry ComponentEntry((string Name, string Href) component)
	{
		string href = component.Href.Contains('#', StringComparison.Ordinal) ? component.Href : $"{component.Href}#{component.Name}";
		return new MokaNavEntry(ComponentsGroup, component.Name, $"/{href}", MokaIcons.Content.Hash, $"Moka{component.Name}");
	}

	private static MokaNavEntry EntryFor(DevAppPage page, string group)
	{
		IEnumerable<string> shown = Components
			.Where(component => string.Equals(PathOf(component.Href), page.Path, StringComparison.Ordinal))
			.Select(component => component.Name);
		string keywords = string.Join(' ', shown.Append(page.Keywords)).Trim();
		return new MokaNavEntry(group, page.Label, page.Route, page.Icon, keywords, NewPill(page))
		{
			ImageSource = page.ImageSource,
			Bottom = page.Bottom
		};
	}

	// The menu draws the word New over a Ready pill, since a pill carries a number and no words.
	private static MokaNavPill? NewPill(DevAppPage page) => page.NewIn switch
	{
		null => null,
		NextRelease => new MokaNavPill(1, MokaBadgeTone.Ready, "New, not released yet"),
		string version => new MokaNavPill(1, MokaBadgeTone.Ready, $"New in {version}")
	};

	private static DevAppPage? PageAt(string path) =>
		Pages.FirstOrDefault(page => string.Equals(page.Path, path, StringComparison.OrdinalIgnoreCase));

	private static string PathOf(string href) => href.Split('?', '#')[0].Trim('/');
}
