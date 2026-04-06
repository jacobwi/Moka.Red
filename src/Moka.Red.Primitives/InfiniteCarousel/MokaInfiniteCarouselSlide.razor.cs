using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;

namespace Moka.Red.Primitives.InfiniteCarousel;

/// <summary>
///     Individual slide within a <see cref="MokaInfiniteCarousel" />.
///     Registers itself with the parent carousel on initialization.
/// </summary>
public partial class MokaInfiniteCarouselSlide : MokaComponentBase
{
	/// <summary>Slide content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Parent infinite carousel reference.</summary>
	[CascadingParameter]
	public MokaInfiniteCarousel? Parent { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-infinite-carousel-slide";

	/// <inheritdoc />
	protected override void OnInitialized() => Parent?.RegisterSlide(this);

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		Parent?.UnregisterSlide(this);
		await base.DisposeAsyncCore();
	}
}
