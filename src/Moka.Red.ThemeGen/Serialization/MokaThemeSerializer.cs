using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Moka.Red.Core.Colors;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.ThemeGen.Serialization;

/// <summary>
///     Serializes and deserializes <see cref="MokaTheme" /> to/from JSON, CSS, and C# code.
///     JSON and C# carry every theme, palette, typography and spacing property, so both read back
///     to an equal theme.
/// </summary>
public static class MokaThemeSerializer
{
	private const string IsDarkKey = "isDark";
	private const string LookKey = "look";
	private const string HighContrastKey = "highContrast";
	private const string SurfacesKey = "surfaces";
	private const string TooltipArrowsKey = "tooltipArrows";
	private const string DescriptionsKey = "descriptions";
	private const string DensityKey = "density";
	private const string FontScaleKey = "fontScale";
	private const string PaletteKey = "palette";
	private const string TypographyKey = "typography";
	private const string SpacingKey = "spacing";
	private const string SeedKey = "seed";
	private const string AccentKey = "accent";
	private const string PaletteBeforeHighContrastKey = "paletteBeforeHighContrast";
	private const string PinsKey = "pins";

	// The export box shows this text to people, so the quotes in font names stay readable instead of
	// turning into escape sequences. It is never written into HTML or a script unencoded.
	private static readonly JsonWriterOptions WriterOptions = new()
	{
		Indented = true,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	private static readonly JsonDocumentOptions ReaderOptions = new()
	{
		AllowTrailingCommas = true,
		CommentHandling = JsonCommentHandling.Skip
	};

	// Besides numbers with a unit, a length field takes the CSS functions that compute one, and var().
	private static readonly string[] LengthFunctions = ["calc(", "min(", "max(", "clamp(", "var("];

	// One row per record property, from Core's token table. Both formats are written from these tables.
	private static readonly Field<MokaPalette>[] PaletteFields = Fields(MokaTokens.PaletteProperties);

	private static readonly Field<MokaTypography>[] TypographyFields = Fields(MokaTokens.TypographyProperties);

	private static readonly Field<MokaSpacing>[] SpacingFields = Fields(MokaTokens.SpacingProperties);

	/// <summary>Serialize a <see cref="MokaTheme" /> to indented JSON with camelCase names.</summary>
	public static string ToJson(MokaTheme theme)
	{
		ArgumentNullException.ThrowIfNull(theme);

		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream, WriterOptions))
		{
			writer.WriteStartObject();
			writer.WriteBoolean(IsDarkKey, theme.IsDark);
			writer.WriteString(LookKey, theme.Look.ToString());
			writer.WriteBoolean(HighContrastKey, theme.HighContrast);
			writer.WriteString(SurfacesKey, theme.Surfaces.ToString());
			foreach (ThemeChoice choice in ThemeChoice.All)
			{
				if (choice.NameIn(theme) is { } name)
				{
					writer.WriteString(choice.Key, name);
				}
			}

			writer.WriteBoolean(TooltipArrowsKey, theme.TooltipArrows);
			writer.WriteBoolean(DescriptionsKey, theme.Descriptions);
			WriteNumber(writer, DensityKey, theme.Density);
			WriteNumber(writer, FontScaleKey, theme.FontScale);
			WriteObject(writer, PaletteKey, theme.Palette, PaletteFields);
			WriteObject(writer, TypographyKey, theme.Typography, TypographyFields);
			WriteObject(writer, SpacingKey, theme.Spacing, SpacingFields);
			WriteSeed(writer, theme.Seed);
			WritePins(writer, theme.Pins);
			WriteAccent(writer, theme.Accent);
			if (theme.PaletteBeforeHighContrast is { } before)
			{
				WriteObject(writer, PaletteBeforeHighContrastKey, before, PaletteFields);
			}
			writer.WriteEndObject();
		}

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	/// <summary>
	///     Deserialize a <see cref="MokaTheme" /> from JSON. Returns <c>null</c> when the text is not a theme;
	///     <see cref="TryFromJson" /> also says why.
	/// </summary>
	public static MokaTheme? FromJson(string? json) => TryFromJson(json, out MokaTheme? theme, out _) ? theme : null;

