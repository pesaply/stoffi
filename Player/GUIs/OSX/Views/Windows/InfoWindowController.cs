using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using MonoMac.Foundation;
using MonoMac.AppKit;

using Stoffi.Core;
using Stoffi.Core.Media;
using Stoffi.Core.Sources;

using MediaManager = Stoffi.Core.Media.Manager;
using ServiceManager = Stoffi.Core.Services.Manager;

namespace Stoffi.GUI.Views
{
	public partial class InfoWindowController : MonoMac.AppKit.NSWindowController
	{
		#region Members

		private List<NSView> generalLabels = new List<NSView>();
		private List<NSView> generalValues = new List<NSView>();
		private List<NSView> moreLabels = new List<NSView>();
		private List<NSView> moreValues = new List<NSView>();
		private Track track;
		private bool togglingGeneral = false;
		private bool togglingMore = false;

		#endregion

		#region Properties
		//strongly typed window accessor
		public new InfoWindow Window {
			get {
				return (InfoWindow)base.Window;
			}
		}
		#endregion

		#region Constructors
		// Called when created from unmanaged code
		public InfoWindowController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public InfoWindowController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		// Call to load from the XIB/NIB file
		public InfoWindowController (Track track) : base ("InfoWindow")
		{
			if (track == null)
				throw new NullReferenceException ("Cannot initialize info window with null track.");
			this.track = track;
			this.track.PropertyChanged += Track_PropertyChanged;
			Initialize ();
		}
		// Shared initialization code
		void Initialize ()
		{
		}
		#endregion

		#region Methods

		#region Private

		/// <summary>
		/// Write the state of the track to disk.
		/// </summary>
		private void Save()
		{
			try
			{
				Files.SaveTrack(track);
			}
			catch (Exception e) {
				U.L (LogLevel.Error, "Info", "Could not save data: " + e.Message);
			}
		}

		/// <summary>
		/// Refresh the visibility of the fields depending on the type of track.
		/// </summary>
		private void RefreshVisibleFields()
		{
			if (track == null)
				return;
			var t = track.Type;
			if (t == TrackType.Unknown)
				throw new NotSupportedException ("Unknown track type.");
			
			LengthLabel.Hidden = t == TrackType.WebRadio;
			GenreLabel.Hidden = t == TrackType.YouTube;
			TrackLabel.Hidden = t != TrackType.File;
			YearLabel.Hidden = t == TrackType.WebRadio;
			BitrateLabel.Hidden = t != TrackType.File;
			SamplingLabel.Hidden = t != TrackType.File;
			ChannelsLabel.Hidden = t != TrackType.File;
			CodecsLabel.Hidden = t != TrackType.File;
			URLLabel.Hidden = t == TrackType.File;
			PlayCountLabel.Hidden = t != TrackType.File && t != TrackType.WebRadio;
			ViewsLabel.Hidden = t == TrackType.File || t == TrackType.WebRadio;
			LastPlayedLabel.Hidden = t != TrackType.File && t != TrackType.WebRadio;

			LengthValue.Hidden = LengthLabel.Hidden;
			GenreValue.Hidden = GenreLabel.Hidden;
			TrackValue.Hidden = TrackLabel.Hidden;
			YearValue.Hidden = YearLabel.Hidden;
			BitrateValue.Hidden = BitrateLabel.Hidden;
			SamplingValue.Hidden = SamplingLabel.Hidden;
			ChannelsValue.Hidden = ChannelsLabel.Hidden;
			CodecsValue.Hidden = CodecsLabel.Hidden;
			URLValue.Hidden = URLLabel.Hidden;
			PlayCountValue.Hidden = PlayCountLabel.Hidden;
			ViewsValue.Hidden = ViewsLabel.Hidden;
			LastPlayedValue.Hidden = LastPlayedLabel.Hidden;
		}

