using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Forms.CountrySelect;
using Moka.Red.Primitives.Flag;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// MokaGate's 0.4.1 gap 16: the rows and the field could only choose their text, so an app with flag pictures of its
// own couldn't show them.
public sealed class MokaCountrySelectTemplateTests : BunitContext
{
	private static readonly RenderFragment<MokaCountry> OwnFlag = country => builder =>
	{
		builder.OpenElement(0, "img");
		builder.AddAttribute(1, "class", "own-flag");
		builder.AddAttribute(2, "src", $"/flags/{country.Code}.svg");
		builder.AddAttribute(3, "alt", "");
		builder.CloseElement();
	};

	private static readonly RenderFragment<MokaCountry> OwnRow = country => builder =>
	{
		builder.OpenElement(0, "span");
		builder.AddAttribute(1, "class", "own-row");
		builder.AddContent(2, $"{country.Code} {country.Name} {country.DialingCode}");
		builder.CloseElement();
	};

	private static readonly RenderFragment<MokaCountry> OwnValue = country => builder =>
	{
		builder.OpenElement(0, "span");
		builder.AddAttribute(1, "class", "own-value");
		builder.AddContent(2, country.NativeName);
		builder.CloseElement();
	};

	public MokaCountrySelectTemplateTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<MokaCountrySelect> Select(Action<ComponentParameterCollectionBuilder<MokaCountrySelect>> extra) =>
		Render<MokaCountrySelect>(p =>
		{
			p.Add(x => x.SuggestBrowserCountry, false);
			extra(p);
		});

	private static void Open(IRenderedComponent<MokaCountrySelect> cut) => cut.Find("[role=combobox]").Click();

