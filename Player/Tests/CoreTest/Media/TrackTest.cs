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

using NUnit.Framework;

using Stoffi.Core.Media;

namespace Media
{
	[TestFixture ()]
	public class TrackTest
	{
		[Test ()]
		public void TestArtist ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "Artist") { fired = true; }};
			track.Artist = "Foo";
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestTitle ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "Title") { fired = true; }};
			track.Title = "Foo";
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestAlbum ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "Album") { fired = true; }};
			track.Album = "Foo";
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestGenre ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "Genre") { fired = true; }};
			track.Genre = "Foo";
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestTrackNumber ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "TrackNumber") { fired = true; }};
			track.TrackNumber = 1;
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestYear ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "Year") { fired = true; }};
			track.Year = 2014;
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestLength ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "Length") { fired = true; }};
			track.Length = 1;
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestPath ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "Path") { fired = true; }};
			track.Path = "Foo";
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestPlayCount ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "PlayCount") { fired = true; }};
			track.PlayCount = 1;
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestURL ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "URL") { fired = true; }};
			track.URL = "Foo";
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestViews ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "Views") { fired = true; }};
			track.Views = 1;
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestLastPlayed ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "LastPlayed") { fired = true; }};
			track.LastPlayed = DateTime.Now;
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestArtURL ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "ArtURL") { fired = true; }};
			track.ArtURL = "Foo";
			Assert.IsTrue (fired);
		}
		[Test ()]
		public void TestOriginalArtURL ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "OriginalArtURL") { fired = true; }};
			track.OriginalArtURL = "Foo";
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestProcessed ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "Processed") { fired = true; }};
			track.Processed = true;
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestLastWrite ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "LastWrite") { fired = true; }};
			track.LastWrite = 1;
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestBitrate ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "Bitrate") { fired = true; }};
			track.Bitrate = 1;
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestChannels ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "Channels") { fired = true; }};
			track.Channels = 1;
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestSampleRate ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "SampleRate") { fired = true; }};
			track.SampleRate = 1;
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestCodecs ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "Codecs") { fired = true; }};
			track.Codecs = "Foo";
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestSource ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "Source") { fired = true; }};
			track.Source = "Foo";
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestBookmarks ()
		{
			var fired = false;
			var track = new Track ();
			track.PropertyChanged += (o, e) => { if (e.PropertyName == "Bookmarks") { fired = true; }};
			track.Bookmarks.Add(new Tuple<string,double>("foo",1));
			Assert.IsTrue (fired);
		}

		[Test ()]
		public void TestType ()
		{
			var track = new Track ();
			Assert.AreEqual (TrackType.Unknown, track.Type);
			track.Path = "/path/to/file.mp3";
			Assert.AreEqual (TrackType.File, track.Type);
			track.Path = "stoffi:jamendo:track:123";
			Assert.AreEqual (TrackType.Jamendo, track.Type);
			track.Path = "stoffi:soundcloud:track:123";
			Assert.AreEqual (TrackType.SoundCloud, track.Type);
			track.Path = "this?is:not\\a/valid*path";
			Assert.AreEqual (TrackType.Unknown, track.Type);
			track.Path = "http://example.com";
			Assert.AreEqual (TrackType.WebRadio, track.Type);
			track.Path = "http://example.mp3";
			Assert.AreEqual (TrackType.WebRadio, track.Type);
			track.Path = "http://example.com/foo/bar";
			Assert.AreEqual (TrackType.WebRadio, track.Type);
			track.Path = "http://example.com/foo/bar.pls";
			Assert.AreEqual (TrackType.File, track.Type); // this is actually not ideal
			track.Path = "http://example.com/foo/bar.mp3";
			Assert.AreEqual (TrackType.File, track.Type);
			track.Path = "https://example.com/foo/bar?something=file.mp3";
			Assert.AreEqual (TrackType.WebRadio, track.Type);
			track.Path = "https://example.com/foo/bar.mp3?domain=www.hello.com";
			Assert.AreEqual (TrackType.File, track.Type);
			track.Path = "stoffi:youtube:track:123";
			Assert.AreEqual (TrackType.YouTube, track.Type);
		}

		[Test ()]
		public void TestIcon ()
		{
			var track = new Track ();
			Assert.AreEqual ("unknown", track.Icon);
			track.Path = "/path/to/file.mp3";
			Assert.AreEqual ("file", track.Icon);
			track.Path = "stoffi:jamendo:track:123";
			Assert.AreEqual ("jamendo", track.Icon);
			track.Path = "stoffi:soundcloud:track:123";
			Assert.AreEqual ("soundcloud", track.Icon);
			track.Path = "stoffi:youtube:track:123";
			Assert.AreEqual ("youtube", track.Icon);
			track.Path = "http://example.com/foo";
			Assert.AreEqual ("radio", track.Icon);
		}
	}
}

