using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.InfiniteCarousel;

/// <summary>
///     Continuous-loop carousel that wraps seamlessly from last to first slide (and vice versa).
///     Uses a clone technique — the first and last slides are duplicated at opposite ends
///     so the transition appears infinite.
/// </summary>
public partial class MokaInfiniteCarousel : MokaVisualComponentBase
{
	private readonly List<MokaInfiniteCarouselSlide> _slides = [];
	private Timer? _autoPlayTimer;
	private int _currentIndex;
	private bool _disposed;
	private bool _isTransitioning;
	private bool _pausedByHover;

	/// <summary>Carousel slide content (MokaInfiniteCarouselSlide children).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Whether slides advance automatically. Default true.</summary>
	[Parameter]
	public bool AutoPlay { get; set; } = true;

	/// <summary>Auto-play interval in milliseconds. Default 4000.</summary>
	[Parameter]
	public int Interval { get; set; } = 4000;

	/// <summary>Slide transition speed in milliseconds. Default 500.</summary>
	[Parameter]
	public int Speed { get; set; } = 500;

	/// <summary>Whether to show navigation arrows. Default true.</summary>
	[Parameter]
	public bool ShowControls { get; set; } = true;

	/// <summary>Whether to show dot indicators. Default true.</summary>
	[Parameter]
	public bool ShowIndicators { get; set; } = true;

	/// <summary>Whether to pause auto-play on hover. Default true.</summary>
	[Parameter]
	public bool PauseOnHover { get; set; } = true;

	/// <summary>Direction of slide movement. Default Horizontal.</summary>
	[Parameter]
	public MokaCarouselDirection Direction { get; set; } = MokaCarouselDirection.Horizontal;

	/// <summary>Currently active slide index (zero-based). Two-way bindable.</summary>
	[Parameter]
	public int ActiveIndex { get; set; }

	/// <summary>Callback when the active slide index changes.</summary>
	[Parameter]
	public EventCallback<int> ActiveIndexChanged { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-infinite-carousel";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-infinite-carousel--vertical", Direction == MokaCarouselDirection.Vertical)
		.AddClass(Class)
		.Build();

	private int SlideCount => _slides.Count;

	// Track offset includes +1 for the cloned last slide prepended at position 0
	private int TrackIndex => _currentIndex + 1;

	private string TrackStyle
	{
		get
		{
			string prop = Direction == MokaCarouselDirection.Horizontal ? "translateX" : "translateY";
			string transition = _isTransitioning ? "none" : $"transform {Speed}ms ease";
			return $"transform: {prop}(-{TrackIndex * 100}%); transition: {transition};";
		}
	}

	/// <summary>The real slide index clamped to valid range, for indicator highlighting.</summary>
	private int RealIndex
	{
		get
		{
			if (_currentIndex < 0)
			{
				return SlideCount - 1;
			}

			return _currentIndex >= SlideCount ? 0 : _currentIndex;
		}
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <summary>Registers a slide with the carousel. Called by child slides.</summary>
	internal void RegisterSlide(MokaInfiniteCarouselSlide slide)
	{
		if (!_slides.Contains(slide))
		{
			_slides.Add(slide);
			StateHasChanged();
		}
	}

	/// <summary>Unregisters a slide from the carousel. Called by child slides on dispose.</summary>
	internal void UnregisterSlide(MokaInfiniteCarouselSlide slide)
	{
		if (_slides.Remove(slide))
		{
			if (_currentIndex >= _slides.Count && _slides.Count > 0)
			{
				_currentIndex = _slides.Count - 1;
			}

			StateHasChanged();
		}
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		_currentIndex = ActiveIndex;
		ConfigureAutoPlay();
	}

	private void ConfigureAutoPlay()
	{
		if (AutoPlay && !_pausedByHover && _autoPlayTimer is null)
		{
			_autoPlayTimer = new Timer(OnAutoPlayTick, null, Interval, Interval);
		}
		else if ((!AutoPlay || _pausedByHover) && _autoPlayTimer is not null)
		{
			_autoPlayTimer.Dispose();
			_autoPlayTimer = null;
		}
	}

	private void OnAutoPlayTick(object? state)
	{
		if (_disposed)
		{
			return;
		}

		InvokeAsync(async () =>
		{
			if (_disposed)
			{
				return;
			}

			await GoToNext();
			StateHasChanged();
		});
	}

	private async Task GoToPrevious()
	{
		if (SlideCount == 0 || _isTransitioning)
		{
			return;
		}

		int newIndex = _currentIndex - 1;
		if (newIndex < 0)
		{
			// Animate to clone of last slide (index -1 maps to TrackIndex 0)
			_currentIndex = -1;
			StateHasChanged();
			await Task.Delay(Speed + 20);
			// Snap without animation to the real last slide
			_isTransitioning = true;
			_currentIndex = SlideCount - 1;
			StateHasChanged();
			await Task.Delay(20);
			_isTransitioning = false;
			StateHasChanged();
		}
		else
		{
			_currentIndex = newIndex;
		}

		await SyncActiveIndex();
	}

	private async Task GoToNext()
	{
		if (SlideCount == 0 || _isTransitioning)
		{
			return;
		}

		int newIndex = _currentIndex + 1;
		if (newIndex >= SlideCount)
		{
			// Animate to clone of first slide (index SlideCount maps to TrackIndex SlideCount+1)
			_currentIndex = SlideCount;
			StateHasChanged();
			await Task.Delay(Speed + 20);
			// Snap without animation back to the real first slide
			_isTransitioning = true;
			_currentIndex = 0;
			StateHasChanged();
			await Task.Delay(20);
			_isTransitioning = false;
			StateHasChanged();
		}
		else
		{
			_currentIndex = newIndex;
		}

		await SyncActiveIndex();
	}

	private async Task GoToSlide(int index)
	{
		if (index >= 0 && index < SlideCount && index != _currentIndex)
		{
			_currentIndex = index;
			await SyncActiveIndex();
		}
	}

	private async Task SyncActiveIndex()
	{
		int realIndex = RealIndex;
		if (realIndex != ActiveIndex)
		{
			ActiveIndex = realIndex;
			if (ActiveIndexChanged.HasDelegate)
			{
				await ActiveIndexChanged.InvokeAsync(ActiveIndex);
			}
		}
	}

	private void HandleMouseEnter()
	{
		if (PauseOnHover && AutoPlay)
		{
			_pausedByHover = true;
			_autoPlayTimer?.Dispose();
			_autoPlayTimer = null;
		}
	}

	private void HandleMouseLeave()
	{
		if (PauseOnHover && AutoPlay)
		{
			_pausedByHover = false;
			ConfigureAutoPlay();
		}
	}

	private string DotClass(int index) => new CssBuilder("moka-infinite-carousel-dot")
		.AddClass("moka-infinite-carousel-dot--active", index == RealIndex)
		.Build();

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		_disposed = true;
		if (_autoPlayTimer is not null)
		{
			await _autoPlayTimer.DisposeAsync();
			_autoPlayTimer = null;
		}

		await base.DisposeAsyncCore();
	}
}