	[Fact]
	public void FlagTemplate_DrawsTheRowsAndTheFieldsFlags()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Value, "SE")
			.Add(x => x.Countries, ["SE", "NO"])
			.Add(x => x.FlagTemplate, OwnFlag));
		Open(cut);

		IElement field = cut.Find("[role=combobox]");
		Assert.Equal("/flags/SE.svg", field.QuerySelector(".moka-country-value > .moka-country-flag > img.own-flag")!.GetAttribute("src"));
		Assert.Equal(["/flags/NO.svg", "/flags/SE.svg"],
			cut.FindAll("[role=option] .moka-country-flag > img.own-flag").Select(img => img.GetAttribute("src")));
		Assert.Empty(cut.FindAll(".moka-flag"));
	}

	[Fact]
	public void FlagTemplate_KeepsTheRowsNamesAndEnds()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Countries, ["SE"])
			.Add(x => x.RowContent, MokaCountryRowContent.NameAndDialingCode)
			.Add(x => x.FlagTemplate, OwnFlag));
		Open(cut);

		IElement option = cut.Find("[role=option]");
		Assert.Equal("Sweden", option.QuerySelector(".moka-country-option__name")!.TextContent);
		Assert.Equal("+46", option.QuerySelector(".moka-country-option__end")!.TextContent);
		Assert.Equal("true", option.QuerySelector(".moka-country-flag")!.GetAttribute("aria-hidden"));
	}

	[Fact]
	public void FlagTemplate_DrawsTheChipsFlags()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.SelectedValues, new List<string> { "SE", "NO" })
			.Add(x => x.FlagTemplate, OwnFlag));

		Assert.Equal(["/flags/SE.svg", "/flags/NO.svg"],
			cut.FindAll(".moka-country-chip img.own-flag").Select(img => img.GetAttribute("src")));
	}

	[Theory]
	[InlineData(MokaCountrySelectShape.FlagButton)]
	[InlineData(MokaCountrySelectShape.Prefix)]
	public void FlagTemplate_DrawsTheButtonShapesFlag_AndKeepsTheirName(MokaCountrySelectShape shape)
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Shape, shape)
			.Add(x => x.Value, "SE")
			.Add(x => x.FlagTemplate, OwnFlag));

		IElement button = cut.Find("[role=combobox]");
		Assert.Single(button.QuerySelectorAll("img.own-flag"));
		Assert.Empty(button.QuerySelectorAll(".moka-flag"));
		Assert.StartsWith(shape == MokaCountrySelectShape.Prefix ? "Country code, Sweden" : "Country, Sweden",
			button.GetAttribute("aria-label"), StringComparison.Ordinal);
	}

	[Fact]
	public void FlagTemplate_LeavesTheFlagButtonsGlobe_ForNoCountry()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Shape, MokaCountrySelectShape.FlagButton)
			.Add(x => x.FlagTemplate, OwnFlag));

		IElement button = cut.Find("[role=combobox]");
		Assert.Single(button.QuerySelectorAll(".moka-flag--unknown"));
		Assert.Empty(button.QuerySelectorAll("img.own-flag"));
	}

	[Fact]
	public void FlagTemplate_DrawsTheGridsTiles()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Shape, MokaCountrySelectShape.Grid)
			.Add(x => x.Countries, ["SE", "NO"])
			.Add(x => x.FlagTemplate, OwnFlag));

		IElement sweden = cut.Find("[role=gridcell][data-moka-code=SE]");
		Assert.Equal("/flags/SE.svg", sweden.QuerySelector(".moka-country-grid__flag > img.own-flag")!.GetAttribute("src"));
		Assert.Equal("SE", sweden.QuerySelector(".moka-country-grid__code")!.TextContent);
		Assert.Empty(cut.FindAll(".moka-flag"));
	}

	[Fact]
	public void ItemTemplate_DrawsEachRow_WithTheSelectsHighlightAndCheckAround_It()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Value, "SE")
			.Add(x => x.Countries, ["SE", "NO"])
			.Add(x => x.ItemTemplate, OwnRow));
		Open(cut);

		IReadOnlyList<IElement> options = cut.FindAll("[role=option]");
		Assert.Equal(["NO Norway +47", "SE Sweden +46"],
			options.Select(option => option.QuerySelector(".moka-select-option-content > .own-row")!.TextContent));
		Assert.Empty(cut.FindAll("[role=option] .moka-country-option"));
		Assert.Equal("true", options[1].GetAttribute("aria-selected"));
	}

	[Fact]
	public void ItemTemplate_KeepsTheSearchTags()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p.Add(x => x.ItemTemplate, OwnRow));
		Open(cut);

		cut.Find(".moka-select-search").Input("+46");

		IElement first = cut.FindAll("[role=option]")[0];
		Assert.Equal("SE Sweden +46", first.QuerySelector(".own-row")!.TextContent);
		Assert.Equal("dialing +46", first.QuerySelector(".moka-select-option-tag")!.TextContent);
	}

	[Fact]
	public void ItemTemplate_LeavesTheGridsTilesAlone()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Shape, MokaCountrySelectShape.Grid)
			.Add(x => x.Countries, ["SE"])
			.Add(x => x.ItemTemplate, OwnRow));

		Assert.Empty(cut.FindAll(".own-row"));
		Assert.Single(cut.FindAll(".moka-country-grid__tile .moka-flag"));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ValueTemplate_DrawsTheChosenCountry_WhateverFlagInFieldSays(bool flagInField)
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Value, "SE")
			.Add(x => x.FlagInField, flagInField)
			.Add(x => x.ValueTemplate, OwnValue));

		IElement field = cut.Find("[role=combobox]");
		Assert.Equal("Sverige", field.QuerySelector(".own-value")!.TextContent);
		Assert.Empty(field.QuerySelectorAll(".moka-country-value"));
	}

	[Theory]
	[InlineData(MokaCountrySelectShape.FlagButton, "Country, Sweden")]
	[InlineData(MokaCountrySelectShape.Prefix, "Country code, Sweden +46")]
	public void ValueTemplate_DrawsTheButtonShapes_AndKeepsTheirName(MokaCountrySelectShape shape, string name)
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Shape, shape)
			.Add(x => x.Value, "SE")
			.Add(x => x.ValueTemplate, OwnValue));

		IElement button = cut.Find("[role=combobox]");
		Assert.Equal("Sverige", button.QuerySelector(".own-value")!.TextContent);
		Assert.Equal(name, button.GetAttribute("aria-label"));
	}

	[Fact]
	public void ValueTemplate_WinsOverFlagTemplate_InTheField_ButNotInTheRows()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Value, "SE")
			.Add(x => x.Countries, ["SE"])
			.Add(x => x.FlagTemplate, OwnFlag)
			.Add(x => x.ValueTemplate, OwnValue));
		Open(cut);

		IElement field = cut.Find("[role=combobox]");
		Assert.Empty(field.QuerySelectorAll("img.own-flag"));
		Assert.Single(cut.FindAll("[role=option] img.own-flag"));
	}

	[Fact]
	public void WithoutTemplates_TheFlagsAreMokaFlags()
	{
		IRenderedComponent<MokaCountrySelect> cut = Select(p => p
			.Add(x => x.Value, "SE")
			.Add(x => x.Countries, ["SE"]));
		Open(cut);

		Assert.Single(cut.Find("[role=combobox]").QuerySelectorAll(".moka-country-value > .moka-country-flag > .moka-flag"));
		Assert.Single(cut.FindAll("[role=option] .moka-country-flag > .moka-flag"));
	}

	// A picture is as tall as MokaFlag's artwork, and :where keeps the rule light enough for the picture's own class.
	[Theory]
	[InlineData(":where(.moka-country-flag) ::deep > :where(img)")]
	[InlineData(":where(.moka-country-flag) ::deep > :where(svg)")]
	[InlineData(":where(.moka-country-grid__flag) ::deep > :where(img)")]
	[InlineData(":where(.moka-country-grid__flag) ::deep > :where(svg)")]
	public void AFlagPicture_IsSizedToTheText(string selector)
	{
		IReadOnlyDictionary<string, string> picture = ScopedCss.Declarations("MokaCountrySelect.razor.css", selector);

		Assert.Equal("0.83em", picture["height"]);
		Assert.Equal("auto", picture["width"]);
	}
}
