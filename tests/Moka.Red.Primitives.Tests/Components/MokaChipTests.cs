using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.Chip;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaChipTests : BunitContext
{
	[Fact]
	public void Defaults_ToSoftVariant_SurfaceColor_AndMediumSize()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Chip"));

		IElement chip = cut.Find(".moka-chip");
		Assert.Contains("moka-chip--soft", chip.ClassList);
		Assert.Contains("moka-chip--surface", chip.ClassList);
		Assert.Contains("moka-chip--md", chip.ClassList);
	}

	[Theory]
	[InlineData(MokaVariant.Filled, "moka-chip--filled")]
	[InlineData(MokaVariant.Outlined, "moka-chip--outlined")]
	[InlineData(MokaVariant.Text, "moka-chip--text")]
	[InlineData(MokaVariant.Soft, "moka-chip--soft")]
	public void Applies_ExactlyOneVariantClass(MokaVariant variant, string expected)
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Chip")
			.Add(x => x.Variant, variant));

		IElement chip = cut.Find(".moka-chip");
		Assert.Contains(expected, chip.ClassList);
		Assert.Single(chip.ClassList, c => c is "moka-chip--filled" or "moka-chip--outlined" or "moka-chip--text" or "moka-chip--soft");
	}

	[Fact]
	public void Applies_ColorClass_InsteadOfSurface()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Chip")
			.Add(x => x.Color, MokaColor.Success));

		IElement chip = cut.Find(".moka-chip");
		Assert.Contains("moka-chip--success", chip.ClassList);
		Assert.DoesNotContain("moka-chip--surface", chip.ClassList);
	}

	[Fact]
	public void Selected_AddsClass_AndCheckIcon()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Chip")
			.Add(x => x.Selected, true));

		Assert.Contains("moka-chip--selected", cut.Find(".moka-chip").ClassList);
		Assert.Single(cut.FindAll(".moka-chip__check"));
	}

	[Fact]
	public async Task Click_TogglesSelected_WhenBound()
	{
		bool? reported = null;
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Chip")
			.Add(x => x.SelectedChanged, EventCallback.Factory.Create<bool>(this, v => reported = v)));

		await cut.Find("button").ClickAsync(new MouseEventArgs());

		Assert.True(reported);
		Assert.Contains("moka-chip--selected", cut.Find("button").ClassList);
	}

	[Fact]
	public async Task Click_DoesNotToggle_WhenDisabled()
	{
		bool changed = false;
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Chip")
			.Add(x => x.Disabled, true)
			.Add(x => x.SelectedChanged, EventCallback.Factory.Create<bool>(this, _ => changed = true)));

		await cut.Find("button").ClickAsync(new MouseEventArgs());

		Assert.False(changed);
		Assert.Contains("moka-chip--disabled", cut.Find("button").ClassList);
	}

	[Fact]
	public async Task CloseButton_InvokesOnClose()
	{
		bool closed = false;
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Chip")
			.Add(x => x.Closable, true)
			.Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closed = true)));

		await cut.Find(".moka-chip__close").ClickAsync(new MouseEventArgs());

		Assert.True(closed);
	}

	[Fact]
	public void UserClass_IsAppendedLast()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Chip")
			.Add(x => x.Class, "custom-chip"));

		Assert.EndsWith("custom-chip", cut.Find(".moka-chip").ClassName, StringComparison.Ordinal);
	}

	// With nothing to click, a button would be a tab stop that does nothing.
	[Fact]
	public void AChipWithNothingToClick_IsASpan_OutOfTheTabOrder()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Chip")
			.Add(x => x.Selected, true));

		IElement chip = cut.Find(".moka-chip");
		Assert.Equal("SPAN", chip.TagName);
		Assert.Contains("moka-chip--static", chip.ClassList);
		Assert.False(chip.HasAttribute("aria-pressed"));
		Assert.False(chip.HasAttribute("tabindex"));
		Assert.Empty(cut.FindAll("button"));
	}

	[Fact]
	public void AChipWithOnlyOnClick_IsAButton_ThatIsNoToggle()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Add filter")
			.Add(x => x.OnClick, _ => { }));

		IElement chip = cut.Find(".moka-chip");
		Assert.Equal("BUTTON", chip.TagName);
		Assert.Equal("button", chip.GetAttribute("type"));
		Assert.False(chip.HasAttribute("aria-pressed"));
		Assert.DoesNotContain("moka-chip--static", chip.ClassList);
	}

	[Fact]
	public async Task ABoundChip_IsAToggleButton_WithAriaPressedAsText()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Design")
			.Add(x => x.Selected, false)
			.Add(x => x.SelectedChanged, EventCallback.Factory.Create<bool>(this, _ => { })));

		Assert.Equal("false", cut.Find("button.moka-chip").GetAttribute("aria-pressed"));

		await cut.Find("button").ClickAsync(new MouseEventArgs());

		Assert.Equal("true", cut.Find("button.moka-chip").GetAttribute("aria-pressed"));
	}

	// A parent that keeps the selection itself through OnClick still owns a toggle.
	[Fact]
	public void AChipWithOnClickAndSelected_SaysWhetherItIsPressed()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Design")
			.Add(x => x.Selected, false)
			.Add(x => x.OnClick, _ => { }));

		Assert.Equal("false", cut.Find("button.moka-chip").GetAttribute("aria-pressed"));

		cut.Render(p => p.Add(x => x.Selected, true));

		Assert.Equal("true", cut.Find("button.moka-chip").GetAttribute("aria-pressed"));
	}

	// bUnit parses the markup with an HTML parser, which closes a button at the start of another, so a remove button
	// nested in the chip's own button would come out beside the chip. Here it stays inside it, in no other button.
	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public void AClosableChip_KeepsItsRemoveButtonOutOfAnyOtherButton(bool onClick, bool bound)
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p =>
		{
			p.Add(x => x.Text, "Blazor").Add(x => x.Closable, true);
			if (onClick)
			{
				p.Add(x => x.OnClick, _ => { });
			}

			if (bound)
			{
				p.Add(x => x.Selected, false).Add(x => x.SelectedChanged, _ => { });
			}
		});

		IElement chip = cut.Find(".moka-chip");
		IElement close = cut.Find(".moka-chip__close");
		Assert.Equal("SPAN", chip.TagName);
		Assert.NotNull(close.Closest(".moka-chip"));
		Assert.Null(close.ParentElement!.Closest("button"));
		Assert.All(cut.FindAll("button"), button => Assert.Null(button.ParentElement!.Closest("button")));
		Assert.Equal(onClick || bound ? 2 : 1, cut.FindAll("button").Count);
	}

	[Fact]
	public void TheRemoveButton_IsATabStop_NamedForTheChip()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Blazor")
			.Add(x => x.Closable, true));

		IElement close = cut.Find(".moka-chip__close");
		Assert.False(close.HasAttribute("tabindex"));
		Assert.False(close.HasAttribute("disabled"));
		Assert.Equal("Remove Blazor", close.GetAttribute("aria-label"));
	}

	[Fact]
	public void TheRemoveButton_OfAChipWithNoText_IsNamedRemove()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Closable, true)
			.Add(x => x.ChildContent, b => b.AddContent(0, "Blazor")));

		Assert.Equal("Remove", cut.Find(".moka-chip__close").GetAttribute("aria-label"));
	}

	[Fact]
	public void ADisabledClosableChip_DisablesBothButtons()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Blazor")
			.Add(x => x.Closable, true)
			.Add(x => x.Disabled, true)
			.Add(x => x.OnClick, _ => { }));

		Assert.All(cut.FindAll("button"), button => Assert.True(button.HasAttribute("disabled")));
	}

	[Fact]
	public async Task AClosableChipWithAnAction_ClicksAndRemovesApart()
	{
		int clicks = 0;
		int removes = 0;
		bool? reported = null;
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Blazor")
			.Add(x => x.Closable, true)
			.Add(x => x.Selected, false)
			.Add(x => x.SelectedChanged, EventCallback.Factory.Create<bool>(this, v => reported = v))
			.Add(x => x.OnClick, _ => clicks++)
			.Add(x => x.OnClose, EventCallback.Factory.Create(this, () => removes++)));

		IElement action = cut.Find(".moka-chip__action");
		Assert.Equal("button", action.GetAttribute("type"));
		Assert.Equal("false", action.GetAttribute("aria-pressed"));
		Assert.Empty(cut.FindAll(".moka-chip__check"));

		await action.ClickAsync(new MouseEventArgs());
		Assert.Equal(1, clicks);
		Assert.True(reported);
		Assert.Equal("true", cut.Find(".moka-chip__action").GetAttribute("aria-pressed"));
		Assert.Contains("moka-chip--selected", cut.Find(".moka-chip").ClassList);
		Assert.NotNull(cut.Find(".moka-chip__action .moka-chip__check"));

		await cut.Find(".moka-chip__close").ClickAsync(new MouseEventArgs());
		Assert.Equal(1, removes);
		Assert.Equal(1, clicks);
	}

	[Fact]
	public void IdClassStyleAndAttributes_GoOnTheOutermostElement()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Blazor")
			.Add(x => x.Closable, true)
			.Add(x => x.OnClick, _ => { })
			.Add(x => x.Id, "chip-1")
			.Add(x => x.Class, "mine")
			.Add(x => x.Style, "order: 2")
			.AddUnmatched("data-test", "chip"));

		IElement chip = cut.Find(".moka-chip");
		Assert.Equal("chip-1", chip.Id);
		Assert.Contains("mine", chip.ClassList);
		Assert.Contains("order: 2", chip.GetAttribute("style"), StringComparison.Ordinal);
		Assert.Equal("chip", chip.GetAttribute("data-test"));
		Assert.False(cut.Find(".moka-chip__action").HasAttribute("id"));
	}

	[Fact]
	public void TheStylesheet_KeepsStaticChipsStill_AndRingsAChipWhoseButtonHasFocus()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "MokaChip.razor.css")).ReplaceLineEndings("\n");

		Assert.DoesNotMatch(@":(hover|active):not\(\.moka-chip--disabled\)", css);
		Assert.Contains(".moka-chip:has(> .moka-chip__action:focus-visible)", css, StringComparison.Ordinal);
		Assert.Equal("default", ScopedCss.Declarations("MokaChip.razor.css", ".moka-chip--static")["cursor"]);
		Assert.Equal("var(--moka-focus-ring)", ScopedCss.Declarations("MokaChip.razor.css", ".moka-chip__close:focus-visible")["box-shadow"]);
	}

	// The new design's selected rules set box-shadow: none and outranked the ring, so a selected chip showed no focus.
	[Theory]
	[InlineData(".moka-gate .moka-chip.moka-chip--selected:focus-visible:not(.moka-chip--disabled)")]
	[InlineData(".moka-gate .moka-chip.moka-chip--selected:has(> .moka-chip__action:focus-visible)")]
	public void ASelectedChip_KeepsTheFocusRing_OnTheNewDesign(string selector)
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "MokaChip.razor.css")).ReplaceLineEndings("\n");

		Assert.Equal("var(--moka-focus-ring)", ScopedCss.Declarations("MokaChip.razor.css", selector)["box-shadow"]);
		Assert.True(css.LastIndexOf(selector, StringComparison.Ordinal) > css.LastIndexOf("box-shadow: none", StringComparison.Ordinal));
	}

	[Fact]
	public async Task Controlled_ShowsWhatTheParentPassesBack_SoARefusalReverts()
	{
		bool? reported = null;
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Design")
			.Add(x => x.Controlled, true)
			.Add(x => x.Selected, false)
			.Add(x => x.SelectedChanged, EventCallback.Factory.Create<bool>(this, v => reported = v)));

		await cut.Find("button").ClickAsync(new MouseEventArgs());

		Assert.True(reported);
		Assert.DoesNotContain("moka-chip--selected", cut.Find(".moka-chip").ClassList);
		Assert.Equal("false", cut.Find(".moka-chip").GetAttribute("aria-pressed"));
	}

	[Fact]
	public async Task Controlled_FollowsTheParent_WhenItTakesTheChange()
	{
		IRenderedComponent<ChipHost> host = Render<ChipHost>(p => p.Add(x => x.Accept, true));

		await host.Find("button").ClickAsync(new MouseEventArgs());
		Assert.True(host.Instance.Value);
		Assert.Contains("moka-chip--selected", host.Find(".moka-chip").ClassList);

		await host.Find("button").ClickAsync(new MouseEventArgs());
		Assert.False(host.Instance.Value);
		Assert.DoesNotContain("moka-chip--selected", host.Find(".moka-chip").ClassList);
	}

	[Fact]
	public async Task Controlled_RefusedByARenderingParent_Reverts()
	{
		IRenderedComponent<ChipHost> host = Render<ChipHost>(p => p.Add(x => x.Accept, false));

		await host.Find("button").ClickAsync(new MouseEventArgs());

		Assert.Equal(1, host.Instance.Asked);
		Assert.False(host.Instance.Value);
		Assert.DoesNotContain("moka-chip--selected", host.Find(".moka-chip").ClassList);
	}

	// Every parameter set copies the value in, even one the uncontrolled chip would skip as unchanged.
	[Fact]
	public async Task Controlled_TakesTheParentsValue_EvenWhenItDidNotChange()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Design")
			.Add(x => x.Controlled, true)
			.Add(x => x.Selected, true)
			.Add(x => x.SelectedChanged, EventCallback.Factory.Create<bool>(this, _ => { })));

		await cut.Find("button").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Selected, true));

		Assert.Contains("moka-chip--selected", cut.Find(".moka-chip").ClassList);
		Assert.Equal("true", cut.Find(".moka-chip").GetAttribute("aria-pressed"));
	}

	// A click never changes the chip itself, so while the parent's answer is on its way the old choice stays, through
	// a parent render that passes the old value too; the answer then decides.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task Controlled_KeepsTheOldChoiceWhileTheParentDecides(bool accept)
	{
		TaskCompletionSource<bool> answer = new();
		IRenderedComponent<ChipHost> host = Render<ChipHost>(p => p.Add(x => x.Answer, answer.Task));

		Task click = host.Find("button").ClickAsync(new MouseEventArgs());
		host.WaitForAssertion(() => Assert.Equal(1, host.Instance.Rendered));
		Assert.DoesNotContain("moka-chip--selected", host.Find(".moka-chip").ClassList);
		Assert.Equal("false", host.Find(".moka-chip").GetAttribute("aria-pressed"));

		await host.InvokeAsync(() => answer.SetResult(accept));
		await click;

		Assert.Equal(accept, host.Find(".moka-chip").ClassList.Contains("moka-chip--selected"));
	}

	[Fact]
	public async Task Uncontrolled_KeepsTheClick_WhenTheParentRefuses()
	{
		IRenderedComponent<ChipHost> host = Render<ChipHost>(p => p
			.Add(x => x.Accept, false)
			.Add(x => x.Controlled, false));

		await host.Find("button").ClickAsync(new MouseEventArgs());

		Assert.Contains("moka-chip--selected", host.Find(".moka-chip").ClassList);
	}

	private sealed class ChipHost : ComponentBase
	{
		[Parameter]
		public bool Accept { get; set; }

		[Parameter]
		public bool Controlled { get; set; } = true;

		[Parameter]
		public Task<bool>? Answer { get; set; }

		public bool Value { get; private set; }

		public int Asked { get; private set; }

		// Renders done while the answer is pending, each passing the old value.
		public int Rendered { get; private set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<MokaChip>(0);
			builder.AddComponentParameter(1, nameof(MokaChip.Text), "Design");
			builder.AddComponentParameter(2, nameof(MokaChip.Controlled), Controlled);
			builder.AddComponentParameter(3, nameof(MokaChip.Selected), Value);
			builder.AddComponentParameter(4, nameof(MokaChip.SelectedChanged), EventCallback.Factory.Create<bool>(this, Changed));
			builder.CloseComponent();
		}

		private async Task Changed(bool value)
		{
			Asked++;
			bool accept = Accept;
			if (Answer is not null)
			{
				await Task.Yield();
				StateHasChanged();
				Rendered++;
				accept = await Answer;
			}

			if (accept)
			{
				Value = value;
			}
		}
	}
}
