using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Reveal;

/// <summary>
///     Scroll-triggered animation wrapper. Content animates in when scrolled into the viewport.
///     Uses IntersectionObserver via JS interop to detect visibility. With <see cref="OnEnter" /> and
///     <see cref="OnLeave" /> it also tells the app when its content comes into view and leaves it, such as to load a
///     thumbnail only once it can be seen, and with <see cref="MokaRevealAnimation.None" /> it does only that.
/// </summary>
public partial class MokaReveal : MokaComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Primitives/Reveal/MokaReveal.razor.js";

	private DotNetObjectReference<MokaReveal>? _dotNetRef;
	private ElementReference _elementRef;
	private bool _isVisible;
	private bool _entered;

	// What the browser was last asked to watch for, or null while nothing is watched.
	private Dictionary<string, object?>? _boundOptions;

	/// <summary>Content to animate in when scrolled into view.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Animation type to play when the element becomes visible. Default FadeUp. <see cref="MokaRevealAnimation.None" />
	///     shows the content as it is from the start, for <see cref="OnEnter" /> and <see cref="OnLeave" /> alone.
	/// </summary>
	[Parameter]
	public MokaRevealAnimation Animation { get; set; } = MokaRevealAnimation.FadeUp;

	/// <summary>Animation duration in milliseconds. Default 600.</summary>
	[Parameter]
	public int Duration { get; set; } = 600;

	/// <summary>Delay before animation starts in milliseconds. Default 0.</summary>
	[Parameter]
	public int Delay { get; set; }

	/// <summary>
	///     How much of the element must be visible to trigger (0.0 to 1.0). Default 0.1. A value outside the range is
	///     taken as the nearest end.
	/// </summary>
	[Parameter]
	public double Threshold { get; set; } = 0.1;

	/// <summary>
	///     Whether to watch only until the element first comes into view. When false, re-animates each time the element
	///     enters the viewport, and <see cref="OnLeave" /> and <see cref="OnEnter" /> are raised every time it leaves and
	///     comes back. Default true, which raises <see cref="OnEnter" /> once and <see cref="OnLeave" /> never.
	/// </summary>
	[Parameter]
	public bool Once { get; set; } = true;

	/// <summary>
	///     Raised when the content comes into view (see <see cref="Threshold" /> and <see cref="RootMargin" />), including
	///     on the first render when it is already in view. .NET hears only the moment it crosses, never the scrolling in
	///     between.
	/// </summary>
	[Parameter]
	public EventCallback OnEnter { get; set; }

	/// <summary>
	///     Raised when the content that came into view leaves it again, and when the component is removed while its content
	///     is in view, so every <see cref="OnEnter" /> has its <see cref="OnLeave" /> and what was loaded on entering can be
	///     released. Needs <see cref="Once" /> off. On removal it runs once the render that removed the component has
	///     finished; the component is gone by then, so a failure in it can't reach an error boundary and counts as a
	///     failure in disposal, which ends a Blazor Server circuit (a JS call that fails because the circuit has already
	///     closed is ignored).
	/// </summary>
	[Parameter]
	public EventCallback OnLeave { get; set; }

	/// <summary>
	///     Grows (or with negative values shrinks) the view the content is measured against, as a CSS margin in px or %,
	///     such as <c>"200px"</c> to start loading a picture a little before it scrolls in. Null (the default) is the view
	///     itself. A value the browser wouldn't take is ignored.
	/// </summary>
	[Parameter]
	public string? RootMargin { get; set; }

	/// <summary>
	///     How long, in milliseconds, the content has to stay in view or out of it before the change counts. Content that
	///     a fast scroll carries past in less time raises nothing, so a long list of thumbnails flung past loads none of
	///     them. Default 0, where every crossing counts at once.
	/// </summary>
	[Parameter]
	public int Debounce { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-reveal";

	private bool Animates => Animation != MokaRevealAnimation.None;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-reveal--{AnimationToKebab(Animation)}")
		.AddClass("moka-reveal--visible", _isVisible && Animates)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-reveal-duration", string.Create(CultureInfo.InvariantCulture, $"{Duration}ms"), Animates)
		.AddStyle("--moka-reveal-delay", string.Create(CultureInfo.InvariantCulture, $"{Delay}ms"), Delay > 0 && Animates)
		.AddStyle(Style)
		.Build();

	// Leaves cost a call only when something reads them: an animation that plays again, or a listener.
	private bool ReportsLeaves => OnLeave.HasDelegate || (Animates && !Once);

	private bool Watches => Animates || OnEnter.HasDelegate || OnLeave.HasDelegate;

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Content revealed once stays revealed, so there is nothing more to watch for.
		bool done = Once && _entered;
		Dictionary<string, object?>? wanted = Watches && !done ? WatchOptions() : null;
		if (SameOptions(wanted, _boundOptions))
		{
			return;
		}

		if (wanted is null)
		{
			_boundOptions = null;
			await SafeModuleInvokeVoidAsync(ModulePath, "unobserve", _elementRef);
			return;
		}

		_dotNetRef ??= DotNetObjectReference.Create(this);
		_boundOptions = wanted;
		await SafeModuleInvokeVoidAsync(ModulePath, "observe", _elementRef, _dotNetRef, wanted);
	}

	/// <summary>Called from JavaScript when the element comes into view or leaves it.</summary>
	/// <param name="isVisible">Whether the element is now in view.</param>
	[JSInvokable]
	public void OnVisibilityChanged(bool isVisible)
	{
		if (isVisible)
		{
			_entered = true;

			// The browser stops watching by itself after the first time in view.
			if (_boundOptions is { } bound && bound["once"] is true)
			{
				_boundOptions = null;
			}
		}

		if (_isVisible != isVisible)
		{
			_isVisible = isVisible;
			if (Animates)
			{
				StateHasChanged();
			}
		}

		EventCallback callback = isVisible ? OnEnter : OnLeave;
		if (callback.HasDelegate)
		{
			RunDetached(() => callback.InvokeAsync());
		}
	}

	private Dictionary<string, object?> WatchOptions() => new()
	{
		["threshold"] = double.IsNaN(Threshold) ? 0.1 : Math.Clamp(Threshold, 0, 1),
		["rootMargin"] = ValidRootMargin(RootMargin),
		["once"] = Once,
		["debounce"] = Math.Max(0, Debounce),
		["leaves"] = ReportsLeaves
	};

	private static bool SameOptions(Dictionary<string, object?>? a, Dictionary<string, object?>? b) =>
		a is null || b is null
			? a is null && b is null
			: a.Count == b.Count && a.All(pair => b.TryGetValue(pair.Key, out object? other) && Equals(pair.Value, other));

	// The browser throws on a margin that isn't one to four lengths in px or %, which would stop the whole watch.
	internal static string ValidRootMargin(string? margin) =>
		margin is not null && MarginPattern().IsMatch(margin) ? margin.Trim() : "0px";

	private static string AnimationToKebab(MokaRevealAnimation animation) => animation switch
	{
		MokaRevealAnimation.FadeIn => "fade-in",
		MokaRevealAnimation.FadeUp => "fade-up",
		MokaRevealAnimation.FadeDown => "fade-down",
		MokaRevealAnimation.FadeLeft => "fade-left",
		MokaRevealAnimation.FadeRight => "fade-right",
		MokaRevealAnimation.ScaleIn => "scale-in",
		MokaRevealAnimation.SlideUp => "slide-up",
		MokaRevealAnimation.None => "none",
		_ => "fade-up"
	};

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		bool leaving = _isVisible && !Once && OnLeave.HasDelegate;
		_isVisible = false;

		if (_boundOptions is not null)
		{
			_boundOptions = null;
			await SafeModuleInvokeVoidAsync(ModulePath, "unobserve", _elementRef);
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();

		if (leaving)
		{
			await LeaveOnRemovalAsync();
		}
	}

	// Disposal runs inside the render that removed the component, and a UI update there isn't supported
	// (component-disposal.md), while OnLeave renders whoever listens. Waiting for that render to finish makes it an
	// ordinary update for a listener still on the page, and nothing at all for one removed with it.
	private async Task LeaveOnRemovalAsync()
	{
		await Task.Yield();
		try
		{
			await OnLeave.InvokeAsync();
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
		}
	}

	[GeneratedRegex(@"^\s*-?(\d+(\.\d+)?|\.\d+)(px|%)(\s+-?(\d+(\.\d+)?|\.\d+)(px|%)){0,3}\s*$", RegexOptions.CultureInvariant)]
	private static partial Regex MarginPattern();
}
