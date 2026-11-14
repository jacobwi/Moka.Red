using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moka.Red.ContextMenu;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Data.Table;
using Moka.Red.Feedback.JobProgress;
using Moka.Red.Icons;

#pragma warning disable BL0005, CA2000 // The value, text and export tests read a kind's own hooks on an instance made for them, never mounted.

namespace Moka.Red.Data.Tests.Table;

// Every column kind: what it draws, and what it tells the sort, the filters, the search and the export.
public sealed class MokaColumnKindTests : BunitContext
{
	private static readonly Row[] Rows =
	[
		new("Ada Lovelace", "ada@example.com", 36.5, 1234.5m, 1_500_000, new DateTimeOffset(2026, 3, 1, 14, 30, 0, TimeSpan.Zero), true, "Running", 4.2, [1, 3, 2]),
		new("Grace Hopper", null, 45, 99m, 2_000, new DateTimeOffset(2025, 1, 2, 9, 0, 0, TimeSpan.Zero), false, "Stopped", 2, [5, 4]),
		new("Alan Turing", "alan@example.com", 41, 5000m, 750_000_000, new DateTimeOffset(2024, 7, 9, 0, 0, 0, TimeSpan.Zero), null, "Running", 5, [])
	];

	public MokaColumnKindTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void ANumberColumn_IsRightAlignedInFigures_SortsByTheNumber_AndExportsItPlain()
	{
		using CultureScope _ = new("en-US");
		IRenderedComponent<MokaTable<Row>> cut = Table(b => Kind<MokaNumberColumn<Row>>(b, "Age", c => c.Value = static r => r.Age));

		Assert.Equal(["36.5", "45", "41"], Texts(cut));
		Assert.Contains("moka-table-cell--right", FirstCell(cut).ClassList);
		Assert.Contains("moka-table-cell--figures", FirstCell(cut).ClassList);

		cut.Find("thead th").Click();
		Assert.Equal(["36.5", "41", "45"], Texts(cut));
		Assert.Equal("36.5", new MokaNumberColumn<Row> { Value = static r => r.Age }.ExportTextOf(Rows[0]));
	}

	[Fact]
	public void ANumberColumn_FiltersByValue()
	{
		using CultureScope _ = new("en-US");
		IRenderedComponent<MokaTable<Row>> cut = Table(b => Kind<MokaNumberColumn<Row>>(b, "Age", c => c.Value = static r => r.Age),
			new MokaTableState { Filters = [new MokaTableFilter("Age", MokaFilterOperator.GreaterThan, "40")] });

		Assert.Equal(["45", "41"], Texts(cut));
	}

	[Fact]
	public void AMoneyColumn_WritesCurrency_InTheCultureOrTheCodeGiven()
	{
		using CultureScope _ = new("en-US");
		MokaMoneyColumn<Row> dollars = new() { Value = static r => r.Price };
		MokaMoneyColumn<Row> euros = new() { Value = static r => r.Price, Currency = "EUR", Decimals = 0 };

		Assert.Equal("$1,234.50", dollars.TextOf(Rows[0]));
		Assert.Equal("€1,235", euros.TextOf(Rows[0]));
		Assert.Equal("1234.5", euros.ExportTextOf(Rows[0]));
		Assert.Equal(1234.5m, dollars.ValueOf(Rows[0]));
	}

	[Fact]
	public void ABytesColumn_WritesTheLargestUnit_AndExportsTheCount()
	{
		using CultureScope _ = new("en-US");
		MokaBytesColumn<Row> sizes = new() { Value = static r => r.Bytes };
		MokaBytesColumn<Row> binary = new() { Value = static r => r.Bytes, Units = MokaByteUnits.Binary, PerSecond = true };

		Assert.Equal("1.5 MB", sizes.TextOf(Rows[0]));
		Assert.Equal("1.4 MiB/s", binary.TextOf(Rows[0]));
		Assert.Equal("1500000", sizes.ExportTextOf(Rows[0]));
	}

	[Fact]
	public void ADateColumn_WritesItsFormat_AndExportsIso()
	{
		MokaDateColumn<Row> dates = new() { Value = static r => r.When, Format = "yyyy-MM-dd HH:mm" };

		Assert.Equal("2026-03-01 14:30", dates.TextOf(Rows[0]));
		Assert.Equal("2026-03-01T14:30:00.0000000+00:00", dates.ExportTextOf(Rows[0]));
		Assert.Null(dates.CellContent);
	}

