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
		return (MokaTheme)Fill(Copy(), ref counter, importable: false);
	}

	/// <summary>
	///     Like <see cref="EveryPropertySet" />, with values the JSON import accepts: a color for every palette
	///     value, a length for every font size and spacing value, and for the rest text with quotes, tabs and
	///     line breaks that a JSON string must escape.
	/// </summary>
	public static MokaTheme EveryPropertySetImportable()
	{
		int counter = 0;
		return (MokaTheme)Fill(Copy(), ref counter, importable: true);
	}

	// Fill writes through init-only setters, and the built-in themes are shared instances.
	private static MokaTheme Copy() => MokaTheme.Light with
	{
		Palette = MokaPalette.Light with { },
		Typography = MokaTypography.Default with { },
		Spacing = MokaSpacing.Default with { }
	};

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
			else if (type == typeof(MokaLook))
			{
				value = MokaLook.Gate;
			}
			else if (type == typeof(MokaSurfaces))
			{
				value = MokaSurfaces.NearBlack;
			}
			else if (type == typeof(MokaSelection))
			{
				value = MokaSelection.Glow;
			}
			else if (type == typeof(MokaMainButton))
			{
				value = MokaMainButton.Outline;
			}
			else if (type == typeof(MokaCurrentPlace))
			{
				value = MokaCurrentPlace.Solid;
			}
			else if (type == typeof(MokaStatusBarLook))
			{
				value = MokaStatusBarLook.Terminal;
			}
			else if (type == typeof(MokaToastLook))
			{
				value = MokaToastLook.Snackbar;
			}
			else if (type == typeof(MokaToastStacking))
			{
				value = MokaToastStacking.Pile;
			}
			else if (type == typeof(MokaNotificationLook))
			{
				value = MokaNotificationLook.Timeline;
			}
			else if (type == typeof(MokaPopupLook))
			{
				value = MokaPopupLook.AccentTint;
			}
			else if (type == typeof(MokaListLook))
			{
				value = MokaListLook.Attached;
			}
			else if (type == typeof(MokaDialogLook))
			{
				value = MokaDialogLook.Centered;
			}
			else if (type == typeof(MokaButtonLook))
			{
				value = MokaButtonLook.Offset;
			}
			else if (type == typeof(MokaDisabledButtonLook))
			{
				value = MokaDisabledButtonLook.Outline;
			}
			else if (type == typeof(MokaTooltipLook))
			{
				value = MokaTooltipLook.Contrast;
			}
			else if (type == typeof(MokaTooltipMotion))
			{
				value = MokaTooltipMotion.Grow;
			}
			else if (type == typeof(MokaTooltipSize))
			{
				value = MokaTooltipSize.Roomy;
			}
			else if (type == typeof(MokaElevation))
			{
				value = MokaElevation.Line;
			}
			else if (type == typeof(MokaTextButtonColor))
			{
				value = MokaTextButtonColor.Neutral;
			}
			else if (type == typeof(MokaCalloutLook))
			{
				value = MokaCalloutLook.Edge;
			}
			else if (type == typeof(MokaSwitchLook))
			{
				value = MokaSwitchLook.Tinted;
			}
			else if (type == typeof(MokaSettingsHeaderLook))
			{
				value = MokaSettingsHeaderLook.Tinted;
			}
			else if (type == typeof(MokaLabelTone))
			{
				value = MokaLabelTone.Pastel;
			}
			else if (type == typeof(MokaChartLook))
			{
				value = MokaChartLook.Bold;
			}
			else if (type == typeof(MokaChartPalette))
			{
				value = MokaChartPalette.Muted;
			}
			else if (type == typeof(MokaChartTexture))
			{
				value = MokaChartTexture.Hatched;
			}
			else if (type == typeof(MokaChartPrint))
			{
				value = MokaChartPrint.Ink;
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
