using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.Motion;

/// <summary>
///     Types text character by character with a blinking cursor.
///     Uses a timer to progressively reveal characters.
///     The cursor blinks via CSS animation — no JavaScript needed.
/// </summary>
public partial class MokaTypewriter : MokaComponentBase
{
	private int _charIndex;
	private bool _isTyping;
	private string _previousText = "";
	private Timer? _timer;
	private string _visibleText = "";

	/// <summary>The text to type out character by character.</summary>
	[Parameter]
	public string Text { get; set; } = "";

	/// <summary>Typing speed in milliseconds per character. Defaults to 50.</summary>
	[Parameter]
	public int Speed { get; set; } = 50;

	/// <summary>Delay in milliseconds before typing starts. Defaults to 0.</summary>
	[Parameter]
	public int Delay { get; set; }

	/// <summary>Whether to show a blinking cursor. Defaults to true.</summary>
	[Parameter]
	public bool ShowCursor { get; set; } = true;

	/// <summary>The character used for the cursor. Defaults to "|".</summary>
	[Parameter]
	public string CursorChar { get; set; } = "|";

	/// <summary>Whether to restart typing after finishing. Defaults to false.</summary>
	[Parameter]
	public bool Loop { get; set; }

	/// <summary>Callback invoked when typing completes.</summary>
	[Parameter]
	public EventCallback OnComplete { get; set; }

	/// <inheritdoc />
	protected override string RootClass => "moka-typewriter";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	/// <inheritdoc />
	protected override bool ShouldRender() => true;

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();

		if (Text != _previousText)
		{
			_previousText = Text;
			StartTyping();
		}
	}

	/// <inheritdoc />
	protected override void OnAfterRender(bool firstRender)
	{
		base.OnAfterRender(firstRender);

		if (firstRender && !_isTyping)
		{
			StartTyping();
		}
	}

	private void StartTyping()
	{
		StopTimer();
		_charIndex = 0;
		_visibleText = "";
		_isTyping = true;

		if (string.IsNullOrEmpty(Text))
		{
			_isTyping = false;
			return;
		}

		int delay = Delay > 0 ? Delay : 0;
		_timer = new Timer(OnTick, null, delay, Speed);
	}

	private void OnTick(object? state)
	{
		if (_charIndex < Text.Length)
		{
			_charIndex++;
			_visibleText = Text[.._charIndex];
			_ = InvokeAsync(StateHasChanged);
		}
		else
		{
			StopTimer();
			_isTyping = false;

			_ = InvokeAsync(async () =>
			{
				if (OnComplete.HasDelegate)
				{
					await OnComplete.InvokeAsync();
				}

				if (Loop)
				{
					StartTyping();
				}
			});
		}
	}

	private void StopTimer()
	{
		_timer?.Dispose();
		_timer = null;
	}

	/// <inheritdoc />
	protected override async ValueTask DisposeAsyncCore()
	{
		StopTimer();
		await base.DisposeAsyncCore();
	}
}
