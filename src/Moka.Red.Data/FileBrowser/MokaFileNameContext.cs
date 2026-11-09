using Microsoft.AspNetCore.Components;

namespace Moka.Red.Data.FileBrowser;

/// <summary>One name in the list, for a <c>NameCell</c> fragment.</summary>
/// <typeparam name="TItem">The item type of the browser.</typeparam>
/// <param name="Item">The item.</param>
/// <param name="Name">The name as the browser draws it, which cuts itself off when it doesn't fit.</param>
public sealed record MokaFileNameContext<TItem>(TItem Item, RenderFragment Name);
