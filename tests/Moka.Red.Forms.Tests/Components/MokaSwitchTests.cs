using AngleSharp.Dom;
using Bunit;
using Moka.Red.Forms.Base;
using Moka.Red.Forms.Switch;

namespace Moka.Red.Forms.Tests.Components;

public class MokaSwitchTests : BunitContext
{
	[Fact]
	public void Renders_RootClass()
	{
		IRenderedComponent<MokaSwitch> cut = Render<MokaSwitch>();

		IElement label = cut.Find(".moka-switch");
		Assert.NotNull(label);
	}

	[Fact]
	public void Label_Renders()
	{
		IRenderedComponent<MokaSwitch> cut = Render<MokaSwitch>(p => p
			.Add(x => x.Label, "Dark Mode"));

		IElement label = cut.Find(".moka-switch-label");
		Assert.Equal("Dark Mode", label.TextContent);
	}

	[Fact]
	public void Disabled_AppliesClass()
	{
		IRenderedComponent<MokaSwitch> cut = Render<MokaSwitch>(p => p
			.Add(x => x.Disabled, true));

		IElement el = cut.Find(".moka-switch");
		Assert.Contains("moka-switch--disabled", el.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void LabelPlacement_Before()
	{
		IRenderedComponent<MokaSwitch> cut = Render<MokaSwitch>(p => p
			.Add(x => x.Label, "Before")
			.Add(x => x.LabelPlacement, MokaToggleBase.LabelPosition.Before));

		// The label should appear before the track in the DOM
		string markup = cut.Markup;
		int labelPos = markup.IndexOf("moka-switch-label", StringComparison.Ordinal);
		int trackPos = markup.IndexOf("moka-switch-track", StringComparison.Ordinal);
		Assert.True(labelPos < trackPos, "Label should appear before track in DOM");
	}

	[Fact]
	public void Toggle_ChangesValue()
	{
		bool value = false;
		IRenderedComponent<MokaSwitch> cut = Render<MokaSwitch>(p => p
			.Add(x => x.Value, value)
			.Add(x => x.ValueChanged, v => value = v));

		cut.Find("input").Change(true);
		Assert.True(value);
	}

	[Fact]
	public void Appends_UserClass()
	{
		IRenderedComponent<MokaSwitch> cut = Render<MokaSwitch>(p => p
			.Add(x => x.Class, "my-switch"));

		IElement el = cut.Find(".moka-switch");
		Assert.Contains("my-switch", el.ClassName, StringComparison.Ordinal);
	}
}
