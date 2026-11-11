using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Popover;
using Moka.Red.Feedback.SlashMenu;
using Moka.Red.Primitives.Emoji;

namespace Moka.Red.Feedback.EmojiTypeahead;

/// <summary>
///     A ":" typeahead for emoji in a text field: type a colon and two letters and a <see cref="MokaSlashMenu" /> opens
///     under the field with the matches; Enter or Tab puts the emoji in, Escape closes it. A whole code such as
///     <c>:tada:</c> turns into 🎉 as its closing colon is typed. Wrap the field (a MokaTextArea, a MokaTextField or a
///     plain textarea) in it, or point <see cref="For" /> at one.
/// </summary>
/// <remarks>
///     The field keeps focus. A collocated module watches it, searches the emoji data in the browser and calls .NET only
///     when the matches change and for the keys the menu takes.
/// </remarks>
public partial class MokaEmojiTypeahead : MokaComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Feedback/EmojiTypeahead/MokaEmojiTypeahead.razor.js";

	private (string? Listbox, string? Active) _described;
	private DotNetObjectReference<MokaEmojiTypeahead>? _dotNetRef;
	private List<MokaSlashMenuItem> _items = [];
	private MokaSlashMenu? _menu;
	private bool _open;
	private string? _query;
	private ElementReference _root;
	private string? _watched;

	/// <summary>The field to watch, or anything holding it. The typeahead looks for the first textarea or text input inside.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     The field to watch when it isn't inside <see cref="ChildContent" />: the textarea or input itself. The menu then
	///     opens against it.
	/// </summary>
	[Parameter]
	public ElementReference? For { get; set; }

	/// <summary>
	///     The data file, the newest emoji version, the skin tone and whether keywords match. Shape and navigation don't
	///     apply. Null is <see cref="MokaEmojiOptions.Default" />.
	/// </summary>
	[Parameter]
	public MokaEmojiOptions? Options { get; set; }

	/// <summary>How many matches the menu shows. Default 8.</summary>
	[Parameter]
	public int MaxResults { get; set; } = 8;

	/// <summary>How many letters after the colon open the menu. Default 2, so "12:30" never does.</summary>
	[Parameter]
	public int MinLength { get; set; } = 2;

	/// <summary>Whether a whole code such as <c>:tada:</c> turns into its emoji as the closing colon is typed. Default true.</summary>
	[Parameter]
	public bool ConvertShortcodes { get; set; } = true;

	/// <summary>The menu's header. Null or empty hides it. Default "Emoji".</summary>
	[Parameter]
	public string? Header { get; set; } = "Emoji";

	/// <summary>Whether the menu says what the keys do. Default true.</summary>
	[Parameter]
	public bool ShowHints { get; set; } = true;

	/// <summary>What the menu says when nothing matches. <c>{query}</c> is what was typed. Default "No emoji for :{query}".</summary>
	[Parameter]
	public string? EmptyText { get; set; } = "No emoji for :{query}";

	/// <summary>The line under <see cref="EmptyText" />. Default "Esc closes this and leaves the colon as text."</summary>
	[Parameter]
	public string? EmptyHint { get; set; } = "Esc closes this and leaves the colon as text.";

	/// <summary>Where the menu opens against the field. Default below its start.</summary>
	[Parameter]
	public MokaPopoverPosition Placement { get; set; } = MokaPopoverPosition.BottomStart;

	/// <summary>Whether the typeahead is off: the field works as if it weren't there.</summary>
	[Parameter]
	public bool Disabled { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-emoji-typeahead";

	private ElementReference MenuAnchor => For is { Id.Length: > 0 } target ? target : _root;

	private MokaEmojiOptions Opt => Options ?? MokaEmojiOptions.Default;

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		string watched = string.Join('\u001f', For?.Id ?? "", Opt.DataSource ?? "",
			Opt.MaxVersion?.ToString(CultureInfo.InvariantCulture) ?? "", ((int)Opt.SkinTone).ToString(CultureInfo.InvariantCulture),
			Opt.SearchKeywords ? "1" : "0", MaxResults.ToString(CultureInfo.InvariantCulture),
			MinLength.ToString(CultureInfo.InvariantCulture), ConvertShortcodes ? "1" : "0", Disabled ? "1" : "0");
		if (watched != _watched)
		{
			_watched = watched;
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await SafeModuleInvokeVoidAsync(ModulePath, "watch", _root, For is { Id.Length: > 0 } ? For : null, _dotNetRef,
				new Dictionary<string, object?>
				{
					["dataSource"] = Opt.DataSource,
					["maxVersion"] = Opt.MaxVersion,
					["tone"] = (int)Opt.SkinTone,
					["keywords"] = Opt.SearchKeywords,
					["max"] = Math.Max(1, MaxResults),
					["minLength"] = Math.Max(1, MinLength),
					["convert"] = ConvertShortcodes,
					["disabled"] = Disabled
				});
		}

		(string? Listbox, string? Active) described = _open && _menu is not null ? (_menu.ListboxId, _menu.ActiveOptionId) : (null, null);
		if (described != _described)
		{
			_described = described;
			await SafeModuleInvokeVoidAsync(ModulePath, "describe", _root, described.Listbox, described.Active);
		}
	}

	/// <summary>Called from the module with what was typed after the colon and the matches, best first.</summary>
	/// <param name="query">What was typed after the colon.</param>
	/// <param name="rows">Each match as its emoji, name, shortcode and keywords.</param>
	[JSInvokable]
	public void OnQuery(string query, string[][] rows)
	{
		if (Disabled)
		{
			return;
		}

		_query = query;
		_items = [.. (rows ?? []).Where(static row => row is { Length: >= 3 }).Select(static row => new MokaSlashMenuItem
		{
			Title = $":{row[2]}:",
			Description = row[1],
			Glyph = row[0],
			Keywords = string.Join(' ', row.Skip(1)),
			Value = row[0]
		})];
		_open = true;
		StateHasChanged();
	}

	/// <summary>Called from the module when the colon is gone, the caret moved away, or Escape closed the menu.</summary>
	[JSInvokable]
	public void OnClosed()
	{
		if (!_open)
		{
			return;
		}

		_open = false;
		StateHasChanged();
	}

	/// <summary>Called from the module with a key the open menu takes: an arrow, Enter or Tab.</summary>
	/// <param name="key">The key.</param>
	[JSInvokable]
	public Task OnKey(string key)
	{
		RunDetached(async () =>
		{
			if (_menu is null || !_open)
			{
				return;
			}

			await _menu.HandleKeyAsync(new KeyboardEventArgs { Key = key == "Tab" ? "Enter" : key });
			StateHasChanged();
		});
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_watched is not null)
		{
			_watched = null;
			await SafeModuleInvokeVoidAsync(ModulePath, "unwatch", _root);
		}

		_dotNetRef?.Dispose();
		_dotNetRef = null;
		await base.DisposeAsyncCore();
	}

	private async Task SelectAsync(MokaSlashMenuItem item)
	{
		if (item.Value is string glyph)
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "insert", _root, glyph);
		}
	}

	private async Task MenuOpenChanged(bool open)
	{
		if (open || !_open)
		{
			return;
		}

		_open = false;
		await SafeModuleInvokeVoidAsync(ModulePath, "closed", _root);
	}
}
