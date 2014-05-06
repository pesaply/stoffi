/**
 * Keyboard.cs
 * 
 * Various helper classes and method for dealing with image related stuff.
 * 
 * * * * * * * * *
 * 
 * Copyright 2012 Simplare
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
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Stoffi
{
	/// <summary>
	/// A utilities class with helper method for GUI related stuff
	/// </summary>
	static partial class Utilities
	{
		/// <summary>
		/// Extracts a specific image from an ico
		/// file given it's size.
		/// If no exact size is matched, the largest 
		/// image will be returned.
		/// </summary>
		/// <param name="path">The path to the ico</param>
		/// <param name="width">The prefered width</param>
		/// <param name="height">The prefered height</param>
		/// <returns>An image from inside the ico file</returns>
		public static BitmapFrame GetIcoImage(string path, int width, int height)
		{
			if (!path.StartsWith("pack://") && !path.Contains('\\') && !path.Contains('/'))
				path = "pack://application:,,,/Platform/Windows 7/GUI/Images/Icons/" + path + ".ico";
			var iconUri = new Uri(path, UriKind.RelativeOrAbsolute);
			try
			{

				var iconDecoder = new IconBitmapDecoder(iconUri,
					BitmapCreateOptions.None, BitmapCacheOption.Default);

				// no image found
				if (iconDecoder.Frames.Count == 0) return null;

				BitmapFrame largest = iconDecoder.Frames[0];
				foreach (BitmapFrame frame in iconDecoder.Frames)
				{
					if (frame.PixelHeight == height &&
						frame.PixelWidth == width)
					{
						return frame;
					}

					if (frame.PixelWidth * frame.PixelHeight >
						largest.PixelWidth * frame.PixelHeight)
						largest = frame;
				}

				return largest;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Extracts a each image from an ico field.
		/// </summary>
		/// <param name="path">The path to the ico</param>
		/// <returns>Each image from inside the ico file</returns>
		public static List<ImageSource> GetIcoImages(string path)
		{
			List<ImageSource> ret = new List<ImageSource>();
			if (!path.StartsWith("pack://") && !path.Contains('\\') && !path.Contains('/'))
				path = "pack://application:,,,/Platform/Windows 7/GUI/Images/Icons/" + path + ".ico";
			var iconUri = new Uri(path, UriKind.RelativeOrAbsolute);
			try
			{
				var iconDecoder = new IconBitmapDecoder(iconUri,
					BitmapCreateOptions.None, BitmapCacheOption.Default);

				// no image found
				if (iconDecoder.Frames.Count == 0) return ret;

				BitmapFrame largest = iconDecoder.Frames[0];
				foreach (BitmapFrame frame in iconDecoder.Frames)
					ret.Add(frame);

				return ret;
			}
			catch (Exception)
			{
				return ret;
			}
		}

		/// <summary>
		/// Gets the album art for a given track
		/// </summary>
		/// <param name="track">The track to find the album art for</param>
		/// <returns>The album art if it could be found, otherwise a default image</returns>
		public static ImageSource GetImageTag(TrackData track)
		{
			BitmapImage def = new BitmapImage(new Uri(DefaultAlbumArt, UriKind.RelativeOrAbsolute));
			switch (MediaManager.GetType(track))
			{
				case TrackType.YouTube:
					return new BitmapImage(new Uri(YouTubeManager.GetThumbnail(track)));

				case TrackType.SoundCloud:
					if (track.ArtURL != null)
						return new BitmapImage(new Uri(track.ArtURL));
					break;

				case TrackType.File:
					try
					{
						TagLib.File file = TagLib.File.Create(track.Path, TagLib.ReadStyle.None);

						if (file.Tag.Pictures.Count() > 0)
						{
							try
							{
								TagLib.IPicture pic = file.Tag.Pictures[0];
								if (pic.Data.Data.Count<byte>() > 0)
								{
									MemoryStream stream = new MemoryStream(pic.Data.Data);
									BitmapFrame bmp = BitmapFrame.Create(stream);
									return bmp;
								}
							}
							catch { }
						}
					}
					catch { }
					break;
			}
			return def; // return default image
		}
	}
}
