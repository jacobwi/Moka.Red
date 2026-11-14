using System.Collections.ObjectModel;

namespace Moka.Red.Feedback.Banner;

/// <summary>The default <see cref="IMokaBannerService" />. Thread safe.</summary>
public sealed class MokaBannerService : IMokaBannerService
{
	private readonly Lock _lock = new();
	private MokaBanner[] _banners = [];
	private ReadOnlyCollection<MokaBanner> _snapshot = ReadOnlyCollection<MokaBanner>.Empty;

	/// <inheritdoc />
	public IReadOnlyList<MokaBanner> Banners => Volatile.Read(ref _snapshot);

	/// <inheritdoc />
	public event Action? Changed;

	/// <inheritdoc />
	public void Show(MokaBanner banner)
	{
		ArgumentNullException.ThrowIfNull(banner);
		ArgumentException.ThrowIfNullOrWhiteSpace(banner.Id);

		lock (_lock)
		{
			int index = IndexOf(banner.Id);
			MokaBanner[] next;
			if (index >= 0)
			{
				if (Equals(_banners[index], banner))
				{
					return;
				}

				next = (MokaBanner[])_banners.Clone();
				next[index] = banner;
			}
			else
			{
				next = [.. _banners, banner];
			}

			Publish(next);
		}

		Changed?.Invoke();
	}

	/// <inheritdoc />
	public void Dismiss(string id)
	{
		ArgumentNullException.ThrowIfNull(id);

		lock (_lock)
		{
			int index = IndexOf(id);
			if (index < 0)
			{
				return;
			}

			Publish([.. _banners[..index], .. _banners[(index + 1)..]]);
		}

		Changed?.Invoke();
	}

	/// <inheritdoc />
	public void Clear()
	{
		lock (_lock)
		{
			if (_banners.Length == 0)
			{
				return;
			}

			Publish([]);
		}

		Changed?.Invoke();
	}

	private int IndexOf(string id)
	{
		for (int i = 0; i < _banners.Length; i++)
		{
			if (string.Equals(_banners[i].Id, id, StringComparison.Ordinal))
			{
				return i;
			}
		}

		return -1;
	}

	private void Publish(MokaBanner[] banners)
	{
		_banners = banners;
		Volatile.Write(ref _snapshot,
			banners.Length == 0 ? ReadOnlyCollection<MokaBanner>.Empty : Array.AsReadOnly(banners));
	}
}
