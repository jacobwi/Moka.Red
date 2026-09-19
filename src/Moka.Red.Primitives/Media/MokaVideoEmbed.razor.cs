using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Media;

/// <summary>
///     Responsive video embed supporting YouTube, Vimeo, and direct video URLs.
///     Auto-detects the platform from the URL and converts to the appropriate embed format.
/// </summary>
public partial class MokaVideoEmbed : MokaVisualComponentBase
{
	/// <summary>
	///     Video URL or embed URL. Required. A YouTube or Vimeo address is embedded in an iframe only when it
	///     is an http or https URL; anything else goes to a <c>video</c> element.
	/// </summary>
	[Parameter]
	[EditorRequired]
	public string Src { get; set; } = string.Empty;

	/// <summary>Title for accessibility (iframe title attribute).</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>CSS aspect ratio. Default "16/9".</summary>
	[Parameter]
	public string AspectRatio { get; set; } = "16/9";

	/// <summary>Whether to allow fullscreen. Default true.</summary>
	[Parameter]
	public bool AllowFullscreen { get; set; } = true;

	/// <summary>Whether to auto-play the video. Default false.</summary>
	[Parameter]
	public bool AutoPlay { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-video-embed";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-fill-width")
		.AddClass("moka-video-embed--rounded", Rounded is not null && Rounded != MokaRounding.None)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("aspect-ratio", AspectRatio)
		.AddStyle(Style)
		.Build();

	private bool IsEmbedUrl => (IsYouTube || IsVimeo) && IsWebUrl(EmbedSrc);

	private bool IsYouTube => !string.IsNullOrEmpty(Src) &&
	                          (Src.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
	                           Src.Contains("youtu.be", StringComparison.OrdinalIgnoreCase));

	private bool IsVimeo => !string.IsNullOrEmpty(Src) &&
	                        Src.Contains("vimeo.com", StringComparison.OrdinalIgnoreCase);

	private string EmbedSrc
	{
		get
		{
			if (string.IsNullOrEmpty(Src))
			{
				return string.Empty;
			}

			string url = ConvertToEmbedUrl(Src);
			if (AutoPlay && !url.Contains("autoplay", StringComparison.OrdinalIgnoreCase))
			{
				url += url.Contains('?', StringComparison.Ordinal) ? "&autoplay=1" : "?autoplay=1";
			}

			return url;
		}
	}

	private string IframeAllow
	{
		get
		{
			var parts = new List<string> { "encrypted-media", "picture-in-picture" };
			if (AutoPlay)
			{
				parts.Insert(0, "autoplay");
			}

			if (AllowFullscreen)
			{
				parts.Add("fullscreen");
			}

			return string.Join("; ", parts);
		}
	}

	// The iframe navigates to this URL, so a javascript: URL would run script in the page's origin.
	// Only web URLs go there; a protocol-relative one takes the page's scheme, which is http or https.
	private static bool IsWebUrl(string url)
	{
		string absolute = url.StartsWith("//", StringComparison.Ordinal) ? "https:" + url : url;
		return Uri.TryCreate(absolute, UriKind.Absolute, out Uri? uri)
		       && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
	}

	private static string ConvertToEmbedUrl(string url)
	{
		// YouTube: youtube.com/watch?v=ID or youtu.be/ID
		Match ytMatch = Regex.Match(url, @"(?:youtube\.com/watch\?v=|youtu\.be/)([\w-]+)");
		if (ytMatch.Success)
		{
			return $"https://www.youtube.com/embed/{ytMatch.Groups[1].Value}";
		}

		// Vimeo: vimeo.com/ID
		Match vimeoMatch = Regex.Match(url, @"vimeo\.com/(\d+)");
		if (vimeoMatch.Success)
		{
			return $"https://player.vimeo.com/video/{vimeoMatch.Groups[1].Value}";
		}

		// Already an embed URL or direct URL
		return url;
	}
}
