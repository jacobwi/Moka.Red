using AngleSharp.Dom;
using Bunit;
using Moka.Red.Core.Clipboard;

namespace Moka.Red.Primitives.Tests.Components;

/// <summary>
///     What the browser does around a copy. Core's click listener copies an element's <c>data-moka-copy</c> inside the
///     click, then raises <c>mokacopied</c>; an element with <c>data-moka-copy-ask</c> raises <c>mokacopyask</c> first
///     and waits for <c>answerCopy</c>.
/// </summary>
internal static class CopyEvents
{
	public const string Module = "./_content/Moka.Red.Core/moka-copy.js";

	/// <summary>The text the browser copies when the element is clicked, or null when it copies nothing of its own.</summary>
	public static string? CopyText(this IElement element) => element.GetAttribute("data-moka-copy");

	/// <summary>Raises what the browser raises once the clipboard has answered.</summary>
	public static Task CopiedAsync(this IElement element, bool copied = true) =>
		element.TriggerEventAsync("onmokacopied", new MokaCopyEventArgs { Copied = copied });

	/// <summary>Raises what the browser raises when it needs the text from .NET.</summary>
	public static Task AskAsync(this IElement element) => element.TriggerEventAsync("onmokacopyask", EventArgs.Empty);

	/// <summary>The text .NET last handed the browser through <c>answerCopy</c>.</summary>
	public static string? AnsweredText(this BunitJSModuleInterop module) =>
		module.Invocations["answerCopy"] is [.., { Arguments: [_, string text] }] ? text : null;
}
