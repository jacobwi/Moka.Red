using System.Globalization;
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
	private readonly MokaChildList<MokaCarouselSlide> _slides;
	private int _activeIndex;
	private Timer? _autoPlayTimer;
	private int? _lastActiveIndex;

	/// <summary>Creates the carousel.</summary>
	public MokaCarousel()
	{
		_slides = new MokaChildList<MokaCarouselSlide>(StateHasChanged, SlidesChanged, static slide => slide.Order);
	}

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

	/// <summary>
	///     The carousel's width to its height, as the CSS <c>aspect-ratio</c> property takes it, such as "16/9" or "1/1".
	///     Every slide is then that tall, so content made to fill its slide, such as an image with <c>width</c> and
	///     <c>height</c> at 100% and an <c>object-fit</c>, fills the carousel. A value that isn't an aspect ratio is
	///     ignored. Null (the default): the carousel is as tall as its tallest slide.
	/// </summary>
	[Parameter]
	public string? AspectRatio { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-carousel";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-fill-width")
		.AddClass("moka-carousel--ratio", HasAspectRatio)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("aspect-ratio", AspectRatio?.Trim(), HasAspectRatio)
		.AddStyle(Style)
		.Build();

	private bool HasAspectRatio => CssValues.IsAspectRatio(AspectRatio);

	private int SlideCount => _slides.Count;

	private string TrackStyle => $"transform: translateX(-{_activeIndex * 100}%)";

	/// <summary>Override ShouldRender to always return true for timer-driven updates.</summary>
	protected override bool ShouldRender() => true;

	private string DotCssClass(int index) => new CssBuilder("moka-carousel-dot")
		.AddClass("moka-carousel-dot--active", index == _activeIndex)
		.Build();

	private static string SlideName(int index) => string.Create(CultureInfo.CurrentCulture, $"Go to slide {index + 1}");

	// The slides keep the order they stand in the markup, as QuickGrid collects its columns, so a slide inside an @if
	// that comes back takes its place again.
	internal void RegisterSlide(MokaCarouselSlide slide) => _slides.Add(slide);

	internal void UnregisterSlide(MokaCarouselSlide slide) => _slides.Remove(slide);

	internal void SawSlide(MokaCarouselSlide slide) => _slides.Saw(slide);

	// The slide showing stays showing wherever it moved to, and a bound parent hears its new position. When it left,
	// the slide that takes its place shows.
	private void SlidesChanged(IReadOnlyList<MokaCarouselSlide> before)
	{
		int active = _slides.IndexNow(before, _activeIndex);
		if (active == _activeIndex)
		{
			return;
		}

		_activeIndex = active;
		if (ActiveIndexChanged.HasDelegate)
		{
			RunDetached(() => ActiveIndexChanged.InvokeAsync(active));
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
		RunDetached(async () =>
		{
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
		if (_autoPlayTimer is not null)
		{
			await _autoPlayTimer.DisposeAsync();
			_autoPlayTimer = null;
		}

		await base.DisposeAsyncCore();
	}
}
