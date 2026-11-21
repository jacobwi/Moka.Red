using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Theming;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Chip;

/// <summary>
///     A compact interactive element for filtering, selection, or tags.
///     Styled with the same variants as <c>MokaButton</c>; a selected chip adds a check icon and a colored fill.
/// </summary>
/// <remarks>
///     A chip with <see cref="Href" /> is a link. Otherwise a chip with <see cref="OnClick" /> or
///     <see cref="SelectedChanged" /> is a button, and a toggle button with <c>aria-pressed</c> once the parent passes
///     <see cref="Selected" /> or <see cref="SelectedChanged" />. A chip with none of them is a plain <c>span</c>. A
///     <see cref="Closable" /> chip is a <c>span</c> holding its own link or button, when it has one, beside the remove
///     button, since neither can hold another button. <c>Class</c>, <c>Style</c>, <c>Id</c> and other attributes go on the
///     outermost element.
/// </remarks>
public partial class MokaChip
{
	private bool? _lastSelected;
	private bool _selected;
	private bool _toggle;

	/// <inheritdoc />
	/// <remarks>Chips default to <see cref="MokaVariant.Soft" /> so they sit on a tinted background.</remarks>
	public override MokaVariant Variant { get; set; } = MokaVariant.Soft;

	/// <summary>Custom child content for the chip label.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Chip label text.</summary>
	[Parameter]
	public string? Text { get; set; }

	/// <summary>Leading icon.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>Small avatar URL displayed as a leading image.</summary>
	[Parameter]
	public string? Avatar { get; set; }

	/// <summary>
	///     Shows a remove button after the content, named "Remove" and the chip's <see cref="Text" /> ("Remove Blazor")
	///     by <c>aria-label</c> and a tooltip. It is a tab stop of its own. Default false.
	/// </summary>
	[Parameter]
	public bool Closable { get; set; }

	/// <summary>Fires when the remove button is clicked, or pressed with Enter or Space.</summary>
	[Parameter]
	public EventCallback OnClose { get; set; }

	/// <summary>Whether the chip is currently selected. Two-way bindable.</summary>
	[Parameter]
	public bool Selected { get; set; }

	/// <summary>Callback for when <see cref="Selected" /> changes.</summary>
	[Parameter]
	public EventCallback<bool> SelectedChanged { get; set; }

	/// <summary>
	///     After raising its change callback, the control shows what the parent passes back, so a refused change reverts.
	///     Here the callback is <see cref="SelectedChanged" />, and the chip always shows <see cref="Selected" />. Default
	///     false: the chip shows the user's pick at once and follows the parent only when it passes a different value.
	/// </summary>
	[Parameter]
	public bool Controlled { get; set; }

	/// <summary>Click event callback. Raised on a link (<see cref="Href" />) too, before the browser follows it.</summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>
	///     Makes the chip a link to this address, such as a tag's page, with the same look, hover and press. A link says
	///     nothing about being pressed, so <c>aria-pressed</c> isn't written, though <see cref="SelectedChanged" /> and
	///     <see cref="OnClick" /> still run on a click. On a <see cref="Closable" /> chip the link sits beside the remove
	///     button. A <c>javascript:</c>, <c>vbscript:</c> or <c>data:</c> URL is not rendered, and the chip is what it
	///     would be without one. A disabled link has no <c>href</c>, so it goes nowhere, and says it is a disabled link.
	/// </summary>
	[Parameter]
	public string? Href { get; set; }

	/// <summary>
	///     Where <see cref="Href" /> opens: <c>_blank</c> for a new tab, or a window's name. Such a link gets
	///     <c>rel="noopener noreferrer"</c> and <see cref="NewTabText" /> for screen readers. <c>_self</c>, <c>_parent</c>
	///     and <c>_top</c> render as given and change nothing else.
	/// </summary>
	[Parameter]
	public string? Target { get; set; }

	/// <summary>
	///     What screen readers hear after the chip's text when its <see cref="Target" /> opens another tab, after a comma.
	///     Defaults to "opens in a new tab".
	/// </summary>
	[Parameter]
	public string NewTabText { get; set; } = "opens in a new tab";

