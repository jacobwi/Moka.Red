using System.Diagnostics.CodeAnalysis;

namespace Moka.Red.Forms.SchemaForm;

/// <summary>
///     What <see cref="MokaFormSchema.Parse(string)" /> read: the schema, or every problem that kept it from being one.
///     A schema with any problem gives no schema, so a form never drops a setting it couldn't read.
/// </summary>
public sealed class MokaSchemaParseResult
{
	internal MokaSchemaParseResult(MokaFormSchema? schema, IReadOnlyList<string> problems)
	{
		Schema = schema;
		Problems = problems;
	}

	/// <summary>The schema, or null when there were problems.</summary>
	public MokaFormSchema? Schema { get; }

	/// <summary>What's wrong, one sentence each, naming the property where there is one. Empty when the schema was read.</summary>
	public IReadOnlyList<string> Problems { get; }

	/// <summary>Whether the schema was read.</summary>
	[MemberNotNullWhen(true, nameof(Schema))]
	public bool Succeeded => Schema is not null;
}
