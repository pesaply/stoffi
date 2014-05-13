using MonoMac.AppKit;

namespace Stoffi.GUI.Views
{
	public interface IPreferencesTab
	{
		string Name { get; }
		NSImage Icon { get; }
		NSView View { get; }
	}
}

