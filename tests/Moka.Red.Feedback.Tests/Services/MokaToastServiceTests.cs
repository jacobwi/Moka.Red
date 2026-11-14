using Moka.Red.Feedback.Notification;
using Moka.Red.Feedback.Toast;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Feedback.Tests.Services;

public class MokaToastServiceTests
{
	private readonly FakeClock _clock = new();
	private readonly MokaNotificationService _center;
	private readonly MokaToastService _toasts;

	public MokaToastServiceTests()
	{
		_center = new MokaNotificationService(_clock);
		_toasts = new MokaToastService(_center, _clock);
	}

	[Fact]
	public void Show_ReturnsAHandleOnTheToast_StampedByTheClock()
	{
		MokaToastHandle handle = _toasts.ShowSuccess("Saved");

		MokaToastMessage toast = Assert.Single(_toasts.Toasts);
		Assert.Equal(handle.Id, toast.Id);
		Assert.True(handle.IsShowing);
		Assert.Equal(_clock.GetUtcNow(), toast.CreatedAt);
		Assert.Equal(MokaToastSeverity.Success, toast.Severity);
	}

	[Fact]
	public void Changed_IsRaisedOncePerChange_AndTheOldSnapshotStaysAsItWas()
	{
		int changes = 0;
		_toasts.Changed += () => changes++;
		_toasts.ShowInfo("One");
		IReadOnlyList<MokaToastMessage> before = _toasts.Toasts;

		_toasts.ShowInfo("Two");

		Assert.Equal(2, changes);
		Assert.Single(before);
		Assert.Equal(2, _toasts.Toasts.Count);
		Assert.Throws<NotSupportedException>(() => ((IList<MokaToastMessage>)_toasts.Toasts).Add(before[0]));
	}

	[Fact]
	public void Update_ReplacesTheRecord_LeavingOthersAsTheSameInstance()
	{
		MokaToastHandle first = _toasts.ShowInfo("First");
		_toasts.ShowInfo("Second");
		MokaToastMessage second = _toasts.Toasts[1];

		Assert.True(first.Update(o => o.Message = "First, changed"));

		Assert.Equal("First, changed", _toasts.Toasts[0].Message);
		Assert.Same(second, _toasts.Toasts[1]);
	}

	[Fact]
	public void Update_OfAGoneToast_ChangesNothing()
	{
		MokaToastHandle handle = _toasts.ShowInfo("Gone");
		handle.Dismiss();

		Assert.False(handle.Update(o => o.Message = "Back"));
		Assert.False(handle.IsShowing);
		Assert.Empty(_toasts.Toasts);
	}

	[Fact]
	public void NewWords_StartTheCountdownAgain_AndAreReadAgain()
	{
		MokaToastHandle handle = _toasts.ShowInfo("Saving");
		MokaToastMessage before = _toasts.Toasts[0];

		handle.Update(o => o.Message = "Saved");

		MokaToastMessage after = _toasts.Toasts[0];
		Assert.Equal(before.Run + 1, after.Run);
		Assert.Equal(before.Announcement + 1, after.Announcement);
	}

	[Fact]
	public void ProgressAlone_NeitherRestartsNorReads_AndStopsTheCountdown()
	{
		MokaToastHandle handle = _toasts.ShowInfo("Uploading", o => o.Progress = 0.1);
		MokaToastMessage before = _toasts.Toasts[0];

		handle.Update(o =>
		{
			o.Progress = 0.5;
			o.Message = "Uploading 5 of 10";
		});

		MokaToastMessage after = _toasts.Toasts[0];
		Assert.Equal(before.Run, after.Run);
		Assert.Equal(before.Announcement, after.Announcement);
		Assert.False(after.CountsDown);
		Assert.Equal(0.5, after.Progress?.Value);
	}

	[Fact]
	public void TakingProgressAway_StartsTheCountdown_AndReadsTheResult()
	{
		MokaToastHandle handle = _toasts.ShowInfo("Uploading", o => o.Progress = MokaToastProgress.Indeterminate);
		MokaToastMessage before = _toasts.Toasts[0];

		handle.Update(o =>
		{
			o.Progress = null;
			o.Message = "Uploaded";
		});

		MokaToastMessage after = _toasts.Toasts[0];
		Assert.True(after.CountsDown);
		Assert.Equal(before.Run + 1, after.Run);
		Assert.Equal(before.Announcement + 1, after.Announcement);
	}

