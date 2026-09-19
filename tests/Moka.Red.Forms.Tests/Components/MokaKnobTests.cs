using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.Knob;

namespace Moka.Red.Forms.Tests.Components;

// The knob had role="slider" and a tab stop, but no key did anything. The keys change the value in
// .NET; moka-keys.js only stops them scrolling the page, so these tests cover the slider's
// attributes, the keys and that wiring.
public class MokaKnobTests : BunitContext
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	public MokaKnobTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void IsASlider_NamedByItsLabel_WithItsValueAsStrings()
	{
		IRenderedComponent<MokaKnob> cut = Render<MokaKnob>(p => p
			.Add(x => x.Label, "Gain")
			.Add(x => x.Value, 30d)
			.Add(x => x.Min, -10d)
			.Add(x => x.Max, 60d));

		IElement slider = cut.Find("[role=slider]");
		Assert.Equal("0", slider.GetAttribute("tabindex"));
		Assert.Equal("-10", slider.GetAttribute("aria-valuemin"));
		Assert.Equal("60", slider.GetAttribute("aria-valuemax"));
		Assert.Equal("30", slider.GetAttribute("aria-valuenow"));
		Assert.Equal(30d.ToString("F0", CultureInfo.CurrentCulture), slider.GetAttribute("aria-valuetext"));
		Assert.Equal("Gain", slider.GetAttribute("aria-label"));
		Assert.False(slider.HasAttribute("aria-disabled"));

		// The value and label drawn in the middle would be read a second time.
		Assert.Equal("true", cut.Find(".moka-knob__center").GetAttribute("aria-hidden"));
	}

	[Fact]
	public void WithoutALabel_TakesTheConsumersName()
	{
		IRenderedComponent<MokaKnob> named = Render<MokaKnob>(p => p.AddUnmatched("aria-label", "Master volume"));
		IRenderedComponent<MokaKnob> unnamed = Render<MokaKnob>();

		Assert.Equal("Master volume", named.Find("[role=slider]").GetAttribute("aria-label"));
		Assert.Equal("Knob", unnamed.Find("[role=slider]").GetAttribute("aria-label"));
	}

	[Fact]
	public void ValueText_UsesTheFormat_AndTheNumbersStayInvariant()
	{
		CultureInfo culture = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
		try
		{
			IRenderedComponent<MokaKnob> cut = Render<MokaKnob>(p => p
				.Add(x => x.Value, 0.25)
				.Add(x => x.Min, 0d)
				.Add(x => x.Max, 1d)
				.Add(x => x.Step, 0.05)
				.Add(x => x.Format, "P0"));

			IElement slider = cut.Find("[role=slider]");

			// Screen readers read what the knob shows. The ARIA numbers are plain numbers: "0,25"
			// is not a number to them.
			Assert.Equal(0.25.ToString("P0", CultureInfo.CurrentCulture), slider.GetAttribute("aria-valuetext"));
			Assert.Equal("0.25", slider.GetAttribute("aria-valuenow"));
			Assert.Equal("1", slider.GetAttribute("aria-valuemax"));
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
		}
	}

	[Fact]
	public async Task ArrowKeys_StepTheValue()
	{
		List<double> changes = [];
		IRenderedComponent<MokaKnob> cut = RenderKnob(10, changes);

		await PressAsync(cut, "ArrowRight");
		await PressAsync(cut, "ArrowUp");
		await PressAsync(cut, "ArrowLeft");
		await PressAsync(cut, "ArrowDown");
		await PressAsync(cut, "ArrowDown");

		Assert.Equal([11, 12, 11, 10, 9], changes);
		Assert.Equal("9", cut.Find("[role=slider]").GetAttribute("aria-valuenow"));
	}

	[Fact]
	public async Task PageKeys_MoveTenSteps_AndStopAtTheEnds()
	{
		List<double> changes = [];
		IRenderedComponent<MokaKnob> cut = RenderKnob(95, changes);

		await PressAsync(cut, "PageUp");
		await PressAsync(cut, "PageUp");
		await PressAsync(cut, "PageDown");
		await PressAsync(cut, "ArrowRight");

		// The second Page Up at 100 changes nothing, so it raises nothing.
		Assert.Equal([100, 90, 91], changes);
	}

	[Fact]
	public async Task HomeAndEnd_JumpToMinAndMax()
	{
		List<double> changes = [];
		IRenderedComponent<MokaKnob> cut = Render<MokaKnob>(p => p
			.Add(x => x.Value, 20d)
			.Add(x => x.Min, 10d)
			.Add(x => x.Max, 50d)
			.Add(x => x.ValueChanged, value => changes.Add(value)));

		await PressAsync(cut, "End");
		Assert.Equal("50", cut.Find("[role=slider]").GetAttribute("aria-valuenow"));

		await PressAsync(cut, "Home");
		await PressAsync(cut, "ArrowLeft");

		Assert.Equal("10", cut.Find("[role=slider]").GetAttribute("aria-valuenow"));
		Assert.Equal([50, 10], changes);
	}

	// 0.6 + 0.1 is 0.7 on the grid but 0.7000000000000001 in binary. That noise went to the parent.
	[Fact]
	public async Task FractionalSteps_LandOnTheGrid()
	{
		List<double> changes = [];
		IRenderedComponent<MokaKnob> cut = Render<MokaKnob>(p => p
			.Add(x => x.Value, 0.6)
			.Add(x => x.Min, 0d)
			.Add(x => x.Max, 1d)
			.Add(x => x.Step, 0.1)
			.Add(x => x.ValueChanged, value => changes.Add(value)));

		await PressAsync(cut, "ArrowRight");

		Assert.Equal([0.7], changes);
		Assert.Equal("0.7", cut.Find("[role=slider]").GetAttribute("aria-valuenow"));
	}

	[Fact]
	public async Task ModifiedKeys_AreLeftAlone()
	{
		List<double> changes = [];
		IRenderedComponent<MokaKnob> cut = RenderKnob(50, changes);
		IElement slider = cut.Find("[role=slider]");

		await slider.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowLeft", AltKey = true });
		await slider.KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight", CtrlKey = true });
		await slider.KeyDownAsync(new KeyboardEventArgs { Key = "End", MetaKey = true });

		Assert.Empty(changes);
		Assert.Equal("50", cut.Find("[role=slider]").GetAttribute("aria-valuenow"));
	}

	[Fact]
	public async Task Disabled_LeavesTheTabOrderAndIgnoresKeys()
	{
		List<double> changes = [];
		IRenderedComponent<MokaKnob> cut = Render<MokaKnob>(p => p
			.Add(x => x.Value, 40d)
			.Add(x => x.Disabled, true)
			.Add(x => x.ValueChanged, value => changes.Add(value)));

		await PressAsync(cut, "End");

		IElement slider = cut.Find("[role=slider]");
		Assert.False(slider.HasAttribute("tabindex"));
		Assert.Equal("true", slider.GetAttribute("aria-disabled"));
		Assert.Empty(changes);
	}

	[Fact]
	public void CancelsTheScrollOfTheKeysItHandles_OnTheSliderItself()
	{
		BunitJSModuleInterop keys = JSInterop.SetupModule(KeysModule);

		IRenderedComponent<MokaKnob> cut = Render<MokaKnob>(p => p.Add(x => x.Value, 5d));

		JSRuntimeInvocation bind = keys.VerifyInvoke("preventKeys");
		ElementReference slider = Assert.IsType<ElementReference>(bind.Arguments[0]);
		Assert.Equal(cut.Find("[role=slider]").GetAttribute("blazor:elementReference"), slider.Id);

		Dictionary<string, object?> rule = Assert.Single(Assert.IsType<Dictionary<string, object?>[]>(bind.Arguments[1]));
		Assert.Null(rule["selector"]);
		Assert.Equal(["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "PageUp", "PageDown", "Home", "End"],
			(string[])rule["keys"]!);

		// The knob ignores these keys with Ctrl, Alt or Meta, so the browser keeps Alt+Left (back).
		Assert.Equal(true, rule["unlessModified"]);
	}

	// A disabled knob leaves the keys to the page. One that turns disabled hands them back.
	[Fact]
	public void Disabled_LeavesTheKeysToThePage()
	{
		BunitJSModuleInterop keys = JSInterop.SetupModule(KeysModule);

		Render<MokaKnob>(p => p.Add(x => x.Disabled, true));
		Assert.Empty(keys.Invocations);

		IRenderedComponent<MokaKnob> cut = Render<MokaKnob>();
		cut.Render(p => p.Add(x => x.Disabled, true));

		IReadOnlyList<JSRuntimeInvocation> binds = keys.VerifyInvoke("preventKeys", 2);
		Assert.Single(Assert.IsType<Dictionary<string, object?>[]>(binds[0].Arguments[1]));
		Assert.Empty(Assert.IsType<Dictionary<string, object?>[]>(binds[1].Arguments[1]));
	}

	// Math.Clamp throws when its minimum is above its maximum, so a knob with Max below Min threw
	// on the first key.
	[Fact]
	public async Task MaxBelowMin_DoesNotThrow_AndTheKeysStayInTheRange()
	{
		double? changed = null;
		IRenderedComponent<MokaKnob> cut = Render<MokaKnob>(p => p
			.Add(x => x.Min, 100)
			.Add(x => x.Max, 0)
			.Add(x => x.Value, 50)
			.Add(x => x.ValueChanged, v => changed = v));

		await cut.Find("[role=slider]").KeyDownAsync(new KeyboardEventArgs { Key = "End" });

		Assert.Equal(100, changed);
	}

	// A Step of zero left the arrow and page keys doing nothing.
	[Fact]
	public async Task AZeroStep_StillLetsTheArrowsMoveTheValue()
	{
		double? changed = null;
		IRenderedComponent<MokaKnob> cut = Render<MokaKnob>(p => p
			.Add(x => x.Min, 0)
			.Add(x => x.Max, 200)
			.Add(x => x.Step, 0)
			.Add(x => x.Value, 100)
			.Add(x => x.ValueChanged, v => changed = v));

		await cut.Find("[role=slider]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });

		Assert.Equal(102, changed);
	}

	private IRenderedComponent<MokaKnob> RenderKnob(double value, List<double> changes) => Render<MokaKnob>(p => p
		.Add(x => x.Value, value)
		.Add(x => x.ValueChanged, v => changes.Add(v)));

	private static Task PressAsync(IRenderedComponent<MokaKnob> cut, string key) =>
		cut.Find("[role=slider]").KeyDownAsync(new KeyboardEventArgs { Key = key });
}
