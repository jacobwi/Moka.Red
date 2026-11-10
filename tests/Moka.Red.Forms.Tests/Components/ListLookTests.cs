using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Theming;
using Moka.Red.Forms.AutoComplete;
using Moka.Red.Forms.SelectField;
using Moka.Red.Forms.TreeSelect;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// MokaListLook draws the rows of the lists that open from MokaSelect, MokaAutoComplete and MokaTreeSelect. A look is a
// class on the root, plus a mark at the start of each row or key hints under the list for the looks that need them.
// Only the new design draws looks; the rules themselves are CSS, checked as text at the end.
public class ListLookTests : BunitContext
{
	private const string SideModule = "./_content/Moka.Red.Forms/moka-list-side.js";

	private static readonly string[] Teams = ["Design", "Engineering", "Finance"];
	private static readonly string[] Cities = ["Berlin", "Bern", "Bergen", "Oslo"];
	private static readonly MokaTreeSelectItem<string>[] Tree = [new("a", "Alpha"), new("b", "Beta")];

	private readonly BunitJSModuleInterop _popup;
	private readonly BunitJSModuleInterop _side;

	public ListLookTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_popup = TopLayerPopup.Setup(JSInterop);
		_side = JSInterop.SetupModule(SideModule);
		_side.SetupVoid("watch", _ => true).SetVoidResult();
		_side.SetupVoid("unwatch", _ => true).SetVoidResult();
	}

	public static TheoryData<MokaListLook, string> Looks => new()
	{
		{ MokaListLook.Check, "check" },
		{ MokaListLook.Solid, "solid" },
		{ MokaListLook.Attached, "attached" },
		{ MokaListLook.Radio, "radio" },
		{ MokaListLook.Compact, "compact" },
		{ MokaListLook.Terminal, "terminal" }
	};

	[Theory]
	[MemberData(nameof(Looks))]
	public void Select_TakesTheThemesLook_WhenItHasNoneOfItsOwn(MokaListLook look, string name)
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = Select(MokaTheme.Red.WithLists(look));

		Assert.Contains($"moka-select--list-{name}", cut.Find(".moka-select").ClassList);
	}

	[Theory]
	[MemberData(nameof(Looks))]
	public void AutoComplete_TakesTheThemesLook_WhenItHasNoneOfItsOwn(MokaListLook look, string name)
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = AutoComplete(MokaTheme.Red.WithLists(look));

		Assert.Contains($"moka-autocomplete--list-{name}", cut.Find(".moka-autocomplete").ClassList);
	}

	[Theory]
	[MemberData(nameof(Looks))]
	public void TreeSelect_TakesTheThemesLook_WhenItHasNoneOfItsOwn(MokaListLook look, string name)
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = TreeSelect(MokaTheme.Red.WithLists(look));

		Assert.Contains($"moka-tree-select--list-{name}", cut.Find(".moka-tree-select").ClassList);
	}

	[Fact]
	public void ALooksOwnParameter_BeatsTheThemes()
	{
		MokaTheme theme = MokaTheme.Gate.WithLists(MokaListLook.Terminal);

		IElement select = Select(theme, p => p.Add(x => x.Look, MokaListLook.Check)).Find(".moka-select");
		IElement auto = AutoComplete(theme, p => p.Add(x => x.Look, MokaListLook.Solid)).Find(".moka-autocomplete");
		IElement tree = TreeSelect(theme, p => p.Add(x => x.Look, MokaListLook.Radio)).Find(".moka-tree-select");

		Assert.Contains("moka-select--list-check", select.ClassList);
		Assert.DoesNotContain("moka-select--list-terminal", select.ClassList);
		Assert.Contains("moka-autocomplete--list-solid", auto.ClassList);
		Assert.DoesNotContain("moka-autocomplete--list-terminal", auto.ClassList);
		Assert.Contains("moka-tree-select--list-radio", tree.ClassList);
		Assert.DoesNotContain("moka-tree-select--list-terminal", tree.ClassList);
	}

	[Fact]
	public void AStandardParameter_BeatsALookFromTheTheme()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut =
			Select(MokaTheme.Red.WithLists(MokaListLook.Radio), p => p.Add(x => x.Look, MokaListLook.Standard));

		Assert.DoesNotContain(cut.Find(".moka-select").ClassList, c => c.Contains("--list-", StringComparison.Ordinal));
		Assert.Empty(cut.FindAll(".moka-select-mark"));
	}

	// Standard is what the lists were before looks existed, so it has to render exactly that.
	[Fact]
	public void Standard_RendersTheSameMarkupAsNoLookAtAll()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> select = Select(MokaTheme.Red, p => p.Add(x => x.Value, "Engineering"));
		IRenderedComponent<MokaSelect<string>> plainSelect =
			Render<MokaSelect<string>>(p => SelectParameters(p).Add(x => x.IsOpen, true).Add(x => x.Value, "Engineering"));

		IRenderedComponent<CascadingValue<MokaTheme>> tree = TreeSelect(MokaTheme.Red, p => p.Add(x => x.Value, "a"));
		IRenderedComponent<MokaTreeSelect<string>> plainTree = Render<MokaTreeSelect<string>>(p => TreeParameters(p).Add(x => x.Value, "a"));
		tree.Find("button.moka-tree-select-trigger").Click();
		plainTree.Find("button.moka-tree-select-trigger").Click();

		select.MarkupMatches(plainSelect.Markup);
		tree.MarkupMatches(plainTree.Markup);
	}

	[Fact]
	public async Task Standard_AutoCompleteRendersTheSameMarkupAsNoLookAtAll()
	{
		// Without the clear button, whose tooltip names a new anchor every time.
		IRenderedComponent<CascadingValue<MokaTheme>> themed =
			AutoComplete(MokaTheme.Red, p => p.Add(x => x.Value, "Bern").Add(x => x.Clearable, false));
		IRenderedComponent<MokaAutoComplete<string>> plain = Render<MokaAutoComplete<string>>(p =>
			AutoCompleteParameters(p).Add(x => x.Value, "Bern").Add(x => x.Clearable, false));

		await SearchAsync(themed, "Ber");
		await SearchAsync(plain, "Ber");

		themed.MarkupMatches(plain.Markup);
		Assert.Empty(themed.FindAll(".moka-autocomplete-option-content"));
	}

	[Theory]
	[InlineData(MokaListLook.Radio)]
	[InlineData(MokaListLook.Compact)]
	[InlineData(MokaListLook.Terminal)]
	public async Task TheOriginalDesign_GetsNothing_FromTheThemeOrTheParameter(MokaListLook look)
	{
		foreach (MokaTheme theme in new[] { MokaTheme.Dark, MokaTheme.Light })
		{
			IRenderedComponent<CascadingValue<MokaTheme>> fromTheme = Select(theme.WithLists(look));
			IRenderedComponent<CascadingValue<MokaTheme>> fromParameter = Select(theme, p => p.Add(x => x.Look, look));
			IRenderedComponent<CascadingValue<MokaTheme>> auto = AutoComplete(theme, p => p.Add(x => x.Look, look));
			IRenderedComponent<CascadingValue<MokaTheme>> tree = TreeSelect(theme, p => p.Add(x => x.Look, look));
			await SearchAsync(auto, "Ber");
			await tree.Find("button.moka-tree-select-trigger").ClickAsync(new MouseEventArgs());

			foreach (IRenderedComponent<CascadingValue<MokaTheme>> cut in new[] { fromTheme, fromParameter, auto, tree })
			{
				Assert.DoesNotContain("--list-", cut.Markup, StringComparison.Ordinal);
				Assert.Empty(cut.FindAll("[class*='-mark']"));
				Assert.Empty(cut.FindAll("[class*='-hints']"));
			}
		}
	}

	[Fact]
	public void Radio_StartsEachRowWithAHiddenCircle_AndOnlyTheChosenOneIsOn()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut =
			Select(MokaTheme.Red, p => p.Add(x => x.Look, MokaListLook.Radio).Add(x => x.Value, "Engineering"));

		IReadOnlyList<IElement> options = cut.FindAll(".moka-select-option");
		Assert.All(options, option =>
		{
			IElement mark = Assert.IsAssignableFrom<IElement>(option.FirstElementChild);
			Assert.Contains("moka-select-mark", mark.ClassList);
			Assert.Equal("true", mark.GetAttribute("aria-hidden"));
			Assert.DoesNotContain("moka-select-mark--box", mark.ClassList);
		});
		Assert.Equal([false, true, false], options.Select(o => o.QuerySelector(".moka-select-mark--on") is not null));
		Assert.Empty(cut.FindAll(".moka-select-check"));
		Assert.Equal("true", options[1].GetAttribute("aria-selected"));
	}

	[Fact]
	public void Radio_InAMultipleSelect_DrawsCheckBoxes_WithACheckInTheChosenOnes()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = Select(MokaTheme.Red, p => p
			.Add(x => x.Look, MokaListLook.Radio)
			.Add(x => x.Multiple, true)
			.Add(x => x.SelectAll, true)
			.Add(x => x.SelectedValues, new List<string> { "Design", "Finance" }));

		IReadOnlyList<IElement> marks = cut.FindAll(".moka-select-option:not(.moka-select-option--select-all) .moka-select-mark");
		Assert.All(marks, mark => Assert.Contains("moka-select-mark--box", mark.ClassList));
		Assert.Equal([true, false, true], marks.Select(mark => mark.QuerySelector("svg") is not null));

		IElement selectAll = cut.Find(".moka-select-option--select-all");
		Assert.NotNull(selectAll.QuerySelector(".moka-select-mark"));
		Assert.Null(selectAll.QuerySelector("input"));
	}

	[Fact]
	public void Terminal_MarksEveryRow_AndDrawsNoCheck()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut =
			Select(MokaTheme.Red.WithLists(MokaListLook.Terminal), p => p.Add(x => x.Value, "Finance"));

		IReadOnlyList<IElement> marks = cut.FindAll(".moka-select-mark");
		Assert.Equal(3, marks.Count);
		Assert.All(marks, mark => Assert.Equal("true", mark.GetAttribute("aria-hidden")));
		Assert.All(marks, mark => Assert.Empty(mark.Children));
		Assert.Equal([false, false, true], marks.Select(mark => mark.ClassList.Contains("moka-select-mark--on")));
		Assert.Empty(cut.FindAll(".moka-select-check"));
	}

	[Theory]
	[InlineData(MokaListLook.Check)]
	[InlineData(MokaListLook.Solid)]
	[InlineData(MokaListLook.Attached)]
	[InlineData(MokaListLook.Compact)]
	public void TheLooksWithoutAMark_EndTheChosenRowWithACheck(MokaListLook look)
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut =
			Select(MokaTheme.Red, p => p.Add(x => x.Look, look).Add(x => x.Value, "Design"));

		Assert.Empty(cut.FindAll(".moka-select-mark"));
		Assert.NotNull(cut.FindAll(".moka-select-option")[0].QuerySelector(".moka-select-check svg"));
		Assert.Null(cut.FindAll(".moka-select-option")[1].QuerySelector(".moka-select-check svg"));
	}

	[Fact]
	public void Compact_DrawsKeyHintsOutsideTheListbox_HiddenFromScreenReaders()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = Select(MokaTheme.Red.WithLists(MokaListLook.Compact));

		IElement hints = cut.Find(".moka-select-hints");
		Assert.Equal("true", hints.GetAttribute("aria-hidden"));
		Assert.Null(hints.Closest("[role=listbox]"));
		Assert.Equal(["↑", "↓", "Enter", "Esc"], hints.QuerySelectorAll("kbd").Select(k => k.TextContent));
		Assert.Equal(["↑↓move", "Enterpick", "Escclose"], cut.FindAll(".moka-select-hint").Select(h => h.TextContent));
	}

	[Fact]
	public void Compact_TakesItsHintWordsFromParameters()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = Select(MokaTheme.Red, p => p
			.Add(x => x.Look, MokaListLook.Compact)
			.Add(x => x.MoveHintText, "bewegen")
			.Add(x => x.PickHintText, "wählen")
			.Add(x => x.CloseHintText, "schließen"));

		Assert.Equal(["↑↓bewegen", "Enterwählen", "Escschließen"], cut.FindAll(".moka-select-hint").Select(h => h.TextContent));
	}

	[Fact]
	public void Compact_HasNoHints_WhileThereIsNothingToMoveThrough()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut =
			Select(MokaTheme.Red.WithLists(MokaListLook.Compact), p => p.Add(x => x.Loading, true));

		Assert.Empty(cut.FindAll(".moka-select-hints"));
	}

	[Theory]
	[InlineData(MokaListLook.Check)]
	[InlineData(MokaListLook.Solid)]
	[InlineData(MokaListLook.Attached)]
	[InlineData(MokaListLook.Radio)]
	[InlineData(MokaListLook.Compact)]
	[InlineData(MokaListLook.Terminal)]
	public void EveryLook_KeepsTheKeyboardAndTheActiveOption(MokaListLook look)
	{
		List<string> picked = [];
		IRenderedComponent<CascadingValue<MokaTheme>> cut = Select(MokaTheme.Red.WithLists(look), p => p
			.Add(x => x.ValueChanged, (string value) => picked.Add(value)));

		cut.Find(".moka-select-trigger").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
		cut.Find(".moka-select-trigger").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

		Assert.Equal("team-listbox-option-1", cut.Find(".moka-select-trigger").GetAttribute("aria-activedescendant"));
		Assert.Contains("moka-select-option--focused", cut.FindAll(".moka-select-option")[1].ClassList);
		Assert.All(cut.FindAll(".moka-select-option"), option => Assert.NotNull(option.GetAttribute("data-moka-index")));

		cut.Find(".moka-select-trigger").KeyDown(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal(["Engineering"], picked);
	}

	[Fact]
	public async Task Attached_OpensTheListAsWideAsTheField_AndWatchesWhichSideItIsOn()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = Select(MokaTheme.Red.WithLists(MokaListLook.Attached), open: false);
		string? rootId = cut.Find(".moka-select").GetAttribute("blazor:elementReference");

		await cut.Find(".moka-select-trigger").ClickAsync(new MouseEventArgs());

		IReadOnlyDictionary<string, object?> options =
			TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-select-dropdown"), rootId, "bottom-start");
		Assert.Equal("match", options["width"]);
		JSRuntimeInvocation watch = Assert.Single(_side.Invocations["watch"]);
		Assert.Equal(rootId, Assert.IsType<ElementReference>(watch.Arguments[1]).Id);
		Assert.Equal(rootId, Assert.IsType<ElementReference>(watch.Arguments[2]).Id);
		Assert.Equal(Assert.IsType<ElementReference>(_popup.Invocations["open"][^1].Arguments[1]).Id,
			Assert.IsType<ElementReference>(watch.Arguments[3]).Id);

		await cut.Find(".moka-select-backdrop").ClickAsync(new MouseEventArgs());

		JSRuntimeInvocation unwatch = Assert.Single(_side.Invocations["unwatch"]);
		Assert.Equal(watch.Arguments[0], unwatch.Arguments[0]);
	}

	[Fact]
	public async Task Attached_StopsWatching_WhenDisposedOpen()
	{
		Select(MokaTheme.Red.WithLists(MokaListLook.Attached));

		await DisposeComponentsAsync();

		Assert.Single(_side.Invocations["unwatch"]);
	}

	[Fact]
	public void AnyOtherLook_NeitherWatchesNorWidensTheList()
	{
		Select(MokaTheme.Red.WithLists(MokaListLook.Check));

		Assert.Empty(_side.Invocations["watch"]);
		Assert.Equal("min", Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(_popup.Invocations["open"][^1].Arguments[4])["width"]);
	}

	[Fact]
	public async Task AutoComplete_MarksTheSuggestionThatIsTheValue()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut =
			AutoComplete(MokaTheme.Red.WithLists(MokaListLook.Check), p => p.Add(x => x.Value, "Bern"));

		await SearchAsync(cut, "Ber");

		IReadOnlyList<IElement> options = cut.FindAll("[role=option]");
		Assert.Equal([false, true, false], options.Select(o => o.ClassList.Contains("moka-autocomplete-option--chosen")));
		Assert.Equal([false, true, false], options.Select(o => o.QuerySelector(".moka-autocomplete-check svg") is not null));
		Assert.Equal(["Berlin", "Bern", "Bergen"], options.Select(o => o.QuerySelector(".moka-autocomplete-option-content")?.TextContent));
	}

	[Fact]
	public async Task AutoComplete_RadioAndTerminal_StartRowsWithAHiddenMark()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> radio =
			AutoComplete(MokaTheme.Red.WithLists(MokaListLook.Radio), p => p.Add(x => x.Value, "Bergen"));
		IRenderedComponent<CascadingValue<MokaTheme>> terminal =
			AutoComplete(MokaTheme.Red.WithLists(MokaListLook.Terminal), p => p.Add(x => x.Value, "Bergen"));

		await SearchAsync(radio, "Ber");
		await SearchAsync(terminal, "Ber");

		foreach (IRenderedComponent<CascadingValue<MokaTheme>> cut in new[] { radio, terminal })
		{
			IReadOnlyList<IElement> marks = cut.FindAll(".moka-autocomplete-mark");
			Assert.Equal(3, marks.Count);
			Assert.All(marks, mark => Assert.Equal("true", mark.GetAttribute("aria-hidden")));
			Assert.Equal([false, false, true], marks.Select(mark => mark.ClassList.Contains("moka-autocomplete-mark--on")));
			Assert.Empty(cut.FindAll(".moka-autocomplete-check"));
		}
	}

	[Fact]
	public async Task AutoComplete_Compact_KeepsItsHintsOutOfTheAccessibleListbox()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = AutoComplete(MokaTheme.Red, p => p
			.Add(x => x.Look, MokaListLook.Compact)
			.Add(x => x.PickHintText, "take"));

		await SearchAsync(cut, "Ber");

		IElement hints = cut.Find(".moka-autocomplete-hints");
		Assert.Equal("true", hints.GetAttribute("aria-hidden"));
		Assert.Equal(["↑↓move", "Entertake", "Escclose"], cut.FindAll(".moka-autocomplete-hint").Select(h => h.TextContent));
		Assert.Equal(3, cut.FindAll("[role=listbox] > [role=option]").Count);
	}

	[Fact]
	public async Task AutoComplete_KeepsTheActiveSuggestion_InALook()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = AutoComplete(MokaTheme.Red.WithLists(MokaListLook.Terminal));
		await SearchAsync(cut, "Ber");

		await cut.Find("input[role=combobox]").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

		Assert.Equal("city-listbox-option-0", cut.Find("input[role=combobox]").GetAttribute("aria-activedescendant"));
		Assert.Contains("moka-autocomplete-option--focused", cut.FindAll("[role=option]")[0].ClassList);
	}

	[Fact]
	public async Task AutoComplete_Attached_WatchesWhichSideTheListIsOn()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = AutoComplete(MokaTheme.Red.WithLists(MokaListLook.Attached));
		string? rootId = cut.Find(".moka-autocomplete").GetAttribute("blazor:elementReference");

		await SearchAsync(cut, "Ber");

		JSRuntimeInvocation watch = Assert.Single(_side.Invocations["watch"]);
		Assert.Equal(rootId, Assert.IsType<ElementReference>(watch.Arguments[1]).Id);
		Assert.Equal(rootId, Assert.IsType<ElementReference>(watch.Arguments[2]).Id);

		await cut.Find("input[role=combobox]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		Assert.Single(_side.Invocations["unwatch"]);
	}

	[Fact]
	public void TreeSelect_Check_EndsAChosenRowWithACheck_InASingleSelectToo()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> check =
			TreeSelect(MokaTheme.Red.WithLists(MokaListLook.Check), p => p.Add(x => x.Value, "b"));
		IRenderedComponent<CascadingValue<MokaTheme>> standard = TreeSelect(MokaTheme.Red, p => p.Add(x => x.Value, "b"));
		check.Find("button.moka-tree-select-trigger").Click();
		standard.Find("button.moka-tree-select-trigger").Click();

		Assert.Equal([false, true], check.FindAll(".moka-tree-select-item").Select(i => i.QuerySelector(".moka-tree-select-check") is not null));
		Assert.Empty(standard.FindAll(".moka-tree-select-check"));
	}

	[Fact]
	public void TreeSelect_Radio_StartsRowsWithAMark_BoxesInAMultipleSelect()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> single =
			TreeSelect(MokaTheme.Red.WithLists(MokaListLook.Radio), p => p.Add(x => x.Value, "a"));
		IRenderedComponent<CascadingValue<MokaTheme>> multiple = TreeSelect(MokaTheme.Red.WithLists(MokaListLook.Radio), p => p
			.Add(x => x.Multiple, true)
			.Add(x => x.SelectedValues, new List<string> { "b" }));
		single.Find("button.moka-tree-select-trigger").Click();
		multiple.Find("button.moka-tree-select-trigger").Click();

		IReadOnlyList<IElement> circles = single.FindAll(".moka-tree-select-item > .moka-tree-select-mark");
		Assert.Equal([true, false], circles.Select(m => m.ClassList.Contains("moka-tree-select-mark--on")));
		Assert.All(circles, m => Assert.Equal("true", m.GetAttribute("aria-hidden")));
		Assert.All(circles, m => Assert.DoesNotContain("moka-tree-select-mark--box", m.ClassList));

		IReadOnlyList<IElement> boxes = multiple.FindAll(".moka-tree-select-mark");
		Assert.All(boxes, m => Assert.Contains("moka-tree-select-mark--box", m.ClassList));
		Assert.Equal([false, true], boxes.Select(m => m.QuerySelector("svg") is not null));
		Assert.Empty(multiple.FindAll(".moka-tree-select-check"));
	}

	[Fact]
	public void TreeSelect_Compact_HintsAtTab_SinceItsRowsAreInTheTabOrder()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut = TreeSelect(MokaTheme.Red.WithLists(MokaListLook.Compact));
		cut.Find("button.moka-tree-select-trigger").Click();

		IElement hints = cut.Find(".moka-tree-select-hints");
		Assert.Equal("true", hints.GetAttribute("aria-hidden"));
		Assert.Equal(["Tabmove", "Enterpick", "Escclose"], cut.FindAll(".moka-tree-select-hint").Select(h => h.TextContent));
	}

	[Fact]
	public async Task TreeSelect_Attached_PlacesTheTreeAgainstTheButton_NotTheLabelAboveIt()
	{
		IRenderedComponent<CascadingValue<MokaTheme>> cut =
			TreeSelect(MokaTheme.Red.WithLists(MokaListLook.Attached), p => p.Add(x => x.Label, "Folder"));
		string? rootId = cut.Find(".moka-tree-select").GetAttribute("blazor:elementReference");
		string? triggerId = cut.Find("button.moka-tree-select-trigger").GetAttribute("blazor:elementReference");

		await cut.Find("button.moka-tree-select-trigger").ClickAsync(new MouseEventArgs());

		TopLayerPopup.AssertOpened(_popup, cut.Find(".moka-tree-select-dropdown"), triggerId, "bottom-start");
		JSRuntimeInvocation watch = Assert.Single(_side.Invocations["watch"]);
		Assert.Equal(rootId, Assert.IsType<ElementReference>(watch.Arguments[1]).Id);
		Assert.Equal(triggerId, Assert.IsType<ElementReference>(watch.Arguments[2]).Id);

		await cut.Find(".moka-tree-select-backdrop").ClickAsync(new MouseEventArgs());

		Assert.Single(_side.Invocations["unwatch"]);
	}

	// What the looks draw lives in the stylesheets, which bUnit doesn't apply.
	[Theory]
	[InlineData("MokaSelect.razor.css", ".moka-gate .moka-select-dropdown")]
	[InlineData("MokaAutoComplete.razor.css", ".moka-gate .moka-autocomplete-dropdown")]
	[InlineData("MokaTreeSelect.razor.css", ".moka-gate .moka-tree-select-dropdown")]
	public void TheBox_FollowsThePopupLook_WithItsOwnValuesAsFallbacks(string file, string selector)
	{
		IReadOnlyDictionary<string, string> box = ScopedCss.Declarations(file, selector);

		Assert.Equal("var(--moka-popup-bg, var(--moka-color-surface))", box["background"]);
		Assert.Equal("var(--moka-popup-color, currentColor)", box["color"]);
		Assert.Equal("var(--moka-popup-font, inherit)", box["font-family"]);
		Assert.Equal("var(--moka-popup-border, var(--moka-color-outline-strong))", box["border-color"]);
		Assert.Equal("var(--moka-popup-radius, var(--moka-radius-popup))", box["border-radius"]);
		Assert.Equal("var(--moka-popup-shadow, var(--moka-shadow-popup))", box["box-shadow"]);
		Assert.Equal("var(--moka-popup-blur, none)", box["backdrop-filter"]);
	}

	[Theory]
	[InlineData("MokaSelect.razor.css", "moka-select", "moka-select-trigger")]
	[InlineData("MokaAutoComplete.razor.css", "moka-autocomplete", "moka-autocomplete-input")]
	[InlineData("MokaTreeSelect.razor.css", "moka-tree-select", "moka-tree-select-trigger")]
	public void Attached_JoinsTheFieldAndTheList_OnWhicheverSideTheListOpened(string file, string root, string field)
	{
		IReadOnlyDictionary<string, string> openField = ScopedCss.Declarations(file, $".moka-gate .{root}--list-attached.{root}--open .{field}");
		IReadOnlyDictionary<string, string> list = ScopedCss.Declarations(file, $".moka-gate .{root}--list-attached .{root}-dropdown");
		IReadOnlyDictionary<string, string> fieldAbove = ScopedCss.Declarations(file, $".moka-gate .{root}--list-attached.{root}--open[data-moka-above] .{field}");
		IReadOnlyDictionary<string, string> listAbove = ScopedCss.Declarations(file, $".moka-gate .{root}--list-attached[data-moka-above] .{root}-dropdown");

		Assert.Equal("0", openField["border-bottom-left-radius"]);
		Assert.Equal("0", openField["border-bottom-right-radius"]);
		Assert.Equal("var(--moka-focus-ring)", openField["box-shadow"]);
		Assert.Equal("0", list["margin-top"]);
		Assert.Equal("0", list["border-top-width"]);
		Assert.Equal("var(--moka-color-primary)", list["border-color"]);
		Assert.StartsWith("var(--moka-focus-ring), ", list["box-shadow"], StringComparison.Ordinal);
		Assert.Equal("0 0 var(--moka-radius-md) var(--moka-radius-md)", fieldAbove["border-radius"]);
		Assert.Equal("0", listAbove["border-bottom-width"]);
		Assert.Equal("var(--moka-radius-md) var(--moka-radius-md) 0 0", listAbove["border-radius"]);
	}

	[Theory]
	[InlineData("MokaSelect.razor.css", "moka-select")]
	[InlineData("MokaAutoComplete.razor.css", "moka-autocomplete")]
	[InlineData("MokaTreeSelect.razor.css", "moka-tree-select")]
	public void Terminal_WritesItsBracketsInCss(string file, string root)
	{
		string mark = $".moka-gate .{root}--list-terminal .{root}-mark";

		Assert.Equal("\"[ ]\"", ScopedCss.Declarations(file, $"{mark}::before")["content"]);
		Assert.Equal("\"[x]\"", ScopedCss.Declarations(file, $"{mark}--on::before")["content"]);
	}

	[Fact]
	public void Solid_FillsTheChosenRowWithTheAccent()
	{
		IReadOnlyDictionary<string, string> chosen =
			ScopedCss.Declarations("MokaSelect.razor.css", ".moka-gate .moka-select--list-solid .moka-select-option--selected");

		Assert.Equal("var(--moka-color-primary)", chosen["background"]);
		Assert.Equal("var(--moka-color-on-primary)", chosen["color"]);
	}

	private IRenderedComponent<CascadingValue<MokaTheme>> Select(MokaTheme theme,
		Action<ComponentParameterCollectionBuilder<MokaSelect<string>>>? parameters = null, bool open = true) =>
		Render<CascadingValue<MokaTheme>>(p => p
			.Add(x => x.Value, theme)
			.AddChildContent<MokaSelect<string>>(select =>
			{
				SelectParameters(select).Add(x => x.IsOpen, open);
				parameters?.Invoke(select);
			}));

	private static ComponentParameterCollectionBuilder<MokaSelect<string>> SelectParameters(
		ComponentParameterCollectionBuilder<MokaSelect<string>> p) => p
		.Add(x => x.Id, "team")
		.Add(x => x.Label, "Team")
		.Add(x => x.Items, Teams);

	private IRenderedComponent<CascadingValue<MokaTheme>> AutoComplete(MokaTheme theme,
		Action<ComponentParameterCollectionBuilder<MokaAutoComplete<string>>>? parameters = null) =>
		Render<CascadingValue<MokaTheme>>(p => p
			.Add(x => x.Value, theme)
			.AddChildContent<MokaAutoComplete<string>>(auto =>
			{
				AutoCompleteParameters(auto);
				parameters?.Invoke(auto);
			}));

	private static ComponentParameterCollectionBuilder<MokaAutoComplete<string>> AutoCompleteParameters(
		ComponentParameterCollectionBuilder<MokaAutoComplete<string>> p) => p
		.Add(x => x.Id, "city")
		.Add(x => x.Debounce, 0)
		.Add(x => x.SearchFunc, query => Task.FromResult(
			Cities.Where(c => c.StartsWith(query, StringComparison.OrdinalIgnoreCase))));

	private IRenderedComponent<CascadingValue<MokaTheme>> TreeSelect(MokaTheme theme,
		Action<ComponentParameterCollectionBuilder<MokaTreeSelect<string>>>? parameters = null) =>
		Render<CascadingValue<MokaTheme>>(p => p
			.Add(x => x.Value, theme)
			.AddChildContent<MokaTreeSelect<string>>(tree =>
			{
				TreeParameters(tree);
				parameters?.Invoke(tree);
			}));

	private static ComponentParameterCollectionBuilder<MokaTreeSelect<string>> TreeParameters(
		ComponentParameterCollectionBuilder<MokaTreeSelect<string>> p) => p
		.Add(x => x.Id, "folder")
		.Add(x => x.Items, Tree);

	private static async Task SearchAsync<TComponent>(IRenderedComponent<TComponent> cut, string text)
		where TComponent : IComponent
	{
		await cut.Find("input[role=combobox]").InputAsync(new ChangeEventArgs { Value = text });
		cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[role=option]")), TimeSpan.FromSeconds(10));
	}
}
