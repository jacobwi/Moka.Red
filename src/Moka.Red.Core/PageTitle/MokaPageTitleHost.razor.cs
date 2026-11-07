using Microsoft.AspNetCore.Components;

namespace Moka.Red.Core.PageTitle;

/// <summary>
///     Writes the browser tab's title from what the page showing said with <see cref="MokaPageTitle" />. Place one in
///     your layout. Its <see cref="Prefix" /> lets the app put what's worth seeing from another tab in front, so a page
///     opened meanwhile can't hide it.
/// </summary>
public sealed partial class MokaPageTitleHost : ComponentBase
{
	[Inject]
	private IMokaPageTitleService Titles { get; set; } = default!;

	/// <summary>Goes in front of the page's name, with a space between: <c>(2)</c> makes <c>(2) Devices</c>.</summary>
	[Parameter]
	public string? Prefix { get; set; }

	/// <summary>The title while no page has said one. Left out, the title stays as the host page wrote it.</summary>
	[Parameter]
	public string? Default { get; set; }

	private string? Title
	{
		get
		{
			string? page = string.IsNullOrEmpty(Titles.Text) ? Default : Titles.Text;
			if (string.IsNullOrEmpty(page))
			{
				return null;
			}

			return string.IsNullOrEmpty(Prefix) ? page : $"{Prefix} {page}";
		}
	}

	/// <inheritdoc />
	public void Dispose() => Titles.OnChanged -= HandleChanged;

	/// <inheritdoc />
	protected override void OnInitialized() => Titles.OnChanged += HandleChanged;

	private void HandleChanged() => _ = InvokeAsync(StateHasChanged);
}
