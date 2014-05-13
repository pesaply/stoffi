using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;

using MonoMac.Foundation;
using MonoMac.AppKit;

using Stoffi.Core;
using Stoffi.Core.Media;
using Stoffi.Core.Settings;
using Stoffi.Core.Sources;

using SettingsManager = Stoffi.Core.Settings.Manager;

namespace Stoffi.GUI.Views
{
	public partial class StatusBarViewController : MonoMac.AppKit.NSViewController
	{
		#region Members

		private List<NSView> controls = new List<NSView> ();
		private string[] qualities = new string[] { "default", "highres", "hd1080", "hd720", "large", "medium", "small" };
		private string[] filters = new string[] { "Music", "None" };
		private Timer refreshStatisticsDelay = null;

		#endregion

		#region Constructors
		// Called when created from unmanaged code
		public StatusBarViewController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public StatusBarViewController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		// Call to load from the XIB/NIB file
		public StatusBarViewController () : base ("StatusBarView", NSBundle.MainBundle)
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
		public new StatusBarView View {
			get {
				return (StatusBarView)base.View;
			}
		}
		#endregion

		#region Methods

		#region Private

		/// <summary>
		/// Align all the buttons according to their visibility.
		/// </summary>
		private void AlignControls()
		{
			float pos = View.Frame.Size.Width - 20;
			foreach (var o in controls) {
				if (!o.Hidden) {
					o.Frame = new RectangleF (new PointF (pos - o.Frame.Width, o.Frame.Y), o.Frame.Size);
					pos -= o.Frame.Width + 8;
				} else {
				}
			}
		}

		/// <summary>
		/// Refresh the YouTube quality selector.
		/// </summary>
		private void RefreshQuality()
		{
			int i;
			for (i=qualities.Count ()-1; i >= 0; i--)
				if (qualities [i] == SettingsManager.YouTubeQuality)
					break;
			Quality.SelectItemWithTag(i);
		}

		/// <summary>
		/// Refresh the YouTube filter selector.
		/// </summary>
		private void RefreshFilter()
		{
			int i;
			for (i=filters.Count ()-1; i >= 0; i--)
				if (filters [i] == SettingsManager.YouTubeFilter)
					break;
			Filter.SelectItemWithTag(i);
		}

		/// <summary>
		/// Refresh the view mode button.
		/// </summary>
		private void RefreshViewMode()
		{
			var config = SettingsManager.GetSelectedListConfiguration ();
			if (config != null && !ViewMode.Hidden) {
				ViewMode.SelectedSegment = config.Mode == Core.Settings.ViewMode.Icons ? 0 : 1;
			}
		}

		/// <summary>
		/// Refreshs the visualizer selector.
		/// </summary>
		private void RefreshVisualizer()
		{
		}

		/// <summary>
		/// Refresh the statistics for the currently selected collection.
		/// </summary>
		/// <param name="param">Track collection casted as object.</param>
		private void RefreshCollectionStatistics(object param)
		{
			var tracks = param as ObservableCollection<Track>;
			if (tracks == null)
				return;
			var t = new Thread (delegate() {
				var stats = U.CalculateCollectionStatistics (tracks);
				var statsString = "";
				var entities = tracks.Count == 1 ? "station" : "stations";
				if (SettingsManager.CurrentSelectedNavigation != "Radio")
				{
					statsString = ", " + U.HumanSize ((long)stats [3], false);
					entities = tracks.Count == 1 ? "track" : "tracks";
				}
				U.GUIContext.Post (_ => {
					Label.StringValue = String.Format ("{0} {1}{2}", tracks.Count, entities, statsString);
				}, null);
			});
			t.Name = "Calculate collection statistics";
			t.Priority = ThreadPriority.BelowNormal;
			t.Start ();
		}

