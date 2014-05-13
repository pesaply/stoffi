using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.WebKit;
using MonoMac.ObjCRuntime;

using Stoffi.Core;
using Stoffi.Core.Media;
using Stoffi.Core.Playlists;
using Stoffi.Core.Settings;
using Stoffi.Core.Sources;
using Stoffi.GUI.Models;
using Stoffi.GUI.Views;

using SettingsManager = Stoffi.Core.Settings.Manager;
using MediaManager = Stoffi.Core.Media.Manager;
using PlaylistManager = Stoffi.Core.Playlists.Manager;
using ServiceManager = Stoffi.Core.Services.Manager;
using SourceManager = Stoffi.Core.Sources.Manager;

namespace Stoffi.GUI
{
	public partial class MainWindowController : MonoMac.AppKit.NSWindowController
	{
		#region Members
		private Dictionary<string,TrackListViewController> trackLists = new Dictionary<string, TrackListViewController> ();
		private NavigationViewController navigationPane = null;
		#endregion

		#region Constructors
		// Called when created from unmanaged code
		public MainWindowController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public MainWindowController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		// Call to load from the XIB/NIB file
		public MainWindowController () : base ("MainWindow")
		{
			Initialize ();
		}
		// Shared initialization code
		void Initialize ()
		{
		}
		#endregion

		#region Properties
		//strongly typed window accessor
		public new MainWindow Window {
			get {
				return (MainWindow)base.Window;
			}
		}

		/// <summary>
		/// Gets or sets the navigation pane.
		/// </summary>
		/// <value>The navigation pane.</value>
		private NavigationViewController NavigationPane
		{
			get { return navigationPane; }
			set
			{
				foreach (var t in trackLists)
					t.Value.NavigationPane = value;
				navigationPane = value;
			}
		}
		#endregion

		#region Methods

		#region Private

		/// <summary>
		/// Play either the current track or the selected track.
		/// </summary>
		private void Play()
		{
			if (SettingsManager.CurrentTrack == null)
				SettingsManager.CurrentTrack = TrackList.SelectedTrack;
			MediaManager.Play ();
		}

		/// <summary>
		/// Refreshs the track info in the playback pane.
		/// </summary>
		private void RefreshTrackInfo()
		{
			var t = SettingsManager.CurrentTrack;
			if (t == null)
			{
				Window.Title = "Nothing is playing";
				TrackInfoPlus.StringValue = "N/A";
				TrackInfoMinus.StringValue = "N/A";
				TrackInfoSeek.FloatValue = 0;
			}
			else
			{
				var type = t.Type;
				if (type == TrackType.WebRadio) {
					Window.Title = t.Title;
					TrackInfoPlus.StringValue = "N/A";
					TrackInfoMinus.StringValue = "N/A";
					TrackInfoSeek.FloatValue = 0;
				} else {
					var pos = MediaManager.Position;
					var len = MediaManager.Length;
					var tPlus = TimeSpan.FromSeconds (pos);
					var tMinus = TimeSpan.FromSeconds (len - pos);
					if (tPlus.TotalSeconds < 0)
						TrackInfoPlus.StringValue = "N/A";
					else
						TrackInfoPlus.StringValue = U.TimeSpanToString (tPlus);
					if (tMinus.TotalSeconds < 0)
						TrackInfoMinus.StringValue = "N/A";
					else
						TrackInfoMinus.StringValue = U.TimeSpanToString (tMinus);

					var seek = SettingsManager.Seek * (TrackInfoSeek.MaxValue / 10.0);
					if (tPlus.TotalSeconds < 0 || Double.IsNaN (seek) || Double.IsInfinity (seek))
						seek = 0;

					TrackInfoSeek.DoubleValue = seek;

					Window.Title = String.Format ("{0} - {1}", t.Artist, t.Title);

				}
			}
		}

