using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Carousel;

/// <summary>
///     Image/content carousel with navigation arrows, dot indicators, and auto-play support.
///     Uses CSS transforms for slide transitions — no JavaScript required.
/// </summary>
public partial class MokaCarousel : MokaVisualComponentBase
{
	private readonly List<MokaCarouselSlide> _slides = [];
	private Timer? _autoPlayTimer;
	private bool _disposed;

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
		.AddClass(Class)
		.Build();

	private int SlideCount => _slides.Count;

	private string TrackStyle => $"transform: translateX(-{ActiveIndex * 100}%)";

	/// <summary>Override ShouldRender to always return true for timer-driven updates.</summary>
	protected override bool ShouldRender() => true;

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
			if (ActiveIndex >= _slides.Count && _slides.Count > 0)
			{
				ActiveIndex = _slides.Count - 1;
			}

			StateHasChanged();
		}
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
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

		int newIndex = ActiveIndex - 1;
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

		int newIndex = ActiveIndex + 1;
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
		if (index != ActiveIndex)
		{
			ActiveIndex = index;
			if (ActiveIndexChanged.HasDelegate)
			{
				await ActiveIndexChanged.InvokeAsync(ActiveIndex);
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
