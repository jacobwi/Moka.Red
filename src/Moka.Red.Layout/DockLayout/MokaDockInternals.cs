using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Moka.Red.Layout.DockLayout;

// Renders a panel's content again only when the panel's parent passed new parameters, not when the layout moved or
// hid the panel: the host around it redraws its own attributes and this skips (performance/rendering.md, "Avoid
// unnecessary rendering of component subtrees").
internal sealed class MokaDockContentGate : ComponentBase
{
	private int _rendered = -1;

	[Parameter]
	public int Version { get; set; }

	[Parameter]
	public RenderFragment? Content { get; set; }

	public static RenderFragment For(int version, RenderFragment? content) => builder =>
	{
		builder.OpenComponent<MokaDockContentGate>(0);
		builder.AddComponentParameter(1, nameof(Version), version);
		builder.AddComponentParameter(2, nameof(Content), content);
		builder.CloseComponent();
	};

	protected override bool ShouldRender() => Version != _rendered;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		_rendered = Version;
		builder.AddContent(0, Content);
	}
}

// The panels opened from code, keyed by id in one list, so each keeps its place in the render tree, and its state,
// as others open and close. Only this redraws when the list changes, not the layout and its child content.
internal sealed class MokaDockOpenedPanels : ComponentBase
{
	private int _version;
	private int _rendered = -1;

	[Parameter]
	public MokaDockLayout? Layout { get; set; }

	public static RenderFragment For(MokaDockLayout layout) => builder =>
	{
		builder.OpenComponent<MokaDockOpenedPanels>(0);
		builder.AddComponentParameter(1, nameof(Layout), layout);
		builder.CloseComponent();
	};

	public void Refresh()
	{
		_version++;
		StateHasChanged();
	}

	protected override void OnInitialized() => Layout?.AttachOpened(this);

	protected override bool ShouldRender() => _version != _rendered;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		_rendered = _version;
		if (Layout is null)
		{
			return;
		}

		foreach (MokaDockPanelOptions options in Layout.OpenedPanels)
		{
			builder.OpenComponent<MokaDockPanel>(0);
			builder.SetKey(options.Id);
			builder.AddComponentParameter(1, nameof(MokaDockPanel.Id), options.Id);
			builder.AddComponentParameter(2, nameof(MokaDockPanel.Title), options.Title);
			builder.AddComponentParameter(3, nameof(MokaDockPanel.Icon), options.Icon);
			builder.AddComponentParameter(4, nameof(MokaDockPanel.Badge), options.Badge);
			builder.AddComponentParameter(5, nameof(MokaDockPanel.BadgeTone), options.BadgeTone);
			builder.AddComponentParameter(6, nameof(MokaDockPanel.ChildContent), options.Content);
			builder.AddComponentParameter(7, nameof(MokaDockPanel.Component), options.Component);
			builder.AddComponentParameter(9, nameof(MokaDockPanel.Closable), options.Closable);
			builder.AddComponentParameter(10, nameof(MokaDockPanel.CanClose), options.CanClose);
			builder.AddComponentParameter(11, nameof(MokaDockPanel.Scrollable), options.Scrollable);
			builder.CloseComponent();
		}
	}
}

// The layout's polite status region: says where a panel went. Only this redraws when it speaks.
internal sealed class MokaDockAnnouncer : ComponentBase
{
	private string? _text;

	[Parameter]
	public MokaDockLayout? Layout { get; set; }

	public static RenderFragment For(MokaDockLayout layout) => builder =>
	{
		builder.OpenComponent<MokaDockAnnouncer>(0);
		builder.AddComponentParameter(1, nameof(Layout), layout);
		builder.CloseComponent();
	};

	public void Say(string text)
	{
		_text = text;
		StateHasChanged();
	}

	protected override void OnInitialized() => Layout?.AttachAnnouncer(this);

	protected override bool ShouldRender() => true;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		builder.OpenElement(0, "div");
		builder.AddAttribute(1, "class", "moka-visually-hidden");
		builder.AddAttribute(2, "role", "status");
		builder.AddAttribute(3, "aria-live", "polite");
		builder.AddContent(4, _text);
		builder.CloseElement();
	}
}
