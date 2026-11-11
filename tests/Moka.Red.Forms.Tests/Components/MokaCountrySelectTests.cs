using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Theming;
using Moka.Red.Forms.CountrySelect;
using Moka.Red.Primitives.Flag;

namespace Moka.Red.Forms.Tests.Components;

/// <summary>
///     Every option on the board: the three shapes and the prefix, what a row shows, the order, and the extras
///     (wide search, flag chips, the flag in the field, type to jump), plus binding, EditForm and a static post.
/// </summary>
public sealed class MokaCountrySelectTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Forms/CountrySelect/MokaCountrySelect.razor.js";

	private readonly Order _order = new();

	public MokaCountrySelectTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<MokaCountrySelect> Select(Action<ComponentParameterCollectionBuilder<MokaCountrySelect>>? extra = null) =>
		Render<MokaCountrySelect>(p =>
		{
			p.Add(x => x.SuggestBrowserCountry, false);
			extra?.Invoke(p);
		});

	private IRenderedComponent<MokaCountrySelect> Opened(Action<ComponentParameterCollectionBuilder<MokaCountrySelect>>? extra = null)
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(extra);
		cut.Find("[role=combobox]").Click();
		return cut;
	}

	private static string[] Headings(IRenderedComponent<MokaCountrySelect> cut) =>
		[.. cut.FindAll(".moka-select-group-header").Select(h => h.TextContent.Trim())];

	private static string FirstOption(IRenderedComponent<MokaCountrySelect> cut) =>
		cut.FindAll("[role=option] .moka-country-option__name")[0].TextContent.Trim();

	// F1A
	[Fact]
	public void Dropdown_ShowsTheFlagAndNameInTheField()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p.Add(x => x.Value, "SE").Add(x => x.Label, "Country"));

		IElement field = cut.Find("[role=combobox]");
		Assert.Single(field.QuerySelectorAll(".moka-country-value .moka-flag"));
		Assert.Equal("Sweden", field.QuerySelector(".moka-country-value__name")!.TextContent);
		Assert.Equal(cut.Find("label.moka-field-label").Id, field.GetAttribute("aria-labelledby"));
		Assert.Contains("moka-country-select--dropdown", cut.Find(".moka-country-select").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Dropdown_HasASearchBoxForCountries()
	{
		IRenderedComponent<MokaCountrySelect> cut = Opened();

		IElement search = cut.Find(".moka-select-search");
		Assert.Equal("Search countries", search.GetAttribute("placeholder"));
		Assert.Equal(MokaCountries.All.Count, cut.FindAll("[role=option]").Count);
	}

	[Fact]
	public void Dropdown_WithNothingPicked_ShowsThePlaceholder()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select();

		Assert.Equal("Choose a country", cut.Find(".moka-select-value").TextContent.Trim());
	}

	// F1B
	[Fact]
	public void FlagButton_IsTheFlagAlone_NamedByItsLabelAndCountry()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Shape, MokaCountrySelectShape.FlagButton)
			.Add(x => x.Label, "Region")
			.Add(x => x.Value, "SE"));

		IElement button = cut.Find("[role=combobox]");
		Assert.Empty(cut.FindAll("label.moka-field-label"));
		Assert.Equal("Region, Sweden", button.GetAttribute("aria-label"));
		Assert.Single(button.QuerySelectorAll(".moka-country-flag--button .moka-flag"));
		Assert.Empty(button.QuerySelectorAll(".moka-country-value__name"));
		Assert.Equal("Region: Sweden", cut.Find(".moka-country-select__tooltip > [role=tooltip]").TextContent.Trim());
	}

	[Fact]
	public void FlagButton_WithNothingPicked_ShowsAGlobe()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p.Add(x => x.Shape, MokaCountrySelectShape.FlagButton));

		IElement button = cut.Find("[role=combobox]");
		Assert.Equal("Country", button.GetAttribute("aria-label"));
		Assert.Single(button.QuerySelectorAll(".moka-flag--unknown"));
	}

	[Fact]
	public void FlagButton_HidesItsTooltip_WhileTheListIsOpen()
	{
		IRenderedComponent<MokaCountrySelect> cut = Opened((p => p
			.Add(x => x.Shape, MokaCountrySelectShape.FlagButton)
			.Add(x => x.Value, "SE")));

		Assert.Empty(cut.FindAll(".moka-country-select__tooltip > [role=tooltip]"));
	}

	// F1C
	[Fact]
	public void Grid_IsRowsOfTiles_WithTheChosenOneSelected()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Shape, MokaCountrySelectShape.Grid)
			.Add(x => x.Label, "Ships from")
			.Add(x => x.Value, "SE"));

		IElement grid = cut.Find("[role=grid]");
		Assert.Equal(cut.Find("label.moka-field-label").Id, grid.GetAttribute("aria-labelledby"));
		IReadOnlyList<IElement> rows = cut.FindAll("[role=grid] > [role=row]");
		Assert.Equal(6, rows[0].QuerySelectorAll("[role=gridcell]").Length);
		Assert.Equal(MokaCountries.All.Count, cut.FindAll("[role=gridcell]").Count);

		IElement sweden = cut.Find("[role=gridcell][data-moka-code=SE]");
		Assert.Equal("true", sweden.GetAttribute("aria-selected"));
		Assert.Equal("Sweden", sweden.GetAttribute("aria-label"));
		Assert.Equal("SE", sweden.QuerySelector(".moka-country-grid__code")!.TextContent);
		Assert.Equal("false", cut.Find("[role=gridcell][data-moka-code=NO]").GetAttribute("aria-selected"));
	}

	[Fact]
	public async Task Grid_BindsItsScript_AndRowsFollowTheColumnsItReports()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p.Add(x => x.Shape, MokaCountrySelectShape.Grid));

		module.VerifyInvoke("bindGrid");
		await cut.InvokeAsync(() => cut.Instance.SetGridColumns(4));

		Assert.Equal(4, cut.FindAll("[role=grid] > [role=row]")[0].QuerySelectorAll("[role=gridcell]").Length);
		Assert.Contains("--_cols: 4", cut.Find("[role=grid]").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public async Task Grid_APick_SetsTheValue()
	{
		string? picked = null;
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Shape, MokaCountrySelectShape.Grid)
			.Add(x => x.ValueChanged, v => picked = v));

		await cut.InvokeAsync(() => cut.Instance.PickFromGrid("no"));

		Assert.Equal("NO", picked);
		Assert.Equal("true", cut.Find("[role=gridcell][data-moka-code=NO]").GetAttribute("aria-selected"));
	}

	[Fact]
	public async Task Grid_InAMultipleSelect_PicksToggle()
	{
		IList<string>? picked = null;
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Shape, MokaCountrySelectShape.Grid)
			.Add(x => x.Multiple, true)
			.Add(x => x.SelectedValues, new List<string> { "SE" })
			.Add(x => x.SelectedValuesChanged, v => picked = v));

		Assert.Equal("true", cut.Find("[role=grid]").GetAttribute("aria-multiselectable"));
		await cut.InvokeAsync(() => cut.Instance.PickFromGrid("NO"));
		Assert.Equal(["SE", "NO"], picked);
		await cut.InvokeAsync(() => cut.Instance.PickFromGrid("SE"));
		Assert.Equal(["NO"], picked);
	}

	[Fact]
	public void Grid_SearchFindsCodesAndOtherNames()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p.Add(x => x.Shape, MokaCountrySelectShape.Grid));

		cut.Find(".moka-country-grid__query").Input("holland");

		Assert.Equal("NL", Assert.Single(cut.FindAll("[role=gridcell]")).GetAttribute("data-moka-code"));

		cut.Find(".moka-country-grid__query").Input("zzzz");
		Assert.Equal("No country matches", cut.Find(".moka-country-grid__empty").TextContent.Trim());
	}

	[Fact]
	public void Grid_ListsAToZ_SinceItHasNoHeadings()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		IRenderedComponent<MokaCountrySelect> cut = Render<MokaCountrySelect>(p => p
			.Add(x => x.Shape, MokaCountrySelectShape.Grid)
			.Add(x => x.Suggested, ["SE"]));

		Assert.Equal("AF", cut.FindAll("[role=gridcell]")[0].GetAttribute("data-moka-code"));
		Assert.Empty(module.Invocations["browserCountry"]);
	}

	[Fact]
	public void Prefix_LeavesOutTheDialingTag_SinceTheRowShowsTheCode()
	{
		IRenderedComponent<MokaCountrySelect> cut = Opened(p => p.Add(x => x.Shape, MokaCountrySelectShape.Prefix));

		cut.Find(".moka-select-search").Input("44");

		Assert.Equal("United Kingdom", cut.Find("[role=option] .moka-country-option__name").TextContent);
		Assert.Empty(cut.FindAll(".moka-select-option-tag"));
	}

	[Fact]
	public async Task Grid_Disabled_TakesNoPick()
	{
		string? picked = null;
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Shape, MokaCountrySelectShape.Grid)
			.Add(x => x.Disabled, true)
			.Add(x => x.ValueChanged, v => picked = v));

		await cut.InvokeAsync(() => cut.Instance.PickFromGrid("NO"));

		Assert.Null(picked);
		Assert.False(cut.Find("[role=grid]").HasAttribute("tabindex"));
		Assert.Equal("true", cut.Find("[role=grid]").GetAttribute("aria-disabled"));
	}

	// F1D's prefix shape, as MokaPhoneInput uses it.
	[Fact]
	public void Prefix_ShowsTheFlagAndDialingCode_AndListsDialingCodes()
	{
		IRenderedComponent<MokaCountrySelect> cut = Opened((p => p
			.Add(x => x.Shape, MokaCountrySelectShape.Prefix)
			.Add(x => x.Value, "SE")));

		IElement button = cut.Find("[role=combobox]");
		Assert.Equal("+46", button.QuerySelector(".moka-country-value__dial")!.TextContent);
		Assert.Equal("Country code, Sweden +46", button.GetAttribute("aria-label"));
		Assert.Equal("Country, code or +46", cut.Find(".moka-select-search").GetAttribute("placeholder"));
		IElement sweden = cut.FindAll("[role=option]").First(o => o.TextContent.Contains("Sweden", StringComparison.Ordinal));
		Assert.Equal("+46", sweden.QuerySelector(".moka-country-option__end")!.TextContent);
	}

	// F2
	[Theory]
	[InlineData(null, "SE")]
	[InlineData(MokaCountryRowContent.NameAndCode, "SE")]
	[InlineData(MokaCountryRowContent.NameAndDialingCode, "+46")]
	[InlineData(MokaCountryRowContent.Name, null)]
	public void RowContent_DecidesWhatEndsARow(MokaCountryRowContent? rows, string? end)
	{
		IRenderedComponent<MokaCountrySelect> cut = Opened((p => p.Add(x => x.RowContent, rows)));

		IElement sweden = cut.FindAll("[role=option]").First(o => o.TextContent.Contains("Sweden", StringComparison.Ordinal));
		Assert.Equal(end, sweden.QuerySelector(".moka-country-option__end")?.TextContent);
	}

	[Fact]
	public void RowContent_OwnName_GoesUnderTheName_WhenItDiffers()
	{
		IRenderedComponent<MokaCountrySelect> cut = Opened((p => p.Add(x => x.RowContent, MokaCountryRowContent.OwnName)));

		IReadOnlyList<IElement> options = cut.FindAll("[role=option]");
		IElement sweden = options.First(o => o.TextContent.Contains("Sweden", StringComparison.Ordinal));
		IElement australia = options.First(o => o.TextContent.Contains("Australia", StringComparison.Ordinal));
		Assert.Equal("Sverige", sweden.QuerySelector(".moka-country-option__own")!.TextContent);
		Assert.Null(australia.QuerySelector(".moka-country-option__own"));
		Assert.Contains("moka-country-select--two-line", cut.Find(".moka-country-select").ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Rows_HideTheirFlagFromScreenReaders()
	{
		IRenderedComponent<MokaCountrySelect> cut = Opened();

		Assert.All(cut.FindAll("[role=option] .moka-country-flag"), flag => Assert.Equal("true", flag.GetAttribute("aria-hidden")));
	}

	// F3
	[Fact]
	public void Alphabetical_IsAToZ_WithAccentsSetAside_AndNoHeadings()
	{
		IRenderedComponent<MokaCountrySelect> cut = Opened((p => p.Add(x => x.Order, MokaCountryOrder.Alphabetical)));

		string[] names = [.. cut.FindAll("[role=option] .moka-country-option__name").Select(n => n.TextContent)];
		Assert.Equal("Afghanistan", names[0]);
		Assert.True(Array.IndexOf(names, "Åland Islands") < Array.IndexOf(names, "Albania"));
		Assert.Empty(Headings(cut));
	}

	[Fact]
	public void SuggestedFirst_ListsTheSuggestionsOnce_OverEveryoneElse()
	{
		IRenderedComponent<MokaCountrySelect> cut = Opened((p => p
			.Add(x => x.Order, MokaCountryOrder.SuggestedFirst)
			.Add(x => x.Suggested, ["se", "US", "SE", "ZZ"])));

		Assert.Equal(["Suggested", "All countries"], Headings(cut));
		string[] names = [.. cut.FindAll("[role=option] .moka-country-option__name").Select(n => n.TextContent)];
		Assert.Equal(["Sweden", "United States", "Afghanistan"], names.Take(3));
		Assert.Single(names, n => n == "Sweden");
		Assert.Equal(MokaCountries.All.Count, names.Length);
	}

	[Fact]
	public void SuggestedFirst_WithNothingToSuggest_IsAToZ()
	{
		IRenderedComponent<MokaCountrySelect> cut = Opened();

		Assert.Empty(Headings(cut));
		Assert.Equal("Afghanistan", FirstOption(cut));
	}

	[Fact]
	public void SuggestedFirst_PutsTheBrowsersCountryFirst()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		module.Setup<string?>("browserCountry").SetResult("NO");

		IRenderedComponent<MokaCountrySelect> cut = Render<MokaCountrySelect>(p => p.Add(x => x.Suggested, ["SE"]));
		cut.Find("[role=combobox]").Click();

		Assert.Equal(["Norway", "Sweden"], cut.FindAll("[role=option] .moka-country-option__name").Take(2).Select(n => n.TextContent));
	}

	[Fact]
	public void SuggestBrowserCountry_Off_AsksTheBrowserNothing()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		module.Setup<string?>("browserCountry").SetResult("NO");

		Select();

		Assert.Empty(module.Invocations["browserCountry"]);
	}

	[Fact]
	public void ByRegion_ListsUnderContinents()
	{
		IRenderedComponent<MokaCountrySelect> cut = Opened((p => p.Add(x => x.Order, MokaCountryOrder.ByRegion)));

		Assert.Equal(["Africa", "Americas", "Antarctica", "Asia", "Europe", "Oceania"], Headings(cut));
		Assert.Equal("Algeria", FirstOption(cut));
	}

	[Fact]
	public void SuggestedThenByRegion_PutsSuggestionsOverTheContinents()
	{
		IRenderedComponent<MokaCountrySelect> cut = Opened((p => p
			.Add(x => x.Order, MokaCountryOrder.SuggestedThenByRegion)
			.Add(x => x.Suggested, ["SE"])
			.Add(x => x.RegionText, region => region.ToString().ToUpperInvariant())));

		Assert.Equal(["Suggested", "AFRICA", "AMERICAS", "ANTARCTICA", "ASIA", "EUROPE", "OCEANIA"], Headings(cut));
		Assert.Equal("Sweden", FirstOption(cut));
	}

	// F4A
	[Theory]
	[InlineData("holland", "Netherlands", "also Holland")]
	[InlineData("se", "Sweden", "code SE")]
	[InlineData("+46", "Sweden", "dialing +46")]
	[InlineData("suomi", "Finland", "Suomi")]
	[InlineData("cote", "Côte d’Ivoire", null)]
	public void ExtendedSearch_FindsCodesDialingCodesAndOtherNames_AndSaysWhy(string text, string first, string? tag)
	{
		IRenderedComponent<MokaCountrySelect> cut = Opened();

		cut.Find(".moka-select-search").Input(text);

		IElement option = cut.FindAll("[role=option]")[0];
		Assert.Equal(first, option.QuerySelector(".moka-country-option__name")!.TextContent);
		Assert.Equal(tag, option.QuerySelector(".moka-select-option-tag")?.TextContent);
	}

	[Fact]
	public void ExtendedSearch_PlusOne_FindsTheUnitedStatesCanadaAndJamaica()
	{
		IRenderedComponent<MokaCountrySelect> cut = Opened();

		cut.Find(".moka-select-search").Input("+1");

		string[] names = [.. cut.FindAll("[role=option] .moka-country-option__name").Select(n => n.TextContent)];
		Assert.Equal("United States", names[0]);
		Assert.Contains("Canada", names);
		Assert.Contains("Jamaica", names);
	}

	[Fact]
	public void ExtendedSearch_Off_MatchesNamesOnly_AccentsAside()
	{
		IRenderedComponent<MokaCountrySelect> cut = Opened((p => p.Add(x => x.ExtendedSearch, false)));

		cut.Find(".moka-select-search").Input("holland");
		Assert.Empty(cut.FindAll("[role=option]"));

		cut.Find(".moka-select-search").Input("aland");
		Assert.Contains("Åland Islands", cut.FindAll("[role=option] .moka-country-option__name").Select(n => n.TextContent));
		Assert.Empty(cut.FindAll(".moka-select-option-tag"));
	}

	[Fact]
	public void MatchText_WritesTheTags()
	{
		IRenderedComponent<MokaCountrySelect> cut = Opened((p => p
			.Add(x => x.MatchText, match => match.Kind == MokaCountryMatchKind.OtherName ? $"aka {match.Matched}" : null)));

		cut.Find(".moka-select-search").Input("holland");

		Assert.Equal("aka Holland", cut.Find(".moka-select-option-tag").TextContent);
	}

	// F4B
	[Fact]
	public void Multiple_ShowsFlagChips_AndBackspaceTakesTheLastOff()
	{
		IList<string>? picked = null;
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.SelectedValues, new List<string> { "SE", "NO" })
			.Add(x => x.SelectedValuesChanged, v => picked = v));

		IReadOnlyList<IElement> chips = cut.FindAll(".moka-country-chip");
		Assert.Equal(2, chips.Count);
		Assert.Single(chips[0].QuerySelectorAll(".moka-flag"));
		Assert.Contains("Sweden", chips[0].TextContent, StringComparison.Ordinal);

		cut.Find("[role=combobox]").Click();
		Assert.Equal("true", cut.Find("[role=listbox]").GetAttribute("aria-multiselectable"));
		cut.Find(".moka-select-search").KeyDown(new KeyboardEventArgs { Key = "Backspace" });

		Assert.Equal(["SE"], picked);
	}

	[Fact]
	public void Multiple_OnTheNewDesignsStandardRows_StartsRowsWithCheckBoxes()
	{
		IRenderedComponent<MokaCountrySelect> cut = Render<MokaCountrySelect>(p => p
			.AddCascadingValue(MokaTheme.Default)
			.Add(x => x.SuggestBrowserCountry, false)
			.Add(x => x.Multiple, true));

		Assert.Contains("moka-select--list-radio", cut.Find(".moka-select").ClassName, StringComparison.Ordinal);
	}

	// F4C
	[Fact]
	public void FlagInField_Off_ShowsTheNameOnly()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p.Add(x => x.Value, "JP").Add(x => x.FlagInField, false));

		IElement field = cut.Find("[role=combobox]");
		Assert.Empty(field.QuerySelectorAll(".moka-flag"));
		Assert.Equal("Japan", cut.Find(".moka-select-value").TextContent.Trim());
	}

	// F4D
	[Fact]
	public async Task TypeToJump_TypingOnTheClosedFieldPicksACountry()
	{
		string? picked = null;
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Value, "SE")
			.Add(x => x.ValueChanged, v => picked = v));

		foreach (string key in new[] { "n", "o", "r", "w" })
		{
			await cut.Find("[role=combobox]").KeyDownAsync(new KeyboardEventArgs { Key = key });
		}

		Assert.Equal("NO", picked);
		Assert.Equal("false", cut.Find("[role=combobox]").GetAttribute("aria-expanded"));
	}

	[Fact]
	public async Task TypeToJump_Off_LeavesTheChoiceAlone()
	{
		string? picked = null;
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Value, "SE")
			.Add(x => x.TypeToJump, false)
			.Add(x => x.ValueChanged, v => picked = v));

		await cut.Find("[role=combobox]").KeyDownAsync(new KeyboardEventArgs { Key = "n" });

		Assert.Null(picked);
	}

	[Fact]
	public void Countries_LimitsTheList()
	{
		IRenderedComponent<MokaCountrySelect> cut = Opened((p => p.Add(x => x.Countries, ["SE", "NO", "DK", "xx"])));

		Assert.Equal(["Denmark", "Norway", "Sweden"], cut.FindAll("[role=option] .moka-country-option__name").Select(n => n.TextContent));
	}

	[Fact]
	public void Names_ShowAnAppsOwnNames_InTheListAndTheField()
	{
		static string German(MokaCountry country) => country.Code == "SE" ? "Schweden" : country.Name;
		IRenderedComponent<MokaCountrySelect> cut = Opened((p => p.Add(x => x.Value, "SE").Add(x => x.Names, German)));

		Assert.Equal("Schweden", cut.Find(".moka-country-value__name").TextContent);
		Assert.Contains("Schweden", cut.FindAll("[role=option] .moka-country-option__name").Select(n => n.TextContent));
	}

	[Fact]
	public void PickingFromTheList_RaisesValueChangedWithTheCode()
	{
		string? picked = null;
		IRenderedComponent<MokaCountrySelect> cut = Opened((p => p.Add(x => x.ValueChanged, v => picked = v)));

		cut.Find(".moka-select-search").Input("norway");
		cut.FindAll("[role=option]")[0].Click();

		Assert.Equal("NO", picked);
		Assert.Equal("Norway", cut.Find(".moka-country-value__name").TextContent);
	}

	[Fact]
	public void InAnEditForm_ShowsTheRequiredMessage_AndAPickClearsIt()
	{
		EditContext context = new(_order);
		IRenderedComponent<EditForm> form = Render<EditForm>(p => p
			.Add(x => x.EditContext, context)
			.Add(x => x.ChildContent, _ => builder =>
			{
				builder.OpenComponent<DataAnnotationsValidator>(0);
				builder.CloseComponent();
				builder.OpenComponent<MokaCountrySelect>(1);
				builder.AddComponentParameter(2, nameof(MokaCountrySelect.Label), "Country");
				builder.AddComponentParameter(3, nameof(MokaCountrySelect.SuggestBrowserCountry), false);
				builder.AddComponentParameter(4, nameof(MokaCountrySelect.Value), _order.Country);
				builder.AddComponentParameter(5, nameof(MokaCountrySelect.ValueChanged),
					EventCallback.Factory.Create<string?>(this, v => _order.Country = v));
				builder.AddComponentParameter(6, nameof(MokaCountrySelect.ValueExpression),
					(Expression<Func<string?>>)(() => _order.Country));
				builder.CloseComponent();
			}));

		form.InvokeAsync(() => context.Validate());
		Assert.Contains("Pick a country", form.Find(".moka-field-helper--error").TextContent, StringComparison.Ordinal);

		form.Find("[role=combobox]").Click();
		form.Find(".moka-select-search").Input("japan");
		form.FindAll("[role=option]")[0].Click();

		Assert.Equal("JP", _order.Country);
		Assert.True(context.IsModified(FieldIdentifier.Create(() => _order.Country)));
		Assert.Empty(form.FindAll(".moka-field-helper--error"));
	}

	[Fact]
	public void Bound_PostsTheCodeUnderTheFieldsName()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Value, "SE")
			.Add(x => x.ValueExpression, () => _order.Country));

		IElement hidden = cut.Find("input[type=hidden]");
		Assert.Equal("_order.Country", hidden.GetAttribute("name"));
		Assert.Equal("SE", hidden.GetAttribute("value"));
	}

	[Fact]
	public void Unbound_PostsNothing()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p.Add(x => x.Value, "SE"));

		Assert.Empty(cut.FindAll("input[type=hidden]"));
	}

	[Fact]
	public void Grid_Bound_PostsTheCodeUnderTheFieldsName()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Shape, MokaCountrySelectShape.Grid)
			.Add(x => x.Value, "SE")
			.Add(x => x.ValueExpression, () => _order.Country));

		IElement hidden = cut.Find("input[type=hidden]");
		Assert.Equal("_order.Country", hidden.GetAttribute("name"));
		Assert.Equal("SE", hidden.GetAttribute("value"));
	}

	[Fact]
	public void Margin_GoesOnTheRoot_AndClassToo()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.MarginValue, "7px")
			.Add(x => x.Class, "mine"));

		IElement root = cut.Find(".moka-country-select");
		Assert.Equal("margin: 7px", root.GetAttribute("style"));
		Assert.Contains("mine", root.ClassName, StringComparison.Ordinal);
	}

	private sealed class Order
	{
		[Required(ErrorMessage = "Pick a country")]
		public string? Country { get; set; }
	}
}