	[Fact]
	public void ARelativeDateColumn_DrawsARelativeTime_WithTheExactTimeInATooltip()
	{
		IRenderedComponent<MokaTable<Row>> cut = Table(b => Kind<MokaDateColumn<Row>>(b, "When", c =>
		{
			c.Value = static r => r.When;
			c.Relative = true;
		}));

		Assert.Equal("2026-03-01T14:30:00.000+00:00", cut.Find("tbody time").GetAttribute("datetime"));
		Assert.NotEmpty(cut.FindAll("tbody .moka-tooltip-trigger"));
	}

	[Fact]
	public void APersonColumn_DrawsAnAvatar_TheName_AndADetailLine()
	{
		IRenderedComponent<MokaTable<Row>> cut = Table(b => Kind<MokaPersonColumn<Row>>(b, "Who", c =>
		{
			c.Name = static r => r.Name;
			c.Detail = static r => r.Email;
		}));

		Assert.Equal("AL", cut.Find("tbody .moka-avatar").TextContent.Trim());
		Assert.Equal("Ada Lovelace", cut.Find("tbody .moka-person-cell__name").TextContent);
		Assert.Equal(2, cut.FindAll("tbody .moka-person-cell__detail").Count);

		cut.Find("thead th").Click();
		Assert.Equal("Ada Lovelace", cut.Find("tbody .moka-person-cell__name").TextContent);
		Assert.Equal("Grace Hopper", new MokaPersonColumn<Row> { Name = static r => r.Name }.ExportTextOf(Rows[1]));
	}

	[Fact]
	public void APersonColumn_CanFallBackToAnIdenticon()
	{
		IRenderedComponent<MokaTable<Row>> cut = Table(b => Kind<MokaPersonColumn<Row>>(b, "Who", c =>
		{
			c.Name = static r => r.Name;
			c.Fallback = MokaAvatarFallback.Identicon;
		}));

		Assert.NotEmpty(cut.FindAll("tbody .moka-avatar .moka-identicon"));
	}

	[Fact]
	public void AnIconTextColumn_DrawsTheIconBeforeTheText()
	{
		IRenderedComponent<MokaTable<Row>> cut = Table(b => Kind<MokaIconTextColumn<Row>>(b, "Name", c =>
		{
			c.Text = static r => r.Name;
			c.Icon = static r => r.Active == true ? MokaIcons.Status.Check : (MokaIconDefinition?)null;
		}));

		Assert.Single(cut.FindAll("tbody tr")[0].QuerySelectorAll("svg"));
		Assert.Empty(cut.FindAll("tbody tr")[1].QuerySelectorAll("svg"));
		Assert.Equal("Ada Lovelace", cut.Find("tbody .moka-icon-text-cell__text").TextContent);
	}

	[Fact]
	public void AStatusColumn_DrawsATagOrADot()
	{
		IRenderedComponent<MokaTable<Row>> tags = Table(b => Kind<MokaStatusColumn<Row>>(b, "State", c =>
		{
			c.Status = static r => r.State;
			c.LabelColor = static r => r.State == "Running" ? MokaLabelColor.Green : MokaLabelColor.Red;
		}));
		IRenderedComponent<MokaTable<Row>> dots = Table(b => Kind<MokaStatusColumn<Row>>(b, "State", c =>
		{
			c.Status = static r => r.State;
			c.Tone = static r => r.State == "Running" ? MokaColor.Success : MokaColor.Error;
			c.Look = MokaStatusLook.Dot;
		}));

		Assert.Equal("Running", tags.Find("tbody .moka-tag").TextContent.Trim());
		Assert.Equal(3, dots.FindAll("tbody .moka-status-dot").Count);
	}

	[Fact]
	public void AProgressColumn_DrawsANamedMeter()
	{
		using CultureScope _ = new("en-US");
		IRenderedComponent<MokaTable<Row>> cut = Table(b => Kind<MokaProgressColumn<Row>>(b, "Load", c =>
		{
			c.Value = static r => r.Age;
			c.ShowFigure = true;
			c.WarnAt = 40;
		}));

		AngleSharp.Dom.IElement meter = cut.Find("tbody [role='meter']");
		Assert.Equal("Load", meter.GetAttribute("aria-label"));
		Assert.Equal("36.5", meter.GetAttribute("aria-valuenow"));
	}

	[Fact]
	public void AYesNoColumn_DrawsAPicture_WithTheWordForScreenReaders()
	{
		IRenderedComponent<MokaTable<Row>> cut = Table(b => Kind<MokaYesNoColumn<Row>>(b, "Active", c => c.Value = static r => r.Active));
		IRenderedComponent<MokaTable<Row>> switches = Table(b => Kind<MokaYesNoColumn<Row>>(b, "Active", c =>
		{
			c.Value = static r => r.Active;
			c.Look = MokaYesNoLook.Switch;
		}));

		Assert.Equal(["Yes", "No", ""], Texts(cut));
		Assert.Empty(cut.FindAll("tbody input"));
		Assert.Single(switches.FindAll("tbody .moka-yes-no-cell__switch--on"));
	}

