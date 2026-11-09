using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.ContextMenu;
using Moka.Red.Data.FileBrowser;

namespace Moka.Red.Data.Tests.Components;

internal sealed record FileEntry(string Key, string Name, bool Folder = false, long? Size = null, DateTimeOffset? Modified = null);

// A small folder tree and the browser rendered over it. The keys arrive through the script, which calls the browser's
// [JSInvokable] methods; these tests call them the same way.
public abstract class MokaFileBrowserKit : BunitContext
{
	internal const string ModulePath = "./_content/Moka.Red.Data/FileBrowser/MokaFileBrowser.razor.js";

	private protected readonly BunitJSModuleInterop Module;

	private protected readonly Dictionary<string, FileEntry[]> Tree = new(StringComparer.Ordinal)
	{
		["/"] =
		[
			new FileEntry("/docs", "Docs", true),
			new FileEntry("/a.txt", "a.txt", Size: 10),
			new FileEntry("/b.txt", "b.txt", Size: 30),
			new FileEntry("/c.txt", "c.txt", Size: 20)
		],
		["/docs"] =
		[
			new FileEntry("/docs/sub", "Sub", true),
			new FileEntry("/docs/x.md", "x.md", Size: 1)
		],
		["/docs/sub"] = [],
		["/empty"] = []
	};

	private protected MokaFileBrowserKit()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Module = JSInterop.SetupModule(ModulePath);
		Module.Setup<bool>("bind", _ => true).SetResult(false);
		Module.SetupVoid("unbind", _ => true).SetVoidResult();
		Module.SetupVoid("measure", _ => true).SetVoidResult();
		Module.SetupVoid("focusListIfLost", _ => true).SetVoidResult();
		Module.SetupVoid("pickFiles", _ => true).SetVoidResult();
		Module.SetupVoid("revealLine", _ => true).SetVoidResult();
		JSInterop.SetupModule("./_content/Moka.Red.Forms/InlineEdit/MokaInlineEdit.razor.js").SetupVoid("open", _ => true).SetVoidResult();
	}

	private protected Task<IReadOnlyList<FileEntry>> LoadTree(string folder, CancellationToken token) =>
		Task.FromResult<IReadOnlyList<FileEntry>>(Tree[folder]);

	private protected static MokaFileInfo Describe(FileEntry entry) =>
		new(entry.Key, entry.Name, entry.Folder, entry.Size, entry.Modified);

	private protected static IReadOnlyList<MokaFileCrumb> CrumbsFor(string folder)
	{
		List<MokaFileCrumb> crumbs = [new MokaFileCrumb("/", "Home")];
		string path = "";
		foreach (string part in folder.Split('/', StringSplitOptions.RemoveEmptyEntries))
		{
			path += $"/{part}";
			crumbs.Add(new MokaFileCrumb(path, part));
		}

		return crumbs;
	}

	private protected IRenderedComponent<MokaFileBrowser<FileEntry>> RenderBrowser(
		Action<ComponentParameterCollectionBuilder<MokaFileBrowser<FileEntry>>>? extra = null,
		string folder = "/",
		bool ownLoad = false,
		bool ownDescribe = false) =>
		Render<MokaFileBrowser<FileEntry>>(p =>
		{
			p.Add(x => x.Folder, folder);
			if (!ownLoad)
			{
				p.Add(x => x.Load, LoadTree);
			}

			if (!ownDescribe)
			{
				p.Add(x => x.Describe, Describe);
			}

			p.Add(x => x.Crumbs, CrumbsFor(folder));
			extra?.Invoke(p);
		});

	private protected static async Task<int> Key(
		IRenderedComponent<MokaFileBrowser<FileEntry>> cut,
		string key,
		bool ctrl = false,
		bool shift = false,
		bool alt = false,
		int pageRows = 10)
	{
		int line = -1;
		await cut.InvokeAsync(async () => line = await cut.Instance.HandleKey(key, ctrl, shift, alt, pageRows));
		return line;
	}

	private protected static IElement Row(IRenderedComponent<MokaFileBrowser<FileEntry>> cut, string name) =>
		cut.FindAll("[role=option]").Single(option => option.QuerySelector(".moka-file-browser__name")?.TextContent == name);

	private protected static string[] Names(IRenderedComponent<MokaFileBrowser<FileEntry>> cut) =>
		[.. cut.FindAll(".moka-file-browser__name").Select(name => name.TextContent)];

	private protected static string[] SelectedNames(IRenderedComponent<MokaFileBrowser<FileEntry>> cut) =>
		[.. cut.FindAll("[role=option][aria-selected=true] .moka-file-browser__name").Select(name => name.TextContent)];

	private protected static void Click(IRenderedComponent<MokaFileBrowser<FileEntry>> cut, string name, bool ctrl = false, bool shift = false) =>
		Row(cut, name).Click(new MouseEventArgs { CtrlKey = ctrl, ShiftKey = shift, Detail = 1 });

	private protected static IElement Button(IRenderedComponent<MokaFileBrowser<FileEntry>> cut, string label) =>
		cut.Find($"button[aria-label='{label}']");

	private protected static void Type(IElement element, string text) => element.Input(text);

	private protected static void Press(IElement element, string key) => element.KeyDown(new KeyboardEventArgs { Key = key });

	private protected static void ClickOn(IElement element) => element.Click();

	private protected static void DoubleClickOn(IElement element) => element.DoubleClick();

	private protected static void ContextMenuOn(IElement element, MouseEventArgs args) => element.ContextMenu(args);

	private protected FakeMenuService UseMenuService()
	{
		FakeMenuService service = new();
		Services.AddSingleton<IMokaContextMenuService>(service);
		return service;
	}

	internal sealed class FakeMenuService : IMokaContextMenuService
	{
#pragma warning disable CS0067 // The interface has it, and nothing here needs to raise it.
		public event Action? OnChanged;
#pragma warning restore CS0067

		public bool Visible { get; private set; }

		public double X => 0;

		public double Y => 0;

		public IReadOnlyList<MokaContextMenuItem> Items { get; private set; } = [];

		public bool HasHost => true;

		public void Show(double x, double y, IReadOnlyList<MokaContextMenuItem> items) => Open(items);

		public void Show(MouseEventArgs mouseEvent, IReadOnlyList<MokaContextMenuItem> items) => Open(items);

		public void Close() => Visible = false;

		public MokaContextMenuItem Named(string text) => Items.Single(item => item.Text == text);

		private void Open(IReadOnlyList<MokaContextMenuItem> items)
		{
			Items = items;
			Visible = true;
		}
	}
}
