using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.SwipeActions;

/// <summary>
///     A swipe-to-reveal action container (similar to iOS swipe-to-delete on list items).
///     Supports swipe left to reveal right actions and swipe right to reveal left actions.
///     Works with touch events on mobile and mouse drag on desktop.
/// </summary>
public partial class MokaSwipeActions
{
	private double _currentX;
	private bool _isDragging;
	private bool _isRevealed;
	private string _revealSide = "";
	private double _startX;

	/// <summary>The main content of the swipeable area. Required.</summary>
	[Parameter]
	[EditorRequired]
	public RenderFragment ChildContent { get; set; } = default!;

	/// <summary>Actions revealed when swiping right (left side actions).</summary>
	[Parameter]
	public RenderFragment? LeftActions { get; set; }

	/// <summary>Actions revealed when swiping left (right side actions).</summary>
	[Parameter]
	public RenderFragment? RightActions { get; set; }

	/// <summary>Pixel distance threshold to trigger action reveal. Defaults to 80.</summary>
	[Parameter]
	public int Threshold { get; set; } = 80;

	/// <inheritdoc />
	protected override string RootClass => "moka-swipe-actions";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-swipe-actions--revealed-left", _isRevealed && _revealSide == "left")
		.AddClass("moka-swipe-actions--revealed-right", _isRevealed && _revealSide == "right")
		.AddClass("moka-swipe-actions--dragging", _isDragging)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle(Style)
		.Build();

	private string ContentStyle
	{
		get
		{
			if (_isDragging)
			{
				double offset = _currentX - _startX;
				// Clamp based on available actions
				if (LeftActions is null && offset > 0)
				{
					offset = 0;
				}

				if (RightActions is null && offset < 0)
				{
					offset = 0;
				}

				return $"transform: translateX({offset}px)";
			}

			if (_isRevealed && _revealSide == "left")
			{
				return $"transform: translateX({Threshold}px)";
			}

			if (_isRevealed && _revealSide == "right")
			{
				return $"transform: translateX(-{Threshold}px)";
			}

			return "transform: translateX(0)";
		}
	}

	/// <summary>Override to allow internal state changes to trigger re-render.</summary>
	protected override bool ShouldRender() => true;

	private void HandlePointerDown(PointerEventArgs e)
	{
		_startX = e.ClientX;
		_currentX = e.ClientX;
		_isDragging = true;
	}

	private void HandlePointerMove(PointerEventArgs e)
	{
		if (!_isDragging)
		{
			return;
		}

		_currentX = e.ClientX;
	}

	private void HandlePointerUp(PointerEventArgs e)
	{
		if (!_isDragging)
		{
			return;
		}

		_isDragging = false;

		double offset = _currentX - _startX;

		if (offset > Threshold && LeftActions is not null)
		{
			_isRevealed = true;
			_revealSide = "left";
		}
		else if (offset < -Threshold && RightActions is not null)
		{
			_isRevealed = true;
			_revealSide = "right";
		}
		else
		{
			_isRevealed = false;
			_revealSide = "";
		}
	}

	/// <summary>Resets the swipe state, hiding any revealed actions.</summary>
	public void Reset()
	{
		_isRevealed = false;
		_revealSide = "";
		_isDragging = false;
		ForceRender();
	}
}
