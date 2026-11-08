using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.AppShell;

/// <summary>
///     Who's signed in, as a card that opens a menu: above it in the side menu and the drawer, beside it on the rail.
///     Arrows, Home and End move through the menu, and Escape closes it and goes back to the card. The open menu sits
///     in the browser's top layer, so the side menu's overflow doesn't cut it off and nothing on the page covers it.
/// </summary>
public partial class MokaAccountCard : MokaVisualComponentBase
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";
	private const string PopupModule = "./_content/Moka.Red.Core/moka-popup.js";

	// Above the card the menu is as wide as the card. Beside the rail its stylesheet gives it a width.
	private static readonly Dictionary<string, object?> AboveCard = new() { ["placement"] = "top", ["width"] = "match" };
	private static readonly Dictionary<string, object?> BesideRail = new() { ["placement"] = "right-end" };

	// Left to the browser these scroll the side menu, and so does Space from a link.
	private static readonly Dictionary<string, object?>[] KeyRules =
	[
		new()
		{
			["selector"] = "[role^=\"menuitem\"]",
			["keys"] = new[] { "ArrowDown", "ArrowUp", "Home", "End" },
			["unlessModified"] = true
		},
		new()
		{
			["selector"] = "a[role=\"menuitem\"]",
			["keys"] = new[] { " " },
			["unlessModified"] = true
		}
	];

	private readonly Dictionary<int, ElementReference> _itemRefs = [];
	private readonly string _menuId = $"moka-account-card-{Guid.NewGuid():N}";

	private ElementReference _card;
	private bool _focusFirst;
	private int _focused;
	private bool _keysBound;
	private bool _lifted;
	private ElementReference _menu;
	private bool _open;
	private ElementReference _root;
	private ElementReference _scrim;

	/// <summary>The person's name.</summary>
	[Parameter]
	public string Name { get; set; } = string.Empty;

	/// <summary>A line under the name, such as their role. Hidden on the rail.</summary>
	[Parameter]
	public string? Detail { get; set; }

	/// <summary>The letters in the avatar. Null takes them from <see cref="Name" />.</summary>
	[Parameter]
	public string? Initials { get; set; }

	/// <summary>A picture for the avatar, instead of the letters.</summary>
	[Parameter]
	public string? AvatarSrc { get; set; }

	/// <summary>The line over the rows, such as "BC, owner". Null draws none.</summary>
	[Parameter]
	public string? Heading { get; set; }

	/// <summary>The rows of the menu, in order. Use <see cref="MokaAccountItem.Divider" /> between groups.</summary>
	[Parameter]
	public IReadOnlyList<MokaAccountItem> Items { get; set; } = [];

	/// <summary>Raised when the person picks a row, a link or not.</summary>
	[Parameter]
	public EventCallback<MokaAccountItem> OnItemClick { get; set; }

	/// <summary>What the card does, said to screen readers after the name.</summary>
	[Parameter]
	public string MenuLabel { get; set; } = "Account menu";

	[CascadingParameter]
	private MokaAppShell? Shell { get; set; }

	[Inject]
	private NavigationManager Navigation { get; set; } = default!;

	/// <inheritdoc />
	protected override string RootClass => "moka-account-card";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle(Style)
		.Build();

	private string CardClass => new CssBuilder("moka-account-card__card")
		.AddClass("moka-account-card__card--open", _open)
		.AddClass("moka-account-card__card--current", Current)
		.Build();

	// One of its pages is showing, so the card is where you are, as a menu's current page is.
	private bool Current => Items.Any(item => item.Active && !item.IsDivider);

	// The card is the box the component draws, so padding and corners go there.
	private string? CardStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	// The rail shows only the avatar, so the name can't come from the text on screen.
	private string CardLabel => string.IsNullOrEmpty(Detail) ? $"{Name}, {MenuLabel}" : $"{Name}, {Detail}, {MenuLabel}";

	private string ResolvedInitials => Initials ?? InitialsOf(Name);

	private EventCallback<KeyboardEventArgs> KeyDownHandler => _open
		? EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync)
		: default;

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();
		Navigation.LocationChanged += HandleLocationChanged;
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// The menu and the scrim go to the top layer before a row takes focus. Rail or not is only known in the browser,
		// since it follows the window's width.
		if (_open != _lifted)
		{
			_lifted = _open;
			if (_open)
			{
				bool beside = Shell is not null && await Shell.ShowsRailAsync();
				await SafeModuleInvokeVoidAsync(PopupModule, "open", _menuId, _menu, _root, _scrim, beside ? BesideRail : AboveCard);
			}
			else
			{
				await SafeModuleInvokeVoidAsync(PopupModule, "close", _menuId);
			}
		}

		if (_focusFirst)
		{
			_focusFirst = false;
			if (!_keysBound)
			{
				_keysBound = true;
				await SafeModuleInvokeVoidAsync(KeysModule, "preventKeys", _root, KeyRules);
			}

			await FocusRowAsync(0);
		}

		await base.OnAfterRenderAsync(firstRender);
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		Navigation.LocationChanged -= HandleLocationChanged;
		if (_lifted)
		{
			_lifted = false;
			await SafeModuleInvokeVoidAsync(PopupModule, "close", _menuId);
		}

		await base.DisposeAsyncCore();
	}

	private static string InitialsOf(string name)
	{
		string[] words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		return words.Length switch
		{
			0 => string.Empty,
			1 => words[0][..Math.Min(2, words[0].Length)].ToUpperInvariant(),
			_ => $"{char.ToUpperInvariant(words[0][0])}{char.ToUpperInvariant(words[1][0])}"
		};
	}

	private static string ItemClass(MokaAccountItem item) => new CssBuilder("moka-account-card__item")
		.AddClass("moka-account-card__item--active", item.Active)
		.AddClass("moka-account-card__item--danger", item.Danger)
		.Build();

	// The rows the arrow keys stop on, as positions in Items.
	private List<int> Rows()
	{
		List<int> rows = [];
		for (int i = 0; i < Items.Count; i++)
		{
			if (!Items[i].IsDivider && !Items[i].Disabled)
			{
				rows.Add(i);
			}
		}

		return rows;
	}

	private async Task FocusRowAsync(int row)
	{
		List<int> rows = Rows();
		if (rows.Count == 0)
		{
			return;
		}

		_focused = row;
		if (_itemRefs.TryGetValue(rows[row], out ElementReference target))
		{
			await SafeFocusAsync(target);
		}
	}

	private async Task ToggleAsync()
	{
		if (_open)
		{
			_open = false;
			await SafeFocusAsync(_card);
			return;
		}

		_open = true;
		_focused = 0;

		// The rows don't exist until this render is done.
		_focusFirst = true;
	}

	private void CloseFromScrim() => _open = false;

	private async Task PickAsync(MokaAccountItem item)
	{
		if (item.Disabled)
		{
			return;
		}

		// A row that turns a setting on or off leaves the menu open, so the person sees it change.
		if (item.Checked is null)
		{
			_open = false;

			// First, so a dialog the app opens from this remembers the card as where to send focus back.
			if (UrlValues.SafeHref(item.Href) is null)
			{
				await SafeFocusAsync(_card);
			}

			if (Shell is not null)
			{
				await Shell.CloseDrawerAsync();
			}
		}

		await OnItemClick.InvokeAsync(item);
	}

	private async Task HandleKeyDownAsync(KeyboardEventArgs e)
	{
		if (e.Key == "Escape")
		{
			_open = false;
			await SafeFocusAsync(_card);
			return;
		}

		// Tab goes on from where the menu was, as it does out of any menu.
		if (e.Key == "Tab")
		{
			_open = false;
			return;
		}

		if (e.AltKey || e.CtrlKey || e.MetaKey)
		{
			return;
		}

		List<int> rows = Rows();
		if (rows.Count == 0)
		{
			return;
		}

		// A button clicks itself on Space. A link doesn't.
		if (e.Key == " ")
		{
			MokaAccountItem focused = Items[rows[_focused]];
			if (UrlValues.SafeHref(focused.Href) is { } href)
			{
				Navigation.NavigateTo(href);
				await PickAsync(focused);
			}

			return;
		}

		int next = e.Key switch
		{
			"ArrowDown" => (_focused + 1) % rows.Count,
			"ArrowUp" => (_focused - 1 + rows.Count) % rows.Count,
			"Home" => 0,
			"End" => rows.Count - 1,
			_ => -1
		};

		if (next >= 0)
		{
			await FocusRowAsync(next);
		}
	}

	// Any way of reaching a page closes the menu: the command palette opens over it without touching it.
	private void HandleLocationChanged(object? sender, LocationChangedEventArgs e) => RunDetached(() =>
	{
		if (_open)
		{
			_open = false;
			StateHasChanged();
		}
	});
}
