using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Icons;
using Moka.Red.Primitives.Button;
using Moka.Red.Primitives.SelectionBar;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// What runs out of room is measured in a browser, by core moka-fit.js. These cover what the bar hands its script, and
// what it does once the script has reported which actions went to the menu.
public class MokaSelectionBarMenuTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Primitives/SelectionBar/MokaSelectionBar.razor.js";
	private const string Popup = "./_content/Moka.Red.Core/moka-popup.js";
	private const string Css = "MokaSelectionBar.razor.css";

	private readonly BunitJSModuleInterop _module;
	private readonly BunitJSModuleInterop _popup;
	private readonly List<string> _done = [];

	public MokaSelectionBarMenuTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(Module);
		_module.Mode = JSRuntimeMode.Loose;
		_popup = JSInterop.SetupModule(Popup);
		_popup.Mode = JSRuntimeMode.Loose;
	}

	private IRenderedComponent<MokaSelectionBar> Bar(
		MokaSelectionBarOverflow overflow = MokaSelectionBarOverflow.Menu,
		int count = 3,
		bool renameDisabled = false,
		Func<int, string>? moreLabel = null) =>
		Render<MokaSelectionBar>(p =>
		{
			p.Add(x => x.Count, count)
				.Add(x => x.Overflow, overflow)
				.AddChildContent(Actions(renameDisabled));
			if (moreLabel is not null)
			{
				p.Add(x => x.MoreLabel, moreLabel);
			}
		});

	private RenderFragment Actions(bool renameDisabled) => builder =>
	{
		Action(builder, 0, "Download", MokaIcons.Action.Download);
		Action(builder, 10, "Rename", MokaIcons.Action.Edit, disabled: renameDisabled);
		Action(builder, 20, "Move", null);
		Action(builder, 30, "Delete", MokaIcons.Action.Delete, MokaColor.Error);
	};

	private void Action(RenderTreeBuilder builder, int sequence, string text, Core.Icons.MokaIconDefinition? icon,
		MokaColor? color = null, bool disabled = false)
	{
		builder.OpenComponent<MokaSelectionAction>(sequence);
		builder.AddComponentParameter(sequence + 1, nameof(MokaSelectionAction.Text), text);
		builder.AddComponentParameter(sequence + 2, nameof(MokaSelectionAction.Icon), icon);
		builder.AddComponentParameter(sequence + 3, nameof(MokaSelectionAction.Color), color);
		builder.AddComponentParameter(sequence + 4, nameof(MokaSelectionAction.Disabled), disabled);
		builder.AddComponentParameter(sequence + 5, nameof(MokaSelectionAction.OnClick),
			EventCallback.Factory.Create<MouseEventArgs>(this, () => _done.Add(text)));
		builder.CloseComponent();
	}

	private static string[] Ids(IRenderedComponent<MokaSelectionBar> cut, params string[] texts) =>
	[
		.. texts.Select(text => cut.FindAll(".moka-selection-action")
			.Single(button => button.TextContent.Trim() == text)
			.GetAttribute("data-moka-action")!)
	];

	private static void Drop(IRenderedComponent<MokaSelectionBar> cut, params string[] texts)
	{
		string[] ids = Ids(cut, texts);
		cut.InvokeAsync(() => cut.Instance.OnDroppedChanged(ids));
	}

	[Fact]
	public void WrapIsTheDefault_AndChangesNothing()
	{
		IRenderedComponent<MokaSelectionBar> cut = Render<MokaSelectionBar>(p => p
			.Add(x => x.Count, 3)
			.AddChildContent("<button id=\"act\">Delete</button>"));

		Assert.Equal(MokaSelectionBarOverflow.Wrap, cut.Instance.Overflow);
		Assert.Equal("moka-selection-bar", cut.Find(".moka-selection-bar").ClassName);
		Assert.Empty(cut.FindAll(".moka-selection-bar-more"));
		Assert.Empty(_module.Invocations["bindSelectionBar"]);
	}

	[Fact]
	public void Menu_IsAClassOnTheBar_AndBindsTheScriptOnce()
	{
		IRenderedComponent<MokaSelectionBar> cut = Bar();

		cut.Render(p => p.Add(x => x.Count, 4));

		Assert.Contains("moka-selection-bar--menu", cut.Find(".moka-selection-bar").ClassList);
		JSRuntimeInvocation bind = Assert.Single(_module.Invocations["bindSelectionBar"]);
		Assert.IsType<ElementReference>(bind.Arguments[0]);
		Assert.NotNull(bind.Arguments[1]);
	}

	[Fact]
	public void EachAction_CarriesItsIdAndPriority()
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("sv-SE");
		IElement button;
		try
		{
			button = Render<MokaSelectionAction>(p => p
				.Add(x => x.Text, "Delete")
				.Add(x => x.Priority, -2)).Find("button");
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}

		Assert.Equal("-2", button.GetAttribute("data-moka-priority"));
		Assert.Null(button.GetAttribute("data-moka-action"));
		Assert.Contains("moka-selection-action", button.ClassList);
		Assert.Contains("moka-btn--text", button.ClassList);

		IRenderedComponent<MokaSelectionBar> bar = Bar();
		string[] ids = [.. bar.FindAll(".moka-selection-action").Select(b => b.GetAttribute("data-moka-action")!)];
		Assert.Equal(4, ids.Distinct(StringComparer.Ordinal).Count());
	}

	[Fact]
	public void TheMoreButton_IsAMenuButtonNamedByATooltip()
	{
		IRenderedComponent<MokaSelectionBar> cut = Bar();
		Drop(cut, "Move", "Delete");

		IElement more = cut.Find(".moka-selection-bar-more-button");
		Assert.True(more.HasAttribute("data-moka-more"));
		Assert.Equal("menu", more.GetAttribute("aria-haspopup"));
		Assert.Equal("false", more.GetAttribute("aria-expanded"));
		IconTooltip.AssertNamed(more, "2 more actions", MokaTooltipPosition.TopEnd);
	}

	[Fact]
	public void MoreLabel_NamesTheButton()
	{
		IRenderedComponent<MokaSelectionBar> cut = Bar(moreLabel: count => $"Noch {count}");
		Drop(cut, "Delete");

		Assert.Equal("Noch 1", cut.Find(".moka-selection-bar-more-button").GetAttribute("aria-label"));
	}

	[Fact]
	public void TheMenu_ListsTheDroppedActionsInTheRowsOrder()
	{
		IRenderedComponent<MokaSelectionBar> cut = Bar();
		Drop(cut, "Move", "Delete");

		cut.Find(".moka-selection-bar-more-button").Click();

		IElement menu = cut.Find("[role=menu]");
		Assert.Equal("manual", menu.GetAttribute("popover"));
		Assert.Equal(cut.Find(".moka-selection-bar-more-button").Id, menu.GetAttribute("aria-labelledby"));
		Assert.Equal(menu.Id, cut.Find(".moka-selection-bar-more-button").GetAttribute("aria-controls"));
		Assert.Equal("true", cut.Find(".moka-selection-bar-more-button").GetAttribute("aria-expanded"));
		Assert.Equal(["Move", "Delete"], cut.FindAll("[role=menuitem]").Select(item => item.TextContent.Trim()));
		Assert.All(cut.FindAll("[role=menuitem]"), item => Assert.Equal("-1", item.GetAttribute("tabindex")));
		Assert.Contains("moka-selection-bar-menu-item--danger", cut.FindAll("[role=menuitem]")[1].ClassList);
		Assert.Single(_popup.Invocations["open"]);
	}

	[Fact]
	public void NothingDropped_TheButtonDoesntOpenAMenu()
	{
		IRenderedComponent<MokaSelectionBar> cut = Bar();

		cut.Find(".moka-selection-bar-more-button").Click();

		Assert.Empty(cut.FindAll("[role=menu]"));
	}

	[Fact]
	public void AMenuItem_ClosesTheMenu_ThenRunsTheAction()
	{
		IRenderedComponent<MokaSelectionBar> cut = Bar();
		Drop(cut, "Move", "Delete");
		cut.Find(".moka-selection-bar-more-button").Click();

		cut.FindAll("[role=menuitem]")[1].Click();

		Assert.Equal(["Delete"], _done);
		Assert.Empty(cut.FindAll("[role=menu]"));
		Assert.Single(_popup.Invocations["close"]);
	}

	[Fact]
	public void TheRowButton_RunsTheSameAction()
	{
		IRenderedComponent<MokaSelectionBar> cut = Bar();

		cut.FindAll(".moka-selection-action")[2].Click();

		Assert.Equal(["Move"], _done);
	}

	[Fact]
	public void ADisabledAction_IsDisabledInTheMenuToo()
	{
		IRenderedComponent<MokaSelectionBar> cut = Bar(renameDisabled: true);
		Drop(cut, "Rename");
		cut.Find(".moka-selection-bar-more-button").Click();

		IElement item = cut.Find("[role=menuitem]");
		Assert.Equal("true", item.GetAttribute("aria-disabled"));
		item.Click();

		Assert.Empty(_done);
		Assert.Single(cut.FindAll("[role=menu]"));
	}

	[Fact]
	public void AnActionThatChanges_RedrawsTheOpenMenu()
	{
		IRenderedComponent<MokaSelectionBar> cut = Bar();
		Drop(cut, "Rename");
		cut.Find(".moka-selection-bar-more-button").Click();
		Assert.Null(cut.Find("[role=menuitem]").GetAttribute("aria-disabled"));

		cut.Render(p => p.AddChildContent(Actions(renameDisabled: true)));

		Assert.Equal("true", cut.Find("[role=menuitem]").GetAttribute("aria-disabled"));
	}

	[Fact]
	public void NothingLeftDropped_ClosesTheMenu()
	{
		IRenderedComponent<MokaSelectionBar> cut = Bar();
		Drop(cut, "Delete");
		cut.Find(".moka-selection-bar-more-button").Click();

		cut.InvokeAsync(() => cut.Instance.OnDroppedChanged([]));

		Assert.Empty(cut.FindAll("[role=menu]"));
	}

	// The script holds the bar's element, which the render that hides the bar removes.
	[Fact]
	public void HidingTheBar_LetsTheScriptGo_AndShowingItBindsTheNewOne()
	{
		IRenderedComponent<MokaSelectionBar> cut = Bar();

		cut.Render(p => p.Add(x => x.Count, 0));
		Assert.Single(_module.Invocations["unbindSelectionBar"]);

		cut.Render(p => p.Add(x => x.Count, 2));
		Assert.Equal(2, _module.Invocations["bindSelectionBar"].Count);
	}

	[Fact]
	public void MovingIntoTheFixedDock_BindsAgain()
	{
		IRenderedComponent<MokaSelectionBar> cut = Bar();

		cut.Render(p => p.Add(x => x.Fixed, true));

		Assert.Single(_module.Invocations["unbindSelectionBar"]);
		Assert.Equal(2, _module.Invocations["bindSelectionBar"].Count);
	}

	[Fact]
	public async Task Disposal_LetsTheScriptGo()
	{
		IRenderedComponent<MokaSelectionBar> cut = Bar();

		await cut.Instance.DisposeAsync();

		Assert.Single(_module.Invocations["unbindSelectionBar"]);
	}

	[Fact]
	public void TheScript_DropsOnlyActions_AndMeasuresARowThatGrows()
	{
		string script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Css", "MokaSelectionBar.razor.js"))
			.ReplaceLineEndings("\n");

		Assert.Contains("sides: [':scope > .moka-selection-bar-actions']", script, StringComparison.Ordinal);
		Assert.Contains("more: ':scope > .moka-selection-bar-more [data-moka-more]'", script, StringComparison.Ordinal);
		Assert.Contains("filler: ':not(.moka-selection-action)'", script, StringComparison.Ordinal);
		Assert.Contains("flexible: true", script, StringComparison.Ordinal);
	}

	[Fact]
	public void TheStylesheet_KeepsOneRowOnlyOnceMeasured()
	{
		Assert.Equal("wrap", ScopedCss.Declarations(Css, ".moka-selection-bar")["flex-wrap"]);
		Assert.Equal("nowrap", ScopedCss.Declarations(Css, ".moka-selection-bar--menu[data-moka-fitted]")["flex-wrap"]);
		Assert.Equal("none", ScopedCss.Declarations(Css,
			".moka-selection-bar--menu > .moka-selection-bar-actions ::deep > [data-moka-dropped]")["display"]);
		Assert.Equal("none", ScopedCss.Declarations(Css, ".moka-selection-bar-more")["display"]);
		Assert.Equal("inline-flex", ScopedCss.Declarations(Css,
			".moka-selection-bar--menu[data-moka-has-more] > .moka-selection-bar-more")["display"]);
	}
}
