using Microsoft.AspNetCore.Components;

namespace Moka.Red.Data.FileBrowser;

/// <summary>Why the list has nothing to show, for an <c>Empty</c> fragment.</summary>
/// <param name="Filter">The text in the filter box, empty when there is none.</param>
/// <param name="Hidden">How many items of the folder <c>ItemFilter</c> is hiding.</param>
/// <param name="ClearFilter">Empties the filter box.</param>
/// <param name="Default">What the browser would show.</param>
public sealed record MokaFileEmptyContext(string Filter, int Hidden, Func<Task> ClearFilter, RenderFragment Default);
