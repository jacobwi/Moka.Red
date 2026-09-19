using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;

namespace Moka.Red.Tests.Shared;

/// <summary>
///     One visual component for the spacing checks: it is rendered once with <c>MarginValue</c>,
///     <c>PaddingValue</c> and <c>RoundedValue</c> set and once without them. Linked into every
///     component test project, so the checks read the same everywhere.
/// </summary>
internal sealed class SpacingCase
{
	private const string Margin = "margin: 7px";
	private const string Padding = "padding: 5px";
	private const string Radius = "border-radius: 3px";

	/// <param name="component">The closed component type to render.</param>
	/// <param name="defaultStyles">
	///     Every style attribute of the rendering without the spacing parameters, in document order,
	///     joined by <c>" | "</c>. Recorded from the components before they applied the parameters.
	/// </param>
	/// <param name="parameters">The parameters the component needs to render its box.</param>
	public SpacingCase(Type component, string defaultStyles, params (string Name, object? Value)[] parameters)
	{
		Component = component;
		DefaultStyles = defaultStyles;
		Parameters = parameters;
	}

	/// <summary>The closed component type to render.</summary>
	public Type Component { get; }

	/// <summary>The style attributes of the rendering without the spacing parameters.</summary>
	public string DefaultStyles { get; }

	/// <summary>The parameters the component needs to render its box.</summary>
	public IReadOnlyList<(string Name, object? Value)> Parameters { get; }

	/// <summary>The outermost element, for a component that renders another element first. Null means the first element.</summary>
	public string? Root { get; init; }

	/// <summary>The element that draws the component's box and takes the padding. Null means the root.</summary>
	public string? Box { get; init; }

	/// <summary>The element that takes the border radius, when it is not <see cref="Box" />.</summary>
	public string? RadiusOn { get; init; }

	/// <summary>An element to click first, for a component whose box only renders once it is open.</summary>
	public string? Open { get; init; }

	/// <summary>Renders the component, with the three spacing parameters or without them.</summary>
	public RenderFragment Render(bool withSpacing) => builder =>
	{
		builder.OpenComponent(0, Component);
		int sequence = 1;
		foreach ((string name, object? value) in Parameters)
		{
			builder.AddAttribute(sequence++, name, value);
		}

		if (withSpacing)
		{
			builder.AddAttribute(sequence++, "MarginValue", "7px");
			builder.AddAttribute(sequence++, "PaddingValue", "5px");
			builder.AddAttribute(sequence, "RoundedValue", "3px");
		}

		builder.CloseComponent();
	};

	/// <summary>Clicks <see cref="Open" /> when the case has one.</summary>
	public void OpenIfNeeded(IRenderedComponent<ContainerFragment> cut)
	{
		if (Open is not null)
		{
			cut.Find(Open).Click();
		}
	}

	/// <summary>
	///     Checks that the margin sits on the outermost element, the padding on the box and the radius
	///     on its element, and that neither the margin nor the padding is applied twice.
	/// </summary>
	public void AssertSpacing(IRenderedComponent<ContainerFragment> cut)
	{
		IElement root = Root is null ? cut.Nodes.OfType<IElement>().First() : cut.Find(Root);
		IElement box = Box is null ? root : cut.Find(Box);
		IElement radius = RadiusOn is null ? box : cut.Find(RadiusOn);

		Assert.Contains(Margin, StyleOf(root), StringComparison.Ordinal);
		Assert.Contains(Padding, StyleOf(box), StringComparison.Ordinal);
		Assert.Contains(Radius, StyleOf(radius), StringComparison.Ordinal);
		Assert.Single(cut.FindAll("[style]"), e => StyleOf(e).Contains(Margin, StringComparison.Ordinal));
		Assert.Single(cut.FindAll("[style]"), e => StyleOf(e).Contains(Padding, StringComparison.Ordinal));
	}

	/// <summary>Every style attribute of the rendering, in document order, joined by <c>" | "</c>.</summary>
	public static string Styles(IRenderedComponent<ContainerFragment> cut) =>
		string.Join(" | ", cut.FindAll("[style]").Select(StyleOf));

	/// <summary>The style MokaIcon writes on its svg for an icon of <paramref name="size" />.</summary>
	public static string IconStyle(string size) =>
		$"display:inline-block;vertical-align:middle;flex-shrink:0;width: {size}; height: {size}";

	private static string StyleOf(IElement element) => element.GetAttribute("style") ?? string.Empty;
}
