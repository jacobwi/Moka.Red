using System.Text.Json;
using Moka.Red.Core.Theming;
using Moka.Red.Data.Table;

namespace Moka.Red.ThemeGen.Serialization;

// A style choice the theme holds as one enum property. The JSON writer and reader, the C# initializer and the With
// chain all walk All, so a new choice on MokaTheme is one row there and nothing else in the serializer. Its JSON key is
// the property's name in camelCase. A nullable property is written only while it holds a value, and read back as null
// when the JSON leaves it out.
internal abstract class ThemeChoice(string property, string withMethod)
{
	public static readonly IReadOnlyList<ThemeChoice> All =
	[
		Of(nameof(MokaTheme.MainButton), nameof(MokaTheme.WithMainButton), theme => theme.MainButton,
			(theme, value) => theme with { MainButton = value }, (theme, value) => theme.WithMainButton(value)),
		Of(nameof(MokaTheme.Selection), nameof(MokaTheme.WithSelection), theme => theme.Selection,
			(theme, value) => theme with { Selection = value }, (theme, value) => theme.WithSelection(value)),
		Of(nameof(MokaTheme.CurrentPlace), nameof(MokaTheme.WithCurrentPlace), theme => theme.CurrentPlace,
			(theme, value) => theme with { CurrentPlace = value }, (theme, value) => theme.WithCurrentPlace(value)),
		Of(nameof(MokaTheme.Elevation), nameof(MokaTheme.WithElevation), theme => theme.Elevation,
			(theme, value) => theme with { Elevation = value }, (theme, value) => theme.WithElevation(value)),
		Of(nameof(MokaTheme.TextButtons), nameof(MokaTheme.WithTextButtons), theme => theme.TextButtons,
			(theme, value) => theme with { TextButtons = value }, (theme, value) => theme.WithTextButtons(value)),
		Of(nameof(MokaTheme.Callouts), nameof(MokaTheme.WithCallouts), theme => theme.Callouts,
			(theme, value) => theme with { Callouts = value }, (theme, value) => theme.WithCallouts(value)),
		Of(nameof(MokaTheme.Switches), nameof(MokaTheme.WithSwitches), theme => theme.Switches,
			(theme, value) => theme with { Switches = value }, (theme, value) => theme.WithSwitches(value)),
		Of(nameof(MokaTheme.SettingsHeaders), nameof(MokaTheme.WithSettingsHeaders), theme => theme.SettingsHeaders,
			(theme, value) => theme with { SettingsHeaders = value }, (theme, value) => theme.WithSettingsHeaders(value)),
		Of(nameof(MokaTheme.DisabledButtons), nameof(MokaTheme.WithDisabledButtons), theme => theme.DisabledButtons,
			(theme, value) => theme with { DisabledButtons = value }, (theme, value) => theme.WithDisabledButtons(value)),
		Of(nameof(MokaTheme.StatusBar), nameof(MokaTheme.WithStatusBar), theme => theme.StatusBar,
			(theme, value) => theme with { StatusBar = value }, (theme, value) => theme.WithStatusBar(value)),
		Of(nameof(MokaTheme.Docks), nameof(MokaTheme.WithDocks), theme => theme.Docks,
			(theme, value) => theme with { Docks = value }, (theme, value) => theme.WithDocks(value)),
		Of(nameof(MokaTheme.LabelTone), nameof(MokaTheme.WithLabelTone), theme => theme.LabelTone,
			(theme, value) => theme with { LabelTone = value }, (theme, value) => theme.WithLabelTone(value)),
		Of(nameof(MokaTheme.Toasts), nameof(MokaTheme.WithToasts), theme => theme.Toasts,
			(theme, value) => theme with { Toasts = value }, (theme, value) => theme.WithToasts(value)),
		Of(nameof(MokaTheme.ToastStacking), nameof(MokaTheme.WithToastStacking), theme => theme.ToastStacking,
			(theme, value) => theme with { ToastStacking = value }, (theme, value) => theme.WithToastStacking(value)),
		Of(nameof(MokaTheme.Notifications), nameof(MokaTheme.WithNotifications), theme => theme.Notifications,
			(theme, value) => theme with { Notifications = value }, (theme, value) => theme.WithNotifications(value)),
		Of(nameof(MokaTheme.Popups), nameof(MokaTheme.WithPopups), theme => theme.Popups,
			(theme, value) => theme with { Popups = value }, (theme, value) => theme.WithPopups(value)),
		Of(nameof(MokaTheme.Lists), nameof(MokaTheme.WithLists), theme => theme.Lists,
			(theme, value) => theme with { Lists = value }, (theme, value) => theme.WithLists(value)),
		Of(nameof(MokaTheme.Dialogs), nameof(MokaTheme.WithDialogs), theme => theme.Dialogs,
			(theme, value) => theme with { Dialogs = value }, (theme, value) => theme.WithDialogs(value)),
		Of(nameof(MokaTheme.Buttons), nameof(MokaTheme.WithButtons), theme => theme.Buttons,
			(theme, value) => theme with { Buttons = value }, (theme, value) => theme.WithButtons(value)),
		Of(nameof(MokaTheme.Tooltips), nameof(MokaTheme.WithTooltips), theme => theme.Tooltips,
			(theme, value) => theme with { Tooltips = value }, (theme, value) => theme.WithTooltips(value)),
		Of(nameof(MokaTheme.TooltipMotion), nameof(MokaTheme.WithTooltipMotion), theme => theme.TooltipMotion,
			(theme, value) => theme with { TooltipMotion = value }, (theme, value) => theme.WithTooltipMotion(value)),
		Of(nameof(MokaTheme.TooltipSize), nameof(MokaTheme.WithTooltipSize), theme => theme.TooltipSize,
			(theme, value) => theme with { TooltipSize = value }, (theme, value) => theme.WithTooltipSize(value)),
		Of(nameof(MokaTheme.Charts), nameof(MokaTheme.WithCharts), theme => theme.Charts,
			(theme, value) => theme with { Charts = value }, (theme, value) => theme.WithCharts(value)),
		Of(nameof(MokaTheme.ChartPalette), nameof(MokaTheme.WithChartPalette), theme => theme.ChartPalette,
			(theme, value) => theme with { ChartPalette = value }, (theme, value) => theme.WithChartPalette(value)),
		Of(nameof(MokaTheme.ChartTexture), nameof(MokaTheme.WithChartTexture), theme => theme.ChartTexture,
			(theme, value) => theme with { ChartTexture = value }, (theme, value) => theme.WithChartTexture(value)),
		Of(nameof(MokaTheme.ChartPrint), nameof(MokaTheme.WithChartPrint), theme => theme.ChartPrint,
			(theme, value) => theme with { ChartPrint = value }, (theme, value) => theme.WithChartPrint(value)),
		Optional(nameof(MokaTheme.Tables), nameof(MokaTheme.WithTables), theme => theme.Tables,
			(theme, value) => theme with { Tables = value }, (theme, value) => theme.WithTables(value)),
		Optional(nameof(MokaTheme.TableDensity), nameof(MokaTheme.WithTableDensity), theme => theme.TableDensity,
			(theme, value) => theme with { TableDensity = value }, (theme, value) => theme.WithTableDensity(value))
	];

