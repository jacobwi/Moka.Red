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

public class MokaTableViewTests : BunitContext
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	private static readonly MokaTableView Everything = new("Everything", new Dictionary<string, string>());
	private static readonly MokaTableView Guests = new("Guests", new Dictionary<string, string> { ["zone"] = "guests" });

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
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Views, [Everything, Guests]));

		Assert.Equal("Saved views", cut.Find(".moka-table-views").GetAttribute("aria-label"));
		Assert.Equal(["Everything", "Guests"], cut.FindAll(".moka-table-view-pick").Select(b => b.TextContent.Trim()));
	}

	[Fact]
	public void TheChipWithTheFiltersInForce_IsLit()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Views, [Everything, Guests])
			.Add(t => t.ActiveView, new MokaTableView("Anything", new Dictionary<string, string> { ["zone"] = "guests" })));

		IReadOnlyList<IElement> chips = cut.FindAll(".moka-table-view");
		Assert.DoesNotContain("moka-table-view--on", chips[0].ClassList);
		Assert.Contains("moka-table-view--on", chips[1].ClassList);
		Assert.Equal("false", cut.FindAll(".moka-table-view-pick")[0].GetAttribute("aria-pressed"));
		Assert.Equal("true", cut.FindAll(".moka-table-view-pick")[1].GetAttribute("aria-pressed"));
	}

	[Fact]
	public void NoActiveView_LightsNoChip()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Views, [Everything, Guests]));

		Assert.Empty(cut.FindAll(".moka-table-view--on"));
	}

	[Fact]
	public void AChip_SaysWhatItFilters()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Views, [Everything, Guests]));

		Assert.Equal("Everything", cut.FindAll(".moka-table-view-pick")[0].GetAttribute("title"));
		Assert.Equal("zone: guests", cut.FindAll(".moka-table-view-pick")[1].GetAttribute("title"));
	}

	[Fact]
	public async Task PickingAChip_RaisesItsView()
	{
		MokaTableView? picked = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Views, [Everything, Guests])
			.Add(t => t.ViewPicked, v => picked = v));

		await cut.FindAll(".moka-table-view-pick")[1].ClickAsync(new MouseEventArgs());

		Assert.Equal(Guests, picked);
	}

	[Fact]
	public async Task ForgettingAChip_RaisesItsView()
	{
		MokaTableView? forgotten = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Views, [Guests])
			.Add(t => t.ViewForgotten, v => forgotten = v));

		await cut.Find(".moka-table-view-forget").ClickAsync(new MouseEventArgs());

		Assert.Equal(Guests, forgotten);
	}

	[Fact]
	public void TheForgetButton_IsNamedByATooltip()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Views, [Guests])
			.Add(t => t.ViewForgotten, _ => { }));

		IconTooltip.AssertNamed(cut.Find(".moka-table-view-forget"), "Forget the view Guests", MokaTooltipPosition.Bottom);
	}

	[Fact]
	public void WithoutAHandler_ChipsCanNotBeForgotten()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Views, [Guests]));

		Assert.Empty(cut.FindAll(".moka-table-view-forget"));
	}

	[Fact]
	public void AViewWithNoFilters_CanNotBeForgotten()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Views, [Everything, Guests])
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
			.Add(t => t.Views, [Guests])
			.Add(t => t.ViewForgotten, _ => { }));

		await cut.Find(".moka-table-view-forget").ClickAsync(new MouseEventArgs());

		Assert.Contains(".moka-table-views button:not([disabled])", (string[])module.VerifyInvoke("focusFirst").Arguments[1]!);
	}

	[Fact]
	public void WithoutASaveHandler_ThereIsNoSaveButton()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.Views, [Guests]));

		Assert.Empty(cut.FindAll(".moka-table-view-save"));
	}

	[Fact]
	public void ASaveHandlerAlone_ShowsTheButton()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ViewSaved, _ => { }));

		Assert.NotEmpty(cut.FindAll(".moka-table-view-save"));
	}

	[Fact]
	public void WithNoFiltersInForce_ThereIsNothingToSave()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p.Add(t => t.ViewSaved, _ => { }));

		IElement save = cut.Find(".moka-table-view-save");
		Assert.True(save.HasAttribute("disabled"));
		Assert.Equal("Pick a filter first", save.GetAttribute("title"));
	}

	[Fact]
	public void FiltersThatAreAViewAlready_CanNotBeSavedAgain()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Views, [Guests])
			.Add(t => t.ActiveView, Guests)
			.Add(t => t.ViewSaved, _ => { }));

		IElement save = cut.Find(".moka-table-view-save");
		Assert.True(save.HasAttribute("disabled"));
		Assert.Equal("These filters are a view already", save.GetAttribute("title"));
	}

	[Fact]
	public async Task Saving_AsksForANameAndRaisesTheNewView()
	{
		MokaTableView? saved = null;
		Dictionary<string, string> filters = new() { ["zone"] = "things" };
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ActiveView, new MokaTableView("Now", filters))
			.Add(t => t.ViewSaved, v => saved = v));

		await cut.Find(".moka-table-view-save").ClickAsync(new MouseEventArgs());

		Assert.Empty(cut.FindAll(".moka-table-view-save"));
		Assert.Equal("Name for this view", cut.Find(".moka-table-view-name").GetAttribute("aria-label"));

		await cut.Find(".moka-table-view-name").InputAsync(new ChangeEventArgs { Value = "  Things  " });
		await cut.FindAll(".moka-table-view-new button")[0].ClickAsync(new MouseEventArgs());

		Assert.Equal(new MokaTableView("Things", filters), saved);
		Assert.NotSame(filters, saved!.Filters);
		Assert.Empty(cut.FindAll(".moka-table-view-name"));
	}

	[Fact]
	public async Task EnterInTheName_Saves()
	{
		MokaTableView? saved = null;
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.ActiveView, Guests)
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
			.Add(t => t.ActiveView, Guests)
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
			.Add(t => t.ActiveView, Guests)
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
			.Add(t => t.ActiveView, Guests)
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
			.Add(t => t.Views, [Guests])
			.Add(t => t.ActiveView, new MokaTableView("Now", new Dictionary<string, string> { ["zone"] = "things" }))
			.Add(t => t.ViewSaved, v => saved = v));
		await cut.Find(".moka-table-view-save").ClickAsync(new MouseEventArgs());

		await cut.Find(".moka-table-view-name").InputAsync(new ChangeEventArgs { Value = "GUESTS" });
		await cut.FindAll(".moka-table-view-new button")[0].ClickAsync(new MouseEventArgs());

		Assert.Null(saved);
		Assert.Equal("There's a view called GUESTS already.", cut.Find(".moka-table-view-problem").TextContent.Trim());
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
			.Add(t => t.ActiveView, Guests)
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
			.Add(t => t.ActiveView, Guests)
			.Add(t => t.ViewSaved, _ => { }));

		await cut.Find(".moka-table-view-save").ClickAsync(new MouseEventArgs());

		Assert.Single(JSInterop.Invocations, i => i.Identifier == "Blazor._internal.domWrapper.focus");
	}

	[Fact]
	public void TheChips_ShareTheToolbarWithTheSearch()
	{
		IRenderedComponent<MokaTable<Person>> cut = Table(p => p
			.Add(t => t.Searchable, true)
			.Add(t => t.Views, [Guests]));

		IElement toolbar = cut.Find(".moka-table-toolbar");
		Assert.Contains("moka-table-toolbar--views", toolbar.ClassList);
		Assert.Single(toolbar.QuerySelectorAll(".moka-table-search"));
		Assert.Single(toolbar.QuerySelectorAll(".moka-table-views"));
	}

	[Fact]
	public void Views_AreEqualByWhatTheySay()
	{
		MokaTableView same = new("Guests", new Dictionary<string, string> { ["zone"] = "guests" });

		Assert.Equal(Guests, same);
		Assert.Equal(Guests.GetHashCode(), same.GetHashCode());
		Assert.NotEqual(Guests, Guests with { Name = "Other" });
		Assert.NotEqual(Guests, Guests with { Filters = new Dictionary<string, string> { ["zone"] = "home" } });
	}

	private IRenderedComponent<MokaTable<Person>> Table(
		Action<ComponentParameterCollectionBuilder<MokaTable<Person>>>? extra = null) =>
		Render<MokaTable<Person>>(p =>
		{
			p.Add(t => t.Items, People);
			p.Add(t => t.ChildContent, DefaultColumns);
			extra?.Invoke(p);
		});
}
