using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Moka.Red.Core.Base;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.NumericField;
using Moka.Red.Forms.SettingsForm;

namespace Moka.Red.Forms.SchemaForm;

/// <summary>
///     A settings form drawn from a JSON Schema, for settings an app doesn't know when it's built, such as a plugin's or
///     a connector's. Each property becomes a field in a <see cref="MokaSettingsForm" />, checked as it's edited and
///     before Save. Write-only properties are secrets: the form shows whether one is set and offers Change, Clear and
///     Keep, and never holds the saved value.
/// </summary>
/// <remarks>
///     The form edits a copy of <see cref="Value" /> and reports each change through <see cref="ValueChanged" />. It keeps
///     what was saved apart from it, so it knows what's unsaved: what it was first given, what a successful save stored,
///     and any value the parent passes while nothing is edited. To start over with new settings while edits are open,
///     give the form a new <c>@key</c>.
/// </remarks>
public partial class MokaSchemaForm : MokaVisualComponentBase
{
	private const string ModulePath = "./_content/Moka.Red.Forms/SchemaForm/MokaSchemaForm.razor.js";

	private readonly HashSet<string> _changed = new(StringComparer.Ordinal);

	// Holds what the number fields can't read ("1e", "abc") while they show it. The form's own checks go through each
	// field's ErrorText, since a field here has no member for a validator to name.
	private readonly EditContext _fieldContext = new(new object());

	private readonly string _generatedId = $"moka-schema-form-{Guid.NewGuid():N}";
	private readonly HashSet<string> _hiddenServerProblems = new(StringComparer.Ordinal);
	private readonly HashSet<string> _live = new(StringComparer.Ordinal);
	private readonly Dictionary<string, string> _localProblems = new(StringComparer.Ordinal);
	private readonly Dictionary<string, SecretEdit> _secretEdits = new(StringComparer.Ordinal);

	private IReadOnlyList<FieldSlot> _advanced = [];
	private bool _dirty;
	private JsonObject _edited = new();
	private string? _focusId;
	private JsonObject? _lastIncoming;
	private string? _lastRefusal;
	private HashSet<string>? _lastSetSecrets;
	private IReadOnlyList<FieldSlot> _main = [];
	private bool _refusalHidden;
	private JsonObject _saved = new();
	private bool _saving;
	private MokaFormSchema? _schema;
	private Dictionary<string, string> _serverProblems = new(StringComparer.Ordinal);
	private bool _serverOthersHidden;
	private HashSet<string> _setSecrets = new(StringComparer.Ordinal);
	private bool _showAdvanced;
	private bool _showSummary;
	private IReadOnlyList<FieldSlot> _slots = [];
	private bool _started;
	private bool _valuePassed;

	/// <summary>The form, read with <see cref="MokaFormSchema.Parse(string)" />. Without one the form says there's nothing to set.</summary>
	[Parameter]
	[EditorRequired]
	public MokaFormSchema? Schema { get; set; }

	/// <summary>
	///     The settings object. A setting it leaves out takes its <c>default</c>. Write-only properties in it are never
	///     shown and count as set. Properties the schema doesn't name are kept as they are.
	/// </summary>
	[Parameter]
	public JsonObject? Value { get; set; }

	/// <summary>
	///     Raised with a new copy of the settings each time one is edited, with defaults filled in and write-only
	///     settings left out. A parent that passes <see cref="Value" /> and listens owns it, as with the library's
	///     inputs: once its handler has run, the form shows the value the parent passes back, so a change it refuses
	///     is undone.
	/// </summary>
	[Parameter]
	public EventCallback<JsonObject> ValueChanged { get; set; }

	/// <summary>The write-only settings that hold a value where they're kept. The form shows them as set.</summary>
	[Parameter]
	public IReadOnlyCollection<string>? SetSecrets { get; set; }

	/// <summary>
	///     Problems from outside, such as the server refusing a save, by property name. Each shows at its field until the
	///     field is edited. A name the schema doesn't have shows under the fields with <see cref="Refusal" />.
	/// </summary>
	[Parameter]
	public IReadOnlyDictionary<string, string>? Problems { get; set; }

