/**
 * Sources.cs
 * 
 * Manages all sources from which music can be played.
 * This includes both local files and streams from Internet.
 * 
 * Right now the sources are hardcoded into the manager which
 * exposes the various source objects.
 * 
 * In the future we will move to a plugin based source system
 * where a special type of plugin will be able to provide
 * a source with methods for searching and playing the tracks
 * as well as fetching meta data.
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

namespace Stoffi.Core.Sources
{
	/// <summary>
	/// Manages music sources.
	/// </summary>
	public static class Manager
	{
		#region Members

		private static YouTube youTube = new YouTube();
		private static SoundCloud soundCloud = new SoundCloud();
		private static Jamendo jamendo = new Jamendo();

		#endregion

		#region Properties

		/// <summary>
		/// Get the YouTube source.
		/// </summary>
		/// <value>YouTube source.</value>
		public static YouTube YouTube { get { return youTube; } }

		/// <summary>
		/// Get the SoundCloud source.
		/// </summary>
		/// <value>SoundCloud source.</value>
		public static SoundCloud SoundCloud { get { return soundCloud; } }

		/// <summary>
		/// Get the Jamendo source.
		/// </summary>
		/// <value>Jamendo source.</value>
		public static Jamendo Jamendo { get { return jamendo; } }

		#endregion
	}
}

