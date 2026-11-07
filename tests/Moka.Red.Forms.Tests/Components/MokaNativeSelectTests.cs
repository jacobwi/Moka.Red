using System.ComponentModel.DataAnnotations;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Moka.Red.Core.Enums;
using Moka.Red.Forms.NativeSelect;
using Moka.Red.Tests.Shared;

namespace Moka.Red.Forms.Tests.Components;

public class MokaNativeSelectTests : BunitContext
{
	private const string Css = "MokaNativeSelect.razor.css";

	private static readonly string[] Zones = ["Home", "Guests", "Lab"];

	[Fact]
	public void Options_CarryTheirPosition_AndTheItemsText()
	{
		IRenderedComponent<MokaNativeSelect<string>> cut = Render<MokaNativeSelect<string>>(p => p.Add(x => x.Items, Zones));

		IReadOnlyList<IElement> options = cut.FindAll("select > option");
		Assert.Equal(["0", "1", "2"], options.Select(o => o.GetAttribute("value")));
		Assert.Equal(Zones, options.Select(o => o.TextContent));
	}

	[Fact]
	public void TextSelector_WritesTheOptionsText()
	{
		IRenderedComponent<MokaNativeSelect<Zone>> cut = Render<MokaNativeSelect<Zone>>(p => p
			.Add(x => x.Items, [new Zone(1, "Home"), new Zone(2, "Guests")])
			.Add(x => x.TextSelector, zone => zone.Name));

		Assert.Equal(["Home", "Guests"], cut.FindAll("option").Select(o => o.TextContent));
	}

	[Fact]
	public void Value_SelectsItsPosition()
	{
		IRenderedComponent<MokaNativeSelect<string>> cut = Render<MokaNativeSelect<string>>(p => p
			.Add(x => x.Items, Zones)
			.Add(x => x.Value, "Lab"));

		Assert.Equal("2", cut.Find("select").GetAttribute("value"));
	}

	[Fact]
	public void Value_MatchingNoItem_SelectsNothing()
	{
		IRenderedComponent<MokaNativeSelect<string>> cut = Render<MokaNativeSelect<string>>(p => p
			.Add(x => x.Items, Zones)
			.Add(x => x.Value, "Elsewhere"));

		Assert.Equal("", cut.Find("select").GetAttribute("value"));
	}

	[Fact]
	public async Task Change_PicksTheItemAtThatPosition_OfAnyType()
	{
		Zone[] zones = [new(1, "Home"), new(2, "Guests")];
		List<Zone?> changes = [];
		IRenderedComponent<MokaNativeSelect<Zone>> cut = Render<MokaNativeSelect<Zone>>(p => p
			.Add(x => x.Items, zones)
			.Add(x => x.TextSelector, zone => zone.Name)
			.Add(x => x.Value, zones[0])
			.Add(x => x.ValueChanged, EventCallback.Factory.Create<Zone>(this, changes.Add)));

		await cut.Find("select").ChangeAsync(new ChangeEventArgs { Value = "1" });

		Assert.Same(zones[1], Assert.Single(changes));
		Assert.Equal("1", cut.Find("select").GetAttribute("value"));
	}

	[Fact]
	public async Task Change_WorksForAValueType()
	{
		int[] ports = [80, 443];
		List<int> changes = [];
		IRenderedComponent<MokaNativeSelect<int>> cut = Render<MokaNativeSelect<int>>(p => p
			.Add(x => x.Items, ports)
			.Add(x => x.Value, 80)
			.Add(x => x.ValueChanged, EventCallback.Factory.Create<int>(this, changes.Add)));

		await cut.Find("select").ChangeAsync(new ChangeEventArgs { Value = "1" });

		Assert.Equal([443], changes);
	}

