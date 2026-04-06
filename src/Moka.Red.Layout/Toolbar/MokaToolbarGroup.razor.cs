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

	/// <inheritdoc />
	protected override string RootClass => "moka-toolbar-group";
}