	/// <summary>
	///     Reads a theme from JSON written by <see cref="ToJson" />. Names match case-insensitively. Missing
	///     values come from the built-in theme of the same <c>look</c> and <c>isDark</c>. Keys it does not know
	///     are skipped. Every value is checked before the theme is built: palette values must be CSS colors
	///     (<see cref="CssValues.IsColor" />), font sizes and spacing CSS lengths (a number with a unit, or a
	///     <c>calc()</c>, <c>min()</c>, <c>max()</c>, <c>clamp()</c> or <c>var()</c> expression; paddings may hold
	///     several), and the rest must pass <see cref="CssValues.IsSafe" /> and
	///     <see cref="CssValues.IsSelfContained" />. Never throws.
	/// </summary>
	/// <param name="json">The JSON text.</param>
	/// <param name="theme">The theme, when the text could be read.</param>
	/// <param name="error">A message for the user, when it could not.</param>
	/// <returns><c>true</c> when <paramref name="theme" /> was read.</returns>
	public static bool TryFromJson(
		string? json,
		[NotNullWhen(true)] out MokaTheme? theme,
		[NotNullWhen(false)] out string? error)
	{
		theme = null;

		if (string.IsNullOrWhiteSpace(json))
		{
			error = "The theme JSON is empty.";
			return false;
		}

		JsonDocument document;
		try
		{
			document = JsonDocument.Parse(json, ReaderOptions);
		}
		catch (JsonException ex)
		{
			error = DescribeSyntaxError(ex);
			return false;
		}
		catch (ArgumentException)
		{
			// Thrown for text that is not valid UTF-16, such as a lone surrogate.
			error = "The theme JSON contains characters that are not valid text.";
			return false;
		}

		using (document)
		{
			error = ReadTheme(document.RootElement, out theme);
			return error is null;
		}
	}

	/// <summary>
	///     The theme as CSS, for a page that loads <c>moka.css</c> without a <see cref="MokaThemeProvider" />, or to keep the
	///     theme in a stylesheet: a comment naming the classes the outermost provider puts on its root, then a <c>:root</c>
	///     rule with <c>color-scheme</c>, the page background on the new design (as the provider writes it), and every token
	///     <see cref="MokaTheme.ToCssVariables" /> writes, pins included. Put the classes on the element that holds the app,
	///     since the components' rules read them.
	/// </summary>
	/// <param name="theme">The theme.</param>
	/// <returns>The stylesheet.</returns>
	public static string ToCss(MokaTheme theme)
	{
		ArgumentNullException.ThrowIfNull(theme);
		string cssVars = theme.ToCssVariables();
		var sb = new StringBuilder();
		sb.Append("/* Root classes: ").Append(string.Join(' ', ThemeRootClasses.For(theme))).AppendLine(" */");
		sb.AppendLine(":root {");
		sb.Append("\tcolor-scheme: ").Append(theme.IsDark ? "dark" : "light").AppendLine(";");
		if (theme.Look == MokaLook.Gate)
		{
			sb.AppendLine("\tbackground: var(--moka-color-background);");
		}

		string[] pairs = cssVars.Split("; ", StringSplitOptions.RemoveEmptyEntries);
		foreach (string pair in pairs)
		{
			string trimmed = pair.TrimEnd(';').Trim();
			if (!string.IsNullOrEmpty(trimmed))
			{
				sb.Append('\t');
				sb.Append(trimmed);
				sb.AppendLine(";");
			}
		}

		sb.Append('}');
		return sb.ToString();
	}

	/// <summary>
	///     The theme as C# that builds it back, written with the <c>With</c> API where it can be: a preset such as
	///     <c>MokaTheme.Red</c>, or <c>MokaTheme.FromSeed(new MokaThemeSeed { ... }, dark: true)</c> for a theme from a seed,
	///     followed by the calls that make the rest of it, such as <c>.WithDensity(MokaDensity.Compact)</c> and
	///     <c>.WithToken(...)</c>. A theme no such chain reaches, such as one with palette colors of its own, is written as a
	///     <c>new MokaTheme { ... }</c> initializer holding every value, with its pins as <c>.WithToken</c> calls after it.
	///     Either way the code builds an equal theme. Strings are written as escaped C# literals.
	/// </summary>
	/// <param name="theme">The theme.</param>
	/// <returns>A C# expression of type <see cref="MokaTheme" />.</returns>
	public static string ToCSharp(MokaTheme theme)
	{
		ArgumentNullException.ThrowIfNull(theme);
		return ThemeRecipe.Find(theme)?.ToCode() ?? ToCSharpInitializer(theme);
	}

