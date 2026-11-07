namespace Moka.Red.Core.Theming;

public sealed partial record MokaPalette
{
	private const string GateDarkInk = "#0A0E15";
	private const string GateLightInk = "#121826";

	/// <summary>
	///     The dark palette of the <see cref="MokaLook.Gate" /> look: blue-grey surfaces, solid borders, a blue
	///     accent, and five shades for each of success, warning, error and info.
	/// </summary>
	public static MokaPalette Gate { get; } = new MokaPalette
	{
		Primary = MokaAccent.Blue.Dark.Color,
		PrimaryLight = MokaAccent.Blue.Dark.Soft,
		PrimaryDark = MokaColorMath.MixHex(MokaAccent.Blue.Dark.Color, GateDarkInk, 0.78),
		OnPrimary = GateDarkInk,

		Secondary = "#96A1B6",
		SecondaryLight = "#C3CADA",
		SecondaryDark = "#6B778C",
		OnSecondary = GateDarkInk,

		Surface = "#111723",
		SurfaceVariant = "#161E2D",
		OnSurface = "#E7EBF3",
		OnSurfaceVariant = "#96A1B6",
		Background = GateDarkInk,
		OnBackground = "#E7EBF3",

		Error = "#FF6369",
		OnError = GateDarkInk,
		Warning = "#FFB547",
		OnWarning = GateDarkInk,
		Success = "#3DD68C",
		OnSuccess = GateDarkInk,
		Info = "#5B9CFF",
		OnInfo = GateDarkInk,

		ErrorDim = "rgba(255, 99, 105, 0.14)",
		WarningDim = "rgba(255, 181, 71, 0.14)",
		SuccessDim = "rgba(61, 214, 140, 0.14)",
		InfoDim = "rgba(91, 156, 255, 0.14)",

		Outline = "#222C3E",
		OutlineVariant = "#1B2433",
		OutlineStrong = "#2C3850",
		OutlineHover = "#36435E",
		Backdrop = "rgba(6, 9, 14, 0.78)",

		SurfaceHover = "#1A2234",
		Surface2 = "#0E131D",
		Surface3 = "#1A2234",
		SurfaceRaised = "#141B29",
		NeutralBg = "#1F2840",

		OnSurface2 = "#C3CADA",
		OnSurfaceTertiary = "#6B778C",
		OnSurfaceQuaternary = "#3A4558",
		Icon = "#7C879C",

		Lift = "white",
		Knob = "#E7EBF3",
		KnobShadow = "none",
		Paper = "#FFFFFF",
		PaperEdge = "transparent",

		PrimaryBg = MokaAccent.Blue.Dark.Background,
		PrimaryRow = "color-mix(in srgb, var(--moka-color-primary) 12%, var(--moka-color-background))",
		PrimaryRing = "color-mix(in srgb, var(--moka-color-primary) 35%, var(--moka-color-background))",
		PrimaryPill = "color-mix(in srgb, var(--moka-color-primary) 25%, var(--moka-color-background))",
		PrimaryFocus = "color-mix(in srgb, var(--moka-color-primary) 22%, transparent)",
		PrimaryText = "color-mix(in srgb, var(--moka-color-primary) 40%, white)",

		PrimaryGlow = MokaColorMath.Rgba(MokaAccent.Blue.Dark.Color, 0.08),
		PrimaryGlowMd = MokaColorMath.Rgba(MokaAccent.Blue.Dark.Color, 0.15),
		PrimaryGlowStrong = MokaColorMath.Rgba(MokaAccent.Blue.Dark.Color, 0.25),
		PrimaryGlowFaint = MokaColorMath.Rgba(MokaAccent.Blue.Dark.Color, 0.04),
		PrimaryBorder = MokaColorMath.Rgba(MokaAccent.Blue.Dark.Color, 0.30),
		PrimaryBorderDim = MokaColorMath.Rgba(MokaAccent.Blue.Dark.Color, 0.12),

		SuccessBg = "#13281F",
		SuccessInk = "#9FE6C0",
		SuccessText = "#C8F2DA",
		SuccessLine = "#2F6E50",

		WarningBg = "#2B2413",
		WarningInk = "#F2DDB0",
		WarningText = "#F2DDB0",
		WarningLine = "#5A4A1E",

		ErrorBg = "#3A1D22",
		ErrorInk = "#FFB3B6",
		ErrorText = "#FFD3D5",
		ErrorLine = "#6B2C33",
		ErrorSoft = "#FF8A8F",

		InfoBg = "#172640",
		InfoInk = "#A9C8FF",
		InfoText = "#C9DDFF",
		InfoLine = "#22406B"
	};

