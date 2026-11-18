using System.Globalization;
using Moka.Red.Data.Table;

namespace Moka.Red.Showcase.Components.Demos.Data.Table;

internal sealed record DemoLedgerLine(int Entry, string Account, string Memo, decimal Amount);

// A ledger of 100,000 lines, each made from its number, answered the way a database would: the search, the account
// filter, the grouping by account and the sort by entry, a window of rows from StartIndex, and the count, the totals, the
// group totals and the facets only when the request asks for more than rows. What matched is kept for the next window,
// as a database keeps a query plan, so scrolling costs a slice.
internal sealed class DemoLedger
{
	public const int Size = 100_000;

	private static readonly string[] Accounts = ["Payroll", "Rent", "Sales", "Supplies", "Travel", "Utilities"];

	private static readonly string[] Memos = ["Invoice paid", "Monthly charge", "Refund", "Card payment", "Transfer", "Adjustment"];

	private string? _matchedFor;
	private int[] _matched = [];

	public int Requests { get; private set; }

	public int WholeRequests { get; private set; }

	public int RowsAsked { get; private set; }

	public static DemoLedgerLine At(int index) => new(
		index + 1,
		Accounts[index % Accounts.Length],
		Memos[index * 5 % Memos.Length],
		Math.Round((index * 7919 % 200_000 - 100_000) / 100m, 2));

	public async Task<MokaTableResult<DemoLedgerLine>> LoadAsync(MokaTableRequest request)
	{
		Requests++;
		WholeRequests += request.RowsOnly ? 0 : 1;
		await Task.Delay(120, request.CancellationToken);

		int[] matched = Matched(request);
		int first = Math.Min(request.StartIndex, matched.Length);
		int count = Math.Min(request.PageSize, matched.Length - first);
		RowsAsked += count;
		List<DemoLedgerLine> items = [.. matched.AsSpan(first, count).ToArray().Select(At)];
		if (request.RowsOnly)
		{
			return new MokaTableResult<DemoLedgerLine> { Items = items };
		}

		return new MokaTableResult<DemoLedgerLine>
		{
			Items = items,
			TotalItems = matched.Length,
			Totals = new Dictionary<string, double> { ["Amount"] = (double)matched.Sum(static i => At(i).Amount) },
			GroupTotals = request.Groups is ["Account"] ? GroupTotals(matched) : null,
			Facets = request.Facets.Contains("Account")
				? new Dictionary<string, IReadOnlyList<MokaFacetValue>> { ["Account"] = AccountFacet(request) }
				: null
		};
	}

	private int[] Matched(MokaTableRequest request)
	{
		string key = string.Join('|', request.Search, string.Join(',', AccountsPicked(request) ?? []),
			string.Join(',', request.Groups), string.Join(',', request.Sorts.Select(static sort => sort.Column + sort.Direction)));
		if (key == _matchedFor)
		{
			return _matched;
		}

		IEnumerable<int> found = Searched(request);
		if (AccountsPicked(request) is { } accounts)
		{
			found = found.Where(i => accounts.Contains(At(i).Account));
		}

		bool newestFirst = request.Sorts is [{ Column: "Entry", Direction: MokaSortDirection.Descending }, ..];
		IEnumerable<int> ordered = newestFirst ? found.OrderByDescending(static i => i) : found;
		if (request.Groups is ["Account"])
		{
			ordered = ordered.OrderBy(static i => At(i).Account, StringComparer.Ordinal);
		}

		_matchedFor = key;
		_matched = [.. ordered];
		return _matched;
	}

	private static IEnumerable<int> Searched(MokaTableRequest request) => request.Search is { Length: > 0 } search
		? Enumerable.Range(0, Size).Where(i => At(i) is var line
		                                       && (line.Memo.Contains(search, StringComparison.OrdinalIgnoreCase)
		                                           || line.Account.Contains(search, StringComparison.OrdinalIgnoreCase)))
		: Enumerable.Range(0, Size);

	private static HashSet<string>? AccountsPicked(MokaTableRequest request) =>
		request.Filters.FirstOrDefault(static filter => filter.Column == "Account") is { Values.Count: > 0 } filter
			? new HashSet<string>(filter.Values, StringComparer.OrdinalIgnoreCase)
			: null;

	// Every group of the whole result, in the order its rows come, so the table can window the headings with the rows.
	private static List<MokaTableGroupTotals> GroupTotals(int[] matched)
	{
		List<MokaTableGroupTotals> groups = [];
		int start = 0;
		for (int at = 1; at <= matched.Length; at++)
		{
			string account = At(matched[start]).Account;
			if (at < matched.Length && At(matched[at]).Account == account)
			{
				continue;
			}

			double sum = 0;
			for (int i = start; i < at; i++)
			{
				sum += (double)At(matched[i]).Amount;
			}

			groups.Add(new MokaTableGroupTotals([account], at - start)
			{
				Totals = new Dictionary<string, double> { ["Amount"] = sum }
			});
			start = at;
		}

		return groups;
	}

	// Each account with how many lines it would give, counted with the search but not the account filter itself.
	private static List<MokaFacetValue> AccountFacet(MokaTableRequest request) => [.. Searched(request)
		.GroupBy(static i => At(i).Account)
		.OrderBy(static group => group.Key, StringComparer.Ordinal)
		.Select(static group => new MokaFacetValue(group.Key, group.Key, group.Count()))];

	public string Describe(MokaTableSelection<DemoLedgerLine>? picked) => string.Create(CultureInfo.CurrentCulture,
		$"Requests: {Requests:N0}, {WholeRequests:N0} of them with counts and totals; rows read: {RowsAsked:N0}. ")
		+ (picked switch
		{
			{ AllMatching: true } all => string.Create(CultureInfo.CurrentCulture,
				$"Picked: every matching line, {all.Count:N0}, but {all.Except.Count:N0} unticked."),
			{ Items.Count: > 0 } some => string.Create(CultureInfo.CurrentCulture, $"Picked: {some.Items.Count:N0} lines."),
			_ => "Nothing picked."
		});
}