	[Fact]
	public async Task ANullItem_IsARealOption()
	{
		string?[] items = [null, "Home"];
		List<string?> changes = [];
		IRenderedComponent<MokaNativeSelect<string?>> cut = Render<MokaNativeSelect<string?>>(p => p
			.Add(x => x.Items, items)
			.Add(x => x.TextSelector, item => item ?? "Default")
			.Add(x => x.Value, "Home")
			.Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, changes.Add)));

		Assert.Equal("Default", cut.FindAll("option")[0].TextContent);

		await cut.Find("select").ChangeAsync(new ChangeEventArgs { Value = "0" });

		Assert.Null(Assert.Single(changes));
		Assert.Equal("0", cut.Find("select").GetAttribute("value"));
	}

	[Fact]
	public void ANullValue_WithNoNullItem_SelectsNothing()
	{
		IRenderedComponent<MokaNativeSelect<string?>> cut = Render<MokaNativeSelect<string?>>(p => p.Add(x => x.Items, Zones));

		Assert.Equal("", cut.Find("select").GetAttribute("value"));
	}

	[Fact]
	public async Task Change_ToAPositionThatIsGone_KeepsTheValue()
	{
		List<string> changes = [];
		IRenderedComponent<MokaNativeSelect<string>> cut = Render<MokaNativeSelect<string>>(p => p
			.Add(x => x.Items, Zones)
			.Add(x => x.Value, "Home")
			.Add(x => x.ValueChanged, EventCallback.Factory.Create<string>(this, changes.Add)));

		await cut.Find("select").ChangeAsync(new ChangeEventArgs { Value = "9" });

		Assert.Empty(changes);
		Assert.Equal("Home", cut.Instance.Value);
	}

	[Fact]
	public void Placeholder_IsAFirstOptionThatCantBeChosen()
	{
		IRenderedComponent<MokaNativeSelect<string>> cut = Render<MokaNativeSelect<string>>(p => p
			.Add(x => x.Items, Zones)
			.Add(x => x.Placeholder, "Choose a zone"));

		IElement placeholder = cut.FindAll("option")[0];
		Assert.Equal("Choose a zone", placeholder.TextContent);
		Assert.Equal("", placeholder.GetAttribute("value"));
		Assert.True(placeholder.HasAttribute("disabled"));
		Assert.Equal(4, cut.FindAll("option").Count);
		Assert.Contains("moka-native-select-input--placeholder", cut.Find("select").ClassList);
	}

	[Fact]
	public void Placeholder_IsQuietOnlyWhileShown()
	{
		IRenderedComponent<MokaNativeSelect<string>> cut = Render<MokaNativeSelect<string>>(p => p
			.Add(x => x.Items, Zones)
			.Add(x => x.Placeholder, "Choose a zone")
			.Add(x => x.Value, "Lab"));

		Assert.DoesNotContain("moka-native-select-input--placeholder", cut.Find("select").ClassList);
	}

	[Fact]
	public void NoPlaceholder_AddsNoOption()
	{
		IRenderedComponent<MokaNativeSelect<string>> cut = Render<MokaNativeSelect<string>>(p => p.Add(x => x.Items, Zones));

		Assert.Equal(3, cut.FindAll("option").Count);
		Assert.DoesNotContain("moka-native-select-input--placeholder", cut.Find("select").ClassList);
	}

	[Fact]
	public void Wrapper_GivesItALabelAndHelperText()
	{
		IRenderedComponent<MokaNativeSelect<string>> cut = Render<MokaNativeSelect<string>>(p => p
			.Add(x => x.Id, "zone")
			.Add(x => x.Items, Zones)
			.Add(x => x.Label, "Zone")
			.Add(x => x.HelperText, "Where it lives"));

		Assert.Equal("zone", cut.Find("label").GetAttribute("for"));
		Assert.Equal("zone", cut.Find("select").Id);
		Assert.Equal("Where it lives", cut.Find(".moka-field-helper").TextContent.Trim());
	}

	[Fact]
	public void Required_ShowsTheMarker_AndTellsTheControl()
	{
		IRenderedComponent<MokaNativeSelect<string>> cut = Render<MokaNativeSelect<string>>(p => p
			.Add(x => x.Items, Zones)
			.Add(x => x.Label, "Zone")
			.Add(x => x.Required, true));

		Assert.Single(cut.FindAll(".moka-field-required"));
		Assert.Equal("true", cut.Find("select").GetAttribute("aria-required"));
	}

	[Fact]
	public void ErrorText_PutsTheFieldInError()
	{
		IRenderedComponent<MokaNativeSelect<string>> cut = Render<MokaNativeSelect<string>>(p => p
			.Add(x => x.Items, Zones)
			.Add(x => x.ErrorText, "Pick one"));

		Assert.Equal("Pick one", cut.Find(".moka-field-helper").TextContent.Trim());
		Assert.Contains("moka-field--error", cut.Find(".moka-field").ClassList);
		Assert.Contains("moka-native-select--error", cut.Find(".moka-native-select").ClassList);
		Assert.Equal("true", cut.Find("select").GetAttribute("aria-invalid"));
	}

	[Fact]
	public void Defaults_NameNoState()
	{
		IRenderedComponent<MokaNativeSelect<string>> cut = Render<MokaNativeSelect<string>>(p => p.Add(x => x.Items, Zones));

		IElement select = cut.Find("select");
		Assert.False(select.HasAttribute("aria-invalid"));
		Assert.False(select.HasAttribute("aria-required"));
		Assert.False(select.HasAttribute("disabled"));
		Assert.False(select.HasAttribute("style"));
		Assert.Equal("moka-native-select-input moka-native-select-input--md", select.GetAttribute("class"));
		Assert.Equal("moka-native-select", cut.Find(".moka-native-select").GetAttribute("class"));
	}

	[Fact]
	public void Disabled_ReachesTheSelect()
	{
		IRenderedComponent<MokaNativeSelect<string>> cut = Render<MokaNativeSelect<string>>(p => p
			.Add(x => x.Items, Zones)
			.Add(x => x.Disabled, true));

		Assert.True(cut.Find("select").HasAttribute("disabled"));
		Assert.Contains("moka-field--disabled", cut.Find(".moka-field").ClassList);
	}

	[Fact]
	public void Size_ReachesTheSelect()
	{
		IRenderedComponent<MokaNativeSelect<string>> cut = Render<MokaNativeSelect<string>>(p => p
			.Add(x => x.Items, Zones)
			.Add(x => x.Size, MokaSize.Lg));

		Assert.Contains("moka-native-select-input--lg", cut.Find("select").ClassList);
	}

	[Fact]
	public void ClassAndStyle_GoOnItsOwnElement_AndOtherAttributesOnTheSelect()
	{
		IRenderedComponent<MokaNativeSelect<string>> cut = Render<MokaNativeSelect<string>>(p => p
			.Add(x => x.Items, Zones)
			.Add(x => x.Class, "mine")
			.Add(x => x.Style, "color: red")
			.AddUnmatched("data-probe", "yes"));

		IElement own = cut.Find(".moka-native-select");
		Assert.Equal("moka-native-select mine", own.GetAttribute("class"));
		Assert.Equal("color: red", own.GetAttribute("style"));
		Assert.Equal("yes", cut.Find("select").GetAttribute("data-probe"));
	}

	[Fact]
	public void EditForm_ShowsTheMessage_AndMarksTheControl()
	{
		Booking booking = new();
		IRenderedComponent<EditForm> cut = RenderForm(booking);
		Assert.False(cut.Find("select").HasAttribute("aria-invalid"));

		cut.Find("form").Submit();

		Assert.Equal("Pick a zone", cut.Find(".moka-field-helper").TextContent.Trim());
		Assert.Equal("true", cut.Find("select").GetAttribute("aria-invalid"));
		Assert.Contains("invalid", cut.Find(".moka-native-select").ClassList);
	}

	[Fact]
	public async Task EditForm_PickingAnItem_ClearsTheMessage_AndReachesTheModel()
	{
		Booking booking = new();
		int submitted = 0;
		IRenderedComponent<EditForm> cut = RenderForm(booking, () => submitted++);
		await cut.Find("form").SubmitAsync();
		Assert.Equal(0, submitted);

		await cut.Find("select").ChangeAsync(new ChangeEventArgs { Value = "1" });
		await cut.Find("form").SubmitAsync();

		Assert.Equal("Guests", booking.Zone);
		Assert.Equal(1, submitted);
		Assert.Empty(cut.FindAll(".moka-field-helper"));
		Assert.False(cut.Find("select").HasAttribute("aria-invalid"));
	}

	[Fact]
	public void Css_IsANativeControl_WithAChevronFromTwoGradients()
	{
		IReadOnlyDictionary<string, string> rule = ScopedCss.Declarations(Css, ".moka-native-select-input");

		Assert.Equal("none", rule["appearance"]);
		Assert.Equal("var(--moka-control-height)", rule["height"]);
		Assert.Equal("0 2.125rem 0 var(--moka-input-padding)", rule["padding"]);
		Assert.Equal(2, rule["background-image"].Split("linear-gradient(").Length - 1);
		Assert.DoesNotContain("url(", rule["background-image"], StringComparison.Ordinal);
	}

	private IRenderedComponent<EditForm> RenderForm(Booking booking, Action? onValid = null) => Render<EditForm>(p => p
		.Add(f => f.Model, booking)
		.Add(f => f.OnValidSubmit, EventCallback.Factory.Create<EditContext>(this, () => onValid?.Invoke()))
		.Add(f => f.ChildContent, _ => builder =>
		{
			builder.OpenComponent<DataAnnotationsValidator>(0);
			builder.CloseComponent();
			builder.OpenComponent<MokaNativeSelect<string?>>(1);
			builder.AddAttribute(2, nameof(MokaNativeSelect<string?>.Items), Zones);
			builder.AddAttribute(3, nameof(MokaNativeSelect<string?>.Placeholder), "Choose");
			builder.AddAttribute(4, nameof(MokaNativeSelect<string?>.Value), booking.Zone);
			builder.AddAttribute(5, nameof(MokaNativeSelect<string?>.ValueChanged),
				EventCallback.Factory.Create<string?>(this, value => booking.Zone = value));
			builder.AddAttribute(6, nameof(MokaNativeSelect<string?>.ValueExpression),
				(System.Linq.Expressions.Expression<Func<string?>>)(() => booking.Zone));
			builder.CloseComponent();
		}));

	private sealed record Zone(int Id, string Name);

	private sealed class Booking
	{
		[Required(ErrorMessage = "Pick a zone")]
		public string? Zone { get; set; }
	}
}
