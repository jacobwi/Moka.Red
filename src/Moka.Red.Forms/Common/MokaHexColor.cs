using System.Diagnostics.CodeAnalysis;

namespace Moka.Red.Forms.Common;

/// <summary>The hex colour check the colour inputs share.</summary>
internal static class MokaHexColor
{
	/// <summary>
	///     Whether <paramref name="value" /> is <c>#</c> followed by 3, 4, 6 or 8 hex digits. The colour
	///     inputs write only such a value into a style, so text such as
	///     <c>"red; background-image: url(...)"</c> cannot add declarations of its own.
	/// </summary>
	internal static bool IsValid([NotNullWhen(true)] string? value) =>
		value is { Length: 4 or 5 or 7 or 9 } && value[0] == '#' && value.Skip(1).All(char.IsAsciiHexDigit);
}
