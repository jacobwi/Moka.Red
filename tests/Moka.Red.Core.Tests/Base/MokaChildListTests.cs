using Moka.Red.Core.Base;

namespace Moka.Red.Core.Tests.Base;

// The order the children report in while the parent's content renders is the markup's. A child that doesn't report
// (Blazor didn't hand it new parameters) keeps its place after the child it followed.
public class MokaChildListTests
{
	private readonly List<IReadOnlyList<string>> _changes = [];
	private int _renders;

	[Fact]
	public void AChildThatComesBack_TakesItsPlaceAgain()
	{
		MokaChildList<string> list = Collected("a", "b", "c");
		list.Remove("b");

		Collect(list, seen: ["a", "b", "c"], added: ["b"]);

		Assert.Equal(["a", "b", "c"], list.Items);
		Assert.False(list.Guessed);
	}

	[Fact]
	public void ChildrenThatDontReport_KeepTheirPlaces()
	{
		MokaChildList<string> list = Collected("a", "b", "c");
		_changes.Clear();
		_renders = 0;

		Collect(list, seen: ["b"], added: []);

		Assert.Equal(["a", "b", "c"], list.Items);
		Assert.Empty(_changes);
		Assert.Equal(0, _renders);
	}

	[Fact]
	public void ANewChild_GoesAfterTheChildBeforeIt_AheadOfOnesThatDidntReport()
	{
		MokaChildList<string> list = Collected("a", "z");

		Collect(list, seen: ["a", "m"], added: ["m"]);

		Assert.Equal(["a", "m", "z"], list.Items);
		Assert.True(list.Guessed);
	}

	[Fact]
	public void ANewChild_AmongChildrenThatDontReport_GoesAfterThem()
	{
		MokaChildList<string> list = Collected("a", "b");

		Collect(list, seen: ["c"], added: ["c"]);

		Assert.Equal(["a", "b", "c"], list.Items);
		Assert.True(list.Guessed);
	}

	[Fact]
	public void ChildrenTheMarkupReordered_TakeTheNewOrder()
	{
		MokaChildList<string> list = Collected("a", "b", "c");

		Collect(list, seen: ["c", "a", "b"], added: []);

		Assert.Equal(["c", "a", "b"], list.Items);
		Assert.Equal(["a", "b", "c"], Assert.Single(_changes.Skip(1)));
	}

	[Fact]
	public void ChangesWhileCollecting_AreReportedOnce_WithTheOrderBeforeThem()
	{
		MokaChildList<string> list = Collected("a", "b", "c");
		_changes.Clear();
		_renders = 0;

		list.StartCollecting();
		list.Saw("a");
		list.Add("x");
		list.Saw("x");
		list.Saw("c");
		list.Remove("b");
		list.Render();
		Assert.Empty(_changes);
		Assert.Equal(0, _renders);
		list.FinishCollecting();

		Assert.Equal(["a", "x", "c"], list.Items);
		Assert.Equal(["a", "b", "c"], Assert.Single(_changes));
		Assert.Equal(1, _renders);
	}

	[Fact]
	public void ARenderAskedForWhileCollecting_ComesOnceItEnds()
	{
		MokaChildList<string> list = Collected("a");
		_renders = 0;

		list.StartCollecting();
		list.Render();
		list.Saw("a");
		Assert.Equal(0, _renders);
		list.FinishCollecting();

		Assert.Equal(1, _renders);
	}

	[Fact]
	public void IndexNow_FollowsAChild_AndKeepsThePlaceOfOneThatLeft()
	{
		MokaChildList<string> list = Collected("a", "b", "c");
		IReadOnlyList<string> before = [.. list.Items];

		list.Remove("a");
		Assert.Equal(1, list.IndexNow(before, 2));
		Assert.Equal(0, list.IndexNow(before, 0));
		Assert.Equal(7, list.IndexNow(before, 7));

		list.Remove("c");
		Assert.Equal(0, list.IndexNow(before, 2));
	}

	[Fact]
	public void Clear_ForgetsEveryChild_WithoutACallback()
	{
		MokaChildList<string> list = Collected("a", "b");
		_changes.Clear();
		_renders = 0;

		list.Clear();

		Assert.Equal(0, list.Count);
		Assert.Equal(-1, list.Remove("a"));
		Assert.Empty(_changes);
		Assert.Equal(0, _renders);
	}

	private MokaChildList<string> Collected(params string[] children)
	{
		MokaChildList<string> list = new(() => _renders++, before => _changes.Add([.. before]));
		Collect(list, children, children);
		return list;
	}

	private static void Collect(MokaChildList<string> list, string[] seen, string[] added)
	{
		list.StartCollecting();
		foreach (string child in seen)
		{
			if (added.Contains(child))
			{
				list.Add(child);
			}

			list.Saw(child);
		}

		list.FinishCollecting();
	}
}