	[Fact]
	public void Progress_EstimatesTheTimeLeft_OnlyOnceItHasMovedLongEnough()
	{
		MokaToastHandle handle = _toasts.ShowInfo("Copying", o => o.Progress = 0.1);
		_clock.Advance(TimeSpan.FromSeconds(1));
		handle.Update(o => o.Progress = 0.3);
		Assert.Null(_toasts.Toasts[0].EstimatedRemaining);

		_clock.Advance(TimeSpan.FromSeconds(3));
		handle.Update(o => o.Progress = 0.5);

		// 0.4 of the work in 4 s leaves 0.5 for another 5 s.
		Assert.Equal(TimeSpan.FromSeconds(5), _toasts.Toasts[0].EstimatedRemaining);
		Assert.Equal(TimeSpan.FromSeconds(5), _toasts.Toasts[0].Remaining);
	}

	[Fact]
	public void Progress_TheAppsOwnTimeLeft_WinsOverTheEstimate()
	{
		_toasts.ShowInfo("Copying", o => o.Progress = new MokaToastProgress(0.2, TimeSpan.FromMinutes(3)));

		Assert.Equal(TimeSpan.FromMinutes(3), _toasts.Toasts[0].Remaining);
	}

	[Fact]
	public void Progress_IsHeldBetweenZeroAndOne_AndRefusesNaN()
	{
		Assert.Equal(1, new MokaToastProgress(1.0000001).Value);
		Assert.Equal(0, ((MokaToastProgress)(-0.2)).Value);
		Assert.True(MokaToastProgress.Indeterminate.IsIndeterminate);
		Assert.Throws<ArgumentOutOfRangeException>(() => new MokaToastProgress(double.NaN));
	}

	[Fact]
	public void Stack_ShowsEveryCall_AsBefore()
	{
		_toasts.ShowInfo("Same");
		_toasts.ShowInfo("Same");

		Assert.Equal(2, _toasts.Toasts.Count);
	}

	[Fact]
	public void Count_BumpsTheCount_AndRestartsTheCountdown_WithoutReadingItAgain()
	{
		MokaToastHandle first = _toasts.ShowError("Connection lost", o => o.Repeats = MokaToastRepeats.Count);
		MokaToastMessage before = _toasts.Toasts[0];

		MokaToastHandle second = _toasts.ShowError("Connection lost", o => o.Repeats = MokaToastRepeats.Count);

		MokaToastMessage after = Assert.Single(_toasts.Toasts);
		Assert.Equal(first.Id, second.Id);
		Assert.Equal(2, after.Count);
		Assert.Equal(before.Run + 1, after.Run);
		Assert.Equal(before.Announcement, after.Announcement);
	}

	[Fact]
	public void Count_MatchesTheWords_SoADifferentMessageStandsApart()
	{
		_toasts.ShowError("Connection lost", o => o.Repeats = MokaToastRepeats.Count);
		_toasts.ShowError("Disk full", o => o.Repeats = MokaToastRepeats.Count);
		_toasts.ShowWarning("Connection lost", o => o.Repeats = MokaToastRepeats.Count);

		Assert.Equal(3, _toasts.Toasts.Count);
	}

	[Fact]
	public void Replace_PutsTheNewWordsInPlace_WithoutACount()
	{
		_toasts.ShowInfo("Before");
		_toasts.ShowInfo("Sync: 1 of 3", o =>
		{
			o.Key = "sync";
			o.Repeats = MokaToastRepeats.Replace;
		});
		MokaToastMessage before = _toasts.Toasts[1];

		_toasts.ShowInfo("Sync: 2 of 3", o =>
		{
			o.Key = "sync";
			o.Repeats = MokaToastRepeats.Replace;
		});

		Assert.Equal(["Before", "Sync: 2 of 3"], _toasts.Toasts.Select(t => t.Message));
		MokaToastMessage after = _toasts.Toasts[1];
		Assert.Equal(before.Id, after.Id);
		Assert.Equal(1, after.Count);
		Assert.Equal(before.Announcement + 1, after.Announcement);
	}

