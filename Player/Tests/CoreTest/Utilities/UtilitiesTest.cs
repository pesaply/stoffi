/***
 * UtilitiesTest.cs
 * 
 * The testing suite for the Utilities class of Core.
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
using System.Collections.ObjectModel;

using NUnit.Framework;
using Newtonsoft.Json.Linq;

using Stoffi.Core;
using Stoffi.Core.Media;
using Stoffi.Core.Playlists;

namespace Utilities
{
	[TestFixture ()]
	public class UtilitiesTest
	{
		[Test ()]
		public void TestC ()
		{
			var arg = "this (doesn't) feel... right?";
			var trg = "this -doesn-t- feel--- right-";
			Assert.AreEqual (trg, U.C(arg));

			Assert.AreEqual (trg, U.C(new JValue(arg)));
			Assert.AreEqual ("Unknown", U.C (new JValue (true)));
			Assert.AreEqual ("Unknown", U.C (new JValue (DateTime.Now)));
		}

		[Test ()]
		public void TestContainsPath ()
		{
			var tracks = new ObservableCollection<Track> ();
			tracks.Add (new Track () { Path = "/path/to/first/file.mp3" });
			tracks.Add (new Track () { Path = "/path/to/second/file.mp3" });
			tracks.Add (new Track () { Path = "/path/to/yet/anotherfile.wav" });

			Assert.AreEqual (true, U.ContainsPath (tracks, "/path/to/second/file.mp3"));
			Assert.AreEqual (false, U.ContainsPath (tracks, "/path/to/second/file.wav"));
		}

		[Test ()]
		public void TestGetTracks ()
		{
			var tracks = new ObservableCollection<Track> ();
			tracks.Add (new Track () { Path = "/path/to/file.mp3" });
			tracks.Add (new Track () { Path = "/path/to/anotherfile.mp3" });
			tracks.Add (new Track () { Path = "/path/to another/file.mp3" });

			var result = U.GetTracks (tracks, "/path/to/");
			Assert.AreEqual (2, result.Count);
			Assert.AreEqual ("/path/to/file.mp3", result[0].Path);

			result = U.GetTracks (tracks, "/path/");
			Assert.AreEqual (3, result.Count);

			result = U.GetTracks (tracks, "/another/path/");
			Assert.AreEqual (0, result.Count);

			// TODO: check GetTracks(string,bool)
			// must use stubs?
		}

		[Test()]
		public void TestGetLock()
		{
			var lock1 = U.GetLock ("lock1");
			var lock2 = U.GetLock ("lock2");
			var lock3 = U.GetLock ("lock1");

			Assert.AreNotSame (lock1, lock2);
			Assert.AreSame (lock1, lock3);
		}

		[Test()]
		public void TestEqual()
		{
			Assert.AreEqual (true, U.Equal("foo","foo"));
			Assert.AreEqual (false, U.Equal("foo","foo "));
			Assert.AreEqual (false, U.Equal("foo",""));
			Assert.AreEqual (true, U.Equal("",""));
			Assert.AreEqual (true, U.Equal(null,""));
			Assert.AreEqual (true, U.Equal(""," "));
			Assert.AreEqual (true, U.Equal(null," "));
		}

		[Test ()]
		public void TestTrackMatchesQuery ()
		{
			var track = new Track ();
			track.Artist = "Eminem";
			track.Album = "Relapse";
			track.Title = "Insane";
			track.Length = 181.0;
			track.Genre = "Horrorcore";
			track.Year = 2009;
			track.TrackNumber = 4;

			Assert.True (U.TrackMatchesQuery (track, "eminem"));
			Assert.True (U.TrackMatchesQuery (track, "rela"));
			Assert.True (U.TrackMatchesQuery (track, "ORRO"));
			Assert.AreEqual (true, U.TrackMatchesQuery (track, "2009"));
			Assert.AreEqual (true, U.TrackMatchesQuery (track, " Insane"));
			Assert.AreEqual (false, U.TrackMatchesQuery (track, "eminems"));
			Assert.AreEqual (false, U.TrackMatchesQuery (track, "foo Insane"));
			Assert.AreEqual (false, U.TrackMatchesQuery (track, "2010"));
		}

		[Test ()]
		public void TestHumanSize ()
		{
			Assert.AreEqual ("0 bytes", U.HumanSize (0));
			Assert.AreEqual ("1 byte", U.HumanSize (1));
			Assert.AreEqual ("2 bytes", U.HumanSize (2));
			Assert.AreEqual ("2 KB", U.HumanSize (2048));
			Assert.AreEqual ("2.05 KB", U.HumanSize (2048, false));
			Assert.AreEqual ("2 MB", U.HumanSize (2097152));
			Assert.AreEqual ("2.1 MB", U.HumanSize (2097152, false));
			Assert.AreEqual ("2 GB", U.HumanSize (2147483648));
			Assert.AreEqual ("2.15 GB", U.HumanSize (2147483648, false));
			Assert.AreEqual ("2 TB", U.HumanSize (2199023255552));
			Assert.AreEqual ("2.2 TB", U.HumanSize (2199023255552, false));
		}

		[Test ()]
		public void TestCleanXMLString ()
		{
			Assert.AreEqual (null, U.CleanXMLString(null));
			Assert.AreEqual ("", U.CleanXMLString(""));
			Assert.AreEqual ("foobar", U.CleanXMLString ("foobar"));
			foreach (var n in new int[] { 7, 11, 31, 57343 })
			{
				var str = "foo{0}bar";
				var str1 = String.Format (str, "");
				var str2 = String.Format (str, (char)n);
				var str3 = String.Format (str, (char)(n+2));
				Assert.AreEqual (str1, U.CleanXMLString (str2));
				Assert.AreEqual (str3, U.CleanXMLString (str3));
			}
			// TODO: 65536 - 1114111
		}

		[Test ()]
		public void TestUnescapeHTML ()
		{
			Assert.AreEqual (null, U.UnescapeHTML(null));
			Assert.AreEqual ("", U.UnescapeHTML(""));
			Assert.AreEqual ("foobar", U.UnescapeHTML("foobar"));
			Assert.AreEqual ("foo>bar", U.UnescapeHTML("foo&gt;bar"));
			Assert.AreEqual ("foo\u00A0bar", U.UnescapeHTML("foo&nbsp;bar"));
			Assert.AreEqual ("foo&NBSP;bar", U.UnescapeHTML("foo&NBSP;bar"));
			Assert.AreEqual ("foo>bar", U.UnescapeHTML("foo&#62;bar"));
		}

		[Test ()]
		public void TestEscapeJSON ()
		{
			Assert.AreEqual ("", U.EscapeJSON (null));
			Assert.AreEqual ("", U.EscapeJSON(""));
			Assert.AreEqual ("foobar", U.EscapeJSON("foobar"));
			Assert.AreEqual ("foo&gt;bar", U.EscapeJSON("foo>bar"));
			Assert.AreEqual ("foo bar", U.EscapeJSON("foo bar"));
			Assert.AreEqual ("foo&quot;bar", U.EscapeJSON("foo\"bar"));
			Assert.AreEqual ("foo&#39;bar", U.EscapeJSON("foo\'bar"));
			Assert.AreEqual ("3.1416", U.EscapeJSON(3.14159));
			Assert.AreEqual ("31337", U.EscapeJSON(31337));
			Assert.AreEqual ("31337", U.EscapeJSON((uint)31337));
			Assert.AreEqual ("31337", U.EscapeJSON((long)31337));
			Assert.AreEqual ("31337", U.EscapeJSON((ulong)31337));
		}

		[Test ()]
		public void TestGetParams ()
		{
			var result = U.GetParams ("one=foo&two=bar&something=foo%3Dbar%26animals%3Dcat%2Cdog%2Cfish");
			Assert.AreEqual (3, result.Count);
			Assert.Contains ("one", result.Keys);
			Assert.AreEqual ("foo", result["one"]);
			Assert.AreEqual ("foo%3Dbar%26animals%3Dcat%2Cdog%2Cfish", result["something"]);
		}

		[Test ()]
		public void TestCreateQuery ()
		{
			Assert.AreEqual ("foo=bar", U.CreateQuery ("foo", new JValue ("bar")));
			Assert.AreEqual ("foo=True", U.CreateQuery ("foo", new JValue (true)));
			Assert.AreEqual ("foo=1337", U.CreateQuery ("foo", new JValue (1337)));
			Assert.AreEqual ("foo=3.141", U.CreateQuery ("foo", new JValue (3.141)));
			Assert.AreEqual ("foo=01%2F03%2F2014%2018%3A24%3A00", U.CreateQuery ("foo", new JValue (DateTime.Parse("2014-01-03 18:24"))));

			var obj = new JObject ();
			obj ["one"] = new JValue (1);
			obj ["three"] = new JValue ("san");
			Assert.AreEqual ("foo[one]=1&foo[three]=san", U.CreateQuery ("foo", obj));

			var arr = new JArray ();
			arr.Add (obj);
			obj = new JObject ();
			obj ["cat"] = new JValue ("blue");
			arr.Add (obj);
			Assert.AreEqual ("foo[0][one]=1&foo[0][three]=san&foo[1][cat]=blue", U.CreateQuery ("foo", arr));
		}

		[Test ()]
		public void TestGetQuery ()
		{
			Assert.AreEqual ("foo=one&bar=two", U.GetQuery ("https://example.com/something?foo=one&bar=two"));
		}

		[Test ()]
		public void TestCapitalize ()
		{
			Assert.AreEqual ("", U.Capitalize (null));
			Assert.AreEqual ("", U.Capitalize (""));
			Assert.AreEqual ("Foobar", U.Capitalize ("foobar"));
			Assert.AreEqual ("Foo bar", U.Capitalize ("foo bar"));
		}

		[Test ()]
		public void TestTitleize ()
		{
			Assert.AreEqual ("", U.Titleize (null));
			Assert.AreEqual ("", U.Titleize (""));
			Assert.AreEqual ("Foobar", U.Titleize ("foobar"));
			Assert.AreEqual ("Foo Bar", U.Titleize ("foo bar"));
			Assert.AreEqual ("Foo of the Bar", U.Titleize ("foo of the bar"));
			Assert.AreEqual ("Foo Of The Bar", U.Titleize ("foo of the bar", false, true));
			Assert.AreEqual ("Foo of the bar", U.Titleize ("foo of the bar", true, true));
		}

		[Test ()]
		public void TestTimeSpanToLongString ()
		{
			Assert.AreEqual ("0 seconds", U.TimeSpanToLongString (new TimeSpan (0)));
			Assert.AreEqual ("1 second", U.TimeSpanToLongString (new TimeSpan (0, 0, 1)));
			Assert.AreEqual ("3 seconds", U.TimeSpanToLongString (new TimeSpan (0, 0, 3)));
			Assert.AreEqual ("3 hours, 1 minute, 4 seconds",
				U.TimeSpanToLongString (new TimeSpan (3, 1, 4)));
			Assert.AreEqual ("3 days, 1 hour, 4 minutes, 1 second",
				U.TimeSpanToLongString (new TimeSpan (3, 1, 4, 1)));
		}

		[Test ()]
		public void TestTimeSpanToString ()
		{
			Assert.AreEqual ("00:00", U.TimeSpanToString (new TimeSpan (0)));
			Assert.AreEqual ("00:01", U.TimeSpanToString (new TimeSpan (0, 0, 1)));
			Assert.AreEqual ("00:03", U.TimeSpanToString (new TimeSpan (0, 0, 3)));
			Assert.AreEqual ("03:01:04", U.TimeSpanToString (new TimeSpan (3, 1, 4)));
			Assert.AreEqual ("3:01:04:01", U.TimeSpanToString (new TimeSpan (3, 1, 4, 1)));
		}

		[Test ()]
		public void TestUnix2Date ()
		{
			Assert.AreEqual ("Wed, Jan 1, 2014 17:40", U.Unix2Date (1388594447));
		}

		[Test ()]
		public void TestVersion2String ()
		{
			Assert.AreEqual ("1.38.859.4447", U.Version2String (1388594447));
		}

		[Test ()]
		public void TestT ()
		{
			Assert.AreEqual ("Text", U.T (""));
			Assert.AreEqual ("Text", U.T ("Foobar"));
			Assert.AreEqual ("Bar", U.T ("Foo", "Bar"));
			Assert.AreEqual ("Foo bar", U.T ("Foobar", "Text", "Foo bar"));
			Assert.AreEqual ("31,337", U.T (31337));
			Assert.AreEqual ("3.14", U.T (3.14159));
			Assert.AreEqual ("File", U.T (Stoffi.Core.Settings.SourceType.File));
			Assert.AreEqual ("Visualizer", U.T (Stoffi.Plugins.PluginType.Visualizer));
			Assert.AreEqual ("Never", U.T (new DateTime(10)));
			Assert.AreEqual ("03/01/2014 17:40:36", U.T (new DateTime(2014,3,1,17,40,36)));
		}

		[Test ()]
		public void TestParseTitle ()
		{
			var result = U.ParseTitle ("foobar - a great song");
			Assert.AreEqual ("foobar", result [0]);
			Assert.AreEqual ("a great song", result [1]);

			result = U.ParseTitle ("foobar -a great song [official]");
			Assert.AreEqual ("foobar", result [0]);
			Assert.AreEqual ("a great song", result [1]);

			result = U.ParseTitle ("foobar: a great song (LYRICS)");
			Assert.AreEqual ("foobar", result [0]);
			Assert.AreEqual ("a great song", result [1]);

			result = U.ParseTitle ("foobar- a great song official video");
			Assert.AreEqual ("foobar", result [0]);
			Assert.AreEqual ("a great song", result [1]);

			result = U.ParseTitle ("a great song by foobar");
			Assert.AreEqual ("foobar", result [0]);
			Assert.AreEqual ("a great song", result [1]);

			result = U.ParseTitle ("a great song - foo feat. bar");
			Assert.AreEqual ("foo feat. bar", result [0]);
			Assert.AreEqual ("a great song", result [1]);

			result = U.ParseTitle ("a great song, by foo and bar");
			Assert.AreEqual ("foo and bar", result [0]);
			Assert.AreEqual ("a great song", result [1]);

			result = U.ParseTitle ("foobar \"a great song\"");
			Assert.AreEqual ("foobar", result [0]);
			Assert.AreEqual ("a great song", result [1]);
		}

		[Test ()]
		public void TestPrettifyTag ()
		{
			Assert.AreEqual ("", U.PrettifyTag (""));
			Assert.AreEqual ("", U.PrettifyTag (null));
			Assert.AreEqual ("Play Count", U.PrettifyTag ("play_count"));
		}
	}
}

