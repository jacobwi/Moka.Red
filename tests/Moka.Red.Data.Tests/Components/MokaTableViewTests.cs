using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Data.Table;
using Moka.Red.Feedback.Tooltip;
using Moka.Red.Tests.Shared;
using static Moka.Red.Data.Tests.Components.TableKit;

namespace Moka.Red.Data.Tests.Components;

// Saved views: table states kept under a name, drawn as chips.
public class MokaTableViewTests : BunitContext
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	private static readonly MokaTableView Everything = new("Everything", Plain());

	private static readonly MokaTableView Researchers =
		new("Researchers", Plain(new MokaTableFilter("Team", MokaFilterOperator.Is, "Research")));

	private static readonly MokaTableState Engineers = Plain(new MokaTableFilter("Team", MokaFilterOperator.Is, "Engineering"));

	public MokaTableViewTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void WithoutViews_NothingIsAdded()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table();

		Assert.Empty(cut.FindAll(".moka-table-views"));
		Assert.Empty(cut.FindAll(".moka-table-toolbar"));
	}

	[Fact]
	public void Views_ShowAsNamedChips()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Views, [Everything, Researchers]));

		Assert.Equal("Saved views", cut.Find(".moka-table-views").GetAttribute("aria-label"));
		Assert.Equal(["Everything", "Researchers"], cut.FindAll(".moka-table-view-pick").Select(b => b.TextContent.Trim()));
	}

	[Fact]
	public void TheChipOfTheViewInForce_IsLit()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Views, [Everything, Researchers])
			.Add(t => t.State, Researchers.State));

		IReadOnlyList<IElement> chips = cut.FindAll(".moka-table-view");
		Assert.DoesNotContain("moka-table-view--on", chips[0].ClassList);
		Assert.Contains("moka-table-view--on", chips[1].ClassList);
		Assert.Equal("false", cut.FindAll(".moka-table-view-pick")[0].GetAttribute("aria-pressed"));
		Assert.Equal("true", cut.FindAll(".moka-table-view-pick")[1].GetAttribute("aria-pressed"));
	}

	[Fact]
	public void ATableAsItStarts_LightsTheViewOfThat()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Views, [Everything, Researchers]));

		IReadOnlyList<IElement> chips = cut.FindAll(".moka-table-view");
		Assert.Contains("moka-table-view--on", chips[0].ClassList);
		Assert.DoesNotContain("moka-table-view--on", chips[1].ClassList);
	}

	[Fact]
	public void AViewOfSomethingElse_IsNotLit()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Views, [Researchers]));

		Assert.Empty(cut.FindAll(".moka-table-view--on"));
	}

	[Fact]
	public void AChip_SaysWhatItShows()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Views, [Everything, Researchers]));

		Assert.Equal("Everything", cut.FindAll(".moka-table-view-pick")[0].GetAttribute("title"));
		Assert.Equal("Team: Research", cut.FindAll(".moka-table-view-pick")[1].GetAttribute("title"));
	}

	[Fact]
	public async Task PickingAChip_PutsItsStateInForce_AndRaisesIt()
	{
		MokaTableState? raised = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Views, [Everything, Researchers])
			.Add(t => t.StateChanged, s => raised = s));

		await cut.FindAll(".moka-table-view-pick")[1].ClickAsync(new MouseEventArgs());

		Assert.Equal(["Grace", "Barbara"], ColumnTexts(cut, 0));
		Assert.Contains("moka-table-view--on", cut.FindAll(".moka-table-view")[1].ClassList);
		Assert.True(Researchers.State.SameView(raised));
	}

	[Fact]
	public async Task ForgettingAChip_RaisesItsView()
	{
		MokaTableView? forgotten = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Views, [Researchers])
			.Add(t => t.ViewForgotten, v => forgotten = v));

		await cut.Find(".moka-table-view-forget").ClickAsync(new MouseEventArgs());

		Assert.Equal(Researchers, forgotten);
	}

	[Fact]
	public void TheForgetButton_IsNamedByATooltip()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Views, [Researchers])
			.Add(t => t.ViewForgotten, _ => { }));

		IconTooltip.AssertNamed(cut.Find(".moka-table-view-forget"), "Forget the view Researchers", MokaTooltipPosition.Bottom);
	}

	[Fact]
	public void WithoutAHandler_ChipsCanNotBeForgotten()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Views, [Researchers]));

		Assert.Empty(cut.FindAll(".moka-table-view-forget"));
	}

	[Fact]
	public void AViewOfTheTableAsItStarts_CanNotBeForgotten()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Views, [Everything, Researchers])
			.Add(t => t.ViewForgotten, _ => { }));

		Assert.Single(cut.FindAll(".moka-table-view-forget"));
		Assert.Empty(cut.FindAll(".moka-table-view")[0].QuerySelectorAll(".moka-table-view-forget"));
	}

	[Fact]
	public async Task ForgettingAChip_LeavesFocusOnTheViews()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(TableModule);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Views, [Researchers])
			.Add(t => t.ViewForgotten, _ => { }));

		await cut.Find(".moka-table-view-forget").ClickAsync(new MouseEventArgs());

		Assert.Contains(".moka-table-views button:not([disabled])", (string[])module.VerifyInvoke("focusFirst").Arguments[1]!);
	}

	[Fact]
	public void WithoutASaveHandler_ThereIsNoSaveButton()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Views, [Researchers]));

		Assert.Empty(cut.FindAll(".moka-table-view-save"));
	}

	[Fact]
	public void ASaveHandlerAlone_ShowsTheButton()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ViewSaved, _ => { }));

		Assert.NotEmpty(cut.FindAll(".moka-table-view-save"));
	}

	[Fact]
	public void ATableAsItStarts_HasNothingToSave()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ViewSaved, _ => { }));

		IElement save = cut.Find(".moka-table-view-save");
		Assert.True(save.HasAttribute("disabled"));
		Assert.Equal("Change the table first", save.GetAttribute("title"));
	}

	[Fact]
	public void AViewInForceAlready_CanNotBeSavedAgain()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Views, [Researchers])
			.Add(t => t.State, Researchers.State)
			.Add(t => t.ViewSaved, _ => { }));

		IElement save = cut.Find(".moka-table-view-save");
		Assert.True(save.HasAttribute("disabled"));
		Assert.Equal("A view shows this already", save.GetAttribute("title"));
	}

	[Fact]
	public void AStateNoViewShows_CanBeSaved()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Views, [Researchers])
			.Add(t => t.State, Engineers)
			.Add(t => t.ViewSaved, _ => { }));

		IElement save = cut.Find(".moka-table-view-save");
		Assert.False(save.HasAttribute("disabled"));
		Assert.Equal("Keep how the table looks now under a name", save.GetAttribute("title"));
	}

	[Fact]
	public async Task Saving_AsksForANameAndRaisesTheNewView()
	{
		MokaTableView? saved = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.State, Engineers)
			.Add(t => t.ViewSaved, v => saved = v));

		await cut.Find(".moka-table-view-save").ClickAsync(new MouseEventArgs());

		Assert.Empty(cut.FindAll(".moka-table-view-save"));
		Assert.Equal("Name for this view", cut.Find(".moka-table-view-name").GetAttribute("aria-label"));

		await cut.Find(".moka-table-view-name").InputAsync(new ChangeEventArgs { Value = "  Things  " });
		await cut.FindAll(".moka-table-view-new button")[0].ClickAsync(new MouseEventArgs());

		Assert.Equal("Things", saved!.Name);
		Assert.True(saved.State.SameView(Engineers));
		Assert.Null(saved.State.Density);
		Assert.Equal(1, saved.State.Page);
		Assert.NotSame(Engineers.Filters, saved.State.Filters);
		Assert.Empty(cut.FindAll(".moka-table-view-name"));
	}

	[Fact]
	public async Task EnterInTheName_Saves()
	{
		MokaTableView? saved = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.State, Engineers)
			.Add(t => t.ViewSaved, v => saved = v));
		await cut.Find(".moka-table-view-save").ClickAsync(new MouseEventArgs());

		await cut.Find(".moka-table-view-name").InputAsync(new ChangeEventArgs { Value = "Mine" });
		await cut.Find(".moka-table-view-name").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		cut.WaitForAssertion(() =>
		{
			Assert.Equal("Mine", saved!.Name);
			Assert.Empty(cut.FindAll(".moka-table-view-name"));
		}, TimeSpan.FromSeconds(10));
	}

	[Fact]
	public async Task EscapeInTheName_GoesBackToTheButton()
	{
		MokaTableView? saved = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.State, Engineers)
			.Add(t => t.ViewSaved, v => saved = v));
		await cut.Find(".moka-table-view-save").ClickAsync(new MouseEventArgs());

		await cut.Find(".moka-table-view-name").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

		cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".moka-table-view-save")), TimeSpan.FromSeconds(10));
		Assert.Null(saved);
	}

	[Fact]
	public async Task Cancel_GoesBackToTheButton()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.State, Engineers)
			.Add(t => t.ViewSaved, _ => { }));
		await cut.Find(".moka-table-view-save").ClickAsync(new MouseEventArgs());

		await cut.FindAll(".moka-table-view-new button")[1].ClickAsync(new MouseEventArgs());

		Assert.NotEmpty(cut.FindAll(".moka-table-view-save"));
	}

	[Fact]
	public async Task AnEmptyName_SavesNothing()
	{
		MokaTableView? saved = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.State, Engineers)
			.Add(t => t.ViewSaved, v => saved = v));
		await cut.Find(".moka-table-view-save").ClickAsync(new MouseEventArgs());

		await cut.Find(".moka-table-view-name").InputAsync(new ChangeEventArgs { Value = "   " });
		await cut.FindAll(".moka-table-view-new button")[0].ClickAsync(new MouseEventArgs());

		Assert.Null(saved);
		Assert.NotEmpty(cut.FindAll(".moka-table-view-name"));
	}

	[Fact]
	public async Task ANameThatIsTaken_SaysSo()
	{
		MokaTableView? saved = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Views, [Researchers])
			.Add(t => t.State, Engineers)
			.Add(t => t.ViewSaved, v => saved = v));
		await cut.Find(".moka-table-view-save").ClickAsync(new MouseEventArgs());

		await cut.Find(".moka-table-view-name").InputAsync(new ChangeEventArgs { Value = "RESEARCHERS" });
		await cut.FindAll(".moka-table-view-new button")[0].ClickAsync(new MouseEventArgs());

		Assert.Null(saved);
		Assert.Equal("There's a view called RESEARCHERS already.", cut.Find(".moka-table-view-problem").TextContent.Trim());
		Assert.Equal("alert", cut.Find(".moka-table-view-problem").GetAttribute("role"));
		Assert.Equal("true", cut.Find(".moka-table-view-name").GetAttribute("aria-invalid"));
	}

	// moka-keys.js calls rule.keys.includes, so a bare list of key names would throw on every key. Enter has to be
	// cancelled, or it submits a form the table sits in.
	[Fact]
	public async Task EnterInTheName_IsCancelledInTheBrowser()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule(KeysModule);
		module.Mode = JSRuntimeMode.Loose;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.State, Engineers)
			.Add(t => t.ViewSaved, _ => { }));

		await cut.Find(".moka-table-view-save").ClickAsync(new MouseEventArgs());

		JSRuntimeInvocation call = Assert.Single(module.Invocations, i => i.Identifier == "preventKeys");
		using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(call.Arguments[1]));
		JsonElement rule = Assert.Single(document.RootElement.EnumerateArray());
		Assert.Equal(JsonValueKind.Null, rule.GetProperty("selector").ValueKind);
		Assert.Contains("Enter", rule.GetProperty("keys").EnumerateArray().Select(k => k.GetString()));
	}

	[Fact]
	public async Task NamingAView_TakesFocus()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.State, Engineers)
			.Add(t => t.ViewSaved, _ => { }));

		await cut.Find(".moka-table-view-save").ClickAsync(new MouseEventArgs());

		Assert.Single(JSInterop.Invocations, i => i.Identifier == "Blazor._internal.domWrapper.focus");
	}

	[Fact]
	public void TheChips_ShareTheToolbarWithTheSearch()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Searchable, true)
			.Add(t => t.Views, [Researchers]));

		IElement toolbar = cut.Find(".moka-table-toolbar");
		Assert.Contains("moka-table-toolbar--views", toolbar.ClassList);
		Assert.Single(toolbar.QuerySelectorAll(".moka-table-search"));
		Assert.Single(toolbar.QuerySelectorAll(".moka-table-views"));
	}

	[Fact]
	public void Views_AreEqualByWhatTheySay()
	{
		MokaTableView same = new("Researchers", Plain(new MokaTableFilter("Team", MokaFilterOperator.Is, "Research")));

		Assert.Equal(Researchers, same);
		Assert.Equal(Researchers.GetHashCode(), same.GetHashCode());
		Assert.NotEqual(Researchers, Researchers with { Name = "Other" });
		Assert.NotEqual(Researchers, Researchers with { State = Engineers });
	}

	// A state with every part said, as the table builds them: no sort, the default page size, no grouping.
	private static MokaTableState Plain(params MokaTableFilter[] filters) =>
		new() { Sorts = [], PageSize = 10, Groups = [], Filters = filters };

	private IRenderedComponent<MokaTable<Person>> Table(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People);
			p.Add(t => t.ChildContent, DefaultColumns);
			extra?.Invoke(p);
		});
}
