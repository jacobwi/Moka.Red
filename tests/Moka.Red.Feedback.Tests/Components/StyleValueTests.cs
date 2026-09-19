using System.Globalization;
using Bunit;
using Moka.Red.Feedback.Loading;
using Moka.Red.Feedback.Progress;

namespace Moka.Red.Feedback.Tests.Components;

// These components built style strings by hand, outside StyleBuilder.
public class StyleValueTests : BunitContext
{
	// SizeValue went into "width: ...; height: ..." as it was, so a semicolon added declarations.
	[Theory]
	[InlineData("20px; position: fixed; inset: 0; z-index: 9999")]
	[InlineData("20px\"")]
	public void Spinner_KeepsSizeValueInItsDeclarations(string size)
	{
		IRenderedComponent<MokaSpinner> cut = Render<MokaSpinner>(p => p
			.Add(x => x.SpinnerStyle, MokaSpinnerStyle.Ring)
			.Add(x => x.SizeValue, size));

		Assert.Null(cut.Find(".moka-spinner-animation").GetAttribute("style"));
		Assert.Null(cut.Find(".moka-spinner-ring").GetAttribute("style"));
	}

	[Fact]
	public void Spinner_AppliesSizeValue()
	{
		IRenderedComponent<MokaSpinner> cut = Render<MokaSpinner>(p => p
			.Add(x => x.SpinnerStyle, MokaSpinnerStyle.Bars)
			.Add(x => x.SizeValue, "32px"));

		Assert.Equal("width: 32px; height: 32px", cut.Find(".moka-spinner-animation").GetAttribute("style"));
		Assert.Equal("height: 32px", cut.Find(".moka-spinner-bar").GetAttribute("style"));
	}

	// The bar width went through the current culture, and "45,5%" is not CSS: under a culture with a
	// decimal comma the bar lost its width.
	[Fact]
	public void Progress_WritesTheBarWidthWithADecimalPointUnderAnyCulture()
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("de-DE");
		try
		{
			IRenderedComponent<MokaProgress> cut = Render<MokaProgress>(p => p.Add(x => x.Value, 45.5));

			Assert.Equal("width: 45.5%", cut.Find(".moka-progress-bar").GetAttribute("style"));
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}
}
