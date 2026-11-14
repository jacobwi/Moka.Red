using Moka.Red.Core.Theming;
using Moka.Red.ThemeGen.Appearance;
using Moka.Red.ThemeGen.ImportExport;
using Moka.Red.ThemeGen.Preview;

namespace Moka.Red.ThemeGen;

/// <summary>Every word <see cref="MokaThemeStudio" /> writes. Change any with <c>new MokaThemeStudioText { ... }</c>.</summary>
public sealed record MokaThemeStudioText
{
	/// <summary>The heading of the controls column, which also names it.</summary>
	public string ControlsLabel { get; init; } = "Theme settings";

	/// <summary>The heading over the previews, which also names the middle column.</summary>
	public string PreviewHeading { get; init; } = "Preview";

	/// <summary>The heading of the inspector column, which also names it.</summary>
	public string InspectorHeading { get; init; } = "Inspector";

	/// <summary>The switch at the preview heading that turns on marking what a change touched.</summary>
	public string ShowChanges { get; init; } = "Show what changed";

	/// <summary>The line under the preview heading while changes are shown, until the first change.</summary>
	public string ChangeHint { get; init; } = "Change a setting and what it changes is marked in the preview.";

	/// <summary>
	///     The line under the preview heading after a change, given what changed and how many elements in the previews look
	///     different because of it. Screen readers hear it too.
	/// </summary>
	public Func<string, int, string> ChangeSummary { get; init; } = (what, count) => count switch
	{
		0 => $"{what}: nothing in the preview uses this choice.",
		1 => $"{what}: 1 element changed.",
		_ => $"{what}: {count} elements changed."
	};

	/// <summary>
	///     Said in place of <see cref="ChangeSummary" /> when a style row changed nothing on screen because what it styles
	///     shows only once something in the preview opens, or only in print. Given the row's name and the row; null for a
	///     row with no such place, which then says nothing in the preview uses it.
	/// </summary>
	public Func<string, MokaAppearanceRows, string?> ShownLater { get; init; } = (what, row) => row switch
	{
		MokaAppearanceRows.Dialogs => $"{what}: open the dialog in the preview to see this choice.",
		MokaAppearanceRows.Toasts or MokaAppearanceRows.ToastStacking => $"{what}: show a few toasts in the preview to see this choice.",
		MokaAppearanceRows.Popups or MokaAppearanceRows.Lists => $"{what}: open the owner list in the preview to see this choice.",
		MokaAppearanceRows.Tooltips or MokaAppearanceRows.TooltipArrows or MokaAppearanceRows.TooltipMotion or MokaAppearanceRows.TooltipSize =>
			$"{what}: point at the sync status in the preview's status bar to see this choice.",
		MokaAppearanceRows.ChartTexture => $"{what}: this choice shows when the page is printed.",
		_ => null
	};

	/// <summary>The heading over the start list.</summary>
	public string StartHeading { get; init; } = "Start from";

	/// <summary>The name of the start that holds a theme the app passed in, while it matches none of the starts.</summary>
	public string CurrentStart { get; init; } = "Current theme";

	/// <summary>The line under that start's name.</summary>
	public string CurrentStartHint { get; init; } = "The theme you opened";

	/// <summary>The heading over the seed colors.</summary>
	public string ColorsHeading { get; init; } = "Colors";

	/// <summary>The line under the colors heading.</summary>
	public string ColorsHint { get; init; } = "Every other color is worked out from these, and text keeps its contrast.";

	/// <summary>The line shown in place of the seed colors on the original design, which keeps its own colors.</summary>
	public string SeedsNeedNewDesign { get; init; } = "The original design keeps its own colors. Start from another design to derive them from seeds.";

	/// <summary>The accent seed's label.</summary>
	public string Accent { get; init; } = "Accent";

	/// <summary>The neutral seed's label.</summary>
	public string Neutral { get; init; } = "Neutral";

	/// <summary>The success seed's label.</summary>
	public string Success { get; init; } = "Good";

	/// <summary>The warning seed's label.</summary>
	public string Warning { get; init; } = "Warning";

	/// <summary>The error seed's label.</summary>
	public string Error { get; init; } = "Error";

	/// <summary>The info seed's label.</summary>
	public string Info { get; init; } = "Info";

	/// <summary>The helper line under a seed that holds a color of its own.</summary>
	public string SeedOwn { get; init; } = "Your color";

	/// <summary>The helper line under a seed that keeps the start's color.</summary>
	public string SeedFromStart { get; init; } = "From the start";

	/// <summary>The button that puts a seed back to the start's color, named after the seed by the caller.</summary>
	public Func<string, string> ResetSeed { get; init; } = seed => $"Put {seed} back";

	/// <summary>The color picker's name inside a seed's field.</summary>
	public string PickColor { get; init; } = "Choose a color";

	/// <summary>The contrast row's label.</summary>
	public string Contrast { get; init; } = "Contrast";

	/// <summary>The standard contrast level.</summary>
	public string ContrastStandard { get; init; } = "AA";

	/// <summary>The higher contrast level.</summary>
	public string ContrastMore { get; init; } = "AAA";

	/// <summary>The high contrast switch.</summary>
	public string HighContrast { get; init; } = "High contrast";

	/// <summary>The neutral strength slider's label.</summary>
	public string NeutralStrength { get; init; } = "Neutral strength";

	/// <summary>The line under the neutral strength slider while it follows the neutral's own strength.</summary>
	public string NeutralStrengthOwn { get; init; } = "The neutral's own strength";

	/// <summary>The button that lets the neutral strength follow the neutral again.</summary>
	public string NeutralStrengthReset { get; init; } = "Use the neutral's own";

