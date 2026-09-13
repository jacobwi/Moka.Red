using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Primitives.Chip;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaChipTests : BunitContext
{
	[Fact]
	public void Defaults_ToSoftVariant_SurfaceColor_AndMediumSize()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Chip"));

		IElement chip = cut.Find("button");
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

		IElement chip = cut.Find("button");
		Assert.Contains(expected, chip.ClassList);
		Assert.Single(chip.ClassList, c => c is "moka-chip--filled" or "moka-chip--outlined" or "moka-chip--text" or "moka-chip--soft");
	}

	[Fact]
	public void Applies_ColorClass_InsteadOfSurface()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Chip")
			.Add(x => x.Color, MokaColor.Success));

		IElement chip = cut.Find("button");
		Assert.Contains("moka-chip--success", chip.ClassList);
		Assert.DoesNotContain("moka-chip--surface", chip.ClassList);
	}

	[Fact]
	public void Selected_AddsClass_AndCheckIcon()
	{
		IRenderedComponent<MokaChip> cut = Render<MokaChip>(p => p
			.Add(x => x.Text, "Chip")
			.Add(x => x.Selected, true));

		Assert.Contains("moka-chip--selected", cut.Find("button").ClassList);
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

		Assert.EndsWith("custom-chip", cut.Find("button").ClassName, StringComparison.Ordinal);
	}
}
