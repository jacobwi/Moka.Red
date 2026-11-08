using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;

namespace Moka.Red.Forms.InlineEdit;

/// <summary>
///     Text that becomes a field. Enter saves, Escape cancels and clicking away saves. A pencil appears on hover and
///     keyboard focus, and stays on a touch screen. An error, from <see cref="Validate" /> or a failed
///     <see cref="Save" />, shows under the field and keeps it open.
/// </summary>
public partial class MokaInlineEdit
{
	private const string ModulePath = "./_content/Moka.Red.Forms/InlineEdit/MokaInlineEdit.razor.js";

	private readonly string _generatedId = $"moka-inline-edit-{Guid.NewGuid():N}";

	private ElementReference _display;
	private string _draft = "";

	// What the field opened with, and all the markup ever writes to it. A render carrying the text typed so far reaches
	// the browser after the next key has landed, and writing it back dropped that key.
	private string _startText = "";
	private bool _editing;
	private string? _error;
	private bool _focusDisplay;

	// A render already on its way to the browser can finish after the state changed, and its after-render would then
	// reach for an element the next render hasn't drawn yet. These say which render the wait is for.
	private int _rendersBuilt;
	private int _rendersFinished;
	private int _elementReadyAt;
	private ElementReference _input;

	// The last value the parent passed, which is not the same as whether the field is open.
	private bool _lastEditing;
	private bool _openPending;
	private ElementReference _root;
	private bool _saving;

	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	/// <summary>Whether the field is open. Bind it to start editing from elsewhere, such as a key or a menu.</summary>
	[Parameter]
	public bool Editing { get; set; }

	/// <summary>Raised when the field opens or closes because of something the person did.</summary>
	[Parameter]
	public EventCallback<bool> EditingChanged { get; set; }

	/// <summary>What starts editing. Default <see cref="MokaInlineEditTrigger.Click" />.</summary>
	[Parameter]
	public MokaInlineEditTrigger Trigger { get; set; }

	/// <summary>
	///     Checks the text before saving, and returns an error message that blocks the save, or null when the text is
	///     fine.
	/// </summary>
	[Parameter]
	public Func<string, string?>? Validate { get; set; }

	/// <summary>
	///     Saves the text and, while it runs, keeps the field open and read-only. An exception shows its message as the
	///     error and keeps the field open. Without it the new text just goes to <c>ValueChanged</c>.
	/// </summary>
	[Parameter]
	public Func<string, Task>? Save { get; set; }

	/// <summary>Whether the text may be saved blank. Default false.</summary>
	[Parameter]
	public bool AllowBlank { get; set; }

	/// <summary>Whether to save text that didn't change, for a name offered as a suggestion. Default false.</summary>
	[Parameter]
	public bool SaveUnchanged { get; set; }

	/// <summary>Whether to remove spaces from both ends of the text before saving. Default true.</summary>
	[Parameter]
	public bool Trim { get; set; } = true;

	/// <summary>What is selected when the field opens. Default <see cref="MokaInlineEditSelection.All" />.</summary>
	[Parameter]
	public MokaInlineEditSelection Selection { get; set; }

	/// <summary>The field's accessible name. Default "Text".</summary>
	[Parameter]
	public string Label { get; set; } = "Text";

	/// <summary>The accessible name of the text as a button. Default "Edit" and the text.</summary>
	[Parameter]
	public string? ButtonLabel { get; set; }

	/// <summary>The message shown when blank text is blocked. Default "This can't be blank".</summary>
	[Parameter]
	public string BlankText { get; set; } = "This can't be blank";

	/// <summary>An error from outside. One from <see cref="Validate" /> or <see cref="Save" /> shows first.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Draws the text and the field in the monospace font, and turns spell check off.</summary>
	[Parameter]
	public bool Mono { get; set; }

