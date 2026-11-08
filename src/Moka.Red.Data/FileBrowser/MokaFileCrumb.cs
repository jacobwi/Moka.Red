namespace Moka.Red.Data.FileBrowser;

/// <summary>One level of the path above the list. The last crumb is the folder on screen.</summary>
/// <param name="FolderKey">The key <c>Load</c> and <c>FolderChanged</c> use for this folder.</param>
/// <param name="Title">The name shown for this level.</param>
public sealed record MokaFileCrumb(string FolderKey, string Title);
