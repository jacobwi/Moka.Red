using System.Globalization;
using System.Text;
using Moka.Red.Core.Colors;

namespace Moka.Red.Core.Theming;

/// <summary>How much contrast a pair of tokens has to have. See <see cref="MokaTheme.CheckContrast" />.</summary>
public enum MokaContrastRequirement
{
	/// <summary>Listed for information; nothing is required of it.</summary>
	Informational,

	/// <summary>Icons, marks and the lines of controls: 3:1 (WCAG 1.4.11).</summary>
	Graphics,

	/// <summary>
	///     Text: 4.5:1 (WCAG AA). On a theme whose seed asks for <see cref="MokaSeedContrast.More" />, 7:1 for text the seed
	///     derives; text on the accent and on label tints stays at 4.5:1, since the seed keeps the accent as given and the
	///     label colors are the theme's tone.
	/// </summary>
	Text
}

/// <summary>One text-on-background or line-on-background pair a theme draws, measured.</summary>
/// <param name="Foreground">The token drawn on top, such as <c>--moka-color-on-surface</c>.</param>
/// <param name="Background">The token it sits on, such as <c>--moka-color-surface</c>.</param>
/// <param name="Use">What draws the pair.</param>
/// <param name="ForegroundColor">The foreground as resolved, before it is drawn over the background.</param>
/// <param name="BackgroundColor">The background as resolved, drawn over the card surface when it is translucent.</param>
/// <param name="Ratio">The WCAG 2 contrast ratio.</param>
/// <param name="Apca">APCA's lightness contrast, Lc.</param>
/// <param name="Requirement">What the pair has to meet.</param>
/// <param name="RequiredRatio">The ratio it has to meet; 0 when nothing is required.</param>
public sealed record MokaContrastCheck(
	string Foreground,
	string Background,
	string Use,
	MokaSrgb ForegroundColor,
	MokaSrgb BackgroundColor,
	double Ratio,
	double Apca,
	MokaContrastRequirement Requirement,
	double RequiredRatio)
{
	/// <summary>Whether the pair meets what is required of it.</summary>
	public bool Passes => Ratio >= RequiredRatio;
}

/// <summary>
///     Every text-on-background and line-on-background pair the library draws from a theme's tokens, measured (see
///     <see cref="MokaTheme.CheckContrast" />).
/// </summary>
public sealed class MokaContrastReport
{
	internal MokaContrastReport(IReadOnlyList<MokaContrastCheck> checks, IReadOnlyList<string> unresolved)
	{
		Checks = checks;
		Unresolved = unresolved;
	}

	/// <summary>Every pair measured.</summary>
	public IReadOnlyList<MokaContrastCheck> Checks { get; }

	/// <summary>Pairs that were left out because a token didn't resolve to one color, written as <c>foreground on background</c>.</summary>
	public IReadOnlyList<string> Unresolved { get; }

	/// <summary>The pairs that miss what is required of them.</summary>
	public IEnumerable<MokaContrastCheck> Failures => Checks.Where(check => !check.Passes);

	/// <summary>Whether every pair meets what is required of it.</summary>
	public bool Passes => Checks.All(check => check.Passes);

	/// <summary>One line per pair: the tokens, the resolved colors, the ratio, Lc, the requirement and whether it passes.</summary>
	/// <returns>The report as text.</returns>
	public override string ToString()
	{
		var text = new StringBuilder();
		foreach (MokaContrastCheck check in Checks)
		{
			text.Append(CultureInfo.InvariantCulture,
				$"{(check.Passes ? "pass" : "FAIL")} {check.Ratio,6:0.00} Lc {check.Apca,7:0.0} needs {check.RequiredRatio:0.#} ");
			text.Append(CultureInfo.InvariantCulture,
				$"{check.Foreground} {check.ForegroundColor.ToHex(withAlpha: true)} on {check.Background} {check.BackgroundColor.ToHex()} ({check.Use})\n");
		}

		foreach (string pair in Unresolved)
		{
			text.Append("unresolved ").Append(pair).Append('\n');
		}

		return text.ToString();
	}

	internal static MokaContrastReport For(MokaTheme theme)
	{
		var resolver = new MokaTokenResolver(theme.ToTokenMap());
		bool more = theme.Seed?.Contrast == MokaSeedContrast.More;
		resolver.TryResolve("--moka-color-surface", out MokaSrgb card);
		MokaSrgb page = card.Over(MokaSrgb.Black);

		List<MokaContrastCheck> checks = [];
		List<string> unresolved = [];
		foreach ((string foreground, string background, MokaContrastRequirement need, bool derived, string use) in Pairs(theme, more))
		{
			if (!resolver.TryResolve(foreground, out MokaSrgb top) || !resolver.TryResolve(background, out MokaSrgb under))
			{
				unresolved.Add($"{foreground} on {background}");
				continue;
			}

			MokaSrgb ground = under.Over(page);
			double required = need switch
			{
				MokaContrastRequirement.Text => more && derived ? MokaContrast.EnhancedTextRatio : MokaContrast.TextRatio,
				MokaContrastRequirement.Graphics => MokaContrast.LargeTextRatio,
				_ => 0
			};
			checks.Add(new MokaContrastCheck(foreground, background, use, top, ground,
				MokaContrast.Ratio(top, ground), MokaContrast.Apca(top, ground), need, required));
		}

		return new MokaContrastReport(checks, unresolved);
	}

