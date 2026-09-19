using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Forms.CreditCard;

namespace Moka.Red.Forms.Tests.Components;

public class MokaCreditCardInputTests : BunitContext
{
	private const string Number = ".moka-creditcard-input--number";
	private const string Expiry = "input[autocomplete='cc-exp']";
	private const string Cvv = "input[autocomplete='cc-csc']";
	private const string Name = "input[autocomplete='cc-name']";

	// A letter typed into a digit box is dropped, which leaves the value the box last rendered, so
	// Blazor sent the browser nothing and the letter stayed on screen. The box has to render the raw
	// text once and then the kept value, which is the update that clears it.
	[Theory]
	[InlineData(Number, "4242", "4242a")]
	[InlineData(Expiry, "12/2", "12/2x")]
	[InlineData(Cvv, "12", "12z")]
	public async Task ALetterTypedIntoADigitBox_IsClearedFromIt(string box, string typed, string withLetter)
	{
		IRenderedComponent<MokaCreditCardInput> cut = Render<MokaCreditCardInput>();
		await cut.Find(box).InputAsync(new ChangeEventArgs { Value = typed });

		List<string?> rendered = [];
		cut.OnMarkupUpdated += (_, _) => rendered.Add(cut.Find(box).GetAttribute("value"));
		await cut.Find(box).InputAsync(new ChangeEventArgs { Value = withLetter });

		Assert.Contains(withLetter, rendered);
		Assert.Equal(typed, rendered[^1]);
	}

	[Fact]
	public void EachBox_HasAName_AndTheLabelNamesTheGroup()
	{
		IRenderedComponent<MokaCreditCardInput> cut = Render<MokaCreditCardInput>(p => p.Add(x => x.Label, "Payment card"));

		IElement root = cut.Find(".moka-creditcard");
		Assert.Equal("group", root.GetAttribute("role"));
		Assert.Equal(cut.Find(".moka-creditcard-label").Id, root.GetAttribute("aria-labelledby"));
		Assert.Equal("Card number", cut.Find(Number).GetAttribute("aria-label"));
		Assert.Equal("Expiry date (MM/YY)", cut.Find(Expiry).GetAttribute("aria-label"));
		Assert.Equal("Security code (CVV)", cut.Find(Cvv).GetAttribute("aria-label"));
		Assert.Equal("Cardholder name", cut.Find(Name).GetAttribute("aria-label"));
	}

	[Fact]
	public void IdAndExtraAttributes_LandOnTheRoot()
	{
		IRenderedComponent<MokaCreditCardInput> cut = Render<MokaCreditCardInput>(p => p
			.Add(x => x.Id, "checkout-card")
			.AddUnmatched("data-test", "card"));

		IElement root = cut.Find(".moka-creditcard");
		Assert.Equal("checkout-card", root.Id);
		Assert.Equal("card", root.GetAttribute("data-test"));
	}

	// An Amex code has four digits. Moving to another brand kept all four in a three digit box.
	[Fact]
	public async Task LeavingAmex_TrimsTheSecurityCode()
	{
		List<string> codes = [];
		IRenderedComponent<MokaCreditCardInput> cut = Render<MokaCreditCardInput>(p => p
			.Add(x => x.CvvChanged, code => codes.Add(code)));

		await cut.Find(Number).InputAsync(new ChangeEventArgs { Value = "3714" });
		await cut.Find(Cvv).InputAsync(new ChangeEventArgs { Value = "1234" });
		await cut.Find(Number).InputAsync(new ChangeEventArgs { Value = "4242" });

		Assert.Equal("123", cut.Find(Cvv).GetAttribute("value"));
		Assert.Equal("3", cut.Find(Cvv).GetAttribute("maxlength"));
		Assert.Equal(["1234", "123"], codes);
	}

	// The length was capped with the brand of the number being replaced, so a Visa pasted over an
	// Amex lost its last digit.
	[Fact]
	public async Task AVisaPastedOverAnAmex_KeepsAllSixteenDigits()
	{
		IRenderedComponent<MokaCreditCardInput> cut = Render<MokaCreditCardInput>();

		await cut.Find(Number).InputAsync(new ChangeEventArgs { Value = "371449635398431" });
		await cut.Find(Number).InputAsync(new ChangeEventArgs { Value = "4242424242424242" });

		Assert.Equal("4242 4242 4242 4242", cut.Find(Number).GetAttribute("value"));
	}

	[Fact]
	public async Task ASlashTypedAfterTheMonth_Stays()
	{
		List<string> dates = [];
		IRenderedComponent<MokaCreditCardInput> cut = Render<MokaCreditCardInput>(p => p
			.Add(x => x.ExpiryDateChanged, date => dates.Add(date)));

		await cut.Find(Expiry).InputAsync(new ChangeEventArgs { Value = "12" });
		await cut.Find(Expiry).InputAsync(new ChangeEventArgs { Value = "12/" });
		Assert.Equal("12/", cut.Find(Expiry).GetAttribute("value"));

		// A letter after the slash goes, the slash stays.
		await cut.Find(Expiry).InputAsync(new ChangeEventArgs { Value = "12/q" });
		Assert.Equal("12/", cut.Find(Expiry).GetAttribute("value"));

		await cut.Find(Expiry).InputAsync(new ChangeEventArgs { Value = "12/3" });

		Assert.Equal("12/3", cut.Find(Expiry).GetAttribute("value"));
		Assert.Equal(["12", "12/", "12/", "12/3"], dates);
	}
}
