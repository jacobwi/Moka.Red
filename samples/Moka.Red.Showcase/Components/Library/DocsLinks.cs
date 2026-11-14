using System.Text.RegularExpressions;

namespace Moka.Red.Showcase.Components.Library;

// The docs site's own nav (docs/mokadocs.yaml, embedded at build) decides which pages exist, so a link never points at
// a page the site dropped.
internal static partial class DocsLinks
{
	public const string Site = "https://jacobwi.github.io/Moka.Red";

	// Components documented on a page that covers several of them.
	private static readonly Dictionary<string, string> Shared = new(StringComparer.Ordinal)
	{
		["AccountCard"] = "/components/app-shell",
		["AppBar"] = "/layout/toolbar-appbar",
		["AutoComplete"] = "/components/forms",
		["Breadcrumb"] = "/components/navigation",
		["Carousel"] = "/components/media",
		["Checkbox"] = "/components/forms",
		["ColorPicker"] = "/components/forms",
		["Container"] = "/layout/grid-flexbox",
		["CurrencyInput"] = "/components/forms",
		["DatePicker"] = "/components/forms",
		["EmojiPanel"] = "/components/emoji-picker",
		["EmojiTypeahead"] = "/components/emoji-picker",
		["FieldWrapper"] = "/components/forms",
		["FileUpload"] = "/components/forms",
		["Flexbox"] = "/layout/grid-flexbox",
		["FloatingActionButton"] = "/components/fab",
		["Grid"] = "/layout/grid-flexbox",
		["Image"] = "/components/media",
		["KanbanBoard"] = "/components/kanban",
		["LoadingOverlay"] = "/components/progress",
		["MediaGallery"] = "/components/media",
		["Menu"] = "/components/navigation",
		["NotificationCenter"] = "/components/notification",
		["NumericField"] = "/components/forms",
		["OtpInput"] = "/components/forms",
		["Panel"] = "/layout/panel-paper",
		["Paper"] = "/layout/panel-paper",
		["PasswordField"] = "/components/forms",
		["PhoneInput"] = "/components/forms",
		["RadioGroup"] = "/components/forms",
		["Rating"] = "/components/forms",
		["SearchInput"] = "/components/forms",
		["SignaturePad"] = "/components/forms",
		["Skeleton"] = "/components/progress",
		["Slider"] = "/components/forms",
		["Spinner"] = "/components/progress",
		["StatusBar"] = "/layout/toolbar-appbar",
		["Stepper"] = "/components/navigation",
		["Switch"] = "/components/forms",
		["TabContainer"] = "/components/tabs",
		["TabPresence"] = "/components/page-title",
		["TabStrip"] = "/components/tabs",
		["Tabs"] = "/components/page-tabs",
		["TagInput"] = "/components/forms",
		["TextArea"] = "/components/forms",
		["TextField"] = "/components/forms",
		["ThemeProvider"] = "/getting-started/theming",
		["TimePicker"] = "/components/forms",
		["Toolbar"] = "/layout/toolbar-appbar",
		["ValidationSummary"] = "/components/forms",
		["VideoEmbed"] = "/components/media"
	};

	private static readonly Lazy<HashSet<string>> Paths = new(Load);

	public static string? For(string name, string slug)
	{
		string[] candidates = Shared.TryGetValue(name, out string? shared)
			? [shared]
			: [$"/components/{slug}", $"/layout/{slug}"];
		string? found = candidates.FirstOrDefault(Paths.Value.Contains);
		return found is null ? null : Site + found;
	}

	private static HashSet<string> Load()
	{
		using Stream? stream = typeof(DocsLinks).Assembly.GetManifestResourceStream("mokadocs.yaml");
		if (stream is null)
		{
			return [];
		}

		using StreamReader reader = new(stream);
		return [.. PathLine().Matches(reader.ReadToEnd()).Select(match => match.Groups["path"].Value)];
	}

	[GeneratedRegex(@"^\s*path:\s*(?<path>/\S*)", RegexOptions.Multiline)]
	private static partial Regex PathLine();
}
