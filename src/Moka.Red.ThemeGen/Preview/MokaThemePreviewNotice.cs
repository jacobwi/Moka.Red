namespace Moka.Red.ThemeGen.Preview;

/// <summary>One notification in <see cref="MokaThemePreview" />'s list, for <see cref="MokaThemePreviewText.Notices" />.</summary>
/// <param name="Title">Its title.</param>
/// <param name="Message">The line under the title.</param>
public sealed record MokaThemePreviewNotice(string Title, string Message);
