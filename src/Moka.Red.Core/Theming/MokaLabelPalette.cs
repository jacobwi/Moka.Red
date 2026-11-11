using System.Globalization;
using System.Text;
using Moka.Red.Core.Enums;

namespace Moka.Red.Core.Theming;

/// <summary>One label color's shades in one tone.</summary>
/// <param name="Tint">What the tag background mixes into the surface.</param>
/// <param name="DarkSolid">The dot and icon color on a dark scheme.</param>
/// <param name="DarkInk">Text on the tint on a dark scheme.</param>
/// <param name="LightSolid">The dot and icon color on a light scheme.</param>
/// <param name="LightInk">Text on the tint on a light scheme.</param>
internal readonly record struct MokaLabelShades(string Tint, string DarkSolid, string DarkInk, string LightSolid, string LightInk);

/// <summary>The label color tokens for every tone and scheme.</summary>
internal static class MokaLabelPalette
{
	// Vivid is the board BC approved: Tailwind's 500 hues, a 20% tint over the surface on dark and 16% on light, ink
	// at 55% toward white or 62% toward black. Where that misses the contrast targets the shade was moved by WCAG
	// luminance at the same hue: light solids are darkened to luminance 0.25 so a dot shows 3:1 on every light page,
	// and light inks are darkened until they read 4.8:1 on their tint. Muted keeps each shade's luminance at 55% of the
	// saturation. Pastel mixes the dark solid 45% toward white, keeps light solids at luminance 0.25 with 75% of the
	// saturation, and tints at 22%. GateContrastTests checks every value against every preset.
	private static readonly (MokaLabelColor Color, MokaLabelShades Vivid, MokaLabelShades Muted, MokaLabelShades Pastel)[] Rows =
	[
		(MokaLabelColor.Red, new("#EF4444", "#EF4444", "#F69898", "#EF4444", "#942A2A"), new("#C76767", "#C76767", "#E0ABAB", "#C76767", "#7B4040"), new("#EF4444", "#F69898", "#F69898", "#DC6262", "#942A2A")),
		(MokaLabelColor.Rose, new("#F43F5E", "#F43F5E", "#F995A6", "#F43F5E", "#97273A"), new("#CB6678", "#CB6678", "#E2ABB5", "#CB6678", "#7E3F4A"), new("#F43F5E", "#F995A6", "#F995A6", "#DF5E75", "#97273A")),
		(MokaLabelColor.Orange, new("#F97316", "#F97316", "#FCB27F", "#E46106", "#9A470E"), new("#CD8C5E", "#CD8C5E", "#E4C0A6", "#C37641", "#7F573A"), new("#F97316", "#FCB27F", "#FCB27F", "#D56D24", "#9A470E")),
		(MokaLabelColor.Amber, new("#F59E0B", "#F59E0B", "#FACA79", "#BE7A08", "#935E07"), new("#D0AB6E", "#D0AB6E", "#E5D1AF", "#AD8239", "#7D6742"), new("#F59E0B", "#FACA79", "#FACA79", "#B67F21", "#8F5C07")),
		(MokaLabelColor.Yellow, new("#EAB308", "#EAB308", "#F3D577", "#AC8306", "#856605"), new("#D1B970", "#D1B970", "#E6D9B0", "#A18634", "#786B40"), new("#EAB308", "#F3D577", "#F3D577", "#A7851D", "#826304")),
		(MokaLabelColor.Lime, new("#84CC16", "#84CC16", "#BBE37F", "#629710", "#4D760D"), new("#A0C467", "#A0C467", "#CBDFAB", "#709439", "#5D723C"), new("#84CC16", "#BBE37F", "#BBE37F", "#699625", "#4B730D")),
		(MokaLabelColor.Green, new("#22C55E", "#22C55E", "#85DFA6", "#1B9E4B", "#157839"), new("#68BC87", "#68BC87", "#ACDABD", "#449A63", "#3F7453"), new("#22C55E", "#85DFA6", "#85DFA6", "#309B57", "#147437")),
		(MokaLabelColor.Emerald, new("#10B981", "#10B981", "#7CD9BA", "#0E9D6D", "#0A7350"), new("#43B58F", "#43B58F", "#98D6C1", "#389A79", "#2A7059"), new("#10B981", "#7CD9BA", "#7CD9BA", "#239A73", "#0A724F")),
		(MokaLabelColor.Teal, new("#14B8A6", "#14B8A6", "#7ED8CE", "#119A8A", "#0C7267"), new("#46B5A8", "#46B5A8", "#99D6CF", "#3B978D", "#2B7068"), new("#14B8A6", "#7ED8CE", "#7ED8CE", "#26998C", "#0C7065")),
		(MokaLabelColor.Cyan, new("#06B6D4", "#06B6D4", "#76D7E7", "#0597AF", "#047183"), new("#4BB4C6", "#4BB4C6", "#9CD6E0", "#3496A6", "#2F707B"), new("#06B6D4", "#76D7E7", "#76D7E7", "#1D96AA", "#046E80")),
		(MokaLabelColor.Sky, new("#0EA5E9", "#0EA5E9", "#7ACEF3", "#0C92CD", "#096690"), new("#5AA4C6", "#5AA4C6", "#A4CDE0", "#3F92B8", "#38667B"), new("#0EA5E9", "#7ACEF3", "#7ACEF3", "#2792C3", "#096690")),
		(MokaLabelColor.Blue, new("#3B82F6", "#3B82F6", "#93BAFA", "#3B82F6", "#255199"), new("#5D86C9", "#5D86C9", "#A6BCE1", "#5D86C9", "#3A537D"), new("#3B82F6", "#93BAFA", "#93BAFA", "#5488DF", "#255199")),
		(MokaLabelColor.Indigo, new("#6366F1", "#6366F1", "#A9ABF7", "#6366F1", "#3D3F95"), new("#6C6EC8", "#6C6EC8", "#AEAFE1", "#6C6EC8", "#43447C"), new("#6366F1", "#A9ABF7", "#A9ABF7", "#7D7FE1", "#3D3F95")),
		(MokaLabelColor.Violet, new("#8B5CF6", "#8B5CF6", "#BFA5FA", "#8B5CF6", "#563999"), new("#886ACC", "#886ACC", "#BEADE3", "#886ACC", "#54427E"), new("#8B5CF6", "#BFA5FA", "#BFA5FA", "#9776E4", "#563999")),
		(MokaLabelColor.Purple, new("#A855F7", "#A855F7", "#CFA2FB", "#A855F7", "#683599"), new("#9C69CD", "#9C69CD", "#C9ADE4", "#9C69CD", "#61417F"), new("#A855F7", "#CFA2FB", "#CFA2FB", "#AA6DE3", "#683599")),
		(MokaLabelColor.Pink, new("#EC4899", "#EC4899", "#F59AC7", "#EC4899", "#922D5F"), new("#C66A98", "#C66A98", "#E0ADC6", "#C66A98", "#7B425E"), new("#EC4899", "#F59AC7", "#F59AC7", "#D85F9B", "#922D5F")),
		(MokaLabelColor.Slate, new("#64748B", "#64748B", "#AAB3BF", "#64748B", "#3E4856"), new("#6B7480", "#6B7480", "#AEB3B9", "#6B7480", "#42484F"), new("#64748B", "#AAB3BF", "#AAB3BF", "#7E8A9B", "#3E4856"))
	];

