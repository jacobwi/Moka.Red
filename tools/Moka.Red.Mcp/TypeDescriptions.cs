using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
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

		List<PropertyInfo> properties = Properties(type, BindingFlags.Public | BindingFlags.Instance);
		ConstructorInfo? primary = PrimaryConstructor(type);
		HashSet<string> positional = primary is null ? [] : [.. primary.GetParameters().Select(parameter => parameter.Name!)];

		AppendConstructors(text, type, primary, properties, positional, catalog.Nullability);
		AppendProperties(text, type, properties, positional, catalog, docs, "Properties");
		AppendProperties(text, type, Properties(type, BindingFlags.Public | BindingFlags.Static), [], catalog, docs, "Static properties");
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

	private static List<PropertyInfo> Properties(Type type, BindingFlags flags) =>
		type.GetProperties(flags)
			.Where(property => property.GetIndexParameters().Length == 0 && property.DeclaringType != typeof(object))
			.Where(property => property.Name != "EqualityContract")
			.ToList();

	// Public constructors, left out when the only one takes nothing, so a plain options class reads as before. A record's
	// copy constructor is protected or private, so it never shows.
	private static void AppendConstructors(
		StringBuilder text,
		Type type,
		ConstructorInfo? primary,
		List<PropertyInfo> properties,
		HashSet<string> positional,
		NullabilityInfoContext nullability)
	{
		if (type.IsAbstract)
		{
			return;
		}

		List<ConstructorInfo> constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
			.Where(constructor => !ComponentCatalog.IsHidden(constructor))
			.OrderBy(constructor => constructor == primary ? 0 : 1)
			.ToList();
		if (constructors.All(constructor => constructor.GetParameters().Length == 0))
		{
			return;
		}

		string name = TypeNames.Of(type);
		text.AppendLine().AppendLine("## Constructors");
		foreach (ConstructorInfo constructor in constructors)
		{
			string parameters = string.Join(", ", constructor.GetParameters().Select(parameter => Parameter(parameter, nullability)));
			text.AppendLine(CultureInfo.InvariantCulture, $"- `new {name}({parameters})`");
		}

		if (primary is not null)
		{
			string? initOnly = properties.FirstOrDefault(property => !positional.Contains(property.Name) && IsInitOnly(property))?.Name;
			text.AppendLine().AppendLine(Usage(type, name, primary, initOnly));
		}
	}

	private static string Usage(Type type, string name, ConstructorInfo primary, string? initOnly)
	{
		ParameterInfo[] parameters = primary.GetParameters();
		string arguments = string.Join(", ", parameters.Where(parameter => !parameter.HasDefaultValue).Select(parameter => parameter.Name));
		List<string> optional = [.. parameters.Where(parameter => parameter.HasDefaultValue).Select(parameter => parameter.Name!)];
		string initializer = initOnly is null ? "" : $" {{ {initOnly} = ... }}";

		List<string> notes = [];
		if (optional.Count > 0)
		{
			notes.Add($"{Listed(optional)} as optional arguments");
		}

		if (initOnly is not null)
		{
			notes.Add("the (init) properties in the initializer");
		}

		string with = notes.Count == 0 ? "" : $", with {string.Join(" and ", notes)}";
		return $"Positional {Kind(type)}: create it with `new {name}({arguments}){initializer}`{with}.";
	}

	private static string Listed(List<string> names) =>
		names.Count == 1 ? names[0] : $"{string.Join(", ", names[..^1])} and {names[^1]}";

	// A record's primary constructor is the one its compiler-made Deconstruct mirrors, name for name and type for type.
	private static ConstructorInfo? PrimaryConstructor(Type type)
	{
		if (!IsRecord(type))
		{
			return null;
		}

		List<ParameterInfo[]> deconstructs = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
			.Where(method => method.Name == "Deconstruct" && method.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
			.Select(method => method.GetParameters())
			.ToList();

		return type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
			.FirstOrDefault(constructor => deconstructs.Any(outs => Mirrors(constructor.GetParameters(), outs)));
	}

	private static bool Mirrors(ParameterInfo[] parameters, ParameterInfo[] outs) =>
		parameters.Length == outs.Length
		&& parameters.Zip(outs).All(pair => pair.First.Name == pair.Second.Name
		                                    && pair.Second.ParameterType.IsByRef
		                                    && pair.First.ParameterType == pair.Second.ParameterType.GetElementType());

	private static bool IsInitOnly(PropertyInfo property) =>
		property.SetMethod is { IsPublic: true } setter
		&& setter.ReturnParameter.GetRequiredCustomModifiers().Any(modifier => modifier.Name == "IsExternalInit");

	private static void AppendProperties(
		StringBuilder text,
		Type type,
		List<PropertyInfo> properties,
		HashSet<string> positional,
		ComponentCatalog catalog,
		DocsLibrary docs,
		string title)
	{
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
			string access = positional.Contains(property.Name) ? " (positional)"
				: property.SetMethod is not { IsPublic: true } ? " (read-only)"
				: IsInitOnly(property) ? " (init)"
				: "";
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
				$"- `{(method.IsStatic ? "static " : "")}{Written(method.ReturnParameter, catalog.Nullability)} {method.Name}{generic}({parameters})`");
		}
	}

	private static string Parameter(ParameterInfo parameter, NullabilityInfoContext nullability)
	{
		Type valueType = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;
		string passing = !parameter.ParameterType.IsByRef ? ""
			: parameter.IsOut ? "out "
			: parameter.IsIn ? "in "
			: "ref ";
		if (parameter.IsDefined(typeof(ParamArrayAttribute), false))
		{
			passing = "params ";
		}

		// A struct parameter that defaults to default(T), such as a CancellationToken, reports null as its default.
		string defaultValue = parameter.HasDefaultValue
			? " = " + parameter.DefaultValue switch
			{
				null when valueType.IsValueType && Nullable.GetUnderlyingType(valueType) is null => "default",
				null => "null",
				bool flag => flag ? "true" : "false",
				string value => $"\"{value}\"",
				Enum member => $"{member.GetType().Name}.{member}",
				IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
				{ } other => other.ToString()
			}
			: "";
		return $"{passing}{Written(parameter, nullability)} {parameter.Name}{defaultValue}";
	}

	// A parameter's or a return value's type with its nullable annotation; an out parameter is annotated for what it writes.
	private static string Written(ParameterInfo parameter, NullabilityInfoContext nullability)
	{
		Type type = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;
		string name = TypeNames.Of(type);
		if (type.IsValueType || type.IsGenericParameter || type == typeof(void))
		{
			return name;
		}

		NullabilityInfo info = nullability.Create(parameter);
		NullabilityState state = parameter.IsOut ? info.WriteState : info.ReadState;
		return state == NullabilityState.Nullable ? name + "?" : name;
	}

	private static string Kind(Type type) => type switch
	{
		{ IsEnum: true } => "enum",
		{ IsInterface: true } => "interface",
		{ IsValueType: true } => IsRecord(type) ? "record struct" : "struct",
		{ IsAbstract: true, IsSealed: true } => "static class",
		_ when IsRecord(type) => "record",
		{ IsAbstract: true } => "abstract class",
		_ => "class"
	};

	private static bool IsRecord(Type type) =>
		type.IsValueType
			? type.GetMethod("PrintMembers", BindingFlags.NonPublic | BindingFlags.Instance) is not null
			: type.GetMethod("<Clone>$") is not null;

	private static string Flat(string? text) =>
		string.IsNullOrWhiteSpace(text)
			? ""
			: text.Replace("\n- ", "; ", StringComparison.Ordinal)
				.Replace('\n', ' ')
				.Replace("|", "\\|", StringComparison.Ordinal);
}