	/// <summary>
	///     A color someone gave this chip: its tint behind readable ink, and a dot in its color before the text when there
	///     is no <see cref="Icon" /> or <see cref="Avatar" />. It replaces <c>Color</c>. <c>Variant</c> still shapes it:
	///     <see cref="MokaVariant.Soft" /> and <see cref="MokaVariant.Filled" /> draw the tint,
	///     <see cref="MokaVariant.Outlined" /> a border in the label's color on no background, and
	///     <see cref="MokaVariant.Text" /> the ink alone. A selected chip keeps the label's color, with a border in it, in
	///     place of the accent. The shades follow the theme, light or dark, and <c>MokaTheme.WithLabelTone</c>.
	/// </summary>
	[Parameter]
	public MokaLabelColor? Label { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-chip";

	private MokaColor ResolvedColor => Color ?? MokaColor.Surface;

	private bool ShowsLabelDot => Label is not null && Icon is null && !_selected && string.IsNullOrEmpty(Avatar);

	private string? LinkHref => UrlValues.SafeHref(Href);

	private bool IsLink => !string.IsNullOrEmpty(LinkHref);

	private string? LinkRel => UrlValues.RelFor(Target);

	private bool OpensNewTab => IsLink && UrlValues.OpensNewTab(Target);

	private bool HasHandler => OnClick.HasDelegate || SelectedChanged.HasDelegate;

	private bool HasAction => IsLink || HasHandler;

	private string? AriaPressed => !HasHandler || !_toggle ? null : _selected ? "true" : "false";

	// No listener on a link that nothing listens to, since every listener costs a round trip.
	private EventCallback<MouseEventArgs> LinkClick => HasHandler
		? EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick)
		: default;

	private string RemoveName => string.IsNullOrEmpty(Text) ? "Remove" : $"Remove {Text}";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass($"moka-chip--{VariantToKebab(Variant)}")
		.AddClass($"moka-chip--{SizeToKebab(Size)}")
		.AddClass($"moka-chip--{ColorToKebab(ResolvedColor)}", Label is null)
		.AddClass("moka-chip--label", Label is not null)
		.AddClass("moka-chip--selected", _selected)
		.AddClass("moka-chip--closable", Closable)
		.AddClass("moka-chip--disabled", Disabled)
		.AddClass("moka-chip--static", !HasAction)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => SpacingStyle()
		.AddStyle("--_chip-label", Label is { } solid ? MokaLabelColors.Solid(solid) : null)
		.AddStyle("--_chip-label-bg", Label is { } tint ? MokaLabelColors.Background(tint) : null)
		.AddStyle("--_chip-label-ink", Label is { } ink ? MokaLabelColors.Ink(ink) : null)
		.AddStyle(Style)
		.Build();

	private MokaSize IconSize => Size switch
	{
		MokaSize.Xs => MokaSize.Xs,
		MokaSize.Sm => MokaSize.Xs,
		MokaSize.Md => MokaSize.Sm,
		MokaSize.Lg => MokaSize.Sm,
		_ => MokaSize.Xs
	};

	/// <summary>Chip has selectable toggle state that changes independently of parameters.</summary>
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	public override Task SetParametersAsync(ParameterView parameters)
	{
		// A chip whose selection the parent speaks for is a toggle button, selected or not. One with only OnClick is an
		// action, and aria-pressed="false" would call it a toggle.
		if (parameters.TryGetValue<bool>(nameof(Selected), out _) ||
		    parameters.TryGetValue<EventCallback<bool>>(nameof(SelectedChanged), out _))
		{
			_toggle = true;
		}

		return base.SetParametersAsync(parameters);
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// Uncontrolled, Selected only seeds the chip's state when the parent passes a new value, so a parent render
		// that passes the old one again cannot undo a click.
		if (Controlled || _lastSelected != Selected)
		{
			_lastSelected = Selected;
			_selected = Selected;
		}
	}

	private async Task HandleClick(MouseEventArgs args)
	{
		if (Disabled)
		{
			return;
		}

		if (SelectedChanged.HasDelegate)
		{
			if (Controlled)
			{
				await SelectedChanged.InvokeAsync(!_selected);
			}
			else
			{
				_selected = !_selected;
				await SelectedChanged.InvokeAsync(_selected);
			}
		}

		if (OnClick.HasDelegate)
		{
			await OnClick.InvokeAsync(args);
		}
	}

	private async Task HandleClose()
	{
		if (!Disabled && OnClose.HasDelegate)
		{
			await OnClose.InvokeAsync();
		}
	}
}
