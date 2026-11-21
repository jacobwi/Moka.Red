using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.CommandPalette;
using Moka.Red.Feedback.Extensions;

namespace Moka.Red.Feedback.Tests.Components;

// The WAI-ARIA combobox pattern with a listbox popup, as in the APG's editable combobox with list autocomplete: focus
// stays in the search box, which points at the highlighted command through aria-activedescendant. Nothing carried a
// role before, so screen readers heard a text box and none of the commands.
public class MokaCommandPaletteAriaTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Feedback/moka-command-palette.js";

	private readonly BunitJSModuleInterop _dialog;

	public MokaCommandPaletteAriaTests()
	{
		Services.AddMokaFeedback();
		JSInterop.SetupModule("./_content/Moka.Red.Core/moka-hover.js").Mode = JSRuntimeMode.Loose;
		_dialog = JSInterop.SetupModule("./_content/Moka.Red.Feedback/moka-dialog.js");
		_dialog.Mode = JSRuntimeMode.Loose;
		BunitJSModuleInterop module = JSInterop.SetupModule(Module);
		module.Setup<int>("registerShortcut", _ => true).SetResult(1);
		module.SetupVoid("updateShortcut", _ => true).SetVoidResult();
		module.SetupVoid("focusInput", _ => true).SetVoidResult();
	}

	private IMokaCommandPaletteService Palette => Services.GetRequiredService<IMokaCommandPaletteService>();

	[Fact]
	public async Task ThePalette_IsAModalDialog_NamedByLabel()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Command("a", "A")]);

		IElement dialog = cut.Find(".moka-command-palette");
		Assert.Equal("dialog", dialog.GetAttribute("role"));
		Assert.Equal("true", dialog.GetAttribute("aria-modal"));
		Assert.Equal("Command palette", dialog.GetAttribute("aria-label"));
	}

	[Fact]
	public async Task TheSearchBox_IsAComboboxThatControlsTheListbox()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Command("a", "A"), Command("b", "B")]);

		IElement input = cut.Find("input");
		IElement listbox = cut.Find("[role=listbox]");
		Assert.Equal("combobox", input.GetAttribute("role"));
		Assert.Equal("Search commands", input.GetAttribute("aria-label"));
		Assert.Equal("true", input.GetAttribute("aria-expanded"));
		Assert.Equal("list", input.GetAttribute("aria-autocomplete"));
		Assert.False(string.IsNullOrEmpty(listbox.Id));
		Assert.Equal(listbox.Id, input.GetAttribute("aria-controls"));
		Assert.Equal("Command palette", listbox.GetAttribute("aria-label"));
		Assert.False(listbox.HasAttribute("hidden"));
	}

	[Fact]
	public async Task TheNames_CanBeReplaced()
	{
		Palette.Register(Command("a", "A"));
		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>(p => p
			.Add(x => x.Label, "Befehle")
			.Add(x => x.SearchLabel, "Befehle suchen"));
		await cut.InvokeAsync(Palette.Open);

		Assert.Equal("Befehle", cut.Find("[role=dialog]").GetAttribute("aria-label"));
		Assert.Equal("Befehle", cut.Find("[role=listbox]").GetAttribute("aria-label"));
		Assert.Equal("Befehle suchen", cut.Find("[role=combobox]").GetAttribute("aria-label"));
	}

	[Fact]
	public async Task EachCommand_IsAnOption_WithAnIdFromTheListbox()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Command("a", "A"), Command("b", "B"), Command("c", "C")]);

		string listbox = cut.Find("[role=listbox]").Id!;
		IReadOnlyList<IElement> options = cut.FindAll("[role=option]");
		Assert.Equal([$"{listbox}-option-0", $"{listbox}-option-1", $"{listbox}-option-2"], options.Select(o => o.Id));
		Assert.All(options, option => Assert.Equal(listbox, option.Closest("[role=listbox]")!.Id));
	}

	[Fact]
	public async Task TheHighlight_IsTheActiveDescendant_AndTheOnlySelectedOption()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Command("a", "A"), Command("b", "B"), Command("c", "C")]);

		AssertHighlighted(cut, 0);

		await cut.Find(".moka-command-palette").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
		AssertHighlighted(cut, 1);

		await cut.Find(".moka-command-palette").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });
		await cut.Find(".moka-command-palette").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });
		AssertHighlighted(cut, 2);
	}

	[Fact]
	public async Task AKeyAfterTheMouse_PointsAtTheOptionItCarriesOnFrom()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Command("a", "A"), Command("b", "B"), Command("c", "C")]);

		await cut.InvokeAsync(() => cut.Instance.OnItemHovered(1));
		await cut.Find(".moka-command-palette").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

		AssertHighlighted(cut, 2);
	}

	[Fact]
	public async Task ADisabledCommand_IsADisabledOption()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open(
		[
			Command("a", "A"),
			new MokaCommand { Id = "b", Title = "B", Disabled = true }
		]);

		IReadOnlyList<IElement> options = cut.FindAll("[role=option]");
		Assert.False(options[0].HasAttribute("aria-disabled"));
		Assert.Equal("true", options[1].GetAttribute("aria-disabled"));
	}

	[Fact]
	public async Task EachGroup_IsAGroupNamedByItsHeading_AndCommandsWithoutOneStandInTheListbox()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open(
		[
			new MokaCommand { Id = "home", Title = "Go to home", Group = "Go" },
			new MokaCommand { Id = "save", Title = "Save", Group = "Edit" },
			new MokaCommand { Id = "find", Title = "Find", Group = "Edit" },
			new MokaCommand { Id = "help", Title = "Help" }
		]);

		IElement listbox = cut.Find("[role=listbox]");
		IReadOnlyList<IElement> groups = cut.FindAll("[role=group]");
		Assert.Equal(2, groups.Count);
		Assert.All(groups, group => Assert.Equal(listbox.Id, group.ParentElement!.Id));

		IElement edit = groups[0];
		IElement heading = cut.Find($"#{edit.GetAttribute("aria-labelledby")}");
		Assert.Equal("Edit", heading.TextContent);
		Assert.Equal("presentation", heading.GetAttribute("role"));
		Assert.Contains("moka-command-palette__group", heading.ClassName, StringComparison.Ordinal);
		Assert.Equal(["Save", "Find"], edit.QuerySelectorAll("[role=option] .moka-command-palette__item-title").Select(t => t.TextContent));

		IElement help = cut.FindAll("[role=option]")[0];
		Assert.Equal(listbox.Id, help.ParentElement!.Id);
		Assert.Equal("Help", help.QuerySelector(".moka-command-palette__item-title")!.TextContent);
		Assert.Equal(["0", "1", "2", "3"], cut.FindAll("[role=option]").Select(o => o.GetAttribute("data-moka-index")));
	}

	[Fact]
	public async Task WithoutGroups_TheOptionsStandInTheListbox()
	{
		Palette.RegisterMany([new MokaCommand { Id = "save", Title = "Save", Group = "Edit" }]);
		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>(p => p.Add(x => x.ShowGroups, false));
		await cut.InvokeAsync(Palette.Open);

		Assert.Empty(cut.FindAll("[role=group]"));
		Assert.Equal(cut.Find("[role=listbox]").Id, cut.Find("[role=option]").ParentElement!.Id);
	}

	[Fact]
	public async Task TheStatusRegion_IsThereAndQuiet_WhileCommandsShow()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Command("a", "A")]);

		IElement status = cut.Find("[role=status]");
		Assert.Equal(string.Empty, status.TextContent);
		Assert.Contains("moka-visually-hidden", status.ClassName, StringComparison.Ordinal);
	}

	[Fact]
	public async Task NoMatches_AreAnnounced_AndTheListboxCollapses()
	{
		IRenderedComponent<MokaCommandPalette> cut = await Open([Command("a", "Save"), Command("b", "Find")]);
		IElement statusBefore = cut.Find("[role=status]");

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "zzz" });

		IElement status = cut.Find("[role=status]");
		Assert.Equal("No commands found", status.TextContent);
		Assert.Equal(statusBefore.GetAttribute("class"), status.GetAttribute("class"));

		IElement input = cut.Find("input");
		Assert.Equal("false", input.GetAttribute("aria-expanded"));
		Assert.False(input.HasAttribute("aria-activedescendant"));
		Assert.True(cut.Find("[role=listbox]").HasAttribute("hidden"));
		Assert.Empty(cut.FindAll("[role=option]"));
		Assert.Equal("true", cut.Find(".moka-command-palette__empty").GetAttribute("aria-hidden"));

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "sa" });

		Assert.Equal(string.Empty, cut.Find("[role=status]").TextContent);
		Assert.Equal("true", cut.Find("input").GetAttribute("aria-expanded"));
	}

	// A live region added with its text already in it is skipped by screen readers, so a palette that opens with nothing
	// to list opens its region empty and fills it on the next render.
	[Fact]
	public async Task OpeningWithNothingToList_FillsTheStatusRegionAfterItIsOnThePage()
	{
		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>();
		List<string> seen = [];
		cut.OnMarkupUpdated += (_, _) =>
		{
			if (cut.FindAll("[role=status]") is [IElement status])
			{
				seen.Add(status.TextContent);
			}
		};

		await cut.InvokeAsync(Palette.Open);

		cut.WaitForAssertion(() => Assert.Equal("No commands found", cut.Find("[role=status]").TextContent));
		Assert.Equal(string.Empty, seen[0]);
		Assert.Equal("No commands found", seen[^1]);
	}

	[Fact]
	public async Task NoResultsText_IsWhatTheRegionSays()
	{
		Palette.Register(Command("a", "A"));
		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>(p => p.Add(x => x.NoResultsText, "Nichts gefunden"));
		await cut.InvokeAsync(Palette.Open);

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "zzz" });

		Assert.Equal("Nichts gefunden", cut.Find("[role=status]").TextContent);
	}

	private static void AssertHighlighted(IRenderedComponent<MokaCommandPalette> cut, int index)
	{
		IReadOnlyList<IElement> options = cut.FindAll("[role=option]");
		Assert.Equal(options[index].Id, cut.Find("[role=combobox]").GetAttribute("aria-activedescendant"));
		Assert.Equal([index], options.Select((o, i) => (o, i)).Where(x => x.o.GetAttribute("aria-selected") == "true").Select(x => x.i));
		Assert.Contains("moka-command-palette__item--focused", options[index].ClassName, StringComparison.Ordinal);
	}

	private static MokaCommand Command(string id, string title) => new() { Id = id, Title = title };

	private async Task<IRenderedComponent<MokaCommandPalette>> Open(IEnumerable<MokaCommand> commands)
	{
		Palette.RegisterMany(commands);
		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>();
		await cut.InvokeAsync(Palette.Open);
		return cut;
	}

	// It says it's a modal dialog, so it holds Tab inside while it's open and gives focus back once it closes.
	[Fact]
	public async Task AnOpenPalette_HoldsFocus_AndLetsGoWhenItCloses()
	{
		_dialog.Setup<int>("trapFocus", _ => true).SetResult(7);
		_dialog.SetupVoid("releaseFocus", _ => true).SetVoidResult();
		Palette.Register(new MokaCommand { Id = "a", Title = "Open settings" });
		IRenderedComponent<MokaCommandPalette> cut = Render<MokaCommandPalette>();

		await cut.InvokeAsync(Palette.Open);

		JSRuntimeInvocation trap = Assert.Single(_dialog.Invocations["trapFocus"]);
		Assert.True(trap.Arguments[0] is ElementReference);
		Assert.Empty(_dialog.Invocations["releaseFocus"]);

		await cut.InvokeAsync(Palette.Close);

		Assert.Equal([7], Assert.Single(_dialog.Invocations["releaseFocus"]).Arguments);
	}
}