		/// <summary>
		/// Refresh the editability of the fields depending on the type of track.
		/// </summary>
		private void RefreshEditableFields()
		{
			if (track == null)
				return;
			var t = track.Type;
			if (t == TrackType.Unknown)
				throw new NotSupportedException ("Unknown track type.");

			bool fileOrRadio = t == TrackType.File || t == TrackType.WebRadio;
			Title.Editable = fileOrRadio;
			Artist.Editable = fileOrRadio;
			AlbumArt.Editable = fileOrRadio;
			TitleValue.Editable = fileOrRadio;
			AlbumValue.Editable = fileOrRadio;
			ArtistValue.Editable = fileOrRadio;
			GenreValue.Editable = fileOrRadio;
			TrackValue.Editable = fileOrRadio;
			YearValue.Editable = fileOrRadio;
		}

		/// <summary>
		/// Sets the window height according to the height of the groups.
		/// </summary>
		/// <param name="animate">If set to <c>true</c> animate.</param>
		private void RefreshWindowHeight(bool animate = false)
		{
			float winH = 132 + General.Frame.Height + More.Frame.Height;
			SetHeight (this.Window, winH, animate);
		}

		/// <summary>
		/// Refresh the heights of the field groups and the window.
		/// </summary>
		private void RefreshHeights(bool animate = false)
		{
			// general
			bool generalVisible = GeneralButton.State == NSCellStateValue.On;
			float generalH = generalVisible ? GetHeight(generalLabels) : 0;
			SetHeight (General, generalH, animate);

			// more
			bool moreVisible = MoreButton.State == NSCellStateValue.On;
			float moreH = moreVisible ? GetHeight(moreLabels) : 0;
			SetHeight (More, moreH, animate);
			
			// window
			RefreshWindowHeight ();
		}

		/// <summary>
		/// Refresh the widths of the labels and the x position of the corresponding values.
		/// </summary>
		/// <param name="labels">Labels.</param>
		/// <param name="values">Values.</param>
		private void RefreshWidths(List<NSView> labels, List<NSView> values)
		{
			float widest = 0;
			foreach (var l in labels) {
				if (l.Hidden)
					continue;
				var t = l as NSTextField;
				var w = t.AttributedStringValue.Size.Width;
				if (w > widest)
					widest = w;
			}
			widest += 16;
			float valueWidth = this.Window.Frame.Width - widest;
			for (int i=0; i < labels.Count; i++) {
				if (labels [i].Hidden)
					continue;
				SetX (labels [i], 0);
				SetWidth (labels [i], widest);
				SetX (values [i], widest);
				SetWidth (values [i], valueWidth);
			}
		}

		/// <summary>
		/// Refreshs the positions of the labels and values in the Y  dimension.
		/// </summary>
		/// <param name="labels">Labels.</param>
		/// <param name="values">Values.</param>
		private void RefreshFieldPositions(List<NSView> labels, List<NSView> values)
		{
			float pos = 5;
			for (int i=labels.Count-1; i >= 0; i--) {
				var l = labels [i];
				var v = values [i];

				if (!l.Hidden) {
					SetY (l, pos);
					SetY (v, pos);
					pos += l.Frame.Height;
				}
			}
		}

		/// <summary>
		/// Refreshs the positions of the field groups.
		/// </summary>
		private void RefreshPositions()
		{
			RefreshFieldPositions (generalLabels, generalValues);
			RefreshFieldPositions (moreLabels, moreValues);
			RefreshGroupPositions ();
		}

		/// <summary>
		/// Refreshes the Y positions of the group titles, their disclosure buttons, and the horizontal lines.
		/// </summary>
		private void RefreshGroupPositions()
		{
			// more
			float y = 3;
			SetY (More, y);
			y += More.Frame.Height;
			SetY (MoreLabel, y);
			y += 1;
			SetY (MoreButton, y);

			// general
			y += 14;
			SetY (GeneralLine, y);
			y += 6;
			SetY (General, y);
		}

		/// <summary>
		/// Refresh the layout of the labels and values.
		/// </summary>
		private void RefreshLayout()
		{
			RefreshHeights ();
			RefreshWidths (generalLabels, generalValues);
			RefreshWidths (moreLabels, moreValues);
			RefreshPositions ();
		}