	private static readonly string[] Blocks = BuildBlocks();

	/// <summary>The shades of <paramref name="color" /> in <paramref name="tone" />.</summary>
	internal static MokaLabelShades For(MokaLabelColor color, MokaLabelTone tone)
	{
		(MokaLabelColor _, MokaLabelShades vivid, MokaLabelShades muted, MokaLabelShades pastel) = Rows[(int)color];
		return tone switch
		{
			MokaLabelTone.Muted => muted,
			MokaLabelTone.Pastel => pastel,
			_ => vivid
		};
	}

	/// <summary>How much of the tint the tag background mixes into the surface, in percent.</summary>
	internal static int TintPercent(MokaLabelTone tone, bool isDark) => tone switch
	{
		MokaLabelTone.Pastel => 22,
		_ => isDark ? 20 : 16
	};

	/// <summary>The value of <c>--moka-label-{key}-bg</c>.</summary>
	internal static string Background(MokaLabelShades shades, MokaLabelTone tone, bool isDark) =>
		string.Create(CultureInfo.InvariantCulture,
			$"color-mix(in srgb, {shades.Tint} {TintPercent(tone, isDark)}%, var(--moka-color-surface))");

	/// <summary>Every label token as <c>name: value; </c> declarations, the format of <c>ToCssVariables</c>.</summary>
	internal static string Tokens(MokaLabelTone tone, bool isDark)
	{
		int index = ((int)tone * 2) + (isDark ? 1 : 0);
		return (uint)index < (uint)Blocks.Length ? Blocks[index] : Blocks[isDark ? 1 : 0];
	}

	private static string[] BuildBlocks()
	{
		MokaLabelTone[] tones = Enum.GetValues<MokaLabelTone>();
		string[] blocks = new string[tones.Length * 2];
		foreach (MokaLabelTone tone in tones)
		{
			for (int scheme = 0; scheme < 2; scheme++)
			{
				bool isDark = scheme == 1;
				var sb = new StringBuilder(4096);
				foreach (MokaLabelColor color in MokaLabelColors.All)
				{
					MokaLabelShades shades = For(color, tone);
					string token = $"--moka-label-{MokaLabelColors.Key(color)}";
					sb.Append(token).Append(": ").Append(isDark ? shades.DarkSolid : shades.LightSolid).Append("; ");
					sb.Append(token).Append("-bg: ").Append(Background(shades, tone, isDark)).Append("; ");
					sb.Append(token).Append("-ink: ").Append(isDark ? shades.DarkInk : shades.LightInk).Append("; ");
				}

				blocks[((int)tone * 2) + scheme] = sb.ToString();
			}
		}

		return blocks;
	}
}