	private static string ToCSharpInitializer(MokaTheme theme)
	{
		var sb = new StringBuilder(4096);
		sb.AppendLine("new MokaTheme");
		sb.AppendLine("{");
		AppendAssignment(sb, "\t", nameof(MokaTheme.IsDark), theme.IsDark ? "true" : "false");
		AppendAssignment(sb, "\t", nameof(MokaTheme.Look), $"{nameof(MokaLook)}.{theme.Look}");
		AppendAssignment(sb, "\t", nameof(MokaTheme.HighContrast), theme.HighContrast ? "true" : "false");
		AppendAssignment(sb, "\t", nameof(MokaTheme.Surfaces), $"{nameof(MokaSurfaces)}.{theme.Surfaces}");
		foreach (ThemeChoice choice in ThemeChoice.All)
		{
			if (choice.CodeIn(theme) is { } code)
			{
				AppendAssignment(sb, "\t", choice.Property, code);
			}
		}

		AppendAssignment(sb, "\t", nameof(MokaTheme.TooltipArrows), theme.TooltipArrows ? "true" : "false");
		AppendAssignment(sb, "\t", nameof(MokaTheme.Descriptions), theme.Descriptions ? "true" : "false");
		AppendAssignment(sb, "\t", nameof(MokaTheme.Density), CSharpDouble(theme.Density));
		AppendAssignment(sb, "\t", nameof(MokaTheme.FontScale), CSharpDouble(theme.FontScale));
		AppendObject(sb, nameof(MokaTheme.Palette), nameof(MokaPalette), theme.Palette, PaletteFields);
		AppendObject(sb, nameof(MokaTheme.Typography), nameof(MokaTypography), theme.Typography, TypographyFields);
		AppendObject(sb, nameof(MokaTheme.Spacing), nameof(MokaSpacing), theme.Spacing, SpacingFields);
		AppendSeed(sb, theme.Seed);
		if (theme.Accent is { } accent)
		{
			AppendAssignment(sb, "\t", nameof(MokaTheme.Accent),
				$"new {nameof(MokaAccent)}({CSharpString(accent.Id)}, {CSharpString(accent.Name)}, {CSharpShades(accent.Dark)}, {CSharpShades(accent.Light)})");
		}

		if (theme.PaletteBeforeHighContrast is { } before)
		{
			AppendObject(sb, nameof(MokaTheme.PaletteBeforeHighContrast), nameof(MokaPalette), before, PaletteFields);
		}

		sb.Append('}');

		// Pins have no setter: WithToken checks each value, so they follow the initializer as calls.
		foreach ((string name, string value) in theme.Pins.OrderBy(pin => pin.Key, StringComparer.Ordinal))
		{
			sb.AppendLine().Append('.').Append(nameof(MokaTheme.WithToken)).Append('(').Append(CSharpString(name)).Append(", ")
				.Append(CSharpString(value)).Append(')');
		}

		return sb.ToString();
	}

	// ── JSON writing ──────────────────────────────────────────────

	private static void WriteSeed(Utf8JsonWriter writer, MokaThemeSeed? seed)
	{
		if (seed is null)
		{
			return;
		}

		writer.WriteStartObject(SeedKey);
		foreach (SeedColor color in SeedColors)
		{
			if (color.Get(seed) is { } value)
			{
				writer.WriteString(color.JsonName, value);
			}
		}

		if (seed.NeutralChroma is { } chroma)
		{
			WriteNumber(writer, NeutralChromaKey, chroma);
		}

		writer.WriteString(ContrastKey, seed.Contrast.ToString());
		writer.WriteEndObject();
	}

	private static void WriteAccent(Utf8JsonWriter writer, MokaAccent? accent)
	{
		if (accent is null)
		{
			return;
		}

		writer.WriteStartObject(AccentKey);
		writer.WriteString(AccentIdKey, accent.Id);
		writer.WriteString(AccentNameKey, accent.Name);
		WriteShades(writer, AccentDarkKey, accent.Dark);
		WriteShades(writer, AccentLightKey, accent.Light);
		writer.WriteEndObject();
	}

	private static void WriteShades(Utf8JsonWriter writer, string name, MokaAccentShades shades)
	{
		writer.WriteStartObject(name);
		writer.WriteString(ShadeColorKey, shades.Color);
		writer.WriteString(ShadeSoftKey, shades.Soft);
		writer.WriteString(ShadeBackgroundKey, shades.Background);
		writer.WriteEndObject();
	}

	private static void WritePins(Utf8JsonWriter writer, IReadOnlyDictionary<string, string> pins)
	{
		if (pins.Count == 0)
		{
			return;
		}

		writer.WriteStartObject(PinsKey);
		foreach ((string name, string value) in pins.OrderBy(pin => pin.Key, StringComparer.Ordinal))
		{
			writer.WriteString(name, value);
		}

		writer.WriteEndObject();
	}