	/// <summary>
	///     The light palette of the <see cref="MokaLook.Gate" /> look. Designed rather than inverted: the same
	///     blue-grey on pale surfaces, with the meanings deepened until they read as text.
	/// </summary>
	public static MokaPalette GateLight { get; } = new MokaPalette
	{
		Primary = MokaAccent.Blue.Light.Color,
		PrimaryLight = MokaAccent.Blue.Light.Soft,
		PrimaryDark = MokaColorMath.MixHex(MokaAccent.Blue.Light.Color, GateLightInk, 0.78),
		OnPrimary = "#FFFFFF",

		Secondary = "#525D71",
		SecondaryLight = "#2F3A4C",
		SecondaryDark = "#626D80",
		OnSecondary = "#FFFFFF",

		Surface = "#FFFFFF",
		SurfaceVariant = "#EEF1F6",
		OnSurface = GateLightInk,
		OnSurfaceVariant = "#525D71",
		Background = "#F4F6FA",
		OnBackground = GateLightInk,

		Error = "#C62832",
		OnError = "#FFFFFF",
		Warning = "#9A5B00",
		OnWarning = "#FFFFFF",
		Success = "#0E7A4A",
		OnSuccess = "#FFFFFF",
		Info = "#1F62D0",
		OnInfo = "#FFFFFF",

		ErrorDim = "rgba(198, 40, 50, 0.09)",
		WarningDim = "rgba(154, 91, 0, 0.10)",
		SuccessDim = "rgba(14, 122, 74, 0.10)",
		InfoDim = "rgba(31, 98, 208, 0.10)",

		Outline = "#DCE2EB",
		OutlineVariant = "#E6EAF1",
		OutlineStrong = "#C8D0DC",
		OutlineHover = "#AEB8C8",
		Backdrop = "rgba(22, 28, 42, 0.36)",

		SurfaceHover = "#EBEFF5",
		Surface2 = "#F7F9FC",
		Surface3 = "#E7EBF2",
		SurfaceRaised = "#FAFBFD",
		NeutralBg = "#E4E9F0",

		OnSurface2 = "#2F3A4C",
		OnSurfaceTertiary = "#626D80",
		OnSurfaceQuaternary = "#B6BFCC",
		Icon = "#66718A",

		Lift = "black",
		Knob = "#FFFFFF",
		KnobShadow = "0 1px 3px rgba(22, 30, 48, 0.3)",
		Paper = "#FFFFFF",
		PaperEdge = "var(--moka-color-outline-strong)",

		PrimaryBg = MokaAccent.Blue.Light.Background,
		PrimaryRow = "color-mix(in srgb, var(--moka-color-primary) 9%, var(--moka-color-surface))",
		PrimaryRing = "color-mix(in srgb, var(--moka-color-primary) 38%, var(--moka-color-surface))",
		PrimaryPill = "color-mix(in srgb, var(--moka-color-primary) 15%, var(--moka-color-surface))",
		PrimaryFocus = "color-mix(in srgb, var(--moka-color-primary) 24%, transparent)",
		PrimaryText = "color-mix(in srgb, var(--moka-color-primary-light) 82%, black)",

		PrimaryGlow = MokaColorMath.Rgba(MokaAccent.Blue.Light.Color, 0.10),
		PrimaryGlowMd = MokaColorMath.Rgba(MokaAccent.Blue.Light.Color, 0.16),
		PrimaryGlowStrong = MokaColorMath.Rgba(MokaAccent.Blue.Light.Color, 0.26),
		PrimaryGlowFaint = MokaColorMath.Rgba(MokaAccent.Blue.Light.Color, 0.05),
		PrimaryBorder = MokaColorMath.Rgba(MokaAccent.Blue.Light.Color, 0.36),
		PrimaryBorderDim = MokaColorMath.Rgba(MokaAccent.Blue.Light.Color, 0.16),

		SuccessBg = "#E3F4EA",
		SuccessInk = "#0B6A40",
		SuccessText = "#08532F",
		SuccessLine = "#A7D9BE",

		WarningBg = "#FBF0DB",
		WarningInk = "#855000",
		WarningText = "#6B3F00",
		WarningLine = "#EACB91",

		ErrorBg = "#FCE8E9",
		ErrorInk = "#A51D27",
		ErrorText = "#86161F",
		ErrorLine = "#F0B4B8",
		ErrorSoft = "#B4232C",

		InfoBg = "#E5EEFC",
		InfoInk = "#1A55B5",
		InfoText = "#154391",
		InfoLine = "#B3CBF4"
	};

