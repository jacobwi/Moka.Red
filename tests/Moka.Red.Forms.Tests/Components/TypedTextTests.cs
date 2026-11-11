using Bunit;
using Moka.Red.Forms.NumericField;
using Moka.Red.Forms.SearchInput;
using Moka.Red.Forms.TextField;

namespace Moka.Red.Forms.Tests.Components;

// A text box shows what was typed until its value changes another way. Writing the formatted value back on every
// render reverted text that doesn't parse yet and, raced against fast typing, put back an older value.
public class TypedTextTests : BunitContext
{
	public TypedTextTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ANumber_ThatDoesNotParseYet_StaysAsTyped()
	{
		IRenderedComponent<MokaNumericField<double>> cut = Render<MokaNumericField<double>>(p => p
			.Add(x => x.Value, 0));

		cut.Find("input").Input("1.");

		Assert.Equal("1.", cut.Find("input").GetAttribute("value"));
		Assert.Equal(1, cut.Instance.Value);
	}

	[Fact]
	public void ADebouncedField_ShowsTheTypedText_BeforeItReportsIt()
	{
		string? reported = null;
		IRenderedComponent<MokaTextField> cut = Render<MokaTextField>(p => p
			.Add(x => x.Value, "")
			.Add(x => x.DebounceDelay, 60_000)
			.Add(x => x.ValueChanged, value => reported = value));

		cut.Find("input").Input("abc");

		Assert.Equal("abc", cut.Find("input").GetAttribute("value"));
		Assert.Null(reported);
	}

	[Fact]
	public void AValueSetFromOutside_ReplacesTheTypedText()
	{
		IRenderedComponent<MokaNumericField<double>> cut = Render<MokaNumericField<double>>(p => p
			.Add(x => x.Value, 0));
		cut.Find("input").Input("1.");

		cut.Render(p => p.Add(x => x.Value, 5));

		Assert.Equal("5", cut.Find("input").GetAttribute("value"));
	}

	[Fact]
	public void TheParentHandingBackTheSameValue_KeepsTheTypedText()
	{
		IRenderedComponent<MokaNumericField<double>> cut = Render<MokaNumericField<double>>(p => p
			.Add(x => x.Value, 0));
		cut.Find("input").Input("1.50");

		cut.Render(p => p.Add(x => x.Value, 1.5));

		Assert.Equal("1.50", cut.Find("input").GetAttribute("value"));
	}

	[Fact]
	public void TheSearchBox_FollowsItsTyping()
	{
		IRenderedComponent<MokaSearchInput> cut = Render<MokaSearchInput>();

		cut.Find("input").Input("date");
		cut.Find("input").Input("datepicker");

		Assert.Equal("datepicker", cut.Find("input").GetAttribute("value"));
	}
}
