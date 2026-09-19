using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.Rating;

namespace Moka.Red.Forms.Tests.Components;

// The rating is one WAI-ARIA slider. The keys change the value in .NET; moka-keys.js only stops
// them scrolling the page, so these tests cover the slider's attributes, the keys and that wiring.
public class MokaRatingTests : BunitContext
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	public MokaRatingTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void IsOneSlider_WithItsValueAsStrings()
	{
		IRenderedComponent<MokaRating> cut = Render<MokaRating>(p => p
			.Add(x => x.Label, "Quality")
			.Add(x => x.Value, 3));

		IElement slider = cut.Find("[role=slider]");
		IElement label = cut.Find("label.moka-field-label");
		Assert.Equal("0", slider.GetAttribute("tabindex"));
		Assert.Equal("0", slider.GetAttribute("aria-valuemin"));
		Assert.Equal("5", slider.GetAttribute("aria-valuemax"));
		Assert.Equal("3", slider.GetAttribute("aria-valuenow"));
		Assert.Equal("3 of 5", slider.GetAttribute("aria-valuetext"));
		Assert.False(string.IsNullOrEmpty(label.Id));
		Assert.Equal(label.Id, slider.GetAttribute("aria-labelledby"));
		Assert.False(slider.HasAttribute("aria-readonly"));
		Assert.False(slider.HasAttribute("aria-disabled"));
	}

	// Each star used to be its own role="button" (or role="img" when read-only), named "3 of 5
	// stars" whatever the value was, and none of them could take focus.
	[Fact]
	public void TheStars_AreHiddenInsideTheSlider()
	{
		IRenderedComponent<MokaRating> cut = Render<MokaRating>(p => p.Add(x => x.Value, 2));

		Assert.Empty(cut.FindAll("[role=button], [role=img]"));
		IReadOnlyList<IElement> stars = cut.FindAll(".moka-rating-star");
		Assert.Equal(5, stars.Count);
		Assert.All(stars, star => Assert.Equal("true", star.GetAttribute("aria-hidden")));
		Assert.All(stars, star => Assert.NotNull(star.Closest("[role=slider]")));
	}

	[Fact]
	public void Unrated_ReadsAsNoRating_AndWithoutALabelTakesTheConsumersName()
	{
		IRenderedComponent<MokaRating> named = Render<MokaRating>(p => p.AddUnmatched("aria-label", "Rate this answer"));
		IRenderedComponent<MokaRating> unnamed = Render<MokaRating>();

		IElement slider = named.Find("[role=slider]");
		Assert.Equal("0", slider.GetAttribute("aria-valuenow"));
		Assert.Equal("No rating", slider.GetAttribute("aria-valuetext"));
		Assert.Equal("Rate this answer", slider.GetAttribute("aria-label"));
		Assert.False(slider.HasAttribute("aria-labelledby"));
		Assert.Equal("Rating", unnamed.Find("[role=slider]").GetAttribute("aria-label"));
	}

	[Fact]
	public async Task ArrowKeys_StepTheValueAndStopAtTheTop()
	{
		List<int> changes = [];
		IRenderedComponent<MokaRating> cut = Render<MokaRating>(p => p
			.Add(x => x.Value, 3)
			.Add(x => x.ValueChanged, value => changes.Add(value)));

		await PressAsync(cut, "ArrowRight");
		await PressAsync(cut, "ArrowUp");
		await PressAsync(cut, "ArrowRight");
		await PressAsync(cut, "ArrowLeft");
		await PressAsync(cut, "ArrowDown");

		// The press at 5 changes nothing, so it raises nothing.
		Assert.Equal([4, 5, 4, 3], changes);
		Assert.Equal("3", cut.Find("[role=slider]").GetAttribute("aria-valuenow"));
		Assert.Equal(3, cut.FindAll(".moka-rating-star--filled").Count);
	}

	[Fact]
	public async Task HomeAndEnd_JumpToTheEnds()
	{
		List<int> changes = [];
		IRenderedComponent<MokaRating> cut = Render<MokaRating>(p => p
			.Add(x => x.Value, 2)
			.Add(x => x.MaxValue, 10)
			.Add(x => x.ValueChanged, value => changes.Add(value)));

		await PressAsync(cut, "End");
		Assert.Equal("10", cut.Find("[role=slider]").GetAttribute("aria-valuenow"));

		await PressAsync(cut, "Home");
		Assert.Equal("0", cut.Find("[role=slider]").GetAttribute("aria-valuenow"));
		Assert.Equal([10, 0], changes);
	}

	// Without AllowClear a click cannot go back to 0, and neither can the keys.
	[Fact]
	public async Task WithoutAllowClear_TheKeysStopAtOne()
	{
		List<int> changes = [];
		IRenderedComponent<MokaRating> cut = Render<MokaRating>(p => p
			.Add(x => x.Value, 2)
			.Add(x => x.AllowClear, false)
			.Add(x => x.ValueChanged, value => changes.Add(value)));

		await PressAsync(cut, "Home");
		await PressAsync(cut, "ArrowLeft");

		Assert.Equal([1], changes);
		Assert.Equal("1", cut.Find("[role=slider]").GetAttribute("aria-valuenow"));
	}

	[Fact]
	public async Task ModifiedKeys_AreLeftAlone()
	{
		List<int> changes = [];
		IRenderedComponent<MokaRating> cut = Render<MokaRating>(p => p
			.Add(x => x.Value, 2)
			.Add(x => x.ValueChanged, value => changes.Add(value)));

		await cut.Find("[role=slider]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowRight", AltKey = true });

		Assert.Empty(changes);
		Assert.Equal("2", cut.Find("[role=slider]").GetAttribute("aria-valuenow"));
	}

	// Read-only: still a tab stop, so the value can be read, like a read-only field.
	[Fact]
	public async Task ReadOnly_KeepsItsTabStopButIgnoresKeysAndClicks()
	{
		List<int> changes = [];
		IRenderedComponent<MokaRating> cut = Render<MokaRating>(p => p
			.Add(x => x.Value, 4)
			.Add(x => x.ReadOnly, true)
			.Add(x => x.ValueChanged, value => changes.Add(value)));

		await PressAsync(cut, "ArrowLeft");
		await cut.FindAll(".moka-rating-star")[0].ClickAsync(new MouseEventArgs());

		IElement slider = cut.Find("[role=slider]");
		Assert.Equal("0", slider.GetAttribute("tabindex"));
		Assert.Equal("true", slider.GetAttribute("aria-readonly"));
		Assert.Equal("4", slider.GetAttribute("aria-valuenow"));
		Assert.Empty(changes);
	}

	[Fact]
	public async Task Disabled_LeavesTheTabOrderAndIgnoresKeys()
	{
		List<int> changes = [];
		IRenderedComponent<MokaRating> cut = Render<MokaRating>(p => p
			.Add(x => x.Value, 4)
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

		IRenderedComponent<MokaRating> cut = Render<MokaRating>(p => p.Add(x => x.Value, 1));

		JSRuntimeInvocation bind = keys.VerifyInvoke("preventKeys");
		ElementReference slider = Assert.IsType<ElementReference>(bind.Arguments[0]);
		Assert.Equal(cut.Find("[role=slider]").GetAttribute("blazor:elementReference"), slider.Id);

		Dictionary<string, object?> rule = Assert.Single(Assert.IsType<Dictionary<string, object?>[]>(bind.Arguments[1]));
		Assert.Null(rule["selector"]);
		Assert.Equal(["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "Home", "End"], (string[])rule["keys"]!);

		// The slider ignores these keys with Ctrl, Alt or Meta, so the browser keeps Alt+Left (back).
		Assert.Equal(true, rule["unlessModified"]);
	}

	// A read-only rating leaves the keys to the page. One that turns read-only hands them back.
	[Fact]
	public void ReadOnly_LeavesTheKeysToThePage()
	{
		BunitJSModuleInterop keys = JSInterop.SetupModule(KeysModule);

		Render<MokaRating>(p => p.Add(x => x.ReadOnly, true));
		Assert.Empty(keys.Invocations);

		IRenderedComponent<MokaRating> cut = Render<MokaRating>();
		cut.Render(p => p.Add(x => x.ReadOnly, true));

		IReadOnlyList<JSRuntimeInvocation> binds = keys.VerifyInvoke("preventKeys", 2);
		Assert.Single(Assert.IsType<Dictionary<string, object?>[]>(binds[0].Arguments[1]));
		Assert.Empty(Assert.IsType<Dictionary<string, object?>[]>(binds[1].Arguments[1]));
	}

	// The click used to write the Value parameter, so the parent's next render put its own
	// value back when it did not bind Value.
	[Fact]
	public async Task AParentRerender_WithTheSameValue_KeepsTheUsersChoice()
	{
		IRenderedComponent<MokaRating> cut = Render<MokaRating>(p => p.Add(x => x.Value, 4));

		await cut.FindAll(".moka-rating-star")[1].ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Value, 4));

		Assert.Equal(2, cut.FindAll(".moka-rating-star--filled").Count);

		// A value the parent has not passed before still wins.
		cut.Render(p => p.Add(x => x.Value, 5));

		Assert.Equal(5, cut.FindAll(".moka-rating-star--filled").Count);
	}

	private static Task PressAsync(IRenderedComponent<MokaRating> cut, string key) =>
		cut.Find("[role=slider]").KeyDownAsync(new KeyboardEventArgs { Key = key });
}
