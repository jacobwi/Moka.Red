using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Forms.CountrySelect;
using Moka.Red.Forms.PhoneInput;

namespace Moka.Red.Forms.Tests.Components;

/// <summary>
///     With a <c>Country</c>, the fixed prefix becomes a country picker and the value is that country's dialing code
///     and the number. Without one, nothing changes.
/// </summary>
public sealed class MokaPhoneInputCountryTests : BunitContext
{
	public MokaPhoneInputCountryTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<MokaPhoneInput> Phone(string? country, string? value, Action<string?> report,
		Action<ComponentParameterCollectionBuilder<MokaPhoneInput>>? extra = null) =>
		Render<MokaPhoneInput>(p =>
		{
			p.Add(x => x.Country, country)
				.Add(x => x.Value, value!)
				.Add(x => x.ValueChanged, EventCallback.Factory.Create<string>(this, v => report(v)));
			extra?.Invoke(p);
		});

	private static void Type(IRenderedComponent<MokaPhoneInput> cut, string text)
	{
		IElement input = cut.Find("input[type=tel]");
		input.Focus();
		input.Input(text);
		input.Blur();
	}

	[Fact]
	public void WithoutACountry_TheFixedPrefixStays()
	{
		IRenderedComponent<MokaPhoneInput> cut = Phone(null, null, _ => { });

		Assert.Equal("+1", cut.Find(".moka-phone-prefix").TextContent);
		Assert.Empty(cut.FindComponents<MokaCountrySelect>());
		Assert.DoesNotContain("moka-phone-wrapper--picker", cut.Find(".moka-phone-wrapper").ClassName, StringComparison.Ordinal);
		Assert.Equal("(555) 123-4567", cut.Find("input").GetAttribute("placeholder"));
		Assert.Equal("tel", cut.Find("input").GetAttribute("autocomplete"));
	}

	[Fact]
	public void WithoutACountry_TheValueIsWhatItWas()
	{
		string? value = null;
		IRenderedComponent<MokaPhoneInput> cut = Phone(null, null, v => value = v);

		Type(cut, "5551234567");

		Assert.Equal("+15551234567", value);
		Assert.Equal("(555) 123-4567", cut.Find("input").GetAttribute("value"));
	}

	[Fact]
	public void WithACountry_ThePrefixIsAPickerWithTheFlagAndDialingCode()
	{
		IRenderedComponent<MokaPhoneInput> cut = Phone("SE", null, _ => { });

		Assert.Empty(cut.FindAll(".moka-phone-prefix"));
		IElement picker = cut.Find(".moka-phone-country [role=combobox]");
		Assert.Equal("Country code, Sweden +46", picker.GetAttribute("aria-label"));
		Assert.Equal("+46", picker.QuerySelector(".moka-country-value__dial")!.TextContent);
		Assert.Contains("moka-phone-wrapper--picker", cut.Find(".moka-phone-wrapper").ClassName, StringComparison.Ordinal);
		Assert.Equal("tel-national", cut.Find("input[type=tel]").GetAttribute("autocomplete"));
		Assert.False(cut.Find("input[type=tel]").HasAttribute("placeholder"));
	}

	[Fact]
	public void WithACountry_TheValueIsItsDialingCodeAndTheNumber_WithoutTheTrunkZero()
	{
		string? value = null;
		IRenderedComponent<MokaPhoneInput> cut = Phone("SE", null, v => value = v);

		Type(cut, "070 123 45 67");

		Assert.Equal("+46701234567", value);
		Assert.Equal("070 123 45 67", cut.Find("input[type=tel]").GetAttribute("value"));
	}

	[Fact]
	public void Italy_KeepsItsLeadingZero()
	{
		string? value = null;
		IRenderedComponent<MokaPhoneInput> cut = Phone("IT", null, v => value = v);

		Type(cut, "06 1234 5678");

		Assert.Equal("+390612345678", value);
	}

	[Fact]
	public void DropTrunkZero_Off_KeepsTheZero()
	{
		string? value = null;
		IRenderedComponent<MokaPhoneInput> cut = Phone("SE", null, v => value = v, p => p.Add(x => x.DropTrunkZero, false));

		Type(cut, "070 123");

		Assert.Equal("+46070123", value);
	}

	[Fact]
	public void TheNorthAmericanPlan_StillFormatsAsBefore()
	{
		string? value = null;
		IRenderedComponent<MokaPhoneInput> cut = Phone("CA", null, v => value = v);

		Type(cut, "4165551234");

		Assert.Equal("+14165551234", value);
		Assert.Equal("(416) 555-1234", cut.Find("input[type=tel]").GetAttribute("value"));
		Assert.Equal("(555) 123-4567", cut.Find("input[type=tel]").GetAttribute("placeholder"));
	}

	[Fact]
	public void LettersAreDropped_AndTheNumberStopsAtFifteenDigits()
	{
		string? value = null;
		IRenderedComponent<MokaPhoneInput> cut = Phone("SE", null, v => value = v);

		Type(cut, "7a0-1234567890123456");

		Assert.Equal("+467012345678901", value);
	}

	[Fact]
	public void AValueFromOutside_ShowsWithoutItsDialingCode()
	{
		IRenderedComponent<MokaPhoneInput> cut = Phone("SE", "+46701234567", _ => { });

		Assert.Equal("701234567", cut.Find("input[type=tel]").GetAttribute("value"));
	}

	[Fact]
	public async Task PickingAnotherCountry_MovesTheNumberToItsDialingCode()
	{
		string? value = null;
		string? country = null;
		IRenderedComponent<MokaPhoneInput> cut = Phone("SE", null, v => value = v,
			p => p.Add(x => x.CountryChanged, EventCallback.Factory.Create<string?>(this, c => country = c)));
		Type(cut, "070 123 45 67");

		MokaCountrySelect picker = cut.FindComponent<MokaCountrySelect>().Instance;
		await cut.InvokeAsync(() => picker.ValueChanged.InvokeAsync("NO"));

		Assert.Equal("NO", country);
		Assert.Equal("+47701234567", value);
		Assert.Equal("+47", cut.Find(".moka-country-value__dial").TextContent);
	}

	[Fact]
	public void ThePickerListsDialingCodes_AndSearchFindsThem()
	{
		IRenderedComponent<MokaPhoneInput> cut = Phone("SE", null, _ => { });

		cut.Find(".moka-phone-country [role=combobox]").Click();
		cut.Find(".moka-select-search").Input("44");

		IElement first = cut.FindAll("[role=option]")[0];
		Assert.Equal("United Kingdom", first.QuerySelector(".moka-country-option__name")!.TextContent);
		Assert.Equal("+44", first.QuerySelector(".moka-country-option__end")!.TextContent);
	}

	[Fact]
	public void Countries_LimitThePicker()
	{
		IRenderedComponent<MokaPhoneInput> cut = Phone("SE", null, _ => { }, p => p
			.Add(x => x.Countries, ["SE", "NO"])
			.Add(x => x.CountryLabel, "Land"));

		IElement picker = cut.Find(".moka-phone-country [role=combobox]");
		Assert.Equal("Land, Sweden +46", picker.GetAttribute("aria-label"));
		picker.Click();
		Assert.Equal(2, cut.FindAll("[role=option]").Count);
	}
}
