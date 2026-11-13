using System.ComponentModel;
using System.Globalization;
using System.Text;
using ModelContextProtocol.Server;

namespace Moka.Red.Mcp;

[McpServerToolType]
internal sealed class MokaRedTools(ComponentCatalog catalog, DocsLibrary docs)
{
	private const int SummaryLength = 140;

	private static readonly string[] DefaultGuides = ["installation", "quick-start"];

	[McpServerTool(Name = "list_components", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
	[Description("Lists every Moka.Red component by package. Names only; use search_components to find one by what it does.")]
	public string ListComponents(
		[Description("Only this package, such as Forms, Data or Primitives. Leave empty for all.")]
		string? package = null)
	{
		IEnumerable<IGrouping<string, MokaComponent>> groups = catalog.Components
			.Where(component => string.IsNullOrWhiteSpace(package)
			                    || component.Category.Equals(package.Trim(), StringComparison.OrdinalIgnoreCase)
			                    || component.Package.Equals(package.Trim(), StringComparison.OrdinalIgnoreCase))
			.GroupBy(component => component.Category)
			.OrderBy(group => group.Key, StringComparer.Ordinal);

		StringBuilder text = new();
		text.AppendLine(CultureInfo.InvariantCulture,
			$"Moka.Red {MokaRedMcpServer.LibraryVersion}: {catalog.Components.Count} components. get_component gives one's parameters, get_component_docs its examples.");

		int shown = 0;
		foreach (IGrouping<string, MokaComponent> group in groups)
		{
			text.AppendLine()
				.AppendLine(CultureInfo.InvariantCulture, $"## {group.Key} ({group.Count()})")
				.AppendLine(string.Join(", ", group.Select(component => component.DisplayName)));
			shown++;
		}

		if (shown == 0)
		{
			text.AppendLine()
				.AppendLine(CultureInfo.InvariantCulture, $"No package named '{package}'. Packages: {string.Join(", ", Packages())}.");
		}

		return text.ToString().TrimEnd();
	}

	[McpServerTool(Name = "search_components", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
	[Description("Finds Moka.Red components by name or by what they do, such as 'date range', 'toast' or 'data grid'.")]
	public string SearchComponents(
		[Description("Words to search for in component names, summaries, docs and parameter names.")]
		string query,
		[Description("Most results to return.")]
		int limit = 15)
	{
		List<(MokaComponent Component, int Score)> matches = Search(query)
			.Take(Math.Clamp(limit, 1, 50))
			.ToList();

		// Some features are a service and a page more than a component (toasts are IMokaToastService), so name the
		// docs pages and types that match too.
		string[] words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		HashSet<string> listedPages = matches
			.Select(match => docs.ForComponent(match.Component.Name)?.Slug)
			.OfType<string>()
			.ToHashSet(StringComparer.Ordinal);
		List<string> pages = docs.ComponentPages
			.Where(page => !listedPages.Contains(page.Slug)
			               && words.All(word => page.Title.Contains(word, StringComparison.OrdinalIgnoreCase)))
			.Select(page => $"`{page.Slug}`")
			.Take(5)
			.ToList();
		List<string> types = catalog.Types
			.Where(type => type.IsPublic && words.All(word => type.Name.Contains(word, StringComparison.OrdinalIgnoreCase)))
			.Select(type => $"`{TypeNames.Of(type)}`")
			.Order(StringComparer.Ordinal)
			.Take(8)
			.ToList();

		if (matches.Count == 0 && pages.Count == 0 && types.Count == 0)
		{
			return $"No Moka.Red component matches '{query}'. list_components shows them all.";
		}

		StringBuilder text = new();
		foreach ((MokaComponent component, _) in matches)
		{
			string summary = Shorten(SummaryOf(component), SummaryLength);
			text.AppendLine(CultureInfo.InvariantCulture,
				$"- {component.DisplayName} ({component.Category}){(summary.Length == 0 ? "" : $": {summary}")}");
		}

		if (pages.Count > 0)
		{
			text.AppendLine(CultureInfo.InvariantCulture, $"Docs pages (get_component_docs): {string.Join(", ", pages)}");
		}

		if (types.Count > 0)
		{
			text.AppendLine(CultureInfo.InvariantCulture, $"Types (get_type): {string.Join(", ", types)}");
		}

		return text.ToString().TrimEnd();
	}

	[McpServerTool(Name = "get_component", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
	[Description("Gives a Moka.Red component's parameters with types, defaults and descriptions, its events, child content slots and enum values. Read it before writing the component's markup.")]
	public string GetComponent(
		[Description("Component name, such as MokaButton, Button or MokaTable.")]
		string name)
	{
		MokaComponent? component = catalog.Find(name);
		if (component is null)
		{
			return NotFound(name);
		}

		DocsPage? page = docs.ForComponent(component.Name);
		StringBuilder text = new();
		text.AppendLine(CultureInfo.InvariantCulture, $"# {component.DisplayName}")
			.AppendLine(CultureInfo.InvariantCulture,
				$"Package `{component.Package}`, namespace `{component.Namespace}`{(page is null ? "" : $", docs page `{page.Slug}` (get_component_docs)")}.");

		string? summary = SummaryOf(component);
		if (!string.IsNullOrWhiteSpace(summary))
		{
			text.AppendLine().AppendLine(summary);
		}

		// A shared parameter the component's docs table lists (Variant on a button) matters to it, so it gets a full
		// row; the rest (Margin, Class and the like) are only named.
		List<ComponentParameter> own = component.Parameters
			.Where(parameter => !catalog.CommonBases.Contains(parameter.DeclaredBy)
			                    || docs.Parameter(component.Name, parameter.Name) is not null)
			.ToList();

		AppendTable(text, "Parameters", Rows(component, own, ParameterKind.Value), withDefault: true);
		AppendTable(text, "Events", Rows(component, own, ParameterKind.Event), withDefault: false);
		AppendTable(text, "Child content", Rows(component, own, ParameterKind.Content), withDefault: false);
		AppendTable(text, "Cascading parameters", Rows(component, own, ParameterKind.Cascading), withDefault: false);

		List<Type> enums = own
			.Select(parameter => parameter.EnumType)
			.OfType<Type>()
			.Distinct()
			.OrderBy(type => type.Name, StringComparer.Ordinal)
			.ToList();
		if (enums.Count > 0)
		{
			text.AppendLine().AppendLine("## Enum values");
			foreach (Type type in enums)
			{
				text.AppendLine(CultureInfo.InvariantCulture, $"- `{type.Name}`: {string.Join(", ", Enum.GetNames(type))}");
			}
		}

		List<string> takes = own
			.SelectMany(parameter => Flatten(parameter.ValueType))
			.Where(type => !type.IsEnum && !type.IsGenericParameter
			               && (type.Assembly.GetName().Name ?? "").StartsWith("Moka.Red", StringComparison.Ordinal)
			               && catalog.FindType(TypeNames.Of(type)) is not null)
			.Select(type => $"`{TypeNames.Of(type.IsGenericType ? type.GetGenericTypeDefinition() : type)}`")
			.Distinct(StringComparer.Ordinal)
			.Order(StringComparer.Ordinal)
			.ToList();
		if (takes.Count > 0)
		{
			text.AppendLine()
				.AppendLine(CultureInfo.InvariantCulture, $"Types its parameters take (get_type describes them): {string.Join(", ", takes)}.");
		}

		IEnumerable<IGrouping<Type, ComponentParameter>> common = component.Parameters
			.Except(own)
			.GroupBy(parameter => parameter.DeclaredBy);
		foreach (IGrouping<Type, ComponentParameter> group in common)
		{
			text.AppendLine()
				.AppendLine(CultureInfo.InvariantCulture,
					$"Also from `{TypeNames.Of(group.Key)}`: {string.Join(", ", group.Select(parameter => $"`{parameter.Name}`{(parameter.CapturesUnmatchedValues ? " (takes any other HTML attribute)" : "")}"))}.");
		}

		return text.ToString().TrimEnd();
	}

	[McpServerTool(Name = "get_component_docs", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
	[Description("Gives a Moka.Red component's docs page: usage, examples in blazor-preview code blocks, and notes. Pass a section to get only that part.")]
	public string GetComponentDocs(
		[Description("Component name (MokaButton, Button) or docs page name (button, bar-chart).")]
		string name,
		[Description("Optional heading to return on its own, such as Examples, Parameters or Accessibility.")]
		string? section = null)
	{
		DocsPage? page = docs.Find(name);
		if (page is null)
		{
			return $"No docs page for '{name}'. {NotFound(name)}";
		}

		if (string.IsNullOrWhiteSpace(section))
		{
			return page.Body;
		}

		return page.Section(section)
		       ?? $"No section '{section}' on the {page.Title} page. Sections: {string.Join(", ", page.Headings.Select(heading => heading.TrimStart('#').Trim()))}.";
	}

	[McpServerTool(Name = "get_type", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
	[Description("Describes a Moka.Red type that isn't a component: a record or options class a parameter takes (MokaLineSeries, MokaToastOptions), an enum, a service such as IMokaToastService, or an icon set such as MokaIcons.Action.")]
	public string DescribeType(
		[Description("Type name, such as MokaLineSeries, IMokaDialogService, MokaVariant or MokaIcons.Action.")]
		string name)
	{
		Type? type = catalog.FindType(name);
		if (type is not null)
		{
			return TypeDescriptions.Describe(type, catalog, docs);
		}

		if (catalog.Find(name) is not null)
		{
			return GetComponent(name);
		}

		string wanted = Bare(name);
		List<string> closest = catalog.Types
			.Select(candidate => (Name: TypeNames.Of(candidate), Distance: Distance(Bare(TypeNames.Of(candidate)), wanted)))
			.Where(match => match.Distance <= Math.Max(1, wanted.Length / 4))
			.OrderBy(match => match.Distance)
			.ThenBy(match => match.Name, StringComparer.Ordinal)
			.Take(5)
			.Select(match => match.Name)
			.ToList();
		return closest.Count == 0
			? $"No Moka.Red type named '{name}'."
			: $"No Moka.Red type named '{name}'. Closest: {string.Join(", ", closest)}.";
	}

	[McpServerTool(Name = "get_getting_started", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
	[Description("Gives the Moka.Red setup guides: installation and quick start by default, or one topic such as imports or theming.")]
	public string GetGettingStarted(
		[Description("Optional topic: installation, quick-start, imports, theming or another getting-started page.")]
		string? topic = null)
	{
		string topics = string.Join(", ", docs.GuidePages.Select(page => page.Slug));
		if (string.IsNullOrWhiteSpace(topic))
		{
			IEnumerable<string> pages = DefaultGuides
				.Select(docs.FindGuide)
				.OfType<DocsPage>()
				.Select(page => page.Body);
			return $"{string.Join("\n\n", pages)}\n\nOther topics: {topics}.";
		}

		return docs.FindGuide(topic)?.Body ?? $"No getting-started topic '{topic}'. Topics: {topics}.";
	}

	private IEnumerable<(MokaComponent Component, int Score)> Search(string query)
	{
		string[] words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		string joined = string.Concat(words);
		if (words.Length == 0)
		{
			return [];
		}

		return catalog.Components
			.Select(component => (Component: component, Score: Score(component, words, joined)))
			.Where(match => match.Score > 0)
			.OrderByDescending(match => match.Score)
			.ThenBy(match => match.Component.Name, StringComparer.Ordinal);
	}

	private int Score(MokaComponent component, string[] words, string joined)
	{
		string bareName = component.Name.StartsWith("Moka", StringComparison.Ordinal) ? component.Name["Moka".Length..] : component.Name;
		int score = 0;

		if (bareName.Equals(joined, StringComparison.OrdinalIgnoreCase) || component.Name.Equals(joined, StringComparison.OrdinalIgnoreCase))
		{
			score += 100;
		}
		else if (bareName.Contains(joined, StringComparison.OrdinalIgnoreCase))
		{
			score += 40;
		}

		DocsPage? page = docs.ForComponent(component.Name);
		string summary = SummaryOf(component) ?? "";
		foreach (string word in words)
		{
			if (bareName.Contains(word, StringComparison.OrdinalIgnoreCase))
			{
				score += 20;
			}

			if (summary.Contains(word, StringComparison.OrdinalIgnoreCase))
			{
				score += 8;
			}

			if (page is not null && (page.Title.Contains(word, StringComparison.OrdinalIgnoreCase)
			                         || page.Description.Contains(word, StringComparison.OrdinalIgnoreCase)))
			{
				score += 6;
			}

			if (component.Parameters.Any(parameter => parameter.Name.Contains(word, StringComparison.OrdinalIgnoreCase)))
			{
				score += 2;
			}
		}

		return score;
	}

	private string? SummaryOf(MokaComponent component) =>
		component.Summary ?? docs.Summary(component.Name);

	// The assembly's XML docs win where they exist; the docs page's parameter table fills the rest.
	private List<ParameterRow> Rows(MokaComponent component, IEnumerable<ComponentParameter> parameters, ParameterKind kind) =>
		parameters
			.Where(parameter => parameter.Kind == kind)
			.Select(parameter =>
			{
				DocsParameter? documented = docs.Parameter(component.Name, parameter.Name);
				return new ParameterRow(
					parameter.Name,
					parameter.TypeName,
					parameter.Default ?? documented?.Default,
					parameter.Description ?? documented?.Description,
					parameter.Required);
			})
			.ToList();

	private sealed record ParameterRow(string Name, string TypeName, string? Default, string? Description, bool Required);

	private string NotFound(string name)
	{
		List<string> closest = Search(name)
			.Take(5)
			.Select(match => match.Component.DisplayName)
			.ToList();

		if (closest.Count == 0)
		{
			string wanted = Bare(name);
			int tolerance = Math.Max(1, wanted.Length / 4);
			closest = catalog.Components
				.Select(component => (Component: component, Distance: Distance(Bare(component.Name), wanted)))
				.Where(match => match.Distance <= tolerance)
				.OrderBy(match => match.Distance)
				.ThenBy(match => match.Component.Name, StringComparer.Ordinal)
				.Take(5)
				.Select(match => match.Component.DisplayName)
				.ToList();
		}

		return closest.Count == 0
			? $"No Moka.Red component named '{name}'. search_components finds one by what it does."
			: $"No Moka.Red component named '{name}'. Closest: {string.Join(", ", closest)}.";
	}

	private static IEnumerable<Type> Flatten(Type type)
	{
		Type actual = Nullable.GetUnderlyingType(type) ?? type;
		yield return actual;

		IEnumerable<Type> inner = actual.IsArray
			? [actual.GetElementType()!]
			: actual.IsGenericType
				? actual.GetGenericArguments()
				: [];
		foreach (Type argument in inner.SelectMany(Flatten))
		{
			yield return argument;
		}
	}

	private static string Bare(string name)
	{
		string key = name.Trim();
		int generic = key.IndexOf('<', StringComparison.Ordinal);
		if (generic >= 0)
		{
			key = key[..generic];
		}

		return key.StartsWith("Moka", StringComparison.OrdinalIgnoreCase) ? key["Moka".Length..] : key;
	}

	private static int Distance(string a, string b)
	{
		int[] previous = new int[b.Length + 1];
		int[] current = new int[b.Length + 1];
		for (int j = 0; j <= b.Length; j++)
		{
			previous[j] = j;
		}

		for (int i = 1; i <= a.Length; i++)
		{
			current[0] = i;
			for (int j = 1; j <= b.Length; j++)
			{
				int substitution = char.ToUpperInvariant(a[i - 1]) == char.ToUpperInvariant(b[j - 1]) ? 0 : 1;
				current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + substitution);
			}

			(previous, current) = (current, previous);
		}

		return previous[b.Length];
	}

	private IEnumerable<string> Packages() =>
		catalog.Components.Select(component => component.Category).Distinct().Order(StringComparer.Ordinal);

	private static void AppendTable(StringBuilder text, string title, List<ParameterRow> rows, bool withDefault)
	{
		if (rows.Count == 0)
		{
			return;
		}

		text.AppendLine()
			.AppendLine(CultureInfo.InvariantCulture, $"## {title}")
			.AppendLine(withDefault ? "| Name | Type | Default | Description |" : "| Name | Type | Description |")
			.AppendLine(withDefault ? "|---|---|---|---|" : "|---|---|---|");

		foreach (ParameterRow parameter in rows)
		{
			string name = $"`{parameter.Name}`{(parameter.Required ? " (required)" : "")}";
			string type = $"`{parameter.TypeName}`";
			string description = Cell(parameter.Description);
			text.AppendLine(withDefault
				? $"| {name} | {type} | {(parameter.Default is null ? "" : $"`{parameter.Default}`")} | {description} |"
				: $"| {name} | {type} | {description} |");
		}
	}

	private static string Cell(string? text) =>
		string.IsNullOrWhiteSpace(text)
			? ""
			: text.Replace("\n- ", "; ", StringComparison.Ordinal)
				.Replace('\n', ' ')
				.Replace("|", "\\|", StringComparison.Ordinal)
				.Replace("  ", " ", StringComparison.Ordinal);

	private static string Shorten(string? text, int length)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}

		string flat = text.Replace('\n', ' ');
		int sentence = flat.IndexOf(". ", StringComparison.Ordinal);
		if (sentence > 0 && sentence < length)
		{
			return flat[..(sentence + 1)];
		}

		return flat.Length <= length ? flat : flat[..length].TrimEnd() + "...";
	}
}
