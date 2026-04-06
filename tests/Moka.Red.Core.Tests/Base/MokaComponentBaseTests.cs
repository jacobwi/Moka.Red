using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Core.Base;

namespace Moka.Red.Core.Tests.Base;

public class MokaComponentBaseTests : BunitContext
{
	[Fact]
	public void Renders_RootClass()
	{
		IRenderedComponent<TestMokaComponent> cut = Render<TestMokaComponent>();

		IElement div = cut.Find("div");
		Assert.Contains("test-component", div.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Appends_UserClass()
	{
		IRenderedComponent<TestMokaComponent> cut = Render<TestMokaComponent>(parameters => parameters
			.Add(p => p.Class, "custom-class"));

		IElement div = cut.Find("div");
		Assert.Contains("test-component", div.ClassName, StringComparison.Ordinal);
		Assert.Contains("custom-class", div.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void Applies_UserStyle()
	{
		IRenderedComponent<TestMokaComponent> cut = Render<TestMokaComponent>(parameters => parameters
			.Add(p => p.Style, "color: red"));

		IElement div = cut.Find("div");
		Assert.Equal("color: red", div.GetAttribute("style"));
	}

	[Fact]
	public void Applies_Id()
	{
		IRenderedComponent<TestMokaComponent> cut = Render<TestMokaComponent>(parameters => parameters
			.Add(p => p.Id, "my-id"));

		IElement div = cut.Find("#my-id");
		Assert.NotNull(div);
	}

	[Fact]
	public void Splats_AdditionalAttributes()
	{
		IRenderedComponent<TestMokaComponent> cut = Render<TestMokaComponent>(parameters => parameters
			.AddUnmatched("data-testid", "test-value")
			.AddUnmatched("aria-label", "My label"));

		IElement div = cut.Find("div");
		Assert.Equal("test-value", div.GetAttribute("data-testid"));
		Assert.Equal("My label", div.GetAttribute("aria-label"));
	}

	[Fact]
	public async Task ShouldRender_PreventsExtraRenders_OnStateHasChanged()
	{
		IRenderedComponent<TestMokaComponent> cut = Render<TestMokaComponent>();

		// First StateHasChanged may or may not render (depends on bUnit initialization).
		// The key behavior: calling StateHasChanged twice in succession without parameter
		// changes should NOT cause render count to keep increasing each time.
		await cut.InvokeAsync(cut.Instance.CallStateHasChanged);
		int countAfterFirst = cut.Instance.RenderCount;

		await cut.InvokeAsync(cut.Instance.CallStateHasChanged);
		int countAfterSecond = cut.Instance.RenderCount;

		// Second StateHasChanged should not cause another render
		Assert.Equal(countAfterFirst, countAfterSecond);
	}

	[Fact]
	public async Task ForceRender_TriggersRerender()
	{
		IRenderedComponent<TestMokaComponent> cut = Render<TestMokaComponent>();

		int countBefore = cut.Instance.RenderCount;
		await cut.InvokeAsync(cut.Instance.CallForceRender);

		Assert.True(cut.Instance.RenderCount > countBefore);
	}

	[Fact]
	public async Task DisposeAsync_IsIdempotent()
	{
		IRenderedComponent<TestMokaComponent> cut = Render<TestMokaComponent>();
		TestMokaComponent component = cut.Instance;

		await component.DisposeAsync();
		await component.DisposeAsync(); // second call should not throw

		Assert.Equal(1, component.DisposeCount);
	}

	/// <summary>
	///     Minimal concrete component for testing MokaComponentBase behavior.
	/// </summary>
	public sealed class TestMokaComponent : MokaComponentBase
	{
		public int RenderCount { get; private set; }
		public int DisposeCount { get; private set; }

		protected override string RootClass => "test-component";

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			ArgumentNullException.ThrowIfNull(builder);
			RenderCount++;
			builder.OpenElement(0, "div");
			builder.AddAttribute(1, "class", CssClass);

			if (CssStyle is not null)
			{
				builder.AddAttribute(2, "style", CssStyle);
			}

			if (Id is not null)
			{
				builder.AddAttribute(3, "id", Id);
			}

			builder.AddMultipleAttributes(4, AdditionalAttributes);
			builder.CloseElement();
		}

		public void CallForceRender() => ForceRender();

		public void CallStateHasChanged() => StateHasChanged();

		protected override async ValueTask DisposeAsyncCore()
		{
			DisposeCount++;
			await base.DisposeAsyncCore();
		}
	}
}