		/// <summary>
		/// Refresh the visibility of the buttons.
		/// </summary>
		private void Refresh()
		{
			if (refreshStatisticsDelay != null)
				refreshStatisticsDelay.Dispose ();

			var n = SettingsManager.CurrentSelectedNavigation;
			Fullscreen.Hidden = n != "Video";
			ViewMode.Hidden = n == "Video" || n == "Visualizer";
			Visualizer.Hidden = n != "Visualizer";
			Quality.Hidden = n != "Video";
			Filter.Hidden = n != "YouTube";

			// TODO: remove this line when the icon grid actually works
			ViewMode.Hidden = true;

			AlignControls ();
			RefreshViewMode ();

			switch (n) {
			case "Video":
			case "Visualizer":
				Label.StringValue = "";
				break;

			case "YouTube":
			case "SoundCloud":
			case "Jamendo":
				Label.StringValue = "Stream music from " + n;
				break;

			default:
				var tracks = SettingsManager.GetSelectedTrackCollection ();
				if (tracks == null)
					Label.StringValue = "";
				else {
					tracks.CollectionChanged -= Tracks_CollectionChanged; // so we don't end up with a million handlers
					if (refreshStatisticsDelay != null)
						refreshStatisticsDelay.Dispose ();
					tracks.CollectionChanged += Tracks_CollectionChanged;
					RefreshCollectionStatistics (tracks);
				}
				break;
			}
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the user clicks the fullscreen button.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void FullscreenClick(NSObject sender)
		{
		}

		/// <summary>
		/// Invoked when the user clicks the view mode button.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void ViewModeChange(NSObject sender)
		{
			var config = SettingsManager.GetSelectedListConfiguration();
			if (config != null)
				config.Mode = ViewMode.SelectedSegment == 0 ? Core.Settings.ViewMode.Icons : Core.Settings.ViewMode.Details;
		}

		/// <summary>
		/// Invoked when the user changes the YouTube filter.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void FilterChange(NSObject sender)
		{
			SettingsManager.YouTubeFilter = filters[Filter.IndexOfSelectedItem];
		}

		/// <summary>
		/// Invoked when the user changes the YouTube quality.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void QualityChange(NSObject sender)
		{
			SettingsManager.YouTubeQuality = qualities[Quality.IndexOfSelectedItem];
		}

		/// <summary>
		/// Invoked when the user changes the current visualizer.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void VisualizerChange(NSObject sender)
		{
		}

		/// <summary>
		/// Invoked when a property of the settings manager changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		private void Settings_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			switch (e.PropertyName) {
			case "CurrentSelectedNavigation":
				Refresh ();
				break;
				
			case "YouTubeFilter":
				RefreshFilter ();
				break;
				
			case "YouTubeQuality":
				RefreshQuality ();
				break;
				
			case "CurrentVisualizer":
				RefreshVisualizer ();
				break;
			}
		}

		/// <summary>
		/// Invoked when the the scan progress changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		private void Files_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			U.GUIContext.Post (_ => {
				var state = e.UserState as string;
				if (state == "start") {
					Progress.Hidden = false;
					Progress.Indeterminate = true;
					Progress.StartAnimation (this.View.Window);
				} else
					Progress.Hidden = true;
			}, null);
		}

		/// <summary>
		/// Invoked when the current track collection changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		private void Tracks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			var tracks = sender as ObservableCollection<Track>;
			if (tracks != null && tracks == SettingsManager.GetSelectedTrackCollection ()) {
				if (refreshStatisticsDelay != null)
					refreshStatisticsDelay.Dispose ();
				refreshStatisticsDelay = new Timer (RefreshCollectionStatistics, tracks, 500, Timeout.Infinite);
			}
		}

		#endregion

		#region Overrides

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			// add in order right-to-left
			controls.Add (ViewMode);
			controls.Add (Visualizer);
			controls.Add (Quality);
			controls.Add (Filter);
			controls.Add (Fullscreen);

			SettingsManager.PropertyChanged += Settings_PropertyChanged;
			Files.ProgressChanged += Files_ProgressChanged;

			Progress.Hidden = true;

			Refresh ();
			RefreshQuality ();
			RefreshFilter ();
			RefreshVisualizer ();
		}

		#endregion

		#endregion
	}
}

