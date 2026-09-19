using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Theming;
using Moka.Red.ThemeGen.ImportExport;

namespace Moka.Red.ThemeGen.Tests.Components;

// Importing {"palette": null} threw a NullReferenceException out of the Apply click, and a theme
// exported as JSON did not import back to the same theme.
public class MokaThemeImportExportTests : BunitContext
{
	public MokaThemeImportExportTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	[Theory]
	[InlineData("{\"palette\": null}")]
	[InlineData("{\"typography\": null}")]
	[InlineData("{\"spacing\": {\"md\": null}}")]
	public async Task Import_ShowsAnErrorForBadJson_InsteadOfThrowing(string json)
	{
		bool imported = false;
		IRenderedComponent<MokaThemeImportExport> cut = Render<MokaThemeImportExport>(p => p
			.Add(x => x.OnImport, EventCallback.Factory.Create<MokaTheme>(this, _ => imported = true)));

		await ImportAsync(cut, json);

		Assert.False(imported);
		Assert.False(string.IsNullOrWhiteSpace(cut.Find(".moka-theme-import-export__error").TextContent));
	}

	[Fact]
	public async Task ExportedJson_ImportsBackToTheSameTheme()
	{
		MokaTheme theme = MokaTheme.Light.WithAccent("#0277bd").WithDensity(0.75);
		MokaTheme? imported = null;
		IRenderedComponent<MokaThemeImportExport> cut = Render<MokaThemeImportExport>(p => p
			.Add(x => x.Theme, theme)
			.Add(x => x.OnImport, EventCallback.Factory.Create<MokaTheme>(this, t => imported = t)));

		IElement output = cut.Find(".moka-theme-import-export__output");
		await ImportAsync(cut, output.GetAttribute("value") ?? output.TextContent);

		Assert.Equal(theme, imported);
	}

	private static async Task ImportAsync(IRenderedComponent<MokaThemeImportExport> cut, string json)
	{
		await cut.Find(".moka-theme-import-export__input").ChangeAsync(new ChangeEventArgs { Value = json });
		await cut.FindAll(".moka-theme-import-export__action-btn")[^1].ClickAsync(new MouseEventArgs());
	}
}