	[Fact]
	public void AKey_KeepsOneToastPerKey_EvenWhenStacking()
	{
		_toasts.ShowInfo("Saving", o => o.Key = "save");
		_toasts.ShowSuccess("Saved", o => o.Key = "save");

		MokaToastMessage toast = Assert.Single(_toasts.Toasts);
		Assert.Equal("Saved", toast.Message);
		Assert.Equal(MokaToastSeverity.Success, toast.Severity);
	}

	[Fact]
	public void Count_WithAKey_TakesTheNewestWords()
	{
		_toasts.ShowInfo("3 new mails", o =>
		{
			o.Key = "mail";
			o.Repeats = MokaToastRepeats.Count;
		});
		_toasts.ShowInfo("4 new mails", o =>
		{
			o.Key = "mail";
			o.Repeats = MokaToastRepeats.Count;
		});

		MokaToastMessage toast = Assert.Single(_toasts.Toasts);
		Assert.Equal("4 new mails", toast.Message);
		Assert.Equal(2, toast.Count);
	}

	[Fact]
	public void Dismiss_SaysWhy()
	{
		List<MokaToastDismissReason> reasons = [];
		MokaToastHandle handle = _toasts.ShowInfo("Bye", o => o.OnDismissed = reasons.Add);

		_toasts.Dismiss(handle.Id, MokaToastDismissReason.Closed);
		_toasts.Dismiss(handle.Id, MokaToastDismissReason.Closed);

		Assert.Equal([MokaToastDismissReason.Closed], reasons);
	}

	[Fact]
	public void Expire_EndsOnlyTheCountdownThatRan()
	{
		List<MokaToastDismissReason> reasons = [];
		MokaToastHandle handle = _toasts.ShowInfo("Again", o =>
		{
			o.Repeats = MokaToastRepeats.Count;
			o.OnDismissed = reasons.Add;
		});
		int oldRun = _toasts.Toasts[0].Run;
		_toasts.ShowInfo("Again", o => o.Repeats = MokaToastRepeats.Count);

		Assert.False(_toasts.Expire(handle.Id, oldRun));
		Assert.True(handle.IsShowing);

		Assert.True(_toasts.Expire(handle.Id, _toasts.Toasts[0].Run));
		Assert.False(handle.IsShowing);
	}

	[Fact]
	public void Expire_LeavesAToastShowingProgress()
	{
		MokaToastHandle handle = _toasts.ShowInfo("Working", o => o.Progress = 0.2);

		Assert.False(_toasts.Expire(handle.Id, _toasts.Toasts[0].Run));
		Assert.True(handle.IsShowing);
	}

	[Fact]
	public void Clear_TellsEveryToast_AndThrowsTheFailuresTogether()
	{
		List<string> heard = [];
		_toasts.ShowInfo("A", o => o.OnDismissed = _ => throw new InvalidOperationException("A failed"));
		_toasts.ShowInfo("B", o => o.OnDismissed = _ => heard.Add("B"));

		AggregateException thrown = Assert.Throws<AggregateException>(_toasts.Clear);

		Assert.Equal(["B"], heard);
		Assert.Equal("A failed", Assert.Single(thrown.InnerExceptions).Message);
		Assert.Empty(_toasts.Toasts);
	}

	[Fact]
	public async Task Track_TurnsTheToastIntoItsResult_AndReturnsIt()
	{
		TaskCompletionSource<int> work = new();
		Task<int> tracked = _toasts.Track(work.Task, "Counting", n => $"{n} found", ex => ex.Message);

		MokaToastMessage loading = Assert.Single(_toasts.Toasts);
		Assert.Equal("Counting", loading.Message);
		Assert.True(loading.Progress?.IsIndeterminate);

		work.SetResult(3);
		Assert.Equal(3, await tracked);

		MokaToastMessage done = Assert.Single(_toasts.Toasts);
		Assert.Equal(loading.Id, done.Id);
		Assert.Equal("3 found", done.Message);
		Assert.Equal(MokaToastSeverity.Success, done.Severity);
		Assert.Null(done.Progress);
		Assert.True(done.CountsDown);
	}

