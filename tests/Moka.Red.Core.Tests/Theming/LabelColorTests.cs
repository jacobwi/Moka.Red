using Moka.Red.Core.Enums;
using Moka.Red.Core.Theming;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Core.Tests.Theming;

// Label colors are names an app stores. The theme decides the shades, so a stored choice follows light, dark and tone.
public class LabelColorTests
{
	private static readonly string[] Keys =
		["red", "rose", "orange", "amber", "yellow", "lime", "green", "emerald", "teal", "cyan", "sky", "blue", "indigo", "violet", "purple", "pink", "slate"];

	// The board BC approved, in picker order. The dark scheme draws Vivid dots in exactly these.
	private static readonly string[] BoardHues =
	[
		"#EF4444", "#F43F5E", "#F97316", "#F59E0B", "#EAB308", "#84CC16", "#22C55E", "#10B981", "#14B8A6", "#06B6D4", "#0EA5E9", "#3B82F6",
		"#6366F1", "#8B5CF6", "#A855F7", "#EC4899", "#64748B"
	];

	public static TheoryData<string> Presets => ["Dark", "Light", "Red", "RedLight", "Gate", "GateLight", "Crisp", "CrispLight", "Matrix", "MatrixLight"];

	[Fact]
	public void All_IsSixteenHuesAroundTheWheel_ThenSlate()
	{
		Assert.Equal(17, MokaLabelColors.All.Count);
		Assert.Equal(Enum.GetValues<MokaLabelColor>(), MokaLabelColors.All);
		Assert.Equal(MokaLabelColor.Slate, MokaLabelColors.All[^1]);
	}

	[Fact]
	public void TheSmallerSets_AreTheBoardsTwelveAndEight_InPickerOrder_WithSlateLast()
	{
		Assert.Equal(13, MokaLabelColors.Standard.Count);
		Assert.Equal(9, MokaLabelColors.Distinct.Count);
		Assert.DoesNotContain(MokaLabelColor.Rose, MokaLabelColors.Standard);
		Assert.DoesNotContain(MokaLabelColor.Emerald, MokaLabelColors.Standard);
		Assert.DoesNotContain(MokaLabelColor.Sky, MokaLabelColors.Standard);
		Assert.DoesNotContain(MokaLabelColor.Violet, MokaLabelColors.Standard);

		foreach (IReadOnlyList<MokaLabelColor> set in new[] { MokaLabelColors.Standard, MokaLabelColors.Distinct })
		{
			Assert.Equal(MokaLabelColor.Slate, set[^1]);
			Assert.Equal(MokaLabelColors.All.Where(set.Contains), set);
		}

		Assert.All(MokaLabelColors.Distinct, color => Assert.Contains(color, MokaLabelColors.Standard));
	}

	[Fact]
	public void EachColor_HasItsTokenReferences()
	{
		Assert.Equal("teal", MokaLabelColors.Key(MokaLabelColor.Teal));
		Assert.Equal("var(--moka-label-teal)", MokaLabelColors.Solid(MokaLabelColor.Teal));
		Assert.Equal("var(--moka-label-teal-bg)", MokaLabelColors.Background(MokaLabelColor.Teal));
		Assert.Equal("var(--moka-label-teal-ink)", MokaLabelColors.Ink(MokaLabelColor.Teal));
		Assert.Equal(Keys, MokaLabelColors.All.Select(MokaLabelColors.Key));
	}

	[Fact]
	public void EachColor_HasAnEnglishName_AndAnAppCanGiveItsOwn()
	{
		Assert.Equal("Teal", MokaLabelColors.Name(MokaLabelColor.Teal));
		Assert.Equal("Slate", MokaLabelColors.Name(MokaLabelColor.Slate));
		Assert.Equal("Sarcelle", MokaLabelColors.Name(MokaLabelColor.Teal, c => c == MokaLabelColor.Teal ? "Sarcelle" : null));
		Assert.Equal("Red", MokaLabelColors.Name(MokaLabelColor.Red, c => c == MokaLabelColor.Teal ? "Sarcelle" : " "));
		Assert.Equal("Red", MokaLabelColors.Name(MokaLabelColor.Red, null));
	}

	[Theory]
	[InlineData("teal", MokaLabelColor.Teal)]
	[InlineData("Teal", MokaLabelColor.Teal)]
	[InlineData(" SLATE ", MokaLabelColor.Slate)]
	[InlineData("var(--moka-label-sky)", MokaLabelColor.Sky)]
	public void TryParse_ReadsAKeyANameOrAReference(string text, MokaLabelColor expected)
	{
		Assert.True(MokaLabelColors.TryParse(text, out MokaLabelColor color));
		Assert.Equal(expected, color);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("8")]
	[InlineData("tealish")]
	[InlineData("#14B8A6")]
	public void TryParse_RefusesAnythingElse(string? text) => Assert.False(MokaLabelColors.TryParse(text, out _));

