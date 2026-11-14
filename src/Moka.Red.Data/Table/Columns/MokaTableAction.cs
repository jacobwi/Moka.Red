using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;

namespace Moka.Red.Data.Table;

/// <summary>One quiet icon button in a <see cref="MokaActionsColumn{TItem}" />, named by its label.</summary>
/// <typeparam name="TItem">The row data type.</typeparam>
/// <param name="Label">The button's name and tooltip ("Edit").</param>
/// <param name="Icon">The button's icon.</param>
/// <param name="OnClick">What a click does with the row.</param>
public sealed record MokaTableAction<TItem>(string Label, MokaIconDefinition Icon, Func<TItem, Task> OnClick)
{
	/// <summary>Whether the button is disabled for a row. Null, the default, never disables it.</summary>
	public Func<TItem, bool>? Disabled { get; init; }

	/// <summary>Whether the button is left out for a row. Null, the default, always draws it.</summary>
	public Func<TItem, bool>? Hidden { get; init; }

	/// <summary>The button's color on hover, such as <see cref="MokaColor.Error" /> for a delete. Null keeps the quiet look.</summary>
	public MokaColor? Color { get; init; }
}
