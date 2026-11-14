using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Forms.FormBuilder;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

// A parent that passes Fields and listens owns them: a list it doesn't take goes back, an edit comes back as a new field
// so the parent's own field is never changed, and the edited field stays selected whichever way the parent answers.
public sealed class MokaFormBuilderOwnedTests : BunitContext
{
	private readonly List<MokaFormField> _start = [new() { Id = "name", Type = MokaFormFieldType.TextField, Label = "Name" }];

	public MokaFormBuilderOwnedTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public async Task AFieldTheParentRefuses_GoesAgain()
	{
		IRenderedComponent<ValueParent<MokaFormBuilder, IList<MokaFormField>>> cut = Parent(p => p.Add(x => x.Keep, _ => _start));

		await cut.Find(".moka-form-builder__palette-item").ClickAsync(new MouseEventArgs());

		Assert.Equal(2, Assert.Single(cut.Instance.Asked)!.Count);
		Assert.Equal(["Name"], Labels(cut));
	}

	[Fact]
	public async Task AFieldTheParentTakes_Stays()
	{
		IRenderedComponent<ValueParent<MokaFormBuilder, IList<MokaFormField>>> cut = Parent();

		await cut.Find(".moka-form-builder__palette-item").ClickAsync(new MouseEventArgs());

		Assert.Equal(2, cut.Instance.Held!.Count);
		Assert.Equal(2, Labels(cut).Count);
	}

	[Fact]
	public async Task AnEditTheParentRefuses_PutsTheFieldBack_AndKeepsItSelected()
	{
		IRenderedComponent<ValueParent<MokaFormBuilder, IList<MokaFormField>>> cut = Parent(p => p.Add(x => x.Keep, _ => _start));

		await EditLabelAsync(cut, "Full name");

		Assert.Equal("Name", _start[0].Label);
		Assert.Equal(["Name"], Labels(cut));
		Assert.Equal("Name", LabelBox(cut).GetAttribute("value"));
		Assert.Single(cut.FindAll(".moka-form-builder__field-item--selected"));
	}

	[Fact]
	public async Task AnEditTheParentTakes_ComesBackAsANewField()
	{
		IRenderedComponent<ValueParent<MokaFormBuilder, IList<MokaFormField>>> cut = Parent();

		await EditLabelAsync(cut, "Full name");

		MokaFormField edited = Assert.Single(cut.Instance.Held!);
		Assert.NotSame(_start[0], edited);
		Assert.Equal("Full name", edited.Label);
		Assert.Equal("name", edited.Id);
		Assert.Equal("Name", _start[0].Label);
		Assert.Equal("Full name", LabelBox(cut).GetAttribute("value"));
		Assert.Single(cut.FindAll(".moka-form-builder__field-item--selected"));
	}

	[Fact]
	public async Task AnEditRefusedLater_ShowsUntilTheRefusal()
	{
		TaskCompletionSource<IList<MokaFormField>?> answer = new();
		IRenderedComponent<ValueParent<MokaFormBuilder, IList<MokaFormField>>> cut =
			Parent(p => p.Add(x => x.Answer, _ => answer.Task));

		await EditLabelAsync(cut, "Full name");
		Assert.Equal(["Full name"], Labels(cut));

		await cut.InvokeAsync(() => answer.SetResult(_start));

		Assert.Equal(["Name"], Labels(cut));
		Assert.Equal("Name", LabelBox(cut).GetAttribute("value"));
	}

	[Fact]
	public async Task TheFirstOfTwoEdits_RefusedLate_DoesNotUndoTheSecond()
	{
		TaskCompletionSource<IList<MokaFormField>?> first = new();
		TaskCompletionSource<IList<MokaFormField>?> second = new();
		int calls = 0;
		IRenderedComponent<ValueParent<MokaFormBuilder, IList<MokaFormField>>> cut =
			Parent(p => p.Add(x => x.Answer, _ => ++calls == 1 ? first.Task : second.Task));

		await EditLabelAsync(cut, "Full name");
		await LabelBox(cut).ChangeAsync(new ChangeEventArgs { Value = "Given name" });
		await cut.InvokeAsync(() => first.SetResult(_start));

		Assert.Equal(["Given name"], Labels(cut));

		await cut.InvokeAsync(() => second.SetResult(cut.Instance.Asked[^1]));

		Assert.Equal("Given name", Assert.Single(cut.Instance.Held!).Label);
		Assert.Equal(["Given name"], Labels(cut));
	}

	[Fact]
	public async Task AListTheParentPassesInstead_Shows()
	{
		List<MokaFormField> other = [new() { Id = "other", Label = "Other" }];
		IRenderedComponent<ValueParent<MokaFormBuilder, IList<MokaFormField>>> cut = Parent(p => p.Add(x => x.Keep, _ => other));

		await cut.Find(".moka-form-builder__palette-item").ClickAsync(new MouseEventArgs());

		Assert.Equal(["Other"], Labels(cut));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task WithoutAnOwner_TheBuilderKeepsItsOwnEdits(bool listen)
	{
		IRenderedComponent<ValueParent<MokaFormBuilder, IList<MokaFormField>>> cut = Parent(p => p
			.Add(x => x.PassValue, false)
			.Add(x => x.Listen, listen)
			.Add(x => x.Keep, _ => _start));

		await cut.Find(".moka-form-builder__palette-item").ClickAsync(new MouseEventArgs());
		cut.Render();

		Assert.Single(Labels(cut));
	}

	private IRenderedComponent<ValueParent<MokaFormBuilder, IList<MokaFormField>>> Parent(
		Action<ComponentParameterCollectionBuilder<ValueParent<MokaFormBuilder, IList<MokaFormField>>>>? configure = null) =>
		Render<ValueParent<MokaFormBuilder, IList<MokaFormField>>>(p =>
		{
			p.Add(x => x.Value, _start).Add(x => x.ValueName, "Fields");
			configure?.Invoke(p);
		});

	private static async Task EditLabelAsync(IRenderedComponent<IComponent> cut, string label)
	{
		await cut.Find(".moka-form-builder__field-item").ClickAsync(new MouseEventArgs());
		await LabelBox(cut).ChangeAsync(new ChangeEventArgs { Value = label });
	}

	private static IElement LabelBox(IRenderedComponent<IComponent> cut) => cut.FindAll(".moka-form-builder__property-input")[0];

	private static List<string> Labels(IRenderedComponent<IComponent> cut) =>
		[.. cut.FindAll(".moka-form-builder__field-label").Select(label => label.TextContent)];
}
