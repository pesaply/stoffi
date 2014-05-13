using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

using Stoffi.Core;
using Stoffi.Core.Settings;

using SettingsManager = Stoffi.Core.Settings.Manager;

namespace Stoffi.GUI.Views
{
	public partial class EqualizerWindowController : MonoMac.AppKit.NSWindowController
	{
		#region Members

		private List<NSSlider> levelSliders = new List<NSSlider>();

		#endregion

		#region Constructors
		// Called when created from unmanaged code
		public EqualizerWindowController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public EqualizerWindowController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		// Call to load from the XIB/NIB file
		public EqualizerWindowController () : base ("EqualizerWindow")
		{
			Initialize ();
		}
		// Shared initialization code
		void Initialize ()
		{
		}
		#endregion

		#region Properties
		//strongly typed window accessor
		public new EqualizerWindow Window {
			get {
				return (EqualizerWindow)base.Window;
			}
		}

		public bool InEditMode
		{
			get { return Profiles.Hidden; }
			set {
				EditButton.Hidden = value;
				Name.Hidden = !value;
				Profiles.Hidden = value;
			}
		}
		#endregion

		#region Methods

		#region Private

		private void InsertProfiles(List<EqualizerProfile> profiles, int start = 0)
		{
			for (int i=0; i < profiles.Count; i++) {
				var profile = profiles [i];

				// remove profiles which are not in protectedProfiles
				if (Profiles.Items ().Count () >= start + i + 1) {
					while (Profiles.ItemAtIndex(start+i).Title != profile.Name)
					{
						bool exists = false;
						foreach (var p in profiles)
						{
							if (p.Name == Profiles.ItemAtIndex (start+i).Title)
							{
								exists = true;
								break;
							}
						}
						if (exists)
							break;

						Profiles.RemoveItem (start+i);

						if (Profiles.Items ().Count () < start + i + 1)
							break;
					}
				}

				// add the profile if it is not already added
				if (Profiles.Items().Count() < start + i + 1 || Profiles.ItemAtIndex (start+i).Title != profile.Name)
					Profiles.InsertItem (profile.Name, start+i);
			}
		}

		private void RefreshProfiles()
		{
			var protectedProfiles = new List<EqualizerProfile> ();
			var manualProfiles = new List<EqualizerProfile> ();

			foreach (var profile in SettingsManager.EqualizerProfiles) {
				profile.PropertyChanged -= Equalizer_PropertyChanged;
				profile.PropertyChanged += Equalizer_PropertyChanged;
				if (profile.IsProtected)
					protectedProfiles.Add (profile);
				else
					manualProfiles.Add (profile);
			}

			InsertProfiles (protectedProfiles);

			if (manualProfiles.Count > 0) {
				if (Profiles.Menu.Count <= protectedProfiles.Count || !Profiles.Menu.ItemAt (protectedProfiles.Count).IsSeparatorItem)
					Profiles.Menu.AddItem (NSMenuItem.SeparatorItem);
				InsertProfiles (manualProfiles, protectedProfiles.Count + 1);
			} else {
				if (Profiles.Menu.Count > protectedProfiles.Count && Profiles.Menu.ItemAt (protectedProfiles.Count).IsSeparatorItem)
					Profiles.RemoveItem (protectedProfiles.Count);
			}

			RefreshProfileSelection();
		}

		private void RefreshProfileSelection()
		{
			var current = SettingsManager.CurrentEqualizerProfile;
			NSMenuItem currentItem = null;

			foreach (var profile in Profiles.Items())
			{
				bool isCurrent = current != null && current.Name == profile.Title;
				profile.State = isCurrent ? NSCellStateValue.On : NSCellStateValue.Off;
				if (isCurrent)
					currentItem = profile;
			}

			if (currentItem != null)
			{
				Profiles.SelectItem (currentItem);
			}

			DeleteButton.Hidden = current.IsProtected;
			EditButton.Hidden = current.IsProtected;
		}

		private void RefreshSliders()
		{
			var eq = SettingsManager.CurrentEqualizerProfile;
			SliderEcho.DoubleValue = eq.EchoLevel;
			Slider125.DoubleValue = eq.Levels [0];
			Slider250.DoubleValue = eq.Levels [1];
			Slider500.DoubleValue = eq.Levels [2];
			Slider1K.DoubleValue = eq.Levels [3];
			Slider2K.DoubleValue = eq.Levels [4];
			Slider4K.DoubleValue = eq.Levels [5];
			Slider8K.DoubleValue = eq.Levels [6];
			Slider16K.DoubleValue = eq.Levels [7];
		}

		private void Refresh()
		{
			RefreshProfiles ();
			RefreshSliders ();
		}

		#endregion

		#region Event handlers

