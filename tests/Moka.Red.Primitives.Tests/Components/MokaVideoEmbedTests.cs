using Bunit;
using Moka.Red.Primitives.Media;

namespace Moka.Red.Primitives.Tests.Components;

// Any Src that mentioned youtube.com or vimeo.com went into the iframe, and one the embed rewrite did
// not recognise went in as it was. javascript:alert(1)//youtube.com ran script in the page's origin as
// soon as the component rendered.
public class MokaVideoEmbedTests : BunitContext
{
	[Theory]
	[InlineData("javascript:alert(document.domain)//youtube.com")]
	[InlineData("JavaScript:alert(1)//vimeo.com")]
	[InlineData(" javascript:alert(1)//youtube.com")]
	[InlineData("data:text/html,<script>alert(1)</script>youtube.com")]
	[InlineData("vbscript:msgbox(1)//youtu.be")]
	public void AScriptUrl_IsNeverLoadedInAFrame(string src)
	{
		IRenderedComponent<MokaVideoEmbed> cut = Render<MokaVideoEmbed>(p => p.Add(x => x.Src, src));

		Assert.Empty(cut.FindAll("iframe"));
	}

	[Theory]
	[InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "https://www.youtube.com/embed/dQw4w9WgXcQ")]
	[InlineData("https://youtu.be/dQw4w9WgXcQ", "https://www.youtube.com/embed/dQw4w9WgXcQ")]
	[InlineData("https://vimeo.com/123456789", "https://player.vimeo.com/video/123456789")]
	[InlineData("https://player.vimeo.com/video/123456789", "https://player.vimeo.com/video/123456789")]
	[InlineData("//player.vimeo.com/video/123456789", "//player.vimeo.com/video/123456789")]
	[InlineData("http://www.youtube.com/embed/dQw4w9WgXcQ", "http://www.youtube.com/embed/dQw4w9WgXcQ")]
	public void AWebVideoUrl_IsEmbedded(string src, string expected)
	{
		IRenderedComponent<MokaVideoEmbed> cut = Render<MokaVideoEmbed>(p => p.Add(x => x.Src, src));

		Assert.Equal(expected, cut.Find("iframe").GetAttribute("src"));
	}

	[Fact]
	public void AutoPlay_IsAddedToTheEmbedUrl()
	{
		IRenderedComponent<MokaVideoEmbed> cut = Render<MokaVideoEmbed>(p => p
			.Add(x => x.Src, "https://youtu.be/dQw4w9WgXcQ")
			.Add(x => x.AutoPlay, true));

		Assert.Equal("https://www.youtube.com/embed/dQw4w9WgXcQ?autoplay=1", cut.Find("iframe").GetAttribute("src"));
	}

	[Fact]
	public void AFileUrl_PlaysInAVideoElement()
	{
		IRenderedComponent<MokaVideoEmbed> cut = Render<MokaVideoEmbed>(p => p
			.Add(x => x.Src, "https://example.com/media/intro.mp4"));

		Assert.Empty(cut.FindAll("iframe"));
		Assert.Equal("https://example.com/media/intro.mp4", cut.Find("video").GetAttribute("src"));
	}
}