	// Each pair says whether a seed derives it, which decides whether More holds it to 7:1.
	private static IEnumerable<(string Foreground, string Background, MokaContrastRequirement Need, bool Derived, string Use)> Pairs(MokaTheme theme, bool more)
	{
		const MokaContrastRequirement text = MokaContrastRequirement.Text;
		const MokaContrastRequirement graphics = MokaContrastRequirement.Graphics;
		const MokaContrastRequirement info = MokaContrastRequirement.Informational;

		foreach (string ground in new[] { "surface", "surface-variant", "background", "surface-2" })
		{
			string on = $"--moka-color-{ground}";
			yield return ("--moka-color-on-surface", on, text, true, "Body text");
			yield return ("--moka-color-on-surface-2", on, text, true, "Labels and secondary text");
			yield return ("--moka-color-on-surface-variant", on, text, true, "Helper and muted text");
			yield return ("--moka-color-on-surface-tertiary", on, text, true, "Captions and timestamps");
			yield return ("--moka-color-icon", on, graphics, true, "Icons");
		}

		yield return ("--moka-color-on-background", "--moka-color-background", text, true, "Text on the page");
		yield return ("--moka-color-primary-light", "--moka-color-surface", text, true, "Links and accent text on cards");
		yield return ("--moka-color-primary-light", "--moka-color-surface-variant", text, true, "Links and accent text on panels");
		yield return ("--moka-color-primary-text", "--moka-color-primary-bg", text, true, "Text on the selected tint");
		yield return ("--moka-color-on-primary", "--moka-color-primary", text, false, "Text on the accent");
		yield return ("--moka-button-primary-color", "--moka-button-primary-bg", text, false, "A primary button's label");
		yield return ("--moka-button-text-color", "--moka-color-surface", text, true, "A text button's label");
		yield return ("--moka-color-on-secondary", "--moka-color-secondary", text, true, "Text on the second accent");
		yield return ("--moka-current-color", "--moka-current-bg", text, true, "The current place's label");

		foreach (string meaning in new[] { "success", "warning", "error", "info" })
		{
			string color = $"--moka-color-{meaning}";
			yield return ($"--moka-color-on-{meaning}", color, text, true, $"Text on the {meaning} color");
			yield return ($"{color}-ink", $"{color}-bg", text, true, $"Short text on the {meaning} tint");
			yield return ($"{color}-text", $"{color}-bg", text, true, $"Longer text on the {meaning} tint");
			yield return (color, "--moka-color-surface", graphics, true, $"The {meaning} color as a mark on cards");
		}

		yield return ("--moka-color-error-soft", "--moka-color-surface", text, true, "Field errors and danger links on cards");
		yield return ("--moka-color-error-soft", "--moka-color-error-bg", text, true, "Field errors on the error tint");

		string code = theme.Look == MokaLook.Gate ? "--moka-color-surface-2" : "--moka-color-surface-variant";
		foreach (string part in new[] { "text", "tag", "attribute", "directive" })
		{
			yield return ($"--moka-code-{part}", code, text, true, $"Code: {part}");
		}

		foreach (string part in new[] { "string", "keyword", "type", "number" })
		{
			yield return ($"--moka-code-{part}", code, text, false, $"Code: {part}, in a label ink");
		}

		yield return ("--moka-code-comment", code, graphics, true, "Code: comments");
		yield return ("--moka-code-punctuation", code, graphics, true, "Code: punctuation");

		for (int i = 0; i < MokaLabelPalette.Names.Count; i += 3)
		{
			string solid = MokaLabelPalette.Names[i];
			yield return ($"{solid}-ink", $"{solid}-bg", text, false, "A label's text on its tint");
			yield return (solid, "--moka-color-surface", graphics, true, "A label's dot on cards");
		}

		yield return ("--moka-color-outline-strong", "--moka-color-surface", graphics, true, "The edge of buttons and key caps on cards");
		yield return ("--moka-color-outline-strong", "--moka-color-background", graphics, true, "The edge of buttons and key caps on the page");
		yield return ("--moka-color-outline", "--moka-color-surface", more ? graphics : info, true, "Field borders");
		yield return ("--moka-color-outline-variant", "--moka-color-surface", info, true, "Dividers and card edges");
		yield return ("--moka-color-primary", "--moka-color-surface", info, true, "The accent as a mark on cards");
		yield return ("--moka-color-primary", "--moka-color-background", info, true, "The accent as a mark on the page");
	}
}
