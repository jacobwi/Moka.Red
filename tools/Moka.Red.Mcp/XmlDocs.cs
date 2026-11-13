using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Moka.Red.Mcp;

internal sealed partial class XmlDocs
{
	private const char ParagraphBreak = '\u0001';
	private const char ListItem = '\u0002';

	private readonly Dictionary<string, string> _summaries = new(StringComparer.Ordinal);

	internal void Load(string path)
	{
		XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
		using XmlReader reader = XmlReader.Create(path, settings);
		XDocument document = XDocument.Load(reader);

		foreach (XElement member in document.Descendants("member"))
		{
			string? name = (string?)member.Attribute("name");
			XElement? summary = member.Element("summary");
			if (name is null || summary is null)
			{
				continue;
			}

			string text = Render(summary);
			if (text.Length > 0)
			{
				_summaries[name] = text;
			}
		}
	}

	internal string? TypeSummary(Type type) =>
		_summaries.GetValueOrDefault("T:" + Key(type));

	internal string? FieldSummary(Type declaringType, string fieldName) =>
		_summaries.GetValueOrDefault($"F:{Key(declaringType)}.{fieldName}");

	internal string? PropertySummary(Type declaringType, string propertyName)
	{
		for (Type? type = declaringType; type is not null; type = type.BaseType)
		{
			if (_summaries.TryGetValue($"P:{Key(type)}.{propertyName}", out string? summary))
			{
				return summary;
			}
		}

		return null;
	}

	private static string Key(Type type)
	{
		Type definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
		return (definition.FullName ?? definition.Name).Replace('+', '.');
	}

	private static string Render(XElement summary)
	{
		StringBuilder text = new();
		AppendNodes(text, summary);

		string collapsed = Whitespace().Replace(text.ToString(), " ");
		collapsed = ParagraphBreaks().Replace(collapsed, "\n\n");
		collapsed = ListItems().Replace(collapsed, "\n- ");
		return collapsed.Trim();
	}

	private static void AppendNodes(StringBuilder text, XElement element)
	{
		foreach (XNode node in element.Nodes())
		{
			switch (node)
			{
				case XText value:
					text.Append(value.Value);
					break;
				case XElement child:
					AppendElement(text, child);
					break;
			}
		}
	}

	private static void AppendElement(StringBuilder text, XElement element)
	{
		switch (element.Name.LocalName)
		{
			case "see" or "seealso":
				string? cref = (string?)element.Attribute("cref");
				string label = element.Value.Length > 0
					? element.Value
					: cref is not null
						? ShortName(cref)
						: (string?)element.Attribute("langword") ?? (string?)element.Attribute("href") ?? "";
				text.Append('`').Append(label).Append('`');
				break;
			case "paramref" or "typeparamref":
				text.Append('`').Append((string?)element.Attribute("name")).Append('`');
				break;
			case "c":
				text.Append('`').Append(element.Value).Append('`');
				break;
			case "para":
				text.Append(ParagraphBreak);
				AppendNodes(text, element);
				text.Append(ParagraphBreak);
				break;
			case "item":
				text.Append(ListItem);
				AppendNodes(text, element);
				break;
			default:
				AppendNodes(text, element);
				break;
		}
	}

	private static string ShortName(string cref)
	{
		string name = cref.Length > 2 && cref[1] == ':' ? cref[2..] : cref;
		int parameters = name.IndexOf('(', StringComparison.Ordinal);
		if (parameters >= 0)
		{
			name = name[..parameters];
		}

		string[] parts = GenericArity().Replace(name, "").Split('.');
		bool isMember = cref.Length > 2 && cref[1] == ':' && cref[0] != 'T';
		return isMember && parts.Length > 1 ? $"{parts[^2]}.{parts[^1]}" : parts[^1];
	}

	[GeneratedRegex(@"\s+")]
	private static partial Regex Whitespace();

	[GeneratedRegex("[ \u0001]*\u0001[ \u0001]*")]
	private static partial Regex ParagraphBreaks();

	[GeneratedRegex(" ?\u0002 ?")]
	private static partial Regex ListItems();

	[GeneratedRegex(@"``?\d+")]
	private static partial Regex GenericArity();
}
