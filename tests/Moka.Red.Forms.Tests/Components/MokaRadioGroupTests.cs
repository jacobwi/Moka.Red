using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Forms.RadioGroup;

namespace Moka.Red.Forms.Tests.Components;

// The native radios are the real controls: the browser gives them the tab stop, the arrow keys and
// the checked state, as long as they share a name. These tests cover the parts Blazor owns.
public class MokaRadioGroupTests : BunitContext
{
	public MokaRadioGroupTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task Selecting_MovesTheSelectionOffThePreviousItem()
	{
		string? chosen = null;
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderGroup("a", value => chosen = value);

		await cut.FindAll("input[type=radio]")[1].ChangeAsync(new ChangeEventArgs { Value = "on" });

		Assert.Equal("b", chosen);
		Assert.Equal([false, true, false], cut.FindAll("input[type=radio]").Select(r => r.HasAttribute("checked")));
		IElement dot = Assert.Single(cut.FindAll(".moka-radio-dot"));
		Assert.Equal("B", dot.Closest(".moka-radio-item")?.TextContent.Trim());
	}

	// The group used to cascade itself with IsFixed, so items with only simple parameters never redrew.
	[Fact]
	public void ChangingTheValueFromOutside_RedrawsTheItems()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderGroup("a", _ => { });

		cut.Render(p => p.Add(x => x.Value, "c"));

