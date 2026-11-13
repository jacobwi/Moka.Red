using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Icons;
using Moka.Red.Primitives.KeyValue;
using Moka.Red.Primitives.List;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Primitives.Tests.Components;

// MokaArk's design audit on 0.4.4: a key-value ran its label and value together, and a dense list sitting above a tree
// in one sidebar had other text, icons and rows than the tree. Each answer is an option, so today's markup stays.
public class MokaAuditVariantTests : BunitContext
{
	private const string KeyValueCss = "MokaKeyValue.razor.css";
	private const string ListCss = "MokaList.razor.css";

	public MokaAuditVariantTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	[Fact]
	public void KeyValue_KeepsTodaysMarkup_ByDefault()
	{
		IRenderedComponent<MokaKeyValue> cut = Render<MokaKeyValue>(p => p
			.Add(x => x.Label, "Role")
			.Add(x => x.Value, "Owner")
			.Add(x => x.LabelWidth, "8rem"));

		cut.MarkupMatches(
			"<div class=\"moka-kv moka-kv--row\"><span class=\"moka-kv__label\" style=\"width: 8rem; min-width: 8rem\">Role</span>" +
			"<span class=\"moka-kv__value\">Owner</span></div>");
		Assert.Equal(MokaKeyValueLook.Standard, cut.Instance.Look);
	}

	[Fact]
	public void KeyValue_Columns_PutsTheMicroLabelInAColumn()
	{
		IRenderedComponent<MokaKeyValue> cut = Render<MokaKeyValue>(p => p
			.Add(x => x.Label, "Role")
			.Add(x => x.Value, "Owner")
			.Add(x => x.Orientation, Core.Enums.MokaDirection.Column)
			.Add(x => x.Look, MokaKeyValueLook.Columns));

		IElement root = cut.Find(".moka-kv");
		Assert.Equal(["moka-kv", "moka-kv--columns"], root.ClassList);
		Assert.Null(root.GetAttribute("style"));
		Assert.Equal(["moka-kv__label", "moka-micro-label"], cut.Find(".moka-kv__label").ClassList);
	}

	// One width for the whole column: on the key-value as the column's width, not on the label.
	[Fact]
	public void KeyValue_Columns_TakesLabelWidthAsTheColumn()
	{
		IRenderedComponent<MokaKeyValue> cut = Render<MokaKeyValue>(p => p
			.Add(x => x.Label, "Role")
			.Add(x => x.Value, "Owner")
			.Add(x => x.LabelWidth, "9rem")
			.Add(x => x.Look, MokaKeyValueLook.Columns)
			.Add(x => x.Style, "color: red"));

		Assert.Equal("--moka-kv-label-width: 9rem; color: red", cut.Find(".moka-kv").GetAttribute("style"));
		Assert.Null(cut.Find(".moka-kv__label").GetAttribute("style"));
	}

	[Fact]
	public void KeyValue_Mono_IsAClass_InEitherLook()
	{
		IRenderedComponent<MokaKeyValue> cut = Render<MokaKeyValue>(p => p
			.Add(x => x.Label, "Id")
			.Add(x => x.Value, "6b4f")
			.Add(x => x.Mono, true));

		Assert.Equal(["moka-kv", "moka-kv--row", "moka-kv--mono"], cut.Find(".moka-kv").ClassList);

		IReadOnlyDictionary<string, string> value = ScopedCss.Declarations(KeyValueCss, ".moka-kv--mono > .moka-kv__value");
		Assert.Equal("var(--moka-font-family-mono)", value["font-family"]);
		Assert.Equal("tabular-nums", value["font-variant-numeric"]);
	}

