using System.Text;
using Moka.Red.Core.Colors;
using Moka.Red.Core.Theming;

namespace Moka.Red.ThemeGen.Serialization;

// A theme written as a start (a preset, or a seed) and the With calls that turn the start into it. Every recipe is
// replayed on the start before it is used, so one that wouldn't give back an equal theme is never written.
internal sealed class ThemeRecipe
{
	private readonly List<string> _calls = [];
	private MokaTheme _current;

	private ThemeRecipe(string start, MokaTheme theme)
	{
		Start = start;
		_current = theme;
	}

	public string Start { get; }

	public IReadOnlyList<string> Calls => _calls;

	public static ThemeRecipe? Find(MokaTheme target)
	{
		ThemeRecipe? best = null;
		foreach ((string code, MokaTheme start) in Starts(target))
		{
			ThemeRecipe recipe = new(code, start);
			if (recipe.Reach(target) && (best is null || recipe._calls.Count < best._calls.Count))
			{
				best = recipe;
			}
		}

		return best;
	}

	public string ToCode()
	{
		StringBuilder code = new(Start);
		foreach (string call in _calls)
		{
			code.AppendLine().Append('\t').Append(call);
		}

		return code.ToString();
	}

	private static IEnumerable<(string Code, MokaTheme Theme)> Starts(MokaTheme target)
	{
		(string Name, MokaTheme Theme)[] presets = target.IsDark
			?
			[
				(nameof(MokaTheme.Red), MokaTheme.Red), (nameof(MokaTheme.Gate), MokaTheme.Gate), (nameof(MokaTheme.Crisp), MokaTheme.Crisp),
				(nameof(MokaTheme.Matrix), MokaTheme.Matrix), (nameof(MokaTheme.Ember), MokaTheme.Ember), (nameof(MokaTheme.Dark), MokaTheme.Dark)
			]
			:
			[
				(nameof(MokaTheme.RedLight), MokaTheme.RedLight), (nameof(MokaTheme.GateLight), MokaTheme.GateLight),
				(nameof(MokaTheme.CrispLight), MokaTheme.CrispLight), (nameof(MokaTheme.MatrixLight), MokaTheme.MatrixLight),
				(nameof(MokaTheme.EmberLight), MokaTheme.EmberLight), (nameof(MokaTheme.Light), MokaTheme.Light)
			];

		if (target.Seed is not { } seed)
		{
			foreach ((string name, MokaTheme theme) in presets.Where(preset => preset.Theme.Look == target.Look))
			{
				yield return ($"{nameof(MokaTheme)}.{name}", theme);
			}

			yield break;
		}

		if (!CanDerive(seed))
		{
			yield break;
		}

		string seedCode = SeedCode(seed);
		yield return ($"{nameof(MokaTheme)}.{nameof(MokaTheme.FromSeed)}({seedCode}, dark: {Bool(target.IsDark)})", MokaTheme.FromSeed(seed, target.IsDark));
		foreach ((string name, MokaTheme theme) in presets.Where(preset => preset.Theme.Look == MokaLook.Gate))
		{
			yield return ($"{nameof(MokaTheme)}.{name}.{nameof(MokaTheme.WithSeed)}({seedCode})", theme.WithSeed(seed));
		}
	}

