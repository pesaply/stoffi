using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

using Stoffi.Core;
using Stoffi.Core.Media;
using Stoffi.Core.Settings;

using MediaManager = Stoffi.Core.Media.Manager;
using SourceManager = Stoffi.Core.Sources.Manager;
using SettingsManager = Stoffi.Core.Settings.Manager;

using Stoffi.GUI.Models;

namespace Stoffi.GUI.Views
{
	public partial class VideoViewController : MonoMac.AppKit.NSViewController
	{
		#region Classes

		private class InvokeScriptState
		{
			public string Command { get; set; }
			public object Result { get; set; }
		}

		#endregion

		#region Members
		private bool showMediaError = false;
		private readonly object videoBrowserLock = new object ();
		#endregion

		#region Constructors
		// Called when created from unmanaged code
		public VideoViewController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public VideoViewController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		// Call to load from the XIB/NIB file
		public VideoViewController () : base ("VideoView", NSBundle.MainBundle)
		{
			Initialize ();
		}
		// Shared initialization code
		void Initialize ()
		{
		}
		#endregion

		#region Properties
		//strongly typed view accessor
		public new VideoView View {
			get {
				return (VideoView)base.View;
			}
		}
		#endregion

		#region Methods

		#region Public

		public object InvokeScript(string function, object[] param = null)
		{
			var state = new InvokeScriptState ();

			// construct command
			string args = "";
			var argList = new List<string> ();
			if (param != null)
			{
				foreach (var p in param) {
					if (p is string)
						argList.Add ("'" + p.ToString () + "'");

					else
						argList.Add (p.ToString ());
				}
				args = String.Join (",", argList);
			}
			state.Command = String.Format ("{0}({1});", function, args);

			U.GUIContext.Send (InvokeScript, state);
			return state.Result;
		}

		#endregion

		#region Private

		private void InvokeScript(object oState)
		{
			if (Browser.IsLoading)
				return;
			var state = oState as InvokeScriptState;
			string returnValue = null;
			lock (videoBrowserLock) {
				returnValue = Browser.StringByEvaluatingJavaScriptFromString (state.Command);
				state.Result = (object)returnValue;
			}
		}

		private void Refresh()
		{
			U.GUIContext.Post (_ => {
				bool show = SettingsManager.CurrentTrack != null && SettingsManager.CurrentTrack.Type == TrackType.YouTube;
				NoVideoMessage.Hidden = show;
				NoVideoBackground.Hidden = show;
				Browser.Hidden = !show;
			}, null);
		}

		#endregion

		#region Event handlers

		private void YouTube_ErrorOccured(object sender, string error)
		{
			U.GUIContext.Post (_ => {
				if (showMediaError) {
					var alert = new NSAlert ();
					alert.AlertStyle = NSAlertStyle.Warning;
					alert.InformativeText = error;
					alert.MessageText = "YouTube Error";
					alert.RunModal();
				}
			}, null);
		}

		private void YouTube_NoFlashDetected(object sender, EventArgs e)
		{
			U.GUIContext.Post (_ => {
				if (showMediaError) {
					var alert = new NSAlert ();
					alert.AlertStyle = NSAlertStyle.Warning;
					alert.InformativeText = "You need to install Flash for Safari in order to play YouTube tracks.\n\nWould you like to install it?";
					alert.MessageText = "No Flash Detected";
					alert.AddButton ("Yes");
					alert.AddButton ("No");
					if (alert.RunModal () == (int)NSAlertButtonReturn.First) {
						System.Diagnostics.Process.Start("open", "-a /Applications/Safari.app http://get.adobe.com/flashplayer");
					}
				}
			}, null);
		}

		private void YouTube_PlayerReady(object sender, EventArgs e)
		{
			SourceManager.YouTube.HasFlash = true;
			if (SettingsManager.CurrentTrack != null && SettingsManager.CurrentTrack.Type == TrackType.YouTube) {
				var vid = SourceManager.YouTube.GetID (SettingsManager.CurrentTrack.Path);
				InvokeScript ("setVolume", new object[] { SettingsManager.Volume });
				InvokeScript ("cueNewVideo", new object[] { vid, 0 });
				SettingsManager.Seek = 0;
			}
		}

		private void YouTube_DoubleClick(object sender, EventArgs e)
		{
		}

		private void YouTube_HideCursor(object sender, EventArgs e)
		{
		}

		private void YouTube_ShowCursor(object sender, EventArgs e)
		{
		}

		private void Settings_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			if (e.PropertyName == "CurrentTrack")
				Refresh ();
		}

		#endregion

		#region Overrides

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			
			Browser.ShouldCloseWithWindow = true;
			Browser.MainFrame.LoadRequest(new NSUrlRequest(new NSUrl("http://uncached-static.stoffiplayer.com/youtube/foo.htm")));
			var ypi = new VideoInterface ();
			ypi.ErrorOccured += YouTube_ErrorOccured;
			ypi.DoubleClick += YouTube_DoubleClick;
			ypi.NoFlashDetected += YouTube_NoFlashDetected;
			ypi.HideCursor += YouTube_HideCursor;
			ypi.ShowCursor += YouTube_ShowCursor;
			ypi.PlayerReady += YouTube_PlayerReady;
			Browser.WindowScriptObject.SetValueForKey (ypi, new NSString ("external"));
			
			MediaManager.InvokeScriptCallback = InvokeScript;

			SettingsManager.PropertyChanged += Settings_PropertyChanged;

			Refresh ();
		}

		#endregion

		#endregion
	}
}

