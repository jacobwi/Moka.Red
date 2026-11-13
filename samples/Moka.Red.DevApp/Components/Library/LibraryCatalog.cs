using System.Reflection;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;
using Moka.Red.DevApp.Components.Layout;
using Moka.Red.Icons;

namespace Moka.Red.DevApp.Components.Library;

internal sealed record LibraryComponent(string Name, string TypeName, string Package, string Href, string Where);

internal sealed record LibraryPackage(
	string Assembly,
	string Label,
	string Href,
	MokaIconDefinition Icon,
	IReadOnlyList<LibraryComponent> Components);

internal sealed record LibraryPreset(string Name, MokaTheme Theme);

// What the overview counts comes from the assemblies themselves, so the numbers can't drift from the library.
internal static class LibraryCatalog
{
	private const string Prefix = "Moka.Red";

	private static readonly Dictionary<string, (string Label, string Href, MokaIconDefinition Icon)> KnownAreas =
		new(StringComparer.Ordinal)
		{
			["Moka.Red.Core"] = ("Core", "/tokens", MokaIcons.Action.Sun),
			["Moka.Red.Primitives"] = ("Primitives", "/primitives", MokaIcons.Toggle.Star),
			["Moka.Red.Layout"] = ("Layout", "/layout", MokaIcons.Content.RowsDense),
			["Moka.Red.Forms"] = ("Forms", "/forms", MokaIcons.Action.Edit),
			["Moka.Red.Feedback"] = ("Feedback", "/feedback", MokaIcons.Status.Bell),
			["Moka.Red.Data"] = ("Data", "/data", MokaIcons.File.Database),
			["Moka.Red.Navigation"] = ("Navigation", "/navigation", MokaIcons.Navigation.Menu),
			["Moka.Red.ContextMenu"] = ("Context menu", "/context-menu", MokaIcons.Navigation.MoreVertical),
			["Moka.Red.Diagnostics"] = ("Diagnostics", "/misc#Diagnostics", MokaIcons.Status.Activity),
			["Moka.Red.ThemeGen"] = ("Theming", "/theme-editor", MokaIcons.Content.Palette)
		};

	private static readonly Lazy<Snapshot> Data = new(Load);

	public static string Version { get; } =
		typeof(MokaTheme).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

	public static int PackageCount => Data.Value.PackageCount;

	public static IReadOnlyList<LibraryPackage> Areas => Data.Value.Areas;

	public static IReadOnlyList<LibraryComponent> Components => Data.Value.Components;

	public static IReadOnlyList<LibraryPreset> Presets => Data.Value.Presets;

	public static string AllHref => "/components";

	public static string HrefFor(string name) => Shown(name, exactOnly: false)?.Href ?? AllHref;

	// A component the assemblies hold, or else a name the dev app shows as a feature rather than one type, such as
	// Toast, which MokaToastHost draws, or FilePicker.
	public static (string Href, string Where)? Shown(string name, bool exactOnly)
	{
		LibraryComponent? found = Components.FirstOrDefault(component =>
			string.Equals(component.Name, name, StringComparison.Ordinal));
		if (found is not null)
		{
			return (found.Href, found.Where);
		}

		foreach ((string shownName, string href) in DevAppCatalog.Components)
		{
			if (string.Equals(shownName, name, StringComparison.Ordinal))
			{
				string rooted = Rooted(href);
				return (rooted, Where(rooted, "All components"));
			}
		}

		return exactOnly || Shown(name) is not { } part ? null : (part, Where(part, "All components"));
	}

