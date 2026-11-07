using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.PageTitle;

namespace Moka.Red.Core.Tests.PageTitle;

// The waiting happens in the browser. What matters here is what the component asks of it and what it does on hearing back.
public class MokaTabPresenceTests : BunitContext
{
	private const string Module = "./_content/Moka.Red.Core/PageTitle/MokaTabPresence.razor.js";

	private readonly BunitJSModuleInterop _module;
	private readonly BunitJSModuleInterop _watch;

	public MokaTabPresenceTests()
	{
		_module = JSInterop.SetupModule(Module);
		_watch = _module.SetupModule(invocation => invocation.Identifier == "watch");
		_watch.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void ItRendersNothing() =>
		Assert.Equal(string.Empty, Render<MokaTabPresence>().Markup.Trim());

	[Fact]
	public void ItWatchesForAMinute_ByDefault()
	{
		IRenderedComponent<MokaTabPresence> cut = Render<MokaTabPresence>();

		JSRuntimeInvocation watch = Assert.Single(_module.Invocations["watch"]);
		Assert.Same(cut.Instance, ((DotNetObjectReference<MokaTabPresence>)watch.Arguments[0]!).Value);
		Assert.Equal(60_000, watch.Arguments[1]);
	}

	[Fact]
	public void HiddenAfter_SetsTheWait()
	{
		Render<MokaTabPresence>(p => p.Add(x => x.HiddenAfter, TimeSpan.FromSeconds(5)));

		Assert.Equal(5_000, Assert.Single(_module.Invocations["watch"]).Arguments[1]);
	}

	[Fact]
	public void ANewWait_StartsTheWatchAgain_AndEndsTheOldOne()
	{
		IRenderedComponent<MokaTabPresence> cut = Render<MokaTabPresence>();

		cut.Render(p => p.Add(x => x.HiddenAfter, TimeSpan.FromSeconds(5)));

		Assert.Equal(2, _module.Invocations["watch"].Count);
		Assert.Single(_watch.Invocations["dispose"]);
	}

	[Fact]
	public void TheSameWait_DoesNotStartAgain()
	{
		IRenderedComponent<MokaTabPresence> cut = Render<MokaTabPresence>();

		cut.Render(p => p.Add(x => x.HiddenAfter, TimeSpan.FromMinutes(1)));

		Assert.Single(_module.Invocations["watch"]);
	}

	[Fact]
	public async Task HearingTheTabIsHidden_RaisesHidden()
	{
		int hidden = 0;
		IRenderedComponent<MokaTabPresence> cut = Render<MokaTabPresence>(p => p
			.Add(x => x.Hidden, EventCallback.Factory.Create(this, () => hidden++)));

		await cut.InvokeAsync(cut.Instance.OnTabHidden);

		Assert.Equal(1, hidden);
	}

	[Fact]
	public async Task HearingTheTabIsBack_RaisesVisible()
	{
		int visible = 0;
		IRenderedComponent<MokaTabPresence> cut = Render<MokaTabPresence>(p => p
			.Add(x => x.Visible, EventCallback.Factory.Create(this, () => visible++)));

		await cut.InvokeAsync(cut.Instance.OnTabVisible);

		Assert.Equal(1, visible);
	}

	[Fact]
	public async Task Disposing_EndsTheWatch()
	{
		IRenderedComponent<MokaTabPresence> cut = Render<MokaTabPresence>();

		await cut.Instance.DisposeAsync();

		Assert.Single(_watch.Invocations["dispose"]);
	}
}
