using System.Globalization;
using Moka.Red.Core.Enums;
using Moka.Red.Data.Table;

namespace Moka.Red.Showcase.Components.Demos.Data.Table;

internal sealed record DemoOrder(
	int Number,
	string Customer,
	string Region,
	string Status,
	string Carrier,
	DateTimeOffset Placed,
	int Items,
	decimal Total,
	string Address)
{
	public string Reference => Number == 0 ? string.Empty : string.Create(CultureInfo.InvariantCulture, $"SO-{Number:0000}");
}

internal sealed record DemoFileEntry(string Name, string Folder, string Owner, DateTimeOffset Modified, long Size);

// Sample rows for the table's phase two demos. Every value comes from the row's number, so a page shows the same rows on
// every visit and in every test.
internal static class DemoOrders
{
	public const string StatusOpen = "Open";
	public const string StatusHeld = "Held";
	public const string StatusShipped = "Shipped";
	public const string StatusDelivered = "Delivered";
	public const string StatusCancelled = "Cancelled";

	private static readonly string[] Customers =
	[
		"Corner Books", "Green Grocer", "Harbor Tools", "Hilltop Cafe", "Lakeside Florist", "Maple Hardware", "Oak Street Deli",
		"Pine Garden Center", "River Print Shop", "Summit Outfitters", "Willow Bakery", "Stone Bridge Studio"
	];

	private static readonly string[] Regions = ["North", "South", "East", "West"];

	private static readonly string[] Statuses = [StatusOpen, StatusHeld, StatusShipped, StatusDelivered, StatusDelivered, StatusCancelled];

	private static readonly string[] Carriers = ["Post", "Courier", "Freight"];

	private static readonly string[] Streets = ["Mill Road", "Church Lane", "Station Street", "Harbour Way", "Orchard Close", "High Street"];

	private static readonly DateTimeOffset Today = new(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);

	public static IReadOnlyList<DemoOrder> All { get; } =
	[
		.. Enumerable.Range(1, 180).Select(i => new DemoOrder(
			1000 + i,
			Customers[i * 7 % Customers.Length],
			Regions[i * 3 % Regions.Length],
			Statuses[i * 5 % Statuses.Length],
			Carriers[i % Carriers.Length],
			Today.AddHours(-i * 11),
			1 + (i * 13 % 9),
			Math.Round(40m + (i * 137 % 1900) + (i % 7 * 0.25m), 2),
			string.Create(CultureInfo.InvariantCulture, $"{10 + (i * 17 % 180)} {Streets[i % Streets.Length]}, {Regions[i * 3 % Regions.Length]}field")))
	];

	public static IReadOnlyList<DemoOrder> Recent { get; } = [.. All.Take(40)];

	public static IReadOnlyList<DemoOrder> Few { get; } = [.. All.Take(12)];

	public static IReadOnlyList<MokaTableQuickFilter> QuickFilters { get; } =
	[
		new("Open", [new MokaTableFilter("Status", MokaFilterOperator.IsAnyOf, [StatusOpen, StatusHeld])]),
		new("Large", [new MokaTableFilter("Total", MokaFilterOperator.AtLeast, "1000")]),
		new("This week", [new MokaTableFilter("Placed", MokaFilterOperator.AtLeast, Today.AddDays(-7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))])
	];

	public static DemoQuery<DemoOrder> Query { get; } = new(All,
	[
		new DemoColumn<DemoOrder>("Number", order => order.Reference) { Searched = true },
		new DemoColumn<DemoOrder>("Customer", order => order.Customer) { Searched = true },
		new DemoColumn<DemoOrder>("Region", order => order.Region),
		new DemoColumn<DemoOrder>("Status", order => order.Status),
		new DemoColumn<DemoOrder>("Carrier", order => order.Carrier),
		new DemoColumn<DemoOrder>("Placed", order => order.Placed.ToString("d", CultureInfo.CurrentCulture)) { Date = order => order.Placed },
		new DemoColumn<DemoOrder>("Items", order => order.Items.ToString(CultureInfo.CurrentCulture)) { Number = order => order.Items },
		new DemoColumn<DemoOrder>("Total", order => order.Total.ToString("N2", CultureInfo.CurrentCulture)) { Number = order => (double)order.Total }
	]);

	public static MokaColor? StatusTone(DemoOrder order) => order.Status switch
	{
		StatusOpen => MokaColor.Info,
		StatusHeld => MokaColor.Warning,
		StatusShipped => MokaColor.Primary,
		StatusDelivered => MokaColor.Success,
		_ => MokaColor.Error
	};

	public static DemoOrder TotalOf(string name, IReadOnlyCollection<DemoOrder> orders) =>
		new(0, name, string.Empty, string.Empty, string.Empty, Today, orders.Sum(order => order.Items), orders.Sum(order => order.Total), string.Empty);

	public static IReadOnlyList<DemoFileEntry> Files { get; } =
	[
		new("Quarterly report.pdf", "Reports", "Ada Park", Today.AddDays(-2), 4_812_000),
		new("report-draft.docx", "Reports", "Lena Ruiz", Today.AddDays(-5), 812_000),
		new("Budget 2026.xlsx", "Finance", "Omar Haddad", Today.AddDays(-9), 186_000),
		new("Budget 2027.xlsx", "Finance", "Omar Haddad", Today.AddDays(-1), 192_000),
		new("Release notes.md", "Docs", "Mia Chen", Today.AddHours(-6), 9_400),
		new("Build log 2026-11-13.txt", "Builds", "Build server", Today.AddDays(-1), 1_204_000),
		new("Build log 2026-11-14.txt", "Builds", "Build server", Today.AddHours(-2), 1_311_000),
		new("Team photo.png", "Photos", "Lena Ruiz", Today.AddDays(-30), 13_600_000),
		new("Interview notes.md", "Docs", "Ada Park", Today.AddDays(-12), 22_000),
		new("Site map.svg", "Design", "Mia Chen", Today.AddDays(-60), 48_300),
		new("Annual report 2025.pdf", "Reports", "Omar Haddad", Today.AddDays(-300), 9_200_000),
		new("Error log.txt", "Builds", "Build server", Today.AddMinutes(-40), 64_000)
	];
}
