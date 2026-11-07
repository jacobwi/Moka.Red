using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Primitives.SwipeActions;

namespace Moka.Red.Primitives.Tests.Components;

// A pointermove listener is a round trip and a render for every move across the element, so it's only there
// during a drag.
public class MokaSwipeActionsDragTests : BunitContext
{
	private const string Content = ".moka-swipe-actions__content";

	public MokaSwipeActionsDragTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void Idle_ThereAreNoMoveOrLeaveListeners()
	{
		IRenderedComponent<MokaSwipeActions> cut = Render();

		Assert.False(cut.Find(Content).HasAttribute("blazor:onpointermove"));
		Assert.False(cut.Find(Content).HasAttribute("blazor:onpointerleave"));
	}

	[Fact]
	public void Dragging_AttachesThem()
	{
		IRenderedComponent<MokaSwipeActions> cut = Render();

		cut.Find(Content).PointerDown(new PointerEventArgs { ClientX = 200 });

		Assert.True(cut.Find(Content).HasAttribute("blazor:onpointermove"));
		Assert.True(cut.Find(Content).HasAttribute("blazor:onpointerleave"));
	}

	[Fact]
	public void ReleasingRemovesThem_AndPastTheThresholdReveals()
	{
		IRenderedComponent<MokaSwipeActions> cut = Render();
		cut.Find(Content).PointerDown(new PointerEventArgs { ClientX = 200 });
		cut.Find(Content).PointerMove(new PointerEventArgs { ClientX = 100 });

		cut.Find(Content).PointerUp();

		Assert.False(cut.Find(Content).HasAttribute("blazor:onpointermove"));
		Assert.Contains("moka-swipe-actions--revealed-right", cut.Find(".moka-swipe-actions").ClassList);
	}

	// A mouse released outside the content sends nothing here, which used to leave the drag running.
	[Fact]
	public void LeavingEndsTheDrag()
	{
		IRenderedComponent<MokaSwipeActions> cut = Render();
		cut.Find(Content).PointerDown(new PointerEventArgs { ClientX = 200 });
		cut.Find(Content).PointerMove(new PointerEventArgs { ClientX = 100 });

		cut.Find(Content).PointerLeave();

		Assert.DoesNotContain("moka-swipe-actions--dragging", cut.Find(".moka-swipe-actions").ClassList);
		Assert.False(cut.Find(Content).HasAttribute("blazor:onpointermove"));
		Assert.Contains("moka-swipe-actions--revealed-right", cut.Find(".moka-swipe-actions").ClassList);
	}

	private IRenderedComponent<MokaSwipeActions> Render() =>
		Render<MokaSwipeActions>(p => p
			.Add(x => x.ChildContent, (RenderFragment)(b => b.AddContent(0, "row")))
			.Add(x => x.RightActions, (RenderFragment)(b => b.AddContent(0, "delete"))));
}
