using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Moka.Red.Data.Table;

/// <summary>
///     Says how a <see cref="MokaTable{TItem}" /> pages its rows. Put it among the table's columns; it draws nothing
///     where it stands, and the table draws the pager in its place. Without one the table draws the
///     <see cref="MokaPagerLook.Numbered" /> pager under the rows.
/// </summary>
public sealed class MokaTablePager : ComponentBase, IDisposable
{
	private PagerSettings? _lastSettings;

	/// <summary>Which pager to draw. Default <see cref="MokaPagerLook.Numbered" />.</summary>
	[Parameter]
	public MokaPagerLook Look { get; set; }

	/// <summary>
	///     Where the numbered, compact, jump and cursor pagers sit. Default under the rows. The entries pager always puts
	///     its page size above the rows and its buttons below, and the load more and endless pagers sit under the rows.
	/// </summary>
	[Parameter]
	public MokaTablePaginationPosition Position { get; set; } = MokaTablePaginationPosition.Bottom;

	/// <summary>Keeps the pager in view at its edge while the page or the table scrolls. Default false.</summary>
	[Parameter]
	public bool Sticky { get; set; }

	/// <summary>The page sizes someone can pick. Default 10, 25, 50 and 100.</summary>
	[Parameter]
	public IReadOnlyList<int> PageSizeOptions { get; set; } = [10, 25, 50, 100];

	/// <summary>Whether the numbered pager offers the page sizes. Default true.</summary>
	[Parameter]
	public bool ShowPageSize { get; set; } = true;

	/// <summary>Whether the numbered pager has first and last page buttons. Default true.</summary>
	[Parameter]
	public bool ShowFirstLast { get; set; } = true;

	/// <summary>Whether the numbered pager says "1-10 of 200". Default true.</summary>
	[Parameter]
	public bool ShowPageInfo { get; set; } = true;

	/// <summary>How many page numbers the numbered pager shows at most. Default 5.</summary>
	[Parameter]
	public int MaxVisiblePages { get; set; } = 5;

	[CascadingParameter] private IMokaTablePartHost? Host { get; set; }

	internal PagerSettings Settings => _lastSettings ?? CurrentSettings();

	/// <inheritdoc />
	public void Dispose() => Host?.RemovePager(this);

	/// <inheritdoc />
	protected override void OnInitialized() => Host?.AddPager(this);

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		PagerSettings settings = CurrentSettings();
		if (_lastSettings is { } previous && previous.Equals(settings))
		{
			return;
		}

		bool first = _lastSettings is null;
		_lastSettings = settings;
		if (!first)
		{
			Host?.OnPagerChanged(this);
		}
	}

	/// <inheritdoc />
	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
	}

	private PagerSettings CurrentSettings() =>
		new(Look, Position, Sticky, PageSizeOptions, ShowPageSize, ShowFirstLast, ShowPageInfo, MaxVisiblePages);
}

/// <summary>What a pager draws, as the parent last set it.</summary>
internal sealed record PagerSettings(
	MokaPagerLook Look,
	MokaTablePaginationPosition Position,
	bool Sticky,
	IReadOnlyList<int> PageSizeOptions,
	bool ShowPageSize,
	bool ShowFirstLast,
	bool ShowPageInfo,
	int MaxVisiblePages)
{
	public static PagerSettings Default { get; } = new(MokaPagerLook.Numbered, MokaTablePaginationPosition.Bottom, false,
		[10, 25, 50, 100], true, true, true, 5);

	public bool Equals(PagerSettings? other) =>
		other is not null
		&& Look == other.Look
		&& Position == other.Position
		&& Sticky == other.Sticky
		&& PageSizeOptions.SequenceEqual(other.PageSizeOptions)
		&& ShowPageSize == other.ShowPageSize
		&& ShowFirstLast == other.ShowFirstLast
		&& ShowPageInfo == other.ShowPageInfo
		&& MaxVisiblePages == other.MaxVisiblePages;

	public override int GetHashCode() => HashCode.Combine(Look, Position, Sticky, PageSizeOptions.Count, MaxVisiblePages);
}

/// <summary>The table, as a pager sees it.</summary>
internal interface IMokaTablePartHost
{
	void AddPager(MokaTablePager pager);

	void RemovePager(MokaTablePager pager);

	void OnPagerChanged(MokaTablePager pager);

	void OnGroupChanged();

	void AddGroup(MokaColumnGroup group);

	void RemoveGroup(MokaColumnGroup group);

	void SawGroup(MokaColumnGroup group);

	void OnGroupColumnsChanged();
}