	[Fact]
	public void AYesNoColumn_FiltersByTrueOrFalse()
	{
		IRenderedComponent<MokaTable<Row>> cut = Table(b => Kind<MokaYesNoColumn<Row>>(b, "Active", c => c.Value = static r => r.Active),
			new MokaTableState { Filters = [new MokaTableFilter("Active", MokaFilterOperator.Is, "true")] });

		Assert.Equal(["Yes"], Texts(cut));
	}

	[Fact]
	public void ALinkColumn_LinksSafeAddresses_AndWritesTheRestAsText()
	{
		IRenderedComponent<MokaTable<Row>> cut = Table(b => Kind<MokaLinkColumn<Row>>(b, "Mail", c =>
		{
			c.Text = static r => r.Name;
			c.Href = static r => r.Email is null ? "javascript:alert(1)" : $"mailto:{r.Email}";
		}));

		Assert.Equal(2, cut.FindAll("tbody a[href^='mailto:']").Count);
		Assert.Empty(cut.FindAll("tbody a[href^='javascript']"));
	}

	[Fact]
	public void ARowMenuColumn_OpensTheOneSharedMenu_WithTheRowsItems()
	{
		MokaContextMenuService menu = new();
		Services.AddSingleton<IMokaContextMenuService>(menu);
		IRenderedComponent<MokaTable<Row>> cut = Table(b => Kind<MokaRowMenuColumn<Row>>(b, null, c =>
		{
			c.Items = static r => [new MokaContextMenuItem { Text = $"Edit {r.Name}" }];
			c.ItemName = static r => r.Name;
		}));

		AngleSharp.Dom.IElement button = cut.Find("tbody .moka-row-menu-cell");
		Assert.Equal("Actions for Ada Lovelace", button.GetAttribute("aria-label"));
		Assert.NotNull(button.Closest(".moka-tooltip-trigger"));

		Act(cut, button);

		Assert.True(menu.Visible);
		Assert.Equal("Edit Ada Lovelace", Assert.Single(menu.Items).Text);
	}

	[Fact]
	public void AnActionsColumn_DrawsNamedQuietButtons_AndRunsThemWithTheRow()
	{
		List<string> ran = [];
		IRenderedComponent<MokaTable<Row>> cut = Table(b => Kind<MokaActionsColumn<Row>>(b, null, c =>
		{
			c.Reveal = MokaActionsReveal.OnHover;
			c.Actions =
			[
				new MokaTableAction<Row>("Edit", MokaIcons.Action.Edit, r =>
				{
					ran.Add(r.Name);
					return Task.CompletedTask;
				}),
				new MokaTableAction<Row>("Delete", MokaIcons.Action.Delete, static _ => Task.CompletedTask)
				{
					Hidden = static r => r.Active != true,
					Color = MokaColor.Error
				}
			];
		}));

		Assert.Equal(2, cut.FindAll("tbody tr")[0].QuerySelectorAll("button").Length);
		Assert.Single(cut.FindAll("tbody tr")[1].QuerySelectorAll("button"));
		Assert.Contains("moka-actions-cell--on-hover", cut.Find("tbody .moka-actions-cell").ClassList);

		Act(cut, cut.Find("tbody button[aria-label='Edit']"));
		cut.WaitForAssertion(() => Assert.Equal(["Ada Lovelace"], ran));
	}

	[Fact]
	public void ACopyableColumn_DrawsTheValueInMono_WithACopyButton()
	{
		IRenderedComponent<MokaTable<Row>> cut = Table(b => Kind<MokaCopyableColumn<Row>>(b, "Mail", c => c.Value = static r => r.Email));
		IRenderedComponent<MokaTable<Row>> secret = Table(b => Kind<MokaCopyableColumn<Row>>(b, "Mail", c =>
		{
			c.Value = static r => r.Email;
			c.Secret = true;
		}));

		Assert.Equal("ada@example.com", cut.Find("tbody [data-moka-copy]").GetAttribute("data-moka-copy"));
		Assert.DoesNotContain("ada@example.com", secret.Find("tbody").InnerHtml, StringComparison.Ordinal);
		Assert.Equal("", new MokaCopyableColumn<Row> { Value = static r => r.Email, Secret = true }.TextOf(Rows[0]));
	}

	[Fact]
	public void ASparklineColumn_DrawsALine_SortsByTheLatestValue_AndExportsThemAll()
	{
		IRenderedComponent<MokaTable<Row>> cut = Table(b => Kind<MokaSparklineColumn<Row>>(b, "Trend", c => c.Values = static r => r.Trend));
		MokaSparklineColumn<Row> trend = new() { Values = static r => r.Trend };

		Assert.Equal(2, cut.FindAll("tbody svg").Count);
		Assert.Equal(2d, trend.ValueOf(Rows[0]));
		Assert.Equal("1 3 2", trend.ExportTextOf(Rows[0]));
		Assert.Null(trend.ValueOf(Rows[2]));
	}