		Assert.Equal([false, false, true], cut.FindAll("input[type=radio]").Select(r => r.HasAttribute("checked")));
		Assert.Equal([false, false, true], cut.FindAll(".moka-radio-item").Select(i => i.ClassList.Contains("moka-radio-item--selected")));
	}

	[Fact]
	public void Radios_ShareOneName_AndCanTakeFocus()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderGroup("a", _ => { });

		IReadOnlyList<IElement> radios = cut.FindAll("input[type=radio]");
		string? name = radios[0].GetAttribute("name");
		Assert.False(string.IsNullOrEmpty(name));
		Assert.All(radios, radio => Assert.Equal(name, radio.GetAttribute("name")));
		Assert.All(radios, radio => Assert.False(radio.HasAttribute("tabindex")));
	}

	[Fact]
	public void TwoGroups_DoNotShareAName()
	{
		string? first = RenderGroup("a", _ => { }).Find("input[type=radio]").GetAttribute("name");
		string? second = RenderGroup("a", _ => { }).Find("input[type=radio]").GetAttribute("name");

		Assert.NotEqual(first, second);
	}

	// aria-checked used to render as an empty value when selected and not at all otherwise.
	[Fact]
	public void CheckedState_ComesFromTheNativeRadio()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderGroup("b", _ => { });

		Assert.Empty(cut.FindAll("[aria-checked]"));
		Assert.Empty(cut.FindAll("[role=radio]"));
		Assert.True(cut.FindAll("input[type=radio]")[1].HasAttribute("checked"));
	}

	// A click on the label reaches the input once. A handler on the row as well selected twice.
	[Fact]
	public void TheRow_IsALabelWithNoClickHandlerOfItsOwn()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderGroup("a", _ => { });

		Assert.All(cut.FindAll(".moka-radio-item"), item =>
		{
			Assert.Equal("LABEL", item.TagName);
			Assert.False(item.HasAttribute("blazor:onclick"));
		});
	}

	[Fact]
	public async Task DisabledItem_IsADisabledRadioAndIgnoresChange()
	{
		string? chosen = null;
		IRenderedComponent<MokaRadioGroup<string>> cut = Render<MokaRadioGroup<string>>(p => p
			.Add(x => x.Value, "a")
			.Add(x => x.ValueChanged, value => chosen = value)
			.AddChildContent<MokaRadioItem<string>>(item => item.Add(x => x.Value, "a").Add(x => x.Label, "A"))
			.AddChildContent<MokaRadioItem<string>>(item => item.Add(x => x.Value, "b").Add(x => x.Label, "B").Add(x => x.Disabled, true)));

		IElement disabled = cut.FindAll("input[type=radio]")[1];
		await disabled.ChangeAsync(new ChangeEventArgs { Value = "on" });

		Assert.True(disabled.HasAttribute("disabled"));
		Assert.Null(chosen);
	}

	[Fact]
	public void Cards_DrawThePictureNameAndHintInThatOrder()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderCards("b");

		IElement card = cut.FindAll(".moka-radio-item")[0];
		Assert.Contains("moka-radio-item--card", card.ClassList);
		Assert.Equal(
			["moka-radio-item__art", "moka-radio-item__head", "moka-radio-item__hint"],
			card.Children.Where(c => c.TagName == "SPAN").Select(c => c.ClassName));
		Assert.Equal("Light", card.QuerySelector(".moka-radio-item__name")?.TextContent.Trim());
		Assert.Equal("Bright", card.QuerySelector(".moka-radio-item__hint")?.TextContent);
		Assert.NotNull(card.QuerySelector(".moka-radio-item__art .pic"));
	}

	[Fact]
	public void Cards_HideThePictureFromScreenReaders()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderCards("b");

		Assert.All(cut.FindAll(".moka-radio-item__art"), art => Assert.Equal("true", art.GetAttribute("aria-hidden")));
	}

	[Fact]
	public void Cards_DrawNoRadioCircle_AndCheckOnlyTheChosenOne()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderCards("b");

		Assert.Empty(cut.FindAll(".moka-radio-circle"));
		IElement check = Assert.Single(cut.FindAll(".moka-radio-item__head svg"));
		Assert.Contains("moka-radio-item--selected", check.Closest(".moka-radio-item")!.ClassList);
		Assert.Equal("Dark", check.Closest(".moka-radio-item")!.QuerySelector(".moka-radio-item__name")?.TextContent.Trim());
	}

	[Fact]
	public async Task Cards_AreStillOneGroupOfNativeRadios()
	{
		string? chosen = null;
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderCards("b", changed: value => chosen = value);

		IReadOnlyList<IElement> radios = cut.FindAll("input[type=radio]");
		Assert.Equal(2, radios.Count);
		Assert.Single(radios.Select(r => r.GetAttribute("name")).Distinct());

		await radios[0].ChangeAsync(new ChangeEventArgs { Value = "on" });

		Assert.Equal("a", chosen);
		Assert.Equal([true, false], cut.FindAll("input[type=radio]").Select(r => r.HasAttribute("checked")));
	}

	[Fact]
	public void Cards_LetChildContentReplaceTheName()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = Render<MokaRadioGroup<string>>(p => p
			.Add(x => x.Cards, true)
			.AddChildContent<MokaRadioItem<string>>(item => item
				.Add(x => x.Value, "a")
				.Add(x => x.Label, "Ignored")
				.AddChildContent("<b>Custom</b>")));

		Assert.Equal("Custom", cut.Find(".moka-radio-item__name b").TextContent);
		Assert.DoesNotContain("Ignored", cut.Find(".moka-radio-item__name").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void Cards_MarkADisabledCardAndItsRadio()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = Render<MokaRadioGroup<string>>(p => p
			.Add(x => x.Cards, true)
			.AddChildContent<MokaRadioItem<string>>(item => item.Add(x => x.Value, "a").Add(x => x.Label, "A").Add(x => x.Disabled, true)));

		Assert.Contains("moka-radio-item--disabled", cut.Find(".moka-radio-item").ClassList);
		Assert.True(cut.Find("input[type=radio]").HasAttribute("disabled"));
	}

	[Theory]
	[InlineData(null, "10rem")]
	[InlineData("14rem", "14rem")]
	[InlineData("200", "200px")]
	[InlineData("var(--card-min)", "var(--card-min)")]
	[InlineData("10rem; color: red", "10rem")]
	public void Cards_WrapAtMinColumn(string? minColumn, string expected)
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderCards("b", minColumn);

		string? style = cut.Find("[role=radiogroup]").GetAttribute("style");
		Assert.Contains($"repeat(auto-fill, minmax(min(100%, {expected}), 1fr))", style, StringComparison.Ordinal);
		Assert.DoesNotContain("color: red", style, StringComparison.Ordinal);
	}

	[Fact]
	public void Cards_IgnoreARowOrientation()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = Render<MokaRadioGroup<string>>(p => p
			.Add(x => x.Cards, true)
			.Add(x => x.Orientation, MokaDirection.Row));

		IElement group = cut.Find("[role=radiogroup]");
		Assert.Contains("moka-radiogroup--cards", group.ClassList);
		Assert.DoesNotContain("moka-radiogroup--row", group.ClassList);
	}

	[Fact]
	public void WithoutCards_AnItemKeepsItsCircle_AndDrawsNoPictureOrHint()
	{
		IRenderedComponent<MokaRadioGroup<string>> cut = RenderCards("b", cards: false);

		IElement group = cut.Find("[role=radiogroup]");
		Assert.DoesNotContain("moka-radiogroup--cards", group.ClassList);
		Assert.DoesNotContain("grid-template-columns", group.GetAttribute("style") ?? "", StringComparison.Ordinal);
		Assert.Equal(2, cut.FindAll(".moka-radio-circle").Count);
		Assert.Empty(cut.FindAll(".moka-radio-item__art, .moka-radio-item__hint, .moka-radio-item--card"));
	}

	private IRenderedComponent<MokaRadioGroup<string>> RenderCards(string value, string? minColumn = null, bool cards = true, Action<string>? changed = null) =>
		Render<MokaRadioGroup<string>>(p => p
			.Add(x => x.Label, "Color mode")
			.Add(x => x.Cards, cards)
			.Add(x => x.MinColumn, minColumn)
			.Add(x => x.Value, value)
			.Add(x => x.ValueChanged, changed ?? (_ => { }))
			.AddChildContent<MokaRadioItem<string>>(item => item
				.Add(x => x.Value, "a")
				.Add(x => x.Label, "Light")
				.Add(x => x.Hint, "Bright")
				.Add(x => x.Preview, (RenderFragment)(b => b.AddMarkupContent(0, "<i class=\"pic\"></i>"))))
			.AddChildContent<MokaRadioItem<string>>(item => item
				.Add(x => x.Value, "b")
				.Add(x => x.Label, "Dark")
				.Add(x => x.Preview, (RenderFragment)(b => b.AddMarkupContent(0, "<i class=\"pic\"></i>")))));

	private IRenderedComponent<MokaRadioGroup<string>> RenderGroup(string value, Action<string> changed) =>
		Render<MokaRadioGroup<string>>(p => p
			.Add(x => x.Label, "Plan")
			.Add(x => x.Value, value)
			.Add(x => x.ValueChanged, changed)
			.AddChildContent<MokaRadioItem<string>>(item => item.Add(x => x.Value, "a").Add(x => x.Label, "A"))
			.AddChildContent<MokaRadioItem<string>>(item => item.Add(x => x.Value, "b").Add(x => x.Label, "B"))
			.AddChildContent<MokaRadioItem<string>>(item => item.Add(x => x.Value, "c").Add(x => x.Label, "C")));
}
