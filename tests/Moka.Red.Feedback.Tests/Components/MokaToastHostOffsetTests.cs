using AngleSharp.Dom;
using Bunit;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Feedback.Toast;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Components;

// The host sat flush at the window's edge, so in a desktop app whose own title bar fills the top of the window a
// top-right toast covered the minimise, maximise and close buttons. Each offset keeps one edge clear, on top of the
// host's padding, and only the edges a position sits against move.
public class MokaToastHostOffsetTests : BunitContext
{
	private const string Css = "MokaToastHost.razor.css";

	public MokaToastHostOffsetTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaFeedback();
	}

	public static TheoryData<MokaToastPosition, string, string[]> Edges => new()
	{
		{ MokaToastPosition.TopRight, ".moka-toast-host--top-right", ["top", "right"] },
		{ MokaToastPosition.TopLeft, ".moka-toast-host--top-left", ["top", "left"] },
		{ MokaToastPosition.TopCenter, ".moka-toast-host--top-center", ["top", "left", "right"] },
		{ MokaToastPosition.BottomRight, ".moka-toast-host--bottom-right", ["bottom", "right"] },
		{ MokaToastPosition.BottomLeft, ".moka-toast-host--bottom-left", ["bottom", "left"] },
		{ MokaToastPosition.BottomCenter, ".moka-toast-host--bottom-center", ["bottom", "left", "right"] }
	};

	// Each edge the position sits against reads its own offset, 0 when nothing sets it; the other edges stay free.
	[Theory]
	[MemberData(nameof(Edges))]
	public void EachOffset_MovesOnlyTheEdgeItNames(MokaToastPosition position, string selector, string[] edges)
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>(p => p.Add(x => x.Position, position));
		Assert.Contains(selector[1..], cut.Find(".moka-toast-host").ClassList);

		IReadOnlyDictionary<string, string> rules = ScopedCss.Declarations(Css, selector);
		foreach (string edge in new[] { "top", "right", "bottom", "left" })
		{
			if (edges.Contains(edge))
			{
				Assert.Equal($"var(--moka-toast-offset-{edge}, 0px)", rules[edge]);
			}
			else
			{
				Assert.False(rules.ContainsKey(edge), $"{selector} also sets {edge}.");
			}
		}
	}

	// The padding stays as it was, so an offset adds to it.
	[Fact]
	public void TheHostKeepsItsPadding() =>
		Assert.Equal("var(--moka-spacing-md)", ScopedCss.Declarations(Css, ".moka-toast-host")["padding"]);

	// The centred hosts sit between the side offsets and stay centred with auto margins: a transform would hold the
	// close buttons' fixed tooltips inside the host.
	[Theory]
	[InlineData(".moka-toast-host--top-center")]
	[InlineData(".moka-toast-host--bottom-center")]
	public void TheCentredHosts_StayCentredWithoutATransform(string selector)
	{
		IReadOnlyDictionary<string, string> rules = ScopedCss.Declarations(Css, selector);

		Assert.Equal("auto", rules["margin-inline"]);
		Assert.Equal("fit-content", rules["width"]);
		Assert.False(rules.ContainsKey("transform"));
	}

	// Without offsets, Class or Style the host renders exactly as before: its two classes and no style at all.
	[Fact]
	public void TheDefaults_ChangeNothing()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>();

		IElement host = cut.Find(".moka-toast-host");
		Assert.Equal("moka-toast-host moka-toast-host--top-right", host.ClassName);
		Assert.False(host.HasAttribute("style"));
	}

	// Only the offsets that were set are written, so a value an app sets in its own CSS for the others still applies.
	[Theory]
	[InlineData("top")]
	[InlineData("right")]
	[InlineData("bottom")]
	[InlineData("left")]
	public void AnOffset_IsWrittenOnlyWhenSet(string edge)
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>(p =>
		{
			switch (edge)
			{
				case "top":
					p.Add(x => x.OffsetTop, "48px");
					break;
				case "right":
					p.Add(x => x.OffsetRight, "48px");
					break;
				case "bottom":
					p.Add(x => x.OffsetBottom, "48px");
					break;
				default:
					p.Add(x => x.OffsetLeft, "48px");
					break;
			}
		});

		Assert.Equal($"--moka-toast-offset-{edge}: 48px", cut.Find(".moka-toast-host").GetAttribute("style"));
	}

	[Fact]
	public void AllOffsets_AreWrittenTogether()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>(p => p
			.Add(x => x.OffsetTop, "48px")
			.Add(x => x.OffsetRight, "1rem")
			.Add(x => x.OffsetBottom, "24px")
			.Add(x => x.OffsetLeft, "calc(56px + 1rem)"));

		Assert.Equal(
			"--moka-toast-offset-top: 48px; --moka-toast-offset-right: 1rem; --moka-toast-offset-bottom: 24px; " +
			"--moka-toast-offset-left: calc(56px + 1rem)",
			cut.Find(".moka-toast-host").GetAttribute("style"));
	}

	// An offset that could end its declaration and add others is dropped, as every other style value is.
	[Fact]
	public void AnOffsetThatWouldAddDeclarations_IsDropped()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>(p => p
			.Add(x => x.OffsetTop, "48px; inset: 0"));

		Assert.False(cut.Find(".moka-toast-host").HasAttribute("style"));
	}

	// Class and Style come last, so a class of the app's and a variable set in Style win over the component's own.
	[Fact]
	public void ClassAndStyle_PassThroughToTheHost()
	{
		IRenderedComponent<MokaToastHost> cut = Render<MokaToastHost>(p => p
			.Add(x => x.OffsetTop, "48px")
			.Add(x => x.Class, "app-toasts")
			.Add(x => x.Style, "--moka-toast-offset-top: 56px"));

		IElement host = cut.Find(".moka-toast-host");
		Assert.Equal("moka-toast-host moka-toast-host--top-right app-toasts", host.ClassName);
		Assert.Equal("--moka-toast-offset-top: 48px; --moka-toast-offset-top: 56px", host.GetAttribute("style"));
	}
}
