using System.Xml.Linq;

namespace Moka.Red.Primitives.Tests.Components;

// MokaBarcode, MokaQRCode and MokaIdenticon render their SVG as a raw MarkupString, which bUnit keeps
// verbatim in the component's markup. This reads it back as XML: a stray quote or ampersand makes it
// fail to parse, and anything a parameter smuggled in shows up as an element or attribute the
// generators never write.
internal static class GeneratedSvg
{
	public static readonly XNamespace Ns = "http://www.w3.org/2000/svg";

	private static readonly HashSet<string> Elements = ["svg", "rect", "text"];

	private static readonly HashSet<string> Attributes =
	[
		"viewBox", "width", "height", "x", "y", "rx", "fill", "text-anchor", "dominant-baseline", "font-family",
		"font-size"
	];

	// Quotes and angle brackets that end an attribute or open an element, a bare ampersand, and CSS
	// that is not a color.
	public static TheoryData<string> HostileValues =>
	[
		"red' onload='alert(1)",
		"#000'/><image href='x' onerror='alert(1)'/><rect fill='",
		"\"><script>alert(1)</script>",
		"red&blue",
		"red; background: url(https://example.com/x)"
	];

	public static XDocument Parse(string markup)
	{
		int start = markup.IndexOf("<svg", StringComparison.Ordinal);
		int end = markup.LastIndexOf("</svg>", StringComparison.Ordinal);
		Assert.True(start >= 0 && end > start, "The markup holds no SVG.");

		XDocument svg = XDocument.Parse(markup[start..(end + "</svg>".Length)]);
		foreach (XElement element in svg.Descendants())
		{
			Assert.Equal(Ns, element.Name.Namespace);
			Assert.Contains(element.Name.LocalName, Elements);
			foreach (XAttribute attribute in element.Attributes().Where(a => !a.IsNamespaceDeclaration))
			{
				Assert.Contains(attribute.Name.LocalName, Attributes);
			}
		}

		return svg;
	}

	public static IEnumerable<XElement> Rects(XDocument svg) => svg.Descendants(Ns + "rect");

	public static string Fill(XElement element) => element.Attribute("fill")?.Value ?? "";
}