		/// <summary>
		/// Refreshes the content according to the navigation tree selection.
		/// </summary>
		private void RefreshContent()
		{
			var config = SettingsManager.GetSelectedListConfiguration();
			if (config != null)
			{
				Search.StringValue = config.Filter;
				Search.Enabled = true;
			}
			else
			{
				Search.StringValue = "";
				Search.Enabled = false;
			}

			switch (SettingsManager.CurrentSelectedNavigation)
			{
			case "Video":
				Visualizer.Hidden = true;
				Video.Hidden = false;
				TrackList.Hidden = true;
				break;

			case "Visualizer":
				Visualizer.Hidden = false;
				Video.Hidden = true;
				TrackList.Hidden = true;
				break;

			default:
				Visualizer.Hidden = true;
				Video.Hidden = true;
				ShowTrackList (SettingsManager.CurrentSelectedNavigation);
				break;
			}
		}

		/// <summary>
		/// Insert a view into the window.
		/// </summary>
		/// <param name="view">View.</param>
		private void InsertView(NSView view)
		{
			InsertView(view, view.Superview.Frame.Size);
		}

		/// <summary>
		/// Insert a view into the window.
		/// </summary>
		/// <param name="view">View.</param>
		/// <param name="size">Size.</param>
		private void InsertView(NSView view, SizeF size)
		{
			view.AutoresizingMask = NSViewResizingMask.HeightSizable | NSViewResizingMask.WidthSizable;
			view.Superview.AutoresizesSubviews = true;
			view.SetFrameSize (size);
		}

		/// <summary>
		/// Create a track list.
		/// </summary>
		/// <param name="key">Identifier for the list.</param>
		/// <param name="tracks">Track collection.</param>
		/// <param name="config">List view configuration.</param>
		private void CreateTrackList(string key, ObservableCollection<Track> tracks, ListConfig config)
		{
			var trackListController = new TrackListViewController ();
			var trackList = trackListController.View;
			trackListController.Config = config;
			trackListController.Tracks = tracks;
			trackListController.NavigationPane = NavigationPane;
			trackListController.StartLoadingAnimation (this);
			TrackList.Hidden = true;
			trackLists [key] = trackListController;
		}

		/// <summary>
		/// Removes a track list.
		/// </summary>
		/// <param name="key">Key.</param>
		private void RemoveTrackList(string key)
		{
			trackLists.Remove (key);
		}

		/// <summary>
		/// Makes a track list visible.
		/// </summary>
		/// <param name="key">Key.</param>
		private void ShowTrackList(string key)
		{
			if (trackLists.ContainsKey (key)) {
				var controller = trackLists [key];
				var view = controller.View;
				ContentContainer.ReplaceSubviewWith (TrackList, view);
				TrackList = view;
				InsertView (view);
				TrackList.Hidden = false;
			}
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the play-pause button is clicked.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		partial void PlayPauseClick(NSObject sender)
		{
			if (SettingsManager.MediaState == MediaState.Playing)
				MediaManager.Pause ();
			else
				Play ();
		}
		
		/// <summary>
		/// Invoked when the previous button is clicked.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		partial void PreviousClick(NSObject sender)
		{
			MediaManager.Previous ();
		}
		
		/// <summary>
		/// Invoked when the next button is clicked.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		partial void NextClick(NSObject sender)
		{
			MediaManager.Next (true);
		}
		
		/// <summary>
		/// Invoked when the seek slider is changed.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		partial void SeekChange(NSObject sender)
		{
			if (MediaManager.IsInitialized)
				MediaManager.Seek = (TrackInfoSeek.DoubleValue * 10.0) / TrackInfoSeek.MaxValue;
		}
		
		/// <summary>
		/// Invoked when the volume slider is changed.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		partial void VolumeChange(NSObject sender)
		{
			SettingsManager.Volume = Volume.DoubleValue;
		}
		
		/// <summary>
		/// Invoked when the repeat button is clicked.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		partial void RepeatClick(NSObject sender)
		{
			SettingsManager.Repeat = Repeat.State == NSCellStateValue.On ? RepeatState.RepeatAll : RepeatState.NoRepeat;
		}
		
		/// <summary>
		/// Invoked when the shuffle button is clicked.
		/// </summary>
		/// <param name="sender">The sender of the event.</param>
		partial void ShuffleClick(NSObject sender)
		{
			SettingsManager.Shuffle = Shuffle.State == NSCellStateValue.On;
		}

		/// <summary>
		/// Invoked when the search box is edited.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void SearchEdit(NSObject sender)
		{
			var config = SettingsManager.GetSelectedListConfiguration();

			switch (SettingsManager.SearchPolicy)
			{
			case SearchPolicy.Global:
				var q = Search.StringValue;
				SettingsManager.FileListConfig.Filter = q;
				SettingsManager.YouTubeListConfig.Filter = q;
				SettingsManager.SoundCloudListConfig.Filter = q;
				SettingsManager.RadioListConfig.Filter = q;
				SettingsManager.JamendoListConfig.Filter = q;
				SettingsManager.QueueListConfig.Filter = q;
				SettingsManager.HistoryListConfig.Filter = q;
				foreach (var p in SettingsManager.Playlists)
					p.Filter = q;
				break;

			case SearchPolicy.Partial:
				if (SettingsManager.CurrentSelectedNavigationIsPlaylist)
					foreach (var p in SettingsManager.Playlists)
						p.Filter = Search.StringValue;
				break;

			case SearchPolicy.Individual:
				if (config != null)
					config.Filter = Search.StringValue;
				break;
			}
		}

		/// <summary>
		/// Invoked when the property changes of the settings.
		/// </summary>
		/// <param name="sender">Sender of the event</param>
		/// <param name="e">The event data</param>
		private void Settings_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			U.GUIContext.Post (_ => 
			          {
				switch (e.PropertyName) {
				case "CurrentTrack":
					RefreshTrackInfo();
					break;

				case "MediaState":
					if (SettingsManager.MediaState == MediaState.Playing)
						PlayPauseButton.Image = NSImage.ImageNamed("pause");
					else
						PlayPauseButton.Image = NSImage.ImageNamed("play");
					break;

				case "Seek":
					RefreshTrackInfo();
					break;

				case "CurrentSelectedNavigation":
					RefreshContent();
					break;
				}
			}, null);
		}

