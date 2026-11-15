using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moka.Red.Extensions;
using Moka.Red.Feedback.Extensions;
using Moka.Red.Feedback.Notification;
using Moka.Red.Feedback.Toast;

namespace Moka.Red.Feedback.Tests.Services;

// App-wide toast defaults and a limit on stored notifications, set once through the options pattern.
public class MokaFeedbackOptionsTests : BunitContext
{
	private static readonly DateTime Start = new(2026, 11, 14, 9, 0, 0, DateTimeKind.Utc);

	private static MokaToastService Toasts(Action<MokaToastOptions> defaults) =>
		new(options: Options.Create(new MokaFeedbackOptions { ToastDefaults = defaults }));

	private static MokaNotificationService Notifications(int? limit) =>
		new(options: Options.Create(new MokaFeedbackOptions { NotificationLimit = limit }));

	private static MokaNotification Note(string title, int minute, bool read = false, string? key = null, Guid? id = null) =>
		new()
		{
			Id = id ?? Guid.NewGuid(),
			Title = title,
			Message = title,
			Timestamp = Start.AddMinutes(minute),
			Read = read,
			Key = key
		};

	// ── Toast defaults ──

	[Fact]
	public void TheDefaults_RunOnEveryToast_AndTheCallsOwnConfigureWins()
	{
		MokaToastService toasts = Toasts(t =>
		{
			t.Repeats = MokaToastRepeats.Count;
			t.Duration = TimeSpan.FromSeconds(8);
		});

		toasts.Show("Saved");
		toasts.Show("Moved", configure: t => t.Duration = TimeSpan.FromSeconds(2));

		Assert.Equal([TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(2)], toasts.Toasts.Select(t => t.Duration));
		Assert.All(toasts.Toasts, t => Assert.Equal(MokaToastRepeats.Count, t.Repeats));
	}

	[Fact]
	public void TheDefaults_SeeTheMessageAndSeverity()
	{
		MokaToastService toasts = Toasts(t =>
		{
			if (t.Severity == MokaToastSeverity.Error)
			{
				t.Duration = null;
			}
		});

		toasts.ShowError("Couldn't save");
		toasts.ShowSuccess("Saved");

		Assert.Equal([null, TimeSpan.FromSeconds(5)], toasts.Toasts.Select(t => t.Duration));
	}

	[Fact]
	public void TheDefaults_ReachTheLoadingToastOfTrack()
	{
		MokaToastService toasts = Toasts(t => t.Title = "Upload");
		TaskCompletionSource running = new();

		_ = toasts.Track(running.Task, "Uploading", "Uploaded", _ => "Failed");

		MokaToastMessage loading = Assert.Single(toasts.Toasts);
		Assert.Equal(("Upload", "Uploading", MokaToastProgress.Indeterminate), (loading.Title, loading.Message, loading.Progress));
		running.SetResult();
	}

	[Fact]
	public void AnUpdate_KeepsWhatTheToastHad_WithoutRunningTheDefaultsAgain()
	{
		int runs = 0;
		MokaToastService toasts = Toasts(t =>
		{
			runs++;
			t.Title = "App";
		});

		MokaToastHandle handle = toasts.Show("Saving");
		handle.Update(t => t.Message = "Saved");

		MokaToastMessage toast = Assert.Single(toasts.Toasts);
		Assert.Equal(1, runs);
		Assert.Equal(("App", "Saved"), (toast.Title, toast.Message));
	}

	[Fact]
	public void AMarkupToast_TakesTheDefaults_ForWhatThePageDoesntPass()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaFeedback(f => f.ToastDefaults = t =>
		{
			t.Duration = TimeSpan.FromSeconds(8);
			t.ShowIcon = false;
		});
		IMokaToastService toasts = Services.GetRequiredService<IMokaToastService>();

		Render<MokaToast>(p => p.Add(x => x.Open, true).Add(x => x.Message, "Plain"));
		Render<MokaToast>(p => p
			.Add(x => x.Open, true)
			.Add(x => x.Message, "Own")
			.Add(x => x.Duration, TimeSpan.FromSeconds(3))
			.Add(x => x.ShowIcon, true));

