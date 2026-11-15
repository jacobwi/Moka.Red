using System.Numerics;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moka.Red.Core.Icons;
using Moka.Red.Core.Utilities;
using Moka.Red.Forms.Common;

namespace Moka.Red.Forms.NumericField;

/// <summary>
///     A numeric input field for any number type of the base library, read through its generic math. Uses text input
///     with numeric inputmode for better mobile UX. A nullable type, such as <c>int?</c>, makes an empty box null.
/// </summary>
/// <typeparam name="TValue">
///     <see cref="int" />, <see cref="long" />, <see cref="short" />, <see cref="byte" />, <see cref="sbyte" />,
///     <see cref="uint" />, <see cref="ulong" />, <see cref="ushort" />, <see cref="Int128" />, <see cref="UInt128" />,
///     <see cref="nint" />, <see cref="nuint" />, <see cref="double" />, <see cref="float" />, <see cref="decimal" />,
///     <see cref="Half" />, <see cref="BigInteger" />, or the nullable form of one of them.
/// </typeparam>
public partial class MokaNumericField<TValue>
{
	private const string ModulePath = "./_content/Moka.Red.Forms/NumericField/MokaNumericField.razor.js";

	private readonly string _generatedId = $"moka-numericfield-{Guid.NewGuid():N}";
	private ElementReference _input;
	private DotNetObjectReference<MokaNumericField<TValue>>? _dotNetRef;
	private bool _hasMin;
	private bool _hasMax;
	private bool _hasStep;
	private bool _stepKeysBound;

	/// <summary>Checks the type once, as Blazor's own number input does, rather than failing on the first edit.</summary>
	/// <exception cref="InvalidOperationException">The type isn't a number type the field reads.</exception>
	public MokaNumericField()
	{
		if (!MokaNumberText<TValue>.IsSupported)
		{
			throw new InvalidOperationException($"The type '{typeof(TValue)}' is not a number type MokaNumericField reads.");
		}
	}

	// Id goes on the input, not a wrapper, so a label's for and getElementById reach the control.
	private string InputId => string.IsNullOrEmpty(Id) ? _generatedId : Id;

	/// <summary>Label text displayed above the input.</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Helper text displayed below the input.</summary>
	[Parameter]
	public string? HelperText { get; set; }

	/// <summary>
	///     Content beside the field's box, in its row, such as a button that acts on the value. The label stays above
	///     and the helper text below, and on the new design a button here is as tall as the box.
	/// </summary>
	[Parameter]
	public RenderFragment? Actions { get; set; }

	/// <summary>Error text displayed below the input when in error state.</summary>
	[Parameter]
	public string? ErrorText { get; set; }

	/// <summary>Whether the field is required.</summary>
	[Parameter]
	public bool Required { get; set; }

	/// <summary>Whether the field can be left empty. Shows a quiet word beside the label. <see cref="Required" /> wins.</summary>
	[Parameter]
	public bool Optional { get; set; }

	/// <summary>Writes the value in the monospace font.</summary>
	[Parameter]
	public bool Mono { get; set; }

	/// <summary>A unit inside the box at the right, such as "Mbit/s". The input's <c>aria-describedby</c> points at it.</summary>
	[Parameter]
	public string? Suffix { get; set; }

	/// <summary>Icon displayed before the input text.</summary>
	[Parameter]
	public MokaIconDefinition? StartIcon { get; set; }

	/// <summary>
	///     The smallest value the field reports. A smaller number is reported as this one while it is typed, and the box
	///     shows it once the field loses focus. Applies only when passed, so a field of <c>int</c> without it takes
	///     negative numbers.
	/// </summary>
	[Parameter]
	public TValue? Min { get; set; }

	/// <summary>
	///     The largest value the field reports. A larger number is reported as this one while it is typed, and the box
	///     shows it once the field loses focus. Applies only when passed.
	/// </summary>
	[Parameter]
	public TValue? Max { get; set; }

