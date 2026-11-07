using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.Knob;

namespace Moka.Red.Forms.Tests.Components;

// A pointermove listener is a round trip and a render for every move across the element, so it's only there
// during a drag.
public class MokaKnobDragTests : BunitContext
{
	private const string DragModule = "./_content/Moka.Red.Core/moka-drag.js";

	private readonly BunitJSModuleInterop _drag;

	public MokaKnobDragTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_drag = JSInterop.SetupModule(DragModule);
		_drag.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void Idle_ThereIsNoMoveListener()
	{
		Assert.False(Render().Find(".moka-knob").HasAttribute("blazor:onpointermove"));
	}

	[Fact]
	public void Dragging_AttachesItAndMovesTheValue()
	{
		List<double> values = [];
		IRenderedComponent<MokaKnob> cut = Render(values);

		cut.Find(".moka-knob").PointerDown(new PointerEventArgs { ClientY = 100, PointerId = 7 });
		IElement knob = cut.Find(".moka-knob");
		Assert.True(knob.HasAttribute("blazor:onpointermove"));

		knob.PointerMove(new PointerEventArgs { ClientY = 80 });

		Assert.Equal([60d], values);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void TheDragEnding_RemovesIt(bool cancelled)
	{
		IRenderedComponent<MokaKnob> cut = Render();
		cut.Find(".moka-knob").PointerDown(new PointerEventArgs { ClientY = 100 });

		if (cancelled)
		{
			cut.Find(".moka-knob").PointerCancel();
		}
		else
		{
			cut.Find(".moka-knob").PointerUp();
		}

		Assert.False(cut.Find(".moka-knob").HasAttribute("blazor:onpointermove"));
	}

	[Fact]
	public void Dragging_CapturesThePointer_SoItCanLeaveTheKnob()
	{
		IRenderedComponent<MokaKnob> cut = Render();

		cut.Find(".moka-knob").PointerDown(new PointerEventArgs { ClientY = 100, PointerId = 7 });

		JSRuntimeInvocation capture = Assert.Single(_drag.Invocations["capturePointer"]);
		Assert.Equal(7L, capture.Arguments[1]);
	}

	[Fact]
	public void Disabled_ItDoesNotStartADrag()
	{
		IRenderedComponent<MokaKnob> cut = Render<MokaKnob>(p => p.Add(x => x.Disabled, true));

		cut.Find(".moka-knob").PointerDown(new PointerEventArgs { ClientY = 100 });

		Assert.False(cut.Find(".moka-knob").HasAttribute("blazor:onpointermove"));
		Assert.Empty(_drag.Invocations["capturePointer"]);
	}

	private IRenderedComponent<MokaKnob> Render(List<double>? values = null) =>
		Render<MokaKnob>(p => p
			.Add(x => x.Value, 50)
			.Add(x => x.Min, 0)
			.Add(x => x.Max, 100)
			.Add(x => x.Step, 1)
			.Add(x => x.ValueChanged, value => values?.Add(value)));
}