	/// <summary>Why the last save was refused, shown under the fields.</summary>
	[Parameter]
	public string? Refusal { get; set; }

	/// <summary>
	///     Raised by Save once every field fits, with the settings to store and what to do with each secret. The form
	///     shows Saving until the handler has run. The save counts as done when the handler leaves no
	///     <see cref="Refusal" /> and no <see cref="Problems" />: what was sent becomes what's saved. Without a listener
	///     Save just takes the edits as saved.
	/// </summary>
	[Parameter]
	public EventCallback<MokaSchemaFormSave> OnSave { get; set; }

	/// <summary>The form's title. Left out, the schema's <c>title</c>, else <see cref="MokaSchemaFormText.Title" />.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>What the settings are for. Left out, the schema's <c>description</c>.</summary>
	[Parameter]
	public string? Lead { get; set; }

	/// <summary>An icon in an accent tile before the title.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>Beside the title: what just happened, like "Saved".</summary>
	[Parameter]
	public RenderFragment? Status { get; set; }

	/// <summary>Controls at the end of the header, such as a button that tests a connection.</summary>
	[Parameter]
	public RenderFragment? HeaderActions { get; set; }

	/// <summary>How the form is drawn, as on <see cref="MokaSettingsForm" />. Plain by default.</summary>
	[Parameter]
	public MokaSettingsAppearance Appearance { get; set; } = MokaSettingsAppearance.Plain;

	/// <summary>The most columns of fields a group has. A column is never narrower than 16rem of the group's own width.</summary>
	[Parameter]
	public int Columns { get; set; } = 2;

