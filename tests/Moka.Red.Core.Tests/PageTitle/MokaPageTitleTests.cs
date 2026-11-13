using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Extensions;
using Moka.Red.Core.PageTitle;
using WebPageTitle = Microsoft.AspNetCore.Components.Web.PageTitle;

namespace Moka.Red.Core.Tests.PageTitle;

// With a PageTitle per page the last one rendered wins, so pages say their name to a service and one host writes it.
public class MokaPageTitleTests : BunitContext
{
	public MokaPageTitleTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaPageTitles();
	}

	private IMokaPageTitleService Titles => Services.GetRequiredService<IMokaPageTitleService>();

	private string? Shown(IRenderedComponent<MokaPageTitleHost> host)
	{
		IReadOnlyList<IRenderedComponent<WebPageTitle>> titles = host.FindComponents<WebPageTitle>();
		return titles.Count == 0 ? null : Render(titles[0].Instance.ChildContent!).Markup;
	}

	[Fact]
	public void TheHost_WritesWhatThePageSaid()
	{
		IRenderedComponent<MokaPageTitleHost> host = Render<MokaPageTitleHost>();

		Render<MokaPageTitle>(p => p.Add(x => x.Text, "Devices"));

		host.WaitForAssertion(() => Assert.Equal("Devices", Shown(host)));
	}

	[Fact]
	public void TheHost_PutsThePrefixInFrontWithASpace()
	{
		Render<MokaPageTitle>(p => p.Add(x => x.Text, "Devices"));

		IRenderedComponent<MokaPageTitleHost> host = Render<MokaPageTitleHost>(p => p.Add(x => x.Prefix, "(2)"));

		Assert.Equal("(2) Devices", Shown(host));
	}

	[Fact]
	public void TheHost_FollowsAPrefixThatChanges()
	{
		Render<MokaPageTitle>(p => p.Add(x => x.Text, "Devices"));
		IRenderedComponent<MokaPageTitleHost> host = Render<MokaPageTitleHost>(p => p.Add(x => x.Prefix, "(2)"));

		host.Render(p => p.Add(x => x.Prefix, null));

		Assert.Equal("Devices", Shown(host));
	}

	[Fact]
	public void TheHost_FollowsAPageWhoseTextChanges()
	{
		IRenderedComponent<MokaPageTitleHost> host = Render<MokaPageTitleHost>();
		IRenderedComponent<MokaPageTitle> page = Render<MokaPageTitle>(p => p.Add(x => x.Text, "Devices"));

		page.Render(p => p.Add(x => x.Text, "Printer"));

		host.WaitForAssertion(() => Assert.Equal("Printer", Shown(host)));
	}

	[Fact]
	public void TheHost_ShowsItsDefault_WhileNoPageHasSaidOne()
	{
		IRenderedComponent<MokaPageTitleHost> host = Render<MokaPageTitleHost>(p => p
			.Add(x => x.Default, "My App")
			.Add(x => x.Prefix, "(1)"));

		Assert.Equal("(1) My App", Shown(host));
	}

	// So the title the host page wrote stands, rather than an empty one replacing it.
	[Fact]
	public void TheHost_WritesNoTitle_WithNothingToSay()
	{
		IRenderedComponent<MokaPageTitleHost> host = Render<MokaPageTitleHost>(p => p.Add(x => x.Prefix, "(1)"));

		Assert.Null(Shown(host));
	}

	[Fact]
	public void APageTitle_RendersNothing() =>
		Assert.Equal(string.Empty, Render<MokaPageTitle>(p => p.Add(x => x.Text, "Devices")).Markup.Trim());

	[Fact]
	public async Task ThePagesName_GoesWithIt()
	{
		IMokaPageTitleService titles = Titles;
		Render<MokaPageTitle>(p => p.Add(x => x.Text, "Devices"));
		Assert.Equal("Devices", titles.Text);

		await DisposeComponentsAsync();

		Assert.Null(titles.Text);
	}

	// The next page says its name before the page it replaces is disposed.
	[Fact]
	public void TheNextPagesName_SurvivesTheOldPageLeaving()
	{
		object oldPage = new();
		object newPage = new();
		Titles.Say(oldPage, "Devices");
		Titles.Say(newPage, "Firewall");
		int changes = 0;
		Titles.OnChanged += () => changes++;

		Titles.Forget(oldPage);

		Assert.Equal("Firewall", Titles.Text);
		Assert.Equal(0, changes);
	}

	[Fact]
	public void TakingBackTheLastName_BringsBackTheOneBefore()
	{
		object page = new();
		object dialog = new();
		Titles.Say(page, "Devices");
		Titles.Say(dialog, "Edit printer");

		Titles.Forget(dialog);

		Assert.Equal("Devices", Titles.Text);
	}

	[Fact]
	public void AnOwnerSayingAgain_ReplacesItsNameWhereItStands()
	{
		object page = new();
		object dialog = new();
		Titles.Say(page, "Devices");
		Titles.Say(dialog, "Edit printer");

		Titles.Say(page, "Firewall");

		Assert.Equal("Edit printer", Titles.Text);
		Titles.Forget(dialog);
		Assert.Equal("Firewall", Titles.Text);
	}

	[Fact]
	public void SayingTheSameNameAgain_IsNoChange()
	{
		object page = new();
		Titles.Say(page, "Devices");
		int changes = 0;
		Titles.OnChanged += () => changes++;

		Titles.Say(page, "Devices");

		Assert.Equal(0, changes);
	}

	[Fact]
	public void AddMokaPageTitles_LeavesAnAppsOwnServiceInPlace()
	{
		ServiceCollection services = [];
		OwnTitles own = new();
		services.AddScoped<IMokaPageTitleService>(_ => own);

		services.AddMokaPageTitles();

		using ServiceProvider provider = services.BuildServiceProvider();
		using IServiceScope scope = provider.CreateScope();
		Assert.Same(own, scope.ServiceProvider.GetRequiredService<IMokaPageTitleService>());
	}

	private sealed class OwnTitles : IMokaPageTitleService
	{
		public string? Text => null;

		public event Action? OnChanged
		{
			add { }
			remove { }
		}

		public void Say(object owner, string text)
		{
		}

		public void Forget(object owner)
		{
		}
	}
}
