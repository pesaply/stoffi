/***
 * PLSTest.cs
 * 
 * The testing suite for the PLS parser class of Core.
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
using System.IO;

using NUnit.Framework;

using Stoffi.Core.Media;
using Stoffi.Core.Playlists;
using Stoffi.Core.Playlists.Parsers;

namespace Playlists.Parsers
{
	[TestFixture ()]
	public class PLSTest
	{
		[Test ()]
		public void TestReadStream ()
		{
			var stream = new MemoryStream ();
			var writer = new StreamWriter (stream);
			writer.WriteLine ("[playlist]");
			writer.WriteLine ("NumberOfEntries=2");
			writer.WriteLine ("Version=1");
			writer.WriteLine ("");
			writer.WriteLine ("File1=http://example.cm/radio/station");
			writer.WriteLine ("length1=185");
			writer.WriteLine ("Title1=First File");
			writer.WriteLine ("");
			writer.WriteLine ("Title2=Radio Station");
			writer.WriteLine ("FILE2=https://example.cm/second/station");
			writer.Flush ();
			stream.Seek (0, SeekOrigin.Begin);

			var pls = new PLS ();

			var result = pls.ReadStream (new StreamReader (stream), "", false);
			Assert.AreEqual (1, result.Count);
			Assert.AreEqual (2, result[0].Tracks.Count);
			Assert.AreEqual ("http://example.cm/radio/station", result[0].Tracks[0].Path);
		}

		[Test ()]
		public void TestWriteStream ()
		{
		}

		[Test ()]
		public void TestSupports ()
		{
		}
	}
}