using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.PageHeader;

/// <summary>
///     What a page starts with: its title, what is so right now, what the page is for, and the actions it offers.
/// </summary>
public partial class MokaPageHeader : MokaVisualComponentBase
{
	/// <summary>The page's name, drawn as its one <c>h1</c>.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>
	///     Instead of <see cref="Title" />, for a title with controls beside it. Include the <c>h1</c>: any <c>h1</c>
	///     in here takes the title's look.
	/// </summary>
	[Parameter]
	public RenderFragment? TitleContent { get; set; }

	/// <summary>The small line above the title, such as the group the page belongs to. It hides on a phone unless it links back.</summary>
	[Parameter]
	public string? Eyebrow { get; set; }

	/// <summary>Makes <see cref="Eyebrow" /> a way back, like "Devices" over one device. It stays on a phone.</summary>
	[Parameter]
	public string? EyebrowHref { get; set; }

	/// <summary>What is so right now, like "12 known, 8 online". It always shows.</summary>
	[Parameter]
	public string? Said { get; set; }

	/// <summary>What the page is for. <c>MokaTheme.WithDescriptions(false)</c> hides it.</summary>
	[Parameter]
	public string? Lead { get; set; }

	/// <summary>Instead of <see cref="Lead" />, for a description with links in it.</summary>
	[Parameter]
	public RenderFragment? LeadContent { get; set; }

	/// <summary>Buttons and controls beside the titles, and full width below them on a phone.</summary>
	[Parameter]
	public RenderFragment? Actions { get; set; }

	/// <summary>A picture to the left of the titles, such as a device's icon tile.</summary>
	[Parameter]
	public RenderFragment? Media { get; set; }

	/// <summary>Under the lead, for tags or facts.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	///     The app's top bar already names the page on a phone, so the title hides there and stays for screen readers.
	/// </summary>
	[Parameter]
	public bool TitleInTopBar { get; set; }

	private string? SafeEyebrowHref => UrlValues.SafeHref(EyebrowHref);

	/// <inheritdoc />
	protected override string RootClass => "moka-page-header";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-page-header--title-in-top-bar", TitleInTopBar)
		.AddClass(Class)
		.Build();
}