		Assert.Equal([(TimeSpan.FromSeconds(8), false), (TimeSpan.FromSeconds(3), true)],
			toasts.Toasts.Select(t => ((TimeSpan?)t.Duration, t.ShowIcon)));
	}

	[Fact]
	public void WithoutDefaults_AMarkupToast_IsWhatItWas()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMokaFeedback();
		IMokaToastService toasts = Services.GetRequiredService<IMokaToastService>();

		Render<MokaToast>(p => p.Add(x => x.Open, true).Add(x => x.Message, "Plain"));

		MokaToastMessage toast = Assert.Single(toasts.Toasts);
		Assert.Equal((TimeSpan.FromSeconds(5), true, true, MokaToastRepeats.Stack),
			((TimeSpan?)toast.Duration, toast.ShowIcon, toast.ShowCloseButton, toast.Repeats));
	}

	// ── Notification limit ──

	[Fact]
	public void PastTheLimit_TheOldestReadNotificationsGoFirst()
	{
		MokaNotificationService notifications = Notifications(3);
		notifications.Push(Note("Old unread", 0));
		notifications.Push(Note("Old read", 1, read: true));
		notifications.Push(Note("Newer read", 2, read: true));

		notifications.Push(Note("New", 3));

		Assert.Equal(["New", "Newer read", "Old unread"], notifications.Notifications.Select(n => n.Title));
	}

	[Fact]
	public void WithNothingRead_TheOldestUnreadGo()
	{
		MokaNotificationService notifications = Notifications(2);
		notifications.Push(Note("First", 0));
		notifications.Push(Note("Second", 1));

		notifications.Push(Note("Third", 2));

		Assert.Equal(["Third", "Second"], notifications.Notifications.Select(n => n.Title));
	}

	[Fact]
	public void ThePushedNotification_StaysEvenWhenItIsTheOldest()
	{
		MokaNotificationService notifications = Notifications(1);
		notifications.Push(Note("Recent", 10));

		notifications.Push(Note("Late arrival", 0));

		Assert.Equal(["Late arrival"], notifications.Notifications.Select(n => n.Title));
	}

	[Fact]
	public void APushThatReplacesByIdOrKey_RemovesNothing()
	{
		Guid id = Guid.NewGuid();
		MokaNotificationService notifications = Notifications(2);
		notifications.Push(Note("Status", 0, id: id));
		notifications.Push(Note("Sync", 1, key: "sync"));

		notifications.Push(Note("Status, later", 2, id: id));
		notifications.Push(Note("Sync, later", 3, key: "sync"));

		Assert.Equal(["Sync, later", "Status, later"], notifications.Notifications.Select(n => n.Title));
	}

	[Fact]
	public void APushThatTrims_RaisesOnChangedOnce()
	{
		MokaNotificationService notifications = Notifications(1);
		notifications.Push(Note("First", 0));
		int changes = 0;
		notifications.OnChanged += () => changes++;

		notifications.Push(Note("Second", 1));

		Assert.Equal(1, changes);
	}

	[Fact]
	public void WithoutALimit_EveryNotificationStays()
	{
		MokaNotificationService notifications = Notifications(null);

		for (int i = 0; i < 250; i++)
		{
			notifications.Push(Note($"#{i}", i));
		}

		Assert.Equal(250, notifications.Notifications.Count);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-5)]
	public void ALimitBelowOne_IsRefused(int limit) =>
		Assert.Throws<ArgumentOutOfRangeException>(() => new MokaFeedbackOptions { NotificationLimit = limit });

	// ── Registration ──

	[Fact]
	public void ConfigureCalls_AddUp()
	{
		ServiceCollection services = new();
		services.AddMokaFeedback(f => f.NotificationLimit = 1);
		services.Configure<MokaFeedbackOptions>(f => f.ToastDefaults = t => t.Title = "App");
		using ServiceProvider provider = services.BuildServiceProvider();

		IMokaNotificationService notifications = provider.GetRequiredService<IMokaNotificationService>();
		notifications.Push("One", "1");
		notifications.Push("Two", "2");
		IMokaToastService toasts = provider.GetRequiredService<IMokaToastService>();
		toasts.Show("Saved");

		Assert.Single(notifications.Notifications);
		Assert.Equal("App", Assert.Single(toasts.Toasts).Title);
	}

	[Fact]
	public void TheMetaPackagesFeedbackSetting_ReachesTheServices()
	{
		ServiceCollection services = new();
		services.AddMokaRed(o => o.Feedback = f => f.ToastDefaults = t => t.Repeats = MokaToastRepeats.Count);
		using ServiceProvider provider = services.BuildServiceProvider();

		IMokaToastService toasts = provider.GetRequiredService<IMokaToastService>();
		toasts.Show("Saved");

		Assert.Equal(MokaToastRepeats.Count, Assert.Single(toasts.Toasts).Repeats);
	}

	[Fact]
	public void WithoutSettings_TheServicesResolve_AndChangeNothing()
	{
		ServiceCollection services = new();
		services.AddMokaFeedback();
		using ServiceProvider provider = services.BuildServiceProvider();

		IMokaToastService toasts = provider.GetRequiredService<IMokaToastService>();
		toasts.Show("Saved");

		MokaToastMessage toast = Assert.Single(toasts.Toasts);
		Assert.Equal((TimeSpan.FromSeconds(5), MokaToastRepeats.Stack), ((TimeSpan?)toast.Duration, toast.Repeats));
	}
}
