using Microsoft.AspNetCore.Components.Forms;

namespace Moka.Red.Forms.FileUpload;

/// <summary>The checks a file picked or dropped onto a Moka upload goes through.</summary>
internal static class MokaFileTypes
{
	/// <summary>
	///     Whether a file matches an <c>accept</c> list. The dialog filters by it, but a dropped file meets it only
	///     here. As in the browser, a token is an extension (".pdf"), a MIME type ("application/pdf") or a wildcard
	///     ("image/*"), compared without case, and any other token is ignored, so a list with no valid token takes
	///     every file.
	/// </summary>
	public static bool IsAccepted(IBrowserFile file, string? accept)
	{
		string contentType = file.ContentType ?? string.Empty;
		string[] types = (accept ?? string.Empty)
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		bool anyValid = false;

		foreach (string type in types)
		{
			bool matches;
			if (type.StartsWith('.'))
			{
				matches = file.Name.EndsWith(type, StringComparison.OrdinalIgnoreCase);
			}
			else if (type.EndsWith("/*", StringComparison.Ordinal))
			{
				matches = type == "*/*" || contentType.StartsWith(type[..^1], StringComparison.OrdinalIgnoreCase);
			}
			else if (type.Contains('/', StringComparison.Ordinal))
			{
				matches = string.Equals(contentType, type, StringComparison.OrdinalIgnoreCase);
			}
			else
			{
				continue;
			}

			anyValid = true;
			if (matches)
			{
				return true;
			}
		}

		return !anyValid;
	}

	/// <summary>A byte count as a short size, such as "1.5 MB".</summary>
	public static string FormatSize(long bytes) => bytes switch
	{
		< 1024 => $"{bytes} B",
		< 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
		< 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
		_ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB"
	};
}
