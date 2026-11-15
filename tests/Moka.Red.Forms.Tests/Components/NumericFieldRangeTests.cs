using AngleSharp.Dom;
using Bunit;
using Moka.Red.Forms.NumericField;

namespace Moka.Red.Forms.Tests.Components;

// Min and Max hold the reported value in range, and Step makes the arrow keys step it. They were declared and never read.
public class NumericFieldRangeTests : BunitContext
{
	private const string ModulePath = "./_content/Moka.Red.Forms/NumericField/MokaNumericField.razor.js";

	public NumericFieldRangeTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ANumberAboveMax_IsReportedAsMax_AndTheBoxKeepsTheTypingUntilBlur()
	{
		int reported = 0;
		IRenderedComponent<MokaNumericField<int>> cut = Render<MokaNumericField<int>>(p => p
			.Add(x => x.Max, 8192)
			.Add(x => x.ValueChanged, value => reported = value));

		cut.Find("input").Input("99999");

		Assert.Equal(8192, reported);
		Assert.Equal("99999", cut.Find("input").GetAttribute("value"));

		cut.Find("input").Blur();

		Assert.Equal("8192", cut.Find("input").GetAttribute("value"));
	}

	[Fact]
	public void ANumberBelowMin_IsReportedAsMin()
	{
		int reported = 0;
		IRenderedComponent<MokaNumericField<int>> cut = Render<MokaNumericField<int>>(p => p
			.Add(x => x.Min, 10)
			.Add(x => x.ValueChanged, value => reported = value));

		cut.Find("input").Input("5");

		Assert.Equal(10, reported);
	}

	[Fact]
	public void ANumberInRange_IsReportedAsTyped_AndBlurLeavesTheText()
	{
		decimal reported = 0;
		IRenderedComponent<MokaNumericField<decimal>> cut = Render<MokaNumericField<decimal>>(p => p
			.Add(x => x.Min, 1m)
			.Add(x => x.Max, 100m)
			.Add(x => x.ValueChanged, value => reported = value));

		cut.Find("input").Input("12.50");
		cut.Find("input").Blur();

		Assert.Equal(12.5m, reported);
		Assert.Equal("12.50", cut.Find("input").GetAttribute("value"));
	}

	// Without the struct constraint an unset Min on an int field reads as 0, so only a passed Min may clamp.
	[Fact]
	public void WithoutMin_AnIntFieldTakesNegativeNumbers()
	{
		int reported = 0;
		IRenderedComponent<MokaNumericField<int>> cut = Render<MokaNumericField<int>>(p => p
			.Add(x => x.Max, 100)
			.Add(x => x.ValueChanged, value => reported = value));

		cut.Find("input").Input("-40");

		Assert.Equal(-40, reported);
	}

	[Fact]
	public void AnEmptyBox_OfATypeThatCantBeEmpty_IsReportedAsMin_AndShowsItOnBlur()
	{
		int reported = 0;
		IRenderedComponent<MokaNumericField<int>> cut = Render<MokaNumericField<int>>(p => p
			.Add(x => x.Min, 5)
			.Add(x => x.ValueChanged, value => reported = value));

		cut.Find("input").Input("8");
		cut.Find("input").Input(string.Empty);

		Assert.Equal(5, reported);

		cut.Find("input").Blur();

		Assert.Equal("5", cut.Find("input").GetAttribute("value"));
	}

	[Fact]
	public void AnEmptyBox_OfANullableType_StaysNullWithMin()
	{
		int? reported = 3;
		IRenderedComponent<MokaNumericField<int?>> cut = Render<MokaNumericField<int?>>(p => p
			.Add(x => x.Value, 3)
			.Add(x => x.Min, 1)
			.Add(x => x.ValueChanged, value => reported = value));

		cut.Find("input").Input(string.Empty);

		Assert.Null(reported);
	}

