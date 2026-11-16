using System.Runtime.CompilerServices;

namespace Moka.Red.Diagnostics.Components.Panels;

// A @key for each item of a log, by reference. The log entries are records, and two equal entries (the same message
// in the same tick) would be one key twice, which Blazor refuses. Keyed rows let a refresh that adds a few entries at
// the top send those rows alone, rather than rewriting every row below them.
internal sealed class RowKeys<T>
	where T : class
{
	private readonly ConditionalWeakTable<T, object> _keys = new();

	public object For(T item) => _keys.GetValue(item, static _ => new object());

	// The logs only grow at the newest end and drop from the oldest, and keep their entries, so the same count with the
	// same entries at both ends is the same list.
	public static bool SameLog(IReadOnlyList<T> shown, IReadOnlyList<T> latest) =>
		shown.Count == latest.Count
		&& (shown.Count == 0 || (ReferenceEquals(shown[0], latest[0]) && ReferenceEquals(shown[^1], latest[^1])));
}
