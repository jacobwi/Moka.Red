using System.Globalization;
using System.Reflection;
using Moka.Red.Core.Theming;

namespace Moka.Red.ThemeGen.Tests;

/// <summary>
///     Walks every public property of <see cref="MokaTheme" /> and the records it holds, so a property
///     added later is covered without touching the tests.
/// </summary>
internal static class ThemeReflection
{
	private static readonly Type[] NestedRecords = [typeof(MokaPalette), typeof(MokaTypography), typeof(MokaSpacing)];

	/// <summary>
	///     A theme where every property holds a value of its own that no built-in theme has. A property a
	///     serializer drops then reads back as some default and the comparison names it. The strings carry
	///     quotes, backslashes and line breaks, which a C# literal must escape.
	/// </summary>
	public static MokaTheme EveryPropertySet()
	{
		int counter = 0;
		return (MokaTheme)Fill(MokaTheme.Light, ref counter, importable: false);
	}

	/// <summary>
	///     Like <see cref="EveryPropertySet" />, with values the JSON import accepts: a color for every palette
	///     value, a length for every font size and spacing value, and for the rest text with quotes, tabs and
	///     line breaks that a JSON string must escape.
	/// </summary>
	public static MokaTheme EveryPropertySetImportable()
	{
		int counter = 0;
		return (MokaTheme)Fill(MokaTheme.Light, ref counter, importable: true);
	}

	/// <summary>Asserts each property of <paramref name="actual" /> equals the one in <paramref name="expected" />.</summary>
	public static void AssertSameProperties(object expected, object actual, string path = nameof(MokaTheme))
	{
		foreach (PropertyInfo property in PublicProperties(expected.GetType()))
		{
			object? expectedValue = property.GetValue(expected);
			object? actualValue = property.GetValue(actual);
			string propertyPath = $"{path}.{property.Name}";

			if (NestedRecords.Contains(property.PropertyType))
			{
				Assert.NotNull(actualValue);
				AssertSameProperties(expectedValue!, actualValue, propertyPath);
				continue;
			}

			Assert.True(Equals(expectedValue, actualValue),
				$"{propertyPath} was not round-tripped: expected '{expectedValue}', got '{actualValue}'.");
		}
	}

	public static IEnumerable<PropertyInfo> PublicProperties(Type type) =>
		type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

	private static object Fill(object instance, ref int counter, bool importable)
	{
		foreach (PropertyInfo property in PublicProperties(instance.GetType()))
		{
			Type type = property.PropertyType;
			object? value;
			if (type == typeof(string))
			{
				counter++;
				value = importable
					? ImportableValue(instance, property.Name, counter)
					: string.Create(CultureInfo.InvariantCulture,
						$"v{counter} \"quoted\" back\\slash 'single'\ttab{(char)0x2028}sep{(char)0x85}next\nline");
			}
			else if (type == typeof(bool))
			{
				value = !(bool)property.GetValue(instance)!;
			}
			else if (type == typeof(double))
			{
				counter++;
				value = 1 + (counter / 8.0);
			}
			else if (NestedRecords.Contains(type))
			{
				value = Fill(property.GetValue(instance)!, ref counter, importable);
			}
			else
			{
				throw new NotSupportedException(
					$"{instance.GetType().Name}.{property.Name} is a {type.Name}. Teach ThemeReflection to fill it.");
			}

			// Init-only setters are ordinary setters to reflection.
			property.SetValue(instance, value);
		}

		return instance;
	}

	private static string ImportableValue(object instance, string name, int counter)
	{
		if (instance is MokaPalette)
		{
			return string.Create(CultureInfo.InvariantCulture, $"#{counter:x6}");
		}

		if (instance is MokaSpacing || name.StartsWith("FontSize", StringComparison.Ordinal))
		{
			return string.Create(CultureInfo.InvariantCulture, $"{counter}.5px");
		}

		return string.Create(CultureInfo.InvariantCulture,
			$"v{counter} \"quoted\" 'single'\ttab{(char)0x2028}sep{(char)0x85}next\nline");
	}
}