	// The numeric keypad has no decimal point on some phones.
	[Fact]
	public void ATypeWithFractions_AsksForTheDecimalKeypad_AndAWholeNumberTypeForTheNumericOne()
	{
		Assert.Equal("decimal", Render<MokaNumericField<decimal>>().Find("input").GetAttribute("inputmode"));
		Assert.Equal("decimal", Render<MokaNumericField<double?>>().Find("input").GetAttribute("inputmode"));
		Assert.Equal("decimal", Render<MokaNumericField<Half>>().Find("input").GetAttribute("inputmode"));
		Assert.Equal("numeric", Render<MokaNumericField<int>>().Find("input").GetAttribute("inputmode"));
		Assert.Equal("numeric", Render<MokaNumericField<long?>>().Find("input").GetAttribute("inputmode"));
	}

	[Fact]
	public void WithoutStep_TheInputIsAPlainTextBox_AndLoadsNoScript()
	{
		IRenderedComponent<MokaNumericField<int>> cut = Render<MokaNumericField<int>>(p => p
			.Add(x => x.Min, 1)
			.Add(x => x.Max, 10));

		IElement input = cut.Find("input");
		Assert.Null(input.GetAttribute("role"));
		Assert.Null(input.GetAttribute("aria-valuemin"));
		Assert.DoesNotContain(JSInterop.Invocations, call => call.Identifier == "bindSteps");
	}

	[Fact]
	public void WithStep_TheInputIsASpinButton_AndTheScriptHearsItsArrowKeys()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("bindSteps", _ => true).SetVoidResult();

		IRenderedComponent<MokaNumericField<int>> cut = Render<MokaNumericField<int>>(p => p
			.Add(x => x.Value, 4)
			.Add(x => x.Min, 1)
			.Add(x => x.Max, 10)
			.Add(x => x.Step, 1));

