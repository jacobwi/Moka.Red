using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Moka.Red.Mcp;

internal sealed record DocsParameter(string? Default, string Description);

internal sealed record DocsPage(
	string Slug,
	string Title,
	string Description,
	string Body,
	IReadOnlyList<string> Components,
	IReadOnlyDictionary<string, IReadOnlyDictionary<string, DocsParameter>> Parameters)
{
	internal IReadOnlyList<string> Headings =>
		[.. Lines(Body).Where(line => line.IsHeading).Select(line => line.Text)];

	internal string? Section(string heading)
	{
		List<MarkdownLine> lines = Lines(Body);
		int start = lines.FindIndex(line => line.IsHeading
		                                    && line.Text.TrimStart('#').Trim().Contains(heading.Trim(), StringComparison.OrdinalIgnoreCase));
		if (start < 0)
		{
			return null;
		}

		int level = lines[start].Level;
		int end = lines.FindIndex(start + 1, line => line.IsHeading && line.Level <= level);
		IEnumerable<string> section = lines
			.Skip(start)
			.Take((end < 0 ? lines.Count : end) - start)
			.Select(line => line.Text);
		return string.Join('\n', section).Trim();
	}

	private static List<MarkdownLine> Lines(string body)
	{
		List<MarkdownLine> lines = [];
		bool inFence = false;
		foreach (string text in body.Split('\n'))
		{
			if (text.TrimStart().StartsWith("```", StringComparison.Ordinal))
			{
				inFence = !inFence;
			}

			int level = inFence ? 0 : text.TakeWhile(c => c == '#').Count();
			bool isHeading = level is > 0 and <= 6 && text.Length > level && text[level] == ' ';
			lines.Add(new MarkdownLine(text, isHeading ? level : 0));
		}

		return lines;
	}

	private readonly record struct MarkdownLine(string Text, int Level)
	{
		internal bool IsHeading => Level > 0;
	}
}

internal sealed partial class DocsLibrary
{
	private const string ComponentPrefix = "docs/components/";
	private const string GuidePrefix = "docs/getting-started/";

	private readonly Dictionary<string, DocsPage> _bySlug = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, DocsPage> _byComponent = new(StringComparer.OrdinalIgnoreCase);

	public DocsLibrary()
		: this(typeof(DocsLibrary).Assembly)
	{
	}

	internal DocsLibrary(Assembly assembly)
	{
		List<DocsPage> components = [];
		List<DocsPage> guides = [];

		foreach (string resource in assembly.GetManifestResourceNames().Order(StringComparer.Ordinal))
		{
			bool isComponent = resource.StartsWith(ComponentPrefix, StringComparison.Ordinal);
			bool isGuide = resource.StartsWith(GuidePrefix, StringComparison.Ordinal);
			if (!isComponent && !isGuide)
			{
				continue;
			}

			string slug = Path.GetFileNameWithoutExtension(resource);
			DocsPage page = Parse(slug, Read(assembly, resource), isComponent);
			(isComponent ? components : guides).Add(page);
		}

		ComponentPages = components;
		GuidePages = guides;

		foreach (DocsPage page in components)
		{
			_bySlug.TryAdd(page.Slug, page);
		}

		// A page's own component (button.md is MokaButton's) wins over one it only covers in a section.
		foreach (DocsPage page in components)
		{
			_byComponent.TryAdd(OwnComponent(page.Slug), page);
		}

		foreach (DocsPage page in components)
		{
			foreach (string component in page.Components)
			{
				_byComponent.TryAdd(component, page);
			}
		}
	}

	internal IReadOnlyList<DocsPage> ComponentPages { get; }

	internal IReadOnlyList<DocsPage> GuidePages { get; }

	internal DocsPage? ForComponent(string componentName) =>
		_byComponent.GetValueOrDefault(componentName.Trim());

	internal DocsParameter? Parameter(string componentName, string parameterName) =>
		ForComponent(componentName)?.Parameters.GetValueOrDefault(componentName)?.GetValueOrDefault(parameterName);

	// A page describes its own component in the front matter; a component it only covers in a section gets nothing
	// from it, or MokaButtonGroup would read as a button.
	internal string? Summary(string componentName) =>
		ForComponent(componentName) is { } page && OwnComponent(page.Slug).Equals(componentName, StringComparison.OrdinalIgnoreCase)
			? page.Description
			: null;

	internal DocsPage? Find(string name)
	{
		string key = name.Trim();
		int generic = key.IndexOf('<', StringComparison.Ordinal);
		if (generic >= 0)
		{
			key = key[..generic];
		}

		return _byComponent.GetValueOrDefault(key)
		       ?? _byComponent.GetValueOrDefault("Moka" + key)
		       ?? _bySlug.GetValueOrDefault(key)
		       ?? _bySlug.GetValueOrDefault(Kebab(key.StartsWith("Moka", StringComparison.Ordinal) ? key["Moka".Length..] : key))
		       ?? ComponentPages.FirstOrDefault(page => page.Title.Equals(key, StringComparison.OrdinalIgnoreCase));
	}

	internal DocsPage? FindGuide(string topic) =>
		GuidePages.FirstOrDefault(page => page.Slug.Equals(topic.Trim(), StringComparison.OrdinalIgnoreCase)
		                                  || page.Title.Equals(topic.Trim(), StringComparison.OrdinalIgnoreCase));

	internal static string OwnComponent(string slug) =>
		"Moka" + string.Concat(slug.Split('-', StringSplitOptions.RemoveEmptyEntries)
			.Select(part => char.ToUpperInvariant(part[0]) + part[1..]));

