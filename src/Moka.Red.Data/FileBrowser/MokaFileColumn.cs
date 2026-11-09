using Microsoft.AspNetCore.Components;

namespace Moka.Red.Data.FileBrowser;

/// <summary>The narrowest a <see cref="MokaFileBrowser{TItem}" /> can be for a column to show.</summary>
public enum MokaFileColumnFit
{
	/// <summary>At any width.</summary>
	Always,

	/// <summary>From 36rem of its own width, or what <c>FitWidths</c> says.</summary>
	Medium,

	/// <summary>From 52rem of its own width, or what <c>FitWidths</c> says.</summary>
	Wide
}

/// <summary>The widths at which the columns of a <see cref="MokaFileBrowser{TItem}" /> start to show.</summary>
/// <param name="Medium">Where <see cref="MokaFileColumnFit.Medium" /> columns start, in rem of the browser's own width.</param>
/// <param name="Wide">Where <see cref="MokaFileColumnFit.Wide" /> columns start, in rem of the browser's own width.</param>
public sealed record MokaFileFitWidths(double Medium = 36, double Wide = 52);

/// <summary>
///     A column after the name. An <see cref="Id" /> of <see cref="MokaFileSort.Size" /> or
///     <see cref="MokaFileSort.Modified" /> with no <see cref="Text" /> or <see cref="Cell" /> is the built-in column.
/// </summary>
/// <typeparam name="TItem">The item type of the browser.</typeparam>
/// <param name="Id">Names the column in the sort of a <see cref="MokaFileView" />.</param>
/// <param name="Title">The heading. Null reads the built-in column's heading from the text record.</param>
public sealed record MokaFileColumn<TItem>(string Id, string? Title = null)
{
	/// <summary>The cell as text. <see cref="Cell" /> wins.</summary>
	public Func<TItem, string>? Text { get; init; }

	/// <summary>Draws the cell.</summary>
	public RenderFragment<TItem>? Cell { get; init; }

	/// <summary>Makes the column sortable. Built-in columns sort without it.</summary>
	public Comparison<TItem>? Compare { get; init; }

	/// <summary>The column's width as a CSS length. The name takes what is left. Default 8rem.</summary>
	public string? Width { get; init; }

	/// <summary>Draws the cell in the monospace font with tabular digits.</summary>
	public bool Mono { get; init; }

	/// <summary>When the column shows. Null is <see cref="MokaFileColumnFit.Wide" /> for a column of your own.</summary>
	public MokaFileColumnFit? Fit { get; init; }
}
