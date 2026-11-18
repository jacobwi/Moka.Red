using Microsoft.AspNetCore.Components;

namespace Moka.Red.Core.Base;

/// <summary>
///     The child components that register with a parent component, kept in the order they stand in the parent's
///     content. A child the page stops rendering and renders again, such as one inside an <c>@if</c>, takes its place in
///     the markup again rather than joining at the end.
/// </summary>
/// <remarks>
///     <para>
///         This is how QuickGrid collects its columns. The parent renders <see cref="Collect" /> of its content inside
///         the cascading value that hands the children the parent. Each child calls <see cref="Add" /> from
///         <c>OnInitialized</c>, <see cref="Saw" /> from <c>OnParametersSet</c> and <see cref="Remove" /> when it is
///         disposed. Blazor sets the children's parameters in the order they stand, so they report in markup order, and a
///         component rendered after the content settles the order once they have.
///     </para>
///     <para>
///         The markup order has two gaps. Blazor hands a child its parameters again only when one of them may have
///         changed: a render fragment or a delegate always may, while strings, numbers, booleans and enums that kept their
///         values never do. A child that didn't report keeps its place after the child it followed. A new child goes
///         right after the child that reported before it; when children that didn't report stand next to it, which side
///         of them it belongs on is a guess, and <see cref="Guessed" /> says so. And a child rendered by another
///         component of the page gets its parameters when that component renders, which Blazor queues after the
///         parent's own children, so it joins after them. QuickGrid, which collects its columns as they render, leaves
///         such a column out.
///     </para>
///     <para>
///         A parent that creates the list with an order key closes both gaps for the children that take one: they are
///         listed by the key, lowest first, and children with the same key in the markup's order, as CSS <c>order</c>
///         does. A child whose key changes moves when it reports. While every key is the same the list is the markup's
///         and nothing is sorted.
///     </para>
///     <para>
///         The parent hears about changes through the callbacks it passes: at once, or, for changes made while it
///         collects, together when the collection ends. A render it asks for through <see cref="Render" /> while it
///         collects comes then too, so it draws once.
///     </para>
/// </remarks>
/// <typeparam name="TChild">The child component type.</typeparam>
public sealed class MokaChildList<TChild> where TChild : class
{
	private readonly HashSet<TChild> _added = new(ReferenceEqualityComparer.Instance);
	private readonly Action<IReadOnlyList<TChild>>? _changed;
	private readonly List<TChild> _children = [];
	private readonly IReadOnlyList<TChild> _childrenView;
	private readonly Action _finish;
	private readonly Dictionary<TChild, int>? _keys;
	private readonly HashSet<TChild> _members = new(ReferenceEqualityComparer.Instance);
	private readonly Func<TChild, int>? _order;
	private readonly Action _render;
	private readonly List<TChild> _seen = [];
	private readonly HashSet<TChild> _seenSet = new(ReferenceEqualityComparer.Instance);
	private List<TChild>? _before;
	private bool _changing;
	private List<TChild>? _ordered;
	private IReadOnlyList<TChild>? _orderedView;
	private bool _renderOwed;

	/// <summary>Creates the list for one parent.</summary>
	/// <param name="render">Asks the parent to render, typically its <c>StateHasChanged</c>.</param>
	/// <param name="changed">
	///     Called before <paramref name="render" /> when children came, went or moved, with the children in the order they
	///     stood before. A parent that follows one child, such as the active step, finds where it went with
	///     <see cref="IndexNow" />.
	/// </param>
	public MokaChildList(Action render, Action<IReadOnlyList<TChild>>? changed = null)
	{
		ArgumentNullException.ThrowIfNull(render);
		_render = render;
		_changed = changed;
		_childrenView = _children.AsReadOnly();
		_finish = FinishCollecting;
	}

	/// <summary>Creates the list for one parent whose children take an order key, such as an <c>Order</c> parameter.</summary>
	/// <param name="render">Asks the parent to render, typically its <c>StateHasChanged</c>.</param>
	/// <param name="changed">
	///     Called before <paramref name="render" /> when children came, went or moved, with the children in the order they
	///     stood before.
	/// </param>
	/// <param name="order">
	///     A child's order key: lower comes first, and children with the same key keep the markup's order. Read when a
	///     child joins, leaves or reports.
	/// </param>
	public MokaChildList(Action render, Action<IReadOnlyList<TChild>>? changed, Func<TChild, int> order)
		: this(render, changed)
	{
		ArgumentNullException.ThrowIfNull(order);
		_order = order;
		_keys = new Dictionary<TChild, int>(ReferenceEqualityComparer.Instance);
	}

	/// <summary>The children in the order they stand.</summary>
	public IReadOnlyList<TChild> Items => _orderedView ?? _childrenView;

