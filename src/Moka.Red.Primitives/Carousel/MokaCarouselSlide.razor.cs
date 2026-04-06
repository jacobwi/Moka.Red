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

	/// <summary>Parent carousel reference.</summary>
	[CascadingParameter]
	public MokaCarousel? Parent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-carousel-slide";

	/// <inheritdoc />
	protected override void OnInitialized() => Parent?.RegisterSlide(this);

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		Parent?.UnregisterSlide(this);
		await base.DisposeAsyncCore();
	}
}
