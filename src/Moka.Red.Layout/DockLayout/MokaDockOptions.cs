using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;
using Moka.Red.Primitives.Badge;

namespace Moka.Red.Layout.DockLayout;

/// <summary>How a group shows its panels' tabs. See <see cref="MokaDockLayout.TabStyle" /> and <see cref="MokaDockGroup.TabStyle" />.</summary>
public enum MokaDockTabStyle
{
	/// <summary>Plain tabs with a line under the one showing, as <c>MokaTabs</c> draws them.</summary>
	Underline,

	/// <summary>Boxed tabs joined to the panel, each with its own close button, as an editor's documents.</summary>
	Editor,

	/// <summary>Rounded pills, the one showing filled.</summary>
	Pills,

	/// <summary>The showing panel's title, with a list of the others to switch to.</summary>
	Switcher
}

/// <summary>What shows where a dragged panel will land. See <see cref="MokaDockLayout.DropIndicator" />.</summary>
public enum MokaDockDropIndicator
{
	/// <summary>The half or the middle of the group under the pointer lights up.</summary>
	Zones,

	/// <summary>Five targets in the middle of the group under the pointer, and four at the layout's edges.</summary>
	Compass
}

/// <summary>How a layout narrower than <see cref="MokaDockLayout.NarrowBelow" /> shows its panels.</summary>
public enum MokaDockNarrowMode
{
	/// <summary>The main area fills the layout, and the other panels open as bottom sheets from a bar of their icons.</summary>
	Sheets,

	/// <summary>Every panel as a tab of one group.</summary>
	Tabs,

	/// <summary>The groups one above the other, each folding open and shut.</summary>
	Stack
}

/// <summary>The buttons a group's header can show. See <see cref="MokaDockLayout.Controls" />.</summary>
[Flags]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "A set of header controls.")]
public enum MokaDockControls
{
	/// <summary>No buttons.</summary>
	None = 0,

	/// <summary>Float the group in a window of its own, and dock it again.</summary>
	Floating = 1,

	/// <summary>Fold a collapsible group to a strip.</summary>
	Collapse = 2,

	/// <summary>Make the group fill the layout; Escape brings it back.</summary>
	Maximize = 4,

	/// <summary>Close the showing panel, when it can be closed.</summary>
	Close = 8,

	/// <summary>A menu of everything the panel can do: float, move, pin to a side, open in a window, close.</summary>
	Menu = 16,

	/// <summary>The buttons a layout shows unless it says otherwise.</summary>
	Default = Floating | Collapse | Maximize | Menu,

	/// <summary>Every button.</summary>
	All = Floating | Collapse | Maximize | Close | Menu
}

/// <summary>A panel opened from code with <see cref="MokaDockLayout.OpenPanelAsync" />, such as a document.</summary>
public sealed record MokaDockPanelOptions
{
	/// <summary>The panel's id, unique in the layout.</summary>
	public required string Id { get; init; }

	/// <summary>The title on the panel's tab.</summary>
	public string? Title { get; init; }

	/// <summary>The icon on the panel's tab.</summary>
	public MokaIconDefinition? Icon { get; init; }

	/// <summary>A count on the panel's tab.</summary>
	public int? Badge { get; init; }

	/// <summary>The count's tone.</summary>
	public MokaBadgeTone BadgeTone { get; init; } = MokaBadgeTone.Count;

	/// <summary>The panel's content. <see cref="Component" /> takes its place when set.</summary>
	public RenderFragment? Content { get; init; }

	/// <summary>
	///     A component to show as the panel's content. Only a panel shown this way can open in another browser window,
	///     since the window renders it in a circuit of its own.
	/// </summary>
	public MokaDockComponent? Component { get; init; }

	/// <summary>Whether the panel can be closed. Panels opened from code can be, unless this says otherwise.</summary>
	public bool Closable { get; init; } = true;

	/// <summary>Asked before the panel closes; false keeps it open, such as a document with unsaved changes.</summary>
	public Func<Task<bool>>? CanClose { get; init; }

	/// <summary>Whether the content scrolls when it overflows. False clips it, for content that scrolls itself.</summary>
	public bool Scrollable { get; init; } = true;
}

/// <summary>
///     A component and its parameters, as a panel's content. A panel shown this way can open in another browser
///     window: the window renders the component in a circuit of its own. Make one with <see cref="Create{TComponent}" />.
/// </summary>
public sealed record MokaDockComponent
{
	/// <summary>Makes the content from a component type and its parameters.</summary>
	/// <param name="type">The component type.</param>
	/// <param name="parameters">Its parameters, by name.</param>
	public MokaDockComponent(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type,
		IReadOnlyDictionary<string, object?>? parameters = null)
	{
		ArgumentNullException.ThrowIfNull(type);
		Type = type;
		Parameters = parameters;
	}

	/// <summary>The component type.</summary>
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
	public Type Type { get; }

	/// <summary>Its parameters, by name.</summary>
	public IReadOnlyDictionary<string, object?>? Parameters { get; init; }

	/// <summary>Makes the content from a component and its parameters.</summary>
	/// <typeparam name="TComponent">The component.</typeparam>
	/// <param name="parameters">Its parameters, by name.</param>
	/// <returns>The content.</returns>
	public static MokaDockComponent Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(
		IReadOnlyDictionary<string, object?>? parameters = null)
		where TComponent : IComponent => new(typeof(TComponent), parameters);

	internal RenderFragment Render() => builder =>
	{
		builder.OpenComponent(0, Type);
		if (Parameters is not null)
		{
			foreach (KeyValuePair<string, object?> parameter in Parameters)
			{
				builder.AddComponentParameter(1, parameter.Key, parameter.Value);
			}
		}

		builder.CloseComponent();
	};
}

/// <summary>A named layout the person can switch to. See <see cref="MokaDockLayout.Presets" />.</summary>
/// <param name="Name">The name, unique among the presets.</param>
/// <param name="State">The layout. Panels it doesn't mention join it where the markup has them.</param>
public sealed record MokaDockPreset(string Name, MokaDockState State);

/// <summary>What happened to a panel, for the layout's panel events.</summary>
/// <param name="Panel">The panel's id.</param>
/// <param name="Location">Where the panel is now.</param>
public sealed record MokaDockPanelChange(string Panel, MokaDockPanelLocation Location);