		/// <summary>
		/// Invoked when a playlist is modified.
		/// </summary>
		/// <param name="sender">Sender of the event</param>
		/// <param name="e">The event data</param>
		void Playlists_PlaylistModified (object sender, ModifiedEventArgs e)
		{
			var playlist = sender as Playlist;
			switch (e.Type)
			{
			case ModifyType.Created:
				CreateTrackList (playlist.NavigationID, playlist.Tracks, playlist.ListConfig);
				if (SettingsManager.CurrentSelectedNavigation == playlist.NavigationID)
					RefreshContent();
				break;

			case ModifyType.Removed:
				RemoveTrackList (playlist.NavigationID);
				break;
			}
		}

		/// <summary>
		/// Invoked when the window is moved.
		/// </summary>
		/// <param name="sender">Sender of the event</param>
		/// <param name="e">The event data</param>
		private void Window_DidMove(object sender, EventArgs e)
		{
			SettingsManager.WinLeft = Window.Frame.Left;
			SettingsManager.WinTop = Window.Frame.Top;
			SettingsManager.WinWidth = Window.Frame.Width;
			SettingsManager.WinHeight = Window.Frame.Height;
		}

		/// <summary>
		/// Invoked when there is a change detected at a scan source.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void Files_SourcesModified(object sender, SourceModifiedEventArgs e)
		{
			if (e.ModificationType == SourceModificationType.Added)
				SettingsManager.FileTracks.Add (e.Track);

			else if (e.ModificationType == SourceModificationType.Removed)
				SettingsManager.FileTracks.Remove (e.Track);
		}

		#endregion

		#region Overrides