	[Fact]
	public async Task Track_ReportsAFailure_AndStillThrowsIt()
	{
		TaskCompletionSource work = new();
		Task tracked = _toasts.Track(work.Task, "Saving", "Saved", ex => $"Not saved: {ex.Message}");

		InvalidOperationException failure = new("disk full");
		work.SetException(failure);

		InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => tracked);
		Assert.Same(failure, thrown);
		MokaToastMessage toast = Assert.Single(_toasts.Toasts);
		Assert.Equal("Not saved: disk full", toast.Message);
		Assert.Equal(MokaToastSeverity.Error, toast.Severity);
	}

	[Fact]
	public async Task Track_ACancellation_TakesTheToastAway_AndStillThrows()
	{
		TaskCompletionSource work = new();
		Task tracked = _toasts.Track(work.Task, "Saving", "Saved", ex => ex.Message);

		work.SetCanceled(Xunit.TestContext.Current.CancellationToken);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tracked);
		Assert.Empty(_toasts.Toasts);
	}

	[Fact]
	public async Task Track_ACancellation_SaysTheWordsGiven()
	{
		TaskCompletionSource work = new();
		Task tracked = _toasts.Track(work.Task, "Saving", "Saved", ex => ex.Message, cancelled: "Not saved");

		work.SetCanceled(Xunit.TestContext.Current.CancellationToken);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tracked);
		Assert.Equal("Not saved", Assert.Single(_toasts.Toasts).Message);
	}

	[Fact]
	public void AlsoInCenter_PutsTheToastThere_UnderOneKey_UpdatedWithItsResult()
	{
		MokaToastHandle handle = _toasts.ShowInfo("Exporting", o =>
		{
			o.Title = "Report";
			o.AlsoInCenter = true;
			o.Progress = MokaToastProgress.Indeterminate;
		});
		MokaNotification first = Assert.Single(_center.Notifications);
		Assert.Equal("Report", first.Title);
		Assert.Equal("Exporting", first.Message);

		handle.Update(o => o.Progress = 0.5);
		Assert.Same(first, Assert.Single(_center.Notifications));

		handle.Update(o =>
		{
			o.Progress = null;
			o.Message = "Exported";
			o.Severity = MokaToastSeverity.Success;
		});

		MokaNotification result = Assert.Single(_center.Notifications);
		Assert.Equal("Exported", result.Message);
		Assert.Equal(MokaToastSeverity.Success, result.Severity);
		Assert.Equal(first.Key, result.Key);
	}

	[Fact]
	public void AlsoInCenter_CarriesTheActionAcross()
	{
		int undone = 0;
		_toasts.ShowInfo("Deleted", o =>
		{
			o.AlsoInCenter = true;
			o.ActionText = "Undo";
			o.OnAction = () => undone++;
		});

		MokaNotificationAction action = Assert.Single(Assert.Single(_center.Notifications).Actions);
		Assert.Equal("Undo", action.Text);
		action.OnClick();
		Assert.Equal(1, undone);
	}

	[Fact]
	public void ShowFromManyThreads_KeepsEveryToast()
	{
		Parallel.For(0, 200, i => _toasts.ShowInfo($"Toast {i}"));

		Assert.Equal(200, _toasts.Toasts.Count);
		Assert.Equal(200, _toasts.Toasts.Select(t => t.Id).Distinct().Count());
	}

	[Fact]
	public void UpdateFromManyThreads_LosesNoChange()
	{
		MokaToastHandle handle = _toasts.ShowInfo("0", o => o.Progress = 0);

		Parallel.For(0, 200, _ =>
			handle.Update(o => o.Title = (int.Parse(o.Title ?? "0", System.Globalization.CultureInfo.InvariantCulture) + 1)
				.ToString(System.Globalization.CultureInfo.InvariantCulture)));

		Assert.Equal("200", _toasts.Toasts[0].Title);
	}
}
