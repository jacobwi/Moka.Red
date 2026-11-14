using Microsoft.AspNetCore.Components;

namespace Moka.Red.Core.Announcer;

/// <summary>
///     Keeps the two live regions <see cref="IMokaAnnouncer" /> speaks through. A <c>MokaToastHost</c> and a
///     <c>MokaBannerHost</c> each carry one, so an app with either needs no other; place one in the layout of an app with
///     neither. Several in a page take turns: the first one mounted speaks, and the next takes over when it goes, so
///     nothing is heard twice.
/// </summary>
public sealed partial class MokaAnnouncerHost : ComponentBase
{
	private bool _disposed;
	private bool _ready;
	private long _since;
	private bool _speaking;

	[Inject]
	private IMokaAnnouncer Announcer { get; set; } = default!;

	/// <inheritdoc />
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		Announcer.Changed -= HandleChanged;
		Announcer.Leave(this);
	}

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		Announcer.Changed += HandleChanged;
		Announcer.Join(this);
		NoteSpeaker();
	}

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		// A region only speaks for what is added once it is in the page, so words wait for the render after the one
		// that wrote the empty regions.
		if (firstRender)
		{
			_ready = true;
			if (_speaking && LatestNumber() > _since)
			{
				StateHasChanged();
			}
		}
	}

	private IEnumerable<MokaAnnouncement> Shown(MokaPoliteness politeness)
	{
		if (!_ready || !_speaking)
		{
			yield break;
		}

		foreach (MokaAnnouncement announcement in Announcer.Recent)
		{
			if (announcement.Politeness == politeness && announcement.Number > _since)
			{
				yield return announcement;
			}
		}
	}

	// What was said before this host began speaking was heard through the host that spoke then.
	private void NoteSpeaker()
	{
		bool speaking = ReferenceEquals(Announcer.Speaker, this);
		if (speaking && !_speaking)
		{
			_since = LatestNumber();
		}

		_speaking = speaking;
	}

	private long LatestNumber()
	{
		IReadOnlyList<MokaAnnouncement> recent = Announcer.Recent;
		return recent.Count > 0 ? recent[^1].Number : 0;
	}

	private void HandleChanged() => _ = ApplyAsync();

	// Announcements arrive on whatever thread made them. The render goes to the renderer's thread, and a failure
	// reaches Blazor's own error handling rather than vanishing with a discarded task.
	private async Task ApplyAsync()
	{
		if (_disposed)
		{
			return;
		}

		try
		{
			await InvokeAsync(() =>
			{
				if (_disposed)
				{
					return;
				}

				NoteSpeaker();
				StateHasChanged();
			});
		}
		catch (ObjectDisposedException)
		{
			// The renderer went away between the announcement and the dispatch.
		}
		catch (Exception ex) when (!_disposed)
		{
			await DispatchExceptionAsync(ex);
		}
	}
}
