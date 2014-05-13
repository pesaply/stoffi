using System;
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;

using Stoffi.Core;
using Stoffi.GUI.Views;

using SettingsManager = Stoffi.Core.Settings.Manager;
using PlaylistManager = Stoffi.Core.Playlists.Manager;

namespace Stoffi.GUI
{
	public partial class AppDelegate : NSApplicationDelegate
	{
		MainWindowController mainWindowController;
		PreferencesWindowController preferencesWindowController;
		EqualizerWindowController equalizerWindowController;
		GeneratorWindowController generatorWindowController;

		public AppDelegate ()
		{
		}

		public override void FinishedLaunching (NSObject notification)
		{
			mainWindowController = new MainWindowController ();
			mainWindowController.Window.MakeKeyAndOrderFront (this);
		}

		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			return true;
		}

		public override void WillTerminate (NSNotification notification)
		{
			U.L (LogLevel.Debug, "App", "Application is terminating");
			mainWindowController.Window.Close ();
			U.L (LogLevel.Debug, "App", "Saving settings to database");
			SettingsManager.Save ();
			while (SettingsManager.IsWriting) ;
			U.L (LogLevel.Debug, "App", "Settings saved");
		}

		partial void ShowPreferencesWindow(NSObject sender)
		{
			if (preferencesWindowController == null)
				preferencesWindowController = new PreferencesWindowController();
			preferencesWindowController.ShowWindow(this);
		}

		partial void ShowEqualizerWindow(NSObject sender)
		{
			if (equalizerWindowController == null)
				equalizerWindowController = new EqualizerWindowController();
			equalizerWindowController.ShowWindow(this);
		}

		partial void ShowGeneratorWindow(NSObject sender)
		{
			if (generatorWindowController == null)
				generatorWindowController = new GeneratorWindowController();
			generatorWindowController.ShowWindow(this);
		}
	}
}

