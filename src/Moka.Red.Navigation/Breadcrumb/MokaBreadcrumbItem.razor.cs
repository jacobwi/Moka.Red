using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Navigation.Breadcrumb;

/// <summary>
///     A single item in a <see cref="MokaBreadcrumb" /> trail.
///     The last item is rendered as plain text (current page).
/// </summary>
public partial class MokaBreadcrumbItem
{
	/// <summary>Custom content for the breadcrumb item.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Display text for the breadcrumb item.</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Navigation link. When set, renders as a clickable link.</summary>
	[Parameter]
	public string? Href { get; set; }

	/// <summary>Icon displayed before the text.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	[CascadingParameter] private MokaBreadcrumb? ParentBreadcrumb { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-breadcrumb-item";

	private bool IsLast => ParentBreadcrumb?.IsLastItem(this) ?? false;
	private bool IsLink => !string.IsNullOrEmpty(Href) && !IsLast;
	private bool ShouldShow => ParentBreadcrumb?.ShouldShowItem(this) ?? true;
	private bool ShowEllipsis => ParentBreadcrumb?.ShouldShowEllipsis(this) ?? false;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-breadcrumb-item--active", IsLast)
		.AddClass(Class)
		.Build();

	protected override void OnInitialized() => ParentBreadcrumb?.RegisterItem(this);

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		ParentBreadcrumb?.UnregisterItem(this);
		await base.DisposeAsyncCore();
	}
}
