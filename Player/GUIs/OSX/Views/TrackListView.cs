using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;

using Stoffi.Core;
using Stoffi.Core.Media;

using SettingsManager = Stoffi.Core.Settings.Manager;
using MediaManager = Stoffi.Core.Media.Manager;

using Stoffi.GUI.Models;

namespace Stoffi.GUI.Views
{
	public partial class TrackListView : MonoMac.AppKit.NSView
	{
		#region Constructors
		// Called when created from unmanaged code
		public TrackListView (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public TrackListView (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		// Shared initialization code
		void Initialize ()
		{
		}
		#endregion

		#region Properties
		
		/// <summary>
		/// Gets the track which is currently selected.
		/// </summary>
		public Track SelectedTrack
		{
			get {
				try
				{
					var ds = List.DataSource as TrackListDataSource;
					if (List.SelectedRowCount > 0 && List.SelectedRow >= 0 && List.SelectedRow < ds.Tracks.Count)
					{
						return ds.Tracks[List.SelectedRow];
					}
				}
				catch (Exception e) {
					U.L (LogLevel.Warning, "Main", "Could not get selected track: " + e.Message);
				}
				return null;
			}
		}
		
		/// <summary>
		/// Gets the tracks which are currently selected.
		/// </summary>
		public List<Track> SelectedTracks
		{
			get {
				var tracks = new List<Track> ();
				try
				{
					if (List.SelectedRowCount > 0)
					{
						var ds = List.DataSource as TrackListDataSource;
						foreach (int row in List.SelectedRows)
						{
							if (row >= 0 && row < ds.FilteredAndSortedTracks.Count)
								tracks.Add (ds.FilteredAndSortedTracks[row]);
						}
					}
				}
				catch (Exception e) {
					U.L (LogLevel.Warning, "Main", "Could not get selected tracks: " + e.Message);
				}
				return tracks;
			}
		}

		#endregion
	}

	public class MyCollectionViewItem : NSCollectionViewItem
	{
		private static readonly NSString EMPTY_NSSTRING = new NSString(string.Empty);
		private MyView view;

		public MyCollectionViewItem() : base()
		{

		}

		public MyCollectionViewItem(IntPtr ptr) : base(ptr)
		{

		}

		public override void LoadView ()
		{
			view = new MyView();
			View = view;
		}

		public override NSObject RepresentedObject 
		{
			get { return base.RepresentedObject; }

			set 
			{
				var item = value as TrackItem;
				if (value == null || item == null)
				{
					// Need to do this because setting RepresentedObject in base to null 
					// throws an exception because of the MonoMac generated wrappers,
					// and even though we don't have any null values, this will get 
					// called during initialization of the content with a null value.
					base.RepresentedObject = EMPTY_NSSTRING;
					view.Label.StringValue = string.Empty;
					view.Icon.Image = NSImage.ImageNamed ("default-album-art");
				}
				else
				{
					base.RepresentedObject = value;
					view.Label.StringValue = item.Label;
					view.Icon.Image = item.Icon;
				}
			}
		}
	}

	public class MyView : NSView
	{
		private NSTextField label;
		private NSImageView icon;

		public MyView() : base(new RectangleF(0,0,100,119))
		{
			icon = new NSImageView (new RectangleF(18,45,64,64));
			label = new NSTextField (new RectangleF (5, 5, 90, 35));
			label.Editable = false;
//			label.Enabled = false;
			label.Bordered = false;
			label.PreferredMaxLayoutWidth = 90;
			label.Alignment = NSTextAlignment.Center;
			label.Selectable = false;
//			label.Cell.LineBreakMode = NSLineBreakMode.TruncatingMiddle;
			label.Cell.UsesSingleLineMode = false;
			label.Cell.TruncatesLastVisibleLine = true;
			label.Cell.FocusRingType = NSFocusRingType.Default;
			label.Cell.Highlighted = true;
			label.Cell.Wraps = true;

			AddSubview (icon);
			AddSubview(label);
		}

		public NSTextField Label
		{
			get { return label; }        
		}

		public NSImageView Icon
		{
			get { return icon; }
		}
	}
}

