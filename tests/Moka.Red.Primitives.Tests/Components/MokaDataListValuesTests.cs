using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Primitives.DataList;
using Moka.Red.Primitives.Utility;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// MokaGate's 0.4.1 gaps 21, 22 and 24: a value can't hold several copyable values, an item takes no class, and a help
// line that has to show goes into the value, since the theme's descriptions setting hides Help.
public sealed class MokaDataListValuesTests : BunitContext
{
	private const string DragModule = "./_content/Moka.Red.Core/moka-drag.js";
	private const string Css = "MokaDataList.razor.css";

	private static readonly MokaDataListItem Addresses = new("Addresses")
	{
		Copyable = true,
		Mono = true,
		Values = [new MokaDataListValue("192.168.1.20", "wired"), new MokaDataListValue("fe80::1c2a", "link-local")]
	};

	public MokaDataListValuesTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<MokaDataList> List(MokaDataListCopyStyle copy, params MokaDataListItem[] items) =>
		Render<MokaDataList>(p => p.Add(x => x.Items, items).Add(x => x.CopyStyle, copy));

	private IRenderedComponent<MokaDataList> List(params MokaDataListItem[] items) => List(MokaDataListCopyStyle.Faint, items);

	[Fact]
	public void Values_ShowOnePerLine_WithTheirNotes()
	{
		IRenderedComponent<MokaDataList> cut = List(new MokaDataListItem("Addresses")
		{
			Values = [new MokaDataListValue("192.168.1.20", "wired"), new MokaDataListValue("fe80::1c2a")]
		});

		IElement value = cut.Find("dd");
		Assert.Contains("moka-data-list-description--values", value.ClassList);
		IReadOnlyList<IElement> lines = [.. value.Children];
		Assert.Equal(2, lines.Count);
		Assert.All(lines, line => Assert.Contains("moka-data-list-value", line.ClassList));
		Assert.Equal("192.168.1.20wired", lines[0].TextContent);
		Assert.Equal("wired", lines[0].QuerySelector(".moka-data-list-note")!.TextContent);
		Assert.Null(lines[1].QuerySelector(".moka-data-list-note"));
		Assert.Empty(cut.FindAll("button"));
		Assert.DoesNotContain("moka-quiet-copies", cut.Find("dl > div").ClassList);
	}

	[Fact]
	public void Values_WinOverValueAndValueContent()
	{
		IRenderedComponent<MokaDataList> cut = List(new MokaDataListItem("Name", "ignored", b => b.AddContent(0, "also ignored"))
		{
			Values = [new MokaDataListValue("one"), new MokaDataListValue("two")]
		});

		Assert.Equal(["one", "two"], cut.FindAll("dd > .moka-data-list-value").Select(line => line.TextContent));
	}

