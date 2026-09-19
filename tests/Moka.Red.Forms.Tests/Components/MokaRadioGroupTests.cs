using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
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

	private IRenderedComponent<MokaRadioGroup<string>> RenderGroup(string value, Action<string> changed) =>
		Render<MokaRadioGroup<string>>(p => p
			.Add(x => x.Label, "Plan")
			.Add(x => x.Value, value)
			.Add(x => x.ValueChanged, changed)
			.AddChildContent<MokaRadioItem<string>>(item => item.Add(x => x.Value, "a").Add(x => x.Label, "A"))
			.AddChildContent<MokaRadioItem<string>>(item => item.Add(x => x.Value, "b").Add(x => x.Label, "B"))
			.AddChildContent<MokaRadioItem<string>>(item => item.Add(x => x.Value, "c").Add(x => x.Label, "C")));
}
