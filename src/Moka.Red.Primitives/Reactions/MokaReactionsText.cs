using System.Globalization;

namespace Moka.Red.Primitives.Reactions;

/// <summary>Every word <see cref="MokaReactions" /> shows or says to a screen reader, in English by default.</summary>
public sealed record MokaReactionsText
{
	/// <summary>The English words.</summary>
	public static MokaReactionsText Default { get; } = new();

	/// <summary>The name of the quick reaction bar. Default "Quick reactions".</summary>
	public string BarLabel { get; init; } = "Quick reactions";

	/// <summary>The name of the row of counts. Default "Reactions".</summary>
	public string ChipsLabel { get; init; } = "Reactions";

	/// <summary>A quick reaction's name. <c>{emoji}</c> is the emoji. Default "React with {emoji}".</summary>
	public string React { get; init; } = "React with {emoji}";

	/// <summary>The bar's button that opens every emoji. Default "All emoji".</summary>
	public string AllEmoji { get; init; } = "All emoji";

	/// <summary>The button after the counts that opens every emoji. Default "Add reaction".</summary>
	public string Add { get; init; } = "Add reaction";

	/// <summary>A count of one. Default "1 person".</summary>
	public string OnePerson { get; init; } = "1 person";

	/// <summary>A count of several. <c>{count}</c> is how many. Default "{count} people".</summary>
	public string People { get; init; } = "{count} people";

	/// <summary>Added when the viewer is one of them. Default ", including you".</summary>
	public string IncludingYou { get; init; } = ", including you";

	internal string ReactWith(string emoji) => React.Replace("{emoji}", emoji, StringComparison.Ordinal);

	internal string ChipLabel(MokaReaction reaction)
	{
		string people = reaction.Count == 1
			? OnePerson
			: People.Replace("{count}", reaction.Count.ToString(CultureInfo.CurrentCulture), StringComparison.Ordinal);
		return $"{reaction.Emoji}, {people}{(reaction.Mine ? IncludingYou : "")}";
	}
}
