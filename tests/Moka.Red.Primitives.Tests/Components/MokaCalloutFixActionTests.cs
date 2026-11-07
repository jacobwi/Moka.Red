using AngleSharp.Dom;
using Bunit;
using Moka.Red.Primitives.Callout;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaCalloutFixActionTests : BunitContext
{
	private const string Stylesheet = "MokaCallout.razor.css";

	[Fact]
	public void Default_HasNoFixAction()
	{
		IRenderedComponent<MokaCallout> cut = Render<MokaCallout>(p => p.AddChildContent("Message"));

		Assert.Equal("moka-callout moka-callout--note", cut.Find(".moka-callout").ClassName);
		Assert.Empty(cut.FindAll(".moka-callout__fix"));
	}

	[Fact]
	public void FixAction_RendersAfterTheTitle()
	{
		IRenderedComponent<MokaCallout> cut = Render<MokaCallout>(p => p
			.Add(x => x.Title, "Printer stopped answering")
			.Add(x => x.FixAction, "<button>Move it back</button>"));

		IElement header = cut.Find(".moka-callout__header");

		Assert.Contains("moka-callout--has-fix", cut.Find(".moka-callout").ClassList);
		Assert.Equal("moka-callout__fix", header.Children[^1].ClassName);
		Assert.Equal("moka-callout__title", header.Children[^2].ClassName);
		Assert.Equal("Move it back", cut.Find(".moka-callout__fix button").TextContent);
	}

	[Fact]
	public void FixAction_RendersBeforeTheCloseButton()
	{
		IRenderedComponent<MokaCallout> cut = Render<MokaCallout>(p => p
			.Add(x => x.Closable, true)
			.Add(x => x.FixAction, "<button>Move it back</button>"));

		IElement header = cut.Find(".moka-callout__header");

		Assert.Equal("moka-callout__fix", header.Children[^2].ClassName);
		Assert.Single(header.Children[^1].QuerySelectorAll(".moka-callout__close"));
	}

	[Fact]
	public void HasFixRule_WrapsTheActionUnderTheTitle()
	{
		Assert.Equal("wrap",
			ScopedCss.Declarations(Stylesheet, ".moka-callout--has-fix > .moka-callout__header")["flex-wrap"]);
		Assert.Equal("1 1 15rem",
			ScopedCss.Declarations(Stylesheet,
				".moka-callout--has-fix > .moka-callout__header > .moka-callout__title")["flex"]);
	}

	[Fact]
	public void Class_StaysLast()
	{
		IElement root = Render<MokaCallout>(p => p
			.Add(x => x.FixAction, "<button>Fix</button>")
			.Add(x => x.Class, "mine")).Find(".moka-callout");

		Assert.EndsWith("moka-callout--has-fix mine", root.ClassName, StringComparison.Ordinal);
	}
}
