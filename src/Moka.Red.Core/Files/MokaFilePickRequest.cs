namespace Moka.Red.Core.Files;

/// <summary>What a component asks an <see cref="IMokaFilePicker" /> for.</summary>
/// <param name="Multiple">Whether the person may pick several files. When false the component keeps the first.</param>
/// <param name="Accept">
///     The types the component takes, as a file input's <c>accept</c> attribute writes them: extensions such as
///     <c>".pdf"</c>, MIME types such as <c>"application/pdf"</c> and wildcards such as <c>"image/*"</c>, separated
///     by commas. Null or empty takes any file.
/// </param>
public sealed record MokaFilePickRequest(bool Multiple, string? Accept = null)
{
	/// <summary>
	///     The tokens of <see cref="Accept" />, trimmed and in order, for mapping to a native picker's file types.
	///     Empty when any file goes.
	/// </summary>
	/// <returns>Each extension, MIME type or wildcard.</returns>
	public IReadOnlyList<string> AcceptedTypes() =>
		(Accept ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
