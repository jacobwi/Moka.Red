using Moka.Red.Core.Theming;

namespace Moka.Red.DevApp.Components.Layout;

// One set of choices behind every LookPanel, so the page bar's and the phone's agree when the window crosses 720 px.
public sealed class LookChoices
{
	public string Colors { get; set; } = "red";

	public bool IsDark { get; set; } = true;

	public MokaDensity Density { get; set; } = MokaDensity.Comfortable;

	public MokaTextSize TextSize { get; set; } = MokaTextSize.Normal;

	public MokaCorners Corners { get; set; } = MokaCorners.Soft;

	public MokaMainButton MainButton { get; set; } = MokaMainButton.Solid;

	public MokaLetterCase LetterCase { get; set; } = MokaLetterCase.Sentence;

	public MokaTitleFont TitleFont { get; set; } = MokaTitleFont.SpaceGrotesk;

	public MokaSelection Selection { get; set; } = MokaSelection.Ring;

	public MokaCurrentPlace CurrentPlace { get; set; } = MokaCurrentPlace.Ring;

	public MokaStatusBarLook StatusBar { get; set; } = MokaStatusBarLook.Standard;

	public bool Descriptions { get; set; } = true;

	public bool IsOriginal => Colors == "original";

	// Crisp has Gate's colors. What makes it Crisp is these choices, so it sets them, and they stay for whichever
	// colors come next.
	public void PickColors(string colors)
	{
		Colors = colors;
		if (colors != "crisp")
		{
			return;
		}

		Density = MokaDensity.Compact;
		TextSize = MokaTextSize.Small;
		Corners = MokaCorners.Tight;
		TitleFont = MokaTitleFont.Inter;
		Selection = MokaSelection.Flat;
	}

	public MokaTheme Build()
	{
		if (IsOriginal)
		{
			return IsDark ? MokaTheme.Dark : MokaTheme.Light;
		}

		MokaTheme colors = Colors switch
		{
			"gate" or "crisp" => IsDark ? MokaTheme.Gate : MokaTheme.GateLight,
			"matrix" => IsDark ? MokaTheme.Matrix : MokaTheme.MatrixLight,
			_ => IsDark ? MokaTheme.Default : MokaTheme.DefaultLight
		};

		return colors
			.WithDensity(Density)
			.WithTextSize(TextSize)
			.WithCorners(Corners)
			.WithMainButton(MainButton)
			.WithLetterCase(LetterCase)
			.WithTitleFont(TitleFont)
			.WithSelection(Selection)
			.WithCurrentPlace(CurrentPlace)
			.WithStatusBar(StatusBar)
			.WithDescriptions(Descriptions);
	}
}