		/// <summary>
		/// Refreshs the values according to the track.
		/// </summary>
		private void ApplyValues()
		{
			Title.StringValue = track.Title ?? "";
			Artist.StringValue = track.Artist ?? "";

			if (!String.IsNullOrWhiteSpace (track.ArtURL)) {
				if (File.Exists (track.ArtURL))
					AlbumArt.Image = new NSImage (track.ArtURL);
				else
					AlbumArt.Image = new NSImage (new NSUrl (track.ArtURL));
			}
			
			TitleValue.StringValue = track.Title ?? "";
			AlbumValue.StringValue = track.Album ?? "";
			ArtistValue.StringValue = track.Artist ?? "";
			GenreValue.StringValue = track.Genre ?? "";
			LengthValue.StringValue = U.TimeSpanToString(TimeSpan.FromSeconds(track.Length));
			YearValue.StringValue = track.Year == 0 ? "" : track.Year.ToString();
			TrackValue.StringValue = track.TrackNumber == 0 ? "" : track.TrackNumber.ToString();
			CodecsValue.StringValue = track.Codecs ?? "";
			BitrateValue.StringValue = U.T(track.Bitrate);
			SamplingValue.StringValue = U.T(track.SampleRate);
			ChannelsValue.StringValue = U.T(track.Channels);
			URLValue.StringValue = track.URL ?? "";
			PlayCountValue.StringValue = U.T(track.PlayCount);
			ViewsValue.StringValue = U.T(track.Views);
			LastPlayedValue.StringValue = U.T(track.LastPlayed);
			PathValue.StringValue = track.Path ?? "";
		}

		/// <summary>
		/// Refresh the GUI according to the track.
		/// </summary>
		private void Refresh()
		{
			RefreshVisibleFields ();
			RefreshEditableFields ();
			RefreshLayout ();
			ApplyValues ();
			var title = track.Path;
			if (track.Type == TrackType.File)
				title = Path.GetFileNameWithoutExtension (track.Path);
			else if (!String.IsNullOrWhiteSpace (track.Title))
				title = track.Title;
			Window.Title = String.Format ("{0} Info", title);
		}

		/// <summary>
		/// Gets the total height of all non-hidden objects.
		/// </summary>
		/// <returns>The height.</returns>
		/// <param name="objects">Objects.</param>
		private float GetHeight(List<NSView> objects)
		{
			float h = 15;
			foreach (var o in objects)
				if (!o.Hidden)
					h += o.Frame.Height;
			return h;
		}

		/// <summary>
		/// Set the Y position of a view.
		/// </summary>
		/// <param name="view">View.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="animate">Whether or not to animate the change.</param>
		private void SetY(NSView view, float y, bool animate = false)
		{
			SetFrame(view, new RectangleF (view.Frame.X, y, view.Frame.Width, view.Frame.Height), animate);
		}

		/// <summary>
		/// Set the X position of a view.
		/// </summary>
		/// <param name="view">View.</param>
		/// <param name="x">The x coordinate.</param>
		private void SetX(NSView view, float x)
		{
			view.Frame = new RectangleF (x, view.Frame.Y, view.Frame.Width, view.Frame.Height);
		}

		/// <summary>
		/// Set the height of a view.
		/// </summary>
		/// <param name="view">View.</param>
		/// <param name="height">Height.</param>
		/// <param name="animate">Whether or not to animate the change.</param>
		private void SetHeight(NSView view, float height, bool animate = false)
		{
			var diff = view.Frame.Height - height;
			SetFrame(view, new RectangleF (view.Frame.X, view.Frame.Y + diff, view.Frame.Width, height), animate);
		}

		/// <summary>
		/// Set the width of a view.
		/// </summary>
		/// <param name="view">View.</param>
		/// <param name="width">Width.</param>
		private void SetWidth(NSView view, float width)
		{
			view.Frame = new RectangleF (view.Frame.X, view.Frame.Y, width, view.Frame.Height);
		}

		/// <summary>
		/// Set the frame of a view.
		/// </summary>
		/// <param name="view">The view.</param>
		/// <param name="frame">The new frame.</param>
		/// <param name="animate">If set to <c>true</c> animate.</param>
		private void SetFrame(NSView view, RectangleF frame, bool animate)
		{
			var v = animate ? (NSView)view.Animator : view;
			v.Frame = frame;
		}

