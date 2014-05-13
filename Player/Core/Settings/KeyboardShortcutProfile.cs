/***
 * KeyboardShortcutProfile.cs
 * 
 * Describes a profile of keyboard shortcuts.
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
using System.ComponentModel;

namespace Stoffi.Core.Settings
{
	/// <summary>
	/// Describes a profile of keyboard shortcuts.
	/// </summary>
	public class KeyboardShortcutProfile : PropertyChangedBase
	{
		#region Members

		private string name = "";
		private bool isProtected = false;
		private ObservableCollection<KeyboardShortcut> shortcuts = new ObservableCollection<KeyboardShortcut>();

		#endregion

		#region Properties

		/// <summary>
		/// Get or sets the name of the profile.
		/// </summary>
		public string Name
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
		/// Get or sets the shortcuts of the profile.
		/// </summary>
		public ObservableCollection<KeyboardShortcut> Shortcuts
		{
			get { return shortcuts; }
			set
			{
				if (shortcuts != null)
					shortcuts.CollectionChanged -= CollectionChanged;
				SetProp<ObservableCollection<KeyboardShortcut>> (ref shortcuts, value, "Shortcuts");
				if (shortcuts != null) {
					foreach (var shortcut in shortcuts) {
						shortcut.PropertyChanged -= Shortcut_PropertyChanged;
						shortcut.PropertyChanged += Shortcut_PropertyChanged;
					}
					shortcuts.CollectionChanged += CollectionChanged;
				}
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.Core.KeyboardShortcutProfile"/> class.
		/// </summary>
		public KeyboardShortcutProfile()
		{
			shortcuts.CollectionChanged += CollectionChanged;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Invoked when a property of a shortcut changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void Shortcut_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			OnPropertyChanged ("Shortcuts");
		}

		/// <summary>
		/// Invoked when a collection changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if ((ObservableCollection<KeyboardShortcut>)sender == shortcuts && shortcuts != null)
			{
				if (e.OldItems != null)
					foreach (KeyboardShortcut s in e.OldItems)
						s.PropertyChanged -= Shortcut_PropertyChanged;
				if (e.NewItems != null)
					foreach (KeyboardShortcut s in e.NewItems)
						s.PropertyChanged += Shortcut_PropertyChanged;
			}
		}
		/// <summary>
		/// Initializes a keyboard shortcut profile.
		/// </summary>
		/// <param name="name">The name of the profile</param>
		/// <param name="isprotected">Whether or not the profile is protected from changes by user</param>
		public void Initialize(String name, Boolean isprotected)
		{
			Name = name;
			IsProtected = isprotected;

			// set the default shortcuts
			Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Add track", IsGlobal = false, Keys = "Alt+T" });
			Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Add folder", IsGlobal = false, Keys = "Alt+F" });
			Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Add playlist", IsGlobal = false, Keys = "Alt+P" });
			Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Add radio station", IsGlobal = false, Keys = "Alt+R" });
			Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Add app", IsGlobal = false, Keys = "Alt+A" });
			Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Generate playlist", IsGlobal = false, Keys = "Alt+G" });
			Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Minimize", IsGlobal = false, Keys = "Ctrl+Shift+M" });
			Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Restore", IsGlobal = true, Keys = "Ctrl+Shift+R" });
			Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Help", IsGlobal = false, Keys = "F1" });
			Shortcuts.Add(new KeyboardShortcut { Category = "Application", Name = "Close", IsGlobal = false, Keys = "Ctrl+W" });

			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Video", IsGlobal = false, Keys = "Ctrl+F1" }); // index 10
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Visualizer", IsGlobal = false, Keys = "Ctrl+F2" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Files", IsGlobal = false, Keys = "Ctrl+F3" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "YouTube", IsGlobal = false, Keys = "Ctrl+F4" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "SoundCloud", IsGlobal = false, Keys = "Ctrl+F5" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Radio", IsGlobal = false, Keys = "Ctrl+F6" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Queue", IsGlobal = false, Keys = "Ctrl+F7" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "History", IsGlobal = false, Keys = "Ctrl+F8" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Playlists", IsGlobal = false, Keys = "Ctrl+F9" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Tracklist", IsGlobal = false, Keys = "Ctrl+T" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Search", IsGlobal = false, Keys = "Ctrl+F" }); // index 20
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "General preferences", IsGlobal = false, Keys = "Alt+F1" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Music sources", IsGlobal = false, Keys = "Alt+F2" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Services", IsGlobal = false, Keys = "Alt+F3" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Apps", IsGlobal = false, Keys = "Alt+F5" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Keyboard shortcuts", IsGlobal = false, Keys = "Alt+F6" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "About", IsGlobal = false, Keys = "Alt+F7" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Toggle details pane", IsGlobal = false, Keys = "Alt+D" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Toggle menu bar", IsGlobal = false, Keys = "Alt+M" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MainWindow", Name = "Create playlist", IsGlobal = false, Keys = "Ctrl+N" });

			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Play or pause", IsGlobal = false, Keys = "Alt+5 (numpad)" }); // index 30
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Next", IsGlobal = false, Keys = "Alt+6 (numpad)" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Previous", IsGlobal = false, Keys = "Alt+4 (numpad)" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Toggle shuffle", IsGlobal = false, Keys = "Alt+9 (numpad)" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Toggle repeat", IsGlobal = false, Keys = "Alt+7 (numpad)" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Increase volume", IsGlobal = false, Keys = "Alt+8 (numpad)" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Decrease volume", IsGlobal = false, Keys = "Alt+2 (numpad)" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Seek forward", IsGlobal = false, Keys = "Alt+3 (numpad)" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Seek backward", IsGlobal = false, Keys = "Alt+1 (numpad)" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Add bookmark", IsGlobal = false, Keys = "Alt+B" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to previous bookmark", IsGlobal = false, Keys = "Alt+Left" }); // index 40
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to next bookmark", IsGlobal = false, Keys = "Alt+Right" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to first bookmark", IsGlobal = false, Keys = "Alt+Home" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to last bookmark", IsGlobal = false, Keys = "Alt+End" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 1", IsGlobal = false, Keys = "Alt+1" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 2", IsGlobal = false, Keys = "Alt+2" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 3", IsGlobal = false, Keys = "Alt+3" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 4", IsGlobal = false, Keys = "Alt+4" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 5", IsGlobal = false, Keys = "Alt+5" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 6", IsGlobal = false, Keys = "Alt+6" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 7", IsGlobal = false, Keys = "Alt+7" }); // index 50
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 8", IsGlobal = false, Keys = "Alt+8" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 9", IsGlobal = false, Keys = "Alt+9" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to bookmark 10", IsGlobal = false, Keys = "Alt+0" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to current track", IsGlobal = false, Keys = "Alt+C" });
			Shortcuts.Add(new KeyboardShortcut { Category = "MediaCommands", Name = "Jump to selected track", IsGlobal = false, Keys = "Alt+X" });

			Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Play track", IsGlobal = false, Keys = "Enter" });
			Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Queue and dequeue", IsGlobal = false, Keys = "Shift+Q" });
			Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Open folder", IsGlobal = false, Keys = "Ctrl+L" });
			Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Remove", IsGlobal = false, Keys = "Delete" });
			Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Remove from harddrive", IsGlobal = false, Keys = "Shift+Delete" }); // index 60
			Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Copy", IsGlobal = false, Keys = "Ctrl+C" });
			Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Move", IsGlobal = false, Keys = "Ctrl+X" });
			Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "View information", IsGlobal = false, Keys = "Ctrl+I" });
			Shortcuts.Add(new KeyboardShortcut { Category = "Track", Name = "Share", IsGlobal = false, Keys = "Shift+S" });
		}

		/// <summary>
		/// Set the keys (and optionally if the shortcut is global) of a shortcut given its name and category.
		/// </summary>
		/// <param name="category">Category of the shortcut to set.</param>
		/// <param name="name">Name of the shortcut to set.</param>
		/// <param name="keysAsText">Keys as text.</param>
		/// <param name="isGlobal">If set to <c>true</c>, shortcut is set to global.</param>
		public void SetShortcut(String category, String name, String keysAsText, bool isGlobal = false)
		{
			KeyboardShortcut sc = GetShortcut(category, name);
			if (sc == null)
			{
				sc = new KeyboardShortcut();
				sc.Category = category;
				sc.Name = name;
				Shortcuts.Add(sc);
			}
			sc.Keys = keysAsText;
			sc.IsGlobal = isGlobal;
		}

		/// <summary>
		/// Find a keyboard shortcut inside this profile by looking for its key combination.
		/// </summary>
		/// <param name="keysAsText">The key combination of the shortcut</param>
		/// <returns>A corresponding shortcut if found, otherwise null</returns>
		public KeyboardShortcut GetShortcut(String keysAsText)
		{
			foreach (KeyboardShortcut s in Shortcuts)
				if (s.Keys == keysAsText)
					return s;
			return null;
		}

		/// <summary>
		/// Find a keyboard shortcut inside this profile by looking for its name.
		/// </summary>
		/// <param name="category">The name of the category of the shortcut</param>
		/// <param name="name">The name of the shortcut</param>
		/// <returns>The keyboard shortcut corresponding to the category and name inside the profile</returns>
		public KeyboardShortcut GetShortcut(String category, String name)
		{
			foreach (KeyboardShortcut s in Shortcuts)
				if (s.Name == name && s.Category == category)
					return s;
			return null;
		}

		#endregion
	}
}

