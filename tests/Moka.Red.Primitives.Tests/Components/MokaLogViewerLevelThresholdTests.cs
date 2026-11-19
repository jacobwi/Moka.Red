using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Moka.Red.Primitives.LogViewer;

namespace Moka.Red.Primitives.Tests.Components;

/// <summary>The level filter as one pick of the lowest level shown, as <c>journalctl -p</c> takes one priority.</summary>
public sealed class MokaLogViewerLevelThresholdTests : BunitContext
{
	private static readonly DateTime At = new(2026, 11, 19, 9, 30, 0, DateTimeKind.Utc);

	private static readonly IReadOnlyList<MokaLogEntry> Entries =
	[
		.. Enum.GetValues<MokaLogLevel>().Select((level, i) => new MokaLogEntry(At.AddSeconds(i), level, $"{level} line"))
	];

	private readonly List<MokaLogLevel> _askedAt = [];
	private MokaLogLevel _minimum = MokaLogLevel.Trace;

	public MokaLogViewerLevelThresholdTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static IEnumerable<string> Messages(IRenderedComponent<MokaLogViewer> cut) =>
		cut.FindAll(".moka-log-viewer__message").Select(m => m.TextContent);

	private ValueTask<ItemsProviderResult<MokaLogEntry>> Provide(ItemsProviderRequest request)
	{
		_askedAt.Add(_minimum);
		MokaLogEntry[] rows = [.. Entries.Where(entry => entry.Level >= _minimum)];
		return ValueTask.FromResult(new ItemsProviderResult<MokaLogEntry>(rows.Skip(request.StartIndex).Take(request.Count), rows.Length));
	}

	[Fact]
	public void ByDefault_TheLevelsAreToggles()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p.Add(x => x.Entries, Entries));

		Assert.Equal(6, cut.FindAll(".moka-log-viewer__filter-btn").Count);
		Assert.Empty(cut.FindAll("select"));
	}

	[Fact]
	public void AtLeast_IsOneNamedListOfTheLowestLevel_InPlaceOfTheToggles()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.LevelFilter, MokaLogLevelFilter.AtLeast));

		IElement list = cut.Find("select.moka-log-viewer__minimum");
		Assert.Equal("Minimum level", list.GetAttribute("aria-label"));
		Assert.Equal("Trace", list.GetAttribute("value"));
		Assert.Equal(["Trace", "Debug", "Info", "Warning", "Error", "Fatal"],
			list.QuerySelectorAll("option").Select(o => o.GetAttribute("value")));
		Assert.Equal(["All levels", "Debug and above", "Info and above", "Warning and above", "Error and above", "Fatal only"],
			list.QuerySelectorAll("option").Select(o => o.TextContent));
		Assert.Empty(cut.FindAll(".moka-log-viewer__filter-btn"));
		Assert.Equal(6, Messages(cut).Count());
	}

	[Fact]
	public void MinimumLevel_ShowsThatLevelAndWorse()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.LevelFilter, MokaLogLevelFilter.AtLeast)
			.Add(x => x.MinimumLevel, MokaLogLevel.Warning));

		Assert.Equal(["Warning line", "Error line", "Fatal line"], Messages(cut));
		Assert.Equal("Warning", cut.Find("select").GetAttribute("value"));
	}

	[Fact]
	public void PickingALevel_FiltersAndRaisesMinimumLevelChanged()
	{
		MokaLogLevel? reported = null;
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.LevelFilter, MokaLogLevelFilter.AtLeast)
			.Add(x => x.MinimumLevelChanged, (MokaLogLevel level) => reported = level));

		cut.Find("select").Change("Error");

		Assert.Equal(MokaLogLevel.Error, reported);
		Assert.Equal(["Error line", "Fatal line"], Messages(cut));
	}

	[Fact]
	public void AParentPassingTheSameLevelAgain_KeepsTheReadersPick()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.LevelFilter, MokaLogLevelFilter.AtLeast)
			.Add(x => x.MinimumLevel, MokaLogLevel.Info));

		cut.Find("select").Change("Fatal");
		cut.Render(p => p.Add(x => x.MinimumLevel, MokaLogLevel.Info));

		Assert.Equal(["Fatal line"], Messages(cut));
	}

	[Fact]
	public void UnderAtLeast_LevelsIsLeftAlone()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.LevelFilter, MokaLogLevelFilter.AtLeast)
			.Add(x => x.Levels, new HashSet<MokaLogLevel> { MokaLogLevel.Trace })
			.Add(x => x.MinimumLevel, MokaLogLevel.Error));

		Assert.Equal(["Error line", "Fatal line"], Messages(cut));
	}

	[Fact]
	public void UnderVirtualize_TheThresholdFiltersTheRows()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.Entries, Entries)
			.Add(x => x.Virtualize, true)
			.Add(x => x.LevelFilter, MokaLogLevelFilter.AtLeast)
			.Add(x => x.MinimumLevel, MokaLogLevel.Debug));

		Assert.Equal(5, cut.FindComponent<Virtualize<MokaLogEntry>>().Instance.Items!.Count);
	}

	[Fact]
	public void AProvider_IsAskedAgainWithTheLevelPicked()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.ItemsProvider, Provide)
			.Add(x => x.LevelFilter, MokaLogLevelFilter.AtLeast)
			.Add(x => x.MinimumLevel, _minimum)
			.Add(x => x.MinimumLevelChanged, (MokaLogLevel level) => _minimum = level));
		int before = _askedAt.Count;

		cut.Find("select").Change("Warning");

		Assert.True(_askedAt.Count > before);
		Assert.Equal(MokaLogLevel.Warning, _askedAt[^1]);
		Assert.Equal(["Warning line", "Error line", "Fatal line"], Messages(cut));
	}

	[Fact]
	public void AProvider_IsAskedAgainWhenTheParentChangesTheLevel()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.ItemsProvider, Provide)
			.Add(x => x.LevelFilter, MokaLogLevelFilter.AtLeast));
		int before = _askedAt.Count;

		_minimum = MokaLogLevel.Fatal;
		cut.Render(p => p.Add(x => x.MinimumLevel, MokaLogLevel.Fatal));

		Assert.Equal(before + 1, _askedAt.Count);
		Assert.Equal(["Fatal line"], Messages(cut));
	}

	[Fact]
	public void AtLeast_TextReplacesTheWords()
	{
		IRenderedComponent<MokaLogViewer> cut = Render<MokaLogViewer>(p => p
			.Add(x => x.LevelFilter, MokaLogLevelFilter.AtLeast)
			.Add(x => x.Text, new MokaLogViewerText
			{
				MinimumLevelLabel = "Mindeststufe",
				AtLeast = level => $"ab {level}"
			}));

		IElement list = cut.Find("select");
		Assert.Equal("Mindeststufe", list.GetAttribute("aria-label"));
		Assert.Equal("ab Trace", list.QuerySelector("option")!.TextContent);
	}
}
