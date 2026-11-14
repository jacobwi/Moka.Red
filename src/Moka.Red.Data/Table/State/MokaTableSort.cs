namespace Moka.Red.Data.Table;

/// <summary>One column a <see cref="MokaTable{TItem}" /> is sorted by.</summary>
/// <param name="Column">The column's <c>Title</c>.</param>
/// <param name="Direction">Which way it is sorted.</param>
public sealed record MokaTableSort(string Column, MokaSortDirection Direction);
