using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Internal;
using Moka.Red.Feedback.Popover;

namespace Moka.Red.Feedback.Onboarding;

/// <summary>
///     A spotlight tour/walkthrough overlay that highlights page elements
///     and shows tooltip-like step cards. Uses JS interop to measure
///     target element positions via <c>getBoundingClientRect</c>.
/// </summary>
public partial class MokaOnboarding : MokaComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Feedback/Onboarding/MokaOnboarding.razor.js";

	private readonly string _titleId = $"moka-onboarding-{Guid.NewGuid():N}-title";
	private bool _active;
	private int _activeStep;
	private ElementReference _card;
	private DotNetObjectReference<MokaOnboarding>? _dotNetRef;
	private MokaOverlayFocusTrap? _focusTrap;
	private bool? _lastActive;
	private int? _lastActiveStep;
	private bool _targetMissing;
	private ElementRect? _targetRect;
	private int? _viewportWatch;

	/// <summary>The ordered list of onboarding steps.</summary>
	[Parameter]
	[EditorRequired]
	public IReadOnlyList<MokaOnboardingStep> Steps { get; set; } = [];

	/// <summary>The zero-based index of the current step. Two-way bindable.</summary>
	[Parameter]
	public int ActiveStep { get; set; }

	/// <summary>Callback invoked when the active step changes.</summary>
	[Parameter]
	public EventCallback<int> ActiveStepChanged { get; set; }

	/// <summary>Whether the onboarding tour is currently active. Two-way bindable.</summary>
	[Parameter]
	public bool Active { get; set; }

	/// <summary>Callback invoked when the active state changes.</summary>
	[Parameter]
	public EventCallback<bool> ActiveChanged { get; set; }

	/// <summary>Callback invoked when the tour is completed (user clicks Next on the last step).</summary>
	[Parameter]
	public EventCallback OnComplete { get; set; }

	/// <summary>Callback invoked when the user skips the tour.</summary>
	[Parameter]
	public EventCallback OnSkip { get; set; }

	/// <summary>Whether to show a Skip button. Defaults to true.</summary>
	[Parameter]
	public bool ShowSkipButton { get; set; } = true;

	/// <summary>Whether to show "Step X of Y" text. Defaults to true.</summary>
	[Parameter]
	public bool ShowStepCount { get; set; } = true;

	/// <summary>Opacity of the overlay backdrop (0.0 to 1.0). Defaults to 0.5.</summary>
	[Parameter]
	public double OverlayOpacity { get; set; } = 0.5;

	/// <inheritdoc />
	protected override string RootClass => "moka-onboarding";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("--moka-onboarding-overlay-opacity", OverlayOpacity.ToString("F2", CultureInfo.InvariantCulture))
		.AddStyle(Style)
		.Build();

	private MokaOnboardingStep? CurrentStep =>
		Steps.Count > 0 && _activeStep >= 0 && _activeStep < Steps.Count
			? Steps[_activeStep]
			: null;

	private string TitleId => _titleId;

	private bool IsShowing => _active && CurrentStep is not null;

	// A step whose target is not on the page still shows its card, centred and without a spotlight,
	// so the overlay never leaves the user with nothing to click.
	private string TooltipCssClass => new CssBuilder("moka-onboarding-tooltip")
		.AddClass("moka-onboarding-tooltip--centered", _targetRect is null && _targetMissing)
		.Build();

	private bool IsFirstStep => _activeStep == 0;
	private bool IsLastStep => _activeStep >= Steps.Count - 1;

	private string SpotlightStyle
	{
		get
		{
			if (_targetRect is null)
			{
				return "display: none;";
			}

			const int padding = 8;
			return new StyleBuilder()
				.AddStyle("top", Px(_targetRect.Top - padding))
				.AddStyle("left", Px(_targetRect.Left - padding))
				.AddStyle("width", Px(_targetRect.Width + padding * 2))
				.AddStyle("height", Px(_targetRect.Height + padding * 2))
				.Build() ?? string.Empty;
		}
	}

	private string TooltipStyle
	{
		get
		{
			if (_targetRect is null)
			{
				return _targetMissing ? string.Empty : "display: none;";
			}

			MokaOnboardingStep? step = CurrentStep;
			MokaPopoverPosition position = step?.Position ?? MokaPopoverPosition.Bottom;

			return position switch
			{
				MokaPopoverPosition.Top or MokaPopoverPosition.TopStart or MokaPopoverPosition.TopEnd =>
					new StyleBuilder()
						.AddStyle("bottom", Px(_targetRect.ViewportHeight - _targetRect.Top + 12))
						.AddStyle("left", Px(_targetRect.Left))
						.Build() ?? string.Empty,

				MokaPopoverPosition.Left =>
					new StyleBuilder()
						.AddStyle("top", Px(_targetRect.Top))
						.AddStyle("right", Px(_targetRect.ViewportWidth - _targetRect.Left + 12))
						.Build() ?? string.Empty,

				MokaPopoverPosition.Right =>
					new StyleBuilder()
						.AddStyle("top", Px(_targetRect.Top))
						.AddStyle("left", Px(_targetRect.Right + 12))
						.Build() ?? string.Empty,

				_ => // Bottom (default)
					new StyleBuilder()
						.AddStyle("top", Px(_targetRect.Bottom + 12))
						.AddStyle("left", Px(_targetRect.Left))
						.Build() ?? string.Empty
			};
		}
	}

	// Element boxes are fractional. With the current culture a comma-decimal locale wrote "92,5px",
	// which the browser drops, so the spotlight and the card lost their place.
	private static string Px(double value) => string.Create(CultureInfo.InvariantCulture, $"{value}px");

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// A tour that starts active renders before JS can measure its target, and nothing rendered it
		// again: the spotlight and the card stayed hidden until some unrelated render. A render with
		// nothing to show measures, and renders again once the target is found. Steps are otherwise
		// measured before they render (MoveToStepAsync, OnParametersSetAsync). Measuring after every
		// render, with a render for each change, would never settle on a moving target.
		if (_active && CurrentStep is not null && _targetRect is null && !_targetMissing)
		{
			await UpdateTargetRect();

			if (_targetRect is not null || _targetMissing)
			{
				StateHasChanged();
			}
		}

		await SyncViewportWatchAsync();
		_focusTrap ??= new MokaOverlayFocusTrap(
			element => SafeModuleInvokeAsync<int>(ModulePath, "trapFocus", element),
			handle => SafeModuleInvokeVoidAsync(ModulePath, "releaseFocus", handle));
		await _focusTrap.SyncAsync(() => IsShowing, _card);

		await base.OnAfterRenderAsync(firstRender);
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Active and ActiveStep only seed the tour when the parent passes a new value. Copying them
		// on every parent render sent an unbound tour back a step, or reopened one the user closed.
		bool turnedOn = false;
		if (_lastActive != Active)
		{
			_lastActive = Active;
			_active = Active;
			turnedOn = Active;
		}

		// A tour the parent turns on begins at the step the parent asks for. The step the last run
		// ended on used to stay, and a parent that never binds ActiveStep passes the same 0 every
		// time, so a finished tour reopened on its last step.
		if (_lastActiveStep != ActiveStep || turnedOn)
		{
			_lastActiveStep = ActiveStep;
			_activeStep = ActiveStep;
		}

		// The last run's spotlight belongs to a step that is not showing. OnParametersSetAsync, or
		// the first render, measures the one that is.
		if (turnedOn)
		{
			_targetRect = null;
			_targetMissing = false;
		}
	}

	/// <inheritdoc />
	protected override async Task OnParametersSetAsync()
	{
		if (_active && CurrentStep is not null && HasRendered)
		{
			await UpdateTargetRect();
		}
	}

	private async Task UpdateTargetRect()
	{
		if (CurrentStep is null)
		{
			return;
		}

		// The selector goes over as data, not spliced into a script for eval: a strict content security
		// policy blocks eval, and a backslash before a quote got past the old escaping.
		ElementRect? rect = await SafeModuleInvokeAsync<ElementRect>(ModulePath, "measureTarget",
			CurrentStep.TargetSelector);

		_targetRect = rect;
		_targetMissing = rect is null && HasRendered;
	}

	// The spotlight is measured in viewport coordinates, so it has to be measured again whenever the
	// page scrolls or the window resizes while the tour shows.
	private async Task SyncViewportWatchAsync()
	{
		if (IsShowing && _viewportWatch is null)
		{
			_dotNetRef ??= DotNetObjectReference.Create(this);
			_viewportWatch = await SafeModuleInvokeAsync<int?>(ModulePath, "watchViewport", _dotNetRef);
		}
		else if (!IsShowing && _viewportWatch is { } handle)
		{
			_viewportWatch = null;
			await SafeModuleInvokeVoidAsync(ModulePath, "unwatchViewport", handle);
		}
	}

	/// <summary>Called from JavaScript when the page scrolls or the window resizes.</summary>
	[JSInvokable]
	public async Task OnViewportChanged()
	{
		if (!IsShowing)
		{
			return;
		}

		await UpdateTargetRect();
		StateHasChanged();
	}

	// Escape leaves the tour the way the skip button does, so only when skipping is allowed.
	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == "Escape" && ShowSkipButton && IsShowing)
		{
			await Skip();
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_viewportWatch is { } handle)
		{
			_viewportWatch = null;
			await SafeModuleInvokeVoidAsync(ModulePath, "unwatchViewport", handle);
		}

		if (_focusTrap is not null)
		{
			await _focusTrap.ReleaseAsync();
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}

	private async Task GoNext()
	{
		if (IsLastStep)
		{
			await Complete();
			return;
		}

		await MoveToStepAsync(_activeStep + 1);
	}

	private async Task GoPrevious()
	{
		if (IsFirstStep)
		{
			return;
		}

		await MoveToStepAsync(_activeStep - 1);
	}

	private async Task MoveToStepAsync(int step)
	{
		_activeStep = step;
		_targetMissing = false;

		// Measured before the render: an unbound tour gets no parameter pass to measure in, and the
		// render after this click would still show the old target's spotlight.
		await UpdateTargetRect();

		if (ActiveStepChanged.HasDelegate)
		{
			await ActiveStepChanged.InvokeAsync(step);
		}
	}

	private async Task Skip()
	{
		await Deactivate();

		if (OnSkip.HasDelegate)
		{
			await OnSkip.InvokeAsync();
		}
	}

	private async Task Complete()
	{
		await Deactivate();

		if (OnComplete.HasDelegate)
		{
			await OnComplete.InvokeAsync();
		}
	}

	private async Task Deactivate()
	{
		_active = false;
		_targetRect = null;
		_targetMissing = false;

		if (ActiveChanged.HasDelegate)
		{
			await ActiveChanged.InvokeAsync(false);
		}
	}

	/// <summary>
	///     Represents the bounding rectangle of a DOM element plus viewport dimensions.
	/// </summary>
	internal sealed class ElementRect
	{
		/// <summary>Distance from the top of the viewport.</summary>
		public double Top { get; set; }

		/// <summary>Distance from the left of the viewport.</summary>
		public double Left { get; set; }

		/// <summary>Element width.</summary>
		public double Width { get; set; }

		/// <summary>Element height.</summary>
		public double Height { get; set; }

		/// <summary>Right edge position.</summary>
		public double Right { get; set; }

		/// <summary>Bottom edge position.</summary>
		public double Bottom { get; set; }

		/// <summary>Viewport width for position calculations.</summary>
		public double ViewportWidth { get; set; }

		/// <summary>Viewport height for position calculations.</summary>
		public double ViewportHeight { get; set; }
	}
}