	[Fact]
	public void AnUndefinedColor_Throws()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => MokaLabelColors.Solid((MokaLabelColor)17));
		Assert.Throws<ArgumentOutOfRangeException>(() => MokaLabelColors.Name((MokaLabelColor)(-1)));
	}

	[Theory]
	[MemberData(nameof(Presets))]
	public void EveryTheme_WritesASolidATintAndAnInkPerColor(string preset)
	{
		Dictionary<string, string> tokens = Tokens(Preset(preset).ToCssVariables());

		Assert.Equal(51, tokens.Keys.Count(name => name.StartsWith("--moka-label-", StringComparison.Ordinal)));
		foreach (string key in Keys)
		{
			Assert.Matches("^#[0-9A-F]{6}$", tokens[$"--moka-label-{key}"]);
			Assert.Matches(@"^color-mix\(in srgb, #[0-9A-F]{6} \d+%, var\(--moka-color-surface\)\)$", tokens[$"--moka-label-{key}-bg"]);
			Assert.Matches("^#[0-9A-F]{6}$", tokens[$"--moka-label-{key}-ink"]);
		}
	}

	[Fact]
	public void TheDarkVividDots_AreTheBoardsHues()
	{
		Dictionary<string, string> tokens = Tokens(MokaTheme.Red.ToCssVariables());

		Assert.Equal(BoardHues, Keys.Select(key => tokens[$"--moka-label-{key}"]));
		Assert.Equal("color-mix(in srgb, #14B8A6 20%, var(--moka-color-surface))", tokens["--moka-label-teal-bg"]);
		Assert.Equal("color-mix(in srgb, #14B8A6 16%, var(--moka-color-surface))", Tokens(MokaTheme.RedLight.ToCssVariables())["--moka-label-teal-bg"]);
	}

	[Fact]
	public void Vivid_IsTheDefault_AndEveryToneHasItsOwnShades()
	{
		Assert.Equal(MokaLabelTone.Vivid, MokaTheme.Red.LabelTone);
		Assert.Equal(MokaLabelTone.Vivid, MokaTheme.Dark.LabelTone);

		string[] teal = [.. Enum.GetValues<MokaLabelTone>().Select(tone => Tokens(MokaTheme.Red.WithLabelTone(tone).ToCssVariables())["--moka-label-teal"])];

		Assert.Equal(["#14B8A6", "#46B5A8", "#7ED8CE"], teal);
		Assert.Contains("--moka-label-teal-bg: color-mix(in srgb, #14B8A6 22%, var(--moka-color-surface));",
			MokaTheme.RedLight.WithLabelTone(MokaLabelTone.Pastel).ToCssVariables(), StringComparison.Ordinal);
	}

	[Fact]
	public void TheTone_IsAStyleChoice_TheOriginalDesignIgnores()
	{
		Assert.Same(MokaTheme.Dark, MokaTheme.Dark.WithLabelTone(MokaLabelTone.Pastel));
		Assert.Same(MokaTheme.Light, MokaTheme.Light.WithLabelTone(MokaLabelTone.Muted));

		MokaTheme forced = MokaTheme.Dark with { LabelTone = MokaLabelTone.Pastel };
		Assert.Equal(MokaTheme.Dark.ToCssVariables(), forced.ToCssVariables());
	}

	[Fact]
	public void TheTone_LeavesTheColorsAndEveryOtherTokenAlone()
	{
		MokaTheme pastel = MokaTheme.Gate.WithLabelTone(MokaLabelTone.Pastel);

		Assert.Equal(MokaTheme.Gate.Palette, pastel.Palette);
		Dictionary<string, string> before = Tokens(MokaTheme.Gate.ToCssVariables());
		Dictionary<string, string> after = Tokens(pastel.ToCssVariables());
		Assert.Equal(
			before.Where(token => !token.Key.StartsWith("--moka-label-", StringComparison.Ordinal)),
			after.Where(token => !token.Key.StartsWith("--moka-label-", StringComparison.Ordinal)));
	}

	[Fact]
	public void TheTone_TakesPartInEqualityAndCopies()
	{
		MokaTheme muted = MokaTheme.Red.WithLabelTone(MokaLabelTone.Muted);

		Assert.NotEqual(MokaTheme.Red, muted);
		Assert.Equal(MokaTheme.Red with { LabelTone = MokaLabelTone.Muted }, muted);
		Assert.Equal(MokaTheme.Red, muted.WithLabelTone(MokaLabelTone.Vivid));
		Assert.Equal(MokaLabelTone.Muted, muted.WithDark(false).LabelTone);
		Assert.Equal(MokaLabelTone.Muted, muted.WithSurfaces(MokaSurfaces.BlueGrey).WithAccent(MokaAccent.Cyan).LabelTone);
	}

	// Without a provider, moka.css draws labels exactly as the original design's themes write them.
	[Fact]
	public void MokaCss_HoldsTheOriginalDesignsValues()
	{
		IReadOnlyDictionary<string, string> root = ScopedCss.Declarations("moka.css", ":root");
		IReadOnlyDictionary<string, string> dark = ScopedCss.Declarations("moka.css", ".moka-dark");

		foreach ((string name, string value) in Tokens(MokaTheme.Light.ToCssVariables()).Where(t => t.Key.StartsWith("--moka-label-", StringComparison.Ordinal)))
		{
			Assert.Equal(value, root[name]);
		}

		foreach ((string name, string value) in Tokens(MokaTheme.Dark.ToCssVariables()).Where(t => t.Key.StartsWith("--moka-label-", StringComparison.Ordinal)))
		{
			Assert.Equal(value, dark[name]);
		}
	}

	internal static MokaTheme Preset(string name) => name switch
	{
		"Dark" => MokaTheme.Dark,
		"Light" => MokaTheme.Light,
		"Red" => MokaTheme.Red,
		"RedLight" => MokaTheme.RedLight,
		"Gate" => MokaTheme.Gate,
		"GateLight" => MokaTheme.GateLight,
		"Crisp" => MokaTheme.Crisp,
		"CrispLight" => MokaTheme.CrispLight,
		"Matrix" => MokaTheme.Matrix,
		"MatrixLight" => MokaTheme.MatrixLight,
		_ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
	};

	internal static Dictionary<string, string> Tokens(string css) =>
		css.Split("; ", StringSplitOptions.RemoveEmptyEntries)
			.Select(pair => pair.Split(": ", 2))
			.ToDictionary(pair => pair[0].Trim(), pair => pair[1].TrimEnd(';').Trim(), StringComparer.Ordinal);
}
