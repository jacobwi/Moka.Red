namespace Moka.Red.Primitives.LogViewer;

/// <summary>When a <see cref="MokaLogViewer" /> searches for what the reader types.</summary>
public enum MokaLogSearchMode
{
	/// <summary>As the reader types, once <see cref="MokaLogViewer.SearchDebounce" /> has passed when that is set.</summary>
	Live,

	/// <summary>When the reader presses Enter in the search box or its search button.</summary>
	OnSubmit
}
