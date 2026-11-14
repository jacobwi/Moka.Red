using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Code;
using Moka.Red.Core.Theming;
using Moka.Red.Primitives.CodeBlock;
using Moka.Red.ThemeGen.ImportExport;
using Moka.Red.ThemeGen.Serialization;

namespace Moka.Red.ThemeGen.Tests.Components;

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
	[InlineData("   ")]
	public async Task Import_ShowsAnErrorForBadJson_InsteadOfThrowing(string json)
	{
		bool imported = false;
		IRenderedComponent<MokaThemeImportExport> cut = Render<MokaThemeImportExport>(p => p
			.Add(x => x.OnImport, EventCallback.Factory.Create<MokaTheme>(this, _ => imported = true)));

		await ImportAsync(cut, json);

		Assert.False(imported);
		Assert.False(string.IsNullOrWhiteSpace(cut.Find(".moka-field-helper--error").TextContent));
	}

	[Fact]
	public async Task ExportedJson_ImportsBackToTheSameTheme()
	{
		MokaTheme theme = MokaTheme.Light.WithAccent("#0277bd").WithDensity(0.75);
		MokaTheme? imported = null;
		IRenderedComponent<MokaThemeImportExport> cut = Render<MokaThemeImportExport>(p => p
			.Add(x => x.Theme, theme)
			.Add(x => x.Format, MokaThemeExportFormat.Json)
			.Add(x => x.OnImport, EventCallback.Factory.Create<MokaTheme>(this, t => imported = t)));

		await ImportAsync(cut, cut.FindComponent<MokaCodeBlock>().Instance.Code);

		Assert.Equal(theme, imported);
	}

	[Theory]
	[InlineData(MokaThemeExportFormat.CSharp, "csharp")]
	[InlineData(MokaThemeExportFormat.Json, "json")]
	[InlineData(MokaThemeExportFormat.Css, "css")]
	public void EachFormat_IsAHighlightedCodeBlockWithACopyButton(MokaThemeExportFormat format, string language)
	{
		MokaTheme theme = MokaTheme.Gate.WithCorners(MokaCorners.Tight);
		IRenderedComponent<MokaThemeImportExport> cut = Render<MokaThemeImportExport>(p => p.Add(x => x.Theme, theme));

		MokaThemeImportExportText words = new();
		cut.Segment(format switch
		{
			MokaThemeExportFormat.Json => words.Json,
			MokaThemeExportFormat.Css => words.Css,
			_ => words.CSharp
		}).Change(new ChangeEventArgs { Value = "on" });

		MokaCodeBlock block = cut.FindComponent<MokaCodeBlock>().Instance;
		string expected = format switch
		{
			MokaThemeExportFormat.Json => MokaThemeSerializer.ToJson(theme),
			MokaThemeExportFormat.Css => MokaThemeSerializer.ToCss(theme),
			_ => MokaThemeSerializer.ToCSharp(theme)
		};
		Assert.Equal(expected, block.Code);
		Assert.Equal(language, block.Language);
		Assert.Same(MokaCodeHighlighters.For(language), block.Highlighter);
		Assert.True(block.ShowCopyButton);
		Assert.NotEmpty(cut.FindAll("[data-moka-copy-from]"));
	}

	[Fact]
	public void TheOutput_IsWrittenOnlyWhenTheThemeOrTheFormatChanges()
	{
		MokaTheme theme = MokaTheme.Red;
		IRenderedComponent<MokaThemeImportExport> cut = Render<MokaThemeImportExport>(p => p.Add(x => x.Theme, theme));
		string first = cut.FindComponent<MokaCodeBlock>().Instance.Code;

		cut.Render(p => p.Add(x => x.Theme, theme));

		Assert.Same(first, cut.FindComponent<MokaCodeBlock>().Instance.Code);
	}

	[Fact]
	public void Words_ComeFromTheText()
	{
		IRenderedComponent<MokaThemeImportExport> cut = Render<MokaThemeImportExport>(p => p
			.Add(x => x.Text, new MokaThemeImportExportText { ExportHeading = "Exporter", Apply = "Appliquer" }));

		Assert.Contains("Exporter", cut.Markup, StringComparison.Ordinal);
		Assert.Contains("Appliquer", cut.Markup, StringComparison.Ordinal);
	}

	private static async Task ImportAsync(IRenderedComponent<MokaThemeImportExport> cut, string json)
	{
		await cut.Find("textarea").InputAsync(new ChangeEventArgs { Value = json });
		IElement apply = cut.FindAll("button").Last(button => button.TextContent.Trim() == new MokaThemeImportExportText().Apply);
		await apply.ClickAsync(new MouseEventArgs());
	}
}
