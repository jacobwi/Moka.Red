using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;

namespace Moka.Red.Data.FileBrowser;

/// <summary>What <see cref="MokaFileBrowser{TItem}" /> needs to know about one item in a folder.</summary>
/// <param name="Key">Identifies the item within its folder. Keys in one folder must differ.</param>
/// <param name="Name">The name people read and rename.</param>
/// <param name="IsFolder">Whether the item is a folder, which opens in the browser and can take a drop.</param>
/// <param name="Size">The size in bytes. Null shows nothing.</param>
/// <param name="Modified">When the item last changed. Null shows nothing.</param>
/// <param name="Icon">The icon. Null is a folder or a document.</param>
/// <param name="IconColor">The icon's color. Null is the warning color for a folder and the muted color for a file.</param>
public sealed record MokaFileInfo(
	string Key,
	string Name,
	bool IsFolder,
	long? Size = null,
	DateTimeOffset? Modified = null,
	MokaIconDefinition? Icon = null,
	MokaColor? IconColor = null);