	/// <summary>
	///     How much Arrow Up and Arrow Down add to or take from the value, within <see cref="Min" /> and
	///     <see cref="Max" />. With a step the input is a spin button. Without one, or with a step that isn't above zero,
	///     the arrow keys move the caret as in any text box.
	/// </summary>
	[Parameter]
	public TValue? Step { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-numericfield";

	// ErrorText is the explicit override; without one, fall back to whatever the cascaded
	// EditContext reports, so DataAnnotations messages are actually visible.
	private bool HasError => !string.IsNullOrEmpty(ErrorText) || HasValidationError;

	/// <summary>Explicit <see cref="ErrorText" /> when set, otherwise the EditContext validation message.</summary>
	private string? ResolvedErrorText => !string.IsNullOrEmpty(ErrorText) ? ErrorText : ValidationErrorText;

	private string ComputedCssClass => new CssBuilder(RootClass)
		.AddClass("moka-numericfield--error", HasError)
		.AddClass(CssClass)
		.AddClass(Class)
		.Build();

	// The field wrapper is the outermost element, so the margin goes there. The input draws the
	// field's border, so it takes the padding and the radius.

	/// <inheritdoc />
	protected override string? ComponentStyle => Style;

	private string? WrapperStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.Build();

	private string? InputStyle => new StyleBuilder()
		.AddStyle("padding", ResolvedPadding)
		.AddStyle("border-radius", ResolvedRounding)
		.AddStyle("--moka-field-suffix-ch", SuffixWidth)
		.Build();

	private string InputCssClass => new CssBuilder("moka-numericfield-input")
		.AddClass($"moka-numericfield-input--{SizeToKebab(Size)}")
		.AddClass("moka-field-input--has-start-icon", StartIcon is not null)
		.AddClass("moka-field-input--has-suffix", HasSuffix)
		.AddClass("moka-field-input--mono", Mono)
		.Build();

	private bool HasSuffix => !string.IsNullOrEmpty(Suffix);

	private string SuffixId => $"{InputId}-suffix";

	private string? SuffixWidth => HasSuffix ? FormattableString.Invariant($"{Suffix!.Length}ch") : null;

	private string? DescribedBy => MokaAttributes.DescribedBy(AdditionalAttributes, HasSuffix ? SuffixId : null,
		MokaFieldWrapper.HelperIdIfShown(InputId, HelperText, ResolvedErrorText));

	private IReadOnlyDictionary<string, object>? InputAttributes => MokaAttributes.WithoutDescribedBy(AdditionalAttributes);

	// The numeric keypad has no decimal point on some phones, so a type with fractions asks for the decimal one.
	private static string InputMode => MokaNumberText<TValue>.HasFraction ? "decimal" : "numeric";

	private bool Steps => _hasStep && Comparer<TValue>.Default.Compare(Step!, MokaNumberText<TValue>.Zero) > 0;

	private string? InputRole => Steps ? "spinbutton" : null;

	private string? ValueNow => Steps ? MokaNumberText<TValue>.Format(CurrentValue) : null;

	private string? ValueMin => Steps && _hasMin ? MokaNumberText<TValue>.Format(Min) : null;

	private string? ValueMax => Steps && _hasMax ? MokaNumberText<TValue>.Format(Max) : null;

	/// <inheritdoc />
	/// <remarks>
	///     Written in the invariant culture, the one the text is read back in. InputBase would write the
	///     current culture's "1,5" on a German page, and the next edit would read it back as 15.
	/// </remarks>
	protected override string? FormatValueAsString(TValue? value) => MokaNumberText<TValue>.Format(value);

	/// <inheritdoc />
	/// <remarks>
	///     Notes which of <see cref="Min" />, <see cref="Max" /> and <see cref="Step" /> the parent passes. Without the
	///     struct constraint <c>TValue?</c> is <c>TValue</c> itself for a value type, so an unset <see cref="Min" /> on a
	///     field of <c>int</c> reads as 0 and only the parameters say whether it was set.
	/// </remarks>
	public override Task SetParametersAsync(ParameterView parameters)
	{
		_hasMin = IsPassed(parameters, nameof(Min));
		_hasMax = IsPassed(parameters, nameof(Max));
		_hasStep = IsPassed(parameters, nameof(Step));
		return base.SetParametersAsync(parameters);
	}

	/// <summary>
	///     Steps the value from the text in the box. Called from the field's script on Arrow Up (+1) and Arrow Down (-1)
	///     while it has a <see cref="Step" />. An empty box steps from zero, and text that isn't a number stays as it is.
	/// </summary>
	/// <param name="direction">Above zero steps up, otherwise down.</param>
	[JSInvokable]
	public void StepBy(int direction)
	{
		if (!Steps || Disabled || ReadOnly)
		{
			return;
		}

		TValue from;
		if (string.IsNullOrWhiteSpace(InputText))
		{
			from = MokaNumberText<TValue>.Zero;
		}
		else if (!MokaNumberText<TValue>.TryParse(InputText, out from))
		{
			return;
		}

		TValue to = Clamp(MokaNumberText<TValue>.Step(from, Step!, direction > 0));
		HandleInputWithDebounce(MokaNumberText<TValue>.Format(to));
		StateHasChanged();
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);
		if (Steps == _stepKeysBound)
		{
			return;
		}

		_stepKeysBound = Steps;
		if (Steps)
		{
			_dotNetRef ??= DotNetObjectReference.Create(this);
			await SafeModuleInvokeVoidAsync(ModulePath, "bindSteps", _input, _dotNetRef);
		}
		else
		{
			await SafeModuleInvokeVoidAsync(ModulePath, "unbindSteps", _input);
		}
	}

	/// <inheritdoc />
	/// <remarks>
	///     An empty box is null for a nullable type, and zero for any other. A number outside <see cref="Min" /> and
	///     <see cref="Max" /> is reported as the nearer one, so the value is never out of range, even when Enter submits
	///     the form before the field loses focus.
	/// </remarks>
	protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			result = Clamp(default!);
			validationErrorMessage = string.Empty;
			return true;
		}

		if (MokaNumberText<TValue>.TryParse(value, out TValue typed))
		{
			result = Clamp(typed);
			validationErrorMessage = string.Empty;
			return true;
		}

		result = default!;
		validationErrorMessage = $"'{value}' is not a valid number.";
		return false;
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		_dotNetRef?.Dispose();
		await base.DisposeAsyncCore();
	}

	private static bool IsPassed(ParameterView parameters, string name) =>
		parameters.TryGetValue(name, out TValue? value) && value is not null;

	private TValue Clamp(TValue value)
	{
		if (value is null)
		{
			return value;
		}

		if (_hasMin && Comparer<TValue>.Default.Compare(value, Min!) < 0)
		{
			return Min!;
		}

		return _hasMax && Comparer<TValue>.Default.Compare(value, Max!) > 0 ? Max! : value;
	}

	// While focused the box keeps what was typed, as it keeps "1." on the way to "1.5". Once focus leaves, a number that
	// was reported as Min or Max, or an empty box reported as Min, shows what the field reported instead.
	private async Task HandleNumberBlurAsync()
	{
		if (BoxShowsAnotherValue())
		{
			ShowValueText();
		}

		await HandleBlurAsync();
	}

	private bool BoxShowsAnotherValue()
	{
		string? text = InputText;
		TValue? shown = string.IsNullOrWhiteSpace(text) ? default
			: MokaNumberText<TValue>.TryParse(text, out TValue typed) ? typed
			: CurrentValue;
		return !EqualityComparer<TValue?>.Default.Equals(shown, CurrentValue);
	}
}
