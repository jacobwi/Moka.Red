using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.Feedback.CommandPalette;
using Moka.Red.Feedback.Extensions;

namespace Moka.Red.Feedback.Tests.Components.CommandPalette;

public abstract class CommandPaletteTestBase : BunitContext
{
	protected const string Module = "./_content/Moka.Red.Feedback/moka-command-palette.js";

	protected CommandPaletteTestBase()
	{
		Services.AddMokaFeedback();
		JSInterop.SetupModule("./_content/Moka.Red.Core/moka-hover.js").Mode = JSRuntimeMode.Loose;
		JSInterop.SetupModule("./_content/Moka.Red.Feedback/moka-dialog.js").Mode = JSRuntimeMode.Loose;
		PaletteModule = JSInterop.SetupModule(Module);
		PaletteModule.Setup<int>("registerShortcut", _ => true).SetResult(1);
		PaletteModule.SetupVoid("updateShortcut", _ => true).SetVoidResult();
		PaletteModule.SetupVoid("focusInput", _ => true).SetVoidResult();
		PaletteModule.SetupVoid("watchHighlight", _ => true).SetVoidResult();
		PaletteModule.SetupVoid("dispose", _ => true).SetVoidResult();
		PaletteModule.SetupVoid("openTab", _ => true).SetVoidResult();
	}

	protected BunitJSModuleInterop PaletteModule { get; }

	protected IMokaCommandPaletteService Palette => Services.GetRequiredService<IMokaCommandPaletteService>();

	protected static MokaCommand Command(string id, string title, string? group = null, Action? run = null) => new()
	{
		Id = id,
		Title = title,
		Group = group,
		OnExecuteSync = run ?? (() => { })
	};

	protected async Task<IRenderedComponent<MokaCommandPalette>> Open(
		IEnumerable<MokaCommand> commands,
		Action<ComponentParameterCollectionBuilder<MokaCommandPalette>>? parameters = null)
	{
		Palette.RegisterMany(commands);
		IRenderedComponent<MokaCommandPalette> cut = Render(parameters ?? (_ => { }));
		await cut.InvokeAsync(Palette.Open);
		return cut;
	}

	protected static IReadOnlyList<string> Titles(IRenderedComponent<MokaCommandPalette> cut) =>
		cut.FindAll(".moka-command-palette__item-title").Select(title => title.TextContent.Trim()).ToList();

	protected static IReadOnlyList<string> Headings(IRenderedComponent<MokaCommandPalette> cut) =>
		cut.FindAll(".moka-command-palette__group").Select(heading => heading.TextContent.Trim()).ToList();

	protected static string Highlighted(IRenderedComponent<MokaCommandPalette> cut) =>
		cut.Find(".moka-command-palette__item--focused .moka-command-palette__item-title").TextContent.Trim();

	protected static Task Type(IRenderedComponent<MokaCommandPalette> cut, string text) =>
		cut.Find("input").InputAsync(new ChangeEventArgs { Value = text });

	protected static Task Press(IRenderedComponent<MokaCommandPalette> cut, string key) =>
		cut.Find(".moka-command-palette").KeyDownAsync(new KeyboardEventArgs { Key = key });

	protected static IElement Status(IRenderedComponent<MokaCommandPalette> cut) => cut.Find("[role=status]");
}
