using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Base;
using Moka.Red.Core.Enums;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Layout.Toolbar;

/// <summary>
///     A horizontal bar of actions and controls - suitable for rich text editor toolbars,
///     action bars, or any row of grouped controls. Flows in-document (not fixed).
/// </summary>
public partial class MokaToolbar : MokaVisualComponentBase
{
	private const string FitModule = "./_content/Moka.Red.Core/moka-fit.js";

	private static readonly Dictionary<string, object?> FitLayout = new()
	{
		["sides"] = new[] { ":scope > .moka-toolbar__items" },
		["more"] = ":scope > .moka-toolbar__more [data-moka-more]",
		["filler"] = "div:empty, span:empty, input[type=\"hidden\"]",
		["flexible"] = true
	};

	private bool _bound;
	private DotNetObjectReference<MokaToolbar>? _dotNetRef;
	private int _droppedCount;
	private bool _expanded;
	private ElementReference _root;

	/// <summary>Toolbar content (buttons, selects, dividers, custom controls).</summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>Whether to use compact height (32px). Default true.</summary>
	[Parameter]
	public bool Dense { get; set; } = true;

	/// <summary>Whether to show a border. Default true.</summary>
	[Parameter]
	public bool Bordered { get; set; } = true;

	/// <summary>Whether to show elevation shadow. Default false.</summary>
	[Parameter]
	public bool Elevated { get; set; }

	/// <summary>Whether to allow items to wrap to the next line. Default false.</summary>
	[Parameter]
	public bool Wrap { get; set; }

	/// <summary>
	///     Keeps the bar on one row. Controls that don't fit drop out whole, lowest
	///     <see cref="MokaToolbarGroup.Priority" /> first, and a button opens the bar onto more rows with those same
	///     controls in their places. The bar closes itself once everything fits. Wins over <see cref="Wrap" />. Needs
	///     an interactive render; without one the controls wrap and nothing is hidden.
	/// </summary>
	[Parameter]
	public bool Expandable { get; set; }

	/// <summary>Names the button in <see cref="Expandable" />, from how many controls didn't fit. Default "Show 2 more tools".</summary>
	[Parameter]
	public Func<int, string>? MoreLabel { get; set; }

	/// <summary>Names the button in <see cref="Expandable" /> while the bar is open.</summary>
	[Parameter]
	public string LessLabel { get; set; } = "Show fewer tools";

	/// <inheritdoc />
	protected override string RootClass => "moka-toolbar";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass("moka-toolbar--dense", Dense)
		.AddClass("moka-toolbar--bordered", Bordered)
		.AddClass("moka-toolbar--elevated", Elevated)
		.AddClass("moka-toolbar--rounded", Rounded is not null && Rounded != MokaRounding.None)
		.AddClass("moka-toolbar--wrap", Wrap && !Expandable)
		.AddClass("moka-toolbar--expandable", Expandable)
		.AddClass("moka-toolbar--expanded", IsExpanded)
		.AddClass(Class)
		.Build();

	private bool IsExpanded => Expandable && _expanded;

	private string AriaExpanded => IsExpanded ? "true" : "false";

	private string MoreName
	{
		get
		{
			if (IsExpanded)
			{
				return LessLabel;
			}

			string count = _droppedCount.ToString(CultureInfo.InvariantCulture);
			return MoreLabel?.Invoke(_droppedCount) ?? (_droppedCount == 1 ? "Show 1 more tool" : $"Show {count} more tools");
		}
	}

	/// <summary>Called by the bar's script when the number of controls that didn't fit changes.</summary>
	/// <param name="count">How many controls are out of the bar.</param>
	/// <returns>A task that completes once the bar has been asked to redraw.</returns>
	[JSInvokable]
	public Task OnDroppedChanged(int count) =>
		InvokeAsync(() =>
		{
			_droppedCount = count;
			_expanded = _expanded && count > 0;
			StateHasChanged();
		});

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		Rounded ??= MokaRounding.Md;
	}

	/// <inheritdoc />
	protected override bool ShouldRender() => Expandable || base.ShouldRender();

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (_bound == Expandable)
		{
			return;
		}

		_bound = Expandable;
		if (Expandable)
		{
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await SafeModuleInvokeVoidAsync(FitModule, "bindFit", _root, _dotNetRef, FitLayout);
		}
		else
		{
			_expanded = false;
			_droppedCount = 0;
			await SafeModuleInvokeVoidAsync(FitModule, "unbindFit", _root);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		if (_bound)
		{
			await SafeModuleInvokeVoidAsync(FitModule, "unbindFit", _root);
		}

		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}

	private void ToggleExpanded() => _expanded = !_expanded;
}