	public string Property { get; } = property;

	public string Key { get; } = JsonNamingPolicy.CamelCase.ConvertName(property);

	public string WithMethod { get; } = withMethod;

	public static ThemeChoice? Find(string key) =>
		All.FirstOrDefault(choice => string.Equals(choice.Key, key, StringComparison.OrdinalIgnoreCase));

	// The enum value's name, as the JSON holds it, or null while a nullable property holds none.
	public abstract string? NameIn(MokaTheme theme);

	// The enum value as C#, such as MokaDockLook.Ide, or null while a nullable property holds none.
	public abstract string? CodeIn(MokaTheme theme);

	// The value under the choice's key, as a change to make to the theme once every value has been read. A value it can't
	// read says so in error, and the theme is then never built.
	public abstract Func<MokaTheme, MokaTheme> Read(JsonElement value, ref string? error);

	public abstract void ChooseIn(ThemeRecipe recipe, MokaTheme target);

	// The C# for a value of an enum. The code is written for a file that imports Moka.Red.Core.Theming alone, so an enum
	// kept under another namespace, as the table's are, is written in full.
	public static string Code<TChoice>(TChoice value) where TChoice : struct, Enum =>
		typeof(TChoice).Namespace == typeof(MokaTheme).Namespace ? $"{typeof(TChoice).Name}.{value}" : $"{typeof(TChoice).FullName}.{value}";

	private static ThemeChoice<TChoice> Of<TChoice>(string property, string withMethod, Func<MokaTheme, TChoice> get,
		Func<MokaTheme, TChoice, MokaTheme> set, Func<MokaTheme, TChoice, MokaTheme> with)
		where TChoice : struct, Enum => new(property, withMethod, theme => get(theme), set, with);

	private static ThemeChoice<TChoice> Optional<TChoice>(string property, string withMethod, Func<MokaTheme, TChoice?> get,
		Func<MokaTheme, TChoice, MokaTheme> set, Func<MokaTheme, TChoice, MokaTheme> with)
		where TChoice : struct, Enum => new(property, withMethod, get, set, with);
}

internal sealed class ThemeChoice<TChoice>(
	string property,
	string withMethod,
	Func<MokaTheme, TChoice?> get,
	Func<MokaTheme, TChoice, MokaTheme> set,
	Func<MokaTheme, TChoice, MokaTheme> with) : ThemeChoice(property, withMethod)
	where TChoice : struct, Enum
{
	public override string? NameIn(MokaTheme theme) => get(theme)?.ToString();

	public override string? CodeIn(MokaTheme theme) => get(theme) is { } value ? Code(value) : null;

	public override Func<MokaTheme, MokaTheme> Read(JsonElement value, ref string? error)
	{
		TChoice choice = MokaThemeSerializer.ReadChoice(value, Key, default(TChoice), ref error);
		return theme => set(theme, choice);
	}

	public override void ChooseIn(ThemeRecipe recipe, MokaTheme target) => recipe.Choose(target, WithMethod, with);
}
