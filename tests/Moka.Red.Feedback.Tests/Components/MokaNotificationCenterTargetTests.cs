using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Feedback.Notification;

namespace Moka.Red.Feedback.Tests.Components;

// A notification could only lead somewhere in this tab. Focus sits on the link itself, so Enter is the browser's and
// opens the other tab with no script.
public sealed class MokaNotificationCenterTargetTests : BunitContext
{
	public MokaNotificationCenterTargetTests()
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

	private void PushBuild(string? target, string? href = "https://ci.example.com/builds/42") =>
		Notifications.Push(new MokaNotification { Title = "Build #42", Message = "Passed.", Href = href, Target = target });

	[Fact]
	public async Task ALinkForANewTab_HasRel_AndEndsInTheWords()
	{
		PushBuild("_blank");
		IRenderedComponent<MokaNotificationCenter> cut = await OpenAsync();

		IElement link = cut.Find("a.moka-notification-item__main");
		Assert.Equal("_blank", link.GetAttribute("target"));
		Assert.Equal("noopener noreferrer", link.GetAttribute("rel"));
		IElement words = link.Children[^1];
		Assert.Contains("moka-visually-hidden", words.ClassList);
		Assert.Equal(", opens in a new tab", words.TextContent);
	}

	[Fact]
	public async Task TheWords_ComeFromTheCentersText()
	{
		PushBuild("ci");
		IRenderedComponent<MokaNotificationCenter> cut = await OpenAsync(p => p.Add(x => x.Text,
			new MokaNotificationCenterText { NewTab = "öppnas i en ny flik" }));

		Assert.Equal(", öppnas i en ny flik", cut.Find("a.moka-notification-item__main").Children[^1].TextContent);
	}

	[Theory]
	[InlineData("_self")]
	[InlineData("_parent")]
	public async Task ATargetInThisTab_AddsNoRelOrWords(string target)
	{
		PushBuild(target);
		IRenderedComponent<MokaNotificationCenter> cut = await OpenAsync();

		IElement link = cut.Find("a.moka-notification-item__main");
		Assert.Equal(target, link.GetAttribute("target"));
		Assert.False(link.HasAttribute("rel"));
		Assert.DoesNotContain("opens in a new tab", link.TextContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task ALinkWithoutTarget_KeepsItsMarkup()
	{
		PushBuild(null);
		IRenderedComponent<MokaNotificationCenter> cut = await OpenAsync();

		IElement link = cut.Find("a.moka-notification-item__main");
		Assert.False(link.HasAttribute("target"));
		Assert.False(link.HasAttribute("rel"));
		Assert.Equal(["moka-notification-item__icon", "moka-notification-item__content"],
			link.Children.Select(child => child.ClassName));
	}

	// Following the link marks the notification read, which replaces it with a copy. The copy keeps where it opens.
	[Fact]
	public async Task AReadNotification_StillOpensInTheOtherTab()
	{
		PushBuild("_blank");
		IRenderedComponent<MokaNotificationCenter> cut = await OpenAsync();

		await cut.Find("a.moka-notification-item__main").ClickAsync(new MouseEventArgs());

		Assert.True(Assert.Single(Notifications.Notifications).Read);
		Assert.Equal("_blank", Notifications.Notifications[0].Target);
		Assert.Equal("_blank", cut.Find("a.moka-notification-item__main").GetAttribute("target"));
	}

	[Fact]
	public async Task ATargetWithoutHref_IsIgnored()
	{
		PushBuild("_blank", href: null);
		IRenderedComponent<MokaNotificationCenter> cut = await OpenAsync();

		IElement button = cut.Find("button.moka-notification-item__main");
		Assert.False(button.HasAttribute("target"));
		Assert.DoesNotContain("opens in a new tab", button.TextContent, StringComparison.Ordinal);
	}
}
