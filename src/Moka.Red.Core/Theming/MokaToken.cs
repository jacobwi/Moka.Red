using System.Diagnostics.CodeAnalysis;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Core.Theming;

/// <summary>What a token is for, for grouping them in a list such as a token reference page.</summary>
public enum MokaTokenGroup
{
	/// <summary>Colors of surfaces, text, lines, the accent and the four meanings (<c>--moka-color-*</c>).</summary>
	Color,

	/// <summary>The label colors people pick for tags and folders (<c>--moka-label-*</c>).</summary>
	Label,

	/// <summary>Chart series, ramps and the greys behind the marks (<c>--moka-chart-*</c>).</summary>
	Chart,

	/// <summary>The colors of highlighted code (<c>--moka-code-*</c>).</summary>
	Code,

	/// <summary>Fonts, text sizes, weights, line heights, letter case and spacing between letters.</summary>
	Typography,

	/// <summary>The spacing scale and the density and text scale factors.</summary>
	Spacing,

	/// <summary>Corner radii.</summary>
	Radius,

	/// <summary>Gaps, paddings and the heights of controls and bars.</summary>
	Layout,

	/// <summary>Shadows and rings that lift things off the page.</summary>
	Elevation,

	/// <summary>Transition timings.</summary>
	Motion,

	/// <summary>The stacking order of floating layers (<c>--moka-z-*</c>).</summary>
	Layer,

	/// <summary>Focus rings, selection, hover and disabled states.</summary>
	Interaction,

	/// <summary>Values one kind of component reads: buttons, the current place, callouts and switches.</summary>
	Component,

	/// <summary>What sits behind modal overlays.</summary>
	Overlay
}

/// <summary>What kind of CSS value a token holds, which decides how <see cref="MokaTheme.WithToken" /> checks a value.</summary>
public enum MokaTokenKind
{
	/// <summary>A color: hex, a color function, a keyword, <c>color-mix()</c> or <c>var()</c>.</summary>
	Color,

	/// <summary>A length, a list of lengths such as a padding, or a <c>calc()</c>, <c>min()</c>, <c>max()</c>, <c>clamp()</c> or <c>var()</c> of lengths.</summary>
	Length,

	/// <summary>Anything else: a shadow, a font stack, a timing, a number or a keyword.</summary>
	Other
}

/// <summary>
///     One CSS custom property a <see cref="MokaTheme" /> writes: its name, what it is for, and the value
///     <c>moka.css</c> gives it when no theme provider is on the page. <see cref="MokaTokens.All" /> lists them all, and
///     <see cref="MokaTheme.ToTokenMap" /> gives one theme's values.
/// </summary>
public sealed class MokaToken
{
	internal MokaToken(string name, MokaTokenGroup group, MokaTokenKind kind, string description, Func<MokaTokenContext, string> produce)
	{
		Name = name;
		Group = group;
		Kind = kind;
		Description = description;
		Produce = produce;
	}

	/// <summary>The custom property's name, such as <c>--moka-color-primary</c>.</summary>
	public string Name { get; }

	/// <summary>What the token is for.</summary>
	public MokaTokenGroup Group { get; }

	/// <summary>What kind of CSS value it holds.</summary>
	public MokaTokenKind Kind { get; }

	/// <summary>A short sentence saying what reads it.</summary>
	public string Description { get; }

	/// <summary>
	///     The value <c>moka.css</c> declares on <c>:root</c>, for components drawn without a theme provider: what
	///     <see cref="MokaTheme.Light" /> writes.
	/// </summary>
	public string Fallback => MokaTokens.Fallbacks[Name].Light;

	/// <summary>
	///     The value <c>moka.css</c> declares again under <c>.moka-dark</c>: what <see cref="MokaTheme.Dark" /> writes, when it
	///     differs from <see cref="Fallback" /> or reads a token that does (a custom property resolves its <c>var()</c>
	///     where it is declared). Null when the <c>:root</c> value serves both.
	/// </summary>
	public string? DarkFallback => MokaTokens.Fallbacks[Name].Dark;

	internal Func<MokaTokenContext, string> Produce { get; }

	/// <summary>Whether <paramref name="value" /> can stand as this token's value: safe to write, and of the token's kind.</summary>
	/// <param name="value">A CSS value.</param>
	/// <returns><c>true</c> when the value can be written.</returns>
	public bool Accepts([NotNullWhen(true)] string? value)
	{
		if (!CssValues.IsSafe(value) || !CssValues.IsSelfContained(value) || string.IsNullOrWhiteSpace(value))
		{
			return false;
		}

		return Kind switch
		{
			MokaTokenKind.Color => CssValues.IsColor(value),
			MokaTokenKind.Length => TopLevelParts(value).All(CssValues.IsLengthExpression),
			_ => true
		};
	}

	// A padding holds up to four lengths, each of which may be a calc() or var() with spaces inside.
	internal static List<string> TopLevelParts(string value)
	{
		List<string> parts = [];
		int depth = 0;
		int start = 0;
		for (int i = 0; i <= value.Length; i++)
		{
			char c = i < value.Length ? value[i] : ' ';
			if (c == '(')
			{
				depth++;
			}
			else if (c == ')')
			{
				depth = Math.Max(0, depth - 1);
			}
			else if (char.IsWhiteSpace(c) && depth == 0)
			{
				if (i > start)
				{
					parts.Add(value[start..i]);
				}

				start = i + 1;
			}
		}

		return parts;
	}

	/// <inheritdoc />
	public override string ToString() => Name;
}
