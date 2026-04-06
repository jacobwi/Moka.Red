namespace Moka.Red.Primitives.Terminal;

/// <summary>
///     Represents a single line of output in a <see cref="MokaTerminal" />.
/// </summary>
/// <param name="Text">The text content of the line.</param>
/// <param name="Color">Optional CSS color override for the line text (e.g., "#4ec9b0", "var(--moka-color-success)").</param>
/// <param name="Prefix">Optional line prefix such as "$", "&gt;", or "#".</param>
public sealed record MokaTerminalLine(string Text, string? Color = null, string? Prefix = null);