	private static string Kebab(string pascal)
	{
		StringBuilder text = new();
		for (int i = 0; i < pascal.Length; i++)
		{
			if (i > 0 && char.IsUpper(pascal[i]))
			{
				text.Append('-');
			}

			text.Append(char.ToLowerInvariant(pascal[i]));
		}

		return text.ToString();
	}

	private static string Read(Assembly assembly, string resource)
	{
		using Stream stream = assembly.GetManifestResourceStream(resource)
		                      ?? throw new InvalidOperationException($"Embedded docs page {resource} is missing.");
		using StreamReader reader = new(stream, Encoding.UTF8);
		return reader.ReadToEnd().ReplaceLineEndings("\n");
	}

	private static DocsPage Parse(string slug, string text, bool isComponent)
	{
		string title = slug;
		string description = "";
		string body = text;

		if (text.StartsWith("---\n", StringComparison.Ordinal))
		{
			int end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
			if (end > 0)
			{
				foreach (string line in text[4..end].Split('\n'))
				{
					int colon = line.IndexOf(':', StringComparison.Ordinal);
					if (colon <= 0)
					{
						continue;
					}

					string value = line[(colon + 1)..].Trim().Trim('"', '\'');
					switch (line[..colon].Trim())
					{
						case "title":
							title = value;
							break;
						case "description":
							description = value;
							break;
					}
				}

				int bodyStart = text.IndexOf('\n', end + 1);
				body = bodyStart < 0 ? "" : text[(bodyStart + 1)..];
			}
		}

		IReadOnlyList<string> components = isComponent
			? [.. ComponentHeading().Matches(body).Select(match => match.Groups[1].Value).Distinct(StringComparer.Ordinal)]
			: [];

		return new DocsPage(slug, title, description, body.Trim(), components,
			isComponent ? ParameterTables(OwnComponent(slug), body) : new Dictionary<string, IReadOnlyDictionary<string, DocsParameter>>());
	}

	// Reads the "| Name | Type | Default | Description |" tables. Rows belong to the component named by the nearest
	// "### MokaX" heading above them, or to the page's own component when there is none.
	private static Dictionary<string, IReadOnlyDictionary<string, DocsParameter>> ParameterTables(string ownComponent, string body)
	{
		Dictionary<string, Dictionary<string, DocsParameter>> tables = new(StringComparer.OrdinalIgnoreCase);
		string component = ownComponent;
		int nameColumn = -1;
		int defaultColumn = -1;
		int descriptionColumn = -1;
		bool inFence = false;

		foreach (string line in body.Split('\n'))
		{
			string trimmed = line.Trim();
			if (trimmed.StartsWith("```", StringComparison.Ordinal))
			{
				inFence = !inFence;
				continue;
			}

			if (inFence)
			{
				continue;
			}

			if (trimmed.StartsWith('#'))
			{
				Match heading = ComponentHeading().Match(trimmed);
				component = heading.Success ? heading.Groups[1].Value : trimmed.StartsWith("## ", StringComparison.Ordinal) ? ownComponent : component;
				nameColumn = -1;
				continue;
			}

			if (!trimmed.StartsWith('|'))
			{
				nameColumn = -1;
				continue;
			}

			string[] cells = Cells(trimmed);
			if (nameColumn < 0)
			{
				nameColumn = Array.FindIndex(cells, cell => cell.Equals("Name", StringComparison.OrdinalIgnoreCase)
				                                            || cell.Equals("Parameter", StringComparison.OrdinalIgnoreCase));
				defaultColumn = Array.FindIndex(cells, cell => cell.Equals("Default", StringComparison.OrdinalIgnoreCase));
				descriptionColumn = Array.FindIndex(cells, cell => cell.Equals("Description", StringComparison.OrdinalIgnoreCase));
				if (descriptionColumn < 0)
				{
					nameColumn = -1;
				}

				continue;
			}

			if (cells.All(cell => cell.Trim('-', ':').Length == 0) || cells.Length <= Math.Max(nameColumn, descriptionColumn))
			{
				continue;
			}

			string name = cells[nameColumn].Trim('`', ' ');
			if (name.Length == 0 || !char.IsLetter(name[0]))
			{
				continue;
			}

			string? defaultValue = defaultColumn >= 0 && defaultColumn < cells.Length ? cells[defaultColumn].Trim('`', ' ') : null;
			if (defaultValue is "" or "-")
			{
				defaultValue = null;
			}

			if (!tables.TryGetValue(component, out Dictionary<string, DocsParameter>? rows))
			{
				rows = new Dictionary<string, DocsParameter>(StringComparer.Ordinal);
				tables[component] = rows;
			}

			rows.TryAdd(name, new DocsParameter(defaultValue, cells[descriptionColumn]));
		}

		return tables.ToDictionary(
			entry => entry.Key,
			entry => (IReadOnlyDictionary<string, DocsParameter>)entry.Value,
			StringComparer.OrdinalIgnoreCase);
	}

	private static string[] Cells(string row)
	{
		const string escapedPipe = "\u0003";
		string[] cells = row.Replace("\\|", escapedPipe, StringComparison.Ordinal)
			.Trim('|')
			.Split('|');
		return [.. cells.Select(cell => cell.Replace(escapedPipe, "|", StringComparison.Ordinal).Trim())];
	}

	[GeneratedRegex(@"^#{2,4}\s+`?(Moka[A-Za-z0-9]+)", RegexOptions.Multiline)]
	private static partial Regex ComponentHeading();
}
