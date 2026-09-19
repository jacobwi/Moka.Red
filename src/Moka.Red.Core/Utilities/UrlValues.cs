namespace Moka.Red.Core.Utilities;

/// <summary>
///     Checks URLs a component renders as a link target. Blazor writes an <c>href</c> as it is given, and
///     a <c>javascript:</c> URL there runs script in the page when the link is followed, so a link from
///     user data could run anything.
/// </summary>
public static class UrlValues
{
	// The longest blocked scheme is "javascript".
	private const int LongestBlockedScheme = 10;

	/// <summary>
	///     True when <paramref name="href" /> has a <c>javascript:</c>, <c>vbscript:</c> or <c>data:</c>
	///     scheme, read the way a browser reads it: leading spaces and control characters are skipped, tabs
	///     and line breaks inside the scheme are ignored, and case does not matter. Relative URLs, fragments,
	///     <c>http</c>, <c>https</c>, <c>mailto</c>, <c>tel</c> and every other scheme are not blocked.
	/// </summary>
	/// <param name="href">The URL to check. <c>null</c> has no scheme.</param>
	/// <returns><c>true</c> when following the URL could run script or load a document made from the URL itself.</returns>
	public static bool HasBlockedScheme(string? href)
	{
		if (href is null)
		{
			return false;
		}

		ReadOnlySpan<char> value = href;
		int i = 0;
		while (i < value.Length && value[i] <= ' ')
		{
			i++;
		}

		Span<char> scheme = stackalloc char[LongestBlockedScheme];
		int length = 0;
		for (; i < value.Length; i++)
		{
			char c = value[i];
			if (c is '\t' or '\n' or '\r')
			{
				continue;
			}

			if (c == ':')
			{
				return IsBlocked(scheme[..length]);
			}

			bool schemeChar = length == 0
				? char.IsAsciiLetter(c)
				: char.IsAsciiLetterOrDigit(c) || c is '+' or '-' or '.';

			// Anything else, a '/', '?' or '#' included, means there is no scheme: the URL is relative.
			// A scheme longer than any blocked one is allowed.
			if (!schemeChar || length == LongestBlockedScheme)
			{
				return false;
			}

			scheme[length++] = char.ToLowerInvariant(c);
		}

		return false;
	}

	/// <summary>
	///     <paramref name="href" /> as given, or <c>null</c> when <see cref="HasBlockedScheme" /> blocks it, so
	///     the link renders with no <c>href</c> at all.
	/// </summary>
	/// <param name="href">The link target, usually straight from a component parameter.</param>
	/// <returns>The link target to render, or <c>null</c>.</returns>
	public static string? SafeHref(string? href) => HasBlockedScheme(href) ? null : href;

	private static bool IsBlocked(ReadOnlySpan<char> scheme) =>
		scheme is "javascript" or "vbscript" or "data";
}
