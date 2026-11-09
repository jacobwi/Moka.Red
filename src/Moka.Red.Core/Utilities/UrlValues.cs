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

	// The longest scheme a picture may come from is "https".
	private const int LongestImageScheme = 5;

	private static readonly string[] ImageDataTypes =
	[
		"image/png", "image/jpeg", "image/jpg", "image/gif", "image/webp", "image/avif", "image/svg+xml", "image/bmp",
		"image/x-icon", "image/vnd.microsoft.icon"
	];

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

	/// <summary>
	///     <paramref name="source" /> as given when it can only be a picture: a relative address, <c>http</c>, <c>https</c>,
	///     <c>blob</c>, or a <c>data:</c> URI whose type is an image (PNG, JPEG, GIF, WebP, AVIF, SVG, BMP or an icon).
	///     Anything else gives <c>null</c>, so an <c>img</c> from a third party's package never gets a document or a script
	///     URL. The scheme is read as <see cref="HasBlockedScheme" /> reads it.
	/// </summary>
	/// <param name="source">The address for an <c>img</c>.</param>
	/// <returns>The address to render, or <c>null</c>.</returns>
	public static string? SafeImageSource(string? source)
	{
		if (string.IsNullOrWhiteSpace(source))
		{
			return null;
		}

		ReadOnlySpan<char> value = source;
		int i = 0;
		while (i < value.Length && value[i] <= ' ')
		{
			i++;
		}

		Span<char> scheme = stackalloc char[LongestImageScheme];
		int length = 0;
		bool tooLong = false;
		for (; i < value.Length; i++)
		{
			char c = value[i];
			if (c is '\t' or '\n' or '\r')
			{
				continue;
			}

			if (c == ':')
			{
				if (tooLong)
				{
					return null;
				}

				ReadOnlySpan<char> name = scheme[..length];
				return name is "http" or "https" or "blob" || (name is "data" && IsImageData(value[(i + 1)..]))
					? source
					: null;
			}

			bool schemeChar = length == 0 && !tooLong
				? char.IsAsciiLetter(c)
				: char.IsAsciiLetterOrDigit(c) || c is '+' or '-' or '.';
			if (!schemeChar)
			{
				return source;
			}

			if (length == LongestImageScheme)
			{
				tooLong = true;
				continue;
			}

			scheme[length++] = char.ToLowerInvariant(c);
		}

		return source;
	}

	private static bool IsImageData(ReadOnlySpan<char> rest)
	{
		int end = rest.IndexOfAny(';', ',');
		ReadOnlySpan<char> type = (end < 0 ? rest : rest[..end]).Trim();
		foreach (string allowed in ImageDataTypes)
		{
			if (type.Equals(allowed, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsBlocked(ReadOnlySpan<char> scheme) =>
		scheme is "javascript" or "vbscript" or "data";
}
