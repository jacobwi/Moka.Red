using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Core.Base;
using Moka.Red.Core.Extensions;
using Moka.Red.Core.Theming;

namespace Moka.Red.Core.Tests.Theming;

// Theme was a cascading parameter on both base classes, and a CascadingValue tells its subscribers every time its
// provider renders, because Blazor can't tell a class hasn't changed. So any layout render redrew every Moka
// component on the page: 500 of 500 in a measurement.
public class ThemeCascadeTests : BunitContext
{
	public ThemeCascadeTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void TheProviderRendering_DoesNotRenderAComponentThatDidNotChange()
	{
		IRenderedComponent<MokaThemeProvider> provider = RenderProvider<CountingComponent>(MokaTheme.Dark);
		CountingComponent leaf = provider.FindComponent<CountingComponent>().Instance;

		provider.Render();
		provider.Render();

		Assert.Equal(1, leaf.Renders);
	}

	[Fact]
	public void ANewTheme_DoesNotRenderAComponentThatDidNotChange()
	{
		IRenderedComponent<MokaThemeProvider> provider = RenderProvider<CountingComponent>(MokaTheme.Dark);
		CountingComponent leaf = provider.FindComponent<CountingComponent>().Instance;

		provider.Render(p => p.Add(x => x.Theme, MokaTheme.Light));

		Assert.Equal(1, leaf.Renders);
	}

	[Fact]
	public void TheProviderRendering_DoesNotRenderAnInputThatDidNotChange()
	{
		IRenderedComponent<MokaThemeProvider> provider = RenderProvider<CountingInput>(MokaTheme.Dark);
		CountingInput input = provider.FindComponent<CountingInput>().Instance;

		provider.Render();

		Assert.Equal(1, input.Renders);
	}

	[Fact]
	public void Theme_ReadsTheProvidersCurrentTheme()
	{
		IRenderedComponent<MokaThemeProvider> provider = RenderProvider<CountingComponent>(MokaTheme.Dark);
		CountingComponent leaf = provider.FindComponent<CountingComponent>().Instance;
		Assert.Same(MokaTheme.Dark, leaf.Theme);

		provider.Render(p => p.Add(x => x.Theme, MokaTheme.Gate));

		Assert.Same(MokaTheme.Gate, leaf.Theme);
	}

	[Fact]
	public void AnInput_ReadsTheProvidersCurrentTheme()
	{
		IRenderedComponent<MokaThemeProvider> provider = RenderProvider<CountingInput>(MokaTheme.Dark);

		Assert.Same(MokaTheme.Dark, provider.FindComponent<CountingInput>().Instance.Theme);
	}

	[Fact]
	public void TheNearestProvider_Wins()
	{
		IRenderedComponent<MokaThemeProvider> outer = Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, MokaTheme.Dark)
			.Add(x => x.ChildContent, (RenderFragment)(b =>
			{
				b.OpenComponent<MokaThemeProvider>(0);
				b.AddComponentParameter(1, nameof(MokaThemeProvider.Theme), MokaTheme.Gate);
				b.AddComponentParameter(2, nameof(MokaThemeProvider.ChildContent), (RenderFragment)(inner =>
				{
					inner.OpenComponent<CountingComponent>(0);
					inner.CloseComponent();
				}));
				b.CloseComponent();
			})));

		Assert.Same(MokaTheme.Gate, outer.FindComponent<CountingComponent>().Instance.Theme);
	}

	[Fact]
	public void WithoutAProvider_ItReadsTheRegisteredTheme()
	{
		Services.AddMokaRedCore(MokaTheme.Gate);

		Assert.Same(MokaTheme.Gate, Render<CountingComponent>().Instance.Theme);
	}

	[Fact]
	public void WithNothingAtAll_ItIsNull()
	{
		Assert.Null(Render<CountingComponent>().Instance.Theme);
	}

	// The way to ask for a redraw: an ordinary cascading parameter, which the provider still supplies.
	[Fact]
	public void AComponentThatDeclaresTheCascade_RendersWhenTheThemeChanges()
	{
		IRenderedComponent<MokaThemeProvider> provider = RenderProvider<ThemeReader>(MokaTheme.Dark);
		ThemeReader reader = provider.FindComponent<ThemeReader>().Instance;
		Assert.Same(MokaTheme.Dark, reader.Read);

		provider.Render(p => p.Add(x => x.Theme, MokaTheme.Gate));

		Assert.Same(MokaTheme.Gate, reader.Read);
		Assert.Equal(2, reader.Renders);
	}

	[Fact]
	public void APlainComponentThatDeclaresTheCascade_StillGetsTheTheme()
	{
		IRenderedComponent<MokaThemeProvider> provider = RenderProvider<PlainReader>(MokaTheme.Dark);
		PlainReader reader = provider.FindComponent<PlainReader>().Instance;

		provider.Render(p => p.Add(x => x.Theme, MokaTheme.Gate));

		Assert.Same(MokaTheme.Gate, reader.Theme);
	}

	private IRenderedComponent<MokaThemeProvider> RenderProvider<TChild>(MokaTheme theme) where TChild : IComponent =>
		Render<MokaThemeProvider>(p => p
			.Add(x => x.Theme, theme)
			.Add(x => x.ChildContent, (RenderFragment)(b =>
			{
				b.OpenComponent<TChild>(0);
				b.CloseComponent();
			})));

	public sealed class CountingComponent : MokaComponentBase
	{
		public int Renders { get; private set; }

		protected override string RootClass => "counting";

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			Renders++;
			builder.OpenElement(0, "div");
			builder.CloseElement();
		}
	}

	public sealed class ThemeReader : MokaComponentBase
	{
		public int Renders { get; private set; }

		[CascadingParameter]
		public MokaTheme? Read { get; set; }

		protected override string RootClass => "reader";

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			Renders++;
			builder.OpenElement(0, "div");
			builder.CloseElement();
		}
	}

	public sealed class PlainReader : ComponentBase
	{
		[CascadingParameter]
		public MokaTheme? Theme { get; set; }
	}

	public sealed class CountingInput : MokaInputBase<string>
	{
		public int Renders { get; private set; }

		protected override string RootClass => "counting-input";

		protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
		{
			result = value ?? string.Empty;
			validationErrorMessage = string.Empty;
			return true;
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			Renders++;
			builder.OpenElement(0, "input");
			builder.CloseElement();
		}
	}
}
