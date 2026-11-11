using System.Collections.Frozen;
using Moka.Red.Core.Enums;

namespace Moka.Red.Data.FileBrowser;

/// <summary>
///     What kind of item something is, for <c>TypeColors</c>, <c>GroupBy</c> and the words under a preview. The browser
///     works it out from the name's extension, and an app can map its own through <c>FileType</c>.
/// </summary>
/// <param name="Category">The broad kind, which names its heading and picks its icon.</param>
/// <param name="Tag">The short tag drawn after the name, such as PNG. Null draws none.</param>
/// <param name="Tone">
///     The status color of the icon and the tag: <see cref="MokaColor.Success" />, <see cref="MokaColor.Warning" />,
///     <see cref="MokaColor.Error" /> or <see cref="MokaColor.Info" />. Null, or any other color, is neutral.
/// </param>
public sealed record MokaFileType(MokaFileCategory Category, string? Tag = null, MokaColor? Tone = null)
{
	private static readonly FrozenDictionary<string, MokaFileCategory> Extensions = new Dictionary<string, MokaFileCategory>(StringComparer.OrdinalIgnoreCase)
	{
		["png"] = MokaFileCategory.Image, ["jpg"] = MokaFileCategory.Image, ["jpeg"] = MokaFileCategory.Image,
		["gif"] = MokaFileCategory.Image, ["webp"] = MokaFileCategory.Image, ["svg"] = MokaFileCategory.Image,
		["bmp"] = MokaFileCategory.Image, ["ico"] = MokaFileCategory.Image, ["avif"] = MokaFileCategory.Image,
		["heic"] = MokaFileCategory.Image, ["heif"] = MokaFileCategory.Image, ["tif"] = MokaFileCategory.Image,
		["tiff"] = MokaFileCategory.Image,
		["txt"] = MokaFileCategory.Document, ["md"] = MokaFileCategory.Document, ["markdown"] = MokaFileCategory.Document,
		["pdf"] = MokaFileCategory.Document, ["doc"] = MokaFileCategory.Document, ["docx"] = MokaFileCategory.Document,
		["rtf"] = MokaFileCategory.Document, ["odt"] = MokaFileCategory.Document, ["xls"] = MokaFileCategory.Document,
		["xlsx"] = MokaFileCategory.Document, ["ods"] = MokaFileCategory.Document, ["csv"] = MokaFileCategory.Document,
		["tsv"] = MokaFileCategory.Document, ["ppt"] = MokaFileCategory.Document, ["pptx"] = MokaFileCategory.Document,
		["odp"] = MokaFileCategory.Document, ["epub"] = MokaFileCategory.Document,
		["cs"] = MokaFileCategory.Code, ["razor"] = MokaFileCategory.Code, ["cshtml"] = MokaFileCategory.Code,
		["js"] = MokaFileCategory.Code, ["mjs"] = MokaFileCategory.Code, ["ts"] = MokaFileCategory.Code,
		["tsx"] = MokaFileCategory.Code, ["jsx"] = MokaFileCategory.Code, ["json"] = MokaFileCategory.Code,
		["xml"] = MokaFileCategory.Code, ["html"] = MokaFileCategory.Code, ["htm"] = MokaFileCategory.Code,
		["css"] = MokaFileCategory.Code, ["scss"] = MokaFileCategory.Code, ["py"] = MokaFileCategory.Code,
		["rb"] = MokaFileCategory.Code, ["go"] = MokaFileCategory.Code, ["rs"] = MokaFileCategory.Code,
		["java"] = MokaFileCategory.Code, ["kt"] = MokaFileCategory.Code, ["c"] = MokaFileCategory.Code,
		["h"] = MokaFileCategory.Code, ["cpp"] = MokaFileCategory.Code, ["hpp"] = MokaFileCategory.Code,
		["sh"] = MokaFileCategory.Code, ["ps1"] = MokaFileCategory.Code, ["bat"] = MokaFileCategory.Code,
		["yml"] = MokaFileCategory.Code, ["yaml"] = MokaFileCategory.Code, ["toml"] = MokaFileCategory.Code,
		["sql"] = MokaFileCategory.Code, ["ini"] = MokaFileCategory.Code, ["config"] = MokaFileCategory.Code,
		["csproj"] = MokaFileCategory.Code, ["sln"] = MokaFileCategory.Code, ["slnx"] = MokaFileCategory.Code,
		["props"] = MokaFileCategory.Code, ["targets"] = MokaFileCategory.Code,
		["mp3"] = MokaFileCategory.Audio, ["wav"] = MokaFileCategory.Audio, ["flac"] = MokaFileCategory.Audio,
		["ogg"] = MokaFileCategory.Audio, ["m4a"] = MokaFileCategory.Audio, ["aac"] = MokaFileCategory.Audio,
		["opus"] = MokaFileCategory.Audio,
		["mp4"] = MokaFileCategory.Video, ["mov"] = MokaFileCategory.Video, ["mkv"] = MokaFileCategory.Video,
		["webm"] = MokaFileCategory.Video, ["avi"] = MokaFileCategory.Video, ["m4v"] = MokaFileCategory.Video,
		["zip"] = MokaFileCategory.Archive, ["tar"] = MokaFileCategory.Archive, ["gz"] = MokaFileCategory.Archive,
		["tgz"] = MokaFileCategory.Archive, ["7z"] = MokaFileCategory.Archive, ["rar"] = MokaFileCategory.Archive,
		["bz2"] = MokaFileCategory.Archive, ["xz"] = MokaFileCategory.Archive, ["nupkg"] = MokaFileCategory.Archive,
		["log"] = MokaFileCategory.Log
	}.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