	/// <summary>How many children there are.</summary>
	public int Count => _children.Count;

	/// <summary>Whether the parent's content is rendering, from the start of <see cref="Collect" /> until its children have reported.</summary>
	public bool IsCollecting { get; private set; }

	/// <summary>
	///     Whether the last collection placed a new child next to children that didn't report, so its side of them is a
	///     guess. A parent whose children hold no state can render them again under a new <c>@key</c> after
	///     <see cref="Clear" />, and they all report in markup order.
	/// </summary>
	public bool Guessed { get; private set; }

	private List<TChild> View => _ordered ?? _children;

	/// <summary>The child at <paramref name="index" />.</summary>
	/// <param name="index">The child's position.</param>
	public TChild this[int index] => View[index];

	/// <summary>Where <paramref name="child" /> stands, or -1 when it isn't here.</summary>
	/// <param name="child">The child.</param>
	/// <returns>Its position.</returns>
	public int IndexOf(TChild child) => _members.Contains(child) ? View.IndexOf(child) : -1;

	/// <summary>Whether <paramref name="child" /> is here.</summary>
	/// <param name="child">The child.</param>
	/// <returns>True when it registered and hasn't left.</returns>
	public bool Contains(TChild child) => _members.Contains(child);

	/// <summary>
	///     Registers a child, from its <c>OnInitialized</c>. While the parent collects it goes right after the last child
	///     that reported, else last.
	/// </summary>
	/// <param name="child">The child.</param>
	/// <returns>False when it was already here.</returns>
	public bool Add(TChild child)
	{
		ArgumentNullException.ThrowIfNull(child);
		if (!_members.Add(child))
		{
			return false;
		}

		BeginChange();
		if (IsCollecting)
		{
			_added.Add(child);
			_children.Insert(_seen.Count == 0 ? 0 : _children.IndexOf(_seen[^1]) + 1, child);
		}
		else
		{
			_children.Add(child);
		}

		ApplyOrder();
		EndChange();
		return true;
	}

	/// <summary>Forgets a child, from its disposal.</summary>
	/// <param name="child">The child.</param>
	/// <returns>Where it stood, or -1 when it wasn't here.</returns>
	public int Remove(TChild child)
	{
		ArgumentNullException.ThrowIfNull(child);
		if (!_members.Remove(child))
		{
			return -1;
		}

		BeginChange();
		int index = View.IndexOf(child);
		_children.Remove(child);
		_ordered?.Remove(child);
		_keys?.Remove(child);
		_added.Remove(child);
		if (_seenSet.Remove(child))
		{
			_seen.Remove(child);
		}

		ApplyOrder();
		EndChange();
		return index;
	}

	/// <summary>
	///     The parent's content with the collection around it, for the parent to render inside the cascading value that
	///     hands its children the parent. It adds no markup of its own.
	/// </summary>
	/// <param name="content">The parent's content.</param>
	/// <returns>The content to render.</returns>
	public RenderFragment Collect(RenderFragment? content) => builder =>
	{
		StartCollecting();
		builder.AddContent(0, content);
		builder.OpenComponent<MokaChildListEnd>(1);
		builder.AddComponentParameter(2, nameof(MokaChildListEnd.Finish), _finish);
		builder.CloseComponent();
	};

	internal void StartCollecting()
	{
		_seen.Clear();
		_seenSet.Clear();
		_added.Clear();
		Guessed = false;
		IsCollecting = true;
	}

	/// <summary>
	///     Reports where a child stands, from its <c>OnParametersSet</c>. Outside a collection it only moves a child whose
	///     order key changed.
	/// </summary>
	/// <param name="child">The child.</param>
	public void Saw(TChild child)
	{
		if (child is null || !_members.Contains(child))
		{
			return;
		}

		if (IsCollecting)
		{
			if (_seenSet.Add(child))
			{
				_seen.Add(child);
			}

			return;
		}

		if (_order is not null && _keys!.TryGetValue(child, out int key) && key != _order(child))
		{
			ApplyOrder();
			EndChange();
		}
	}

	/// <summary>Asks the parent to render: at once, or once the collection under way ends.</summary>
	public void Render()
	{
		if (IsCollecting)
		{
			_renderOwed = true;
		}
		else
		{
			_render();
		}
	}

	/// <summary>
	///     Where the child that stood at <paramref name="index" /> in <paramref name="before" /> stands now. When it left,
	///     the same position, kept within the children left; an index outside <paramref name="before" /> stays as it is.
	/// </summary>
	/// <param name="before">The children as they stood, as the change callback hands them.</param>
	/// <param name="index">A position in <paramref name="before" />.</param>
	/// <returns>The position now.</returns>
	public int IndexNow(IReadOnlyList<TChild> before, int index)
	{
		ArgumentNullException.ThrowIfNull(before);
		if (index < 0 || index >= before.Count)
		{
			return index;
		}

		int now = IndexOf(before[index]);
		return now >= 0 ? now : Math.Min(index, Math.Max(Count - 1, 0));
	}

