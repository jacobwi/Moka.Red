using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.SelectionBar;

/// <summary>
///     One action of a <see cref="MokaSelectionBar" />: a quiet button in the bar's row. Under
///     <see cref="MokaSelectionBarOverflow.Menu" /> an action that doesn't fit leaves the row for the bar's More menu,
///     where it is a menu item with the same text, icon and click. Outside a bar it is a plain button.
/// </summary>
public partial class MokaSelectionAction : MokaVisualComponentBase
{
	private string? _id;
	private string? _lastText;
	private MokaIconDefinition? _lastIcon;
	private bool _lastDisabled;
	private MokaColor? _lastColor;

	/// <summary>What the action does, as the button and the menu item say it: "Download".</summary>
	[Parameter]
	[EditorRequired]
	public string Text { get; set; } = string.Empty;

	/// <summary>An icon before the text, in the row and in the menu.</summary>
	[Parameter]
	public MokaIconDefinition? Icon { get; set; }

	/// <summary>Raised when the action is chosen, from the row or from the More menu.</summary>
	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	/// <summary>
	///     Which actions stay in the row longest when the bar runs out of room: a lower number goes to the More menu
	///     first, and among equals the last one goes first. 0 by default.
	/// </summary>
	[Parameter]
	public int Priority { get; set; }

	/// <summary>A text button, as a selection bar's actions are. Set another variant for one that should stand out.</summary>
	[Parameter]
	public override MokaVariant Variant { get; set; } = MokaVariant.Text;

	/// <summary>Small, as a selection bar's actions are.</summary>
	[Parameter]
	public override MokaSize Size { get; set; } = MokaSize.Sm;

	[CascadingParameter]
	private MokaSelectionBar? Bar { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-selection-action";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	internal string? ActionId => _id;

	internal bool IsDanger => Color == MokaColor.Error;

	private string PriorityText => Priority.ToString(CultureInfo.InvariantCulture);

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();
		_id = Bar?.Register(this);
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		// The bar draws the menu before its children take their new parameters, so a change it shows has to reach it.
		bool changed = Text != _lastText || !Nullable.Equals(Icon, _lastIcon) || Disabled != _lastDisabled || Color != _lastColor;
		_lastText = Text;
		_lastIcon = Icon;
		_lastDisabled = Disabled;
		_lastColor = Color;
		if (changed)
		{
			Bar?.ActionChanged();
		}
	}

	/// <inheritdoc />
	protected override ValueTask DisposeAsyncCore()
	{
		if (_id is not null)
		{
			Bar?.Unregister(_id);
		}

		return base.DisposeAsyncCore();
	}

	internal Task ClickAsync(MouseEventArgs args) => Disabled ? Task.CompletedTask : OnClick.InvokeAsync(args);
}
