using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Enums;
using Moka.Red.Icons;
using Moka.Red.Primitives.DataList;
using Moka.Red.Primitives.Utility;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaDataListLookTests : BunitContext
{

	private static readonly MokaDataListItem[] Device =
	[
		new("Name", "Living room TV") { Icon = MokaIcons.Content.Tv },
		new("Address", "192.168.1.24") { Icon = MokaIcons.Content.Globe, Mono = true, Copyable = true },
		new("Owner") { Help = "Who looks after it." }
	];

	[Fact]
	public void TheNewOptions_DefaultToTodaysList()
	{
		MokaDataList list = Render<MokaDataList>(p => p.Add(x => x.Items, Device)).Instance;

		Assert.Equal(MokaDataListLook.Standard, list.Look);
		Assert.Equal(MokaDataListTermStyle.Standard, list.TermStyle);
		Assert.Equal(MokaDataListCopyStyle.Faint, list.CopyStyle);
		Assert.False(new MokaDataListItem("Name").Wide);
	}

	[Fact]
	public void TheDefaults_AddNoClass()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p.Add(x => x.Items, Device));

		Assert.Equal("moka-data-list moka-data-list--md", cut.Find("dl").ClassName);
		Assert.Equal(["moka-data-list-entry", "moka-data-list-entry moka-quiet-copies", "moka-data-list-entry"],
			cut.FindAll("dl > div").Select(row => row.ClassName));
	}

	[Theory]
	[InlineData(MokaDataListLook.SettingsRows, "moka-data-list--settings")]
	[InlineData(MokaDataListLook.InsetPanel, "moka-data-list--inset")]
	[InlineData(MokaDataListLook.FactTiles, "moka-data-list--tiles")]
	[InlineData(MokaDataListLook.SpecSheet, "moka-data-list--spec")]
	[InlineData(MokaDataListLook.Terminal, "moka-data-list--terminal")]
	public void ALook_IsAClassOnTheList_AndKeepsTheDefinitionList(MokaDataListLook look, string expected)
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p.Add(x => x.Items, Device).Add(x => x.Look, look));

		IElement list = cut.Find("dl");
		Assert.Contains(expected, list.ClassList);
		Assert.Equal(3, cut.FindAll("dl > div > dt + dd").Count);
	}

	[Theory]
	[InlineData(MokaDataListLook.SettingsRows)]
	[InlineData(MokaDataListLook.InsetPanel)]
	[InlineData(MokaDataListLook.FactTiles)]
	[InlineData(MokaDataListLook.SpecSheet)]
	[InlineData(MokaDataListLook.Terminal)]
	public void OnlyStandard_ReadsTheOrientation(MokaDataListLook look)
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, Device)
			.Add(x => x.Look, look)
			.Add(x => x.Orientation, MokaDirection.Row));

		Assert.DoesNotContain("moka-data-list--row", cut.Find("dl").ClassList);
	}

	[Theory]
	[InlineData(MokaDataListLook.Standard, false)]
	[InlineData(MokaDataListLook.SettingsRows, true)]
	[InlineData(MokaDataListLook.InsetPanel, true)]
	[InlineData(MokaDataListLook.FactTiles, false)]
	[InlineData(MokaDataListLook.SpecSheet, false)]
	[InlineData(MokaDataListLook.Terminal, false)]
	public void SettingsRowsAndTheInsetPanel_DrawLinesBetweenRows(MokaDataListLook look, bool lines)
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p.Add(x => x.Items, Device).Add(x => x.Look, look));

		Assert.Equal(lines, cut.Find("dl").ClassList.Contains("moka-data-list--bordered"));
	}

	[Fact]
	public void Bordered_StillDrawsLinesUnderTheOtherLooks()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, Device)
			.Add(x => x.Look, MokaDataListLook.SpecSheet)
			.Add(x => x.Bordered, true));

		Assert.Contains("moka-data-list--bordered", cut.Find("dl").ClassList);
	}

	[Theory]
	[InlineData(MokaDataListTermStyle.Quiet, "moka-data-list--term-quiet")]
	[InlineData(MokaDataListTermStyle.Micro, "moka-data-list--term-micro")]
	[InlineData(MokaDataListTermStyle.Strong, "moka-data-list--term-strong")]
	public void ATermStyle_IsAClassOnTheList(MokaDataListTermStyle term, string expected)
	{
		foreach (MokaDataListLook look in new[]
		         {
			         MokaDataListLook.Standard, MokaDataListLook.SettingsRows, MokaDataListLook.InsetPanel, MokaDataListLook.SpecSheet
		         })
		{
			IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
				.Add(x => x.Items, Device)
				.Add(x => x.Look, look)
				.Add(x => x.TermStyle, term));

			Assert.Contains(expected, cut.Find("dl").ClassList);
		}
	}

	[Theory]
	[InlineData(MokaDataListLook.FactTiles)]
	[InlineData(MokaDataListLook.Terminal)]
	public void TilesAndTheTerminal_KeepTheirOwnTerm(MokaDataListLook look)
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, Device)
			.Add(x => x.Look, look)
			.Add(x => x.TermStyle, MokaDataListTermStyle.Strong));

		Assert.DoesNotContain(cut.Find("dl").ClassList, name => name.StartsWith("moka-data-list--term-", StringComparison.Ordinal));
	}

	[Fact]
	public void TheTerminal_ReadsNotSet_ForAnEmptyValue()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p.Add(x => x.Items, Device).Add(x => x.Look, MokaDataListLook.Terminal));

		IElement owner = cut.FindAll("dd")[2];
		Assert.Equal(MokaDataList.DefaultTerminalEmptyText, owner.TextContent);
		Assert.Equal("not set", MokaDataList.DefaultTerminalEmptyText);
		Assert.Contains("moka-data-list-description--empty", owner.ClassList);
	}

	[Fact]
	public void EmptyText_WinsInTheTerminal()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, Device)
			.Add(x => x.Look, MokaDataListLook.Terminal)
			.Add(x => x.EmptyText, "nichts"));

		Assert.Equal("nichts", cut.FindAll("dd")[2].TextContent);
	}

	[Fact]
	public void OtherLooks_LeaveAnEmptyValueEmpty_WithoutEmptyText()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p.Add(x => x.Items, Device).Add(x => x.Look, MokaDataListLook.SpecSheet));

		IElement owner = cut.FindAll("dd")[2];
		Assert.Equal("", owner.TextContent);
		Assert.DoesNotContain("moka-data-list-description--empty", owner.ClassList);
	}

	[Fact]
	public void TheTerminal_LeavesOutIconsAndHelp()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p.Add(x => x.Items, Device).Add(x => x.Look, MokaDataListLook.Terminal));

		Assert.Empty(cut.FindAll("dt svg"));
		Assert.Empty(cut.FindAll(".moka-data-list-help"));
		Assert.All(cut.FindAll("dt"), term => Assert.Equal("moka-data-list-term", term.ClassName));
		Assert.Equal(["Name", "Address", "Owner"], cut.FindAll("dt").Select(term => term.TextContent));
	}

	[Fact]
	public void TheOtherLooks_KeepIconsAndHelp()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p.Add(x => x.Items, Device).Add(x => x.Look, MokaDataListLook.FactTiles));

		Assert.Equal(2, cut.FindAll("dt svg.moka-data-list-icon").Count);
		Assert.Equal("Who looks after it.", cut.Find(".moka-data-list-help").TextContent);
	}

	[Fact]
	public void AWideItem_CarriesTheWideClass()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p.Add(x => x.Items,
			[new MokaDataListItem("Name", "TV"), new MokaDataListItem("SSH key", "ssh-ed25519 AAAA") { Wide = true }]));

		Assert.Equal(["moka-data-list-entry", "moka-data-list-entry moka-data-list-entry--wide"],
			cut.FindAll("dl > div").Select(row => row.ClassName));
	}

	[Fact]
	public void Tiles_TakeTheMinimumColumn_WithoutColumns()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, Device)
			.Add(x => x.Look, MokaDataListLook.FactTiles)
			.Add(x => x.MinColumn, "9rem"));

		Assert.Equal("--_dl-min-column: 9rem", cut.Find("dl").GetAttribute("style"));
	}

	[Theory]
	[InlineData(MokaDataListCopyStyle.Always, "moka-data-list--copy-always")]
	[InlineData(MokaDataListCopyStyle.ClickValue, "moka-data-list--copy-value")]
	public void ACopyStyle_IsAClassOnTheList_AndNoRowStaysFaint(MokaDataListCopyStyle copy, string expected)
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p.Add(x => x.Items, Device).Add(x => x.CopyStyle, copy));

		Assert.Contains(expected, cut.Find("dl").ClassList);
		Assert.Empty(cut.FindAll(".moka-quiet-copies"));
	}

	[Fact]
	public void Always_KeepsTheQuietCopyButtonAfterTheValue()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p.Add(x => x.Items, Device).Add(x => x.CopyStyle, MokaDataListCopyStyle.Always));

		IElement button = cut.Find("dd > .moka-copyable button");
		Assert.Equal("Copy Address", button.GetAttribute("aria-label"));
		Assert.Contains("moka-copy-btn--quiet", button.ClassList);
	}

	[Fact]
	public void ClickValue_MakesTheValueItsOwnCopyButton()
	{
		IRenderedComponent<MokaDataList> cut = ClickValueList(Device);

		IElement value = cut.FindAll("dd")[1];
		Assert.Empty(value.QuerySelectorAll(".moka-copyable"));
		IElement button = value.QuerySelector("button.moka-data-list-copy-value")!;
		Assert.Equal("button", button.GetAttribute("type"));
		Assert.Equal("Copy Address, 192.168.1.24", button.GetAttribute("aria-label"));
		Assert.Equal("192.168.1.24", button.QuerySelector(".moka-data-list-copy-text")!.TextContent);
		Assert.NotNull(button.QuerySelector(".moka-data-list-copy-icon--idle > svg[aria-hidden='true']"));
		Assert.NotNull(button.QuerySelector(".moka-data-list-copy-icon--done > svg[aria-hidden='true']"));
		Assert.False(button.HasAttribute("disabled"));
		Assert.Equal("status", value.QuerySelector(".moka-visually-hidden")!.GetAttribute("role"));
	}

	[Fact]
	public void ClickValue_LeavesValuesThatCantBeCopiedAsText()
	{
		IRenderedComponent<MokaDataList> cut = ClickValueList(Device);

		Assert.Equal("Living room TV", cut.FindAll("dd")[0].TextContent);
		Assert.Single(cut.FindAll("button"));
	}

	[Fact]
	public async Task ClickValue_CopiesOnClick_AndSaysSo()
	{
		IRenderedComponent<MokaDataList> cut = ClickValueList(
			new MokaDataListItem("Key", "ssh-ed25519 AAAA...") { Copyable = true, CopyText = "ssh-ed25519 AAAAC3Nz full" });

		Assert.Equal("ssh-ed25519 AAAAC3Nz full", cut.Find("button.moka-data-list-copy-value").CopyText());
		await cut.Find("button.moka-data-list-copy-value").CopiedAsync();

		IElement button = cut.Find("button.moka-data-list-copy-value");
		Assert.Contains("moka-copy-btn--copied", button.ClassList);
		Assert.Equal("Copy Key, ssh-ed25519 AAAA...", button.GetAttribute("aria-label"));
		Assert.Equal("Copied", cut.Find("dd [role='status']").TextContent);
	}

	[Fact]
	public async Task ClickValue_SaysWhenTheBrowserRefused()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, Device)
			.Add(x => x.CopyStyle, MokaDataListCopyStyle.ClickValue)
			.Add(x => x.CopyFailedText, "Nicht kopiert"));

		await cut.Find("button.moka-data-list-copy-value").CopiedAsync(false);

		Assert.Contains("moka-copy-btn--failed", cut.Find("button.moka-data-list-copy-value").ClassList);
		Assert.Equal("Nicht kopiert", cut.Find("dd [role='status']").TextContent);
	}

	[Fact]
	public void ClickValue_TakesTheCopyWords()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, Device)
			.Add(x => x.CopyStyle, MokaDataListCopyStyle.ClickValue)
			.Add(x => x.CopyLabel, item => $"{item.Label} kopieren"));

		Assert.Equal("Address kopieren, 192.168.1.24", cut.Find("button.moka-data-list-copy-value").GetAttribute("aria-label"));
		Assert.Equal("Address kopieren", cut.FindComponent<MokaCopyButton>().Instance.TooltipText);
	}

	[Fact]
	public void ClickValue_KeepsRichContentInsideTheButton()
	{
		IRenderedComponent<MokaDataList> cut = ClickValueList(
			new MokaDataListItem("Status", "online", b => b.AddMarkupContent(0, "<b>Online</b>")) { Copyable = true });

		IElement button = cut.Find("button.moka-data-list-copy-value");
		Assert.Equal("Online", button.QuerySelector(".moka-data-list-copy-text b")!.TextContent);
		Assert.Equal("Copy Status, online", button.GetAttribute("aria-label"));
	}

	[Fact]
	public void ClickValue_SitsBeforeEndContent()
	{
		IRenderedComponent<MokaDataList> cut = ClickValueList(new MokaDataListItem("Key", "abc")
		{
			Copyable = true,
			EndContent = b => b.AddMarkupContent(0, "<button type=\"button\">Reveal</button>")
		});

		Assert.NotNull(cut.Find("dd > .moka-data-list-value button.moka-data-list-copy-value"));
		Assert.Equal("Reveal", cut.Find("dd > .moka-data-list-end > button").TextContent);
	}

	[Fact]
	public void ADisabledList_DisablesItsValueButtons()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, Device)
			.Add(x => x.CopyStyle, MokaDataListCopyStyle.ClickValue)
			.Add(x => x.Disabled, true));

		Assert.True(cut.Find("button.moka-data-list-copy-value").HasAttribute("disabled"));
	}

	private IRenderedComponent<MokaDataList> ClickValueList(params MokaDataListItem[] items) =>
		Render<MokaDataList>(p => p.Add(x => x.Items, items).Add(x => x.CopyStyle, MokaDataListCopyStyle.ClickValue));
}