	// Every word has to appear in the name, the type, the package or the page it is shown on. A name that starts
	// with what was typed comes first.
	public static IReadOnlyList<LibraryComponent> Find(string query)
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
				.Where(component => words.All(word =>
					$"{component.Name} {component.TypeName} {component.Package} {component.Where}"
						.Contains(word, StringComparison.OrdinalIgnoreCase)))
				.OrderBy(component => Rank(component, trimmed))
				.ThenBy(component => component.Name, StringComparer.OrdinalIgnoreCase)
		];
	}

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
		Assembly[] assemblies = LoadLibraryAssemblies();
		var areas = new List<LibraryPackage>();
		foreach (Assembly assembly in assemblies.OrderBy(a => a.GetName().Name, StringComparer.Ordinal))
		{
			string assemblyName = assembly.GetName().Name ?? string.Empty;
			Type[] types = ComponentTypes(assembly);
			if (types.Length == 0)
			{
				continue;
			}

			(string label, string href, MokaIconDefinition icon) = KnownAreas.TryGetValue(assemblyName, out var known)
				? known
				: (assemblyName[(Prefix.Length + 1)..], AllHref, MokaIcons.File.Package);

			LibraryComponent[] components =
			[
				.. types
					.Select(type => Describe(type, label, href))
					.DistinctBy(component => component.Name, StringComparer.Ordinal)
					.OrderBy(component => component.Name, StringComparer.OrdinalIgnoreCase)
			];
			areas.Add(new LibraryPackage(assemblyName, label, href, icon, components));
		}

		LibraryPackage[] ordered = [.. areas.OrderByDescending(area => area.Components.Count)];
		return new Snapshot(
			assemblies.Length,
			ordered,
			[.. ordered.SelectMany(area => area.Components).OrderBy(component => component.Name, StringComparer.OrdinalIgnoreCase)],
			LoadPresets());
	}

	// The build copies every referenced project next to the app, so the folder is the list of packages: the
	// app's own references leave out a package it never names in code.
	private static Assembly[] LoadLibraryAssemblies()
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

	private static Type[] ComponentTypes(Assembly assembly) =>
	[
		.. assembly.GetExportedTypes()
			.Where(type => type is { IsClass: true, IsAbstract: false }
				&& type.IsSubclassOf(typeof(ComponentBase))
				&& type.Namespace?.StartsWith($"{Prefix}.", StringComparison.Ordinal) == true)
	];

	private static LibraryComponent Describe(Type type, string area, string areaHref)
	{
		string typeName = type.Name;
		int tick = typeName.IndexOf('`', StringComparison.Ordinal);
		if (tick >= 0)
		{
			typeName = typeName[..tick];
		}

		string name = typeName.StartsWith("Moka", StringComparison.Ordinal) && typeName.Length > 4
			? typeName[4..]
			: typeName;
		string? href = Shown(name);
		return href is null
			? new LibraryComponent(name, typeName, area, areaHref, area)
			: new LibraryComponent(name, typeName, area, href, Where(href, area));
	}

	// The dev app's own list says where a component is shown. A part with no entry of its own, such as
	// AccordionItem, goes where its whole is shown.
	private static string? Shown(string name)
	{
		string? whole = null;
		int wholeLength = 0;
		foreach ((string shownName, string href) in DevAppCatalog.Components)
		{
			if (string.Equals(shownName, name, StringComparison.Ordinal))
			{
				return Rooted(href);
			}

			bool isPart = name.Length > shownName.Length
				&& name.StartsWith(shownName, StringComparison.Ordinal)
				&& char.IsUpper(name[shownName.Length]);
			if (isPart && shownName.Length > wholeLength)
			{
				whole = href;
				wholeLength = shownName.Length;
			}
		}

		return whole is null ? null : Rooted(whole);
	}

	private static string Where(string href, string area)
	{
		string page = DevAppCatalog.PageName(href);
		return page.Length > 0 ? page : area;
	}

	private static string Rooted(string href) => href.StartsWith('/') ? href : $"/{href}";

	private static LibraryPreset[] LoadPresets()
	{
		IEnumerable<(string Name, MokaTheme Theme)> found = typeof(MokaTheme)
			.GetProperties(BindingFlags.Public | BindingFlags.Static)
			.Where(property => property.PropertyType == typeof(MokaTheme) && property.GetIndexParameters().Length == 0)
			.Select(property => (property.Name, Theme: (MokaTheme)property.GetValue(null)!));

		var presets = new List<LibraryPreset>();
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

	private sealed record Snapshot(
		int PackageCount,
		IReadOnlyList<LibraryPackage> Areas,
		IReadOnlyList<LibraryComponent> Components,
		IReadOnlyList<LibraryPreset> Presets);
}