	// A tag longer than this would push the name aside, so such an extension goes untagged.
	private const int LongestTag = 5;

	/// <summary>A folder: no tag, in the warning color, as folders are drawn without <c>TypeColors</c> too.</summary>
	public static MokaFileType Folder { get; } = new(MokaFileCategory.Folder, null, MokaColor.Warning);

	/// <summary>
	///     Works out a file's type from its name: the extension picks the category and becomes the tag. Pictures are
	///     green, text and code blue, PDFs red, and logs and the rest neutral.
	/// </summary>
	/// <param name="name">The file's name.</param>
	/// <returns>Its type. <see cref="MokaFileCategory.Other" /> with no tag when the name has no extension.</returns>
	public static MokaFileType FromName(string name)
	{
		ArgumentNullException.ThrowIfNull(name);
		int dot = name.LastIndexOf('.');
		if (dot <= 0 || dot == name.Length - 1)
		{
			return new MokaFileType(MokaFileCategory.Other);
		}

		string extension = name[(dot + 1)..];
		MokaFileCategory category = Extensions.GetValueOrDefault(extension, MokaFileCategory.Other);
		string? tag = extension.Length <= LongestTag ? extension.ToUpperInvariant() : null;
		MokaColor? tone = category switch
		{
			MokaFileCategory.Image => MokaColor.Success,
			MokaFileCategory.Document when string.Equals(extension, "pdf", StringComparison.OrdinalIgnoreCase) => MokaColor.Error,
			MokaFileCategory.Document or MokaFileCategory.Code => MokaColor.Info,
			_ => null
		};

		return new MokaFileType(category, tag, tone);
	}

	/// <summary>Works out an item's type: <see cref="Folder" /> for a folder, and <see cref="FromName" /> for a file.</summary>
	/// <param name="info">What the browser knows about the item.</param>
	/// <returns>Its type.</returns>
	public static MokaFileType Of(MokaFileInfo info)
	{
		ArgumentNullException.ThrowIfNull(info);
		return info.IsFolder ? Folder : FromName(info.Name);
	}
}
