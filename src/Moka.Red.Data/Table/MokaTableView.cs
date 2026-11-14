namespace Moka.Red.Data.Table;

/// <summary>
///     A table state kept under a name and drawn as a chip over a <see cref="MokaTable{TItem}" />. Picking the chip puts
///     its state in force; the chip is lit while the table shows the same view (<see cref="MokaTableState.SameView" />).
///     Storing views is the app's job. Two views are equal when their names and states match.
/// </summary>
/// <param name="Name">The name on the chip.</param>
/// <param name="State">The state the view puts in force. Its page, density and folded or opened rows are left alone.</param>
public sealed record MokaTableView(string Name, MokaTableState State);
