using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Core.Announcer;
using Moka.Red.Core.Extensions;

namespace Moka.Red.Core.Tests.Announcer;

public class MokaAnnouncerTests : BunitContext
{
	public MokaAnnouncerTests() => Services.AddMokaAnnouncer();

	private IMokaAnnouncer Announcer => Services.GetRequiredService<IMokaAnnouncer>();

	[Fact]
	public void Announce_KeepsTheLatestThreeOfEachPoliteness_NumberedInOrder()
	{
		MokaAnnouncer announcer = new();
		for (int i = 1; i <= 5; i++)
		{
			announcer.Announce($"Polite {i}");
		}

		announcer.Announce("Urgent", MokaPoliteness.Assertive);

		Assert.Equal(["Polite 3", "Polite 4", "Polite 5", "Urgent"], announcer.Recent.Select(a => a.Text));
		Assert.Equal([3L, 4L, 5L, 6L], announcer.Recent.Select(a => a.Number));
	}

	[Fact]
	public void Announce_RefusesNoWords()
	{
		MokaAnnouncer announcer = new();

		Assert.Throws<ArgumentException>(() => announcer.Announce("  "));
	}

	[Fact]
	public void TheFirstHostJoined_Speaks_UntilItLeaves()
	{
		MokaAnnouncer announcer = new();
		object first = new();
		object second = new();
		int changes = 0;
		announcer.Changed += () => changes++;

		announcer.Join(first);
		announcer.Join(second);
		Assert.Same(first, announcer.Speaker);
		Assert.Equal(1, changes);

		announcer.Leave(first);
		Assert.Same(second, announcer.Speaker);
		Assert.Equal(2, changes);

		announcer.Leave(second);
		Assert.Null(announcer.Speaker);
	}

	[Fact]
	public void TheHost_KeepsBothRegionsInThePage_EmptyUntilWordsArrive()
	{
		IRenderedComponent<MokaAnnouncerHost> cut = Render<MokaAnnouncerHost>();

		Assert.Equal("polite", cut.Find("[role=status]").GetAttribute("aria-live"));
		Assert.Equal("assertive", cut.Find("[role=alert]").GetAttribute("aria-live"));
		Assert.Equal("false", cut.Find("[role=alert]").GetAttribute("aria-atomic"));
		Assert.All(cut.FindAll(".moka-announcer"), region =>
		{
			Assert.Contains("moka-visually-hidden", region.ClassList);
			Assert.Empty(region.Children);
		});
		Assert.Equal(1, cut.RenderCount);
	}

	[Fact]
	public void TheHost_SpeaksEachAnnouncement_InItsRegion()
	{
		IRenderedComponent<MokaAnnouncerHost> cut = Render<MokaAnnouncerHost>();

		Announcer.Announce("Saved");
		Announcer.Announce("Connection lost", MokaPoliteness.Assertive);

		cut.WaitForAssertion(() =>
		{
			Assert.Equal(["Saved"], cut.Find("[role=status]").Children.Select(c => c.TextContent));
			Assert.Equal(["Connection lost"], cut.Find("[role=alert]").Children.Select(c => c.TextContent));
		});
	}

	[Fact]
	public void TheSameWordsTwice_AreTwoElements_SoBothAreHeard()
	{
		IRenderedComponent<MokaAnnouncerHost> cut = Render<MokaAnnouncerHost>();

		Announcer.Announce("Copied");
		Announcer.Announce("Copied");

		cut.WaitForAssertion(() => Assert.Equal(2, cut.Find("[role=status]").Children.Length));
	}

	[Fact]
	public void WordsSaidBeforeTheHostSpoke_AreNotHeardAgain()
	{
		Announcer.Announce("Before");

		IRenderedComponent<MokaAnnouncerHost> cut = Render<MokaAnnouncerHost>();
		Announcer.Announce("After");

		cut.WaitForAssertion(() => Assert.Equal(["After"], cut.Find("[role=status]").Children.Select(c => c.TextContent)));
	}

	[Fact]
	public void OnlyTheSpeakingHost_FillsItsRegions_AndTheNextTakesOver()
	{
		IRenderedComponent<MokaAnnouncerHost> first = Render<MokaAnnouncerHost>();
		IRenderedComponent<MokaAnnouncerHost> second = Render<MokaAnnouncerHost>();

		Announcer.Announce("One");
		first.WaitForAssertion(() => Assert.Single(first.Find("[role=status]").Children));
		Assert.Empty(second.Find("[role=status]").Children);

		first.Instance.Dispose();
		Announcer.Announce("Two");

		second.WaitForAssertion(() => Assert.Equal(["Two"], second.Find("[role=status]").Children.Select(c => c.TextContent)));
	}

	[Fact]
	public async Task AnnouncementsFromAnotherThread_AreSpoken()
	{
		IRenderedComponent<MokaAnnouncerHost> cut = Render<MokaAnnouncerHost>();

		await Task.Run(() => Announcer.Announce("From a worker"), Xunit.TestContext.Current.CancellationToken);

		cut.WaitForAssertion(() => Assert.Single(cut.Find("[role=status]").Children));
	}
}
