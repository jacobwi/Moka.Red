using System.Reflection;
using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Navigation.Menu;

namespace Moka.Red.Navigation.Tests.Components;

// Match lets a link work out from the address whether it's the current page, the way NavLink does, so a menu doesn't
// have to track the address itself.
public class MokaMenuItemMatchTests : BunitContext
{
	private BunitNavigationManager Navigation => Services.GetRequiredService<BunitNavigationManager>();

	[Fact]
	public void All_LightsThePageItself_AndNothingUnderIt()
	{
		Navigation.NavigateTo("devices");
		IRenderedComponent<MokaMenuItem> cut = Item("devices", NavLinkMatch.All);
		AssertLit(cut, true);

		Navigation.NavigateTo("devices/tv");

		cut.WaitForAssertion(() => AssertLit(cut, false));
	}

	[Fact]
	public void Prefix_LightsThePage_AndEverythingUnderIt()
	{
		Navigation.NavigateTo("devices/tv");

		AssertLit(Item("devices", NavLinkMatch.Prefix), true);
	}

	// NavLink's rule: a prefix ends at a path separator, so one page's name starting another's doesn't count.
	[Fact]
	public void APrefix_EndsAtASlash()
	{
		Navigation.NavigateTo("theme-editor");

		AssertLit(Item("theme", NavLinkMatch.Prefix), false);
	}

	[Fact]
	public void ATrailingSlash_DoesNotCount()
	{
		Navigation.NavigateTo("devices/");

		AssertLit(Item("devices", NavLinkMatch.All), true);
	}

	[Fact]
	public void TheHomePage_WithAll_LightsOnlyAtTheBase()
	{
		IRenderedComponent<MokaMenuItem> cut = Item("./", NavLinkMatch.All);
		AssertLit(cut, true);

		Navigation.NavigateTo("devices");

		cut.WaitForAssertion(() => AssertLit(cut, false));
	}

	[Fact]
	public void ItFollows_NavigateTo()
	{
		IRenderedComponent<MokaMenuItem> cut = Item("forms", NavLinkMatch.Prefix);
		AssertLit(cut, false);

		Navigation.NavigateTo("forms");
		cut.WaitForAssertion(() => AssertLit(cut, true));

		Navigation.NavigateTo("data");
		cut.WaitForAssertion(() => AssertLit(cut, false));
	}

	// As NavLink does on .NET 10: they say where on the page, or what it shows, not which page it is.
	[Fact]
	public void TheQueryAndFragment_DoNotCount()
	{
		Navigation.NavigateTo("forms?tab=2#Checkbox");

		AssertLit(Item("forms", NavLinkMatch.All), true);
		AssertLit(Item("forms#Slider", NavLinkMatch.All), true);
	}

	[Fact]
	public void Active_StillLightsIt()
	{
		IRenderedComponent<MokaMenuItem> cut = Render<MokaMenuItem>(p => p
			.Add(x => x.Text, "Data")
			.Add(x => x.Href, "data")
			.Add(x => x.Match, NavLinkMatch.All)
			.Add(x => x.Active, true));

		AssertLit(cut, true);
	}

	// Every listener is a render waiting to happen on each page change, so an item that never asked for one has none.
	[Fact]
	public void WithoutMatch_ItDoesNotListen()
	{
		int before = Listeners();

		IRenderedComponent<MokaMenuItem> cut = Item("forms", null);
		Navigation.NavigateTo("forms");

		Assert.Equal(before, Listeners());
		AssertLit(cut, false);
	}

	// An item with children is a button that opens them, never a link, so it has no page to match.
	[Fact]
	public void AnItemWithChildren_DoesNotListen()
	{
		int before = Listeners();

		Render<MokaMenuItem>(p => p
			.Add(x => x.Text, "Users")
			.Add(x => x.Href, "users")
			.Add(x => x.Match, NavLinkMatch.Prefix)
			.AddChildContent<MokaMenuItem>(child => child.Add(x => x.Text, "All").Add(x => x.Href, "users/all")));

		Assert.Equal(before, Listeners());
	}

	[Fact]
	public void APageChangeThatChangesNothing_DoesNotRender()
	{
		IRenderedComponent<MokaMenuItem> cut = Item("forms", NavLinkMatch.Prefix);
		int renders = cut.RenderCount;

		Navigation.NavigateTo("data");
		Navigation.NavigateTo("media");

		Assert.Equal(renders, cut.RenderCount);
	}

	[Fact]
	public void TakingMatchAway_StopsListening_AndPutsTheLightOut()
	{
		int before = Listeners();
		Navigation.NavigateTo("forms");
		IRenderedComponent<MokaMenuItem> cut = Item("forms", NavLinkMatch.All);
		Assert.Equal(before + 1, Listeners());

		cut.Render(p => p.Add(x => x.Match, null));

		Assert.Equal(before, Listeners());
		AssertLit(cut, false);
	}

	[Fact]
	public async Task Disposing_StopsListening()
	{
		int before = Listeners();
		Item("forms", NavLinkMatch.Prefix);
		Assert.Equal(before + 1, Listeners());

		await DisposeComponentsAsync();

		Assert.Equal(before, Listeners());
	}

	private static void AssertLit(IRenderedComponent<MokaMenuItem> cut, bool lit)
	{
		IElement link = cut.Find("a.moka-menu-item");
		Assert.Equal(lit, link.ClassList.Contains("moka-menu-item--active"));
		Assert.Equal(lit ? "page" : null, link.GetAttribute("aria-current"));
	}

	private IRenderedComponent<MokaMenuItem> Item(string href, NavLinkMatch? match) => Render<MokaMenuItem>(p => p
		.Add(x => x.Text, "Page")
		.Add(x => x.Href, href)
		.Add(x => x.Match, match));

	// NavigationManager doesn't say how many handlers it has, so this reads the event's backing field.
	private int Listeners()
	{
		FieldInfo field = typeof(NavigationManager).GetField("_locationChanged", BindingFlags.Instance | BindingFlags.NonPublic)
		                  ?? throw new InvalidOperationException("NavigationManager no longer keeps LocationChanged in _locationChanged.");
		return (field.GetValue(Navigation) as Delegate)?.GetInvocationList().Length ?? 0;
	}
}
