using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Feedback.Notification;

namespace Moka.Red.Feedback.Tests.Components;

public class MokaNotificationCenterActionsTests : BunitContext
{
	public MokaNotificationCenterActionsTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaFeedback();
	}

	private IMokaNotificationService Notifications => Services.GetRequiredService<IMokaNotificationService>();

	private async Task<IRenderedComponent<MokaNotificationCenter>> OpenAsync(
		Action<ComponentParameterCollectionBuilder<MokaNotificationCenter>>? parameters = null)
	{
		IRenderedComponent<MokaNotificationCenter> cut = Render<MokaNotificationCenter>(p => parameters?.Invoke(p));
		await cut.Find("button.moka-notification-bell").ClickAsync(new MouseEventArgs());
		return cut;
	}

	[Fact]
	public async Task Actions_AreButtons_ThatRunAndMarkTheNotificationRead()
	{
		int retried = 0;
		Notifications.Push(new MokaNotification
		{
			Title = "Sync failed",
			Message = "No server.",
			Actions = [new MokaNotificationAction("Retry", () => retried++)]
		});
		IRenderedComponent<MokaNotificationCenter> cut = await OpenAsync();

		IElement retry = cut.Find(".moka-notification-item__action");
		Assert.Equal("button", retry.GetAttribute("type"));
		Assert.Equal("Retry", retry.TextContent);
		Assert.Contains("moka-notification-item--has-actions", cut.Find(".moka-notification-item").ClassList);

		await retry.ClickAsync(new MouseEventArgs());

		Assert.Equal(1, retried);
		Assert.True(Assert.Single(Notifications.Notifications).Read);
	}

	[Fact]
	public async Task AnActionThatRemoves_TakesTheNotificationAway()
	{
		Notifications.Push(new MokaNotification
		{
			Title = "Invite",
			Message = "Join the project?",
			Actions = [new MokaNotificationAction("Accept", () => Task.CompletedTask) { Removes = true }]
		});
		IRenderedComponent<MokaNotificationCenter> cut = await OpenAsync();

		await cut.Find(".moka-notification-item__action").ClickAsync(new MouseEventArgs());

		Assert.Empty(Notifications.Notifications);
	}

	[Fact]
	public async Task AHref_MakesTheNotificationALink()
	{
		Notifications.Push(new MokaNotification { Title = "Build #42", Message = "Passed.", Href = "/builds/42" });
		IRenderedComponent<MokaNotificationCenter> cut = await OpenAsync();

		IElement link = cut.Find("a.moka-notification-item__main");
		Assert.Equal("/builds/42", link.GetAttribute("href"));

		await link.ClickAsync(new MouseEventArgs());
		Assert.True(Assert.Single(Notifications.Notifications).Read);
	}

	[Fact]
	public async Task AScriptHref_IsDropped()
	{
		Notifications.Push(new MokaNotification { Title = "Bad", Message = "Link.", Href = "javascript:alert(1)" });
		IRenderedComponent<MokaNotificationCenter> cut = await OpenAsync();

		Assert.Empty(cut.FindAll("a.moka-notification-item__main"));
		Assert.Single(cut.FindAll("button.moka-notification-item__main"));
	}

	[Fact]
	public void AKey_ReplacesTheEarlierNotification()
	{
		Notifications.Push(new MokaNotification { Key = "build-42", Title = "Build #42", Message = "Running." });
		Notifications.Push(new MokaNotification { Title = "Other", Message = "Stays." });
		Notifications.Push(new MokaNotification { Key = "build-42", Title = "Build #42", Message = "Passed." });

		Assert.Equal(["Passed.", "Stays."], Notifications.Notifications.Select(n => n.Message).OrderBy(m => m));
		Assert.Equal(2, Notifications.Notifications.Count);
	}

	[Fact]
	public async Task TheWords_CanBeReplaced()
	{
		Notifications.Push("Sauvegarde", "Terminée.");
		IRenderedComponent<MokaNotificationCenter> cut = await OpenAsync(p => p.Add(x => x.Text, new MokaNotificationCenterText
		{
			Title = "Avis",
			MarkAllRead = "Tout lu",
			ClearAll = "Tout effacer",
			Unread = "Non lu",
			Dismiss = title => $"Fermer {title}",
			Bell = unread => $"Avis, {unread} non lus",
			Time = (_, _) => "à l'instant"
		}));

		Assert.Equal("Avis", cut.Find(".moka-notification-panel__title").TextContent);
		Assert.Equal(["Tout lu", "Tout effacer"], cut.FindAll(".moka-notification-panel__action").Select(b => b.TextContent));
		Assert.Equal("Fermer Sauvegarde", cut.Find(".moka-notification-item__dismiss").GetAttribute("aria-label"));
		Assert.Equal("Avis, 1 non lus", cut.Find(".moka-notification-bell").GetAttribute("aria-label"));
		Assert.Equal("à l'instant", cut.Find(".moka-notification-item__time").TextContent);
		Assert.Equal("Non lu", cut.Find(".moka-notification-panel__list > span[hidden]").TextContent);
	}

	[Fact]
	public async Task TheEmptyPanel_SaysItsOwnWords()
	{
		IRenderedComponent<MokaNotificationCenter> cut = await OpenAsync(p => p.Add(x => x.Text,
			new MokaNotificationCenterText { Empty = "Rien de neuf" }));

		Assert.Equal("Rien de neuf", cut.Find(".moka-notification-panel__empty span").TextContent);
	}
}
