namespace Moka.Red.Data.FileBrowser;

/// <summary>A folder that didn't load, for a <c>Problem</c> fragment such as a prompt to unlock it.</summary>
/// <param name="Error">What <c>Load</c> threw.</param>
/// <param name="Retry">Loads the folder again.</param>
public sealed record MokaFileProblem(Exception Error, Func<Task> Retry);
