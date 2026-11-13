namespace Moka.Red.Primitives.KeyValue;

/// <summary>How <see cref="MokaKeyValue" /> lays out its label and value.</summary>
public enum MokaKeyValueLook
{
	/// <summary>The label in muted text right before the value, or above it, as <see cref="MokaKeyValue.Orientation" /> says.</summary>
	Standard,

	/// <summary>
	///     Two columns: the label as the small uppercase micro label in a column of its own, the value beside it. The label
	///     column is the same width in every key-value of a list, so the values line up: <see cref="MokaKeyValue.LabelWidth" />
	///     sets it, or <c>--moka-kv-label-width</c> on a parent for the whole list. <see cref="MokaKeyValue.Orientation" />
	///     doesn't apply.
	/// </summary>
	Columns
}
