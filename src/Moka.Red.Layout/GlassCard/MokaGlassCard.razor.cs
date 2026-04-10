using System.Globalization;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.GlassCard;

/// <summary>
///     A glassmorphism card with backdrop-filter blur, translucent background,
///     and optional animated border glow. Designed for layering over
///     grid backgrounds, images, or gradient surfaces.
/// </summary>
public partial class MokaGlassCard : MokaVisualComponentBase
{
	/// <summary>Card content.</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Header content slot.</summary>
	[Parameter]
	public RenderFragment? Header { get; set; }

	/// <summary>Footer content slot.</summary>
	[Parameter]
	public RenderFragment? Footer { get; set; }

	/// <summary>Simple text title for the header.</summary>
	[Parameter]
	public string? Title { get; set; }

	/// <summary>Subtitle below the title.</summary>
	[Parameter]
	public string? Subtitle { get; set; }

	/// <summary>Backdrop blur amount in pixels. Default 12.</summary>
	[Parameter]
	public int Blur { get; set; } = 12;

	/// <summary>Background opacity (0–100%). Default 8 (very translucent).</summary>
	[Parameter]
	public int BackgroundOpacity { get; set; } = 8;

	/// <summary>Background tint color. Defaults to surface color.</summary>
	[Parameter]
	public string? Tint { get; set; }

	/// <summary>Border color. Defaults to outline-variant.</summary>
	[Parameter]
	public string? BorderColor { get; set; }

	/// <summary>When true, shows a subtle animated glow on the border. Default false.</summary>
	[Parameter]
	public bool Glow { get; set; }

	/// <summary>Glow color. Defaults to primary.</summary>
	[Parameter]
	public string? GlowColor { get; set; }

	/// <summary>When true, the card is interactive (hover effects). Default false.</summary>
	[Parameter]
	public bool Clickable { get; set; }

	/// <summary>Click event handler.</summary>
	[Parameter]
	public EventCallback OnClick { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-glass-card";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-glass-card--clickable", Clickable)
		.AddClass("moka-glass-card--glow", Glow)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override string? CssStyle
	{
		get
		{
			var tint = Tint ?? "var(--moka-color-surface)";
			var border = BorderColor ?? "var(--moka-color-outline-variant)";
			var glow = GlowColor ?? "var(--moka-color-primary)";

			return new StyleBuilder()
				.AddStyle("--glass-blur", $"{Blur}px")
				.AddStyle("--glass-tint", tint)
				.AddStyle("--glass-opacity", (BackgroundOpacity / 100.0).ToString("F2", CultureInfo.InvariantCulture))
				.AddStyle("--glass-border", border)
				.AddStyle("--glass-glow", glow)
				.AddStyle("border-radius", ResolvedRounding)
				.AddStyle("margin", ResolvedMargin)
				.AddStyle("padding", ResolvedPadding)
				.AddStyle(Style)
				.Build();
		}
	}

	private bool HasHeader => Header is not null || Title is not null;

	private async Task HandleClick()
	{
		if (Clickable && OnClick.HasDelegate)
		{
			await OnClick.InvokeAsync();
		}
	}

	private async Task HandleKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
	{
		if (Clickable && e.Key is "Enter" or " ")
		{
			await HandleClick();
		}
	}
}
