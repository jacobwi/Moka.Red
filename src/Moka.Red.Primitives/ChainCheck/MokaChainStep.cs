using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Icons;

namespace Moka.Red.Primitives.ChainCheck;

/// <summary>One part of a <see cref="MokaChainCheck" />, in the order someone passes them.</summary>
/// <param name="Icon">Drawn in the step's round mark.</param>
/// <param name="Label">The step's name, under its mark.</param>
/// <param name="Tone">How the step stands.</param>
/// <param name="Word">How it stands in a word or two, under its name. Screen readers hear it after the name, and it hides on a phone.</param>
/// <param name="Sentence">What's so, in words, and for a problem what's wrong. Drawn in the list below the chain. Empty leaves the step out of that list.</param>
/// <param name="Detail">What to do, or the numbers behind it, in smaller text under the sentence.</param>
/// <param name="Fix">A button or link that puts it right, at the end of the step's row. An outlined button in a warn or fail row takes the row's tone.</param>
public sealed record MokaChainStep(
	MokaIconDefinition Icon,
	string Label,
	MokaChainTone Tone,
	string Word,
	string Sentence,
	string? Detail = null,
	RenderFragment? Fix = null);