		/// <summary>
		/// Called when gets awoken from the nib.
		/// </summary>
		public override void AwakeFromNib()
		{
			base.AwakeFromNib ();
			NavigationPane = new NavigationViewController ();

			var fullPath = Uri.UnescapeDataString(new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath);
			U.Initialize (fullPath, SynchronizationContext.Current, LogLevel.Debug);
			SettingsManager.Initialize ();
			PlaylistManager.Initialize ();
			ServiceManager.Initialize ();
			MediaManager.Initialize ();
			MediaManager.FetchCollectionCallback = SettingsManager.GetActiveTrackCollection;
			Files.SourceModified += Files_SourcesModified;
			Files.Initialize ();

			var x = (float)SettingsManager.WinLeft;
			var y = (float)SettingsManager.WinTop;
			var w = (float)SettingsManager.WinWidth;
			var h = (float)SettingsManager.WinHeight;
			if (x >= 0 && y >= 0 && w >= 0 && h >= 0)
				Window.SetFrame (new RectangleF (x,y,w,h), true);
			Window.DidMoved += Window_DidMove;

			SettingsManager.PropertyChanged += Settings_PropertyChanged;
			PlaylistManager.PlaylistModified += Playlists_PlaylistModified;

			RefreshTrackInfo ();
			Volume.DoubleValue = SettingsManager.Volume;
			Repeat.State = SettingsManager.Repeat == RepeatState.NoRepeat ? NSCellStateValue.Off : NSCellStateValue.On;
			Shuffle.State = SettingsManager.Shuffle ? NSCellStateValue.On : NSCellStateValue.Off;

			var visualizer = new VisualizerViewController ().View;
			ContentContainer.ReplaceSubviewWith (Visualizer, visualizer);
			Visualizer = visualizer;
			InsertView (visualizer);
			Visualizer.Hidden = true;

			var video = new VideoViewController ().View;
			ContentContainer.ReplaceSubviewWith (Video, video);
			Video = video;
			InsertView (video);
			Video.Hidden = true;

			var lists = new List<Tuple<string,ObservableCollection<Track>,ListConfig>> ();
			lists.Add (new Tuple<string,ObservableCollection<Track>,ListConfig> ("Files", SettingsManager.FileTracks, SettingsManager.FileListConfig));
			lists.Add (new Tuple<string,ObservableCollection<Track>,ListConfig> ("Queue", SettingsManager.QueueTracks, SettingsManager.QueueListConfig));
			lists.Add (new Tuple<string,ObservableCollection<Track>,ListConfig> ("History", SettingsManager.HistoryTracks, SettingsManager.HistoryListConfig));
			lists.Add (new Tuple<string,ObservableCollection<Track>,ListConfig> ("YouTube", SourceManager.YouTube.Tracks, SettingsManager.YouTubeListConfig));
			lists.Add (new Tuple<string,ObservableCollection<Track>,ListConfig> ("SoundCloud", SourceManager.SoundCloud.Tracks, SettingsManager.SoundCloudListConfig));
			lists.Add (new Tuple<string,ObservableCollection<Track>,ListConfig> ("Jamendo", SourceManager.Jamendo.Tracks, SettingsManager.JamendoListConfig));
			lists.Add (new Tuple<string,ObservableCollection<Track>,ListConfig> ("Radio", SettingsManager.RadioTracks, SettingsManager.RadioListConfig));
			foreach (var l in lists)
				CreateTrackList (l.Item1, l.Item2, l.Item3);
			foreach (var playlist in SettingsManager.Playlists)
				CreateTrackList (playlist.NavigationID, playlist.Tracks, playlist.ListConfig);

			var navigation = navigationPane.View;
			VerticalSplit.ReplaceSubviewWith (Navigation, navigation);
			InsertView (navigation, Navigation.Frame.Size);
			Navigation = navigation;

			var statusBar = new StatusBarViewController ().View;
			Window.ContentView.ReplaceSubviewWith (StatusBar, statusBar);
			statusBar.AutoresizingMask = NSViewResizingMask.WidthSizable;
			statusBar.SetFrameSize (StatusBar.Frame.Size);
			StatusBar = statusBar;

			Window.SetContentBorderThickness (24, NSRectEdge.MinYEdge);

			RefreshContent ();

			Files.AddSystemFolders (true);
			SourceManager.YouTube.PopulateTracks (SettingsManager.YouTubeListConfig.Filter);
			SourceManager.SoundCloud.PopulateTracks (SettingsManager.SoundCloudListConfig.Filter);
			SourceManager.Jamendo.PopulateTracks (SettingsManager.JamendoListConfig.Filter);
			U.L (LogLevel.Debug, "Main window", "Awoken");
		}

		#endregion

		#endregion
	}
}

