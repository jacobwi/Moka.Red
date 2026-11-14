using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Moka.Red.Showcase.Components.Library;

internal sealed record ComponentParameter(
	string Name,
	string Type,
	string Default,
	string Description,
	string Group,
	bool Inherited,
	bool IsEvent,
	bool IsRequired,
	bool CapturesUnmatched);

// Every [Parameter] a component declares or inherits, read from the assembly so the table can't drift from it.
internal static class ComponentParameters
{
	private static readonly ConcurrentDictionary<Type, IReadOnlyList<ComponentParameter>> Cache = new();
	private static readonly NullabilityInfoContext Nullability = new();

	private static readonly Dictionary<Type, string> Keywords = new()
	{
		[typeof(bool)] = "bool",
		[typeof(byte)] = "byte",
		[typeof(char)] = "char",
		[typeof(decimal)] = "decimal",
		[typeof(double)] = "double",
		[typeof(float)] = "float",
		[typeof(int)] = "int",
		[typeof(long)] = "long",
		[typeof(object)] = "object",
		[typeof(short)] = "short",
		[typeof(string)] = "string",
		[typeof(uint)] = "uint",
		[typeof(ulong)] = "ulong"
	};

	private static readonly Type[] StandIns = [typeof(string), typeof(int), typeof(double)];

	public static IReadOnlyList<ComponentParameter> Of(Type component) => Cache.GetOrAdd(component, Read);

	public static string DisplayName(Type type)
	{
		string name = WithoutArity(type.Name);
		return type.IsGenericTypeDefinition
			? $"{name}<{string.Join(", ", type.GetGenericArguments().Select(argument => argument.Name))}>"
			: name;
	}

	private static List<ComponentParameter> Read(Type component)
	{
		Type? closed = Closed(component);
		object? instance = closed?.GetConstructor(Type.EmptyTypes) is null ? null : Activator.CreateInstance(closed);
		string ownName = DisplayName(component);
		List<ComponentParameter> rows = [];
		foreach (PropertyInfo property in component.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (property.GetCustomAttribute<ParameterAttribute>(inherit: true) is not { } parameter)
			{
				continue;
			}

			Type declaring = Open(property.DeclaringType!);
			bool inherited = declaring != component;
			rows.Add(new ComponentParameter(
				property.Name,
				TypeName(property.PropertyType, Nullability.Create(property)),
				instance is null ? string.Empty : DefaultOf(instance, property),
				XmlDocs.Summary(property),
				inherited ? $"Inherited from {DisplayName(declaring)}" : ownName,
				inherited,
				IsEvent(property.PropertyType),
				// As the Razor compiler reads it: an override that leaves the attribute off isn't required.
				property.GetCustomAttribute<EditorRequiredAttribute>(inherit: false) is not null,
				parameter.CaptureUnmatchedValues));
		}

		return
		[
			.. rows.OrderBy(row => row.Inherited)
				.ThenBy(row => row.Group, StringComparer.Ordinal)
				.ThenBy(row => row.Name, StringComparer.Ordinal)
		];
	}

	// A generic component is closed over a type its constraints accept, so a fresh instance can give its defaults.
	private static Type? Closed(Type component)
	{
		if (!component.IsGenericTypeDefinition)
		{
			return component;
		}

		Type[] parameters = component.GetGenericArguments();
		Type[] arguments = new Type[parameters.Length];
		for (int i = 0; i < parameters.Length; i++)
		{
			Type? fits = StandIns.FirstOrDefault(candidate => Accepts(parameters[i], candidate));
			if (fits is null)
			{
				return null;
			}

			arguments[i] = fits;
		}

		return component.MakeGenericType(arguments);
	}

	private static bool Accepts(Type parameter, Type candidate)
	{
		GenericParameterAttributes flags = parameter.GenericParameterAttributes;
		if (flags.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint) && !candidate.IsValueType)
		{
			return false;
		}

		if (flags.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint) && candidate.IsValueType)
		{
			return false;
		}

		return parameter.GetGenericParameterConstraints().All(constraint =>
		{
			Type wanted = constraint.ContainsGenericParameters
				? constraint.GetGenericTypeDefinition().MakeGenericType(
					[.. constraint.GetGenericArguments().Select(argument => argument == parameter ? candidate : argument)])
				: constraint;
			return !wanted.ContainsGenericParameters && wanted.IsAssignableFrom(candidate);
		});
	}

	// Only a property with a compiler-made backing field is read, so no getter with logic of its own runs.
	private static string DefaultOf(object instance, PropertyInfo property)
	{
		FieldInfo? field = null;
		for (Type? type = instance.GetType(); type is not null && field is null; type = type.BaseType)
		{
			field = type.GetField($"<{property.Name}>k__BackingField",
				BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
		}

		return field is null ? string.Empty : Format(field.GetValue(instance), property.PropertyType);
	}

	private static string Format(object? value, Type type) => value switch
	{
		null => "null",
		string text => $"\"{text}\"",
		bool flag => flag ? "true" : "false",
		Enum member => $"{member.GetType().Name}.{member}",
		IFormattable number when value.GetType().IsPrimitive || value is decimal => number.ToString(null, CultureInfo.InvariantCulture),
		TimeSpan span => span.ToString("c", CultureInfo.InvariantCulture),
		Delegate => string.Empty,
		_ when IsEvent(type) => string.Empty,
		ICollection { Count: 0 } => "[]",
		ICollection collection => $"[{string.Join(", ", collection.Cast<object?>().Select(item => Format(item, item?.GetType() ?? typeof(object))))}]",
		_ when value.GetType().IsValueType => value.Equals(Activator.CreateInstance(value.GetType())) || value.ToString() is not { } shown || shown == value.GetType().FullName
			? string.Empty
			: shown,
		_ => "new()"
	};

	private static bool IsEvent(Type type) =>
		type == typeof(EventCallback) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>));

	private static string TypeName(Type type, NullabilityInfo? nullability)
	{
		if (Nullable.GetUnderlyingType(type) is { } underlying)
		{
			return TypeName(underlying, null) + "?";
		}

		string name;
		if (Keywords.TryGetValue(type, out string? keyword))
		{
			name = keyword;
		}
		else if (type.IsArray)
		{
			name = TypeName(type.GetElementType()!, nullability?.ElementType) + "[]";
		}
		else if (type.IsGenericType && type.Name.StartsWith("ValueTuple", StringComparison.Ordinal))
		{
			name = $"({string.Join(", ", type.GetGenericArguments().Select((argument, i) => TypeName(argument, ArgumentInfo(nullability, i))))})";
		}
		else if (type.IsGenericType)
		{
			Type[] arguments = type.GetGenericArguments();
			name = $"{WithoutArity(type.Name)}<{string.Join(", ", arguments.Select((argument, i) => TypeName(argument, ArgumentInfo(nullability, i))))}>";
		}
		else
		{
			name = type.Name;
		}

		bool nullable = !type.IsValueType && nullability?.ReadState == NullabilityState.Nullable;
		return nullable ? name + "?" : name;
	}

	private static NullabilityInfo? ArgumentInfo(NullabilityInfo? nullability, int index) =>
		nullability is not null && index < nullability.GenericTypeArguments.Length ? nullability.GenericTypeArguments[index] : null;

	private static Type Open(Type type) => type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;

	private static string WithoutArity(string name)
	{
		int tick = name.IndexOf('`', StringComparison.Ordinal);
		return tick < 0 ? name : name[..tick];
	}
}
