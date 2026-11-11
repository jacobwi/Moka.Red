using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Moka.Red.Data.FileBrowser;

public partial class MokaFileBrowser<TItem>
{
	private const string PopupModulePath = "./_content/Moka.Red.Core/moka-popup.js";
	private const int MostSuggestions = 8;
	private const char Separator = '/';

	private bool _pathOpen;
	private bool _pathShown;
	private bool _pathMissing;
	private bool _pathOpening;
	private bool _focusPathEdit;
	private int _pathHighlight = -1;
	private string _pathText = "";
	private ElementReference _pathAnchor;
	private ElementReference _pathPanel;

	private string PathListId => $"{_idBase}-path";

	private string PopupKey => $"{_idBase}-path-popup";

	private string? PathHighlightId => _pathHighlight >= 0 ? PathOptionId(_pathHighlight) : null;

	private string PathOptionId(int index) => FormattableString.Invariant($"{_idBase}-path-{index}");

	private string RootPrefix => Crumbs.Count == 0 ? "" : Crumbs[0].Title.TrimEnd(Separator) + Separator;

	/// <summary>A key pressed in the path field, from the browser, with what the field holds.</summary>
	/// <param name="key">ArrowDown, ArrowUp, Enter, Escape or Tab.</param>
	/// <param name="value">The text in the field.</param>
	/// <returns>A task that ends once the browser has acted on it.</returns>
	[JSInvokable]
	public async Task HandlePathKey(string key, string? value)
	{
		ArgumentNullException.ThrowIfNull(key);
		if (!_pathOpen)
		{
			return;
		}

		_pathText = value ?? _pathText;
		IReadOnlyList<PathSuggestion> suggestions = Suggestions(ParsePath(_pathText));
		switch (key)
		{
			case "ArrowDown" when suggestions.Count > 0:
				_pathHighlight = (_pathHighlight + 1) % suggestions.Count;
				break;
			case "ArrowUp" when suggestions.Count > 0:
				_pathHighlight = _pathHighlight <= 0 ? suggestions.Count - 1 : _pathHighlight - 1;
				break;
			case "Tab" when _pathHighlight >= 0 && _pathHighlight < suggestions.Count:
				FillPath(suggestions[_pathHighlight]);
				break;
			case "Enter":
				await GoToTypedPathAsync(suggestions);
				break;
			case "Escape":
				ClosePath(focusEdit: true);
				break;
			default:
				break;
		}

		StateHasChanged();
	}

	/// <summary>Focus left the path field, from the browser. The crumbs come back.</summary>
	[JSInvokable]
	public void HandlePathLeft()
	{
		if (_pathOpen && !_pathOpening)
		{
			ClosePath(focusEdit: false);
			StateHasChanged();
		}
	}

	private void OpenPath()
	{
		_pathText = PathOfCrumbs();
		_pathHighlight = -1;
		_pathMissing = false;
		_pathOpen = true;
		_pathOpening = true;
	}

	private void ClosePath(bool focusEdit)
	{
		_pathOpen = false;
		_pathHighlight = -1;
		_pathMissing = false;
		_focusPathEdit = focusEdit;
	}

	private void TypePath(string? typed)
	{
		_pathText = typed ?? "";
		_pathHighlight = -1;
		_pathMissing = false;
	}

	private void FillPath(PathSuggestion suggestion)
	{
		_pathText = suggestion.Path;
		_pathHighlight = -1;
		_pathMissing = false;
	}

	private async Task GoToTypedPathAsync(IReadOnlyList<PathSuggestion> suggestions)
	{
		string? folder = _pathHighlight >= 0 && _pathHighlight < suggestions.Count
			? suggestions[_pathHighlight].Folder
			: ResolvePath(_pathText);
		if (folder is null)
		{
			_pathMissing = ParsePath(_pathText).Waiting is null;
			return;
		}

		ClosePath(focusEdit: false);
		await GoToAsync(folder);
		FocusListWhenDrawn();
	}

	private string PathOfCrumbs()
	{
		if (Crumbs.Count == 0)
		{
			return "";
		}

		return RootPrefix + string.Concat(Crumbs.Skip(1).Select(crumb => crumb.Title + Separator));
	}

