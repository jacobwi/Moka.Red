using System.Diagnostics.CodeAnalysis;
using Moka.Red.Core.Enums;

namespace Moka.Red.Core.Theming;

/// <summary>
///     The CSS for each <see cref="MokaLabelColor" />, an English name for it, and the sets to offer in a picker. Each
///     value is a <c>var()</c> reference to one of the theme's label tokens, so it follows light and dark and
///     <see cref="MokaTheme.WithLabelTone" /> without the app knowing any color.
/// </summary>
public static class MokaLabelColors
{
	private static readonly MokaLabelColor[] Ordered = Enum.GetValues<MokaLabelColor>();

	private static readonly string[] Keys =
	[
		"red", "rose", "orange", "amber", "yellow", "lime", "green", "emerald", "teal", "cyan", "sky", "blue", "indigo",
		"violet", "purple", "pink", "slate"
	];

	private static readonly string[] Names =
	[
		"Red", "Rose", "Orange", "Amber", "Yellow", "Lime", "Green", "Emerald", "Teal", "Cyan", "Sky", "Blue", "Indigo",
		"Violet", "Purple", "Pink", "Slate"
	];

	private static readonly string[] Solids = [.. Keys.Select(key => $"var(--moka-label-{key})")];

	private static readonly string[] Backgrounds = [.. Keys.Select(key => $"var(--moka-label-{key}-bg)")];

	private static readonly string[] Inks = [.. Keys.Select(key => $"var(--moka-label-{key}-ink)")];

	/// <summary>Every label color in picker order: sixteen hues around the wheel, then slate.</summary>
	public static IReadOnlyList<MokaLabelColor> All { get; } = Ordered;

	/// <summary>
	///     Twelve hues and slate: red, orange, amber, yellow, lime, green, teal, cyan, blue, indigo, purple and pink.
	///     Enough to tell many tags apart, each still distinct.
	/// </summary>
	public static IReadOnlyList<MokaLabelColor> Standard { get; } =
	[
		MokaLabelColor.Red, MokaLabelColor.Orange, MokaLabelColor.Amber, MokaLabelColor.Yellow, MokaLabelColor.Lime,
		MokaLabelColor.Green, MokaLabelColor.Teal, MokaLabelColor.Cyan, MokaLabelColor.Blue, MokaLabelColor.Indigo,
		MokaLabelColor.Purple, MokaLabelColor.Pink, MokaLabelColor.Slate
	];

	/// <summary>
	///     Eight hues and slate: red, orange, yellow, green, teal, blue, purple and pink. Fewer, so no two look alike,
	///     even for people who see color differently.
	/// </summary>
	public static IReadOnlyList<MokaLabelColor> Distinct { get; } =
	[
		MokaLabelColor.Red, MokaLabelColor.Orange, MokaLabelColor.Yellow, MokaLabelColor.Green, MokaLabelColor.Teal,
		MokaLabelColor.Blue, MokaLabelColor.Purple, MokaLabelColor.Pink, MokaLabelColor.Slate
	];

	/// <summary>
	///     The color's lowercase key, such as <c>teal</c>: the part of its token names after <c>--moka-label-</c>. Stable,
	///     so it suits storage as well as the enum's name does.
	/// </summary>
	/// <param name="color">The label color.</param>
	/// <returns>The key.</returns>
	/// <exception cref="ArgumentOutOfRangeException">The value is not a defined <see cref="MokaLabelColor" />.</exception>
	public static string Key(MokaLabelColor color) => Keys[Index(color)];

	/// <summary>
	///     The solid color, for dots, icons and swatches: <c>var(--moka-label-teal)</c>. It stands out 3:1 against the
	///     theme's surfaces.
	/// </summary>
	/// <param name="color">The label color.</param>
	/// <returns>A CSS <c>var()</c> reference.</returns>
	/// <exception cref="ArgumentOutOfRangeException">The value is not a defined <see cref="MokaLabelColor" />.</exception>
	public static string Solid(MokaLabelColor color) => Solids[Index(color)];

	/// <summary>The soft tint behind a tag: <c>var(--moka-label-teal-bg)</c>.</summary>
	/// <param name="color">The label color.</param>
	/// <returns>A CSS <c>var()</c> reference.</returns>
	/// <exception cref="ArgumentOutOfRangeException">The value is not a defined <see cref="MokaLabelColor" />.</exception>
	public static string Background(MokaLabelColor color) => Backgrounds[Index(color)];

	/// <summary>Text on the tint, readable at 4.5:1: <c>var(--moka-label-teal-ink)</c>.</summary>
	/// <param name="color">The label color.</param>
	/// <returns>A CSS <c>var()</c> reference.</returns>
	/// <exception cref="ArgumentOutOfRangeException">The value is not a defined <see cref="MokaLabelColor" />.</exception>
	public static string Ink(MokaLabelColor color) => Inks[Index(color)];

	/// <summary>The color's English name, such as <c>Teal</c>.</summary>
	/// <param name="color">The label color.</param>
	/// <returns>The name.</returns>
	/// <exception cref="ArgumentOutOfRangeException">The value is not a defined <see cref="MokaLabelColor" />.</exception>
	public static string Name(MokaLabelColor color) => Names[Index(color)];

	/// <summary>
	///     The color's name from <paramref name="names" />, for an app with words of its own, or the English name when it
	///     gives none.
	/// </summary>
	/// <param name="color">The label color.</param>
	/// <param name="names">The app's names. A null or blank answer falls back to the English name.</param>
	/// <returns>The name.</returns>
	/// <exception cref="ArgumentOutOfRangeException">The value is not a defined <see cref="MokaLabelColor" />.</exception>
	public static string Name(MokaLabelColor color, Func<MokaLabelColor, string?>? names)
	{
		string? own = names?.Invoke(color);
		return string.IsNullOrWhiteSpace(own) ? Name(color) : own;
	}

	/// <summary>
	///     Reads a stored label color: its key (<c>teal</c>), its name in any case (<c>Teal</c>), or its
	///     <see cref="Solid" /> reference. Numbers are not read, since the enum's values are no promise.
	/// </summary>
	/// <param name="value">The text to read.</param>
	/// <param name="color">The label color, when it could be read.</param>
	/// <returns><c>true</c> when <paramref name="value" /> names a label color.</returns>
	public static bool TryParse([NotNullWhen(true)] string? value, out MokaLabelColor color)
	{
		color = default;
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}

		string text = value.Trim();
		for (int i = 0; i < Keys.Length; i++)
		{
			if (string.Equals(text, Keys[i], StringComparison.OrdinalIgnoreCase)
			    || string.Equals(text, Solids[i], StringComparison.OrdinalIgnoreCase))
			{
				color = Ordered[i];
				return true;
			}
		}

		return false;
	}

	private static int Index(MokaLabelColor color) => (uint)color < (uint)Ordered.Length
		? (int)color
		: throw new ArgumentOutOfRangeException(nameof(color), color, "Not a label color.");
}
