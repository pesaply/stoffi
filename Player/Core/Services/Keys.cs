/**
 * Keys.cs
 * 
 * This file lists all the keys and secrets used for access to
 * various API services.
 * 
 * You need to copy the file Keys.cs.tmpl to Keys.cs and fill it
 * with your own keys and secrets obtained from the services'
 * websites.
 * 
 * For a complete list of the links to all websites visit:
 * http://dev.stoffiplayer.com/wiki/Hacking
 * 
 * IMPORTANT:
 * Make sure you never upload your secret keys to any public
 * repository (include the offical Stoffi repo). If you do
 * you should consider your keys to be compromised and anyone
 * can impersonate your application and developer account at
 * the API service.
 * 
 * * * * * * * * *
 * 
 * Copyright 2013 Simplare
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
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stoffi.Core
{
	/// <summary>
	/// All API keys and secrets.
	/// </summary>
	public static partial class U
	{
		/// <summary>
		/// Checks if you have added the keys, secrets, etc.
		/// </summary>
		public static void Check()
		{
			string[] strings = new string[] { StoffiKey, StoffiSecret, JamendoKey, JamendoSecret, LastFMKey, YouTubeKey, SoundCloudID, BassMail, BassKey };
			if (strings.Contains(""))
				throw new Exception("You need to add API keys. See http://dev.stoffiplayer.com/wiki/Hacking for more info.");
		}

		/// <summary>
		/// The key for the stoffiplayer.com service
		/// </summary>
		public static string StoffiKey = "baAito0V8WXtdpfjrE4GfUhld4IvFMd9Ud5EYw8i";

		/// <summary>
		/// The secret for the stoffiplayer.com service
		/// </summary>
		public static string StoffiSecret = "cU1esKuX0VruYYVhU5Mrry4SukK5yL9uHcYoHip1";

		/// <summary>
		/// The key for the jamendo.com service
		/// </summary>
		public static string JamendoKey = "b8c37e13";

		/// <summary>
		/// The secret for the jamendo.com service
		/// </summary>
		public static string JamendoSecret = "deca42d7912094b0f18c5d0a4f6ccb7a";

		/// <summary>
		/// The key for the Last.fm service
		/// </summary>
		public static string LastFMKey = "0353aad0b5cf5fbdaa0bbfb94618e57c";

		/// <summary>
		/// The key for the YouTube service.
		/// </summary>
		public static string YouTubeKey = "AIzaSyDFMnuvS992IwFevrWji_o7Wrw2kGAGehg";

		/// <summary>
		/// The client ID for the SoundCloud service.
		/// </summary>
		public static string SoundCloudID = "2ad7603ebaa9cd252eabd8dd293e9c40";

		/// <summary>
		/// The email for the Bass library.
		/// </summary>
		public static string BassMail = "christoffer.brodd.reijer@gmail.com";

		/// <summary>
		/// The key for the Bass library.
		/// </summary>
		public static string BassKey = "2X2313734152222";
	}
}
