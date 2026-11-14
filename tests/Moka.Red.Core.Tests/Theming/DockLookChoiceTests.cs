using Moka.Red.Core.Theming;

namespace Moka.Red.Core.Tests.Theming;

// WithDocks is a style choice like the other component looks: it reaches the new design only, and the original
// design keeps drawing dock layouts as it always has.
public class DockLookChoiceTests
{
	[Fact]
	public void Docks_AreClassic_UntilAnAppSaysOtherwise()
	{
		Assert.Equal(MokaDockLook.Classic, MokaTheme.Default.Docks);
		Assert.Equal(MokaDockLook.Classic, MokaTheme.Gate.Docks);
	}

	[Theory]
	[InlineData(MokaDockLook.Ide)]
	[InlineData(MokaDockLook.Cards)]
	[InlineData(MokaDockLook.Minimal)]
	[InlineData(MokaDockLook.Terminal)]
	public void WithDocks_SetsTheLook_OnTheNewDesign(MokaDockLook look)
	{
		MokaTheme theme = MokaTheme.Default.WithDocks(look);

		Assert.Equal(look, theme.Docks);
		Assert.Equal(MokaDockLook.Classic, MokaTheme.Default.Docks);
	}

	[Fact]
	public void WithDocks_DoesNothing_OnTheOriginalDesign()
	{
		Assert.Same(MokaTheme.Dark, MokaTheme.Dark.WithDocks(MokaDockLook.Ide));
		Assert.Same(MokaTheme.Light, MokaTheme.Light.WithDocks(MokaDockLook.Cards));
	}

	[Fact]
	public void TheChoice_SurvivesOtherChoices_AndWritesNoToken()
	{
		MokaTheme theme = MokaTheme.Default.WithDocks(MokaDockLook.Terminal).WithDensity(MokaDensity.Compact).WithAccent(MokaAccent.Violet);

		Assert.Equal(MokaDockLook.Terminal, theme.Docks);
		Assert.Equal(MokaTheme.Default.ToCssVariables(), MokaTheme.Default.WithDocks(MokaDockLook.Ide).ToCssVariables());
	}
}