	private bool Reach(MokaTheme target)
	{
		if (target.Seed is null && target.Look == MokaLook.Gate && target.Surfaces != _current.Surfaces)
		{
			MokaSurfaces surfaces = target.Surfaces;
			Step($".{nameof(MokaTheme.WithSurfaces)}({nameof(MokaSurfaces)}.{surfaces})", theme => theme.WithSurfaces(surfaces));
		}

		if (target.Seed is null && !Equals(target.Accent, _current.Accent) && !TakeAccent(target))
		{
			return false;
		}

		TakeDensity(target);

		// The choices that live only in the type and spacing tokens, with no property of their own.
		Choose<MokaTextSize>(target, nameof(MokaTheme.WithTextSize), (theme, value) => theme.WithTextSize(value));
		Choose<MokaCorners>(target, nameof(MokaTheme.WithCorners), (theme, value) => theme.WithCorners(value));
		Choose<MokaLetterCase>(target, nameof(MokaTheme.WithLetterCase), (theme, value) => theme.WithLetterCase(value));
		Choose<MokaTitleFont>(target, nameof(MokaTheme.WithTitleFont), (theme, value) => theme.WithTitleFont(value));
		Choose<MokaTitleSize>(target, nameof(MokaTheme.WithTitleSize), (theme, value) => theme.WithTitleSize(value));
		Choose<MokaNumberFont>(target, nameof(MokaTheme.WithNumberFont), (theme, value) => theme.WithNumberFont(value));

		foreach (ThemeChoice choice in ThemeChoice.All)
		{
			choice.ChooseIn(this, target);
		}

		ChooseFlag(target, nameof(MokaTheme.WithDescriptions), theme => theme.Descriptions, (theme, value) => theme.WithDescriptions(value));
		ChooseFlag(target, nameof(MokaTheme.WithTooltipArrows), theme => theme.TooltipArrows, (theme, value) => theme.WithTooltipArrows(value));

		if (!target.FontScale.Equals(_current.FontScale))
		{
			double scale = target.FontScale;
			Step($".{nameof(MokaTheme.WithFontScale)}({Number(scale)})", theme => theme.WithFontScale(scale));
		}

		if (target.HighContrast != _current.HighContrast)
		{
			bool high = target.HighContrast;
			Step(high ? $".{nameof(MokaTheme.WithHighContrast)}()" : $".{nameof(MokaTheme.WithHighContrast)}(false)", theme => theme.WithHighContrast(high));
		}

		foreach (string name in _current.Pins.Keys.Where(name => !target.Pins.ContainsKey(name)).Order(StringComparer.Ordinal).ToList())
		{
			Step($".{nameof(MokaTheme.WithoutToken)}({MokaThemeSerializer.CSharpString(name)})", theme => theme.WithoutToken(name));
		}

		foreach ((string name, string value) in target.Pins.OrderBy(pin => pin.Key, StringComparer.Ordinal))
		{
			if (!_current.Pins.TryGetValue(name, out string? held) || !string.Equals(held, value, StringComparison.Ordinal))
			{
				Step($".{nameof(MokaTheme.WithToken)}({MokaThemeSerializer.CSharpString(name)}, {MokaThemeSerializer.CSharpString(value)})",
					theme => theme.WithToken(name, value));
			}
		}

		return _current.Equals(target);
	}

	private void Step(string code, Func<MokaTheme, MokaTheme> apply)
	{
		_current = apply(_current);
		_calls.Add(code);
	}

	// A With call sets its part of the theme whatever was there, so the target holds a choice exactly when making that
	// choice again leaves it unchanged. The original design ignores the calls, and the recipe then has nothing to say.
	internal void Choose<TChoice>(MokaTheme target, string method, Func<MokaTheme, TChoice, MokaTheme> apply)
		where TChoice : struct, Enum
	{
		foreach (TChoice choice in Enum.GetValues<TChoice>())
		{
			if (!apply(target, choice).Equals(target))
			{
				continue;
			}

			if (!apply(_current, choice).Equals(_current))
			{
				Step($".{method}({ThemeChoice.Code(choice)})", theme => apply(theme, choice));
			}

			return;
		}
	}

	private void ChooseFlag(MokaTheme target, string method, Func<MokaTheme, bool> read, Func<MokaTheme, bool, MokaTheme> apply)
	{
		bool wanted = read(target);
		if (read(_current) != wanted)
		{
			Step($".{method}({Bool(wanted)})", theme => apply(theme, wanted));
		}
	}

	private void TakeDensity(MokaTheme target)
	{
		foreach (MokaDensity density in Enum.GetValues<MokaDensity>())
		{
			if (target.Look == MokaLook.Gate && target.WithDensity(density).Equals(target))
			{
				if (!_current.WithDensity(density).Equals(_current))
				{
					Step($".{nameof(MokaTheme.WithDensity)}({nameof(MokaDensity)}.{density})", theme => theme.WithDensity(density));
				}

				return;
			}
		}

		if (!target.Density.Equals(_current.Density))
		{
			double density = target.Density;
			Step($".{nameof(MokaTheme.WithDensity)}({Number(density)})", theme => theme.WithDensity(density));
		}
	}

