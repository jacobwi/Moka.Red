using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Base;
using Moka.Red.Core.Utilities;

namespace Moka.Red.Primitives.ChainCheck;

/// <summary>
///     The parts between someone and what they want, in a row, with where the chain breaks. Each step has a round mark
///     in its tone, and below the row a list says what's so for each step, with a fix where one helps. For progress
///     through steps, use <c>MokaSteps</c>.
/// </summary>
public partial class MokaChainCheck : MokaVisualComponentBase
{
	/// <summary>The steps, in the order someone passes them.</summary>
	[Parameter]
	[EditorRequired]
	public IReadOnlyList<MokaChainStep> Steps { get; set; } = [];

	/// <summary>Names the row of steps for screen readers, such as "From your phone to the internet".</summary>
	[Parameter]
	public string? Label { get; set; }

	/// <summary>Names the list under the row for screen readers. Defaults to "Findings".</summary>
	[Parameter]
	public string? FindingsLabel { get; set; } = "Findings";

	/// <inheritdoc />
	protected override string RootClass => "moka-chain-check";

	/// <inheritdoc />
	protected override string CssClass => new CssBuilder(RootClass)
		.AddClass(Class)
		.Build();

	// The root draws nothing, so the radius shapes the rows, which do.

	/// <inheritdoc />
	protected override string? CssStyle => new StyleBuilder()
		.AddStyle("margin", ResolvedMargin)
		.AddStyle("padding", ResolvedPadding)
		.AddStyle(Style)
		.Build();

	private string? RowStyle => new StyleBuilder()
		.AddStyle("border-radius", ResolvedRounding)
		.Build();

	private bool HasFindings => Steps.Any(step => !string.IsNullOrEmpty(step.Sentence));

	private int FirstBreak()
	{
		for (int i = 0; i < Steps.Count; i++)
		{
			if (Steps[i].Tone == MokaChainTone.Fail)
			{
				return i;
			}
		}

		return -1;
	}

	// Past a break nothing can be judged. Wait and Off already look it, so only the judged tones fade.
	private static bool IsAfterBreak(MokaChainStep step, int index, int broken) =>
		broken >= 0 && index > broken && step.Tone is MokaChainTone.Ok or MokaChainTone.Warn or MokaChainTone.Fail;

	private static string StepClass(MokaChainStep step, bool afterBreak) => new CssBuilder("moka-chain-check__step")
		.AddClass($"moka-chain-check__step--{ToneName(step.Tone)}")
		.AddClass("moka-chain-check__step--after", afterBreak)
		.Build();

	private static string RowClass(MokaChainStep step, bool afterBreak) => new CssBuilder("moka-chain-check__row")
		.AddClass($"moka-chain-check__row--{ToneName(step.Tone)}")
		.AddClass("moka-chain-check__row--after", afterBreak)
		.Build();

	private static string ToneName(MokaChainTone tone) => tone switch
	{
		MokaChainTone.Ok => "ok",
		MokaChainTone.Warn => "warn",
		MokaChainTone.Fail => "fail",
		MokaChainTone.Wait => "wait",
		_ => "off"
	};
}
