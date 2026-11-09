namespace Moka.Red.Data.FileBrowser;

/// <summary>What the failure alert is showing, for a <c>FailureActions</c> fragment.</summary>
/// <param name="Error">What went wrong.</param>
/// <param name="Retry">Runs what failed again. Null when the app reported the failure outside any action.</param>
public sealed record MokaFileFailure(Exception Error, Func<Task>? Retry);