		IElement input = cut.Find("input");
		Assert.Equal("spinbutton", input.GetAttribute("role"));
		Assert.Equal("4", input.GetAttribute("aria-valuenow"));
		Assert.Equal("1", input.GetAttribute("aria-valuemin"));
		Assert.Equal("10", input.GetAttribute("aria-valuemax"));
		module.VerifyInvoke("bindSteps");
	}

	[Fact]
	public void TakingStepAway_UnbindsTheArrowKeys()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("bindSteps", _ => true).SetVoidResult();
		module.SetupVoid("unbindSteps", _ => true).SetVoidResult();
		IRenderedComponent<MokaNumericField<int?>> cut = Render<MokaNumericField<int?>>(p => p.Add(x => x.Step, 1));

		cut.Render(p => p.Add(x => x.Step, null));

		module.VerifyInvoke("unbindSteps");
		Assert.Null(cut.Find("input").GetAttribute("role"));
	}

	[Fact]
	public void AStepThatIsntAboveZero_StepsNothing()
	{
		IRenderedComponent<MokaNumericField<int>> cut = Render<MokaNumericField<int>>(p => p
			.Add(x => x.Value, 4)
			.Add(x => x.Step, 0));

		cut.InvokeAsync(() => cut.Instance.StepBy(1));

		Assert.Null(cut.Find("input").GetAttribute("role"));
		Assert.Equal("4", cut.Find("input").GetAttribute("value"));
	}

	[Fact]
	public void ArrowUpAndDown_StepTheValueWithinMinAndMax()
	{
		int reported = 0;
		IRenderedComponent<MokaNumericField<int>> cut = Render<MokaNumericField<int>>(p => p
			.Add(x => x.Min, 0)
			.Add(x => x.Max, 10)
			.Add(x => x.Step, 3)
			.Add(x => x.ValueChanged, value => reported = value));

		cut.Find("input").Input("6");
		cut.InvokeAsync(() => cut.Instance.StepBy(1));
		Assert.Equal(9, reported);
		Assert.Equal("9", cut.Find("input").GetAttribute("value"));

		cut.InvokeAsync(() => cut.Instance.StepBy(1));
		Assert.Equal(10, reported);

		cut.InvokeAsync(() => cut.Instance.StepBy(-1));
		Assert.Equal(7, reported);
	}

	[Fact]
	public void AStep_StartsFromWhatIsTyped()
	{
		int reported = 0;
		IRenderedComponent<MokaNumericField<int>> cut = Render<MokaNumericField<int>>(p => p
			.Add(x => x.Step, 5)
			.Add(x => x.ValueChanged, value => reported = value));

		cut.Find("input").Input("20");
		cut.InvokeAsync(() => cut.Instance.StepBy(-1));

		Assert.Equal(15, reported);
	}

	[Fact]
	public void AnEmptyBox_StepsFromZero_IntoRange()
	{
		int? reported = null;
		IRenderedComponent<MokaNumericField<int?>> cut = Render<MokaNumericField<int?>>(p => p
			.Add(x => x.Min, 5)
			.Add(x => x.Step, 1)
			.Add(x => x.ValueChanged, value => reported = value));

		cut.InvokeAsync(() => cut.Instance.StepBy(1));

		Assert.Equal(5, reported);
	}

	[Fact]
	public void TextThatIsntANumber_IsLeftAlone()
	{
		IRenderedComponent<MokaNumericField<int>> cut = Render<MokaNumericField<int>>(p => p.Add(x => x.Step, 1));

		cut.Find("input").Input("abc");
		cut.InvokeAsync(() => cut.Instance.StepBy(1));

		Assert.Equal("abc", cut.Find("input").GetAttribute("value"));
	}

	[Fact]
	public void DecimalSteps_AreExact()
	{
		decimal reported = 0;
		IRenderedComponent<MokaNumericField<decimal>> cut = Render<MokaNumericField<decimal>>(p => p
			.Add(x => x.Step, 0.1m)
			.Add(x => x.ValueChanged, value => reported = value));

		for (int i = 0; i < 3; i++)
		{
			cut.InvokeAsync(() => cut.Instance.StepBy(1));
		}

		Assert.Equal(0.3m, reported);
	}

	[Fact]
	public void AStepPastTheEndOfTheType_LeavesTheValue()
	{
		IRenderedComponent<MokaNumericField<uint>> unsigned = Render<MokaNumericField<uint>>(p => p
			.Add(x => x.Value, 0u)
			.Add(x => x.Step, 1u));
		unsigned.InvokeAsync(() => unsigned.Instance.StepBy(-1));

		IRenderedComponent<MokaNumericField<int>> signed = Render<MokaNumericField<int>>(p => p
			.Add(x => x.Value, int.MaxValue)
			.Add(x => x.Step, 1));
		signed.InvokeAsync(() => signed.Instance.StepBy(1));

		Assert.Equal("0", unsigned.Find("input").GetAttribute("value"));
		Assert.Equal("2147483647", signed.Find("input").GetAttribute("value"));
	}

	[Fact]
	public void ADisabledOrReadOnlyField_DoesntStep()
	{
		bool reported = false;
		IRenderedComponent<MokaNumericField<int>> disabled = Render<MokaNumericField<int>>(p => p
			.Add(x => x.Value, 1)
			.Add(x => x.Step, 1)
			.Add(x => x.Disabled, true)
			.Add(x => x.ValueChanged, _ => reported = true));
		IRenderedComponent<MokaNumericField<int>> readOnly = Render<MokaNumericField<int>>(p => p
			.Add(x => x.Value, 1)
			.Add(x => x.Step, 1)
			.Add(x => x.ReadOnly, true)
			.Add(x => x.ValueChanged, _ => reported = true));

		disabled.InvokeAsync(() => disabled.Instance.StepBy(1));
		readOnly.InvokeAsync(() => readOnly.Instance.StepBy(1));

		Assert.False(reported);
	}
}