		partial void SliderChange(NSObject sender)
		{
			if (SettingsManager.CurrentEqualizerProfile.IsProtected)
			{
				try{
					// find a name which is not already taken: Custom, Custom 1, Custom 2, etc...
					var name = SettingsManager.GenerateEqualizerName("Custom");

					// create new profile and select it
					var profile = new EqualizerProfile();
					profile.EchoLevel = SliderEcho.FloatValue;
					profile.IsProtected = false;
					profile.Name = name;
					profile.Levels = new ObservableCollection<float>()
					{
						Slider125.FloatValue,
						Slider250.FloatValue,
						Slider500.FloatValue,
						Slider1K.FloatValue,
						Slider2K.FloatValue,
						Slider4K.FloatValue,
						Slider8K.FloatValue,
						Slider16K.FloatValue
					};
					SettingsManager.EqualizerProfiles.Add (profile);
					SettingsManager.CurrentEqualizerProfile = profile;
				}
				catch (Exception e)
				{
					U.L (LogLevel.Error, "Equalizer", "Could not create new profile: " + e.Message);
				}
			}
			else
			{
				try{
					var slider = sender as NSSlider;
					if (slider == SliderEcho)
						SettingsManager.CurrentEqualizerProfile.EchoLevel = slider.FloatValue;
					else
						SettingsManager.CurrentEqualizerProfile.Levels[levelSliders.IndexOf(slider)] = slider.FloatValue;
				}
				catch (Exception e)
				{
					U.L (LogLevel.Error, "Equalizer", "Could not handle slider update: " + e.Message);
				}
			}
		}

		partial void NameChange(NSObject sender)
		{
			Name.Hidden = true;
			Profiles.Hidden = false;
			if (!SettingsManager.CurrentEqualizerProfile.IsProtected)
			{
				SettingsManager.CurrentEqualizerProfile.Name = Name.StringValue;
			}
			Name.StringValue = "";
		}

		partial void ProfileAdd(NSObject sender)
		{
			var name = SettingsManager.GenerateEqualizerName("Custom");
			var profile = new EqualizerProfile();
			profile.EchoLevel = 0;
			profile.IsProtected = false;
			profile.Name = name;
			profile.Levels = new ObservableCollection<float>() { 0, 0, 0, 0, 0, 0, 0, 0 };
			SettingsManager.EqualizerProfiles.Add (profile);
			SettingsManager.CurrentEqualizerProfile = profile;
			Name.StringValue = name;
			Name.Hidden = false;
			Profiles.Hidden = true;
		}

		partial void ProfileChange(NSObject sender)
		{
			var name = Profiles.SelectedItem.Title;
			foreach (var profile in SettingsManager.EqualizerProfiles)
			{
				if (profile.Name == name)
				{
					SettingsManager.CurrentEqualizerProfile = profile;
					break;
				}
			}
		}

		partial void ProfileDel(NSObject sender)
		{
			if (SettingsManager.CurrentEqualizerProfile.IsProtected)
				return;

			// get index to remove in menu
			var index = -1;
			for (int i=0; i < Profiles.ItemCount; i++)
			{
				if (Profiles.ItemAtIndex(i).Title == SettingsManager.CurrentEqualizerProfile.Name)
				{
					index = i;
					break;
				}
			}

			// get profile to remove in collection
			var profile = SettingsManager.CurrentEqualizerProfile;

			// select default profile
			SettingsManager.CurrentEqualizerProfile = SettingsManager.EqualizerProfiles[0];

			// remove from menu
			Profiles.RemoveItem(index);

			// remove from collection
			SettingsManager.EqualizerProfiles.Remove (profile);
		}

		partial void ProfileEdit(NSObject sender)
		{
			Name.StringValue = SettingsManager.CurrentEqualizerProfile.Name;
		}

		private void Settings_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			switch (e.PropertyName)
			{
			case "CurrentEqualizerProfile":
				RefreshProfileSelection ();
				RefreshSliders ();
				break;
			}
		}

		private void EqualizerProfiles_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			RefreshProfiles ();
		}

		private void Equalizer_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			RefreshProfiles ();
		}

		#endregion

		#region Overrides

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
			Refresh ();
			SettingsManager.PropertyChanged += Settings_PropertyChanged;
			SettingsManager.EqualizerProfiles.CollectionChanged += EqualizerProfiles_CollectionChanged;

			// add in the correct order for levels in model
			levelSliders.Add (Slider125);
			levelSliders.Add (Slider250);
			levelSliders.Add (Slider500);
			levelSliders.Add (Slider1K);
			levelSliders.Add (Slider2K);
			levelSliders.Add (Slider4K);
			levelSliders.Add (Slider8K);
			levelSliders.Add (Slider16K);
		}

		#endregion

		#endregion
	}
}

