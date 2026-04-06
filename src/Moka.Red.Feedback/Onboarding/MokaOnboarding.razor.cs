using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Popover;

namespace Moka.Red.Feedback.Onboarding;

/// <summary>
///     A spotlight tour/walkthrough overlay that highlights page elements
///     and shows tooltip-like step cards. Uses JS interop to measure
///     target element positions via <c>getBoundingClientRect</c>.
/// </summary>
public partial class MokaOnboarding : MokaComponentBase
{
	private ElementRect? _targetRect;

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
		Steps.Count > 0 && ActiveStep >= 0 && ActiveStep < Steps.Count
			? Steps[ActiveStep]
			: null;

	private bool IsFirstStep => ActiveStep == 0;
	private bool IsLastStep => ActiveStep >= Steps.Count - 1;

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
				.AddStyle("top", $"{_targetRect.Top - padding}px")
				.AddStyle("left", $"{_targetRect.Left - padding}px")
				.AddStyle("width", $"{_targetRect.Width + padding * 2}px")
				.AddStyle("height", $"{_targetRect.Height + padding * 2}px")
				.Build() ?? string.Empty;
		}
	}

	private string TooltipStyle
	{
		get
		{
			if (_targetRect is null)
			{
				return "display: none;";
			}

			MokaOnboardingStep? step = CurrentStep;
			MokaPopoverPosition position = step?.Position ?? MokaPopoverPosition.Bottom;

			return position switch
			{
				MokaPopoverPosition.Top or MokaPopoverPosition.TopStart or MokaPopoverPosition.TopEnd =>
					new StyleBuilder()
						.AddStyle("bottom", $"{_targetRect.ViewportHeight - _targetRect.Top + 12}px")
						.AddStyle("left", $"{_targetRect.Left}px")
						.Build() ?? string.Empty,

				MokaPopoverPosition.Left =>
					new StyleBuilder()
						.AddStyle("top", $"{_targetRect.Top}px")
						.AddStyle("right", $"{_targetRect.ViewportWidth - _targetRect.Left + 12}px")
						.Build() ?? string.Empty,

				MokaPopoverPosition.Right =>
					new StyleBuilder()
						.AddStyle("top", $"{_targetRect.Top}px")
						.AddStyle("left", $"{_targetRect.Right + 12}px")
						.Build() ?? string.Empty,

				_ => // Bottom (default)
					new StyleBuilder()
						.AddStyle("top", $"{_targetRect.Bottom + 12}px")
						.AddStyle("left", $"{_targetRect.Left}px")
						.Build() ?? string.Empty
			};
		}
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (Active && CurrentStep is not null)
		{
			await UpdateTargetRect();
		}

		await base.OnAfterRenderAsync(firstRender);
	}

	protected override async Task OnParametersSetAsync()
	{
		if (Active && CurrentStep is not null && HasRendered)
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

		ElementRect? rect = await SafeJsInvokeAsync<ElementRect?>(
			"eval",
			$"(function(){{ var el = document.querySelector('{EscapeSelector(CurrentStep.TargetSelector)}'); if(!el) return null; var r = el.getBoundingClientRect(); return {{ top: r.top, left: r.left, width: r.width, height: r.height, right: r.right, bottom: r.bottom, viewportWidth: window.innerWidth, viewportHeight: window.innerHeight }}; }})()");

		_targetRect = rect;
	}

	private async Task GoNext()
	{
		if (IsLastStep)
		{
			await Complete();
			return;
		}

		ActiveStep++;

		if (ActiveStepChanged.HasDelegate)
		{
			await ActiveStepChanged.InvokeAsync(ActiveStep);
		}
	}

	private async Task GoPrevious()
	{
		if (IsFirstStep)
		{
			return;
		}

		ActiveStep--;

		if (ActiveStepChanged.HasDelegate)
		{
			await ActiveStepChanged.InvokeAsync(ActiveStep);
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
		Active = false;
		_targetRect = null;

		if (ActiveChanged.HasDelegate)
		{
			await ActiveChanged.InvokeAsync(false);
		}
	}

	private static string EscapeSelector(string selector) =>
		selector.Replace("'", "\\'", StringComparison.Ordinal);

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
