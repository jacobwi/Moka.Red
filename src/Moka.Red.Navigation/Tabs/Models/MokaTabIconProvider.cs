using Microsoft.AspNetCore.Components;

namespace Moka.Red.Navigation.Tabs.Models;

/// <summary>
///     Provides customizable icon rendering for the tab system's built-in icons (close, pin, group toggle).
///     Register as a scoped service to override default SVG icons.
/// </summary>
public class MokaTabIconProvider
{
	/// <summary>
	///     Gets the render fragment for the close button icon.
	/// </summary>
	public virtual RenderFragment CloseIcon => DefaultCloseIcon;

	/// <summary>
	///     Gets the render fragment for the pin button icon (unpinned state).
	/// </summary>
	public virtual RenderFragment PinIcon => DefaultPinIcon;

	/// <summary>
	///     Gets the render fragment for the pin button icon (pinned/active state).
	/// </summary>
	public virtual RenderFragment PinnedIcon => DefaultPinnedIcon;

	/// <summary>
	///     Gets the render fragment for the group expand toggle icon.
	/// </summary>
	public virtual RenderFragment GroupExpandedIcon => DefaultGroupExpandedIcon;

	/// <summary>
	///     Gets the render fragment for the group collapse toggle icon.
	/// </summary>
	public virtual RenderFragment GroupCollapsedIcon => DefaultGroupCollapsedIcon;

	#region Default Icons

	private static readonly RenderFragment DefaultCloseIcon = builder =>
	{
		builder.OpenElement(0, "svg");
		builder.AddAttribute(1, "viewBox", "0 0 16 16");
		builder.AddAttribute(2, "width", "12");
		builder.AddAttribute(3, "height", "12");
		builder.AddAttribute(4, "fill", "currentColor");
		builder.AddMarkupContent(5,
			"<path d=\"M3.72 3.72a.75.75 0 0 1 1.06 0L8 6.94l3.22-3.22a.75.75 0 1 1 1.06 1.06L9.06 8l3.22 3.22a.75.75 0 1 1-1.06 1.06L8 9.06l-3.22 3.22a.75.75 0 0 1-1.06-1.06L6.94 8 3.72 4.78a.75.75 0 0 1 0-1.06z\"/>");
		builder.CloseElement();
	};

	private static readonly RenderFragment DefaultPinIcon = builder =>
	{
		builder.OpenElement(0, "svg");
		builder.AddAttribute(1, "viewBox", "0 0 16 16");
		builder.AddAttribute(2, "width", "12");
		builder.AddAttribute(3, "height", "12");
		builder.AddAttribute(4, "fill", "currentColor");
		builder.AddMarkupContent(5,
			"<path d=\"M4.456 2.013a.75.75 0 0 1 .531.219l4.5 4.5a.75.75 0 0 1 0 1.06l-1.47 1.47 2.72 2.72a.75.75 0 1 1-1.06 1.06l-2.72-2.72-1.47 1.47a.75.75 0 0 1-1.06 0l-4.5-4.5a.75.75 0 0 1 0-1.06l3-3a.75.75 0 0 1 .53-.22zM5.28 4.22 3.56 5.94l3.5 3.5 1.72-1.72z\"/>");
		builder.CloseElement();
	};

	private static readonly RenderFragment DefaultPinnedIcon = builder =>
	{
		builder.OpenElement(0, "svg");
		builder.AddAttribute(1, "viewBox", "0 0 16 16");
		builder.AddAttribute(2, "width", "12");
		builder.AddAttribute(3, "height", "12");
		builder.AddAttribute(4, "fill", "currentColor");
		builder.AddMarkupContent(5,
			"<path d=\"M4.456 2.013a.75.75 0 0 1 .531.219l4.5 4.5a.75.75 0 0 1 0 1.06l-1.47 1.47 2.72 2.72a.75.75 0 1 1-1.06 1.06l-2.72-2.72-1.47 1.47a.75.75 0 0 1-1.06 0l-4.5-4.5a.75.75 0 0 1 0-1.06l3-3a.75.75 0 0 1 .53-.22z\"/>");
		builder.CloseElement();
	};

	private static readonly RenderFragment DefaultGroupExpandedIcon = builder =>
	{
		builder.OpenElement(0, "svg");
		builder.AddAttribute(1, "viewBox", "0 0 16 16");
		builder.AddAttribute(2, "width", "10");
		builder.AddAttribute(3, "height", "10");
		builder.AddAttribute(4, "fill", "currentColor");
		builder.AddMarkupContent(5,
			"<path d=\"M3.2 5.6a.6.6 0 0 1 .85-.05L8 9.17l3.95-3.62a.6.6 0 1 1 .8.9l-4.35 4a.6.6 0 0 1-.8 0l-4.35-4a.6.6 0 0 1-.05-.85z\"/>");
		builder.CloseElement();
	};

	private static readonly RenderFragment DefaultGroupCollapsedIcon = builder =>
	{
		builder.OpenElement(0, "svg");
		builder.AddAttribute(1, "viewBox", "0 0 16 16");
		builder.AddAttribute(2, "width", "10");
		builder.AddAttribute(3, "height", "10");
		builder.AddAttribute(4, "fill", "currentColor");
		builder.AddMarkupContent(5,
			"<path d=\"M5.6 3.2a.6.6 0 0 1 .85.05l4 4.35a.6.6 0 0 1 0 .8l-4 4.35a.6.6 0 1 1-.9-.8L9.17 8 5.55 4.05a.6.6 0 0 1 .05-.85z\"/>");
		builder.CloseElement();
	};

	#endregion
}
