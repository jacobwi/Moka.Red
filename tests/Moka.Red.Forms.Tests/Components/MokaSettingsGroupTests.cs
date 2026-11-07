using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Forms.SettingsForm;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

public class MokaSettingsGroupTests : BunitContext
{
	private const string Stylesheet = "MokaSettingsGroup.razor.css";

	public MokaSettingsGroupTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<MokaSettingsGroup> Group(Action<ComponentParameterCollectionBuilder<MokaSettingsGroup>>? more = null) =>
		Render<MokaSettingsGroup>(p =>
		{
			p.Add(x => x.Id, "time-zone").Add(x => x.Title, "Time zone");
			more?.Invoke(p);
		});

	[Fact]
	public void ItsId_IsWhatALinkLandsOn() =>
		Assert.Equal("time-zone", Group().Find(".moka-settings-group").Id);

	[Fact]
	public void TheHeading_LinksToItself_OnThisPage()
	{
		Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/settings/general?tab=1#elsewhere");

		IElement link = Group().Find("h3 a");

		Assert.Equal("Time zone", link.TextContent);
		Assert.Equal("settings/general?tab=1#time-zone", link.GetAttribute("href"));
	}

	[Fact]
	public void WithoutAnId_TheHeadingIsPlain()
	{
		IRenderedComponent<MokaSettingsGroup> cut = Render<MokaSettingsGroup>(p => p.Add(x => x.Title, "Time zone"));

		Assert.Empty(cut.FindAll("h3 a"));
		Assert.Contains("Time zone", cut.Find("h3").TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public void TheHash_ShowsOnHover()
	{
		Assert.Equal("0", ScopedCss.Declarations(Stylesheet, ".moka-settings-group__anchor::after")["opacity"]);
		Assert.Equal("1", ScopedCss.Declarations(Stylesheet, ".moka-settings-group__anchor:hover::after")["opacity"]);
	}

	[Fact]
	public void TwoColumns_AreTheDefault_WithNothingWrittenInline()
	{
		Assert.False(Group().Find(".moka-settings-group__fields").HasAttribute("style"));
		Assert.Equal(
			"repeat(auto-fill, minmax(min(100%, max(16rem, (100% - (var(--_settings-columns, 2) - 1) * var(--_settings-gap)) / var(--_settings-columns, 2))), 1fr))",
			ScopedCss.Declarations(Stylesheet, ".moka-settings-group__fields")["grid-template-columns"]);
	}

	[Fact]
	public void Columns_ChangesTheGrid()
	{
		IElement fields = Group(p => p.Add(x => x.Columns, 3)).Find(".moka-settings-group__fields");

		Assert.Equal("--_settings-columns: 3", fields.GetAttribute("style"));
	}

	// The window's width said nothing inside an app shell: at 794 px MokaArk's two-column group put each value in a
	// 120 px cell. A column is now never narrower than 16rem of the group's own width, so the group drops columns.
	[Fact]
	public void TheGroupsOwnWidth_DecidesTheColumns_NotTheWindows()
	{
		Assert.Single(ScopedCss.Values(Stylesheet, ".moka-settings-group__fields", "grid-template-columns"));
		Assert.DoesNotContain("@media", System.IO.File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "Css", Stylesheet)),
			StringComparison.Ordinal);
	}

	[Fact]
	public void TheFields_GoInTheGrid()
	{
		IElement fields = Group(p => p.Add(x => x.ChildContent, b => b.AddMarkupContent(0, "<input id=\"name\"/>")))
			.Find(".moka-settings-group__fields");

		Assert.NotNull(fields.QuerySelector("#name"));
	}

	[Fact]
	public void Help_IsHiddenWithDescriptions()
	{
		IElement help = Group(p => p.Add(x => x.Help, "What other devices see.")).Find(".moka-settings-group__help");

		Assert.Equal("What other devices see.", help.TextContent);
		Assert.Contains("moka-about", help.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public void WithoutHelp_ThereIsNoLine() =>
		Assert.Empty(Group().FindAll(".moka-settings-group__help"));

	[Fact]
	public void Tag_IsTheNewLook()
	{
		IElement tag = Group(p => p.Add(x => x.Tag, "New")).Find(".moka-tag--new");

		Assert.Equal("New", tag.TextContent.Trim());
	}

	[Fact]
	public void Advanced_SaysSoBesideTheTitle() =>
		Assert.Equal("Advanced", Group(p => p.Add(x => x.Advanced, true)).Find("h3 .moka-tag").TextContent.Trim());

	[Fact]
	public void OutsideAForm_AnAdvancedGroupShows() =>
		Assert.Single(Group(p => p.Add(x => x.Advanced, true)).FindAll(".moka-settings-group"));

	[Fact]
	public void TheGroup_TakesClassStyleAndAttributes()
	{
		IElement group = Group(p => p
			.Add(x => x.Class, "mine")
			.Add(x => x.Style, "max-width: 30rem")
			.AddUnmatched("data-area", "main")).Find(".moka-settings-group");

		Assert.EndsWith("mine", group.ClassName, StringComparison.Ordinal);
		Assert.Equal("max-width: 30rem", group.GetAttribute("style"));
		Assert.Equal("main", group.GetAttribute("data-area"));
	}
}