	/// <summary>The words the form writes. Null takes <see cref="MokaSchemaFormText.Default" />.</summary>
	[Parameter]
	public MokaSchemaFormText? Text { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-schema-form";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	private MokaSchemaFormText Words => Text ?? MokaSchemaFormText.Default;

	private string ResolvedTitle => Title ?? _schema?.Title ?? Words.Title;

	private string? FormMessage
	{
		get
		{
			List<string> parts = [];
			if (_showSummary && ProblemCount() is > 0 and var count)
			{
				parts.Add(Words.CheckMarked(count));
			}

			if (!_refusalHidden && !string.IsNullOrEmpty(Refusal))
			{
				parts.Add(Refusal);
			}

			if (!_serverOthersHidden)
			{
				parts.AddRange(_serverProblems.Where(problem => _schema?.Field(problem.Key) is null).Select(problem => problem.Value));
			}

			return parts.Count == 0 ? null : string.Join(" ", parts);
		}
	}

	/// <inheritdoc />
	/// <remarks>Notes whether the parent passes <see cref="Value" />, which decides whether it owns the value.</remarks>
	public override Task SetParametersAsync(ParameterView parameters)
	{
		_valuePassed = parameters.TryGetValue<JsonObject?>(nameof(Value), out _);
		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		bool newSchema = !_started || !SameSchema(_schema, Schema);
		if (newSchema)
		{
			_schema = Schema;
			BuildSlots();
		}

		TakeSecrets();
		TakeProblems(false);
		if (!string.Equals(Refusal, _lastRefusal, StringComparison.Ordinal))
		{
			_lastRefusal = Refusal;
			_refusalHidden = false;
		}

		JsonObject incoming = Prepare(Value);
		if (newSchema)
		{
			StartOver(incoming);
		}
		else if (!JsonNode.DeepEquals(incoming, _lastIncoming) && !JsonNode.DeepEquals(incoming, _edited))
		{
			Follow(incoming);
		}

		_lastIncoming = incoming;
		_started = true;
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (_focusId is { } id)
		{
			_focusId = null;
			await SafeModuleInvokeVoidAsync(ModulePath, "focusField", id);
		}
	}

	private static bool SameSchema(MokaFormSchema? shown, MokaFormSchema? given) =>
		shown is null ? given is null : shown.SameAs(given);

	private static JsonObject Clone(JsonObject value) => value.DeepClone().AsObject();

	private void BuildSlots()
	{
		_slots = _schema is null ? [] : [.. _schema.Fields.Select((field, index) => new FieldSlot(this, field, index))];
		_main = [.. _slots.Where(slot => !slot.Field.Advanced)];
		_advanced = [.. _slots.Where(slot => slot.Field.Advanced)];
	}

	// The form's view of a value: defaults filled in, a switch with no value off, and secrets left out.
	private JsonObject Prepare(JsonObject? value)
	{
		if (_schema is null)
		{
			return value is null ? new JsonObject() : Clone(value);
		}

		JsonObject prepared = _schema.WithDefaults(value);
		foreach (MokaSchemaField field in _schema.Fields)
		{
			if (field.WriteOnly)
			{
				prepared.Remove(field.Name);
			}
			else if (field.Kind == MokaSchemaFieldKind.Toggle && MokaSchemaJson.IsUnset(prepared[field.Name]))
			{
				prepared[field.Name] = false;
			}
		}

		return prepared;
	}

	private void StartOver(JsonObject saved)
	{
		_saved = saved;
		_edited = Clone(saved);
		_secretEdits.Clear();
		_localProblems.Clear();
		_live.Clear();
		_changed.Clear();
		_showSummary = false;
		NewTokens();
		UpdateDirty();
	}

	// A value the parent passes while nothing is edited is newly saved settings; while edits are open it's the parent
	// changing them.
	private void Follow(JsonObject incoming)
	{
		if (_dirty)
		{
			_edited = Clone(incoming);
			RecheckLive();
			UpdateDirty();
		}
		else
		{
			StartOver(incoming);
		}
	}

	private void TakeSecrets()
	{
		HashSet<string> given = new(SetSecrets ?? [], StringComparer.Ordinal);
		foreach (FieldSlot slot in _slots.Where(slot => slot.Field.WriteOnly))
		{
			if (!MokaSchemaJson.IsUnset(Value?[slot.Name]))
			{
				given.Add(slot.Name);
			}
		}

		if (_lastSetSecrets is null || !_lastSetSecrets.SetEquals(given))
		{
			_lastSetSecrets = given;
			_setSecrets = new HashSet<string>(given, StringComparer.Ordinal);
		}
	}

	// Problems from outside show until their field is edited. A new set shows in full, and so does whatever a save leaves.
	private void TakeProblems(bool afterSave)
	{
		Dictionary<string, string> given = Problems is null
			? new Dictionary<string, string>(StringComparer.Ordinal)
			: new Dictionary<string, string>(Problems, StringComparer.Ordinal);
		bool same = given.Count == _serverProblems.Count && given.All(problem =>
			_serverProblems.TryGetValue(problem.Key, out string? shown) && string.Equals(shown, problem.Value, StringComparison.Ordinal));
		if (same && !afterSave)
		{
			return;
		}

		_serverProblems = given;
		_hiddenServerProblems.Clear();
		_serverOthersHidden = false;
		if (_slots.Any(slot => slot.Field.Advanced && given.ContainsKey(slot.Name)))
		{
			_showAdvanced = true;
		}
	}

	private void HideServerProblems()
	{
		_hiddenServerProblems.UnionWith(_serverProblems.Keys);
		_serverOthersHidden = true;
	}

	private void NewTokens()
	{
		foreach (FieldSlot slot in _slots)
		{
			slot.Token = new object();
		}
	}

	private void UpdateDirty() =>
		_dirty = !Disabled && (!JsonNode.DeepEquals(_edited, _saved) || _secretEdits.Values.Any(edit => edit.IsChange));

	private string IdOf(FieldSlot slot) => $"{Id ?? _generatedId}-{slot.Index}";

	private string? ErrorTextOf(FieldSlot slot) =>
		_localProblems.GetValueOrDefault(slot.Name) ??
		(_hiddenServerProblems.Contains(slot.Name) ? null : _serverProblems.GetValueOrDefault(slot.Name));

	private bool HasProblem(FieldSlot slot) =>
		ErrorTextOf(slot) is not null ||
		(slot.WholeInput ?? (object?)slot.NumberInput) is { } input &&
		_fieldContext.GetValidationMessages(new FieldIdentifier(input, "Value")).Any();

	private int ProblemCount() => _slots.Count(HasProblem);

	private string TextOf(FieldSlot slot) =>
		_edited[slot.Name] is { } node && MokaSchemaJson.IsPrimitive(node) ? MokaSchemaJson.Display(node) : string.Empty;

	private long? WholeOf(FieldSlot slot) =>
		_edited[slot.Name] is { } node && slot.Field.TryReadNumber(node, out double number, out _) && MokaSchemaField.IsWhole(number)
			? (long)number
			: null;

	private double? NumberOf(FieldSlot slot) =>
		_edited[slot.Name] is { } node && slot.Field.TryReadNumber(node, out double number, out _) ? number : null;

	private bool ToggleOf(FieldSlot slot) => _edited[slot.Name]?.GetValueKind() == JsonValueKind.True;

	private MokaSchemaChoice? ChoiceOf(FieldSlot slot) => slot.Field.ChoiceFor(_edited[slot.Name]);

	private string ChoiceTextOf(FieldSlot slot) => ChoiceOf(slot)?.Label ?? TextOf(slot);

	private SecretEdit SecretOf(FieldSlot slot) => _secretEdits.GetValueOrDefault(slot.Name, SecretEdit.Keep);

	private Task SetAsync(FieldSlot slot, JsonNode? value)
	{
		if (value is null)
		{
			_edited.Remove(slot.Name);
		}
		else
		{
			_edited[slot.Name] = value;
		}

		Touch(slot, slot.Field.Kind is MokaSchemaFieldKind.Toggle or MokaSchemaFieldKind.Choice);
		return RaiseValueChangedAsync();
	}

	private Task SetNumberAsync(FieldSlot slot, double? number)
	{
		if (number is { } given && !double.IsFinite(given))
		{
			_localProblems[slot.Name] = Words.NotNumber(slot.Field.Title);
			return Task.CompletedTask;
		}

		return SetAsync(slot, number is { } value ? JsonValue.Create(value) : null);
	}

	// A switch or a choice is checked as soon as it changes. Text and numbers are checked once the field is left, and on
	// every change after that, so a half-typed value isn't called wrong.
	private void Touch(FieldSlot slot, bool checkNow)
	{
		_changed.Add(slot.Name);
		_hiddenServerProblems.Add(slot.Name);
		if (checkNow)
		{
			_live.Add(slot.Name);
		}

		if (_live.Contains(slot.Name))
		{
			Recheck(slot);
		}

		UpdateDirty();
	}

	private void Left(FieldSlot slot)
	{
		if (_changed.Contains(slot.Name) && _live.Add(slot.Name))
		{
			Recheck(slot);
		}
	}

	private void Recheck(FieldSlot slot)
	{
		if (LocalProblem(slot) is { } problem)
		{
			_localProblems[slot.Name] = problem;
		}
		else
		{
			_localProblems.Remove(slot.Name);
		}
	}

	private void RecheckLive()
	{
		foreach (FieldSlot slot in _slots.Where(slot => _live.Contains(slot.Name)))
		{
			Recheck(slot);
		}
	}

	private string? LocalProblem(FieldSlot slot)
	{
		MokaSchemaField field = slot.Field;
		if (field.ReadOnly || Disabled)
		{
			return null;
		}

		if (field.WriteOnly)
		{
			return SecretProblem(slot);
		}

		JsonNode? value = _edited[slot.Name];
		if (MokaSchemaJson.IsUnset(value))
		{
			return field.Required ? Words.Needed(field.Title) : null;
		}

		return field.Problem(value!, Words);
	}

	private string? SecretProblem(FieldSlot slot)
	{
		MokaSchemaField field = slot.Field;
		SecretEdit edit = SecretOf(slot);
		return edit.Mode switch
		{
			SecretMode.Change when edit.Text.Length > 0 => field.TextProblem(edit.Text, Words),
			SecretMode.Clear => field.Required ? Words.Needed(field.Title) : null,
			_ => field.Required && !_setSecrets.Contains(field.Name) ? Words.Needed(field.Title) : null
		};
	}

	private void EditSecret(FieldSlot slot, SecretEdit edit, bool focus)
	{
		if (edit.Mode == SecretMode.Keep)
		{
			_secretEdits.Remove(slot.Name);
		}
		else
		{
			_secretEdits[slot.Name] = edit;
		}

		if (focus)
		{
			_focusId = IdOf(slot);
		}

		Touch(slot, false);
	}

	private async Task RaiseValueChangedAsync()
	{
		if (!ValueChanged.HasDelegate)
		{
			return;
		}

		await ValueChanged.InvokeAsync(Clone(_edited));
		await AfterParentRendersAsync();
		if (!_valuePassed)
		{
			return;
		}

		// The parent owns the value: once its handler has run, what it passes back shows, so a refused edit is undone.
		JsonObject owned = Prepare(Value);
		_lastIncoming = owned;
		if (!JsonNode.DeepEquals(owned, _edited))
		{
			_edited = owned;
			RecheckLive();
			UpdateDirty();
		}
	}

	private async Task SaveAsync()
	{
		if (Disabled || _saving)
		{
			return;
		}

		foreach (FieldSlot slot in _slots)
		{
			_live.Add(slot.Name);
			Recheck(slot);
		}

		_showSummary = true;
		if (_slots.FirstOrDefault(HasProblem) is { } first)
		{
			Reveal(first);
			return;
		}

		_showSummary = false;
		JsonObject snapshot = Clone(_edited);
		Dictionary<string, SecretEdit> edits = new(_secretEdits, StringComparer.Ordinal);
		MokaSchemaFormSave save = new(Clone(snapshot), ChangedNames(snapshot), SecretChanges(edits));

		HideServerProblems();
		_refusalHidden = true;
		_saving = true;
		try
		{
			await OnSave.InvokeAsync(save);
			await AfterParentRendersAsync();
		}
		finally
		{
			_saving = false;
		}

		TakeProblems(true);
		_lastRefusal = Refusal;
		_refusalHidden = false;
		if (!string.IsNullOrEmpty(Refusal) || _serverProblems.Count > 0)
		{
			if (_slots.FirstOrDefault(slot => ErrorTextOf(slot) is not null) is { } refused)
			{
				Reveal(refused);
			}

			return;
		}

		Commit(snapshot, save.Secrets, edits);
	}

	// A handler that finishes at once, inside the click's render batch, has only queued its parent's render. Yielding lets
	// that batch apply, so the parameters read next are the ones the parent passed after it ran.
	private static async Task AfterParentRendersAsync() => await Task.Yield();

	private void Reveal(FieldSlot slot)
	{
		if (slot.Field.Advanced)
		{
			_showAdvanced = true;
		}

		_focusId = IdOf(slot);
	}

	private void Commit(JsonObject snapshot, IReadOnlyDictionary<string, MokaSecretChange> secrets,
		Dictionary<string, SecretEdit> edits)
	{
		_saved = snapshot;
		foreach ((string name, MokaSecretChange change) in secrets)
		{
			if (change.Edit == MokaSecretEdit.Unchanged)
			{
				continue;
			}

			if (change.Edit == MokaSecretEdit.Set)
			{
				_setSecrets.Add(name);
			}
			else
			{
				_setSecrets.Remove(name);
			}

			// An edit made while the save ran wasn't saved, so it stays.
			if (_secretEdits.TryGetValue(name, out SecretEdit now) && now == edits[name])
			{
				_secretEdits.Remove(name);
			}
		}

		_showSummary = false;
		UpdateDirty();
	}

	private List<string> ChangedNames(JsonObject snapshot) =>
	[
		.. snapshot.Select(entry => entry.Key)
			.Union(_saved.Select(entry => entry.Key), StringComparer.Ordinal)
			.Where(name => !JsonNode.DeepEquals(snapshot[name], _saved[name]))
	];

	private Dictionary<string, MokaSecretChange> SecretChanges(Dictionary<string, SecretEdit> edits)
	{
		Dictionary<string, MokaSecretChange> changes = new(StringComparer.Ordinal);
		foreach (FieldSlot slot in _slots.Where(slot => slot.Field.WriteOnly))
		{
			SecretEdit edit = edits.GetValueOrDefault(slot.Name, SecretEdit.Keep);
			changes[slot.Name] = edit.Mode switch
			{
				SecretMode.Change when edit.Text.Length > 0 => MokaSecretChange.Set(edit.Text),
				SecretMode.Clear => MokaSecretChange.Cleared,
				_ => MokaSecretChange.Unchanged
			};
		}

		return changes;
	}

	private Task PutBackAsync()
	{
		StartOver(_saved);
		HideServerProblems();
		_refusalHidden = true;
		return RaiseValueChangedAsync();
	}

	private void AdvancedShown(bool showing) => _showAdvanced = showing;

	private enum SecretMode
	{
		Keep,
		Change,
		Clear
	}

	private readonly record struct SecretEdit(SecretMode Mode, string Text)
	{
		public static SecretEdit Keep { get; } = new(SecretMode.Keep, string.Empty);

		public bool IsChange => Mode == SecretMode.Clear || (Mode == SecretMode.Change && Text.Length > 0);
	}

	// One per field for the schema's life, holding the field's handlers so a render makes no new delegates
	// (blazor/performance/rendering.md, "Avoid recreating delegates for many repeated elements or components").
	private sealed class FieldSlot
	{
		public FieldSlot(MokaSchemaForm form, MokaSchemaField field, int index)
		{
			Field = field;
			Index = index;
			SetText = text => form.SetAsync(this, string.IsNullOrEmpty(text) ? null : JsonValue.Create(text));
			SetWhole = number => form.SetAsync(this, number is { } whole ? JsonValue.Create(whole) : null);
			SetNumber = number => form.SetNumberAsync(this, number);
			SetToggle = on => form.SetAsync(this, JsonValue.Create(on));
			SetChoice = choice => form.SetAsync(this, choice?.Node.DeepClone());
			Left = () => form.Left(this);
			TypeSecret = text => form.EditSecret(this, new SecretEdit(SecretMode.Change, text ?? string.Empty), false);
			ChangeSecret = () => form.EditSecret(this, new SecretEdit(SecretMode.Change, string.Empty), true);
			ClearSecret = () => form.EditSecret(this, new SecretEdit(SecretMode.Clear, string.Empty), true);
			KeepSecret = () => form.EditSecret(this, SecretEdit.Keep, true);
			ChoiceText = choice => choice?.Label ?? form.Words.NotSet;
			ChoiceItems = field.Required ? [.. field.Choices] : [null, .. field.Choices];
		}

		public MokaSchemaField Field { get; }

		public string Name => Field.Name;

		public int Index { get; }

		// Replaced to make the field's control anew, which drops what it holds, such as text a number field couldn't read.
		public object Token { get; set; } = new();

		public MokaNumericField<long?>? WholeInput { get; set; }

		public MokaNumericField<double?>? NumberInput { get; set; }

		public IReadOnlyList<MokaSchemaChoice?> ChoiceItems { get; }

		public Func<MokaSchemaChoice?, string> ChoiceText { get; }

		public Func<string?, Task> SetText { get; }

		public Func<long?, Task> SetWhole { get; }

		public Func<double?, Task> SetNumber { get; }

		public Func<bool, Task> SetToggle { get; }

		public Func<MokaSchemaChoice?, Task> SetChoice { get; }

		public Action Left { get; }

		public Action<string?> TypeSecret { get; }

		public Action ChangeSecret { get; }

		public Action ClearSecret { get; }

		public Action KeepSecret { get; }
	}
}