	// The whole text as a folder, the part after the last slash included when something is typed there.
	private string? ResolvePath(string text)
	{
		ParsedPath parsed = ParsePath(text);
		if (parsed.Folder is not { } folder)
		{
			return null;
		}

		if (parsed.Partial.Length == 0)
		{
			return folder;
		}

		return ListingOf(folder)?.FirstOrDefault(info => info.IsFolder && string.Equals(info.Name, parsed.Partial, StringComparison.OrdinalIgnoreCase))?.Key;
	}

	// Walks the text from the first crumb, folder by folder, as far as what has been read of them allows.
	private ParsedPath ParsePath(string text)
	{
		if (Crumbs.Count == 0)
		{
			return new ParsedPath(null, text, null, "");
		}

		string prefix = RootPrefix;
		if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			return new ParsedPath(null, text, null, "");
		}

		string rest = text[prefix.Length..];
		int cut = rest.LastIndexOf(Separator);
		string head = cut >= 0 ? rest[..(cut + 1)] : "";
		string partial = cut >= 0 ? rest[(cut + 1)..] : rest;
		string folder = Crumbs[0].FolderKey;
		foreach (string segment in head.Split(Separator, StringSplitOptions.RemoveEmptyEntries))
		{
			if (ListingOf(folder) is not { } listing)
			{
				return new ParsedPath(null, partial, folder, prefix + head);
			}

			MokaFileInfo? next = listing.FirstOrDefault(info => info.IsFolder && string.Equals(info.Name, segment, StringComparison.OrdinalIgnoreCase));
			if (next is null)
			{
				return new ParsedPath(null, partial, null, prefix + head);
			}

			folder = next.Key;
		}

		return new ParsedPath(folder, partial, ListingOf(folder) is null ? folder : null, prefix + head);
	}

	private IReadOnlyList<PathSuggestion> Suggestions(ParsedPath parsed)
	{
		if (parsed.Folder is not { } folder)
		{
			if (parsed.Head.Length == 0 && Crumbs.Count > 0 && RootPrefix.StartsWith(_pathText, StringComparison.OrdinalIgnoreCase))
			{
				return [new PathSuggestion(Crumbs[0].FolderKey, Crumbs[0].Title, null, RootPrefix)];
			}

			return [];
		}

		if (ListingOf(folder) is not { } listing)
		{
			return [];
		}

		string parent = TitleOf(folder);
		List<MokaFileInfo> folders = [.. listing.Where(info => info.IsFolder && info.Name.StartsWith(parsed.Partial, StringComparison.OrdinalIgnoreCase))];
		folders.Sort((a, b) => MokaFileNames.Compare(a.Name, b.Name));
		return [.. folders.Take(MostSuggestions).Select(info => new PathSuggestion(info.Key, info.Name, parent, parsed.Head + info.Name + Separator))];
	}

	private string? PathNoneText(ParsedPath parsed) => parsed.Waiting is not null
		? null
		: parsed.Folder is not null && parsed.Partial.Length == 0 && !_pathMissing ? Text.NoSubfolders : Text.NoSuchFolder;

	private async Task SyncPathAsync()
	{
		if (_pathOpen && !_pathShown)
		{
			_pathShown = true;
			await SafeModuleInvokeVoidAsync(PopupModulePath, "open", PopupKey, _pathPanel, _pathAnchor, null, new Dictionary<string, object?>
			{
				["placement"] = "bottom-start",
				["width"] = "min"
			});
			await SafeModuleInvokeVoidAsync(ModulePath, "openPath", _root);
			_pathOpening = false;
		}
		else if (!_pathOpen && _pathShown)
		{
			_pathShown = false;
			await SafeModuleInvokeVoidAsync(PopupModulePath, "close", PopupKey);
		}

		if (_focusPathEdit && !_pathOpen)
		{
			_focusPathEdit = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "focusPathEdit", _root);
		}
	}

	private sealed record ParsedPath(string? Folder, string Partial, string? Waiting, string Head);

	private sealed record PathSuggestion(string Folder, string Name, string? Parent, string Path);
}