	/// <summary>Replaces how the text is drawn while it isn't being edited. It receives the text.</summary>
	[Parameter]
	public RenderFragment<string>? DisplayTemplate { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-inline-edit";

	/// <inheritdoc />
	protected override string ComponentCssClass => new CssBuilder(RootClass)
		.AddClass($"moka-inline-edit--{SizeToKebab(Size)}")
		.AddClass("moka-inline-edit--editing moka-fill-width", _editing)
		.AddClass("moka-inline-edit--mono", Mono)
		.AddClass("moka-inline-edit--error", ShownError is not null)
		.AddClass(CssClass)
		.AddClass(Class)
		.Build();

	private string? RootStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("font-size", SizeValue is null ? null : CssValues.WithPixels(SizeValue))
		.AddStyle(Style)
		.Build();

	private string? BoxStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private string InputCssClass => new CssBuilder("moka-inline-edit__input")
		.AddClass("moka-inline-edit__input--saving", _saving)
		.Build();

	private bool CanOpen => !Disabled && !ReadOnly;

	private bool CanEdit => CanOpen && Trigger != MokaInlineEditTrigger.Manual;

	private string ButtonName => ButtonLabel ?? (string.IsNullOrEmpty(CurrentValue) ? "Edit" : $"Edit {CurrentValue}");

	private string? ShownError => _error ?? (string.IsNullOrEmpty(ErrorText) ? ValidationErrorText : ErrorText);

	private string ErrorId => $"{InputId}-error";

	private string? ErrorDescribedBy => ShownError is null ? null : MokaAttributes.DescribedBy(AdditionalAttributes, ErrorId);

	private IReadOnlyDictionary<string, object>? InputAttributes =>
		MokaAttributes.Without(MokaAttributes.Without(AdditionalAttributes, "aria-label"), "aria-describedby");

	/// <inheritdoc />
	protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
	{
		result = value ?? string.Empty;
		validationErrorMessage = string.Empty;
		return true;
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (Editing == _lastEditing)
		{
			return;
		}

		_lastEditing = Editing;
		if (Editing && !_editing && CanOpen)
		{
			Open();
		}
		else if (!Editing && _editing)
		{
			_editing = false;
			_error = null;
		}
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		_rendersFinished++;
		if (_rendersFinished < _elementReadyAt)
		{
			return;
		}

		if (_openPending)
		{
			_openPending = false;
			(int start, int end) = SelectionRange(_draft);
			await SafeModuleInvokeVoidAsync(ModulePath, "open", _root, _input, start, end);
		}
		else if (_focusDisplay)
		{
			_focusDisplay = false;
			if (CanEdit)
			{
				await FocusDisplayAsync();
			}
		}
	}

	private async ValueTask FocusDisplayAsync()
	{
		try
		{
			await _display.FocusAsync();
		}
		catch (Exception ex) when (MokaJsInterop.IsCircuitGone(ex))
		{
		}
		catch (JSException)
		{
			// The button left the page before the call reached the browser.
		}
	}

	private (int Start, int End) SelectionRange(string text) => Selection switch
	{
		MokaInlineEditSelection.End => (text.Length, text.Length),
		MokaInlineEditSelection.Stem => (0, StemLength(text)),
		_ => (0, text.Length)
	};

	private static int StemLength(string text)
	{
		int dot = text.LastIndexOf('.');
		return dot > 0 ? dot : text.Length;
	}

	private void Open()
	{
		_draft = CurrentValue ?? "";
		_startText = _draft;
		_error = null;
		_editing = true;
		_openPending = true;
		_elementReadyAt = _rendersBuilt + 1;
	}

	private async Task BeginAsync()
	{
		if (_editing || !CanOpen)
		{
			return;
		}

		Open();
		await EditingChanged.InvokeAsync(true);
	}

	// A click from the keyboard reports no clicks, so Enter and Space start a double-click field too.
	private Task HandleDisplayClick(MouseEventArgs e) =>
		Trigger == MokaInlineEditTrigger.Click || e.Detail == 0 ? BeginAsync() : Task.CompletedTask;

	private Task HandleDisplayDoubleClick() =>
		Trigger == MokaInlineEditTrigger.DoubleClick ? BeginAsync() : Task.CompletedTask;

	private void HandleInput(ChangeEventArgs e)
	{
		_draft = e.Value?.ToString() ?? "";
		_error = null;
	}

	private Task HandleKeyDown(KeyboardEventArgs e)
	{
		// Enter also confirms what an input method is composing, which must not save.
		if (e.IsComposing || _saving)
		{
			return Task.CompletedTask;
		}

		return e.Key switch
		{
			"Enter" => FinishAsync(fromKeyboard: true),
			"Escape" => CloseAsync(fromKeyboard: true),
			_ => Task.CompletedTask
		};
	}

	private async Task HandleInputBlurAsync()
	{
		await HandleBlurAsync();

		if (_editing && !_saving)
		{
			await FinishAsync(fromKeyboard: false);
		}
	}

	private async Task FinishAsync(bool fromKeyboard)
	{
		string text = Trim ? _draft.Trim() : _draft;
		if (!SaveUnchanged && string.Equals(text, CurrentValue ?? "", StringComparison.Ordinal))
		{
			await CloseAsync(fromKeyboard);
			return;
		}

		string? problem = ProblemWith(text);
		if (problem is not null)
		{
			_error = problem;
			return;
		}

		if (Save is not null && !await TrySaveAsync(text))
		{
			return;
		}

		CurrentValueAsString = text;
		await CloseAsync(fromKeyboard);
	}

	private string? ProblemWith(string text) =>
		!AllowBlank && string.IsNullOrWhiteSpace(text) ? BlankText : Validate?.Invoke(text);

	private async Task<bool> TrySaveAsync(string text)
	{
		_saving = true;
		_error = null;
		StateHasChanged();
		try
		{
			await Save!(text);
			return true;
		}
#pragma warning disable CA1031 // Whatever a save throws is what the person has to read, and the field stays open for another try.
		catch (Exception ex)
#pragma warning restore CA1031
		{
			_error = ex.Message;
			return false;
		}
		finally
		{
			_saving = false;
		}
	}

	private async Task CloseAsync(bool fromKeyboard)
	{
		_editing = false;
		_error = null;
		_focusDisplay = fromKeyboard;
		_elementReadyAt = _rendersBuilt + 1;
		await EditingChanged.InvokeAsync(false);
	}
}
