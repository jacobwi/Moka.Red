using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Theming;

namespace Moka.Red.ThemeGen.Tests.Components;

// The ThemeGen buttons had no type, so inside a form every tab, preset card and Copy button submitted
// it. No field had an accessible name: the labels were plain spans, or labels without a "for".
public class ThemeGenAccessibilityTests : BunitContext
{
	public static TheoryData<string> Tabs => ["Palette", "Typography", "Spacing", "Presets", "Import/Export"];

	[Theory]
	[MemberData(nameof(Tabs))]
	public void EveryButton_IsAPlainButton(string tab)
	{
		IRenderedComponent<MokaThemeEditor> cut = RenderOnTab(tab);

		IReadOnlyList<IElement> buttons = cut.FindAll("button");
		Assert.NotEmpty(buttons);
		Assert.All(buttons, button => Assert.Equal("button", button.GetAttribute("type")));
	}

	[Theory]
	[MemberData(nameof(Tabs))]
	public void EveryField_HasItsOwnAccessibleName(string tab)
	{
		IRenderedComponent<MokaThemeEditor> cut = RenderOnTab(tab);

		string[] names = cut.FindAll("input, select, textarea").Select(field => AccessibleName(cut, field)).ToArray();

		Assert.NotEmpty(names);
		Assert.All(names, name => Assert.False(string.IsNullOrWhiteSpace(name)));
		Assert.Equal(names.Length, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
	}

	private IRenderedComponent<MokaThemeEditor> RenderOnTab(string tab)
	{
		IRenderedComponent<MokaThemeEditor> cut = Render<MokaThemeEditor>(p => p.Add(x => x.Theme, MokaTheme.Light));
		cut.FindAll(".moka-theme-editor__tab").Single(button => button.TextContent.Trim() == tab).Click();
		return cut;
	}

	// The parts of the accessible name computation these components use.
	private static string AccessibleName(IRenderedComponent<MokaThemeEditor> cut, IElement field)
	{
		if (field.GetAttribute("aria-label") is { Length: > 0 } label)
		{
			return label;
		}

		if (field.GetAttribute("aria-labelledby") is { Length: > 0 } labelledBy)
		{
			return string.Join(" ", labelledBy.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.Select(id => cut.FindAll($"[id='{id}']").SingleOrDefault()?.TextContent.Trim()));
		}

		if (field.Id is { Length: > 0 } fieldId &&
		    cut.FindAll($"label[for='{fieldId}']").SingleOrDefault() is { } forLabel)
		{
			return forLabel.TextContent.Trim();
		}

		return field.Closest("label")?.TextContent.Trim() ?? "";
	}
}
