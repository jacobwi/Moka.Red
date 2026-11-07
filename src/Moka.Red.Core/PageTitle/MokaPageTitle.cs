using Microsoft.AspNetCore.Components;

namespace Moka.Red.Core.PageTitle;

/// <summary>
///     What a page calls itself in the browser's tab: <c>&lt;MokaPageTitle Text="Devices"/&gt;</c>. One
///     <see cref="MokaPageTitleHost" /> in the layout writes it. Renders nothing.
/// </summary>
public sealed class MokaPageTitle : ComponentBase, IDisposable
{
	[Inject]
	private IMokaPageTitleService Titles { get; set; } = default!;

	/// <summary>The page's name.</summary>
	[Parameter]
	[EditorRequired]
	public string Text { get; set; } = string.Empty;

	/// <inheritdoc />
	public void Dispose() => Titles.Forget(this);

	/// <inheritdoc />
	protected override void OnParametersSet() => Titles.Say(this, Text);
}
