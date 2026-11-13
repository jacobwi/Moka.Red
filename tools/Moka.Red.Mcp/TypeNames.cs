using System.Reflection;

namespace Moka.Red.Mcp;

internal static class TypeNames
{
	private static readonly Dictionary<Type, string> Keywords = new()
	{
		[typeof(bool)] = "bool",
		[typeof(byte)] = "byte",
		[typeof(sbyte)] = "sbyte",
		[typeof(char)] = "char",
		[typeof(short)] = "short",
		[typeof(ushort)] = "ushort",
		[typeof(int)] = "int",
		[typeof(uint)] = "uint",
		[typeof(long)] = "long",
		[typeof(ulong)] = "ulong",
		[typeof(float)] = "float",
		[typeof(double)] = "double",
		[typeof(decimal)] = "decimal",
		[typeof(string)] = "string",
		[typeof(object)] = "object",
		[typeof(void)] = "void"
	};

	internal static string Of(Type type)
	{
		if (Keywords.TryGetValue(type, out string? keyword))
		{
			return keyword;
		}

		if (type.IsGenericParameter)
		{
			return type.Name;
		}

		if (type.IsArray)
		{
			return Of(type.GetElementType()!) + "[]";
		}

		if (Nullable.GetUnderlyingType(type) is { } underlying)
		{
			return Of(underlying) + "?";
		}

		string name = type.Name;
		int arity = name.IndexOf('`', StringComparison.Ordinal);
		if (arity >= 0)
		{
			name = name[..arity];
		}

		if (type.IsNested && !type.IsGenericParameter && type.DeclaringType is { } outer)
		{
			name = Of(outer) + "." + name;
		}

		if (!type.IsGenericType)
		{
			return name;
		}

		Type[] arguments = type.GetGenericArguments();
		if (type.IsNested && type.DeclaringType is { IsGenericType: true } declaring)
		{
			arguments = arguments[declaring.GetGenericArguments().Length..];
		}

		return arguments.Length == 0 ? name : $"{name}<{string.Join(", ", arguments.Select(Of))}>";
	}

	internal static string Of(PropertyInfo property, NullabilityInfoContext nullability)
	{
		string name = Of(property.PropertyType);
		bool nullableReference = !property.PropertyType.IsValueType
		                         && !property.PropertyType.IsGenericParameter
		                         && nullability.Create(property).ReadState == NullabilityState.Nullable;
		return nullableReference ? name + "?" : name;
	}
}
