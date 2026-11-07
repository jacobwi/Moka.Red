using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Forms.ColorInput;
using Moka.Red.Forms.NumericField;
using Moka.Red.Forms.PasswordField;
using Moka.Red.Forms.TextArea;

namespace Moka.Red.Forms.Tests.Components;

public class DebounceDelayTests : BunitContext
{
	private const int Delay = 300;

	public DebounceDelayTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task TextArea_ReportsTheLastValueOnceAfterTheDelay()
	{
		List<string> values = [];
		IRenderedComponent<MokaTextArea> cut = Render<MokaTextArea>(p => p
			.Add(x => x.DebounceDelay, Delay)
			.Add(x => x.ValueChanged, value => values.Add(value)));

		await TypeTwice(() => cut.Find("textarea"), "ab", "abc");

		Assert.Empty(values);
		cut.WaitForAssertion(() => Assert.Equal(["abc"], values), TimeSpan.FromSeconds(5));
	}

	[Fact]
	public async Task PasswordField_ReportsTheLastValueOnceAfterTheDelay()
	{
		List<string> values = [];
		IRenderedComponent<MokaPasswordField> cut = Render<MokaPasswordField>(p => p
			.Add(x => x.DebounceDelay, Delay)
			.Add(x => x.ValueChanged, value => values.Add(value)));

		await TypeTwice(() => cut.Find("input"), "ab", "abc");

		Assert.Empty(values);
		cut.WaitForAssertion(() => Assert.Equal(["abc"], values), TimeSpan.FromSeconds(5));
	}

	[Fact]
	public async Task ColorInput_ReportsTheLastValueOnceAfterTheDelay()
	{
		List<string> values = [];
		IRenderedComponent<MokaColorInput> cut = Render<MokaColorInput>(p => p
			.Add(x => x.DebounceDelay, Delay)
			.Add(x => x.ValueChanged, value => values.Add(value)));

		await TypeTwice(() => cut.Find("input"), "#33", "#336699");

		Assert.Empty(values);
		cut.WaitForAssertion(() => Assert.Equal(["#336699"], values), TimeSpan.FromSeconds(5));
	}

	[Fact]
	public async Task ColorInput_RedrawsItsSwatchAfterTheDelay()
	{
		// The event's own render has already happened by the time the timer fires.
		IRenderedComponent<MokaColorInput> cut = Render<MokaColorInput>(p => p
			.Add(x => x.DebounceDelay, 1)
			.Add(x => x.ShowPreview, true));

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "#336699" });

		cut.WaitForAssertion(
			() => Assert.Contains("#336699", cut.Find(".moka-color-input__swatch").GetAttribute("style"), StringComparison.Ordinal),
			TimeSpan.FromSeconds(5));
	}

	[Fact]
	public async Task NumericField_ReportsTheLastValueOnceAfterTheDelay()
	{
		List<int> values = [];
		IRenderedComponent<MokaNumericField<int>> cut = Render<MokaNumericField<int>>(p => p
			.Add(x => x.DebounceDelay, Delay)
			.Add(x => x.ValueChanged, value => values.Add(value)));

		await TypeTwice(() => cut.Find("input"), "1", "12");

		Assert.Empty(values);
		cut.WaitForAssertion(() => Assert.Equal([12], values), TimeSpan.FromSeconds(5));
	}

	[Fact]
	public async Task TextArea_WithoutADelay_ReportsEveryKeystroke()
	{
		List<string> values = [];
		IRenderedComponent<MokaTextArea> cut = Render<MokaTextArea>(p => p
			.Add(x => x.ValueChanged, value => values.Add(value)));

		await TypeTwice(() => cut.Find("textarea"), "ab", "abc");

		Assert.Equal(["ab", "abc"], values);
	}

	private static async Task TypeTwice(Func<IElement> element, string first, string second)
	{
		await element().InputAsync(new ChangeEventArgs { Value = first });
		await element().InputAsync(new ChangeEventArgs { Value = second });
	}
}
