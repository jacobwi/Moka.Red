using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;

namespace Moka.Red.Primitives.Carousel;

/// <summary>
///     Individual slide within a <see cref="MokaCarousel" />.
///     Registers itself with the parent carousel on initialization.
/// </summary>
public partial class MokaCarouselSlide : MokaComponentBase
{
	/// <summary>Slide content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Where the slide stands among the carousel's slides: lower comes first, and slides with the same <c>Order</c>
	///     keep the order the markup gives. Default 0. Set it on a slide rendered by a component of your own, which
	///     otherwise joins after the carousel's own slides, and number the slides in the order they stand: the track
	///     shows them where the markup puts them.
	/// </summary>
	[Parameter]
	public int Order { get; set; }

	/// <summary>Parent carousel reference.</summary>
	[CascadingParameter]
	public MokaCarousel? Parent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-carousel-slide";

	/// <inheritdoc />
	protected override void OnInitialized() => Parent?.RegisterSlide(this);

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		Parent?.SawSlide(this);
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		Parent?.UnregisterSlide(this);
		await base.DisposeAsyncCore();
	}
}
