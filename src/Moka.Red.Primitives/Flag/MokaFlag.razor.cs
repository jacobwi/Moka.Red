using System.Collections.Frozen;
using System.Text;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Tooltip;

namespace Moka.Red.Primitives.Flag;

/// <summary>
///     A country's flag from its two-letter code, drawn as the flag emoji in the Twemoji Country Flags font that
///     <c>moka-flags.css</c> declares, so it looks the same on every system, Windows included. A missing or
///     unknown code shows a globe.
/// </summary>
public partial class MokaFlag : MokaVisualComponentBase
{
	private const int RegionalIndicatorA = 0x1F1E6;
	private const int WavingBlackFlag = 0x1F3F4;
	private const int TagBase = 0xE0000;
	private const int CancelTag = 0xE007F;

	// The regions Unicode draws a flag for (RGI_Emoji_Flag_Sequence): ISO 3166-1 codes plus EU, UN and the
	// few exceptional reservations emoji fonts carry. Any other pair of letters would draw as two boxed letters.
	private static readonly FrozenSet<string> Regions = (
			"AC AD AE AF AG AI AL AM AO AQ AR AS AT AU AW AX AZ BA BB BD BE BF BG BH BI BJ BL BM BN BO BQ BR BS BT BV " +
			"BW BY BZ CA CC CD CF CG CH CI CK CL CM CN CO CP CR CU CV CW CX CY CZ DE DG DJ DK DM DO DZ EA EC EE EG EH ER " +
			"ES ET EU FI FJ FK FM FO FR GA GB GD GE GF GG GH GI GL GM GN GP GQ GR GS GT GU GW GY HK HM HN HR HT HU IC ID " +
			"IE IL IM IN IO IQ IR IS IT JE JM JO JP KE KG KH KI KM KN KP KR KW KY KZ LA LB LC LI LK LR LS LT LU LV LY MA " +
			"MC MD ME MF MG MH MK ML MM MN MO MP MQ MR MS MT MU MV MW MX MY MZ NA NC NE NF NG NI NL NO NP NR NU NZ OM PA " +
			"PE PF PG PH PK PL PM PN PR PS PT PW PY QA RE RO RS RU RW SA SB SC SD SE SG SH SI SJ SK SL SM SN SO SR SS ST " +
			"SV SX SY SZ TA TC TD TF TG TH TJ TK TL TM TN TO TR TT TV TW TZ UA UG UM UN US UY UZ VA VC VE VG VI VN VU WF " +
			"WS XK YE YT ZA ZM ZW")
		.Split(' ')
		.ToFrozenSet(StringComparer.Ordinal);

	private static readonly FrozenSet<string> Subdivisions =
		new[] { "GBENG", "GBSCT", "GBWLS" }.ToFrozenSet(StringComparer.Ordinal);

	private string? _emoji;

	/// <summary>
	///     The country's ISO 3166-1 alpha-2 code, such as "SE" or "us", in any case. "GB-ENG", "GB-SCT" and "GB-WLS"
	///     draw England, Scotland and Wales. Null, empty or a code with no flag shows a globe.
	/// </summary>
	[Parameter]
	public string? Code { get; set; }

	/// <summary>The country's name, read out by screen readers and shown in the tooltip. Null uses the code.</summary>
	[Parameter]
	public string? Name { get; set; }

	/// <summary>What the globe is called when there is no code. Default "Unknown country".</summary>
	[Parameter]
	public string UnknownName { get; set; } = "Unknown country";

	/// <summary>Whether a tooltip shows the name on hover. Default false.</summary>
	[Parameter]
	public bool ShowTooltip { get; set; }

	/// <summary>Where the tooltip opens. Default Top.</summary>
	[Parameter]
	public MokaTooltipPosition TooltipPosition { get; set; } = MokaTooltipPosition.Top;

	/// <inheritdoc />
	protected override string RootClass => "moka-flag";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-flag--{SizeToKebab(Size)}", SizeValue is null)
		.AddClass("moka-flag--unknown", _emoji is null)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("font-size", SizeValue is null ? null : CssValues.WithPixels(SizeValue))
		.AddStyle(Style)
		.Build();

	private string Label
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(Name))
			{
				return Name;
			}

			return _emoji is null ? UnknownName : Code!.Trim().ToUpperInvariant();
		}
	}

	/// <summary>
	///     The flag emoji for a country code, or null when the code has no flag. Accepts the same codes as
	///     <see cref="Code" />.
	/// </summary>
	/// <param name="code">A two-letter country code, or GB-ENG, GB-SCT or GB-WLS.</param>
	/// <returns>The emoji, or null.</returns>
	public static string? ToEmoji(string? code)
	{
		if (string.IsNullOrWhiteSpace(code))
		{
			return null;
		}

		string letters = code.Trim().Replace("-", "", StringComparison.Ordinal)
			.Replace("_", "", StringComparison.Ordinal)
			.ToUpperInvariant();

		if (letters.Length == 2 && Regions.Contains(letters))
		{
			return char.ConvertFromUtf32(RegionalIndicatorA + (letters[0] - 'A'))
			       + char.ConvertFromUtf32(RegionalIndicatorA + (letters[1] - 'A'));
		}

		if (Subdivisions.Contains(letters))
		{
			StringBuilder sequence = new(char.ConvertFromUtf32(WavingBlackFlag));
			foreach (char letter in letters)
			{
				sequence.Append(char.ConvertFromUtf32(TagBase + char.ToLowerInvariant(letter)));
			}

			return sequence.Append(char.ConvertFromUtf32(CancelTag)).ToString();
		}

		return null;
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		_emoji = ToEmoji(Code);
	}
}
