/***
 * EqualizerProfile.cs
 * 
 * Describes a profile of equalizer settings.
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Stoffi.Core.Settings
{
	/// <summary>
	/// Describes an equalizer profile.
	/// </summary>
	public class EqualizerProfile : PropertyChangedBase
	{
		#region Members

		private string name = "";
		private bool isProtected = false;
		private ObservableCollection<float> levels = new ObservableCollection<float> {0,0,0,0,0,0,0,0,0,0};
		private float echoLevel = 0;

		#endregion

		#region Properties

		/// <summary>
		/// Get or sets the name of the profile.
		/// </summary>
		public String Name
		{
			get { return name; }
			set { SetProp<string> (ref name, value, "Name"); }
		}

		/// <summary>
		/// Get or sets whether the user can modify the profile.
		/// </summary>
		public bool IsProtected
		{
			get { return isProtected; }
			set { SetProp<bool> (ref isProtected, value, "IsProtected"); }
		}

		/// <summary>
		/// Get or sets the levels (between -10 and 10).
		/// </summary>
		/// <remarks>
		/// Is a list with 10 floats between -10 and 10,
		/// where each float represents the maximum level
		/// on a frequency band going from lower to higher.
		/// </remarks>
		public ObservableCollection<float> Levels
		{
			get { return levels; }
			set
			{
				if (levels != null)
					levels.CollectionChanged -= CollectionChanged;
				SetProp<ObservableCollection<float>> (ref levels, value, "Levels");
				if (levels != null)
					levels.CollectionChanged += CollectionChanged;
			}
		}

		/// <summary>
		/// Get or sets the echo level.
		/// A float ranging from 0 to 10 going from
		/// dry to wet.
		/// </summary>
		public float EchoLevel
		{
			get { return echoLevel; }
			set { SetProp<float> (ref echoLevel, value, "EchoLevel"); }
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.Core.EqualizerProfile"/> class.
		/// </summary>
		public EqualizerProfile()
		{
			levels.CollectionChanged += CollectionChanged;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Invoked when a collection changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if ((ObservableCollection<float>)sender == levels && levels != null)
				OnPropertyChanged ("Levels");
		}
		/// <summary>
		/// Creates a profile with some given values.
		/// </summary>
		/// <param name="name">The name of the profile</param>
		/// <param name="levels">The 10 levels (ranging from 0 to 10)</param>
		/// <param name="echo">The echo level (ranging from 0 to 10)</param>
		/// <param name="isProtected">Whether or not the user can edit the profile</param>
		public static EqualizerProfile Create(string name, ObservableCollection<float> levels, float echo, bool isProtected = false)
		{
			return new EqualizerProfile() { EchoLevel = echo, IsProtected = isProtected, Levels = levels, Name = name };
		}


		#endregion
	}
}

