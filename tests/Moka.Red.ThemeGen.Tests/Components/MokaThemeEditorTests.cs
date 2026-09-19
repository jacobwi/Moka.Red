using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Theming;
using Moka.Red.ThemeGen;

namespace Moka.Red.ThemeGen.Tests.Components;

// The editor used to write each edit into its own Theme parameter. Blazor passes every parameter
// again whenever the parent renders, so with a one-way Theme the parent's next render threw the
// user's edits away. MokaTheme.Light builds a new theme on every access, so an inline
// Theme="MokaTheme.Light" did that on every render.
public class MokaThemeEditorTests : BunitContext
{
	[Fact]
	public async Task KeepsAnEdit_WhenTheParentPassesAnEqualTheme()
	{
		IRenderedComponent<MokaThemeEditor> cut = Render<MokaThemeEditor>(p => p.Add(x => x.Theme, MokaTheme.Light));

		await PrimaryHexInput(cut).ChangeAsync(new ChangeEventArgs { Value = "#123456" });
		cut.Render(p => p.Add(x => x.Theme, MokaTheme.Light));

		Assert.Equal("#123456", PrimaryHexInput(cut).GetAttribute("value"));
	}

	[Fact]
	public async Task FollowsADifferentThemeFromTheParent()
	{
		IRenderedComponent<MokaThemeEditor> cut = Render<MokaThemeEditor>(p => p.Add(x => x.Theme, MokaTheme.Light));

		await PrimaryHexInput(cut).ChangeAsync(new ChangeEventArgs { Value = "#123456" });
		cut.Render(p => p.Add(x => x.Theme, MokaTheme.Dark));

		Assert.Equal(MokaTheme.Dark.Palette.Primary, PrimaryHexInput(cut).GetAttribute("value"));
	}

	[Fact]
	public async Task ReportsTheEditedTheme()
	{
		MokaTheme? reported = null;
		IRenderedComponent<MokaThemeEditor> cut = Render<MokaThemeEditor>(p => p
			.Add(x => x.Theme, MokaTheme.Light)
			.Add(x => x.ThemeChanged, EventCallback.Factory.Create<MokaTheme>(this, t => reported = t)));

		await PrimaryHexInput(cut).ChangeAsync(new ChangeEventArgs { Value = "#123456" });

		Assert.NotNull(reported);
		Assert.Equal("#123456", reported.Palette.Primary);
		Assert.Equal(MokaTheme.Light.Palette.Secondary, reported.Palette.Secondary);
	}

	// The first hex field is Primary.
	private static IElement PrimaryHexInput(IRenderedComponent<MokaThemeEditor> cut) =>
		cut.FindAll(".moka-palette-editor__hex-input")[0];
}
