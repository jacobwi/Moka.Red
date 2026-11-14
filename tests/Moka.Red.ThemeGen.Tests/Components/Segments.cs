using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Moka.Red.ThemeGen.Tests.Components;

// A segment's radio carries no value of its own, so a test finds it by the words on its label: the first one in the page.
internal static class Segments
{
	public static IElement Segment<TComponent>(this IRenderedComponent<TComponent> cut, string text)
		where TComponent : IComponent =>
		cut.FindAll(".moka-segment")
			.First(label => string.Equals(label.QuerySelector(".moka-segment__text")?.TextContent.Trim(), text, StringComparison.Ordinal))
			.QuerySelector("input")!;

	public static Task PickAsync<TComponent>(this IRenderedComponent<TComponent> cut, string text)
		where TComponent : IComponent =>
		cut.Segment(text).ChangeAsync(new ChangeEventArgs { Value = "on" });
}
