using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.SettingsForm;

/// <summary>
///     A section of settings with a real save flow: what's set now, an Unsaved tag and a sticky footer while edited, a
///     question before leaving unsaved edits, and deep links that land on a field. It keeps none of the app's save logic:
///     the app says whether it's <see cref="Dirty" />, hears <see cref="OnSave" /> and <see cref="OnReset" />, and hands
///     back any <see cref="Refusal" />.
/// </summary>
public partial class MokaSettingsForm : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Forms/SettingsForm/MokaSettingsForm.razor.js";

	// What a link points at may only render once the page has loaded its data, so a miss is tried again on later renders.
	private const int MaxRevealAttempts = 20;

	private readonly HashSet<MokaSettingsGroup> _advancedGroups = [];

	private string? _advancedShownFor;
	private int _attempts;
	private string? _browserFragment;
	private DotNetObjectReference<MokaSettingsForm>? _dotNetRef;
	private ElementReference _footer;
	private IJSObjectReference? _fragmentWatch;
	private bool _lastShowAdvancedParameter;
	private bool _leaving;
	private string? _leaveTo;
	private bool _promptWanted;
	private string? _revealed;
	private ElementReference _root;
	private bool _showAdvanced;

	[Inject]
	private NavigationManager Navigation { get; set; } = default!;

	[CascadingParameter]
	private MokaSettingsLayout? Layout { get; set; }

	/// <summary>The section's name, for its heading and for screen readers.</summary>
	[Parameter]
	[EditorRequired]
	public string Title { get; set; } = string.Empty;

	/// <summary>What the section is for. <c>MokaTheme.WithDescriptions(false)</c> hides it.</summary>
	[Parameter]
	public string? Lead { get; set; }

	/// <summary>Beside the title, at the right: what just happened, like "Saved to your account".</summary>
	[Parameter]
	public RenderFragment? Status { get; set; }

	/// <summary>What's set now, in a line. It's state, so it always shows.</summary>
	[Parameter]
	public string? Now { get; set; }

	/// <summary>Instead of <see cref="Now" />, for a line with bold parts.</summary>
	[Parameter]
	public RenderFragment? NowContent { get; set; }

	/// <summary>The word in front of the "Now" line.</summary>
	[Parameter]
	public string NowLabel { get; set; } = "Now:";

	/// <summary>The groups and fields.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Whether there are edits nobody saved. It shows the Unsaved tag and the footer, and arms the question before leaving.</summary>
	[Parameter]
	public bool Dirty { get; set; }

	/// <summary>Whether what's typed can be saved. Off, Save is disabled.</summary>
	[Parameter]
	public bool CanSave { get; set; } = true;

	/// <summary>Whether a save is under way. Both footer buttons are disabled, and Save says so.</summary>
	[Parameter]
	public bool Saving { get; set; }

	/// <summary>Why the last save was refused, shown under the fields.</summary>
	[Parameter]
	public string? Refusal { get; set; }

	/// <summary>Raised by Save.</summary>
	[Parameter]
	public EventCallback OnSave { get; set; }

	/// <summary>Raised by the button that puts the edits back. Without a listener there is no such button.</summary>
	[Parameter]
	public EventCallback OnReset { get; set; }

	/// <summary>Shows "Show advanced" even with no <see cref="MokaSettingsGroup" /> marked <c>Advanced</c>, for an app that hides single fields itself.</summary>
	[Parameter]
	public bool HasAdvanced { get; set; }

	/// <summary>Whether advanced settings are showing.</summary>
	[Parameter]
	public bool ShowAdvanced { get; set; }

	/// <summary>Raised when the person, or a deep link to an advanced setting, changes <see cref="ShowAdvanced" />.</summary>
	[Parameter]
	public EventCallback<bool> ShowAdvancedChanged { get; set; }

	/// <summary>The settings a deep link can land on, for the ones inside an advanced part. Defaults to the layout's.</summary>
	[Parameter]
	public IReadOnlyList<MokaSettingsEntry>? Entries { get; set; }

	/// <summary>The tag's text while edited.</summary>
	[Parameter]
	public string UnsavedText { get; set; } = "Unsaved";

	/// <summary>The footer's sentence while edited, like "2 changes not saved yet".</summary>
	[Parameter]
	public string DirtyText { get; set; } = "Changes not saved yet";

	/// <summary>The text of the button that puts the edits back.</summary>
	[Parameter]
	public string ResetText { get; set; } = "Put back";

	/// <summary>The text of the save button.</summary>
	[Parameter]
	public string SaveText { get; set; } = "Save";

	/// <summary>The text of the save button during a save.</summary>
	[Parameter]
	public string SavingText { get; set; } = "Saving";

	/// <summary>The toggle's text while advanced settings are hidden.</summary>
	[Parameter]
	public string ShowAdvancedText { get; set; } = "Show advanced";

	/// <summary>The toggle's text while advanced settings show.</summary>
	[Parameter]
	public string HideAdvancedText { get; set; } = "Hide advanced";

	/// <summary>What the footer asks before leaving. Left out, it names the section.</summary>
	[Parameter]
	public string? LeavePrompt { get; set; }

	/// <summary>The text of the button that stays.</summary>
	[Parameter]
	public string StayText { get; set; } = "Stay";

	/// <summary>The text of the button that leaves anyway.</summary>
	[Parameter]
	public string LeaveText { get; set; } = "Leave without saving";

	/// <inheritdoc />
	protected override string RootClass => "moka-settings-form";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	// The body draws the padding, so the footer can run edge to edge.
	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle(Style)
		.Build();

	private string? BodyStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.Build();

	private string ResolvedLeavePrompt => LeavePrompt ?? $"Leave without saving your edits to {Title}?";

	internal bool AdvancedShowing => _showAdvanced;

	/// <summary>Called from JS when a link to another part of this page was followed, which Blazor doesn't report.</summary>
	/// <param name="fragment">The address's fragment, with or without its <c>#</c>.</param>
	[JSInvokable]
	public void OnFragmentFollowed(string? fragment)
	{
		_browserFragment = fragment ?? string.Empty;
		Arrive();
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();
		Navigation.LocationChanged += HandleLocationChanged;
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (ShowAdvanced != _lastShowAdvancedParameter)
		{
			_lastShowAdvancedParameter = ShowAdvanced;
			SetAdvanced(ShowAdvanced);
		}

		if (!Dirty)
		{
			_leaveTo = null;
		}

		Layout?.ReportDirty(Dirty);
		ShowAdvancedForFragment();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (firstRender)
		{
			_dotNetRef = DotNetObjectReference.Create(this);
			_fragmentWatch = await SafeModuleInvokeAsync<IJSObjectReference>(ModulePath, "watchFragment", _dotNetRef);
		}

		if (_promptWanted)
		{
			_promptWanted = false;
			await SafeModuleInvokeVoidAsync(ModulePath, "showPrompt", _footer);
		}

		string? target = Fragment();
		if (target is null || target == _revealed || _attempts >= MaxRevealAttempts)
		{
			return;
		}

		_attempts++;
		if (await SafeModuleInvokeAsync<bool>(ModulePath, "reveal", _root, target))
		{
			_revealed = target;
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		Navigation.LocationChanged -= HandleLocationChanged;
		Layout?.ReportDirty(false);
		await _fragmentWatch.SafeInvokeVoidAsync("dispose");
		await _fragmentWatch.SafeDisposeAsync();
		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}

	// A group is told apart by its presence: it stays mounted, drawing nothing, while advanced settings are hidden, so a
	// deep link can find it and the toggle knows it's wanted.
	internal void ReportGroup(MokaSettingsGroup group, bool advanced)
	{
		bool changed = advanced ? _advancedGroups.Add(group) : _advancedGroups.Remove(group);
		if (!changed)
		{
			return;
		}

		ShowAdvancedForFragment();
		RunDetached(StateHasChanged);
	}

	private string? Fragment() =>
		(_browserFragment ?? new Uri(Navigation.Uri).Fragment).TrimStart('#') is { Length: > 0 } fragment
			? Uri.UnescapeDataString(fragment)
			: null;

	// Once per arrival at a link, so the person can turn advanced settings off again while it's still in the address.
	private void ShowAdvancedForFragment()
	{
		if (Fragment() is not { } target || target == _advancedShownFor)
		{
			return;
		}

		IReadOnlyList<MokaSettingsEntry>? entries = Entries ?? Layout?.Entries;
		bool advanced = _advancedGroups.Any(group => group.Id == target) ||
		                (entries?.Any(entry => entry.Advanced && entry.Anchor == target) ?? false);
		if (!advanced)
		{
			return;
		}

		_advancedShownFor = target;
		if (!_showAdvanced)
		{
			SetAdvanced(true);
			RunDetached(() => ShowAdvancedChanged.InvokeAsync(true));
		}
	}

	// The cascade is fixed and the form's own render reaches a group only if it has content, so each one is told.
	private void SetAdvanced(bool showing)
	{
		_showAdvanced = showing;
		foreach (MokaSettingsGroup group in _advancedGroups.ToArray())
		{
			group.Refresh();
		}
	}

	private async Task ToggleAdvancedAsync()
	{
		SetAdvanced(!_showAdvanced);
		await ShowAdvancedChanged.InvokeAsync(_showAdvanced);
	}

	private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
	{
		_browserFragment = null;
		_leaving = false;
		Arrive();
	}

	// A link to another setting on this page changes only the fragment, which doesn't render the page again.
	private void Arrive() =>
		RunDetached(() =>
		{
			_attempts = 0;
			_revealed = null;
			_advancedShownFor = null;
			ShowAdvancedForFragment();
			StateHasChanged();
		});

	private void HandleBeforeLeaving(LocationChangingContext context)
	{
		if (_leaving || SamePage(context.TargetLocation))
		{
			return;
		}

		_leaveTo = context.TargetLocation;
		_promptWanted = true;
		context.PreventNavigation();
	}

	// A link to another field of this section isn't leaving it.
	private bool SamePage(string target)
	{
		static string Page(string uri) => uri.Split('#')[0].Split('?')[0].TrimEnd('/');

		return string.Equals(Page(Navigation.ToAbsoluteUri(target).ToString()), Page(Navigation.Uri),
			StringComparison.Ordinal);
	}

	private void Stay() => _leaveTo = null;

	private void Leave()
	{
		if (_leaveTo is not { } target)
		{
			return;
		}

		_leaving = true;
		_leaveTo = null;
		Layout?.ReportDirty(false);
		Navigation.NavigateTo(target);
	}
}