	[Fact]
	public void ARatingColumn_DrawsStars_WithTheRatingInWords()
	{
		using CultureScope _ = new("en-US");
		IRenderedComponent<MokaTable<Row>> cut = Table(b => Kind<MokaRatingColumn<Row>>(b, "Stars", c => c.Value = static r => r.Rating));

		AngleSharp.Dom.IElement first = cut.Find("tbody .moka-rating-cell");
		Assert.Equal(5, first.QuerySelectorAll("svg").Length);
		Assert.Equal(4, first.QuerySelectorAll(".moka-rating-cell__star--lit").Length);
		Assert.Equal("4.2 of 5", first.QuerySelector(".moka-visually-hidden")!.TextContent);
	}

	[Fact]
	public void AnImageColumn_OpensOneViewerForTheShownImages()
	{
		IRenderedComponent<MokaTable<Row>> cut = Table(b => Kind<MokaImageColumn<Row>>(b, "Photo", c =>
		{
			c.Src = static r => r.Email is null ? "javascript:alert(1)" : $"https://example.com/{r.Name[0]}.png";
			c.Alt = static r => r.Name;
		}));

		Assert.Equal(2, cut.FindAll("tbody .moka-image-cell").Count);
		Assert.Empty(cut.FindAll(".moka-lightbox"));

		Act(cut, cut.FindAll("tbody .moka-image-cell")[1]);

		Assert.Single(cut.FindAll(".moka-lightbox"));
	}

	[Fact]
	public void AColumnKind_DrawsNothingWhereItsTableHasNone() =>
		Assert.Empty(Table(b => Kind<MokaNumberColumn<Row>>(b, "Age", c => c.Value = static r => r.Age)).FindAll(".moka-lightbox"));

	// The table hears every cell's controls through one listener in moka-table.js, which hands .NET the row and the
	// control's data-moka-act; this is what it sends for a click.
	private static void Act(IRenderedComponent<MokaTable<Row>> cut, AngleSharp.Dom.IElement control)
	{
		Assert.False(control.HasAttribute("blazor:onclick"));
		string row = control.Closest("tr")!.GetAttribute("data-row-index")!;
		string action = control.GetAttribute("data-moka-act")!;
		cut.InvokeAsync(() => cut.Instance.OnCellAction(row, action, 10, 20, 1)).GetAwaiter().GetResult();
	}

	private IRenderedComponent<MokaTable<Row>> Table(RenderFragment columns, MokaTableState? state = null) =>
		Render<MokaTable<Row>>(p => p
			.Add(x => x.Items, Rows)
			.Add(x => x.State, state)
			.Add(x => x.ChildContent, columns));

	// Writes one kind at fixed sequence numbers, setting its parameters through a template instance.
	private static void Kind<TColumn>(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, string? title, Action<TColumn> set)
		where TColumn : MokaColumn<Row>, new()
	{
		TColumn template = new();
		set(template);
		builder.OpenComponent<TColumn>(0);
		builder.AddComponentParameter(1, nameof(MokaColumn<Row>.Title), title);
		int sequence = 2;
		foreach (System.Reflection.PropertyInfo property in typeof(TColumn).GetProperties())
		{
			if (property.DeclaringType != typeof(MokaColumn<Row>)
			    && property.GetCustomAttributes(typeof(ParameterAttribute), true).Length > 0
			    && property.CanWrite)
			{
				builder.AddComponentParameter(sequence++, property.Name, property.GetValue(template));
			}
		}

		builder.CloseComponent();
	}

	private static List<string> Texts(IRenderedComponent<MokaTable<Row>> cut) =>
		cut.FindAll("tbody tr[data-row-index] td").Select(static td => td.TextContent.Trim()).ToList();

	private static AngleSharp.Dom.IElement FirstCell(IRenderedComponent<MokaTable<Row>> cut) => cut.Find("tbody td");

	public sealed record Row(
		string Name,
		string? Email,
		double Age,
		decimal Price,
		long Bytes,
		DateTimeOffset When,
		bool? Active,
		string State,
		double Rating,
		IReadOnlyList<double> Trend);

	private sealed class CultureScope : IDisposable
	{
		private readonly CultureInfo _culture = CultureInfo.CurrentCulture;

		public CultureScope(string name) => CultureInfo.CurrentCulture = new CultureInfo(name);

		public void Dispose() => CultureInfo.CurrentCulture = _culture;
	}
}
