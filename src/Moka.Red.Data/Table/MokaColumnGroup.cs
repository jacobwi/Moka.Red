using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Core.Base;

namespace Moka.Red.Data.Table;

/// <summary>
///     A heading over several columns of a <see cref="MokaTable{TItem}" />, drawn in a row above theirs. Put the columns
///     inside it. Columns that stand together under the same group share one heading; a group whose columns are all hidden
///     draws none, and pinning or moving a column out from among the others splits the heading in two.
/// </summary>
public sealed class MokaColumnGroup : ComponentBase, IDisposable
{
	private readonly MokaChildList<object> _columns;
	private (string? Title, bool KeepTogether, string? Class)? _last;

	/// <summary>Creates the group.</summary>
	public MokaColumnGroup()
	{
		_columns = new MokaChildList<object>(() => Host?.OnGroupColumnsChanged());
	}

	/// <summary>The heading's text.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>The columns under the heading.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     Keeps the group's columns together: a column of the group can only be dragged onto another of the group, and no
	///     other column onto one of them. Default false.
	/// </summary>
	[Parameter]
	public bool KeepTogether { get; set; }

	/// <summary>Classes on the heading's cell.</summary>
	[Parameter]
	public string? Class { get; set; }

	[CascadingParameter] private IMokaTablePartHost? Host { get; set; }

	// The group's columns in the order they stand in its content, as the table keeps its own parts.
	internal IReadOnlyList<object> Columns => _columns.Items;

	/// <inheritdoc />
	public void Dispose() => Host?.RemoveGroup(this);

	// A group starts with its siblings, in the order they stand, while its own columns start only once it renders, after
	// them; the table keeps the group's place so its columns stand where the markup puts them.
	/// <inheritdoc />
	protected override void OnInitialized() => Host?.AddGroup(this);

	internal void AddColumn(object column) => _columns.Add(column);

	internal void RemoveColumn(object column) => _columns.Remove(column);

	internal void SawColumn(object column) => _columns.Saw(column);

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		Host?.SawGroup(this);
		(string? Title, bool KeepTogether, string? Class) now = (Title, KeepTogether, Class);
		if (_last is { } previous && previous != now)
		{
			Host?.OnGroupChanged();
		}

		_last = now;
	}

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		builder.OpenComponent<CascadingValue<MokaColumnGroup>>(0);
		builder.AddComponentParameter(1, nameof(CascadingValue<MokaColumnGroup>.Value), this);
		builder.AddComponentParameter(2, nameof(CascadingValue<MokaColumnGroup>.IsFixed), true);
		builder.AddComponentParameter(3, nameof(CascadingValue<MokaColumnGroup>.ChildContent), _columns.Collect(ChildContent));
		builder.CloseComponent();
	}
}
