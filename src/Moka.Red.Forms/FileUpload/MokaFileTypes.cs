using System.Globalization;
using Microsoft.AspNetCore.Components.Forms;
using Moka.Red.Core.Icons;
using Moka.Red.Icons;

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

	/// <summary>A limit as people say it, such as "10 MB": like <see cref="FormatSize" /> without a trailing ".0".</summary>
	public static string FormatLimit(long bytes) => bytes switch
	{
		< 1024 => $"{bytes} B",
		< 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
		< 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):0.#} MB",
		_ => $"{bytes / (1024.0 * 1024.0 * 1024.0):0.#} GB"
	};

	/// <summary>A byte count the way a terminal writes it, such as "2.4M" or "860K".</summary>
	public static string FormatShortSize(long bytes) => bytes switch
	{
		< 1024 => string.Create(CultureInfo.InvariantCulture, $"{bytes}B"),
		< 1024 * 1024 => Short(bytes / 1024.0, 'K'),
		< 1024 * 1024 * 1024 => Short(bytes / (1024.0 * 1024.0), 'M'),
		_ => Short(bytes / (1024.0 * 1024.0 * 1024.0), 'G')
	};

	private static string Short(double value, char unit) => value < 10
		? string.Create(CultureInfo.InvariantCulture, $"{value:0.#}{unit}")
		: string.Create(CultureInfo.InvariantCulture, $"{Math.Round(value):0}{unit}");

	/// <summary>Text in lower case, for matching extensions and types and for the terminal look's type list.</summary>
	public static string Lower(string value) => string.Create(value.Length, value, static (span, source) =>
	{
		for (int i = 0; i < source.Length; i++)
		{
			span[i] = char.ToLowerInvariant(source[i]);
		}
	});

	/// <summary>What kind of file this is, from its extension and then its content type.</summary>
	public static MokaFileKind KindOf(string name, string? contentType)
	{
		string extension = Lower(Path.GetExtension(name).TrimStart('.'));
		string type = Lower(contentType ?? string.Empty);
		return extension switch
		{
			"pdf" => MokaFileKind.Pdf,
			"png" or "jpg" or "jpeg" or "gif" or "webp" or "avif" or "bmp" or "svg" or "heic" or "ico" => MokaFileKind.Image,
			"xls" or "xlsx" or "csv" or "ods" or "tsv" or "numbers" => MokaFileKind.Sheet,
			"zip" or "rar" or "7z" or "tar" or "gz" or "tgz" or "bz2" or "xz" => MokaFileKind.Archive,
			"doc" or "docx" or "txt" or "md" or "rtf" or "odt" or "pages" => MokaFileKind.Document,
			"mp4" or "mov" or "webm" or "mkv" or "avi" or "mp3" or "wav" or "ogg" or "flac" or "m4a" => MokaFileKind.Media,
			_ when type == "application/pdf" => MokaFileKind.Pdf,
			_ when type.StartsWith("image/", StringComparison.Ordinal) => MokaFileKind.Image,
			_ when type.StartsWith("video/", StringComparison.Ordinal) || type.StartsWith("audio/", StringComparison.Ordinal) => MokaFileKind.Media,
			_ when type.StartsWith("text/", StringComparison.Ordinal) => MokaFileKind.Document,
			_ when type.Contains("zip", StringComparison.Ordinal) => MokaFileKind.Archive,
			_ => MokaFileKind.Other
		};
	}

	/// <summary>The icon that stands for a kind of file.</summary>
	public static MokaIconDefinition IconOf(MokaFileKind kind) => kind switch
	{
		MokaFileKind.Pdf or MokaFileKind.Document => MokaIcons.File.FileText,
		MokaFileKind.Image => MokaIcons.File.FileImage,
		MokaFileKind.Sheet => MokaIcons.File.FileSpreadsheet,
		MokaFileKind.Archive => MokaIcons.File.FileArchive,
		MokaFileKind.Media => MokaIcons.File.FileVideo,
		_ => MokaIcons.File.Document
	};

	/// <summary>The tone a kind of file's tile takes, as the end of a class name.</summary>
	public static string ToneOf(MokaFileKind kind) => kind switch
	{
		MokaFileKind.Pdf => "error",
		MokaFileKind.Image => "info",
		MokaFileKind.Sheet => "success",
		MokaFileKind.Archive => "warning",
		MokaFileKind.Document or MokaFileKind.Media => "primary",
		_ => "neutral"
	};

	/// <summary>A file's type as a short label, such as "PDF": its extension, or the end of its content type.</summary>
	public static string TypeLabel(string name, string? contentType)
	{
		string extension = Path.GetExtension(name).TrimStart('.');
		if (extension.Length is > 0 and <= 8)
		{
			return extension.ToUpperInvariant();
		}

		return TypeLabel(contentType);
	}

	private static string TypeLabel(string? contentType)
	{
		string type = contentType ?? string.Empty;
		int slash = type.IndexOf('/', StringComparison.Ordinal);
		if (slash < 0 || slash == type.Length - 1)
		{
			return string.Empty;
		}

		string subtype = type[(slash + 1)..];
		int plus = subtype.IndexOf('+', StringComparison.Ordinal);
		subtype = plus > 0 ? subtype[..plus] : subtype;
		subtype = subtype.StartsWith("x-", StringComparison.OrdinalIgnoreCase) ? subtype[2..] : subtype;
		return Lower(subtype) switch
		{
			"jpeg" => "JPG",
			"zip-compressed" => "ZIP",
			"vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "XLSX",
			"vnd.openxmlformats-officedocument.wordprocessingml.document" => "DOCX",
			"vnd.ms-excel" => "XLS",
			"msword" => "DOC",
			"plain" => "TXT",
			_ when subtype.Length > 8 => string.Empty,
			_ => subtype.ToUpperInvariant()
		};
	}

	/// <summary>
	///     The types an <c>accept</c> list takes, as short labels in its order ("PDF", "PNG"), with each wildcard as the
	///     word for it. Empty when the list takes every file.
	/// </summary>
	public static IReadOnlyList<string> AcceptLabels(string? accept, string images)
	{
		List<string> labels = [];
		foreach (string token in (accept ?? string.Empty)
			         .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			if (token == "*/*")
			{
				return [];
			}

			string? label = token switch
			{
				_ when token.StartsWith('.') => token.Length > 1 ? token[1..].ToUpperInvariant() : null,
				_ when token.Equals("image/*", StringComparison.OrdinalIgnoreCase) => images,
				_ when token.EndsWith("/*", StringComparison.Ordinal) => Lower(token[..^2]),
				_ when token.Contains('/', StringComparison.Ordinal) => TypeLabel(token),
				_ => null
			};

			if (!string.IsNullOrEmpty(label) && !labels.Contains(label, StringComparer.OrdinalIgnoreCase))
			{
				labels.Add(label);
			}
		}

		return labels;
	}

	/// <summary>Labels joined the way a sentence lists them: "PDF, PNG or JPG".</summary>
	public static string JoinLabels(IReadOnlyList<string> labels, string or) => labels.Count switch
	{
		0 => string.Empty,
		1 => labels[0],
		_ => $"{string.Join(", ", labels.Take(labels.Count - 1))} {or} {labels[^1]}"
	};
}

/// <summary>What kind of file a file is, for the icon and the tone its tile takes.</summary>
internal enum MokaFileKind
{
	Other,
	Pdf,
	Document,
	Image,
	Sheet,
	Archive,
	Media
}
