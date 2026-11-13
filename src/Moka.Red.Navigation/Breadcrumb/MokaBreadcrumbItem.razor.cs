using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Feedback.Tooltip;

namespace Moka.Red.Navigation.Breadcrumb;

/// <summary>
///     A single item in a <see cref="MokaBreadcrumb" /> trail.
///     The last item is rendered as plain text (current page).
/// </summary>
public partial class MokaBreadcrumbItem
{
	private (string? Text, string? Href, string? Icon, string? Segment, bool IconOnly, bool HasContent) _lastReported;
	private MokaBreadcrumbPlace _place = MokaBreadcrumbPlace.Alone;
	private bool _reported;

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

	/// <summary>
	///     Shows only <see cref="Icon" />, named by <see cref="Text" /> through <c>aria-label</c> and a tooltip. Made for
	///     Home. Needs both an icon and text.
	/// </summary>
	[Parameter]
	public bool IconOnly { get; set; }

	/// <summary>
	///     What this level adds to the copied path when it differs from <see cref="Text" />, such as a slug or an id.
	///     See <see cref="MokaBreadcrumb.CopyText" />.
	/// </summary>
	[Parameter]
	public string? Segment { get; set; }

	[CascadingParameter] private MokaBreadcrumb? ParentBreadcrumb { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-breadcrumb-item";

	private string? LinkHref => UrlValues.SafeHref(Href);

	private bool IsLink => !string.IsNullOrEmpty(LinkHref) && !_place.Current;

	private bool ShowsOnlyIcon => IconOnly && Icon is not null && !string.IsNullOrWhiteSpace(Text);

	private bool MayBeCut => !ShowsOnlyIcon && ChildContent is null && (ParentBreadcrumb?.MayCut(Text, _place.Current) ?? false);

	// The first level's tooltip opens along the row, so it doesn't hang off the start of it.
	private MokaTooltipPosition TooltipSide => _place.Index == 0 ? MokaTooltipPosition.BottomStart : MokaTooltipPosition.Bottom;

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-breadcrumb-item--active", _place.Current)
		.AddClass("moka-breadcrumb-item--icon-only", ShowsOnlyIcon)
		.AddClass(MokaBreadcrumb.ShowClass(_place.Show))
		.AddClass(Class)
		.Build();

	private string SeparatorClass => new CssBuilder("moka-breadcrumb-item__separator")
		.AddClass(MokaBreadcrumb.ShowClass(_place.Separator))
		.Build();

	private string LinkClass => new CssBuilder("moka-breadcrumb-item__link")
		.AddClass("moka-breadcrumb-item__link--icon", ShowsOnlyIcon)
		.Build();

	/// <inheritdoc />
	protected override void OnInitialized() => ParentBreadcrumb?.RegisterItem(this);

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// The trail draws the folded menu, the back link and the copied path from these, so it has to hear of a change.
		(string?, string?, string?, string?, bool, bool) now = (Text, Href, Icon?.SvgPath, Segment, IconOnly, ChildContent is not null);
		if (_reported && now != _lastReported)
		{
			ParentBreadcrumb?.ItemChanged(this);
		}

		_lastReported = now;
		_reported = true;
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		ParentBreadcrumb?.UnregisterItem(this);
		await base.DisposeAsyncCore();
	}
}
