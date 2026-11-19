using System.Net;
using System.Net.Mail;
using System.Net.Sockets;

namespace Moka.Red.Forms.SchemaForm;

// The JSON Schema 2020-12 formats the form checks (validation spec, section 7.3). Any other format is an annotation only,
// as the format-annotation vocabulary has it.
internal static class MokaSchemaFormats
{
	internal const string Email = "email";
	internal const string Uri = "uri";
	internal const string HostName = "hostname";
	internal const string Ipv4 = "ipv4";
	internal const string Ipv6 = "ipv6";
	internal const string Uuid = "uuid";

	internal static string? Problem(string? format, string text, string title, MokaSchemaFormText words) => format switch
	{
		Email when !IsEmail(text) => words.NotEmail(title),
		Uri when !IsAbsoluteUri(text) => words.NotUri(title),
		HostName when !IsHostName(text) => words.NotHostName(title),
		Ipv4 when !IsIpv4(text) => words.NotIpv4(title),
		Ipv6 when !IsIpv6(text) => words.NotIpv6(title),
		Uuid when !Guid.TryParseExact(text, "D", out _) => words.NotUuid(title),
		_ => null
	};

	internal static string InputType(string? format) => format switch
	{
		Email => "email",
		Uri => "url",
		_ => "text"
	};

	internal static bool IsMono(string? format) => format is Uri or HostName or Ipv4 or Ipv6 or Uuid;

	// RFC 5321 mailbox: the address alone, with no display name around it.
	private static bool IsEmail(string text) =>
		MailAddress.TryCreate(text, out MailAddress? address) &&
		address.DisplayName.Length == 0 &&
		string.Equals(address.Address, text, StringComparison.Ordinal);

	// RFC 3986 absolute URI. Uri.TryCreate also takes "C:\x" and "/x" as file paths, so the text has to start with the
	// scheme it was read with.
	private static bool IsAbsoluteUri(string text) =>
		!text.Any(char.IsWhiteSpace) &&
		System.Uri.TryCreate(text, UriKind.Absolute, out Uri? uri) &&
		text.StartsWith(uri.Scheme + ":", StringComparison.OrdinalIgnoreCase);

	// RFC 1123 section 2.1: labels of letters, digits and hyphens, 1 to 63 long, not starting or ending with a hyphen.
	private static bool IsHostName(string text)
	{
		if (text.Length is 0 or > 253)
		{
			return false;
		}

		foreach (string label in text.Split('.'))
		{
			if (label.Length is 0 or > 63 || label[0] == '-' || label[^1] == '-' ||
			    !label.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
			{
				return false;
			}
		}

		return true;
	}

	// RFC 2673 dotted quad. IPAddress.TryParse also reads "1" and "0x7f.1", which the format doesn't allow.
	private static bool IsIpv4(string text)
	{
		string[] parts = text.Split('.');
		return parts.Length == 4 && parts.All(part =>
			part.Length is >= 1 and <= 3 &&
			part.All(char.IsAsciiDigit) &&
			(part.Length == 1 || part[0] != '0') &&
			int.Parse(part, System.Globalization.CultureInfo.InvariantCulture) <= 255);
	}

	// RFC 4291, without a zone, which the format doesn't include.
	private static bool IsIpv6(string text) =>
		text.Contains(':', StringComparison.Ordinal) &&
		!text.Contains('%', StringComparison.Ordinal) &&
		!text.Contains('[', StringComparison.Ordinal) &&
		IPAddress.TryParse(text, out IPAddress? address) &&
		address.AddressFamily == AddressFamily.InterNetworkV6;
}
