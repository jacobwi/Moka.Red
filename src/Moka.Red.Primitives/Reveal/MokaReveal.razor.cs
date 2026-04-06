using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Reveal;

/// <summary>
///     Scroll-triggered animation wrapper. Content animates in when scrolled into the viewport.
///     Uses IntersectionObserver via JS interop to detect visibility.
/// </summary>
public partial class MokaReveal : MokaComponentBase
{
	private DotNetObjectReference<MokaReveal>? _dotNetRef;
	private ElementReference _elementRef;
	private bool _isVisible;
	private IJSObjectReference? _module;

	/// <summary>Content to animate in when scrolled into view.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Animation type to play when the element becomes visible. Default FadeUp.</summary>
	[Parameter]
	public MokaRevealAnimation Animation { get; set; } = MokaRevealAnimation.FadeUp;

	/// <summary>Animation duration in milliseconds. Default 600.</summary>
	[Parameter]
	public int Duration { get; set; } = 600;

	/// <summary>Delay before animation starts in milliseconds. Default 0.</summary>
	[Parameter]
	public int Delay { get; set; }

	/// <summary>
	///     How much of the element must be visible to trigger (0.0 to 1.0). Default 0.1.
	/// </summary>
	[Parameter]
	public double Threshold { get; set; } = 0.1;

	/// <summary>Whether to animate only once. When false, re-animates each time the element enters the viewport. Default true.</summary>
	[Parameter]
	public bool Once { get; set; } = true;

	/// <inheritdoc />
	protected override string RootClass => "moka-reveal";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-reveal--{AnimationToKebab(Animation)}")
		.AddClass("moka-reveal--visible", _isVisible)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-reveal-duration", $"{Duration}ms")
		.AddStyle("--moka-reveal-delay", $"{Delay}ms", Delay > 0)
		.AddStyle(Style)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_module = await GetJsModuleAsync(
				"./_content/Moka.Red.Primitives/Reveal/MokaReveal.razor.js");
			_dotNetRef = DotNetObjectReference.Create(this);
			await _module.InvokeVoidAsync("observe", _elementRef, Threshold, _dotNetRef, Once);
		}
	}

	/// <summary>Called from JavaScript when the element's visibility changes.</summary>
	[JSInvokable]
	public void OnVisibilityChanged(bool isVisible)
	{
		if (_isVisible != isVisible)
		{
			_isVisible = isVisible;
			StateHasChanged();
		}
	}

	private static string AnimationToKebab(MokaRevealAnimation animation) => animation switch
	{
		MokaRevealAnimation.FadeIn => "fade-in",
		MokaRevealAnimation.FadeUp => "fade-up",
		MokaRevealAnimation.FadeDown => "fade-down",
		MokaRevealAnimation.FadeLeft => "fade-left",
		MokaRevealAnimation.FadeRight => "fade-right",
		MokaRevealAnimation.ScaleIn => "scale-in",
		MokaRevealAnimation.SlideUp => "slide-up",
		_ => "fade-up"
	};

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_module is not null)
		{
			try
			{
				await _module.InvokeVoidAsync("unobserve", _elementRef);
			}
			catch (JSDisconnectedException)
			{
				// Circuit already disconnected
			}
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}
}
