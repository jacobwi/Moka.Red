using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.Flag;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaFlagTests : BunitContext
{
	public MokaFlagTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Theory]
	[InlineData("SE", "\U0001F1F8\U0001F1EA")]
	[InlineData("se", "\U0001F1F8\U0001F1EA")]
	[InlineData(" us ", "\U0001F1FA\U0001F1F8")]
	[InlineData("EU", "\U0001F1EA\U0001F1FA")]
	public void ToEmoji_ACountryCode_IsItsRegionalIndicatorPair(string code, string emoji) =>
		Assert.Equal(emoji, MokaFlag.ToEmoji(code));

	[Theory]
	[InlineData("GB-ENG", "\U0001F3F4\U000E0067\U000E0062\U000E0065\U000E006E\U000E0067\U000E007F")]
	[InlineData("gb_sct", "\U0001F3F4\U000E0067\U000E0062\U000E0073\U000E0063\U000E0074\U000E007F")]
	[InlineData("GBWLS", "\U0001F3F4\U000E0067\U000E0062\U000E0077\U000E006C\U000E0073\U000E007F")]
	public void ToEmoji_EnglandScotlandAndWales_AreTagSequences(string code, string emoji) =>
		Assert.Equal(emoji, MokaFlag.ToEmoji(code));

	// Any other pair of letters would draw as two boxed letters, so it gets the globe instead.
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("  ")]
	[InlineData("ZZ")]
	[InlineData("S")]
	[InlineData("SWE")]
	[InlineData("1A")]
	[InlineData("GB-NIR")]
	public void ToEmoji_ACodeWithNoFlag_IsNull(string? code) => Assert.Null(MokaFlag.ToEmoji(code));

	[Fact]
	public void ACode_DrawsTheFlag_NamedByTheCountry()
	{
		IRenderedComponent<MokaFlag> cut = Render<MokaFlag>(p => p
			.Add(x => x.Code, "se")
			.Add(x => x.Name, "Sweden"));

		IElement flag = cut.Find(".moka-flag");
		Assert.Equal("img", flag.GetAttribute("role"));
		Assert.Equal("Sweden", flag.GetAttribute("aria-label"));
		Assert.Equal("\U0001F1F8\U0001F1EA", flag.TextContent.Trim());
		Assert.Empty(cut.FindAll("svg"));
		Assert.Null(flag.GetAttribute("title"));
	}

	[Fact]
	public void WithoutAName_TheCodeNamesIt()
	{
		IRenderedComponent<MokaFlag> cut = Render<MokaFlag>(p => p.Add(x => x.Code, "de"));

		Assert.Equal("DE", cut.Find(".moka-flag").GetAttribute("aria-label"));
	}

	[Fact]
	public void NoCode_DrawsAGlobe_NamedUnknown()
	{
		IRenderedComponent<MokaFlag> cut = Render<MokaFlag>();

		IElement flag = cut.Find(".moka-flag");
		Assert.Contains("moka-flag--unknown", flag.ClassList);
		Assert.Single(cut.FindAll("svg"));
		Assert.Equal("Unknown country", flag.GetAttribute("aria-label"));

		cut.Render(p => p.Add(x => x.UnknownName, "Somewhere"));
		Assert.Equal("Somewhere", cut.Find(".moka-flag").GetAttribute("aria-label"));
	}

	[Fact]
	public void Size_ScalesWithTheText_AndSizeValueSetsIt()
	{
		IRenderedComponent<MokaFlag> cut = Render<MokaFlag>(p => p
			.Add(x => x.Code, "SE")
			.Add(x => x.Size, MokaSize.Lg));

		Assert.Contains("moka-flag--lg", cut.Find(".moka-flag").ClassList);

		cut.Render(p => p.Add(x => x.SizeValue, "32"));
		IElement flag = cut.Find(".moka-flag");
		Assert.DoesNotContain("moka-flag--lg", flag.ClassList);
		Assert.Contains("font-size: 32px", flag.GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void ShowTooltip_PutsTheNameInATooltip()
	{
		IRenderedComponent<MokaFlag> cut = Render<MokaFlag>(p => p
			.Add(x => x.Code, "SE")
			.Add(x => x.Name, "Sweden")
			.Add(x => x.ShowTooltip, true));

		Assert.Equal("Sweden", cut.Find("[role=tooltip]").TextContent.Trim());
		Assert.NotNull(cut.Find(".moka-tooltip-trigger > .moka-flag"));
	}

	// The 258 regions Unicode draws a flag for, every one of them two letters.
	[Fact]
	public void EveryRegionWithAFlag_Draws()
	{
		int drawn = 0;
		for (char first = 'A'; first <= 'Z'; first++)
		{
			for (char second = 'A'; second <= 'Z'; second++)
			{
				if (MokaFlag.ToEmoji($"{first}{second}") is not null)
				{
					drawn++;
				}
			}
		}

		Assert.Equal(258, drawn);
	}
}