	[Fact]
	public void KeyValue_ColumnsStylesheet_LinesValuesUp_AndLetsTheMicroLabelColorItself()
	{
		IReadOnlyDictionary<string, string> columns = ScopedCss.Declarations(KeyValueCss, ".moka-kv--columns");
		Assert.Equal("grid", columns["display"]);
		Assert.Equal("var(--moka-kv-label-width, min(11rem, 40%)) minmax(0, 1fr)", columns["grid-template-columns"]);

		IReadOnlyDictionary<string, string> label = ScopedCss.Declarations(KeyValueCss, ".moka-kv__label");
		Assert.DoesNotContain("color", label.Keys);
		Assert.DoesNotContain("font-weight", label.Keys);
		Assert.Equal("var(--moka-color-on-surface-variant)",
			ScopedCss.Declarations(KeyValueCss, ".moka-kv__label:not(.moka-micro-label)")["color"]);
	}

	[Fact]
	public void List_KeepsTodaysRows_ByDefault()
	{
		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.Add(x => x.Style, "color: red;")
			.AddChildContent<MokaListItem>(item => item.Add(x => x.Text, "Recent").Add(x => x.Icon, MokaIcons.Action.History)));

		IElement list = cut.Find(".moka-list");
		Assert.Equal(["moka-list", "moka-list--dense", "moka-list--hoverable"], list.ClassList);
		Assert.Equal("color: red;", list.GetAttribute("style"));
		Assert.Contains("width: 16px", cut.Find(".moka-list-item__icon").GetAttribute("style"), StringComparison.Ordinal);
	}

	[Fact]
	public void List_TreeRows_TakeTheTreesIconAndToggleColumn()
	{
		IRenderedComponent<MokaList> cut = Render<MokaList>(p => p
			.Add(x => x.RowStyle, MokaListRowStyle.Tree)
			.Add(x => x.Style, "color: red;")
			.AddChildContent(builder =>
			{
				builder.OpenComponent<MokaListItem>(0);
				builder.AddComponentParameter(1, nameof(MokaListItem.Text), "Recent");
				builder.AddComponentParameter(2, nameof(MokaListItem.Icon), MokaIcons.Action.History);
				builder.CloseComponent();
				builder.OpenComponent<MokaListItem>(3);
				builder.AddComponentParameter(4, nameof(MokaListItem.Text), "Garden");
				builder.AddComponentParameter(5, nameof(MokaListItem.IconContent), (RenderFragment)(b => b.AddContent(0, "G")));
				builder.CloseComponent();
			}));

		IElement list = cut.Find(".moka-list");
		Assert.Contains("moka-list--tree-rows", list.ClassList);
		Assert.Equal("--_list-tree-toggle: 20px; color: red", list.GetAttribute("style"));
		Assert.Contains("width: 14px", cut.Find("svg.moka-list-item__icon").GetAttribute("style"), StringComparison.Ordinal);
		Assert.Contains("--moka-icon-slot: 14px", cut.Find("span.moka-list-item__icon").GetAttribute("style"), StringComparison.Ordinal);
	}

	// The tree's row: two pixels of padding top and bottom, its toggle column, then the icon.
	[Fact]
	public void List_TreeRowsStylesheet_SizesRowsLikeTheTree()
	{
		IReadOnlyDictionary<string, string> row = ScopedCss.Declarations(ListCss, ".moka-list--tree-rows ::deep .moka-list-item");

		Assert.Equal("var(--moka-spacing-xxs)", row["gap"]);
		Assert.Equal("calc(var(--_list-tree-toggle) + 2 * var(--moka-spacing-xxs))", row["min-height"]);
		Assert.Equal(
			"var(--moka-spacing-xxs) var(--moka-spacing-xs) var(--moka-spacing-xxs) calc(var(--_list-tree-toggle) + var(--moka-spacing-xxs))",
			row["padding"]);
		Assert.Equal("var(--moka-font-size-base)", row["font-size"]);
		Assert.Equal("var(--moka-font-weight-normal)",
			ScopedCss.Declarations(ListCss, ".moka-list--tree-rows ::deep .moka-list-item__text")["font-weight"]);
		Assert.Equal("0", ScopedCss.Declarations(ListCss, ".moka-list--tree-rows ::deep .moka-list-item--active")["border"]);
	}
}
