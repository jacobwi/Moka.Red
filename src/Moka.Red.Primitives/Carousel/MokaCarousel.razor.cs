using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Carousel;

/// <summary>
///     Image/content carousel with navigation arrows, dot indicators, and auto-play support.
///     Uses CSS transforms for slide transitions - no JavaScript required.
/// </summary>
public partial class MokaCarousel : MokaVisualComponentBase
{
	private readonly List<MokaCarouselSlide> _slides = [];
	private int _activeIndex;
	private Timer? _autoPlayTimer;
	private bool _disposed;
	private int? _lastActiveIndex;

	/// <summary>Carousel slide content (MokaCarouselSlide children or any content).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Whether slides advance automatically. Default false.</summary>
	[Parameter]
	public bool AutoPlay { get; set; }

	/// <summary>Auto-play interval in milliseconds. Default 5000.</summary>
	[Parameter]
	public int Interval { get; set; } = 5000;

	/// <summary>Whether to show left/right navigation arrows. Default true.</summary>
	[Parameter]
	public bool ShowArrows { get; set; } = true;

	/// <summary>Whether to show dot indicators. Default true.</summary>
	[Parameter]
	public bool ShowDots { get; set; } = true;

	/// <summary>Whether to wrap around at ends. Default true.</summary>
	[Parameter]
	public bool Loop { get; set; } = true;

	/// <summary>Currently active slide index. Two-way bindable.</summary>
	[Parameter]
	public int ActiveIndex { get; set; }

	/// <summary>Callback when the active slide index changes.</summary>
	[Parameter]
	public EventCallback<int> ActiveIndexChanged { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-carousel";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-fill-width")
		.AddClass(Class)
		.Build();

	private int SlideCount => _slides.Count;

	private string TrackStyle => $"transform: translateX(-{_activeIndex * 100}%)";

	/// <summary>Override ShouldRender to always return true for timer-driven updates.</summary>
	protected override bool ShouldRender() => true;

	private string DotCssClass(int index) => new CssBuilder("moka-carousel-dot")
		.AddClass("moka-carousel-dot--active", index == _activeIndex)
		.Build();

	/// <summary>Registers a slide with the carousel. Called by child slides.</summary>
	internal void RegisterSlide(MokaCarouselSlide slide)
	{
		if (!_slides.Contains(slide))
		{
			_slides.Add(slide);
			StateHasChanged();
		}
	}

	/// <summary>Unregisters a slide from the carousel. Called by child slides on dispose.</summary>
	internal void UnregisterSlide(MokaCarouselSlide slide)
	{
		if (_slides.Remove(slide))
		{
			if (_activeIndex >= _slides.Count && _slides.Count > 0)
			{
				_activeIndex = _slides.Count - 1;
			}

			StateHasChanged();
		}
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// ActiveIndex only moves the carousel when the parent passes a new value. Copying it on every
		// parent render sent an unbound carousel back to the slide it started on.
		if (_lastActiveIndex != ActiveIndex)
		{
			_lastActiveIndex = ActiveIndex;
			_activeIndex = ActiveIndex;
		}

		ConfigureAutoPlay();
	}

	private void ConfigureAutoPlay()
	{
		if (AutoPlay && _autoPlayTimer is null)
		{
			_autoPlayTimer = new Timer(OnAutoPlayTick, null, Interval, Interval);
		}
		else if (!AutoPlay && _autoPlayTimer is not null)
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
		if (SlideCount == 0)
		{
			return;
		}

		int newIndex = _activeIndex - 1;
		if (newIndex < 0)
		{
			newIndex = Loop ? SlideCount - 1 : 0;
		}

		await SetActiveIndex(newIndex);
	}

	private async Task GoToNext()
	{
		if (SlideCount == 0)
		{
			return;
		}

		int newIndex = _activeIndex + 1;
		if (newIndex >= SlideCount)
		{
			newIndex = Loop ? 0 : SlideCount - 1;
		}

		await SetActiveIndex(newIndex);
	}

	private async Task GoToSlide(int index)
	{
		if (index >= 0 && index < SlideCount)
		{
			await SetActiveIndex(index);
		}
	}

	private async Task SetActiveIndex(int index)
	{
		if (index != _activeIndex)
		{
			_activeIndex = index;
			if (ActiveIndexChanged.HasDelegate)
			{
				await ActiveIndexChanged.InvokeAsync(index);
			}
		}
	}

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
