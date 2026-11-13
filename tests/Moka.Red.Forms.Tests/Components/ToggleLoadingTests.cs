using AngleSharp.Dom;
using Bunit;
using Moka.Red.Forms.Checkbox;
using Moka.Red.Forms.Switch;
using Moka.Red.Icons;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// A toggle that saves to a server needs a busy state that keeps focus and won't toggle again. A native checkbox flips
// itself before any handler runs, so the click's default action is what has to be cancelled: then the box never
// changes and no change event reaches .NET. bUnit can't run the browser's default action, so the flag is checked here
// and the real click is checked in a browser.
public sealed class ToggleLoadingTests : BunitContext
{
	[Fact]
	public void Switch_Loading_CancelsTheClick_AndSaysItIsBusy_WithoutDisablingIt()
	{
		IRenderedComponent<MokaSwitch> cut = Render<MokaSwitch>(p => p
			.Add(x => x.Value, true)
			.Add(x => x.Loading, true)
			.Add(x => x.Label, "Sync"));
		IElement input = cut.Find("input");

		Assert.True(input.HasAttribute("blazor:onclick:preventDefault"));
		Assert.Equal("true", input.GetAttribute("aria-busy"));
		Assert.False(input.HasAttribute("disabled"));
		Assert.Equal("switch", input.GetAttribute("role"));
		Assert.Contains("moka-switch--loading", cut.Find(".moka-switch").ClassList);

		IElement spinner = cut.Find(".moka-switch-thumb svg");
		Assert.Contains("moka-spin", spinner.ClassList);
		Assert.Equal(MokaIcons.Status.Spinner.SvgPath, spinner.QuerySelector("path")!.GetAttribute("d"));
	}

	[Fact]
	public void Checkbox_Loading_CancelsTheClick_AndDrawsTheSpinnerInTheBox()
	{
		IRenderedComponent<MokaCheckbox> cut = Render<MokaCheckbox>(p => p
			.Add(x => x.Value, true)
			.Add(x => x.Loading, true)
			.Add(x => x.Label, "Subscribed"));
		IElement input = cut.Find("input");

		Assert.True(input.HasAttribute("blazor:onclick:preventDefault"));
		Assert.Equal("true", input.GetAttribute("aria-busy"));
		Assert.False(input.HasAttribute("disabled"));
		Assert.True(input.HasAttribute("checked"));

		IReadOnlyList<IElement> drawn = cut.FindAll(".moka-checkbox-box svg");
		Assert.Single(drawn);
		Assert.Contains("moka-spin", drawn[0].ClassList);
		Assert.Contains("moka-checkbox--checked", cut.Find(".moka-checkbox").ClassList);
	}

	[Fact]
	public void NotLoading_TheTogglesKeepTheirMarkup()
	{
		IRenderedComponent<MokaSwitch> toggle = Render<MokaSwitch>(p => p.Add(x => x.Value, true));
		IRenderedComponent<MokaCheckbox> box = Render<MokaCheckbox>(p => p.Add(x => x.Value, true));

		foreach (IElement input in new[] { toggle.Find("input"), box.Find("input") })
		{
			Assert.False(input.HasAttribute("blazor:onclick:preventDefault"));
			Assert.False(input.HasAttribute("aria-busy"));
		}

		Assert.Empty(toggle.FindAll(".moka-switch-thumb *"));
		Assert.DoesNotContain("moka-spin", box.Find(".moka-checkbox-box svg").ClassList);
	}

	[Fact]
	public void Loading_EndsWithTheValueTheParentPasses()
	{
		bool value = false;
		IRenderedComponent<MokaSwitch> cut = Render<MokaSwitch>(p => p
			.Add(x => x.Value, value)
			.Add(x => x.ValueChanged, v => value = v));

		cut.Find("input").Change(true);
		Assert.True(value);

		cut.Render(p => p.Add(x => x.Value, true).Add(x => x.Loading, true));
		Assert.True(cut.Find("input").HasAttribute("checked"));
		Assert.True(cut.Find("input").HasAttribute("blazor:onclick:preventDefault"));

		cut.Render(p => p.Add(x => x.Value, false).Add(x => x.Loading, false));
		Assert.False(cut.Find("input").HasAttribute("checked"));
		Assert.Empty(cut.FindAll(".moka-switch-thumb svg"));
	}

	[Fact]
	public void ADisplayOnlyCheckbox_DrawsTheSpinnerToo()
	{
		IRenderedComponent<MokaCheckbox> cut = Render<MokaCheckbox>(p => p
			.Add(x => x.DisplayOnly, true)
			.Add(x => x.Loading, true));

		Assert.Contains("moka-spin", cut.Find(".moka-checkbox-box svg").ClassList);
		Assert.Empty(cut.FindAll("input"));
	}

	[Fact]
	public void TheSpinner_TurnsInTheTogglesTone_WithAProgressCursor()
	{
		Assert.Equal("var(--_switch-tone, var(--moka-color-primary))",
			ScopedCss.Declarations("MokaSwitch.razor.css", ".moka-switch--loading .moka-switch-thumb")["color"]);
		Assert.Equal("progress", ScopedCss.Declarations("MokaSwitch.razor.css", ".moka-switch--loading")["cursor"]);
		Assert.Equal("var(--_checkbox-tone, var(--moka-color-primary))",
			ScopedCss.Declarations("MokaCheckbox.razor.css", ".moka-checkbox--loading .moka-checkbox-box")["color"]);
		Assert.Equal("progress", ScopedCss.Declarations("MokaCheckbox.razor.css", ".moka-checkbox--loading")["cursor"]);
	}
}
