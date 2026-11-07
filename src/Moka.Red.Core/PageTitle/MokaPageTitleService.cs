namespace Moka.Red.Core.PageTitle;

/// <summary>Default <see cref="IMokaPageTitleService" />. Holds who said what, in the order they said it.</summary>
public sealed class MokaPageTitleService : IMokaPageTitleService
{
	private readonly List<(object Owner, string Text)> _said = [];

	/// <inheritdoc />
	public string? Text => _said.Count == 0 ? null : _said[^1].Text;

	/// <inheritdoc />
	public event Action? OnChanged;

	/// <inheritdoc />
	public void Say(object owner, string text)
	{
		ArgumentNullException.ThrowIfNull(owner);
		ArgumentNullException.ThrowIfNull(text);

		string? before = Text;
		int existing = _said.FindIndex(said => ReferenceEquals(said.Owner, owner));
		if (existing >= 0)
		{
			_said[existing] = (owner, text);
		}
		else
		{
			_said.Add((owner, text));
		}

		RaiseIfChanged(before);
	}

	/// <inheritdoc />
	public void Forget(object owner)
	{
		ArgumentNullException.ThrowIfNull(owner);

		string? before = Text;
		_said.RemoveAll(said => ReferenceEquals(said.Owner, owner));
		RaiseIfChanged(before);
	}

	private void RaiseIfChanged(string? before)
	{
		if (!string.Equals(before, Text, StringComparison.Ordinal))
		{
			OnChanged?.Invoke();
		}
	}
}
