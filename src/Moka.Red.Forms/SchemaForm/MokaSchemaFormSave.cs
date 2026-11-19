using System.Text.Json.Nodes;

namespace Moka.Red.Forms.SchemaForm;

/// <summary>What <see cref="MokaSchemaForm.OnSave" /> hands over: the settings to store and what to do with each secret.</summary>
/// <param name="Values">
///     The whole settings object to store, with defaults filled in and write-only settings left out. Properties the
///     schema doesn't name are kept as they came. A copy: changing it changes nothing in the form.
/// </param>
/// <param name="Changed">The names of the settings, write-only ones aside, whose values differ from the saved ones.</param>
/// <param name="Secrets">Every write-only setting of the schema, with whether to keep, replace or remove it.</param>
public sealed record MokaSchemaFormSave(
	JsonObject Values,
	IReadOnlyList<string> Changed,
	IReadOnlyDictionary<string, MokaSecretChange> Secrets);
