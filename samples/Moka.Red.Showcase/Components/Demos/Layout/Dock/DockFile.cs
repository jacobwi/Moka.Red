namespace Moka.Red.Showcase.Components.Demos.Layout.Dock;

// A plain event rather than an EventCallback: a document can open in another window, which runs in a circuit of its
// own, so whoever listens marshals onto its own dispatcher.
public sealed class DockFile(string name, string text)
{
	public event EventHandler? DirtyChanged;

	public string Name { get; } = name;

	public string Text { get; private set; } = text;

	public string SavedText { get; private set; } = text;

	public bool Dirty => !string.Equals(Text, SavedText, StringComparison.Ordinal);

	public string PanelId => $"doc:{Name}";

	public void SetText(string text)
	{
		bool wasDirty = Dirty;
		Text = text;
		RaiseIfFlipped(wasDirty);
	}

	public void Save()
	{
		bool wasDirty = Dirty;
		SavedText = Text;
		RaiseIfFlipped(wasDirty);
	}

	public void Revert()
	{
		bool wasDirty = Dirty;
		Text = SavedText;
		RaiseIfFlipped(wasDirty);
	}

	public static IReadOnlyList<DockFile> Samples() =>
	[
		new("report.md", "# Quarterly report\n\nRevenue grew in every region.\nCosts held flat.\n"),
		new("budget.csv", "item,amount\nservers,1200\nlicenses,340\ntravel,180\n"),
		new("notes.txt", "Call the printer about the new layout.\nSend the draft on Friday.\n"),
		new("readme.md", "# Build scripts\n\nRun build.sh to compile and pack.\n"),
		new("settings.json", "{\n  \"theme\": \"dark\",\n  \"autosave\": true\n}\n")
	];

	private void RaiseIfFlipped(bool wasDirty)
	{
		if (wasDirty != Dirty)
		{
			DirtyChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