	/// <summary>
	///     The dark palette of <see cref="MokaTheme.Red" />: the <see cref="MokaLook.Gate" /> meanings on neutral
	///     graphite surfaces, with the red accent.
	/// </summary>
	public static MokaPalette Graphite { get; } = Gate.WithGateAccent(MokaAccent.Red.Dark, true) with
	{
		Secondary = "#A0A0AA",
		SecondaryLight = "#C8C8CF",
		SecondaryDark = "#82828C",

		Surface = "#141417",
		SurfaceVariant = "#1A1A1E",
		OnSurface = "#ECECEF",
		OnSurfaceVariant = "#A0A0AA",
		Background = "#0C0C0E",
		OnBackground = "#ECECEF",

		Outline = "#27272D",
		OutlineVariant = "#1F1F24",
		OutlineStrong = "#33333A",
		OutlineHover = "#44444C",
		Backdrop = "rgba(8, 8, 10, 0.78)",

		SurfaceHover = "#1F1F24",
		Surface2 = "#101013",
		Surface3 = "#1F1F24",
		SurfaceRaised = "#17171B",
		NeutralBg = "#26262C",

		OnSurface2 = "#C8C8CF",
		OnSurfaceTertiary = "#82828C",
		OnSurfaceQuaternary = "#44444C",
		Icon = "#8A8A94",

		Knob = "#ECECEF"
	};

	/// <summary>The light palette of <see cref="MokaTheme.RedLight" />: graphite on pale neutral surfaces.</summary>
	public static MokaPalette GraphiteLight { get; } = GateLight.WithGateAccent(MokaAccent.Red.Light, false) with
	{
		Secondary = "#5A5A64",
		SecondaryLight = "#33333A",
		SecondaryDark = "#666670",

		SurfaceVariant = "#EFEFF1",
		OnSurface = "#16161A",
		OnSurfaceVariant = "#5A5A64",
		Background = "#F5F5F6",
		OnBackground = "#16161A",

		Outline = "#DFDFE3",
		OutlineVariant = "#E8E8EB",
		OutlineStrong = "#CCCCD2",
		OutlineHover = "#B2B2BA",
		Backdrop = "rgba(22, 22, 28, 0.36)",

		SurfaceHover = "#EBEBEE",
		Surface2 = "#F8F8F9",
		Surface3 = "#E9E9EC",
		SurfaceRaised = "#FBFBFB",
		NeutralBg = "#E5E5E8",

		OnSurface2 = "#33333A",
		OnSurfaceTertiary = "#666670",
		OnSurfaceQuaternary = "#BBBBC2",
		Icon = "#6C6C76"
	};

	/// <summary>
	///     The dark palette of <see cref="MokaTheme.Matrix" />: near black with red-tinted borders and the red
	///     accent. The same colors as <see cref="MokaTheme.Dark" />, lifted where small grey text needs it to read.
	/// </summary>
	public static MokaPalette NearBlack { get; } = Gate.WithGateAccent(MokaAccent.Red.Dark, true) with
	{
		Secondary = "#A0A0AA",
		SecondaryLight = "#C8C8CF",
		SecondaryDark = "#84848E",

		Surface = "#0C0C10",
		SurfaceVariant = "#101015",
		OnSurface = "#E8E8EC",
		OnSurfaceVariant = "#A0A0AA",
		Background = "#060608",
		OnBackground = "#E8E8EC",

		Outline = "rgba(239, 83, 80, 0.12)",
		OutlineVariant = "rgba(239, 83, 80, 0.06)",
		OutlineStrong = "rgba(239, 83, 80, 0.24)",
		OutlineHover = "rgba(239, 83, 80, 0.32)",
		Backdrop = "rgba(6, 6, 8, 0.85)",

		SurfaceHover = "#14141A",
		Surface2 = "#0A0A0D",
		Surface3 = "#1A1A22",
		SurfaceRaised = "#101015",
		NeutralBg = "#1A1A22",

		OnSurface2 = "#C8C8CF",
		OnSurfaceTertiary = "#84848E",
		OnSurfaceQuaternary = "#40404A",
		Icon = "#84848E",

		Knob = "#E8E8EC"
	};

	/// <summary>The light palette of <see cref="MokaTheme.MatrixLight" />: pale neutral with red-tinted borders.</summary>
	public static MokaPalette NearBlackLight { get; } = GraphiteLight with
	{
		Outline = "rgba(198, 40, 40, 0.16)",
		OutlineVariant = "rgba(198, 40, 40, 0.10)",
		OutlineStrong = "rgba(198, 40, 40, 0.30)",
		OutlineHover = "rgba(198, 40, 40, 0.42)"
	};

	internal static MokaPalette ForSurfaces(MokaSurfaces surfaces, bool isDark) => (surfaces, isDark) switch
	{
		(MokaSurfaces.Graphite, true) => Graphite,
		(MokaSurfaces.Graphite, false) => GraphiteLight,
		(MokaSurfaces.NearBlack, true) => NearBlack,
		(MokaSurfaces.NearBlack, false) => NearBlackLight,
		(_, true) => Gate,
		(_, false) => GateLight
	};