		/// <summary>
		/// Set the height of a window.
		/// </summary>
		/// <param name="window">Window.</param>
		/// <param name="height">Height.</param>
		/// <param name="animate">Whether or not to animate the change.</param>
		private void SetHeight(NSWindow window, float height, bool animate)
		{
			var diff = window.Frame.Height - height;
			var y = window.Frame.Y + diff;
			window.SetFrame(new RectangleF (window.Frame.X, y, window.Frame.Width, height), true, animate);
		}

		/// <summary>
		/// Toogle the visibility of a group.
		/// </summary>
		/// <param name="view">The group view.</param>
		private void ToggleGroup(NSView view)
		{
			if ((view == General && togglingGeneral) || (view == More && togglingMore))
				return;

			try
			{
				if (view == General)
					togglingGeneral = true;
				else if (view == More)
					togglingMore = true;

				var button = view == General ? GeneralButton : MoreButton;
				var labels = view == General ? generalLabels : moreLabels;
				while (true)
				{
					var goal = button.State == NSCellStateValue.Off ? 0 : GetHeight(labels);
					var diff = goal - view.Frame.Height;
					if (diff == 0)
						break;
					float step = diff > 0 ? 1 : -1;
					step *= 10;
					if (Math.Abs(diff) < Math.Abs(step))
						step = diff;
					SetHeight(view, view.Frame.Height + step);
					RefreshWindowHeight();
					RefreshGroupPositions();
					Thread.Sleep(10);
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Error, "Info", "Error occured while toggling field group: " + e.Message);
			}
			finally
			{
				if (view == General)
					togglingGeneral = false;
				else if (view == More)
					togglingMore = false;
			}
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the user toggles the General group.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void ToggleGeneral(NSObject sender)
		{
			ToggleGroup(General);
		}

		/// <summary>
		/// Invoked when the user toggles the More info group.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void ToggleMore(NSObject sender)
		{
			ToggleGroup(More);
		}

		/// <summary>
		/// Invoked when the user changes the album art.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void ChangeAlbumArt(NSObject sender)
		{
			var t = new Thread (delegate() {
				try
				{
					U.GUIContext.Post(_ => {
						var img = AlbumArt.Image;
						using (var imageData = img.AsTiff()) { 
							var imgRep = NSBitmapImageRep.ImageRepFromData(imageData) as NSBitmapImageRep;
							var imageProps = new NSDictionary();
							var data = imgRep.RepresentationUsingTypeProperties(NSBitmapImageFileType.Png, imageProps);
							ServiceManager.SetArt(track, Image.FromStream(data.AsStream()));
						}
					}, null);
				}
				catch (EntryPointNotFoundException e)
				{
					U.L(LogLevel.Error, "Info", "Could not set track art: " + e.Message);
				}
			});
			t.Name = "Set album art";
			t.Priority = ThreadPriority.Normal;
			t.Start();
		}

		/// <summary>
		/// Invoked when the user changes the title.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void ChangeTitle(NSObject sender)
		{
			var tf = sender as NSTextField;
			if (U.Equal(tf.StringValue, track.Title))
				return;
			track.Title = tf.StringValue;
			Save();
		}
		
		/// <summary>
		/// Invoked when the user changes the artist.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void ChangeArtist(NSObject sender)
		{
			var tf = sender as NSTextField;
			if (U.Equal(tf.StringValue, track.Artist))
				return;
			track.Artist = tf.StringValue;
			Save();
		}

		/// <summary>
		/// Invoked when the user changes the album.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void ChangeAlbum(NSObject sender)
		{
			var tf = sender as NSTextField;
			if (U.Equal(tf.StringValue, track.Album))
				return;
			track.Album = tf.StringValue;
			Save();
		}
		
		/// <summary>
		/// Invoked when the user changes the genre.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void ChangeGenre(NSObject sender)
		{
			var tf = sender as NSTextField;
			if (U.Equal(tf.StringValue, track.Genre))
				return;
			track.Genre = tf.StringValue;
			Save();
		}
		
		/// <summary>
		/// Invoked when the user changes the year.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void ChangeYear(NSObject sender)
		{
			var tf = sender as NSTextField;
			if (tf.IntValue != track.Year)
				return;
			track.Year = (uint)tf.IntValue;
			Save();
		}
		
		/// <summary>
		/// Invoked when the user changes the track.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		partial void ChangeTrack(NSObject sender)
		{
			var tf = sender as NSTextField;
			if (tf.IntValue != track.TrackNumber)
				return;
			track.TrackNumber = (uint)tf.IntValue;
			Save();
		}

		/// <summary>
		/// Invoked when a property of the track changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void Track_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			U.GUIContext.Post (_ => {
				try
				{
					switch (e.PropertyName) {
						case "Title":
						Title.StringValue = track.Title ?? "";
						TitleValue.StringValue = track.Title ?? "";
						break;

						case "Artist":
						Artist.StringValue = track.Artist ?? "";
						ArtistValue.StringValue = track.Artist ?? "";
						break;

						case "ArtURL":
						AlbumArt.Image = new NSImage (track.ArtURL);
						break;

						case "Album":
						AlbumValue.StringValue = track.Album ?? "";
						break;

						case "Genre":
						GenreValue.StringValue = track.Genre ?? "";
						break;

						case "Length":
						LengthValue.StringValue = U.TimeSpanToString(TimeSpan.FromSeconds(track.Length));
						break;

						case "Year":
						YearValue.StringValue = track.Year == 0 ? "" : track.Year.ToString();
						break;

						case "TrackNumber":
						TrackValue.StringValue = track.TrackNumber == 0 ? "" : track.TrackNumber.ToString();
						break;

						case "Path":
						PathValue.StringValue = track.Path ?? "";
						break;

						case "PlayCount":
						PlayCountValue.StringValue = U.T(track.PlayCount);
						break;

						case "URL":
						URLValue.StringValue = track.URL ?? "";
						break;

						case "Views":
						ViewsValue.StringValue = U.T(track.Views);
						break;

						case "LastPlayed":
						LastPlayedValue.StringValue = U.T(track.LastPlayed);
						break;
					}
				}
				catch (Exception exc) {
					U.L (LogLevel.Error, "Info", "Could not handle updated track property: " + exc.Message);
				}
			}, null);
		}

		#endregion

		#region Override

		/// <summary>
		/// Awakes from nib.
		/// </summary>
		public override void AwakeFromNib ()
		{
			generalLabels.Add (TitleLabel);
			generalLabels.Add (AlbumLabel);
			generalLabels.Add (ArtistLabel);
			generalLabels.Add (LengthLabel);
			generalLabels.Add (GenreLabel);
			generalLabels.Add (TrackLabel);
			generalLabels.Add (YearLabel);
			
			generalValues.Add (TitleValue);
			generalValues.Add (AlbumValue);
			generalValues.Add (ArtistValue);
			generalValues.Add (LengthValue);
			generalValues.Add (GenreValue);
			generalValues.Add (TrackValue);
			generalValues.Add (YearValue);

			moreLabels.Add (PathLabel);
			moreLabels.Add (BitrateLabel);
			moreLabels.Add (SamplingLabel);
			moreLabels.Add (ChannelsLabel);
			moreLabels.Add (CodecsLabel);
			moreLabels.Add (URLLabel);
			moreLabels.Add (PlayCountLabel);
			moreLabels.Add (ViewsLabel);
			moreLabels.Add (LastPlayedLabel);

			moreValues.Add (PathValue);
			moreValues.Add (BitrateValue);
			moreValues.Add (SamplingValue);
			moreValues.Add (ChannelsValue);
			moreValues.Add (CodecsValue);
			moreValues.Add (URLValue);
			moreValues.Add (PlayCountValue);
			moreValues.Add (ViewsValue);
			moreValues.Add (LastPlayedValue);

			Refresh ();

			base.AwakeFromNib ();
		}

		#endregion

		#endregion
	}
}

