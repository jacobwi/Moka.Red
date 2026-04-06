using Moka.Red.Diagnostics.Services;

namespace Moka.Red.Diagnostics.Tests.Services;

public class DiagnosticsOptionsTests
{
	[Fact]
	public void DefaultKeyboardShortcut_IsCtrlShiftD()
	{
		var options = new DiagnosticsOptions();

		Assert.Equal("Ctrl+Shift+D", options.KeyboardShortcut);
	}

	[Fact]
	public void DefaultPosition_IsBottomRight()
	{
		var options = new DiagnosticsOptions();

		Assert.Equal(OverlayPosition.BottomRight, options.Position);
	}

	[Fact]
	public void DefaultStartExpanded_IsFalse()
	{
		var options = new DiagnosticsOptions();

		Assert.False(options.StartExpanded);
	}

	[Fact]
	public void CanSetCustomValues()
	{
		var options = new DiagnosticsOptions
		{
			KeyboardShortcut = "Ctrl+D",
			Position = OverlayPosition.TopLeft,
			StartExpanded = true
		};

		Assert.Equal("Ctrl+D", options.KeyboardShortcut);
		Assert.Equal(OverlayPosition.TopLeft, options.Position);
		Assert.True(options.StartExpanded);
	}
}
