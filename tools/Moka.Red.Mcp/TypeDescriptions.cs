using System.Globalization;
using System.Reflection;
using System.Text;

namespace Moka.Red.Mcp;

internal static class TypeDescriptions
{
	private const int NamesOnlyAbove = 40;

	internal static string Describe(Type type, ComponentCatalog catalog, DocsLibrary docs)
	{
		string name = TypeNames.Of(type);
		StringBuilder text = new();
		text.AppendLine(CultureInfo.InvariantCulture, $"# {name} ({Kind(type)})")
			.AppendLine(CultureInfo.InvariantCulture, $"Package `{type.Assembly.GetName().Name}`, namespace `{type.Namespace}`.");

		string? summary = catalog.Docs.TypeSummary(type) ?? docs.Summary(type.Name);
		if (!string.IsNullOrWhiteSpace(summary))
		{
			text.AppendLine().AppendLine(summary);
		}

		if (type.IsEnum)
		{
			text.AppendLine().AppendLine("## Values");
			foreach (string value in Enum.GetNames(type))
			{
				string? description = catalog.Docs.FieldSummary(type, value) ?? docs.Parameter(type.Name, value)?.Description;
				text.AppendLine(CultureInfo.InvariantCulture, $"- `{value}`{(string.IsNullOrWhiteSpace(description) ? "" : $": {Flat(description)}")}");
			}

			return text.ToString().TrimEnd();
		}

		AppendProperties(text, type, catalog, docs, BindingFlags.Public | BindingFlags.Instance, "Properties");
		AppendProperties(text, type, catalog, docs, BindingFlags.Public | BindingFlags.Static, "Static properties");
		AppendFields(text, type);
		AppendMethods(text, type, catalog);

		Type[] nested = type.GetNestedTypes(BindingFlags.Public);
		if (nested.Length > 0)
		{
			text.AppendLine()
				.AppendLine("## Nested types")
				.AppendLine(string.Join(", ", nested.Select(inner => $"`{TypeNames.Of(inner)}`").Order(StringComparer.Ordinal)));
		}

		return text.ToString().TrimEnd();
	}

	private static void AppendProperties(StringBuilder text, Type type, ComponentCatalog catalog, DocsLibrary docs, BindingFlags flags, string title)
	{
		List<PropertyInfo> properties = type.GetProperties(flags)
			.Where(property => property.GetIndexParameters().Length == 0 && property.DeclaringType != typeof(object))
			.Where(property => property.Name != "EqualityContract")
			.ToList();
		if (properties.Count == 0)
		{
			return;
		}

		text.AppendLine().AppendLine(CultureInfo.InvariantCulture, $"## {title}");
		if (properties.Count > NamesOnlyAbove)
		{
			text.AppendLine(string.Join(", ", properties.Select(property => property.Name)));
			return;
		}

		text.AppendLine("| Name | Type | Description |").AppendLine("|---|---|---|");
		foreach (PropertyInfo property in properties)
		{
			string? description = catalog.Docs.PropertySummary(property.DeclaringType!, property.Name)
			                      ?? docs.Parameter(type.Name, property.Name)?.Description;
			string access = property.SetMethod is { IsPublic: true } setter
				? setter.ReturnParameter.GetRequiredCustomModifiers().Any(modifier => modifier.Name == "IsExternalInit") ? " (init)" : ""
				: " (read-only)";
			text.AppendLine(CultureInfo.InvariantCulture,
				$"| `{property.Name}`{access} | `{TypeNames.Of(property, catalog.Nullability)}` | {Flat(description)} |");
		}
	}

	// Icon sets such as MokaIcons.Action keep their icons in static readonly fields.
	private static void AppendFields(StringBuilder text, Type type)
	{
		IEnumerable<IGrouping<string, FieldInfo>> groups = type
			.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
			.GroupBy(field => $"{(field.IsStatic ? "Static fields" : "Fields")} ({TypeNames.Of(field.FieldType)})");

		foreach (IGrouping<string, FieldInfo> group in groups.OrderBy(group => group.Key, StringComparer.Ordinal))
		{
			text.AppendLine()
				.AppendLine(CultureInfo.InvariantCulture, $"## {group.Key}")
				.AppendLine(string.Join(", ", group.Select(field => field.Name)));
		}
	}

	private static void AppendMethods(StringBuilder text, Type type, ComponentCatalog catalog)
	{
		List<MethodInfo> methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
			.Where(method => !method.IsSpecialName && method.DeclaringType != typeof(object))
			.Where(method => method.Name is not ("Equals" or "GetHashCode" or "ToString" or "Deconstruct" or "PrintMembers" or "<Clone>$"))
			.OrderBy(method => method.Name, StringComparer.Ordinal)
			.ToList();
		if (methods.Count == 0)
		{
			return;
		}

		text.AppendLine().AppendLine("## Methods");
		foreach (MethodInfo method in methods)
		{
			string generic = method.IsGenericMethodDefinition
				? $"<{string.Join(", ", method.GetGenericArguments().Select(argument => argument.Name))}>"
				: "";
			string parameters = string.Join(", ", method.GetParameters().Select(parameter => Parameter(parameter, catalog.Nullability)));
			text.AppendLine(CultureInfo.InvariantCulture,
				$"- `{(method.IsStatic ? "static " : "")}{TypeNames.Of(method.ReturnType)} {method.Name}{generic}({parameters})`");
		}
	}

	private static string Parameter(ParameterInfo parameter, NullabilityInfoContext nullability)
	{
		string type = TypeNames.Of(parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType);
		if (!parameter.ParameterType.IsValueType && !parameter.ParameterType.IsGenericParameter
		                                         && nullability.Create(parameter).ReadState == NullabilityState.Nullable)
		{
			type += "?";
		}

		string defaultValue = parameter.HasDefaultValue
			? " = " + parameter.DefaultValue switch
			{
				null => "null",
				bool flag => flag ? "true" : "false",
				string value => $"\"{value}\"",
				Enum member => $"{member.GetType().Name}.{member}",
				IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
				{ } other => other.ToString()
			}
			: "";
		return $"{type} {parameter.Name}{defaultValue}";
	}

	private static string Kind(Type type) => type switch
	{
		{ IsEnum: true } => "enum",
		{ IsInterface: true } => "interface",
		{ IsValueType: true } => type.GetMethod("<Clone>$") is null && type.GetMethod("PrintMembers", BindingFlags.NonPublic | BindingFlags.Instance) is not null ? "record struct" : "struct",
		{ IsAbstract: true, IsSealed: true } => "static class",
		_ when type.GetMethod("<Clone>$") is not null => "record",
		{ IsAbstract: true } => "abstract class",
		_ => "class"
	};

	private static string Flat(string? text) =>
		string.IsNullOrWhiteSpace(text)
			? ""
			: text.Replace("\n- ", "; ", StringComparison.Ordinal)
				.Replace('\n', ' ')
				.Replace("|", "\\|", StringComparison.Ordinal);
}
