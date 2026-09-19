using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Layout.Resizable;

namespace Moka.Red.Layout.Tests.Components;

// A drag ends in OnWidthResized/OnHeightResized/OnCornerResized, called from JS. These used to
// write the Width and Height parameters: nothing rendered the new size, and the next parent render
// passed the old value back, so a MokaResizable with a one-way Width lost every drag.
public class MokaResizableTests : BunitContext
{
	private const string ResizableModule = "./_content/Moka.Red.Layout/Resizable/MokaResizable.razor.js";

	private readonly BunitJSModuleInterop _module;

	public MokaResizableTests()
	{
		_module = JSInterop.SetupModule(ResizableModule);
		_module.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public async Task DraggedWidth_RendersAndReports_AndOnlyANewParentValueReplacesIt()
	{
		string? reportedWidth = null;
		MokaResizeResult? resized = null;
		IRenderedComponent<MokaResizable> cut = Render<MokaResizable>(p => p
			.Add(x => x.Width, "300px")
			.Add(x => x.Height, "150px")
			.Add(x => x.WidthChanged, width => reportedWidth = width)
			.Add(x => x.OnResized, result => resized = result));

		await cut.InvokeAsync(() => cut.Instance.OnWidthResized(420));

		Assert.Equal("420px", Style(cut, "width"));
		Assert.Equal("420px", reportedWidth);
		Assert.Equal(new MokaResizeResult(420, 150), resized);

		cut.Render(p => p.Add(x => x.Width, "300px"));
		Assert.Equal("420px", Style(cut, "width"));

		cut.Render(p => p.Add(x => x.Width, "200px"));
		Assert.Equal("200px", Style(cut, "width"));
	}

	[Fact]
	public async Task DraggedHeight_Renders_AndSurvivesAParentRenderWithTheOldValue()
	{
		IRenderedComponent<MokaResizable> cut = Render<MokaResizable>(p => p
			.Add(x => x.Direction, MokaResizeDirection.Vertical)
			.Add(x => x.Height, "120px"));

		await cut.InvokeAsync(() => cut.Instance.OnHeightResized(200));
		cut.Render(p => p.Add(x => x.Height, "120px"));

		Assert.Equal("200px", Style(cut, "height"));
	}

	[Fact]
	public async Task CornerDrag_Renders_AndSurvivesAParentRenderWithTheOldValues()
	{
		IRenderedComponent<MokaResizable> cut = Render<MokaResizable>(p => p
			.Add(x => x.Direction, MokaResizeDirection.Both)
			.Add(x => x.Width, "300px")
			.Add(x => x.Height, "200px"));

		await cut.InvokeAsync(() => cut.Instance.OnCornerResized(360, 240));
		cut.Render(p => p.Add(x => x.Width, "300px").Add(x => x.Height, "200px"));

		Assert.Equal("360px", Style(cut, "width"));
		Assert.Equal("240px", Style(cut, "height"));
	}

	// The shared makeResizable resizes the track of a grid the element sits in unless told
	// otherwise, which for a MokaResizable inside a grid resized the wrong thing.
	[Fact]
	public void EdgeHandles_SizeTheElementItself()
	{
		Render<MokaResizable>(p => p.Add(x => x.Direction, MokaResizeDirection.Both));

		IReadOnlyList<JSRuntimeInvocation> attached = _module.Invocations["makeResizable"];
		Assert.Equal(2, attached.Count);
		Assert.All(attached, invocation =>
			Assert.Contains("\"target\":\"element\"", JsonSerializer.Serialize(invocation.Arguments[3]), StringComparison.Ordinal));
	}

	// Each handle kept a flag that said "attached" for good. A handle Direction removed and brought
	// back is a new element, and it never got a listener. (The markup only carries an element's
	// reference id in the render that created it, so each check follows the render that brings
	// the handle back.)
	[Fact]
	public void DirectionChanges_SetUpEveryNewHandle()
	{
		IRenderedComponent<MokaResizable> cut = Render<MokaResizable>(p => p
			.Add(x => x.Direction, MokaResizeDirection.Horizontal));

		cut.Render(p => p.Add(x => x.Direction, MokaResizeDirection.Vertical));
		cut.Render(p => p.Add(x => x.Direction, MokaResizeDirection.Horizontal));
		Assert.Contains(HandleId(cut, "right"), AttachedHandles("makeResizable"));

		cut.Render(p => p.Add(x => x.Direction, MokaResizeDirection.Both));
		Assert.Contains(HandleId(cut, "bottom"), AttachedHandles("makeResizable"));
		Assert.Contains(HandleId(cut, "corner"), AttachedHandles("makeCornerResizable"));

		cut.Render(p => p.Add(x => x.Direction, MokaResizeDirection.Horizontal));
		cut.Render(p => p.Add(x => x.Direction, MokaResizeDirection.Both));
		Assert.Contains(HandleId(cut, "bottom"), AttachedHandles("makeResizable"));
		Assert.Contains(HandleId(cut, "corner"), AttachedHandles("makeCornerResizable"));
	}

	// The limits were copied into the listener once, at the first render, so new ones never
	// reached the drag.
	[Fact]
	public void NewLimits_ReachTheDrag()
	{
		IRenderedComponent<MokaResizable> cut = Render<MokaResizable>(p => p
			.Add(x => x.MinWidth, "100px")
			.Add(x => x.MaxWidth, "500px"));

		cut.Render(p => p.Add(x => x.MinWidth, "250px"));

		JSRuntimeInvocation removed = Assert.Single(_module.Invocations["removeResizable"]);
		IReadOnlyList<JSRuntimeInvocation> attached = _module.Invocations["makeResizable"];
		Assert.Equal(2, attached.Count);
		Assert.Equal(
			Assert.IsType<ElementReference>(attached[0].Arguments[2]).Id,
			Assert.IsType<ElementReference>(removed.Arguments[0]).Id);
		string options = JsonSerializer.Serialize(attached[1].Arguments[3]);
		Assert.Contains("\"min\":\"250px\"", options, StringComparison.Ordinal);
		Assert.Contains("\"max\":\"500px\"", options, StringComparison.Ordinal);

		// A render with the same limits leaves the listener alone.
		cut.Render(p => p.Add(x => x.MinWidth, "250px"));
		Assert.Equal(2, _module.Invocations["makeResizable"].Count);
	}

	// The handles could not be reached or used from the keyboard. The edge handles are window
	// splitters now; moka-drag.js does the keys and keeps their values. The corner handle adds
	// nothing the two edges do not, so it stays pointer only.
	[Fact]
	public void EdgeHandles_AreNamedFocusableSeparators_AndTheCornerIsPointerOnly()
	{
		IRenderedComponent<MokaResizable> cut = Render<MokaResizable>(p => p
			.Add(x => x.Direction, MokaResizeDirection.Both)
			.Add(x => x.Id, "panel"));

		IElement right = cut.Find(".moka-resizable__handle--right");
		Assert.Equal("separator", right.GetAttribute("role"));
		Assert.Equal("0", right.GetAttribute("tabindex"));
		Assert.Equal("vertical", right.GetAttribute("aria-orientation"));
		Assert.Equal("Resize width", right.GetAttribute("aria-label"));
		Assert.Equal("panel", right.GetAttribute("aria-controls"));

		IElement bottom = cut.Find(".moka-resizable__handle--bottom");
		Assert.Equal("separator", bottom.GetAttribute("role"));
		Assert.Equal("0", bottom.GetAttribute("tabindex"));
		Assert.Equal("horizontal", bottom.GetAttribute("aria-orientation"));
		Assert.Equal("Resize height", bottom.GetAttribute("aria-label"));

		IElement corner = cut.Find(".moka-resizable__handle--corner");
		Assert.Equal("true", corner.GetAttribute("aria-hidden"));
		Assert.False(corner.HasAttribute("tabindex"));
		Assert.False(corner.HasAttribute("role"));

		Assert.All(_module.Invocations["makeResizable"], invocation =>
			Assert.Contains("\"keyboard\":true", JsonSerializer.Serialize(invocation.Arguments[3]), StringComparison.Ordinal));
	}

	// The classes were put together with a ternary in the markup, which left a stray space.
	[Theory]
	[InlineData(true, "moka-resizable__handle moka-resizable__handle--right")]
	[InlineData(false, "moka-resizable__handle moka-resizable__handle--right moka-resizable__handle--hidden")]
	public void HandleClasses_ComeFromCssBuilder(bool showHandle, string classes)
	{
		IRenderedComponent<MokaResizable> cut = Render<MokaResizable>(p => p.Add(x => x.ShowHandle, showHandle));

		Assert.Equal(classes, cut.Find(".moka-resizable__handle--right").GetAttribute("class"));
	}

	private HashSet<string?> AttachedHandles(string identifier) =>
		_module.Invocations[identifier]
			.Select(invocation => (string?)Assert.IsType<ElementReference>(invocation.Arguments[2]).Id)
			.ToHashSet();

	private static string? HandleId(IRenderedComponent<MokaResizable> cut, string edge) =>
		cut.Find($".moka-resizable__handle--{edge}").GetAttribute("blazor:elementReference");

	private static string? Style(IRenderedComponent<MokaResizable> cut, string property)
	{
		string style = cut.Find(".moka-resizable").GetAttribute("style") ?? "";
		foreach (string declaration in style.Split(';'))
		{
			int colon = declaration.IndexOf(':', StringComparison.Ordinal);
			if (colon > 0 && string.Equals(declaration[..colon].Trim(), property, StringComparison.Ordinal))
			{
				return declaration[(colon + 1)..].Trim();
			}
		}

		return null;
	}
}
