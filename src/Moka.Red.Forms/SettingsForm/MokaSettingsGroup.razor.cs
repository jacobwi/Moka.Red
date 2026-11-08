using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Forms.SettingsForm;

/// <summary>
///     A heading that links to itself and the fields under it, in a grid. Its <c>Id</c> is the anchor a deep link lands
///     on. Inside a <see cref="MokaSettingsForm" />, an <see cref="Advanced" /> group shows only while the form's advanced
///     settings do.
/// </summary>
public partial class MokaSettingsGroup : MokaVisualComponentBase
{
	[Inject]
	private NavigationManager Navigation { get; set; } = default!;

	[CascadingParameter]
	private MokaSettingsForm? Form { get; set; }

	/// <summary>The group's name.</summary>
	[Parameter]
	[EditorRequired]
	public string Title { get; set; } = string.Empty;

	/// <summary>What the group is for. <c>MokaTheme.WithDescriptions(false)</c> hides it.</summary>
	[Parameter]
	public string? Help { get; set; }

	/// <summary>Hides the group until the form's "Show advanced" is on.</summary>
	[Parameter]
	public bool Advanced { get; set; }

	/// <summary>A short word beside the title, like "New" for a choice that just arrived.</summary>
	[Parameter]
	public string? Tag { get; set; }

	/// <summary>The grid's columns. Fields take two by default, three where they're short. It's one column on a phone.</summary>
	[Parameter]
	public int Columns { get; set; } = 2;

	/// <summary>
	///     How much of a row the group takes in a form drawn as cards (<see cref="MokaSettingsAppearance.Cards" />). One
	///     column by default.
	/// </summary>
	[Parameter]
	public MokaSettingsSpan Span { get; set; } = MokaSettingsSpan.Auto;

	/// <summary>The fields.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>The tag beside the title of an <see cref="Advanced" /> group.</summary>
	[Parameter]
	public string AdvancedText { get; set; } = "Advanced";

	/// <inheritdoc />
	protected override string RootClass => "moka-settings-group";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-settings-group--full", Span == MokaSettingsSpan.Full)
		.AddClass(Class)
		.Build();

	private bool Showing => !Advanced || Form is null || Form.AdvancedShowing;

	// A bare fragment would resolve against the document's base address, not the page.
	private string? AnchorHref => string.IsNullOrEmpty(Id)
		? null
		: $"{Navigation.ToBaseRelativePath(Navigation.Uri).Split('#')[0]}#{Uri.EscapeDataString(Id)}";

	private string? FieldsStyle => new StyleBuilder()
		.AddStyle("--_settings-columns", Math.Max(1, Columns).ToString(CultureInfo.InvariantCulture), Columns != 2)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		Form?.ReportGroup(this, Advanced);
	}

	/// <inheritdoc />
	protected override ValueTask DisposeAsyncCore()
	{
		Form?.ReportGroup(this, false);
		return base.DisposeAsyncCore();
	}

	internal void Refresh() => StateHasChanged();
}
