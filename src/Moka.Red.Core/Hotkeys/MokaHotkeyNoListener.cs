namespace Moka.Red.Core.Hotkeys;

internal sealed class MokaHotkeyNoListener : IDisposable
{
	public static MokaHotkeyNoListener Instance { get; } = new();

	public void Dispose()
	{
	}
}
