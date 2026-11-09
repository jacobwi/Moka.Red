using Microsoft.AspNetCore.Components;

namespace Moka.Red.Data.FileBrowser;

/// <summary>The controls a <see cref="MokaFileBrowser{TItem}" /> draws in its toolbars.</summary>
[Flags]
public enum MokaFileControls
{
	/// <summary>None of them.</summary>
	None = 0,

	/// <summary>The Back button.</summary>
	Back = 1 << 0,

	/// <summary>The Forward button.</summary>
	Forward = 1 << 1,

	/// <summary>The Up button.</summary>
	Up = 1 << 2,

	/// <summary>The breadcrumb.</summary>
	Path = 1 << 3,

	/// <summary>The Refresh button.</summary>
	Refresh = 1 << 4,

	/// <summary>The filter box.</summary>
	Filter = 1 << 5,

	/// <summary>The New folder button, drawn only with a <c>CreateFolder</c> action.</summary>
	NewFolder = 1 << 6,

	/// <summary>The Upload button, drawn only with an <c>Upload</c> action.</summary>
	Upload = 1 << 7,

	/// <summary>The sort choice and the button that reverses it.</summary>
	Sort = 1 << 8,

	/// <summary>The list and tiles buttons.</summary>
	ViewMode = 1 << 9,

	/// <summary>Every control. The default.</summary>
	All = Back | Forward | Up | Path | Refresh | Filter | NewFolder | Upload | Sort | ViewMode
}

/// <summary>One built-in control, for a <c>ToolbarControl</c> fragment that draws it differently or adds to it.</summary>
/// <param name="Which">Which control this is, one flag of <see cref="MokaFileControls" />.</param>
/// <param name="Enabled">Whether the built-in control would be enabled.</param>
/// <param name="Run">
///     What a press does. Null for <see cref="MokaFileControls.Path" />, <see cref="MokaFileControls.Filter" />,
///     <see cref="MokaFileControls.Sort" /> and <see cref="MokaFileControls.ViewMode" />, which a replacement drives
///     through <c>NavigateAsync</c>, <c>@bind-Filter</c> and <c>@bind-View</c>.
/// </param>
/// <param name="Default">The built-in control, to keep it and add something beside it.</param>
public sealed record MokaFileControlContext(MokaFileControls Which, bool Enabled, Func<Task>? Run, RenderFragment Default);