	// PrimaryRow, PrimaryRing and the other mixes read --moka-color-primary, so they follow on their own.
	internal MokaPalette WithGateAccent(MokaAccentShades accent, bool isDark)
	{
		MokaPalette tiers = isDark
			? this with
			{
				OnPrimary = GateDarkInk,
				PrimaryDark = MokaColorMath.MixHex(accent.Color, GateDarkInk, 0.78),
				PrimaryGlow = MokaColorMath.Rgba(accent.Color, 0.08),
				PrimaryGlowMd = MokaColorMath.Rgba(accent.Color, 0.15),
				PrimaryGlowStrong = MokaColorMath.Rgba(accent.Color, 0.25),
				PrimaryGlowFaint = MokaColorMath.Rgba(accent.Color, 0.04),
				PrimaryBorder = MokaColorMath.Rgba(accent.Color, 0.30),
				PrimaryBorderDim = MokaColorMath.Rgba(accent.Color, 0.12)
			}
			: this with
			{
				OnPrimary = "#FFFFFF",
				PrimaryDark = MokaColorMath.MixHex(accent.Color, GateLightInk, 0.78),
				PrimaryGlow = MokaColorMath.Rgba(accent.Color, 0.10),
				PrimaryGlowMd = MokaColorMath.Rgba(accent.Color, 0.16),
				PrimaryGlowStrong = MokaColorMath.Rgba(accent.Color, 0.26),
				PrimaryGlowFaint = MokaColorMath.Rgba(accent.Color, 0.05),
				PrimaryBorder = MokaColorMath.Rgba(accent.Color, 0.36),
				PrimaryBorderDim = MokaColorMath.Rgba(accent.Color, 0.16)
			};

		return tiers with
		{
			Primary = accent.Color,
			PrimaryLight = accent.Soft,
			PrimaryBg = accent.Background
		};
	}

	internal MokaPalette WithGateContrast(bool high, bool isDark, MokaSurfaces surfaces = MokaSurfaces.BlueGrey)
	{
		if (!high)
		{
			MokaPalette normal = ForSurfaces(surfaces, isDark);
			return this with
			{
				OnSurface = normal.OnSurface,
				OnBackground = normal.OnBackground,
				OnSurface2 = normal.OnSurface2,
				OnSurfaceVariant = normal.OnSurfaceVariant,
				Secondary = normal.Secondary,
				Icon = normal.Icon,
				OnSurfaceTertiary = normal.OnSurfaceTertiary,
				Outline = normal.Outline,
				OutlineStrong = normal.OutlineStrong,
				OutlineVariant = normal.OutlineVariant
			};
		}

		return (surfaces, isDark) switch
		{
			(MokaSurfaces.Graphite or MokaSurfaces.NearBlack, true) => this with
			{
				OnSurface = "#F4F4F6",
				OnBackground = "#F4F4F6",
				OnSurface2 = "#DEDEE3",
				OnSurfaceVariant = "#BFBFC8",
				Secondary = "#BFBFC8",
				Icon = "#A8A8B2",
				OnSurfaceTertiary = "#9C9CA6",
				Outline = "#44444C",
				OutlineStrong = "#56565F",
				OutlineVariant = "#33333A"
			},
			(MokaSurfaces.Graphite or MokaSurfaces.NearBlack, false) => this with
			{
				OnSurface = "#0C0C10",
				OnBackground = "#0C0C10",
				OnSurface2 = "#25252B",
				OnSurfaceVariant = "#44444C",
				Secondary = "#44444C",
				Icon = "#52525B",
				OnSurfaceTertiary = "#52525B",
				Outline = "#BCBCC4",
				OutlineStrong = "#A0A0A9",
				OutlineVariant = "#D0D0D6"
			},
			(_, true) => this with
			{
				OnSurface = "#F4F6FA",
				OnBackground = "#F4F6FA",
				OnSurface2 = "#DCE1EA",
				OnSurfaceVariant = "#BAC3D4",
				Secondary = "#BAC3D4",
				Icon = "#A3ADBF",
				OnSurfaceTertiary = "#97A2B6",
				Outline = "#3A4762",
				OutlineStrong = "#48577A",
				OutlineVariant = "#2C374C"
			},
			(_, false) => this with
			{
				OnSurface = "#0B0F18",
				OnBackground = "#0B0F18",
				OnSurface2 = "#222B3A",
				OnSurfaceVariant = "#3E4859",
				Secondary = "#3E4859",
				Icon = "#4C566A",
				OnSurfaceTertiary = "#4C566A",
				Outline = "#B9C3D3",
				OutlineStrong = "#9DA9BC",
				OutlineVariant = "#CDD4DF"
			}
		};
	}
}
