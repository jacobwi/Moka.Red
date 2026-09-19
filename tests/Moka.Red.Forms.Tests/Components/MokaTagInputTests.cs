using System.Collections.ObjectModel;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.TagInput;

namespace Moka.Red.Forms.Tests.Components;

// The component used to add to and remove from the caller's own list and hand the same instance
// back, which threw on arrays and read-only lists and gave the parent no way to see a change.
public class MokaTagInputTests : BunitContext
{
	private static readonly string[] Initial = ["a"];

	[Fact]
	public async Task AddingATag_ReportsANewList_AndLeavesAnArrayAlone()
	{
		IList<string>? reported = null;
		string[] original = ["a"];
		IRenderedComponent<MokaTagInput> cut = Render<MokaTagInput>(p => p
			.Add(x => x.Values, original)
			.Add(x => x.ValuesChanged, values => reported = values));

		await TypeAsync(cut, "b");
		await cut.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.NotNull(reported);
		Assert.NotSame(original, reported);
		Assert.Equal(["a", "b"], reported);
		Assert.Equal(["a"], original);
	}

	[Fact]
	public async Task RemovingATag_WorksOnAReadOnlyList()
	{
		IList<string>? reported = null;
		IRenderedComponent<MokaTagInput> cut = Render<MokaTagInput>(p => p
			.Add(x => x.Values, new ReadOnlyCollection<string>(["a", "b"]))
			.Add(x => x.ValuesChanged, values => reported = values));

		await cut.FindAll(".moka-taginput-tag-dismiss")[0].ClickAsync(new MouseEventArgs());

		Assert.Equal(["b"], reported);
	}

	[Fact]
	public async Task TagsTypedWithTheDelimiter_AreReported()
	{
		IList<string>? reported = null;
		IRenderedComponent<MokaTagInput> cut = Render<MokaTagInput>(p => p
			.Add(x => x.Values, Initial)
			.Add(x => x.ValuesChanged, values => reported = values));

		await TypeAsync(cut, "x,y,");

		Assert.Equal(["a", "x", "y"], reported);
	}

	[Fact]
	public async Task ADuplicate_IsNotReportedAsAChange()
	{
		int reports = 0;
		IRenderedComponent<MokaTagInput> cut = Render<MokaTagInput>(p => p
			.Add(x => x.Values, Initial)
			.Add(x => x.ValuesChanged, _ => reports++));

		await TypeAsync(cut, "A");
		await cut.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal(0, reports);
	}

	[Fact]
	public async Task WithoutBinding_TheNewTagStillShows()
	{
		IRenderedComponent<MokaTagInput> cut = Render<MokaTagInput>(p => p.Add(x => x.Values, Initial));

		await TypeAsync(cut, "b");
		await cut.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

		Assert.Equal(["a", "b"], cut.FindAll(".moka-taginput-tag-text").Select(t => t.TextContent));
	}

	[Fact]
	public void AParentEditingItsOwnListInPlace_IsPickedUp()
	{
		List<string> tags = ["a"];
		IRenderedComponent<MokaTagInput> cut = Render<MokaTagInput>(p => p.Add(x => x.Values, tags));

		tags.Add("b");
		cut.Render(p => p.Add(x => x.Values, tags));

		Assert.Equal(["a", "b"], cut.FindAll(".moka-taginput-tag-text").Select(t => t.TextContent));
	}

	private static Task TypeAsync(IRenderedComponent<MokaTagInput> cut, string text) =>
		cut.Find("input").InputAsync(new ChangeEventArgs { Value = text });
}