	/// <summary>
	///     Forgets every child, with no callback, for a parent that renders its children again under a new <c>@key</c>.
	/// </summary>
	public void Clear()
	{
		_children.Clear();
		_members.Clear();
		_seen.Clear();
		_seenSet.Clear();
		_added.Clear();
		_keys?.Clear();
		_ordered = null;
		_orderedView = null;
		_before = null;
		_changing = false;
		Guessed = false;
	}

	internal void FinishCollecting()
	{
		if (!IsCollecting)
		{
			return;
		}

		IsCollecting = false;
		if (SettledOrder() is { } order)
		{
			BeginChange();
			_children.Clear();
			_children.AddRange(order);
		}

		ApplyOrder();
		_seen.Clear();
		_seenSet.Clear();
		_added.Clear();
		EndChange();
	}

	private void BeginChange()
	{
		if (_changing)
		{
			return;
		}

		_changing = true;
		_before = _changed is null ? null : [.. View];
	}

	private void EndChange()
	{
		if (IsCollecting)
		{
			return;
		}

		bool changed = _changing;
		List<TChild>? before = _before;
		bool render = changed || _renderOwed;
		_changing = false;
		_before = null;
		_renderOwed = false;
		if (before is not null)
		{
			_changed!(before);
		}

		if (render)
		{
			_render();
		}
	}

	// Lists the children by their order keys, the markup's order between equal keys. While every key is the same the
	// markup's list is the list, with no copy. A change to what the parent sees counts as a change.
	private void ApplyOrder()
	{
		if (_order is null)
		{
			return;
		}

		_keys!.Clear();
		bool varied = false;
		int first = 0;
		for (int i = 0; i < _children.Count; i++)
		{
			int key = _order(_children[i]);
			_keys[_children[i]] = key;
			if (i == 0)
			{
				first = key;
			}
			else if (key != first)
			{
				varied = true;
			}
		}

		List<TChild>? ordered = varied ? [.. _children.OrderBy(child => _keys[child])] : null;
		if (!(ordered ?? _children).SequenceEqual(View, ReferenceEqualityComparer.Instance))
		{
			BeginChange();
		}

		_ordered = ordered;
		_orderedView = ordered?.AsReadOnly();
	}

	// Null when the order stands. The children that didn't report keep their places: those before the first old child
	// that reported come first, and the others follow the old child they followed. A new child follows the child that
	// reported before it, ahead of the ones that didn't.
	private List<TChild>? SettledOrder()
	{
		if (_added.Count == 0 && SeenInPlace())
		{
			return null;
		}

		if (_seen.Count == _children.Count)
		{
			return _seen.SequenceEqual(_children, ReferenceEqualityComparer.Instance) ? null : [.. _seen];
		}

		List<TChild> order = new(_children.Count);
		Dictionary<TChild, List<TChild>>? following = null;
		TChild? anchor = null;
		foreach (TChild child in _children)
		{
			if (_seenSet.Contains(child))
			{
				if (!_added.Contains(child))
				{
					anchor = child;
				}
			}
			else if (anchor is null)
			{
				order.Add(child);
			}
			else
			{
				following ??= new Dictionary<TChild, List<TChild>>(ReferenceEqualityComparer.Instance);
				if (!following.TryGetValue(anchor, out List<TChild>? run))
				{
					following[anchor] = run = [];
				}

				run.Add(child);
			}
		}

		int leading = order.Count;
		int fresh = 0;
		TChild? last = null;
		foreach (TChild child in _seen)
		{
			if (_added.Contains(child))
			{
				order.Add(child);
				fresh++;
				continue;
			}

			CloseRun(last);
			order.Add(child);
			last = child;
			fresh = 0;
		}

		CloseRun(last);
		return order.SequenceEqual(_children, ReferenceEqualityComparer.Instance) ? null : order;

		void CloseRun(TChild? owner)
		{
			List<TChild>? run = owner is null ? null : following?.GetValueOrDefault(owner);
			int skipped = owner is null ? leading : run?.Count ?? 0;
			if (fresh > 0 && skipped > 0)
			{
				Guessed = true;
			}

			if (run is not null)
			{
				order.AddRange(run);
			}
		}
	}

	private bool SeenInPlace()
	{
		int next = 0;
		foreach (TChild child in _children)
		{
			if (_seenSet.Contains(child) && !ReferenceEquals(_seen[next++], child))
			{
				return false;
			}
		}

		return true;
	}
}