	private static void WriteNumber(Utf8JsonWriter writer, string name, double value)
	{
		// JSON has no NaN or Infinity. They go out as the string names System.Text.Json uses for
		// them, which ReadNumber accepts, so even a broken density round-trips instead of throwing.
		if (double.IsFinite(value))
		{
			writer.WriteNumber(name, value);
		}
		else
		{
			writer.WriteString(name, value.ToString(CultureInfo.InvariantCulture));
		}
	}

	private static void WriteObject<T>(Utf8JsonWriter writer, string name, T value, Field<T>[] fields)
	{
		writer.WriteStartObject(name);
		foreach (Field<T> field in fields)
		{
			writer.WriteString(field.JsonName, field.Get(value));
		}

		writer.WriteEndObject();
	}

	// ── JSON reading ──────────────────────────────────────────────

	private static string? ReadTheme(JsonElement root, out MokaTheme? theme)
	{
		theme = null;

		if (root.ValueKind != JsonValueKind.Object)
		{
			return "The theme JSON must be an object, like the one Export writes.";
		}

		JsonElement? isDark = null, look = null, highContrast = null, density = null, fontScale = null;
		JsonElement? surfaces = null, descriptions = null, tooltipArrows = null;
		JsonElement? palette = null, typography = null, spacing = null, seed = null, pins = null, accent = null, paletteBefore = null;
		Dictionary<ThemeChoice, JsonElement> choices = [];
		foreach (JsonProperty property in root.EnumerateObject())
		{
			string name = property.Name;
			if (ThemeChoice.Find(name) is { } choice)
			{
				choices[choice] = property.Value;
			}
			else if (IsKey(name, IsDarkKey))
			{
				isDark = property.Value;
			}
			else if (IsKey(name, LookKey))
			{
				look = property.Value;
			}
			else if (IsKey(name, HighContrastKey))
			{
				highContrast = property.Value;
			}
			else if (IsKey(name, SurfacesKey))
			{
				surfaces = property.Value;
			}
			else if (IsKey(name, DescriptionsKey))
			{
				descriptions = property.Value;
			}
			else if (IsKey(name, TooltipArrowsKey))
			{
				tooltipArrows = property.Value;
			}
			else if (IsKey(name, DensityKey))
			{
				density = property.Value;
			}
			else if (IsKey(name, FontScaleKey))
			{
				fontScale = property.Value;
			}
			else if (IsKey(name, PaletteKey))
			{
				palette = property.Value;
			}
			else if (IsKey(name, TypographyKey))
			{
				typography = property.Value;
			}
			else if (IsKey(name, SpacingKey))
			{
				spacing = property.Value;
			}
			else if (IsKey(name, SeedKey))
			{
				seed = property.Value;
			}
			else if (IsKey(name, PinsKey))
			{
				pins = property.Value;
			}
			else if (IsKey(name, AccentKey))
			{
				accent = property.Value;
			}
			else if (IsKey(name, PaletteBeforeHighContrastKey))
			{
				paletteBefore = property.Value;
			}

			// Any other key is skipped, so a theme exported by a later version, with tokens this one
			// does not have yet, still imports.
		}

		bool dark = false;
		if (isDark is { } isDarkValue)
		{
			if (isDarkValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
			{
				return $"'{IsDarkKey}' must be true or false.";
			}

			dark = isDarkValue.GetBoolean();
		}

		bool high = false;
		if (highContrast is { } highContrastValue)
		{
			if (highContrastValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
			{
				return $"'{HighContrastKey}' must be true or false.";
			}

			high = highContrastValue.GetBoolean();
		}

		bool showDescriptions = true;
		if (descriptions is { } descriptionsValue)
		{
			if (descriptionsValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
			{
				return $"'{DescriptionsKey}' must be true or false.";
			}

			showDescriptions = descriptionsValue.GetBoolean();
		}

		bool arrows = false;
		if (tooltipArrows is { } tooltipArrowsValue)
		{
			if (tooltipArrowsValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
			{
				return $"'{TooltipArrowsKey}' must be true or false.";
			}

			arrows = tooltipArrowsValue.GetBoolean();
		}

		string? choiceError = null;
		MokaSurfaces themeSurfaces = ReadChoice(surfaces, SurfacesKey, MokaSurfaces.BlueGrey, ref choiceError);

		// In the table's order, so the first choice it can't read is the one named whatever order the keys came in. A
		// choice the JSON leaves out keeps the theme's default.
		List<Func<MokaTheme, MokaTheme>> chosen = [];
		foreach (ThemeChoice choice in ThemeChoice.All)
		{
			if (choices.TryGetValue(choice, out JsonElement value))
			{
				chosen.Add(choice.Read(value, ref choiceError));
			}
		}

		if (choiceError is not null)
		{
			return choiceError;
		}

		MokaLook themeLook = MokaLook.Matrix;
		if (look is { } lookValue)
		{
			// By name only: Enum.TryParse also takes numbers and comma lists, which no export writes.
			string lookName = lookValue.ValueKind == JsonValueKind.String ? lookValue.GetString() ?? "" : "";
			string? known = Array.Find(Enum.GetNames<MokaLook>(), name => IsKey(name, lookName));
			if (known is null)
			{
				return $"'{LookKey}' must be one of {string.Join(", ", Enum.GetNames<MokaLook>())}.";
			}

			themeLook = Enum.Parse<MokaLook>(known);
		}

		// Missing values come from the built-in theme that matches the look and isDark, so a light theme
		// written before a palette color existed does not pick up the dark MokaPalette initializer defaults.
		MokaTheme baseTheme = themeLook != MokaLook.Gate
			? dark ? MokaTheme.Dark : MokaTheme.Light
			: themeSurfaces switch
			{
				MokaSurfaces.Graphite => dark ? MokaTheme.Red : MokaTheme.RedLight,
				MokaSurfaces.NearBlack => dark ? MokaTheme.Matrix : MokaTheme.MatrixLight,
				_ => dark ? MokaTheme.Gate : MokaTheme.GateLight
			};

		string? error = null;
		theme = new MokaTheme
		{
			IsDark = dark,
			Look = themeLook,
			HighContrast = high,
			Surfaces = themeSurfaces,
			TooltipArrows = arrows,
			Descriptions = showDescriptions,
			Density = ReadNumber(density, DensityKey, baseTheme.Density, ref error),
			FontScale = ReadNumber(fontScale, FontScaleKey, baseTheme.FontScale, ref error),
			Palette = ReadObject(palette, PaletteKey, baseTheme.Palette, PaletteFields,
				field => field == nameof(MokaPalette.KnobShadow) ? ValueKind.Css : ValueKind.Color,
				ref error),
			Typography = ReadObject(typography, TypographyKey, baseTheme.Typography, TypographyFields,
				field => field.StartsWith("FontSize", StringComparison.Ordinal) ? ValueKind.Length : ValueKind.Css,
				ref error),
			Spacing = ReadObject(spacing, SpacingKey, baseTheme.Spacing, SpacingFields,
				field => field.EndsWith("Padding", StringComparison.Ordinal) ? ValueKind.Lengths : ValueKind.Length,
				ref error),
			Seed = ReadSeed(seed, ref error),
			Accent = ReadAccent(accent, ref error),
			PaletteBeforeHighContrast = paletteBefore is { ValueKind: JsonValueKind.Object }
				? ReadObject(paletteBefore, PaletteBeforeHighContrastKey, baseTheme.Palette, PaletteFields,
					field => field == nameof(MokaPalette.KnobShadow) ? ValueKind.Css : ValueKind.Color,
					ref error)
				: null
		};

		foreach (Func<MokaTheme, MokaTheme> choose in chosen)
		{
			theme = choose(theme);
		}

		theme = ReadPins(pins, theme, ref error);
		if (error is not null)
		{
			theme = null;
		}

		return error;
	}

	private static bool IsKey(string name, string key) => string.Equals(name, key, StringComparison.OrdinalIgnoreCase);

	private static MokaThemeSeed? ReadSeed(JsonElement? element, ref string? error)
	{
		if (error is not null || element is not { ValueKind: not JsonValueKind.Null } json)
		{
			return null;
		}

		if (json.ValueKind != JsonValueKind.Object)
		{
			error = $"'{SeedKey}' must be an object.";
			return null;
		}

		Dictionary<string, string> colors = new(StringComparer.Ordinal);
		double? chroma = null;
		MokaSeedContrast contrast = MokaSeedContrast.Standard;
		foreach (JsonProperty property in json.EnumerateObject())
		{
			if (Array.Find(SeedColors, color => IsKey(property.Name, color.JsonName)) is { } color)
			{
				string? value = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
				if (!CssValues.IsColor(value))
				{
					error = $"'{SeedKey}.{color.JsonName}' must be a CSS color.";
					return null;
				}

				colors[color.Name] = value;
			}
			else if (IsKey(property.Name, NeutralChromaKey))
			{
				chroma = ReadNumber(property.Value, $"{SeedKey}.{NeutralChromaKey}", 0, ref error);
			}
			else if (IsKey(property.Name, ContrastKey))
			{
				contrast = ReadChoice<MokaSeedContrast>(property.Value, $"{SeedKey}.{ContrastKey}", MokaSeedContrast.Standard, ref error);
			}
		}

		if (error is not null)
		{
			return null;
		}

		if (!colors.TryGetValue(nameof(MokaThemeSeed.Accent), out string? accent))
		{
			error = $"'{SeedKey}' needs an accent.";
			return null;
		}

		return new MokaThemeSeed
		{
			Accent = accent,
			Neutral = colors.GetValueOrDefault(nameof(MokaThemeSeed.Neutral)),
			NeutralChroma = chroma,
			Success = colors.GetValueOrDefault(nameof(MokaThemeSeed.Success)),
			Warning = colors.GetValueOrDefault(nameof(MokaThemeSeed.Warning)),
			Error = colors.GetValueOrDefault(nameof(MokaThemeSeed.Error)),
			Info = colors.GetValueOrDefault(nameof(MokaThemeSeed.Info)),
			Contrast = contrast
		};
	}

	private static MokaAccent? ReadAccent(JsonElement? element, ref string? error)
	{
		if (error is not null || element is not { ValueKind: not JsonValueKind.Null } json)
		{
			return null;
		}

		if (json.ValueKind != JsonValueKind.Object)
		{
			error = $"'{AccentKey}' must be an object.";
			return null;
		}

		string? id = null, name = null;
		MokaAccentShades? dark = null, light = null;
		foreach (JsonProperty property in json.EnumerateObject())
		{
			if (IsKey(property.Name, AccentIdKey))
			{
				id = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
			}
			else if (IsKey(property.Name, AccentNameKey))
			{
				name = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
			}
			else if (IsKey(property.Name, AccentDarkKey))
			{
				dark = ReadShades(property.Value);
			}
			else if (IsKey(property.Name, AccentLightKey))
			{
				light = ReadShades(property.Value);
			}
		}

		if (id is null || name is null || dark is null || light is null)
		{
			error = $"'{AccentKey}' needs an id, a name, and dark and light shades as hex colors.";
			return null;
		}

		return new MokaAccent(id, name, dark, light);
	}

	// The theme works an accent's shades out as hex, so only hex is read.
	private static MokaAccentShades? ReadShades(JsonElement json)
	{
		if (json.ValueKind != JsonValueKind.Object)
		{
			return null;
		}

		string? Hex(string key) => json.EnumerateObject()
			.Where(property => IsKey(property.Name, key) && property.Value.ValueKind == JsonValueKind.String)
			.Select(property => property.Value.GetString())
			.FirstOrDefault(value => MokaCssColor.TryParseHex(value, out _));

		return Hex(ShadeColorKey) is { } color && Hex(ShadeSoftKey) is { } soft && Hex(ShadeBackgroundKey) is { } background
			? new MokaAccentShades(color, soft, background)
			: null;
	}

	// Each pin is checked the way WithToken checks it, so a bad one comes back as a message rather than an exception.
	private static MokaTheme ReadPins(JsonElement? element, MokaTheme theme, ref string? error)
	{
		if (error is not null || element is not { ValueKind: not JsonValueKind.Null } json)
		{
			return theme;
		}

		if (json.ValueKind != JsonValueKind.Object)
		{
			error = $"'{PinsKey}' must be an object of token names and values.";
			return theme;
		}

		foreach (JsonProperty property in json.EnumerateObject())
		{
			string? value = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString()?.Trim() : null;
			if (!MokaTokens.TryFind(property.Name, out MokaToken? token) || !token.Accepts(value))
			{
				error = $"'{PinsKey}.{property.Name}' is not a theme token with a value it takes.";
				return theme;
			}

			theme = theme.WithToken(property.Name, value);
		}

		return theme;
	}

	// The Read helpers return the fallback and record the first problem in error, so the caller can
	// read every part in one expression and check once.
	private static double ReadNumber(JsonElement? element, string name, double fallback, ref string? error)
	{
		if (error is not null || element is not { } json)
		{
			return fallback;
		}

		if (json.ValueKind == JsonValueKind.Number && json.TryGetDouble(out double number) && double.IsFinite(number))
		{
			return number;
		}

		if (json.ValueKind == JsonValueKind.String && TryReadNamedDouble(json.GetString(), out double named))
		{
			return named;
		}

		error = $"'{name}' must be a number.";
		return fallback;
	}

	private static bool TryReadNamedDouble(string? text, out double value)
	{
		switch (text)
		{
			case "NaN":
				value = double.NaN;
				return true;
			case "Infinity":
				value = double.PositiveInfinity;
				return true;
			case "-Infinity":
				value = double.NegativeInfinity;
				return true;
			default:
				value = 0;
				return false;
		}
	}

	private static T ReadObject<T>(JsonElement? element, string name, T fallback, Field<T>[] fields,
		Func<string, ValueKind> kindOf, ref string? error)
	{
		if (error is not null || element is not { } json)
		{
			return fallback;
		}

		if (json.ValueKind != JsonValueKind.Object)
		{
			error = $"'{name}' must be an object.";
			return fallback;
		}

		T value = fallback;
		foreach (JsonProperty property in json.EnumerateObject())
		{
			Field<T>? field = Array.Find(fields, f => IsKey(property.Name, f.JsonName));
			if (field is null)
			{
				// A token added in a later version: skipped, like an unknown top-level key.
				continue;
			}

			string? text = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
			if (string.IsNullOrWhiteSpace(text))
			{
				error = $"'{name}.{field.JsonName}' must be a non-empty string.";
				return fallback;
			}

			// The theme writes its values into a <style> element, where anything else could end the rule
			// or swallow the tokens after it, and a value of the wrong kind does nothing.
			string? problem = Check(kindOf(field.Name), text);
			if (problem is not null)
			{
				error = $"'{name}.{field.JsonName}' {problem}";
				return fallback;
			}

			value = field.Set(value, text);
		}

		return value;
	}

	// By name only: Enum.TryParse also takes numbers and comma lists, which no export writes.
	internal static TChoice ReadChoice<TChoice>(JsonElement? element, string key, TChoice fallback, ref string? error)
		where TChoice : struct, Enum
	{
		if (element is not { } value)
		{
			return fallback;
		}

		string name = value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
		string? known = Array.Find(Enum.GetNames<TChoice>(), candidate => IsKey(candidate, name));
		if (known is null)
		{
			error ??= $"{key} must be one of {string.Join(", ", Enum.GetNames<TChoice>())}.";
			return fallback;
		}

		return Enum.Parse<TChoice>(known);
	}

	private static string? Check(ValueKind kind, string value) => kind switch
	{
		ValueKind.Color when !CssValues.IsColor(value) => "must be a CSS color, like #ef5350 or rgb(239 83 80).",
		ValueKind.Length when !IsLength(value) =>
			"must be a CSS length, like 8px or 0.5rem, or a calc(), min(), max(), clamp() or var() expression.",
		ValueKind.Lengths when !IsLength(value) && !CssValues.IsLengthList(value) =>
			"must be CSS lengths, like 12px 14px, or a calc(), min(), max(), clamp() or var() expression.",
		ValueKind.Css when !IsCssValue(value) =>
			"cannot contain ; { } < > or \\, or leave a quote, bracket or comment open.",
		_ => null
	};

	private static bool IsLength(string value)
	{
		if (CssValues.IsLength(value))
		{
			return true;
		}

		ReadOnlySpan<char> trimmed = value.AsSpan().Trim();
		foreach (string function in LengthFunctions)
		{
			if (trimmed.StartsWith(function, StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith(')'))
			{
				return IsCssValue(value);
			}
		}

		return false;
	}

	private static bool IsCssValue(string value) => CssValues.IsSafe(value) && CssValues.IsSelfContained(value);

	private static string DescribeSyntaxError(JsonException ex)
	{
		// The reader's message ends with a 0-based "LineNumber: 0 | BytePositionInLine: 3." part, which
		// reads badly next to editors that count from 1. Keep the reason and give the place 1-based.
		string reason = ex.Message;
		int cut = reason.IndexOf(" LineNumber:", StringComparison.Ordinal);
		if (cut > 0)
		{
			reason = reason[..cut];
		}

		return ex.LineNumber is { } line && ex.BytePositionInLine is { } position
			? string.Create(CultureInfo.InvariantCulture,
				$"Not valid JSON (line {line + 1}, position {position + 1}): {reason}")
			: $"Not valid JSON: {reason}";
	}

	// ── C# writing ────────────────────────────────────────────────

	private static void AppendObject<T>(StringBuilder sb, string propertyName, string typeName, T value, Field<T>[] fields)
	{
		sb.Append('\t').Append(propertyName).Append(" = new ").AppendLine(typeName);
		sb.AppendLine("\t{");
		foreach (Field<T> field in fields)
		{
			AppendAssignment(sb, "\t\t", field.Name, CSharpString(field.Get(value)));
		}

		sb.AppendLine("\t},");
	}

	private static void AppendSeed(StringBuilder sb, MokaThemeSeed? seed)
	{
		if (seed is null)
		{
			return;
		}

		sb.Append('\t').Append(nameof(MokaTheme.Seed)).Append(" = new ").AppendLine(nameof(MokaThemeSeed));
		sb.AppendLine("\t{");
		foreach (SeedColor color in SeedColors)
		{
			if (color.Get(seed) is { } value)
			{
				AppendAssignment(sb, "\t\t", color.Name, CSharpString(value));
			}
		}

		if (seed.NeutralChroma is { } chroma)
		{
			AppendAssignment(sb, "\t\t", nameof(MokaThemeSeed.NeutralChroma), CSharpDouble(chroma));
		}

		AppendAssignment(sb, "\t\t", nameof(MokaThemeSeed.Contrast), $"{nameof(MokaSeedContrast)}.{seed.Contrast}");
		sb.AppendLine("\t},");
	}

	private static string CSharpShades(MokaAccentShades shades) =>
		$"new {nameof(MokaAccentShades)}({CSharpString(shades.Color)}, {CSharpString(shades.Soft)}, {CSharpString(shades.Background)})";

	private static void AppendAssignment(StringBuilder sb, string indent, string name, string literal) =>
		sb.Append(indent).Append(name).Append(" = ").Append(literal).AppendLine(",");

	internal static string CSharpDouble(double value) => value switch
	{
		double.PositiveInfinity => "double.PositiveInfinity",
		double.NegativeInfinity => "double.NegativeInfinity",
		_ when double.IsNaN(value) => "double.NaN",
		_ => value.ToString("R", CultureInfo.InvariantCulture)
	};

	internal static string CSharpString(string value)
	{
		var sb = new StringBuilder(value.Length + 2);
		sb.Append('"');
		foreach (char c in value)
		{
			switch (c)
			{
				case '"':
					sb.Append("\\\"");
					break;
				case '\\':
					sb.Append("\\\\");
					break;
				case '\n':
					sb.Append("\\n");
					break;
				case '\r':
					sb.Append("\\r");
					break;
				case '\t':
					sb.Append("\\t");
					break;
				case '\0':
					sb.Append("\\0");
					break;
				// Other control characters, and the characters C# treats as line breaks, cannot sit
				// in a regular string literal.
				case < ' ' or (char)0x7F or (char)0x85 or (char)0x2028 or (char)0x2029:
					sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
					break;
				default:
					sb.Append(c);
					break;
			}
		}

		sb.Append('"');
		return sb.ToString();
	}

	private enum ValueKind
	{
		Color,
		Length,
		Lengths,
		Css
	}

	private const string NeutralChromaKey = "neutralChroma";
	private const string AccentIdKey = "id";
	private const string AccentNameKey = "name";
	private const string AccentDarkKey = "dark";
	private const string AccentLightKey = "light";
	private const string ShadeColorKey = "color";
	private const string ShadeSoftKey = "soft";
	private const string ShadeBackgroundKey = "background";
	private const string ContrastKey = "contrast";

	private static readonly SeedColor[] SeedColors =
	[
		new(nameof(MokaThemeSeed.Accent), seed => seed.Accent),
		new(nameof(MokaThemeSeed.Neutral), seed => seed.Neutral),
		new(nameof(MokaThemeSeed.Success), seed => seed.Success),
		new(nameof(MokaThemeSeed.Warning), seed => seed.Warning),
		new(nameof(MokaThemeSeed.Error), seed => seed.Error),
		new(nameof(MokaThemeSeed.Info), seed => seed.Info)
	];

	private static Field<T>[] Fields<T>(IReadOnlyList<MokaThemeProperty<T>> properties) =>
		[.. properties.Select(property => new Field<T>(property.Name, property.Get, property.Set))];

	private sealed class SeedColor(string name, Func<MokaThemeSeed, string?> get)
	{
		public string Name { get; } = name;

		public string JsonName { get; } = JsonNamingPolicy.CamelCase.ConvertName(name);

		public Func<MokaThemeSeed, string?> Get { get; } = get;
	}

	private sealed class Field<T>(string name, Func<T, string> get, Func<T, string, T> set)
	{
		public string Name { get; } = name;

		public string JsonName { get; } = JsonNamingPolicy.CamelCase.ConvertName(name);

		public Func<T, string> Get { get; } = get;

		public Func<T, string, T> Set { get; } = set;
	}
}