	[Fact]
	public void AnEmptyListOfValues_IsNoValue()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, [new MokaDataListItem("Addresses", "ignored", b => b.AddContent(0, "ignored")) { Values = [] }])
			.Add(x => x.EmptyText, "None yet"));

		IElement value = cut.Find("dd");
		Assert.Equal("None yet", value.TextContent);
		Assert.Contains("moka-data-list-description--empty", value.ClassList);
		Assert.DoesNotContain("moka-data-list-description--values", value.ClassList);
	}

	[Fact]
	public void Copyable_GivesEachValueItsOwnButton_NamedWithItsValue()
	{
		IRenderedComponent<MokaDataList> cut = List(Addresses);

		IReadOnlyList<IRenderedComponent<MokaCopyable>> copies = cut.FindComponents<MokaCopyable>();
		Assert.Equal(["192.168.1.20", "fe80::1c2a"], copies.Select(copy => copy.Instance.Text));
		Assert.Equal(["Copy Addresses, 192.168.1.20", "Copy Addresses, fe80::1c2a"],
			cut.FindAll("dd button").Select(button => button.GetAttribute("aria-label")));
		Assert.Contains("moka-quiet-copies", cut.Find("dl > div").ClassList);
		Assert.All(cut.FindAll("dd > .moka-data-list-value"),
			line => Assert.Contains(line.Children, child => child.ClassList.Contains("moka-copyable")));
		Assert.Equal("wired", cut.Find("dd > .moka-data-list-value > .moka-data-list-note").TextContent);
	}

	[Fact]
	public async Task EachButton_CopiesItsOwnValue_WithoutTheNote()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(DragModule);
		module.Setup<bool?>("copyToClipboard", "fe80::1c2a").SetResult(true);
		IRenderedComponent<MokaDataList> cut = List(Addresses);

		await cut.FindAll("dd button")[1].ClickAsync(new MouseEventArgs());

		JSRuntimeInvocation copy = Assert.Single(module.Invocations, i => i.Identifier == "copyToClipboard");
		Assert.Equal("fe80::1c2a", copy.Arguments[0]);
	}

	[Fact]
	public void AValuesCopyTextAndCopyLabel_WinForItsButton()
	{
		IRenderedComponent<MokaDataList> cut = List(new MokaDataListItem("Keys")
		{
			Copyable = true,
			CopyText = "the item's copy text, unused",
			Values =
			[
				new MokaDataListValue("ssh-ed25519 AAAA...") { CopyText = "ssh-ed25519 AAAAC3Nz full", CopyLabel = "Copy the laptop's key" },
				new MokaDataListValue("ssh-rsa BBBB...")
			]
		});

		Assert.Equal(["ssh-ed25519 AAAAC3Nz full", "ssh-rsa BBBB..."],
			cut.FindComponents<MokaCopyable>().Select(copy => copy.Instance.Text));
		Assert.Equal(["Copy the laptop's key", "Copy Keys, ssh-rsa BBBB..."],
			cut.FindAll("dd button").Select(button => button.GetAttribute("aria-label")));
	}

	[Fact]
	public void TheListsCopyLabel_NamesEveryValuesButton()
	{
		IRenderedComponent<MokaDataList> cut = Render<MokaDataList>(p => p
			.Add(x => x.Items, [Addresses])
			.Add(x => x.CopyLabel, item => $"{item.Label} kopieren"));

		Assert.Equal("Addresses kopieren, 192.168.1.20", cut.FindAll("dd button")[0].GetAttribute("aria-label"));
	}

	[Fact]
	public void AValueWithNothingToCopy_HasNoButton()
	{
		IRenderedComponent<MokaDataList> cut = List(new MokaDataListItem("Addresses")
		{
			Copyable = true,
			Values = [new MokaDataListValue("10.0.0.4"), new MokaDataListValue("")]
		});

		Assert.Single(cut.FindAll("dd button"));
	}

	[Fact]
	public void Always_KeepsAButtonAfterEachValue_AndNoRowFaint()
	{
		IRenderedComponent<MokaDataList> cut = List(MokaDataListCopyStyle.Always, Addresses);

		Assert.Equal(2, cut.FindAll("dd > .moka-data-list-value > .moka-copyable button").Count);
		Assert.Empty(cut.FindAll(".moka-quiet-copies"));
	}

	[Fact]
	public void ClickValue_MakesEachValueItsOwnCopyButton_WithTheNoteOutside()
	{
		IRenderedComponent<MokaDataList> cut = List(MokaDataListCopyStyle.ClickValue, Addresses);

		IReadOnlyList<IElement> buttons = cut.FindAll("dd > .moka-data-list-value > .moka-tooltip-trigger > button.moka-data-list-copy-value");
		Assert.Equal(["Copy Addresses, 192.168.1.20", "Copy Addresses, fe80::1c2a"], buttons.Select(b => b.GetAttribute("aria-label")));
		Assert.Equal("192.168.1.20", buttons[0].QuerySelector(".moka-data-list-copy-text")!.TextContent);
		Assert.Null(buttons[0].QuerySelector(".moka-data-list-note"));
		Assert.Equal(["Copy Addresses", "Copy Addresses"],
			cut.FindComponents<MokaCopyButton>().Select(button => button.Instance.TooltipText));
	}

	[Fact]
	public void Values_KeepTheEndContentBesideThem()
	{
		IRenderedComponent<MokaDataList> cut = List(Addresses with
		{
			EndContent = b => b.AddMarkupContent(0, "<button type=\"button\" class=\"edit\">Edit</button>")
		});

		IElement value = cut.Find("dd");
		Assert.Contains("moka-data-list-description--end", value.ClassList);
		Assert.Equal(["moka-data-list-value", "moka-data-list-value", "moka-data-list-end"],
			value.Children.Select(child => child.ClassName));
		Assert.NotNull(value.QuerySelector(".moka-data-list-end > button.edit"));
	}

	[Fact]
	public void Class_GoesOnTheItemsRow_AfterTheListsOwn()
	{
		IRenderedComponent<MokaDataList> cut = List(
			new MokaDataListItem("Fingerprint", "AB:CD") { Class = "print wide-value", Wide = true },
			new MokaDataListItem("Name", "Router"));

		IReadOnlyList<IElement> rows = cut.FindAll("dl > div");
		Assert.Equal("moka-data-list-entry moka-data-list-entry--wide print wide-value", rows[0].ClassName);
		Assert.Equal("moka-data-list-entry", rows[1].ClassName);
	}

	[Fact]
	public void AlwaysShowHelp_LeavesOutTheClassTheThemeHides()
	{
		IRenderedComponent<MokaDataList> cut = List(
			new MokaDataListItem("SHA-1", "AB:CD") { Help = "Windows calls it the thumbprint.", AlwaysShowHelp = true },
			new MokaDataListItem("Retention", "30 days") { Help = "How long logs are kept." });

		IReadOnlyList<IElement> help = cut.FindAll(".moka-data-list-help");
		Assert.Equal("Windows calls it the thumbprint.", help[0].TextContent);
		Assert.DoesNotContain("moka-about", help[0].ClassList);
		Assert.Contains("moka-about", help[1].ClassList);
	}

	[Fact]
	public void TheValues_StackInAColumn_AndEndContentGetsTheSecond()
	{
		IReadOnlyDictionary<string, string> values = ScopedCss.Declarations(Css, ".moka-data-list-description--values");
		Assert.Equal("grid", values["display"]);
		Assert.Equal("minmax(0, 1fr)", values["grid-template-columns"]);

		Assert.Equal("minmax(0, 1fr) auto",
			ScopedCss.Declarations(Css, ".moka-data-list-description--values.moka-data-list-description--end")["grid-template-columns"]);
		Assert.Equal("1", ScopedCss.Declarations(Css, ".moka-data-list-description--values > .moka-data-list-value")["grid-column"]);
		IReadOnlyDictionary<string, string> end = ScopedCss.Declarations(Css, ".moka-data-list-description--values > .moka-data-list-end");
		Assert.Equal("2", end["grid-column"]);
		Assert.Equal("1", end["grid-row"]);
	}

	// The values rules have to come after the end content's flex row, which they replace.
	[Fact]
	public void TheValuesGrid_ComesAfterTheEndContentsRow()
	{
		string css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", Css));

		Assert.True(css.IndexOf(".moka-data-list-description--values {", StringComparison.Ordinal) >
		            css.IndexOf(".moka-data-list-description--end {", StringComparison.Ordinal));
	}

	// moka-about is what made the help a line of its own, so without it the help ran on after the term.
	[Fact]
	public void HelpThatAlwaysShows_IsStillALineOfItsOwn()
	{
		Assert.Equal("block", ScopedCss.Declarations(Css, ".moka-data-list-help:not(.moka-about)")["display"]);
		Assert.False(ScopedCss.Declarations(Css, ".moka-data-list-help").ContainsKey("display"));
	}

	[Fact]
	public void ANote_KeepsTheTextFont_ExceptInTheTerminal()
	{
		IReadOnlyDictionary<string, string> note = ScopedCss.Declarations(Css, ".moka-data-list-note");
		Assert.Equal("var(--moka-font-family)", note["font-family"]);
		Assert.Equal("var(--moka-color-on-surface-tertiary)", note["color"]);
		Assert.Equal("inline-block", note["display"]);

		Assert.Equal("inherit",
			ScopedCss.Declarations(Css, ".moka-data-list--terminal ::deep > div > dd > .moka-data-list-value > .moka-data-list-note")["font-family"]);
	}
}
