using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.BentoGrid;

/// <summary>
///     A single cell in a <see cref="MokaBentoGrid" />.
///     Configure column/row span to create the asymmetric bento layout.
/// </summary>
public partial class MokaBentoItem : MokaVisualComponentBase
{
	private const string KeysModule = "./_content/Moka.Red.Core/moka-keys.js";

	private ElementReference _element;
	private bool _keysBound;

	/// <summary>Cell content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Number of columns this cell spans. Default 1.</summary>
	[Parameter]
	public int ColSpan { get; set; } = 1;

	/// <summary>Number of rows this cell spans. Default 1.</summary>
	[Parameter]
	public int RowSpan { get; set; } = 1;

	/// <summary>Explicit column start position (1-based). Default auto.</summary>
	[Parameter]
	public int? ColStart { get; set; }

	/// <summary>Explicit row start position (1-based). Default auto.</summary>
	[Parameter]
	public int? RowStart { get; set; }

	/// <summary>Simple text title for the cell header.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Description text below the title.</summary>
	[Parameter]
	public string? Description { get; set; }

	/// <summary>When true, the cell is interactive (hover effects). Default false.</summary>
	[Parameter]
	public bool Clickable { get; set; }

	/// <summary>Click event handler.</summary>
	[Parameter]
	public EventCallback OnClick { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-bento-item";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-bento-item--clickable", Clickable)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("grid-column", $"span {ColSpan}", ColSpan > 1 && ColStart is null)
		.AddStyle("grid-row", $"span {RowSpan}", RowSpan > 1 && RowStart is null)
		.AddStyle("grid-column", $"{ColStart} / span {ColSpan}", ColStart.HasValue)
		.AddStyle("grid-row", $"{RowStart} / span {RowSpan}", RowStart.HasValue)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private int? TabIndex => Clickable ? 0 : null;

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// Enter and Space are handled in the browser, for the card itself only. A .NET key
		// handler also fired for keys pressed in a button or input inside the card, and Space
		// scrolled the page as well.
		if (Clickable && !_keysBound)
		{
			_keysBound = true;
			await SafeModuleInvokeVoidAsync(KeysModule, "bindActivation", _element);
		}
	}

	private async Task HandleClick()
	{
		if (Clickable && OnClick.HasDelegate)
		{
			await OnClick.InvokeAsync();
		}
	}
}