	private bool TakeAccent(MokaTheme target)
	{
		if (target.Accent is not { } accent)
		{
			return false;
		}

		if (MokaAccent.Presets.FirstOrDefault(preset => preset.Equals(accent)) is { } preset)
		{
			Step($".{nameof(MokaTheme.WithAccent)}({nameof(MokaAccent)}.{preset.Name})", theme => theme.WithAccent(preset));
			return true;
		}

		if (target.Look == MokaLook.Matrix && accent.Dark.Equals(accent.Light) && MokaCssColor.TryParseHex(accent.Dark.Color, out _))
		{
			string color = accent.Dark.Color;
			Step($".{nameof(MokaTheme.WithAccent)}({MokaThemeSerializer.CSharpString(color)})", theme => theme.WithAccent(color));
			return true;
		}

		Step($".{nameof(MokaTheme.WithAccent)}(new {nameof(MokaAccent)}({MokaThemeSerializer.CSharpString(accent.Id)}, {MokaThemeSerializer.CSharpString(accent.Name)}, " +
			$"{Shades(accent.Dark)}, {Shades(accent.Light)}))", theme => theme.WithAccent(accent));
		return true;
	}

	// What FromSeed refuses: a color it can't read, or an accent that isn't solid. Such a seed only comes from a theme built
	// by hand, which the initializer writes as it is.
	private static bool CanDerive(MokaThemeSeed seed) =>
		MokaCssColor.TryParse(seed.Accent, out MokaSrgb accent) && accent.IsOpaque &&
		new[] { seed.Neutral, seed.Success, seed.Warning, seed.Error, seed.Info }.All(color => color is null || MokaCssColor.TryParse(color, out _));

	private static string SeedCode(MokaThemeSeed seed)
	{
		List<string> fields = [$"{nameof(MokaThemeSeed.Accent)} = {MokaThemeSerializer.CSharpString(seed.Accent)}"];
		AddColor(fields, nameof(MokaThemeSeed.Neutral), seed.Neutral);
		if (seed.NeutralChroma is { } chroma)
		{
			fields.Add($"{nameof(MokaThemeSeed.NeutralChroma)} = {Number(chroma)}");
		}

		AddColor(fields, nameof(MokaThemeSeed.Success), seed.Success);
		AddColor(fields, nameof(MokaThemeSeed.Warning), seed.Warning);
		AddColor(fields, nameof(MokaThemeSeed.Error), seed.Error);
		AddColor(fields, nameof(MokaThemeSeed.Info), seed.Info);
		if (seed.Contrast != MokaSeedContrast.Standard)
		{
			fields.Add($"{nameof(MokaThemeSeed.Contrast)} = {nameof(MokaSeedContrast)}.{seed.Contrast}");
		}

		StringBuilder code = new();
		code.Append("new ").AppendLine(nameof(MokaThemeSeed)).AppendLine("{");
		for (int i = 0; i < fields.Count; i++)
		{
			code.Append('\t').Append(fields[i]).AppendLine(i < fields.Count - 1 ? "," : "");
		}

		return code.Append('}').ToString();
	}

	private static void AddColor(List<string> fields, string name, string? color)
	{
		if (color is not null)
		{
			fields.Add($"{name} = {MokaThemeSerializer.CSharpString(color)}");
		}
	}

	private static string Shades(MokaAccentShades shades) =>
		$"new {nameof(MokaAccentShades)}({MokaThemeSerializer.CSharpString(shades.Color)}, {MokaThemeSerializer.CSharpString(shades.Soft)}, " +
		$"{MokaThemeSerializer.CSharpString(shades.Background)})";

	private static string Bool(bool value) => value ? "true" : "false";

	private static string Number(double value) => MokaThemeSerializer.CSharpDouble(value);
}
