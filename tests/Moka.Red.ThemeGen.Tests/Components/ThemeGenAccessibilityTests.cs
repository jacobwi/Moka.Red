using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.Extensions;

namespace Moka.Red.ThemeGen.Tests.Components;

// Inside a form a button without a type submits it, and a field without a name is read out as "edit text".
public class ThemeGenAccessibilityTests : BunitContext
{
	public ThemeGenAccessibilityTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaFeedback();
		SetRendererInfo(new RendererInfo("Server", true));
	}

	public static TheoryData<string> Tabs => [new MokaThemeStudioText().ContrastTab, new MokaThemeStudioText().TokensTab, new MokaThemeStudioText().ExportTab];

	[Theory]
	[MemberData(nameof(Tabs))]
	public async Task EveryButton_IsAPlainButton(string tab)
	{
		IRenderedComponent<MokaThemeStudio> cut = await RenderOnTabAsync(tab);

		IReadOnlyList<IElement> buttons = cut.FindAll("button");
		Assert.NotEmpty(buttons);
		Assert.All(buttons, button => Assert.Equal("button", button.GetAttribute("type")));
	}

	[Theory]
	[MemberData(nameof(Tabs))]
	public async Task EveryFieldOfTheStudio_HasAnAccessibleName(string tab)
	{
		IRenderedComponent<MokaThemeStudio> cut = await RenderOnTabAsync(tab);

		IElement[] fields = [.. cut.FindAll(".moka-theme-studio__controls input, .moka-theme-studio__controls textarea, .moka-theme-studio__inspector input, .moka-theme-studio__inspector textarea")
			.Where(field => field.GetAttribute("type") is not ("hidden" or "color"))];

		Assert.NotEmpty(fields);
		Assert.All(fields, field => Assert.False(string.IsNullOrWhiteSpace(AccessibleName(cut, field)), field.OuterHtml));
	}

	// Each column is named by the heading it shows, so what a screen reader hears is what the eye reads.
	[Fact]
	public void TheColumns_AreNamedByTheirHeadings()
	{
		IRenderedComponent<MokaThemeStudio> cut = Render<MokaThemeStudio>();
		MokaThemeStudioText text = new();

		Assert.Equal(text.ControlsLabel, NameOf(cut, "aside.moka-theme-studio__controls"));
		Assert.Equal(text.PreviewHeading, NameOf(cut, "section.moka-theme-studio__stage"));
		Assert.Equal(text.InspectorHeading, NameOf(cut, "aside.moka-theme-studio__inspector"));
		Assert.All(cut.FindAll("section.moka-theme-preview"), preview => Assert.False(string.IsNullOrWhiteSpace(preview.GetAttribute("aria-label"))));
	}

	[Fact]
	public void EachPreview_IsCaptionedWithItsScheme()
	{
		IRenderedComponent<MokaThemeStudio> cut = Render<MokaThemeStudio>();
		MokaThemeStudioText text = new();

		Assert.Equal([text.Dark, text.Light], cut.FindAll("figure.moka-theme-studio__frame > figcaption").Select(caption => caption.TextContent.Trim()));
		Assert.All(cut.FindAll("figure.moka-theme-studio__frame"), frame => Assert.NotNull(frame.QuerySelector(".moka-theme-preview")));
	}

	private static string NameOf(IRenderedComponent<MokaThemeStudio> cut, string selector) =>
		cut.Find("#" + cut.Find(selector).GetAttribute("aria-labelledby")).TextContent.Trim();

	private async Task<IRenderedComponent<MokaThemeStudio>> RenderOnTabAsync(string tab)
	{
		IRenderedComponent<MokaThemeStudio> cut = Render<MokaThemeStudio>();
		await cut.FindAll("[role='tab']").First(element => element.TextContent.Trim() == tab).ClickAsync(new MouseEventArgs());
		return cut;
	}

	// The parts of the accessible name computation these components use.
	private static string AccessibleName(IRenderedComponent<MokaThemeStudio> cut, IElement field)
	{
		if (field.GetAttribute("aria-label") is { Length: > 0 } label)
		{
			return label;
		}

		if (field.GetAttribute("aria-labelledby") is { Length: > 0 } labelledBy)
		{
			return string.Join(" ", labelledBy.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.Select(id => FirstText(cut, $"[id='{id}']")));
		}

		if (field.Id is { Length: > 0 } fieldId && FirstText(cut, $"label[for='{fieldId}']") is { Length: > 0 } forLabel)
		{
			return forLabel;
		}

		return field.Closest("label")?.TextContent.Trim() ?? "";
	}

	private static string FirstText(IRenderedComponent<MokaThemeStudio> cut, string selector) =>
		cut.FindAll(selector) is { Count: > 0 } found ? found[0].TextContent.Trim() : "";
}
