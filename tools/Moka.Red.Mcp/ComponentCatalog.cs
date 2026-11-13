using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Moka.Red.Mcp;

internal enum ParameterKind
{
	Value,
	Event,
	Content,
	Cascading
}

internal sealed record ComponentParameter(
	string Name,
	string TypeName,
	ParameterKind Kind,
	string? Default,
	string? Description,
	bool Required,
	bool CapturesUnmatchedValues,
	Type DeclaredBy,
	Type ValueType,
	Type? EnumType);

internal sealed record MokaComponent(
	string Name,
	string DisplayName,
	string Package,
	string Namespace,
	string? Summary,
	IReadOnlyList<ComponentParameter> Parameters,
	Type Type)
{
	internal string Category => Package.StartsWith("Moka.Red.", StringComparison.Ordinal) ? Package["Moka.Red.".Length..] : Package;
}

internal sealed class ComponentCatalog
{
	private readonly Dictionary<string, MokaComponent> _byName;
	private readonly Dictionary<string, Type> _types;

	public ComponentCatalog()
		: this(LoadPackages(AppContext.BaseDirectory))
	{
	}

	internal ComponentCatalog(IReadOnlyList<Assembly> packages)
	{
		foreach (Assembly package in packages)
		{
			string xml = Path.ChangeExtension(package.Location, ".xml");
			if (File.Exists(xml))
			{
				Docs.Load(xml);
			}
		}

		List<Type> exported = [.. packages.SelectMany(package => package.GetExportedTypes())];

		Components = exported
			.Where(IsComponent)
			.Select(type => Describe(type, Docs, Nullability))
			.OrderBy(component => component.Name, StringComparer.Ordinal)
			.ToList();

		_types = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
		foreach (Type type in exported.Where(type => !IsComponent(type) && !type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute))))
		{
			_types.TryAdd(WithoutGenerics(TypeNames.Of(type)), type);
		}

		_byName = new Dictionary<string, MokaComponent>(StringComparer.OrdinalIgnoreCase);
		foreach (MokaComponent component in Components)
		{
			_byName.TryAdd(component.Name, component);
			if (component.Name.StartsWith("Moka", StringComparison.Ordinal))
			{
				_byName.TryAdd(component.Name["Moka".Length..], component);
			}
		}

		CommonBases = FindCommonBases(Components);
	}

	internal IReadOnlyList<MokaComponent> Components { get; }

	internal XmlDocs Docs { get; } = new();

	internal NullabilityInfoContext Nullability { get; } = new();

	// Records, options, enums, services and icon sets: everything public in the packages that isn't a component.
	internal IReadOnlyCollection<Type> Types => _types.Values;

	internal Type? FindType(string name)
	{
		string key = WithoutGenerics(name.Trim());
		return _types.GetValueOrDefault(key) ?? _types.GetValueOrDefault("Moka" + key);
	}

	private static string WithoutGenerics(string name)
	{
		int generic = name.IndexOf('<', StringComparison.Ordinal);
		return generic >= 0 ? name[..generic] : name;
	}

	// Base classes more than half the components share. Their parameters are listed by name only, so each
	// component's description doesn't repeat Class, Style and the rest in full.
	internal IReadOnlySet<Type> CommonBases { get; }

	internal MokaComponent? Find(string name) =>
		_byName.GetValueOrDefault(WithoutGenerics(name.Trim()));

	internal static IReadOnlyList<Assembly> LoadPackages(string folder) =>
		Directory.GetFiles(folder, "Moka.Red*.dll")
			.Select(Path.GetFileNameWithoutExtension)
			.Where(name => name is not null
			               && !name.Equals("Moka.Red.Mcp", StringComparison.Ordinal)
			               && !name.EndsWith(".Tests", StringComparison.Ordinal))
			.Select(name => Assembly.Load(new AssemblyName(name!)))
			.OrderBy(assembly => assembly.GetName().Name, StringComparer.Ordinal)
			.ToList();

	private static bool IsComponent(Type type) =>
		type is { IsClass: true, IsAbstract: false }
		&& typeof(IComponent).IsAssignableFrom(type)
		&& !type.Name.StartsWith('_');

	private static MokaComponent Describe(Type type, XmlDocs docs, NullabilityInfoContext nullability)
	{
		object? sample = CreateSample(type);

		List<ComponentParameter> parameters = type
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Select(property => (Property: property, Parameter: property.GetCustomAttribute<ParameterAttribute>(true),
				Cascading: property.GetCustomAttribute<CascadingParameterAttribute>(true)))
			.Where(found => found.Parameter is not null || found.Cascading is not null)
			.Select(found => DescribeParameter(found.Property, found.Parameter, found.Cascading is not null, sample, docs, nullability))
			.OrderBy(parameter => Depth(type, parameter.DeclaredBy))
			.ToList();

		string name = TypeNames.Of(type);
		int generic = name.IndexOf('<', StringComparison.Ordinal);
		return new MokaComponent(
			generic >= 0 ? name[..generic] : name,
			name,
			type.Assembly.GetName().Name ?? "",
			type.Namespace ?? "",
			docs.TypeSummary(type),
			parameters,
			type);
	}

	private static ComponentParameter DescribeParameter(
		PropertyInfo property,
		ParameterAttribute? parameter,
		bool cascading,
		object? sample,
		XmlDocs docs,
		NullabilityInfoContext nullability)
	{
		Type propertyType = property.PropertyType;
		ParameterKind kind = cascading ? ParameterKind.Cascading
			: IsGeneric(propertyType, typeof(EventCallback), typeof(EventCallback<>)) ? ParameterKind.Event
			: IsGeneric(propertyType, typeof(RenderFragment), typeof(RenderFragment<>)) ? ParameterKind.Content
			: ParameterKind.Value;

		Type declaredBy = property.DeclaringType!.IsGenericType
			? property.DeclaringType.GetGenericTypeDefinition()
			: property.DeclaringType;

		Type enumType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

		return new ComponentParameter(
			property.Name,
			TypeNames.Of(property, nullability),
			kind,
			kind == ParameterKind.Value ? ReadDefault(sample, property.Name) : null,
			docs.PropertySummary(property.DeclaringType, property.Name),
			property.IsDefined(typeof(EditorRequiredAttribute), true),
			parameter?.CaptureUnmatchedValues ?? false,
			declaredBy,
			propertyType,
			enumType.IsEnum ? enumType : null);
	}

	private static bool IsGeneric(Type type, Type plain, Type open) =>
		type == plain || (type.IsGenericType && type.GetGenericTypeDefinition() == open);

	private static int Depth(Type component, Type declaredBy)
	{
		int depth = 0;
		for (Type? type = component; type is not null; type = type.BaseType, depth++)
		{
			Type definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
			if (definition == declaredBy)
			{
				return depth;
			}
		}

		return depth;
	}

	private static HashSet<Type> FindCommonBases(IReadOnlyList<MokaComponent> components)
	{
		Dictionary<Type, int> counts = [];
		foreach (MokaComponent component in components)
		{
			for (Type? type = component.Type.BaseType; type is not null; type = type.BaseType)
			{
				Type definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
				counts[definition] = counts.GetValueOrDefault(definition) + 1;
			}
		}

		return counts
			.Where(entry => entry.Value * 2 > components.Count)
			.Select(entry => entry.Key)
			.ToHashSet();
	}

	private static object? CreateSample(Type type)
	{
		Type? concrete = type;
		if (type.IsGenericTypeDefinition)
		{
			Type[] arguments = type.GetGenericArguments();
			if (arguments.Any(argument => argument.GetGenericParameterConstraints().Length > 0
			                              || argument.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint)))
			{
				return null;
			}

			concrete = type.MakeGenericType([.. arguments.Select(_ => typeof(object))]);
		}

		if (concrete.GetConstructor(Type.EmptyTypes) is null)
		{
			return null;
		}

		try
		{
			return Activator.CreateInstance(concrete);
		}
		catch (TargetInvocationException)
		{
			return null;
		}
		catch (MemberAccessException)
		{
			return null;
		}
	}

	private static string? ReadDefault(object? sample, string propertyName)
	{
		if (sample is null)
		{
			return null;
		}

		PropertyInfo? property = sample.GetType()
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.FirstOrDefault(candidate => candidate.Name == propertyName && candidate.GetIndexParameters().Length == 0);
		if (property?.GetMethod is null)
		{
			return null;
		}

		object? value;
		try
		{
			value = property.GetValue(sample);
		}
		catch (TargetInvocationException)
		{
			return null;
		}

		return value switch
		{
			null => "null",
			bool flag => flag ? "true" : "false",
			string text => $"\"{text}\"",
			char letter => $"'{letter}'",
			Enum member => string.Join(" | ", member.ToString().Split(", ").Select(flag => $"{member.GetType().Name}.{flag}")),
			IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
			_ => null
		};
	}
}
