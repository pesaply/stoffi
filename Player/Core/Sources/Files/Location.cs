/***
 * ScanSource.cs
 * 
 * Describes a location where the scanner should look for audio files.
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
 ***/

using System;

using Stoffi.Core.Settings;

namespace Stoffi.Core.Sources
{
	/// <summary>
	/// Describes a source.
	/// </summary>
	public class Location : ListItem
	{
		#region Members

		private SourceType type;
		private string data;
		private bool ignore;
		private bool automatic = false;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets whether the source was added automatically or not.
		/// </summary>
		public bool Automatic
		{
			get { return automatic; }
			set { SetProp<bool> (ref automatic, value, "Automatic"); }
		}

		/// <summary>
		/// Gets or sets whether the source should be striked through.
		/// </summary>
		public new bool Strike
		{
			get { return ignore; }
			set
			{
				if (SetProp<bool> (ref ignore, value, "Strike"))
				{
					OnPropertyChanged ("Ignore");
					OnPropertyChanged ("Include");
				}
			}
		}

		/// <summary>
		/// Gets or sets the type of the source.
		/// </summary>
		public SourceType Type
		{
			get { return type; }
			set { SetProp<SourceType> (ref type, value, "Type"); }
		}

		/// <summary>
		/// Gets or sets whether the files inside the source should be ignored.
		/// </summary>
		public bool Ignore
		{
			get { return ignore; }
			set
			{
				if (SetProp<bool> (ref ignore, value, "Ignore"))
				{
					OnPropertyChanged ("Strike");
					OnPropertyChanged ("Include");
				}
			}
		}

		/// <summary>
		/// Gets or sets whether the files inside the source should be included.
		/// </summary>
		public bool Include
		{
			get { return !Ignore; }
			set
			{
				if (SetProp<bool> (ref ignore, !value, "Include"))
				{
					OnPropertyChanged ("Ignore");
					OnPropertyChanged ("Strike");
				}
			}
		}

		/// <summary>
		/// Gets or sets the name (if type is "Library") or path (if type is "File" or "Folder")
		/// of the source.
		/// </summary>
		public string Data
		{ 
			get { return data; }
			set { SetProp<string> (ref data, value, "Data"); }
		} 

		#endregion
	}
}

