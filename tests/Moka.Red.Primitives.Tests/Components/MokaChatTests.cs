using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Moka.Red.Primitives.Chat;

namespace Moka.Red.Primitives.Tests.Components;

public class MokaChatTests : BunitContext
{
	private const string DragModule = "./_content/Moka.Red.Core/moka-drag.js";

	public MokaChatTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	// The input used to cancel every keydown once it held text, which stopped typing after the
	// first character.
	[Fact]
	public async Task TypedText_DoesNotCancelFurtherKeys()
	{
		IRenderedComponent<MokaChat> cut = Render<MokaChat>(p => p.Add(x => x.OnSend, _ => { }));

		await cut.Find("textarea").InputAsync(new ChangeEventArgs { Value = "h" });

		Assert.False(cut.Find("textarea").HasAttribute("blazor:onkeydown:preventDefault"));
	}

	[Fact]
	public async Task Enter_Sends_ButShiftEnterAndImeEnterDoNot()
	{
		List<string> sent = [];
		IRenderedComponent<MokaChat> cut = Render<MokaChat>(p => p.Add(x => x.OnSend, text => sent.Add(text)));
		await cut.Find("textarea").InputAsync(new ChangeEventArgs { Value = "hi" });

		await cut.Find("textarea").KeyDownAsync(new KeyboardEventArgs { Key = "Enter", ShiftKey = true });
		await cut.Find("textarea").KeyDownAsync(new KeyboardEventArgs { Key = "Enter", IsComposing = true });
		Assert.Empty(sent);

		await cut.Find("textarea").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
		Assert.Equal(["hi"], sent);
	}

	[Fact]
	public void CancelsOnlyPlainEnter()
	{
		BunitJSModuleInterop drag = JSInterop.SetupModule(DragModule);

		Render<MokaChat>(p => p.Add(x => x.OnSend, _ => { }));

		Dictionary<string, object?> rule = Assert.Single(
			Assert.IsType<Dictionary<string, object?>[]>(drag.VerifyInvoke("preventKeys").Arguments[1]));
		Assert.Equal(["Enter"], (string[])rule["keys"]!);
		Assert.Equal(true, rule["unlessShift"]);
	}
}
