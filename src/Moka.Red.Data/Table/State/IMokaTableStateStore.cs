namespace Moka.Red.Data.Table;

/// <summary>
///     Where a <see cref="MokaTable{TItem}" /> with a <see cref="MokaTable{TItem}.StorageKey" /> keeps its state. Without
///     one registered the table keeps it in the browser's local storage. Register your own as a scoped service to keep
///     it per person on the server, for example; <see cref="MokaTableState.ToJson" /> and
///     <see cref="MokaTableState.FromJson" /> turn a state into text and back.
/// </summary>
public interface IMokaTableStateStore
{
	/// <summary>Reads the state kept under a key.</summary>
	/// <param name="key">The table's storage key.</param>
	/// <param name="cancellationToken">Cancelled when the table goes away.</param>
	/// <returns>The state, or null when nothing is kept under the key.</returns>
	ValueTask<MokaTableState?> LoadAsync(string key, CancellationToken cancellationToken);

	/// <summary>Keeps a state under a key, replacing what was there.</summary>
	/// <param name="key">The table's storage key.</param>
	/// <param name="state">The state to keep.</param>
	/// <param name="cancellationToken">Cancelled when the table goes away.</param>
	/// <returns>A task that completes once the state is kept.</returns>
	ValueTask SaveAsync(string key, MokaTableState state, CancellationToken cancellationToken);
}
