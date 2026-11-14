using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Moka.Red.Layout.DockLayout;

namespace Moka.Red.Layout.Tests.Components;

// Builds dock layout markup in C#, since this test project compiles no Razor: Row(...), Group(...), Panel(...).
internal static class DockKit
{
	public static RenderFragment Row(params RenderFragment[] parts) => Container<MokaDockRow>(null, null, parts);

	public static RenderFragment Row(string id, params RenderFragment[] parts) => Container<MokaDockRow>(id, null, parts);

	public static RenderFragment Column(params RenderFragment[] parts) => Container<MokaDockColumn>(null, null, parts);

	public static RenderFragment Group(string id, string? size, params RenderFragment[] panels) =>
		Container<MokaDockGroup>(id, size, panels);

	public static RenderFragment Group(string id, string? size, IDictionary<string, object?> extra, params RenderFragment[] panels) =>
		Container<MokaDockGroup>(id, size, panels, extra);

	public static RenderFragment Content(RenderFragment? body = null) => builder =>
	{
		builder.OpenComponent<MokaDockContent>(0);
		builder.AddComponentParameter(1, nameof(MokaDockContent.ChildContent), body ?? Text("main"));
		builder.CloseComponent();
	};

	public static RenderFragment Panel(string id, string? title = null, RenderFragment? body = null,
		IDictionary<string, object?>? extra = null) => builder =>
	{
		builder.OpenComponent<MokaDockPanel>(0);
		builder.AddComponentParameter(1, nameof(MokaDockPanel.Id), id);
		builder.AddComponentParameter(2, nameof(MokaDockPanel.Title), title ?? id);
		builder.AddComponentParameter(3, nameof(MokaDockPanel.ChildContent), body ?? Text(id + " body"));
		if (extra is not null)
		{
			foreach ((string name, object? value) in extra)
			{
				builder.AddComponentParameter(4, name, value);
			}
		}

		builder.CloseComponent();
	};

	public static RenderFragment Parts(params RenderFragment[] parts) => builder =>
	{
		foreach (RenderFragment part in parts)
		{
			builder.AddContent(0, part);
		}
	};

	public static RenderFragment Text(string text) => builder => builder.AddContent(0, text);

	public static RenderFragment Probe(string name) => builder =>
	{
		builder.OpenComponent<DockProbe>(0);
		builder.AddComponentParameter(1, nameof(DockProbe.Name), name);
		builder.CloseComponent();
	};

	// Row[ side(240px: explorer, search) | Column[ content | bottom(180px: terminal, problems) ] ], each body a probe.
	public static RenderFragment Ide() => Row("row",
		Group("side", "240px", Panel("explorer", "Explorer", Probe("explorer")), Panel("search", "Search", Probe("search"))),
		Column(
			Content(Probe("content")),
			Group("bottom", "180px", Panel("terminal", "Terminal", Probe("terminal")), Panel("problems", "Problems", Probe("problems")))));

	private static RenderFragment Container<T>(string? id, string? size, RenderFragment[] parts,
		IDictionary<string, object?>? extra = null) where T : MokaDockContainer => builder =>
	{
		builder.OpenComponent<T>(0);
		if (id is not null)
		{
			builder.AddComponentParameter(1, nameof(MokaDockContainer.Id), id);
		}

		if (size is not null)
		{
			builder.AddComponentParameter(2, nameof(MokaDockContainer.Size), size);
		}

		if (extra is not null)
		{
			foreach ((string name, object? value) in extra)
			{
				builder.AddComponentParameter(3, name, value);
			}
		}

		builder.AddComponentParameter(4, nameof(MokaDockContainer.ChildContent), Parts(parts));
		builder.CloseComponent();
	};
}

// A body that counts its clicks and its instances: the same instance after a move means the content kept its state.
public sealed class DockProbe : ComponentBase
{
	private int _count;

	public static int Created { get; set; }

	[Parameter]
	public string? Name { get; set; }

	public int Count => _count;

	protected override void OnInitialized() => Created++;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		builder.OpenElement(0, "button");
		builder.AddAttribute(1, "class", "probe");
		builder.AddAttribute(2, "data-probe", Name);
		builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, () => { _count++; }));
		builder.AddContent(4, _count);
		builder.CloseElement();
	}
}
