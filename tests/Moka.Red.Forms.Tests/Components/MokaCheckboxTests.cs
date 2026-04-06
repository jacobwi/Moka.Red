using AngleSharp.Dom;
using Bunit;
using Moka.Red.Forms.Checkbox;

namespace Moka.Red.Forms.Tests.Components;

public class MokaCheckboxTests : BunitContext
{
	[Fact]
	public void Renders_CheckboxInput()
	{
		IRenderedComponent<MokaCheckbox> cut = Render<MokaCheckbox>();

		IElement input = cut.Find("input[type='checkbox']");
		Assert.NotNull(input);
	}

	[Fact]
	public void Renders_RootClass()
	{
		IRenderedComponent<MokaCheckbox> cut = Render<MokaCheckbox>();

		IElement label = cut.Find(".moka-checkbox");
		Assert.NotNull(label);
	}

	[Fact]
	public void Label_RendersAfter_ByDefault()
	{
		IRenderedComponent<MokaCheckbox> cut = Render<MokaCheckbox>(p => p
			.Add(x => x.Label, "Accept terms"));

		IElement label = cut.Find(".moka-checkbox-label");
		Assert.Equal("Accept terms", label.TextContent);
	}

	[Fact]
	public void Disabled_AppliesClassAndAttribute()
	{
		IRenderedComponent<MokaCheckbox> cut = Render<MokaCheckbox>(p => p
			.Add(x => x.Disabled, true)
			.Add(x => x.Label, "Disabled"));

		IElement cssLabel = cut.Find(".moka-checkbox");
		Assert.Contains("moka-checkbox--disabled", cssLabel.ClassName, StringComparison.Ordinal);

		IElement input = cut.Find("input");
		Assert.True(input.HasAttribute("disabled"));
	}

	[Fact]
	public void Toggle_ChangesValue()
	{
		bool value = false;
		IRenderedComponent<MokaCheckbox> cut = Render<MokaCheckbox>(p => p
			.Add(x => x.Value, value)
			.Add(x => x.ValueChanged, v => value = v));

		cut.Find("input").Change(true);
		Assert.True(value);
	}

	[Fact]
	public void Toggle_DoesNothing_WhenDisabled()
	{
		bool value = false;
		IRenderedComponent<MokaCheckbox> cut = Render<MokaCheckbox>(p => p
			.Add(x => x.Disabled, true)
			.Add(x => x.Value, value)
			.Add(x => x.ValueChanged, v => value = v));

		// Disabled checkbox input won't fire change event
		Assert.False(value);
	}

	[Fact]
	public void Indeterminate_AppliesClass()
	{
		IRenderedComponent<MokaCheckbox> cut = Render<MokaCheckbox>(p => p
			.Add(x => x.Indeterminate, true));

		IElement label = cut.Find(".moka-checkbox");
		Assert.Contains("moka-checkbox--indeterminate", label.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Appends_UserClass()
	{
		IRenderedComponent<MokaCheckbox> cut = Render<MokaCheckbox>(p => p
			.Add(x => x.Class, "my-checkbox"));

		IElement label = cut.Find(".moka-checkbox");
		Assert.Contains("my-checkbox", label.ClassName, StringComparison.Ordinal);
	}
}
