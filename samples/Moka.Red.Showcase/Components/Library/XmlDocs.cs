using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Moka.Red.Showcase.Components.Library;

// Each package's XML doc file sits beside its assembly in bin, so the text shown is always the build's own.
internal static class XmlDocs
{
	private static readonly ConcurrentDictionary<Assembly, IReadOnlyDictionary<string, XElement>> Files = new();

	public static string Summary(Type type) => Text(Member(type.Assembly, $"T:{DocName(type)}"));

	public static string Summary(PropertyInfo property)
	{
		PropertyInfo? current = property;
		while (current is not null)
		{
			Type declaring = Open(current.DeclaringType!);
			XElement? member = Member(declaring.Assembly, $"P:{DocName(declaring)}.{current.Name}");
			if (member is not null && member.Element("inheritdoc") is null)
			{
				return Text(member);
			}

			current = BaseOf(current);
		}

		return string.Empty;
	}

	public static string FirstSentence(string text)
	{
		int end = 0;
		while (true)
		{
			int stop = text.IndexOf(". ", end, StringComparison.Ordinal);
			if (stop < 0)
			{
				return text;
			}

			// "e.g. " and "i.e. " end a word, not a sentence.
			if (stop >= 3 && text[stop - 2] == '.')
			{
				end = stop + 2;
				continue;
			}

			return text[..(stop + 1)];
		}
	}

	private static PropertyInfo? BaseOf(PropertyInfo property)
	{
		MethodInfo? getter = property.GetMethod ?? property.SetMethod;
		if (getter is null)
		{
			return null;
		}

		MethodInfo definition = getter.GetBaseDefinition();
		if (definition.DeclaringType == getter.DeclaringType)
		{
			return null;
		}

		return definition.DeclaringType?.GetProperty(property.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
	}

	private static Type Open(Type type) => type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;

	private static string DocName(Type type) => (Open(type).FullName ?? type.Name).Replace('+', '.');

	private static XElement? Member(Assembly assembly, string id) =>
		Files.GetOrAdd(assembly, Load).GetValueOrDefault(id);

	private static Dictionary<string, XElement> Load(Assembly assembly)
	{
		string path = Path.ChangeExtension(assembly.Location, ".xml");
		if (!File.Exists(path))
		{
			return [];
		}

		XDocument document = XDocument.Load(path);
		return document.Descendants("member")
			.Where(member => member.Attribute("name") is not null)
			.GroupBy(member => member.Attribute("name")!.Value, StringComparer.Ordinal)
			.ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
	}

	private static string Text(XElement? member)
	{
		if (member?.Element("summary") is not { } summary)
		{
			return string.Empty;
		}

		StringBuilder text = new();
		Append(summary, text);
		return string.Join(' ', text.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
	}

	private static void Append(XElement element, StringBuilder text)
	{
		foreach (XNode node in element.Nodes())
		{
			switch (node)
			{
				case XText plain:
					text.Append(plain.Value);
					break;
				case XElement { Name.LocalName: "see" or "seealso" } see:
					text.Append(see.Attribute("langword")?.Value ?? ShortName(see.Attribute("cref")?.Value) ?? see.Value);
					break;
				case XElement { Name.LocalName: "paramref" or "typeparamref" } reference:
					text.Append(reference.Attribute("name")?.Value);
					break;
				case XElement { Name.LocalName: "para" or "br" } paragraph:
					text.Append(' ');
					Append(paragraph, text);
					text.Append(' ');
					break;
				case XElement other:
					Append(other, text);
					break;
			}
		}
	}

	// "P:Moka.Red.Primitives.Button.MokaButton.Href" reads as "Href", "T:...MokaTable`1" as "MokaTable".
	private static string? ShortName(string? cref)
	{
		if (string.IsNullOrEmpty(cref))
		{
			return null;
		}

		string name = cref.Length > 2 && cref[1] == ':' ? cref[2..] : cref;
		int arguments = name.IndexOf('(', StringComparison.Ordinal);
		if (arguments >= 0)
		{
			name = name[..arguments];
		}

		name = name[(name.LastIndexOf('.') + 1)..];
		int tick = name.IndexOf('`', StringComparison.Ordinal);
		return tick >= 0 ? name[..tick] : name;
	}
}