	/// <summary>The heading over the style rows.</summary>
	public string StyleHeading { get; init; } = "Style";

	/// <summary>The accessible name of the scheme choice over the previews.</summary>
	public string ViewLabel { get; init; } = "Schemes shown";

	/// <summary>The dark scheme.</summary>
	public string Dark { get; init; } = "Dark";

	/// <summary>The light scheme.</summary>
	public string Light { get; init; } = "Light";

	/// <summary>Both schemes side by side.</summary>
	public string Both { get; init; } = "Both";

	/// <summary>The accessible name of the scheme choice over the inspector, while both schemes show.</summary>
	public string InspectLabel { get; init; } = "Scheme inspected";

	/// <summary>The accessible name of the inspector's tabs.</summary>
	public string InspectorLabel { get; init; } = "Inspect the theme";

	/// <summary>The contrast tab.</summary>
	public string ContrastTab { get; init; } = "Contrast";

	/// <summary>The tokens tab.</summary>
	public string TokensTab { get; init; } = "Tokens";

	/// <summary>The export tab.</summary>
	public string ExportTab { get; init; } = "Export";

	/// <summary>The line over the contrast report, given the number of pairs that fail and the number measured.</summary>
	public Func<int, int, string> ContrastSummary { get; init; } = (failing, total) => failing == 0
		? $"All {total} pairs meet their target."
		: $"{failing} of {total} pairs miss their target.";

	/// <summary>The sample text drawn in a pair's two colors.</summary>
	public string Sample { get; init; } = "Aa";

	/// <summary>What draws a pair. Default is the report's own words (<see cref="MokaContrastCheck.Use" />).</summary>
	public Func<MokaContrastCheck, string> CheckUse { get; init; } = check => check.Use;

	/// <summary>The word on a pair that meets its target.</summary>
	public string Passes { get; init; } = "Pass";

	/// <summary>The word on a pair that misses its target.</summary>
	public string Fails { get; init; } = "Fail";

	/// <summary>The word on a pair listed for information.</summary>
	public string Informational { get; init; } = "Info";

	/// <summary>A pair's measurements, given its WCAG ratio, its APCA Lc and the ratio it needs (0 when nothing is).</summary>
	public Func<double, double, double, string> ContrastFigures { get; init; } = (ratio, lc, needed) => needed > 0
		? FormattableString.Invariant($"{ratio:0.00}:1, Lc {lc:0}, needs {needed:0.#}:1")
		: FormattableString.Invariant($"{ratio:0.00}:1, Lc {lc:0}");

	/// <summary>The line naming the pairs whose tokens didn't resolve, given how many.</summary>
	public Func<int, string> Unresolved { get; init; } = count => $"{count} pairs couldn't be measured.";

	/// <summary>The token search field's label.</summary>
	public string SearchTokens { get; init; } = "Search tokens";

	/// <summary>The line shown when no token matches the search.</summary>
	public string NoTokens { get; init; } = "No token matches.";

	/// <summary>The name of each token group, as its heading.</summary>
	public Func<MokaTokenGroup, string> GroupName { get; init; } = group => group switch
	{
		MokaTokenGroup.Color => "Colors",
		MokaTokenGroup.Label => "Label colors",
		MokaTokenGroup.Chart => "Chart colors",
		MokaTokenGroup.Code => "Code colors",
		MokaTokenGroup.Typography => "Type",
		MokaTokenGroup.Spacing => "Spacing",
		MokaTokenGroup.Radius => "Corners",
		MokaTokenGroup.Layout => "Layout",
		MokaTokenGroup.Elevation => "Depth",
		MokaTokenGroup.Motion => "Motion",
		MokaTokenGroup.Layer => "Layers",
		MokaTokenGroup.Interaction => "Focus and selection",
		MokaTokenGroup.Component => "Components",
		_ => "Overlays"
	};

	/// <summary>The button that pins a token.</summary>
	public string Pin { get; init; } = "Pin";

	/// <summary>The accessible name of the pin button, given the token.</summary>
	public Func<string, string> PinLabel { get; init; } = token => $"Pin {token}";

	/// <summary>The tag on a pinned token.</summary>
	public string Pinned { get; init; } = "Pinned";

	/// <summary>The button that lets a pinned token go.</summary>
	public string Unpin { get; init; } = "Reset";

	/// <summary>The accessible name of the reset button, given the token.</summary>
	public Func<string, string> UnpinLabel { get; init; } = token => $"Reset {token}";

	/// <summary>The button that lets every pin go in the scheme inspected.</summary>
	public string UnpinAll { get; init; } = "Reset all pins";

	/// <summary>The field's label while a token is being pinned.</summary>
	public string PinValue { get; init; } = "Value";

	/// <summary>The button that applies a pin.</summary>
	public string ApplyPin { get; init; } = "Apply";

	/// <summary>The button that closes the pin editor without pinning.</summary>
	public string CancelPin { get; init; } = "Cancel";

	/// <summary>The error under the pin field when the value isn't one the token takes.</summary>
	public string PinInvalid { get; init; } = "That isn't a value this token takes.";

	/// <summary>The line under a color pin's field: a color pin holds in the scheme inspected only.</summary>
	public string PinColorHint { get; init; } = "Holds in this scheme only.";

	/// <summary>The line under any other pin's field: it holds in both schemes.</summary>
	public string PinOtherHint { get; init; } = "Holds in both schemes.";

	/// <summary>The words of the embedded style rows.</summary>
	public MokaAppearanceText Appearance { get; init; } = new();

	/// <summary>The words of the sample app in the previews.</summary>
	public MokaThemePreviewText Preview { get; init; } = new();

	/// <summary>The words of the export tab.</summary>
	public MokaThemeImportExportText Export { get; init; } = new();
}
