using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;

namespace Moka.Red.Layout.Toolbar;

/// <summary>
///     Groups related toolbar items together with an optional label.
/// </summary>
public partial class MokaToolbarGroup : MokaComponentBase
{
	/// <summary>Group content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Optional label displayed above the group in extra-small font.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>
	///     Which groups stay when an <see cref="MokaToolbar.Expandable" /> toolbar runs out of room. The lowest drops
	///     first; among equals the last one does. Default 0, and negative values are fine.
	/// </summary>
	[Parameter]
	public int Priority { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-toolbar-group";

	private string? PriorityAttribute => Priority == 0 ? null : Priority.ToString(CultureInfo.InvariantCulture);
}
