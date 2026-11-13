using AngleSharp.Dom;
using Bunit;
using Moka.Red.Forms.SearchInput;

namespace Moka.Red.Forms.Tests.Components;

// A search box in a toolbar needs a name but no visible label. An app passed aria-label through the extra attributes;
// HideLabel keeps the real label, tied to the input, and only hides it.
public class MokaSearchInputLabelTests : BunitContext
{
	[Fact]
	public void ByDefault_TheLabelShows()
	{
		IRenderedComponent<MokaSearchInput> cut = Render<MokaSearchInput>(p => p.Add(x => x.Label, "Search files"));

		Assert.Equal("moka-search-label", cut.Find("label").ClassName);
	}

	[Fact]
	public void HideLabel_KeepsTheLabelAsTheInputsName_OutOfSight()
	{
		IRenderedComponent<MokaSearchInput> cut = Render<MokaSearchInput>(p => p
			.Add(x => x.Label, "Search files")
			.Add(x => x.HideLabel, true));

		IElement label = cut.Find("label");
		Assert.Contains("moka-visually-hidden", label.ClassList);
		Assert.Equal("Search files", label.TextContent);
		Assert.Equal(cut.Find("input").Id, label.GetAttribute("for"));
		Assert.Null(cut.Find("input").GetAttribute("aria-label"));
	}
}
