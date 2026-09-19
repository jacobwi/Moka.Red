using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Moka.Red.Core.Enums;
using Moka.Red.Forms.IpAddressInput;
using Moka.Red.Forms.MacAddressInput;
using Moka.Red.Forms.OtpInput;

namespace Moka.Red.Forms.Tests.Components;

public class MokaSegmentedInputTests : BunitContext
{
	public MokaSegmentedInputTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// Typing "." after "10" left "10." in the box. The filtered value matched the last render, so
	// Blazor sent the browser nothing to change. The box has to render the raw text once and then
	// the filtered value, which is the update that clears it.
	[Fact]
	public async Task IpAddress_RejectedSeparator_IsClearedFromTheBox()
	{
		IRenderedComponent<MokaIpAddressInput> cut = Render<MokaIpAddressInput>();
		await cut.FindAll("input")[0].InputAsync(new ChangeEventArgs { Value = "10" });

		List<string?> rendered = [];
		cut.OnMarkupUpdated += (_, _) => rendered.Add(cut.FindAll("input")[0].GetAttribute("value"));
		await cut.FindAll("input")[0].InputAsync(new ChangeEventArgs { Value = "10." });

		Assert.Contains("10.", rendered);
		Assert.Equal("10", rendered[^1]);
	}

	[Fact]
	public async Task Otp_RejectedLetter_IsClearedFromTheBox()
	{
		IRenderedComponent<MokaOtpInput> cut = Render<MokaOtpInput>();

		List<string?> rendered = [];
		cut.OnMarkupUpdated += (_, _) => rendered.Add(cut.FindAll("input")[0].GetAttribute("value"));
		await cut.FindAll("input")[0].InputAsync(new ChangeEventArgs { Value = "a" });

		Assert.Contains("a", rendered);
		Assert.True(string.IsNullOrEmpty(rendered[^1]));
		Assert.True(string.IsNullOrEmpty(cut.Instance.Value));
	}

	// Size was documented but ignored: every IP input rendered at Md.
	[Theory]
	[InlineData(MokaSize.Xs, "moka-ip--xs")]
	[InlineData(MokaSize.Lg, "moka-ip--lg")]
	public void IpAddress_AppliesItsSize(MokaSize size, string expected)
	{
		IRenderedComponent<MokaIpAddressInput> cut = Render<MokaIpAddressInput>(p => p.Add(x => x.Size, size));

		Assert.Contains(expected, cut.Find(".moka-ip").ClassList);
	}

	// Typing wrote the Value parameter, so a parent passing Value one way put the old value back
	// on its next render.
	[Fact]
	public async Task Otp_TypedDigits_SurviveAParentRerender_UntilANewValueArrives()
	{
		IRenderedComponent<MokaOtpInput> cut = Render<MokaOtpInput>(p => p.Add(x => x.Value, "1"));
		await cut.FindAll("input")[1].InputAsync(new ChangeEventArgs { Value = "2" });

		cut.Render(p => p.Add(x => x.Value, "1"));
		Assert.Equal("2", cut.FindAll("input")[1].GetAttribute("value"));

		cut.Render(p => p.Add(x => x.Value, "9"));
		Assert.Equal("9", cut.FindAll("input")[0].GetAttribute("value"));
		Assert.True(string.IsNullOrEmpty(cut.FindAll("input")[1].GetAttribute("value")));
	}

	// Moving to the next box waited for a server round trip, so on Blazor Server a fast typist's next
	// key hit the full box and maxlength dropped it. The browser now moves focus itself.
	[Fact]
	public void TheBoxes_AreBoundToTheBrowsersFocusScript_WithTheirRules()
	{
		BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Moka.Red.Forms/moka-segments.js");
		module.Mode = JSRuntimeMode.Loose;

		IRenderedComponent<MokaIpAddressInput> ip = Render<MokaIpAddressInput>();
		IRenderedComponent<MokaOtpInput> otp = Render<MokaOtpInput>();
		IRenderedComponent<MokaMacAddressInput> mac = Render<MokaMacAddressInput>();

		Assert.Equal(3, module.VerifyInvoke("bindSegments", 3).Count);
		Assert.Equal(".", ip.Find(".moka-ip").GetAttribute("data-moka-separator"));
		Assert.All(ip.FindAll("input"), box => Assert.Equal("[0-9]", box.GetAttribute("data-moka-accept")));
		Assert.False(otp.Find(".moka-otp").HasAttribute("data-moka-separator"));
		Assert.All(otp.FindAll("input"), box => Assert.Equal("[0-9]", box.GetAttribute("data-moka-accept")));
		Assert.All(mac.FindAll("input"), box => Assert.Equal("[0-9a-fA-F]", box.GetAttribute("data-moka-accept")));
	}
}
