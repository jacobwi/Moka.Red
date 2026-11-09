namespace Moka.Red.Data.FileBrowser;

// Back and Forward. A new folder after going back drops the forward half, as in a web browser.
internal sealed class MokaFileHistory
{
	private readonly List<string> _folders = [];
	private int _position = -1;

	public bool CanGoBack => _position > 0;

	public bool CanGoForward => _position >= 0 && _position < _folders.Count - 1;

	public void Visit(string folder)
	{
		if (_position >= 0 && string.Equals(_folders[_position], folder, StringComparison.Ordinal))
		{
			return;
		}

		_folders.RemoveRange(_position + 1, _folders.Count - _position - 1);
		_folders.Add(folder);
		_position = _folders.Count - 1;
	}

	public string? PeekBack() => CanGoBack ? _folders[_position - 1] : null;

	public string? PeekForward() => CanGoForward ? _folders[_position + 1] : null;

	public void StepBack()
	{
		if (CanGoBack)
		{
			_position--;
		}
	}

	public void StepForward()
	{
		if (CanGoForward)
		{
			_position++;
		}
	}
}
