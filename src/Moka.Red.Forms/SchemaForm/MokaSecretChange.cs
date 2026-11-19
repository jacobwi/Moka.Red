namespace Moka.Red.Forms.SchemaForm;

/// <summary>What a save does to a write-only setting.</summary>
public enum MokaSecretEdit
{
	/// <summary>Leave it as it is: keep the saved value, or keep it unset.</summary>
	Unchanged,

	/// <summary>Store <see cref="MokaSecretChange.Value" /> as the new value.</summary>
	Set,

	/// <summary>Remove the saved value.</summary>
	Cleared
}

/// <summary>
///     What a save does to one write-only setting. The form never holds the saved value, so it can only say whether to
///     keep it, replace it or remove it.
/// </summary>
public readonly record struct MokaSecretChange
{
	private MokaSecretChange(MokaSecretEdit edit, string? value)
	{
		Edit = edit;
		Value = value;
	}

	/// <summary>Keep the setting as it is.</summary>
	public static MokaSecretChange Unchanged => default;

	/// <summary>Remove the saved value.</summary>
	public static MokaSecretChange Cleared { get; } = new(MokaSecretEdit.Cleared, null);

	/// <summary>Whether to keep, replace or remove the setting.</summary>
	public MokaSecretEdit Edit { get; }

	/// <summary>The new value when <see cref="Edit" /> is <see cref="MokaSecretEdit.Set" />, otherwise null.</summary>
	public string? Value { get; }

	/// <summary>Store a new value.</summary>
	/// <param name="value">The new value.</param>
	/// <returns>The change.</returns>
	public static MokaSecretChange Set(string value)
	{
		ArgumentNullException.ThrowIfNull(value);
		return new MokaSecretChange(MokaSecretEdit.Set, value);
	}

	/// <summary>The edit alone, so a secret never reaches a log through this.</summary>
	/// <returns>"Unchanged", "Set" or "Cleared".</returns>
	public override string ToString() => Edit.ToString();
}
