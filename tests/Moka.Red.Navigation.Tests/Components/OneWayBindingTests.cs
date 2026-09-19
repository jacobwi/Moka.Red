using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Navigation.CommandBar;
using Moka.Red.Navigation.Sidebar;

namespace Moka.Red.Navigation.Tests.Components;

// These components used to write the user's change into their own [Parameter]. Blazor passes every
// parameter again whenever the parent renders, so with a one-way value the parent's next render put
// the old value back. Each test changes the state through the UI, re-renders with the same value
// (what an unrelated parent render does), and then checks that a new value still wins.
public class OneWayBindingTests : BunitContext
{
	[Fact]
	public async Task Sidebar_StaysDismissed_WhenTheParentPassesTheSameValue()
	{
		IRenderedComponent<MokaSidebar> cut = RenderOverlaySidebar();

		await cut.Find(".moka-sidebar__backdrop").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Open, true));

		Assert.Contains("moka-sidebar--closed", cut.Find("nav").ClassList);
		Assert.Empty(cut.FindAll(".moka-sidebar__backdrop"));
	}

	[Fact]
	public async Task Sidebar_FollowsANewValueFromTheParent()
	{
		IRenderedComponent<MokaSidebar> cut = RenderOverlaySidebar();

		await cut.Find(".moka-sidebar__backdrop").ClickAsync(new MouseEventArgs());
		cut.Render(p => p.Add(x => x.Open, false));
		cut.Render(p => p.Add(x => x.Open, true));

		Assert.Contains("moka-sidebar--open", cut.Find("nav").ClassList);
	}

	[Fact]
	public async Task CommandBar_KeepsTheTypedSearch_WhenTheParentPassesTheSameValue()
	{
		string? searched = null;
		IRenderedComponent<MokaCommandBar> cut = Render<MokaCommandBar>(p => p
			.Add(x => x.SearchValue, "rep")
			.Add(x => x.OnSearch, EventCallback.Factory.Create<string>(this, v => searched = v)));

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "report" });
		cut.Render(p => p.Add(x => x.SearchValue, "rep"));
		await cut.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal("report", cut.Find("input").GetAttribute("value"));
		Assert.Equal("report", searched);
	}

	[Fact]
	public async Task CommandBar_FollowsANewValueFromTheParent()
	{
		IRenderedComponent<MokaCommandBar> cut = Render<MokaCommandBar>(p => p.Add(x => x.SearchValue, "rep"));

		await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "report" });
		cut.Render(p => p.Add(x => x.SearchValue, "readme"));

		Assert.Equal("readme", cut.Find("input").GetAttribute("value"));
	}

	private IRenderedComponent<MokaSidebar> RenderOverlaySidebar()
	{
		// An open overlay sidebar moves focus in and out through its script.
		JSInterop.SetupModule("./_content/Moka.Red.Navigation/Sidebar/MokaSidebar.razor.js").Mode = JSRuntimeMode.Loose;

		return Render<MokaSidebar>(p => p
			.Add(x => x.Overlay, true)
			.Add(x => x.Open, true)
			.AddChildContent("<a href=\"/\">Home</a>"));
	}
}
