using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Primitives.Icon;

namespace Moka.Red.Primitives.Tests.Components;

// Blazor treats a struct or enum parameter as possibly changed, so every icon on a page rendered again
// with its parent and came out identical.
public class MokaIconRenderTests : BunitContext
{
	private static readonly MokaIconDefinition First = new("first", "M0 0");

	public MokaIconRenderTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void AParentRender_WithTheSameParameters_DoesNotRenderTheIcon()
	{
		IRenderedComponent<Host> host = RenderHost(First, MokaColor.Primary);
		IRenderedComponent<CountingIcon> icon = host.FindComponent<CountingIcon>();

		host.Render();
		host.Render();

		Assert.Equal(1, icon.Instance.Renders);
	}

	[Fact]
	public void AChangedIcon_RendersIt()
	{
		IRenderedComponent<Host> host = RenderHost(First);

		host.Render(p => p.Add(x => x.Icon, new MokaIconDefinition("second", "M1 1")));

		Assert.Contains("M1 1", host.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void TheSameNameWithADifferentPath_RendersIt()
	{
		// MokaIconDefinition's own equality looks only at Name and Filled.
		IRenderedComponent<Host> host = RenderHost(First);

		host.Render(p => p.Add(x => x.Icon, new MokaIconDefinition("first", "M2 2")));

		Assert.Contains("M2 2", host.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void TheSameNameWithADifferentViewBox_RendersIt()
	{
		IRenderedComponent<Host> host = RenderHost(First);

		host.Render(p => p.Add(x => x.Icon, new MokaIconDefinition("first", "M0 0", "0 0 16 16")));

		Assert.Contains("0 0 16 16", host.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void AChangedColor_RendersIt()
	{
		IRenderedComponent<Host> host = RenderHost(First, MokaColor.Primary);

		host.Render(p => p.Add(x => x.Color, MokaColor.Error));

		Assert.Contains("moka-icon--error", host.Markup, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(nameof(MokaIcon.Class), "changed-class")]
	[InlineData(nameof(MokaIcon.Style), "changed-style")]
	[InlineData(nameof(MokaIcon.Id), "changed-id")]
	[InlineData(nameof(MokaIcon.SizeValue), "33px")]
	[InlineData(nameof(MokaIcon.MarginValue), "11px")]
	[InlineData(nameof(MokaIcon.PaddingValue), "12px")]
	[InlineData(nameof(MokaIcon.RoundedValue), "13px")]
	public void AChangedStringParameter_RendersIt(string parameter, string value)
	{
		IRenderedComponent<MokaIcon> cut = Render<MokaIcon>(p => p.Add(x => x.Icon, First));

		cut.Render(ParameterView.FromDictionary(new Dictionary<string, object?> { [parameter] = value }));

		Assert.Contains(value, cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void AChangedSize_RendersIt()
	{
		IRenderedComponent<MokaIcon> cut = Render<MokaIcon>(p => p.Add(x => x.Icon, First).Add(x => x.Size, MokaSize.Sm));

		cut.Render(p => p.Add(x => x.Size, MokaSize.Lg));

		Assert.Contains("moka-icon--lg", cut.Markup, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(nameof(MokaIcon.Margin), "margin:")]
	[InlineData(nameof(MokaIcon.Padding), "padding:")]
	public void AChangedSpacingScale_RendersIt(string parameter, string declaration)
	{
		IRenderedComponent<MokaIcon> cut = Render<MokaIcon>(p => p.Add(x => x.Icon, First));

		cut.Render(ParameterView.FromDictionary(new Dictionary<string, object?> { [parameter] = (MokaSpacingScale?)MokaSpacingScale.Md }));

		Assert.Contains(declaration, cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void AChangedRounding_RendersIt()
	{
		IRenderedComponent<MokaIcon> cut = Render<MokaIcon>(p => p.Add(x => x.Icon, First));

		cut.Render(p => p.Add(x => x.Rounded, MokaRounding.Full));

		Assert.Contains("border-radius:", cut.Markup, StringComparison.Ordinal);
	}

	[Fact]
	public void ExtraAttributes_AlwaysRender()
	{
		// They can hold anything, including a new delegate, so a comparison can't vouch for them.
		IRenderedComponent<Host> host = RenderHost(First, attributes: new Dictionary<string, object> { ["data-x"] = "1" });
		IRenderedComponent<CountingIcon> icon = host.FindComponent<CountingIcon>();

		host.Render();

		Assert.Equal(2, icon.Instance.Renders);
	}

	private IRenderedComponent<Host> RenderHost(MokaIconDefinition icon, MokaColor? color = null,
		IReadOnlyDictionary<string, object>? attributes = null) =>
		Render<Host>(p => p
			.Add(x => x.Icon, icon)
			.Add(x => x.Color, color)
			.Add(x => x.Attributes, attributes));

	private sealed class CountingIcon : MokaIcon
	{
		public int Renders { get; private set; }

		protected override void OnAfterRender(bool firstRender)
		{
			base.OnAfterRender(firstRender);
			Renders++;
		}
	}

	private sealed class Host : ComponentBase
	{
		[Parameter] public MokaIconDefinition Icon { get; set; }

		[Parameter] public MokaColor? Color { get; set; }

		[Parameter] public IReadOnlyDictionary<string, object>? Attributes { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<CountingIcon>(0);
			builder.AddComponentParameter(1, nameof(MokaIcon.Icon), Icon);
			builder.AddComponentParameter(2, nameof(MokaIcon.Color), Color);
			if (Attributes is not null)
			{
				builder.AddMultipleAttributes(3, Attributes);
			}

			builder.CloseComponent();
		}
	}
}
