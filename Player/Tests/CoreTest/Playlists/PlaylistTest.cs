/***
 * TrackTest.cs
 * 
 * The testing suite for the Track class of Core.
 * 
 * * * * * * * * *
 * 
 * Copyright 2014 Simplare
 * 
 * This code is part of the Stoffi Music Player Project.
 * Visit our website at: stoffiplayer.com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version
 * 3 of the License, or (at your option) any later version.
 * 
 * See stoffiplayer.com/license for more information.
 ***/

using System;
using System.Collections.Generic;

using NUnit.Framework;

using Stoffi.Core.Media;
using Stoffi.Core.Playlists;

namespace Playlists
{
	[TestFixture ()]
	public class PlaylistTest
	{
		[Test ()]
		public void TestNavigationID ()
		{
			var playlist = new Playlist ();
			Assert.AreEqual ("Playlist::0", playlist.NavigationID);
			playlist.Name = "Foo";
			Assert.AreEqual ("Playlist:Foo:0", playlist.NavigationID);
			playlist.ID = 3;
			Assert.AreEqual ("Playlist:Foo:3", playlist.NavigationID);
		}

		[Test ()]
		public void TestName ()
		{
			var fired = false;
			var playlist = new Playlist ();
			playlist.PropertyChanged += (o, e) => { if (e.PropertyName == "Name") { fired = true; }};
			playlist.Name = "Foo";
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestID ()
		{
			var fired = false;
			var playlist = new Playlist ();
			playlist.PropertyChanged += (o, e) => { if (e.PropertyName == "ID") { fired = true; }};
			playlist.ID = 1;
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestTime ()
		{
			var fired = false;
			var playlist = new Playlist ();
			playlist.PropertyChanged += (o, e) => { if (e.PropertyName == "Time") { fired = true; }};
			playlist.Time = 1;
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestOwnerID ()
		{
			var fired = false;
			var playlist = new Playlist ();
			playlist.PropertyChanged += (o, e) => { if (e.PropertyName == "OwnerID") { fired = true; }};
			playlist.OwnerID = 1;
			playlist.OwnerID = 2;
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestOwnerName ()
		{
			var fired = false;
			var playlist = new Playlist ();
			playlist.PropertyChanged += (o, e) => { if (e.PropertyName == "OwnerName") { fired = true; }};
			playlist.OwnerName = "Foo";
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestOwnerCacheTime ()
		{
			var fired = false;
			var playlist = new Playlist ();
			playlist.PropertyChanged += (o, e) => { if (e.PropertyName == "OwnerCacheTime") { fired = true; }};
			playlist.OwnerCacheTime = DateTime.Now;
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestFilter ()
		{
			var fired = false;
			var playlist = new Playlist ();
			playlist.PropertyChanged += (o, e) => { if (e.PropertyName == "Filter") { fired = true; }};
			playlist.Filter = "Foo";
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestTracks ()
		{
			var fired = false;
			var playlist = new Playlist ();
			playlist.PropertyChanged += (o, e) => { if (e.PropertyName == "Tracks") { fired = true; }};
			playlist.Tracks.Add (new Track ());
			Assert.IsTrue (fired);

			fired = false;
			playlist.TrackChanged += (o, e) => { if (e.PropertyName == "Title") { fired = true; }};
			playlist.Tracks [0].Title = "Foo";
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestListConfig ()
		{
			var fired = false;
			var playlist = new Playlist ();
			playlist.ListConfig.Columns.Add (new Stoffi.Core.Settings.ListColumn ());
			playlist.PropertyChanged += (o, e) => { if (e.PropertyName == "ListConfig") { fired = true; }};
			playlist.ListConfig.Columns [0].Name = "Foo";
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestType ()
		{
			var playlist = new Playlist ();
			Assert.AreEqual (PlaylistType.Standard, playlist.Type);
			playlist.Filter = "Foo";
			Assert.AreEqual (PlaylistType.Dynamic, playlist.Type);
			playlist.Filter = " ";
			Assert.AreEqual (PlaylistType.Standard, playlist.Type);
		}

		[Test ()]
		public void TestAdd ()
		{
			var playlist = new Playlist ();
			playlist.Name = "Foo";
			var tracks = new List<Track> ();
			tracks.Add (new Track{ Path = "file1.mp3" });
			tracks.Add (new Track{ Path = "file2.mp3" });
			tracks.Add (new Track{ Path = "file3.mp3" });
			playlist.Add (tracks);
			Assert.AreEqual (3, playlist.Tracks.Count);
			Assert.AreEqual ("Playlist:Foo", playlist.Tracks[0].Source);

			tracks.Add (new Track{ Path = "file4.mp3" });
			tracks.Add (new Track{ Path = "file1.mp3" });
			playlist.Add (tracks);
			//Assert.AreEqual (4, playlist.Tracks.Count);
			Assert.AreEqual ("file2.mp3", playlist.Tracks[0].Path);
			Assert.AreEqual ("file3.mp3", playlist.Tracks[1].Path);
			Assert.AreEqual ("file4.mp3", playlist.Tracks[2].Path);
			Assert.AreEqual ("file1.mp3", playlist.Tracks[3].Path);

			tracks.Clear ();
			tracks.Add (new Track () { Path = "file5.mp3" });
			playlist.Add (tracks, 2);
			Assert.AreEqual (5, playlist.Tracks.Count);
			Assert.AreEqual ("file5.mp3", playlist.Tracks[2].Path);
		}

		[Test ()]
		public void TestRemove ()
		{
			var playlist = new Playlist ();
			playlist.Tracks.Add (new Track{ Path = "file1.mp3" });
			playlist.Tracks.Add (new Track{ Path = "file2.mp3" });
			playlist.Tracks.Add (new Track{ Path = "file3.mp3" });
			playlist.Tracks.Add (new Track{ Path = "file4.mp3" });
			playlist.Tracks.Add (new Track{ Path = "file5.mp3" });

			var tracks = new List<Track> ();
			tracks.Add (new Track{ Path = "file2.mp3" });
			tracks.Add (new Track{ Path = "file3.mp3" });
			tracks.Add (new Track{ Path = "file5.mp3" });
			playlist.Remove (tracks);

			Assert.AreEqual (2, playlist.Tracks.Count);
			Assert.AreEqual ("file4.mp3", playlist.Tracks[1].Path);
		}

		[Test ()]
		public void TestRefresh ()
		{
			var playlist = new Playlist ();

			var tracks = new List<Track> ();
			tracks.Add (new Track{ Path = "file1.mp3", Title = "Foobar" });
			tracks.Add (new Track{ Path = "file2.mp3", Title = "Foo" });
			tracks.Add (new Track{ Path = "file3.mp3", Title = "Bar" });
			tracks.Add (new Track{ Path = "file4.mp3", Title = "Something" });

			playlist.Filter = "foo";
			playlist.Refresh (tracks);

			Assert.AreEqual (2, playlist.Tracks.Count);
			Assert.AreEqual ("file1.mp3", playlist.Tracks[0].Path);

			playlist.Filter = "some";
			playlist.Refresh (tracks);

			Assert.AreEqual (1, playlist.Tracks.Count);
			Assert.AreEqual ("file4.mp3", playlist.Tracks[0].Path);
		}
	}
}