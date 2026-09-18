using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.SelectField;

namespace Moka.Red.Forms.Tests.Components;

/// <summary>
///     The trigger is a div with <c>role="combobox"</c>, which <c>&lt;label for&gt;</c> cannot name.
/// </summary>
public class MokaSelectTests : BunitContext
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	private static readonly string[] Fruits = ["Apple", "Banana", "Cherry"];

	public MokaSelectTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void WithLabel_ComboboxIsLabelledByTheLabel()
	{
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Label, "Fruit")
			.Add(x => x.Items, Fruits));

		IElement combobox = cut.Find("[role=combobox]");
		IElement label = cut.Find("label.moka-field-label");
		Assert.False(string.IsNullOrEmpty(label.Id));
		Assert.Equal(label.Id, combobox.GetAttribute("aria-labelledby"));
		Assert.False(combobox.HasAttribute("aria-label"));
	}

	[Fact]
	public void WithoutLabel_UsesTheConsumersAriaLabel()
	{
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, Fruits)
			.Add(x => x.Placeholder, "Choose")
			.AddUnmatched("aria-label", "Favourite fruit"));

		IElement combobox = cut.Find("[role=combobox]");
		Assert.Equal("Favourite fruit", combobox.GetAttribute("aria-label"));
		Assert.False(combobox.HasAttribute("aria-labelledby"));
	}

	[Fact]
	public void WithoutLabel_FallsBackToThePlaceholder()
	{
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, Fruits)
			.Add(x => x.Placeholder, "Choose a fruit"));

		Assert.Equal("Choose a fruit", cut.Find("[role=combobox]").GetAttribute("aria-label"));
	}

	[Fact]
	public void Closed_ReportsNotExpanded()
	{
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Label, "Fruit")
			.Add(x => x.Items, Fruits));

		IElement combobox = cut.Find("[role=combobox]");
		Assert.Equal("false", combobox.GetAttribute("aria-expanded"));
		Assert.False(combobox.HasAttribute("aria-controls"));
	}

	[Fact]
	public async Task Open_ComboboxControlsALabelledListbox()
	{
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Label, "Fruit")
			.Add(x => x.Items, Fruits));

		await cut.Find("[role=combobox]").ClickAsync(new MouseEventArgs());

		IElement combobox = cut.Find("[role=combobox]");
		IElement listbox = cut.Find("[role=listbox]");
		Assert.Equal("true", combobox.GetAttribute("aria-expanded"));
		Assert.Equal(listbox.Id, combobox.GetAttribute("aria-controls"));
		Assert.Equal(combobox.GetAttribute("aria-labelledby"), listbox.GetAttribute("aria-labelledby"));
	}

	[Fact]
	public void CancelsTheBrowsersDefault_ForTheKeysItHandles()
	{
		BunitJSModuleInterop keys = JSInterop.SetupModule(KeysModule);

		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, Fruits));

		JSRuntimeInvocation bind = keys.VerifyInvoke("preventKeys");
		ElementReference root = Assert.IsType<ElementReference>(bind.Arguments[0]);
		Assert.Equal(root.Id, cut.Find(".moka-select").GetAttribute("blazor:elementReference"));

		Dictionary<string, object?>[] rules = Assert.IsType<Dictionary<string, object?>[]>(bind.Arguments[1]);
		Assert.Contains(rules, r => (string?)r["selector"] == ".moka-select-trigger" && ((string[])r["keys"]!).Contains(" "));
		Assert.Contains(rules, r => (string?)r["selector"] == ".moka-select-search" && ((string[])r["keys"]!).Contains("Enter"));
	}

	[Fact]
	public void Disabled_IsOutOfTheTabOrderAndCannotBeCleared()
	{
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, Fruits)
			.Add(x => x.Value, "Apple")
			.Add(x => x.Clearable, true)
			.Add(x => x.Disabled, true));

		IElement combobox = cut.Find("[role=combobox]");
		Assert.False(combobox.HasAttribute("tabindex"));
		Assert.Equal("true", combobox.GetAttribute("aria-disabled"));
		Assert.True(cut.Find(".moka-select-clear").HasAttribute("disabled"));
	}

	[Fact]
	public async Task Options_ReportSelectionAsTrueOrFalse()
	{
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, Fruits)
			.Add(x => x.Value, "Banana"));

		await cut.Find("[role=combobox]").ClickAsync(new MouseEventArgs());

		Assert.Equal(["false", "true", "false"], cut.FindAll("[role=option]").Select(o => o.GetAttribute("aria-selected")));
	}

	[Fact]
	public async Task ArrowKeys_PointTheComboboxAtTheHighlightedOption()
	{
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, Fruits));

		await cut.Find("[role=combobox]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
		await cut.Find("[role=combobox]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

		IElement second = cut.FindAll("[role=option]")[1];
		Assert.False(string.IsNullOrEmpty(second.Id));
		Assert.Equal(second.Id, cut.Find("[role=combobox]").GetAttribute("aria-activedescendant"));
	}

	[Fact]
	public async Task Grouped_ArrowKeysFollowTheOrderOnScreen()
	{
		Drink[] drinks = [new("Espresso", "Coffee"), new("Green Tea", "Tea"), new("Latte", "Coffee")];
		Drink? chosen = null;
		IRenderedComponent<MokaSelect<Drink>> cut = Render<MokaSelect<Drink>>(p => p
			.Add(x => x.Items, drinks)
			.Add(x => x.ValueSelector, d => d.Name)
			.Add(x => x.GroupBy, d => d.Kind)
			.Add(x => x.ValueChanged, d => chosen = d));

		// Shown as Coffee: Espresso, Latte, then Tea: Green Tea. The second option on screen is Latte.
		await cut.Find("[role=combobox]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
		await cut.Find("[role=combobox]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
		await cut.Find("[role=combobox]").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal("Latte", chosen?.Name);
	}

	[Fact]
	public async Task EnterOnTheClearButton_DoesNotAlsoOpenTheList()
	{
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, Fruits)
			.Add(x => x.Value, "Apple")
			.Add(x => x.Clearable, true));

		await cut.Find(".moka-select-clear").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal("false", cut.Find("[role=combobox]").GetAttribute("aria-expanded"));
	}

	[Fact]
	public async Task Searchable_OpeningMovesFocusToTheSearchBox()
	{
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, Fruits)
			.Add(x => x.Searchable, true));

		await cut.Find("[role=combobox]").ClickAsync(new MouseEventArgs());

		ElementReference focused = Assert.IsType<ElementReference>(JSInterop.VerifyFocusAsyncInvoke().Arguments[0]);
		Assert.Equal(cut.Find(".moka-select-search").GetAttribute("blazor:elementReference"), focused.Id);
	}

	[Fact]
	public async Task Searchable_EscapeHandsFocusBackToTheTrigger()
	{
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, Fruits)
			.Add(x => x.Searchable, true));
		// bUnit writes an element's reference id only on the render that creates it.
		string? triggerRef = cut.Find("[role=combobox]").GetAttribute("blazor:elementReference");

		await cut.Find("[role=combobox]").ClickAsync(new MouseEventArgs());
		await cut.Find(".moka-select-search").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		IReadOnlyList<JSRuntimeInvocation> focuses = JSInterop.VerifyFocusAsyncInvoke(2);
		ElementReference last = Assert.IsType<ElementReference>(focuses[1].Arguments[0]);
		Assert.Equal(triggerRef, last.Id);
		Assert.Equal("false", cut.Find("[role=combobox]").GetAttribute("aria-expanded"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Options_RenderInsideTheListbox(bool grouped)
	{
		IRenderedComponent<MokaSelect<string>> cut = Render<MokaSelect<string>>(p => p
			.Add(x => x.Items, Fruits)
			.Add(x => x.GroupBy, grouped ? f => f.Length > 5 ? "Long" : "Short" : null));

		await cut.Find("[role=combobox]").ClickAsync(new MouseEventArgs());

		Assert.Equal(3, cut.FindAll("[role=listbox] [role=option]").Count);
		Assert.Equal(3, cut.FindAll("[role=option]").Count);
	}

	private sealed record Drink(string Name, string Kind);
}
