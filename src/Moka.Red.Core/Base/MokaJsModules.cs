using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.JSInterop;

namespace Moka.Red.Core.Base;

// The browser keeps one instance of a module per URL, so a handle per component only added round trips: 20
// selects, 10 dropdowns and 5 trees made 45 imports of 4 URLs, then 45 disposals on teardown. Keying on IJSRuntime
// gives the right lifetime everywhere without a service to register: it's one per circuit on Server and one per page
// on WebAssembly and MAUI, and the handles it gave out are worth nothing without it. Nothing disposes them. There
// is one per path and runtime, and they go when the runtime goes.
internal static class MokaJsModules
{
	private static readonly ConditionalWeakTable<IJSRuntime, ConcurrentDictionary<string, Task<IJSObjectReference>>>
		ImportsByRuntime = new();

	/// <summary>Gets the runtime's import of a module, starting it if nothing has yet.</summary>
	/// <param name="runtime">The runtime the module is imported through.</param>
	/// <param name="path">The module's path.</param>
	/// <param name="started">True when this call started the import, false when it joined one.</param>
	/// <returns>The import, which callers share.</returns>
	internal static Task<IJSObjectReference> Get(IJSRuntime runtime, string path, out bool started)
	{
		ConcurrentDictionary<string, Task<IJSObjectReference>> imports = ImportsByRuntime.GetOrCreateValue(runtime);
		if (imports.TryGetValue(path, out Task<IJSObjectReference>? existing))
		{
			started = false;
			return existing;
		}

		started = true;
		Task<IJSObjectReference> import = runtime.InvokeAsync<IJSObjectReference>("import", path).AsTask();
		imports[path] = import;

		// A failed import is forgotten so the next caller tries again, since the network may be back by then. Prerendering
		// never gets this far: InvokeAsync throws there before anything is stored.
		_ = import.ContinueWith(
			failed => imports.TryRemove(new KeyValuePair<string, Task<IJSObjectReference>>(path, failed)),
			CancellationToken.None,
			TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously,
			TaskScheduler.Default);

		return import;
	}
}
