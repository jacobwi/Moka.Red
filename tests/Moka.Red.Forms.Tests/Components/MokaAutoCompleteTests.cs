using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.AutoComplete;

namespace Moka.Red.Forms.Tests.Components;

public class MokaAutoCompleteTests : BunitContext
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	private static readonly string[] Cities = ["Berlin", "Bern", "Bergen"];

	public MokaAutoCompleteTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void Closed_ReportsNotExpanded()
	{
		IRenderedComponent<MokaAutoComplete<string>> cut = RenderCities();

		IElement input = cut.Find("input[role=combobox]");
		Assert.Equal("false", input.GetAttribute("aria-expanded"));
		Assert.False(input.HasAttribute("aria-controls"));
	}

	[Fact]
	public async Task Open_InputControlsALabelledListbox()
	{
		IRenderedComponent<MokaAutoComplete<string>> cut = await RenderOpenAsync();

		IElement input = cut.Find("input[role=combobox]");
		IElement listbox = cut.Find("[role=listbox]");
		Assert.Equal("true", input.GetAttribute("aria-expanded"));
		Assert.Equal(listbox.Id, input.GetAttribute("aria-controls"));
		Assert.Equal(cut.Find("label.moka-field-label").Id, listbox.GetAttribute("aria-labelledby"));
	}

	[Fact]
	public async Task ArrowDown_PointsTheInputAtTheHighlightedSuggestion()
	{
		IRenderedComponent<MokaAutoComplete<string>> cut = await RenderOpenAsync();

		await cut.Find("input[role=combobox]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

		IElement first = cut.FindAll("[role=option]")[0];
		Assert.False(string.IsNullOrEmpty(first.Id));
		Assert.Equal(first.Id, cut.Find("input[role=combobox]").GetAttribute("aria-activedescendant"));
		Assert.Equal("true", first.GetAttribute("aria-selected"));
		Assert.Equal("false", cut.FindAll("[role=option]")[1].GetAttribute("aria-selected"));
	}

	[Fact]
	public void CancelsEnter_OnlyWhileASuggestionIsHighlighted()
	{
		BunitJSModuleInterop keys = JSInterop.SetupModule(KeysModule);

		RenderCities();

		Dictionary<string, object?>[] rules =
			Assert.IsType<Dictionary<string, object?>[]>(keys.VerifyInvoke("preventKeys").Arguments[1]);
		Dictionary<string, object?> enter = Assert.Single(rules, r => ((string[])r["keys"]!).Contains("Enter"));
		Assert.Equal(".moka-autocomplete-option--focused", enter["when"]);
	}

	private IRenderedComponent<MokaAutoComplete<string>> RenderCities() =>
		Render<MokaAutoComplete<string>>(p => p
			.Add(x => x.Label, "City")
			.Add(x => x.Debounce, 0)
			.Add(x => x.SearchFunc, query => Task.FromResult(
				Cities.Where(c => c.StartsWith(query, StringComparison.OrdinalIgnoreCase)))));

	private async Task<IRenderedComponent<MokaAutoComplete<string>>> RenderOpenAsync()
	{
		IRenderedComponent<MokaAutoComplete<string>> cut = RenderCities();
		await cut.Find("input[role=combobox]").InputAsync(new ChangeEventArgs { Value = "Ber" });
		cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[role=option]").Count));
		return cut;
	}
}
