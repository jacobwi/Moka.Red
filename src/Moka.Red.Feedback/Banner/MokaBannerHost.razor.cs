using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Announcer;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Internal;

namespace Moka.Red.Feedback.Banner;

/// <summary>
///     Draws the banners of <see cref="IMokaBannerService" />: app-wide notices along the top of the page. Place one at
///     the top of the layout. Screen readers hear each banner once, politely, through <see cref="IMokaAnnouncer" />; the
///     host carries a <see cref="MokaAnnouncerHost" />. Colors follow <c>MokaNotice</c>.
/// </summary>
public sealed partial class MokaBannerHost : MokaComponentBase
{
	private readonly Dictionary<string, string> _announced = new(StringComparer.Ordinal);
	private readonly Dictionary<string, bool> _dismissed = new(StringComparer.Ordinal);
	private IReadOnlyList<MokaBanner> _banners = [];
	private bool _checking;
	private bool _disposed;
	private bool _interactive;
	private List<MokaBanner> _shown = [];

	[Inject]
	private IMokaBannerService BannerService { get; set; } = default!;

	[Inject]
	private IMokaBannerStorage Storage { get; set; } = default!;

	[Inject]
	private IMokaAnnouncer Announcer { get; set; } = default!;

	/// <summary>Whether the banners stay at the top of the window as the page scrolls. On by default.</summary>
	[Parameter]
	public bool Sticky { get; set; } = true;

	/// <summary>Every word the host writes around the app's own, to change or translate.</summary>
	[Parameter]
	public MokaBannerText Text { get; set; } = new();

	/// <inheritdoc />
	protected override string RootClass => "moka-banner-host";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-banner-host--sticky", Sticky)
		.AddClass(Class)
		.Build();

	private IReadOnlyList<MokaBanner> Shown => _shown;

	/// <summary>Draws whenever the service's banners change, which no parameter says.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		BannerService.Changed += HandleChanged;
		Refresh();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
		{
			return;
		}

		// The storage lives in the browser, so remembered banners are checked once the page is interactive, and the
		// announcer host inside this one has joined by now.
		_interactive = true;
		await CheckRememberedAsync();
		Announce();
	}

	private void HandleChanged()
	{
		if (!_disposed)
		{
			RunDetached(ApplyAsync);
		}
	}

	private async Task ApplyAsync()
	{
		Refresh();
		StateHasChanged();
		if (_interactive)
		{
			await CheckRememberedAsync();
			Announce();
		}
	}

	private void Refresh()
	{
		_banners = BannerService.Banners;
		List<MokaBanner> shown = new(_banners.Count);
		foreach (MokaBanner banner in _banners)
		{
			// A remembered banner waits for the storage to say it wasn't dismissed, so it never flashes up and goes.
			if (!banner.Remember || (_dismissed.TryGetValue(banner.Id, out bool dismissed) && !dismissed))
			{
				shown.Add(banner);
			}
		}

		_shown = shown;
	}

	private async Task CheckRememberedAsync()
	{
		if (_checking)
		{
			return;
		}

		_checking = true;
		try
		{
			// The list is read again after every check, since banners can arrive while one is under way.
			bool found = false;
			while (Unchecked() is { } banner)
			{
				_dismissed[banner.Id] = await Storage.IsDismissedAsync(banner.Id);
				found = true;
			}

			if (found)
			{
				Refresh();
				StateHasChanged();
			}
		}
		finally
		{
			_checking = false;
		}
	}

	private MokaBanner? Unchecked()
	{
		foreach (MokaBanner banner in BannerService.Banners)
		{
			if (banner.Remember && !_dismissed.ContainsKey(banner.Id))
			{
				return banner;
			}
		}

		return null;
	}

	private void Announce()
	{
		foreach (MokaBanner banner in _shown)
		{
			string words = Text.Announcement(banner);
			if (string.IsNullOrWhiteSpace(words) ||
			    (_announced.TryGetValue(banner.Id, out string? heard) && string.Equals(heard, words, StringComparison.Ordinal)))
			{
				continue;
			}

			_announced[banner.Id] = words;
			Announcer.Announce(words);
		}
	}

	private async Task DismissAsync(MokaBanner banner)
	{
		BannerService.Dismiss(banner.Id);
		_announced.Remove(banner.Id);
		if (banner.Remember)
		{
			_dismissed[banner.Id] = true;
			await Storage.RememberDismissedAsync(banner.Id);
		}
	}

	private static MokaIconDefinition IconFor(MokaBanner banner)
		=> banner.Icon ?? MokaFeedbackFormat.SeverityIcon(banner.Severity);

	/// <inheritdoc />
	protected override ValueTask DisposeAsyncCore()
	{
		_disposed = true;
		BannerService.Changed -= HandleChanged;
		return base.DisposeAsyncCore();
	}
}
