using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.ColorWheel;
using Moka.Red.Forms.CreditCard;
using Moka.Red.Forms.Knob;
using Moka.Red.Forms.SearchInput;
using Moka.Red.Forms.SignaturePad;
using Moka.Red.Forms.Slider;
using Moka.Red.Forms.ToggleGroup;

namespace Moka.Red.Forms.Tests.Components;

// These inputs used to write the user's value into their own [Parameter]. Blazor passes every
// parameter again whenever the parent renders, so with a one-way value the parent's next render put
// the old value back and the user's input vanished. Each test changes the value through the UI,
// re-renders with the same value (what an unrelated parent render does), and then checks that a
// new value from the parent still wins.
public class OneWayBindingTests : BunitContext
{
	private static readonly string[] OnlyA = ["a"];
	private static readonly string[] OnlyC = ["c"];

	[Fact]
	public async Task ColorWheel_KeepsTheColor_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaColorWheel> cut = Render<MokaColorWheel>(p => p.Add(x => x.Value, "#ff0000"));

		await cut.Find(".moka-color-wheel-slider--hue").InputAsync(new ChangeEventArgs { Value = "120" });
		cut.Render(p => p.Add(x => x.Value, "#ff0000"));

		Assert.Equal("#00ff00", cut.Find(".moka-color-wheel-hex-input").GetAttribute("value"));
	}

	[Fact]
	public async Task ColorWheel_FollowsANewValueFromTheParent()
	{
		IRenderedComponent<MokaColorWheel> cut = Render<MokaColorWheel>(p => p.Add(x => x.Value, "#ff0000"));

		await cut.Find(".moka-color-wheel-slider--hue").InputAsync(new ChangeEventArgs { Value = "120" });
		cut.Render(p => p.Add(x => x.Value, "#0000ff"));

		Assert.Equal("#0000ff", cut.Find(".moka-color-wheel-hex-input").GetAttribute("value"));
	}

	// White is white at any hue and saturation. Parsing the hex a bound parent echoes back put the
	// saturation slider at 0 as the user dragged lightness to the top.
	[Fact]
	public async Task ColorWheel_KeepsItsSliders_WhenABoundParentEchoesItsOwnValue()
	{
		string value = "#ff0000";
		IRenderedComponent<MokaColorWheel> cut = Render<MokaColorWheel>(p => p
			.Add(x => x.Value, value)
			.Add(x => x.ValueChanged, EventCallback.Factory.Create<string>(this, v => value = v)));

		await cut.FindAll(".moka-color-wheel-slider")[2].InputAsync(new ChangeEventArgs { Value = "100" });
		cut.Render(p => p.Add(x => x.Value, value));

		Assert.Equal("#ffffff", value);
		Assert.Equal("100", cut.FindAll(".moka-color-wheel-slider")[1].GetAttribute("value"));
	}

	[Fact]
	public async Task Knob_KeepsTheValue_WhenTheParentPassesTheSameValue()
	{
		// The knob binds its keys through moka-keys.js on the first render.
		JSInterop.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaKnob> cut = Render<MokaKnob>(p => p.Add(x => x.Value, 10d));

		await cut.Find("[role=slider]").WheelAsync(new WheelEventArgs { DeltaY = -1 });
		cut.Render(p => p.Add(x => x.Value, 10d));

		Assert.Equal("11", cut.Find("[role=slider]").GetAttribute("aria-valuenow"));
	}

	[Fact]
	public async Task Knob_FollowsANewValueFromTheParent()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaKnob> cut = Render<MokaKnob>(p => p.Add(x => x.Value, 10d));

		await cut.Find("[role=slider]").WheelAsync(new WheelEventArgs { DeltaY = -1 });
		cut.Render(p => p.Add(x => x.Value, 50d));

		Assert.Equal("50", cut.Find("[role=slider]").GetAttribute("aria-valuenow"));
	}

	// The common case: OnSearch sets Loading, the parent renders, and the typed text went back to
	// the parent's stale Value.
	[Fact]
	public async Task SearchInput_KeepsTheTypedText_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaSearchInput> cut = Render<MokaSearchInput>(p => p.Add(x => x.Value, "a"));

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "ab" });
		cut.Render(p => p.Add(x => x.Value, "a").Add(x => x.Loading, true));

		Assert.Equal("ab", cut.Find("input").GetAttribute("value"));
	}

	[Fact]
	public async Task SearchInput_KeepsTheCleared_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaSearchInput> cut = Render<MokaSearchInput>(p => p.Add(x => x.Value, "a"));

		await cut.Find(".moka-search-clear").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Value, "a"));

		Assert.Equal("", cut.Find("input").GetAttribute("value"));
	}

	[Fact]
	public async Task SearchInput_FollowsANewValueFromTheParent()
	{
		IRenderedComponent<MokaSearchInput> cut = Render<MokaSearchInput>(p => p.Add(x => x.Value, "a"));

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "ab" });
		cut.Render(p => p.Add(x => x.Value, "z"));

		Assert.Equal("z", cut.Find("input").GetAttribute("value"));
	}

	[Fact]
	public async Task SignaturePad_KeepsThePlaceholderHidden_WhenTheParentPassesTheSameValue()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaSignaturePad> cut = Render<MokaSignaturePad>(p => p.Add(x => x.Value, (string?)null));

		await cut.InvokeAsync(() => cut.Instance.OnSignatureChanged("data:image/png;base64,AAAA"));
		Assert.Empty(cut.FindAll(".moka-signature-pad__placeholder"));

		cut.Render(p => p.Add(x => x.Value, (string?)null));

		Assert.Empty(cut.FindAll(".moka-signature-pad__placeholder"));
	}

	[Fact]
	public async Task SignaturePad_FollowsANewValueFromTheParent()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaSignaturePad> cut = Render<MokaSignaturePad>(p => p.Add(x => x.Value, (string?)null));

		await cut.InvokeAsync(() => cut.Instance.OnSignatureChanged("data:image/png;base64,AAAA"));
		cut.Render(p => p.Add(x => x.Value, ""));

		Assert.Single(cut.FindAll(".moka-signature-pad__placeholder"));
	}

	[Fact]
	public async Task Slider_KeepsTheValue_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaSlider> cut = Render<MokaSlider>(p => p.Add(x => x.Value, 50d));

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "70" });
		cut.Render(p => p.Add(x => x.Value, 50d));

		Assert.Equal("70", cut.Find("input").GetAttribute("value"));
		Assert.Equal("70", cut.Find(".moka-slider-value-label").TextContent);
	}

	[Fact]
	public async Task Slider_FollowsANewValueFromTheParent()
	{
		IRenderedComponent<MokaSlider> cut = Render<MokaSlider>(p => p.Add(x => x.Value, 50d));

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "70" });
		cut.Render(p => p.Add(x => x.Value, 30d));

		Assert.Equal("30", cut.Find("input").GetAttribute("value"));
	}

	[Fact]
	public async Task RangeSlider_KeepsBothEnds_WhenTheParentPassesTheSameValues()
	{
		IRenderedComponent<MokaRangeSlider> cut = RenderRangeSlider();

		await cut.Find(".moka-range-slider-input--start").InputAsync(new ChangeEventArgs { Value = "20" });
		await cut.Find(".moka-range-slider-input--end").InputAsync(new ChangeEventArgs { Value = "80" });
		cut.Render(p => p.Add(x => x.ValueStart, 10d).Add(x => x.ValueEnd, 90d));

		Assert.Equal("20", cut.Find(".moka-range-slider-input--start").GetAttribute("value"));
		Assert.Equal("80", cut.Find(".moka-range-slider-input--end").GetAttribute("value"));
	}

	[Fact]
	public async Task RangeSlider_FollowsNewValuesFromTheParent()
	{
		IRenderedComponent<MokaRangeSlider> cut = RenderRangeSlider();

		await cut.Find(".moka-range-slider-input--start").InputAsync(new ChangeEventArgs { Value = "20" });
		await cut.Find(".moka-range-slider-input--end").InputAsync(new ChangeEventArgs { Value = "80" });
		cut.Render(p => p.Add(x => x.ValueStart, 30d).Add(x => x.ValueEnd, 60d));

		Assert.Equal("30", cut.Find(".moka-range-slider-input--start").GetAttribute("value"));
		Assert.Equal("60", cut.Find(".moka-range-slider-input--end").GetAttribute("value"));
	}

	[Fact]
	public async Task ToggleGroup_KeepsTheChoice_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaToggleGroup> cut = RenderToggleGroup(p => p.Add(x => x.Value, "a"));

		await cut.FindAll("button")[1].ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Value, "a"));

		Assert.Equal(["false", "true", "false"], Pressed(cut));
	}

	[Fact]
	public async Task ToggleGroup_FollowsANewValueFromTheParent()
	{
		IRenderedComponent<MokaToggleGroup> cut = RenderToggleGroup(p => p.Add(x => x.Value, "a"));

		await cut.FindAll("button")[1].ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Value, "c"));

		Assert.Equal(["false", "false", "true"], Pressed(cut));
	}

	// A new array with the same items, as an inline Values="@(new[] { ... })" gives on every render.
	[Fact]
	public async Task MultiToggleGroup_KeepsTheChoice_WhenTheParentPassesTheSameValues()
	{
		IRenderedComponent<MokaToggleGroup> cut = RenderToggleGroup(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.Values, OnlyA));

		await cut.FindAll("button")[1].ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Values, OnlyA.ToArray()));

		Assert.Equal(["true", "true", "false"], Pressed(cut));
	}

	[Fact]
	public async Task MultiToggleGroup_FollowsNewValuesFromTheParent()
	{
		IRenderedComponent<MokaToggleGroup> cut = RenderToggleGroup(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.Values, OnlyA));

		await cut.FindAll("button")[1].ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Values, OnlyC));

		Assert.Equal(["false", "false", "true"], Pressed(cut));
	}

	[Fact]
	public void MultiToggleGroup_SeesAParentEditingItsListInPlace()
	{
		List<string> selected = ["a"];
		IRenderedComponent<MokaToggleGroup> cut = RenderToggleGroup(p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.Values, selected));

		selected.Add("c");
		cut.Render(p => p.Add(x => x.Values, selected));

		Assert.Equal(["true", "false", "true"], Pressed(cut));
	}

	[Fact]
	public async Task CreditCard_KeepsWhatWasTyped_WhenTheParentPassesTheSameValues()
	{
		IRenderedComponent<MokaCreditCardInput> cut = Render<MokaCreditCardInput>();

		await NumberInput(cut).InputAsync(new ChangeEventArgs { Value = "4242424242424242" });
		await cut.Find("input[placeholder='MM/YY']").InputAsync(new ChangeEventArgs { Value = "1226" });
		await cut.Find("input[placeholder='CVV']").InputAsync(new ChangeEventArgs { Value = "123" });
		await cut.Find("input[placeholder='Cardholder name']").InputAsync(new ChangeEventArgs { Value = "Ada Lovelace" });
		cut.Render(p => p
			.Add(x => x.CardNumber, "")
			.Add(x => x.ExpiryDate, "")
			.Add(x => x.Cvv, "")
			.Add(x => x.CardholderName, ""));

		Assert.Equal("4242 4242 4242 4242", NumberInput(cut).GetAttribute("value"));
		Assert.Equal("12/26", cut.Find("input[placeholder='MM/YY']").GetAttribute("value"));
		Assert.Equal("123", cut.Find("input[placeholder='CVV']").GetAttribute("value"));
		Assert.Equal("Ada Lovelace", cut.Find("input[placeholder='Cardholder name']").GetAttribute("value"));
	}

	[Fact]
	public async Task CreditCard_FollowsANewNumberFromTheParent()
	{
		IRenderedComponent<MokaCreditCardInput> cut = Render<MokaCreditCardInput>();

		await NumberInput(cut).InputAsync(new ChangeEventArgs { Value = "4242424242424242" });
		cut.Render(p => p.Add(x => x.CardNumber, "5555 5555 5555 4444"));

		Assert.Equal("5555 5555 5555 4444", NumberInput(cut).GetAttribute("value"));
	}

	// The brand used to come only from typing, so a number the parent filled in showed none.
	[Fact]
	public void CreditCard_ShowsTheBrandOfANumberFromTheParent()
	{
		IRenderedComponent<MokaCreditCardInput> cut = Render<MokaCreditCardInput>(p => p
			.Add(x => x.CardNumber, "5555 5555 5555 4444"));

		Assert.Equal("MC", cut.Find(".moka-creditcard-type-badge").TextContent);
	}

	private IRenderedComponent<MokaRangeSlider> RenderRangeSlider() => Render<MokaRangeSlider>(p => p
		.Add(x => x.ValueStart, 10d)
		.Add(x => x.ValueEnd, 90d));

	private IRenderedComponent<MokaToggleGroup> RenderToggleGroup(
		Action<ComponentParameterCollectionBuilder<MokaToggleGroup>> parameters) => Render<MokaToggleGroup>(p =>
	{
		parameters(p);
		p.AddChildContent<MokaToggleGroupItem>(item => item.Add(x => x.Value, "a").Add(x => x.Text, "A"))
			.AddChildContent<MokaToggleGroupItem>(item => item.Add(x => x.Value, "b").Add(x => x.Text, "B"))
			.AddChildContent<MokaToggleGroupItem>(item => item.Add(x => x.Value, "c").Add(x => x.Text, "C"));
	});

	private static IEnumerable<string?> Pressed(IRenderedComponent<MokaToggleGroup> cut) =>
		cut.FindAll("button").Select(b => b.GetAttribute("aria-pressed"));

	private static IElement NumberInput(IRenderedComponent<MokaCreditCardInput> cut) =>
		cut.Find(".moka-creditcard-input--number");
}
