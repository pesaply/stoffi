/***
 * Database.cs
 * 
 * Contains the glue between the properties of the Settings class and
 * the underlying SQLite database.
 * 
 * Takes care of creating, initializing, updating and loading the
 * data in the SQLite database file.
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

//using System.Data.SQLite;
using Mono.Data.Sqlite;

using Stoffi.Core.Media;
using Stoffi.Core.Playlists;
using Stoffi.Core.Plugins;
using Stoffi.Core.Services;
using Stoffi.Core.Settings;
using Stoffi.Core.Sources;

namespace Stoffi.Core.Settings
{
	/// <summary>
	/// Describes an SQLite database.
	/// </summary>
	public class Database
	{
		#region Members
		private string dbConnection;
		private SqliteConnection cnn;
		#endregion

		#region Constructor
		public Database(string filename)
		{
			if (!File.Exists(filename))
				SqliteConnection.CreateFile(filename);
			dbConnection = "uri=file:settings.s3db";
			cnn = new SqliteConnection (dbConnection);
			cnn.Open ();
		}
		public static string[] Split(string source, char separator)
		{
			char[] toks = new char[2] { '\"', separator };
			char[] quot = new char[1] { '\"' };
			int n = 0;
			List<string> ls = new List<string>();
			string s;

			while (source.Length > 0)
			{
				n = source.IndexOfAny(toks, n);
				if (n == -1) break;
				if (source[n] == toks[0])
				{
					//source = source.Remove(n, 1);
					n = source.IndexOfAny(quot, n + 1);
					if (n == -1)
					{
						//source = "\"" + source;
						break;
					}
					n++;
					//source = source.Remove(n, 1);
				}
				else
				{
					s = source.Substring(0, n).Trim();
					if (s.Length > 1 && s[0] == quot[0] && s[s.Length - 1] == s[0])
						s = s.Substring(1, s.Length - 2);

					source = source.Substring(n + 1).Trim();
					if (s.Length > 0) ls.Add(s);
					n = 0;
				}
			}
			if (source.Length > 0)
			{
				s = source.Trim();
				if (s.Length > 1 && s[0] == quot[0] && s[s.Length - 1] == s[0])
					s = s.Substring(1, s.Length - 2);
				ls.Add(s);
			}

			string[] ar = new string[ls.Count];
			ls.CopyTo(ar, 0);

			return ar;
		}
		#endregion

		#region Destructor
		~Database()
		{
			if (cnn != null)
				cnn.Close();
		}
		#endregion

		#region Methods

		public DataTable ExecuteReader(string sql)
		{
			DataTable dt = new DataTable();
			try{
				var cmd = new SqliteCommand(cnn);
				cmd.CommandText = sql;
				var reader = cmd.ExecuteReader();
				dt.Load(reader);
				reader.Close();
			}
			catch (Exception e) {
				U.L (LogLevel.Error, "Settings", "Error executing query in database: " + e.Message);
				throw e;
			}
			return dt;
		}

		public int ExecuteNonQuery(string sql)
		{
			var fails = 0;
			var maxAttempts = 5;
			while (fails < maxAttempts) {
				try
				{
					var cmd = new SqliteCommand (cnn);
					cmd.CommandText = sql;
					var rowsUpdated = cmd.ExecuteNonQuery ();
					return rowsUpdated;
				}
				catch (Exception e) {
					U.L (LogLevel.Error, "Settings", "Error executing non-query in database: " + e.Message);
				}
				fails++;
			}
			return 0;
		}

		public string ExecuteScalar(string sql)
		{
			try
			{
				var cmd = new SqliteCommand (cnn);
				cmd.CommandText = sql;
				var value = cmd.ExecuteScalar();
				if (value != null)
					return value.ToString();
			}
			catch (Exception e) {
				U.L (LogLevel.Error, "Settings", "Error executing scalar in database: " + e.Message);
				throw e;
			}
			return "";
		}

		public DataTable Select(string sql)
		{
			return ExecuteReader (sql);
		}

		public int Insert(string table, Dictionary<string,string> data)
		{
			var columns = String.Join (", ", from d in data select d.Key);
			var values = String.Join (", ", from d in data select d.Value);
			return ExecuteNonQuery (String.Format ("insert into {0}({1}) values({2});", table, columns, values));
		}

		public int LastID(string table)
		{
			var data = ExecuteReader (String.Format("select max(rowid) as id from {0};", table));
			if (data.Rows.Count == 0)
				return -1;
			return Convert.ToInt32 (data.Rows [0]["id"].ToString ());
		}

		public int Update(string table, Dictionary<string,string> data, string filter)
		{
			var vals = "";
			if (data.Count > 0)
				vals = String.Join (",", from d in data select String.Format ("{0}={1}", d.Key, d.Value));
			return ExecuteNonQuery (String.Format ("update {0} set {1} where {2};", table, vals, filter));
		}

		public int Delete(string table, string filter)
		{
			return ExecuteNonQuery (String.Format ("delete from {0} where {1};", table, filter));
		}

		public int Delete(string table)
		{
			return Delete (table, "1");
		}

		public int CreateTable(string name, string[] textFields = null, string[] integerFields = null, string[] realFields = null)
		{
			var sql = "create table if not exists " + name + " (";

			var tF = "";
			var iF = "";
			var rF = "";

			if (textFields != null)
				tF = String.Join (", ", from f in textFields select String.Format ("{0} text", f));
			if (integerFields != null)
				iF = String.Join (", ", from f in integerFields select String.Format ("{0} integer", f));
			if (realFields != null)
				rF = String.Join (", ", from f in realFields select String.Format ("{0} real", f));

			var fieldGroups = new List<string> ();
			if (tF != "")
				fieldGroups.Add (tF);
			if (iF != "")
				fieldGroups.Add (iF);
			if (rF != "")
				fieldGroups.Add (rF);

			sql += String.Join (", ", fieldGroups);
			sql += ");";
			return ExecuteNonQuery (sql);
		}

		#endregion
	}

	/// <summary>
	/// Represents a manager that takes care of all
	/// application settings.
	/// </summary>
	public static partial class Manager
	{
		#region Members
		private static Database db;
		private static object saveToDatabaseLock = new object();

		// when a collection (such as files) is changed we save the collection and the arguments
		// in a buffer and call a timer which after a delay will save the change in the database.
		private static List<Tuple<IEnumerable<object>,NotifyCollectionChangedEventArgs>> collectionChangedBuffer = new List<Tuple<IEnumerable<object>, NotifyCollectionChangedEventArgs>>();
		private static Timer collectionChangedTimer = null;

		// when an object is changed we save the object and the arguments
		// in a buffer and call a timer which after a delay will save the change in the database.
		private static List<Tuple<PropertyChangedBase,PropertyChangedEventArgs>> propertyChangedBuffer = new List<Tuple<PropertyChangedBase, PropertyChangedEventArgs>>();
		private static Timer propertyChangedTimer = null;

		private static object saveLock = new object();
		#endregion

		#region Constructor

		/// <summary>
		/// Initializes the <see cref="Stoffi.Core.Settings"/> class.
		/// </summary>
		static Manager()
		{
            InitializeDatabase();
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Saves the settings
		/// </summary>
		public static void Save()
		{
			db.Delete("listenBuffer");
			if (listenBuffer != null)
				SaveListenBuffer(listenBuffer, "listenBuffer");
		}

		#region Converters

		/// <summary>
		/// String representation of the repeat state.
		/// </summary>
		/// <returns>The string representation.</returns>
		/// <param name="repeat">Repeat state.</param>
		public static string RepeatToString(RepeatState repeat)
		{
			switch (repeat)
			{
			case RepeatState.NoRepeat:
				return "off";

			case RepeatState.RepeatAll:
				return "all";

			case RepeatState.RepeatOne:
				return "one";
			}
			return "off";
		}

		/// <summary>
		/// The repeat state represented by a string.
		/// </summary>
		/// <returns>The repeat state.</returns>
		/// <param name="repeat">The string representation.</param>
		public static RepeatState StringToRepeat(string repeat)
		{
			switch (repeat)
			{
			case "off":
				return RepeatState.NoRepeat;

			case "all":
				return RepeatState.RepeatAll;

			case "one":
				return RepeatState.RepeatOne;
			}
			return RepeatState.NoRepeat;
		}

		/// <summary>
		/// String representation of the shuffle state.
		/// </summary>
		/// <returns>The string representation.</returns>
		/// <param name="shuffle">Shuffle state.</param>
		public static string ShuffleToString(bool shuffle)
		{
			return shuffle ? "random" : "off";
		}

		/// <summary>
		/// The shuffle state represented by a string.
		/// </summary>
		/// <returns>The shuffle state.</returns>
		/// <param name="shuffle">The string representation.</param>
		public static bool StringToShuffle(string shuffle)
		{
			return shuffle == "random";
		}

		/// <summary>
		/// String representation of the media state.
		/// </summary>
		/// <returns>The string representation.</returns>
		/// <param name="mediaState">Media state.</param>
		public static string MediaStateToString(MediaState mediaState)
		{
			switch (mediaState)
			{
			case MediaState.Paused:
			case MediaState.Ended:
			case MediaState.Stopped:
				return "paused";

			case MediaState.Playing:
				return "playing";
			}
			return "paused";
		}

		/// <summary>
		/// The media state represented by a string.
		/// </summary>
		/// <returns>The media state.</returns>
		/// <param name="mediaState">The string representation.</param>
		public static MediaState StringToMediaState(string mediaState)
		{
			return mediaState == "playing" ? MediaState.Playing : MediaState.Paused;
		}

		/// <summary>
		/// String representation of the upgrade policy.
		/// </summary>
		/// <returns>The string representation.</returns>
		/// <param name="upgrade">Upgrade policy.</param>
		public static string UpgradeToString(UpgradePolicy upgrade)
		{
			switch (upgrade)
			{
			case UpgradePolicy.Automatic:
				return "auto";

			case UpgradePolicy.Manual:
				return "manual";

			case UpgradePolicy.Notify:
				return "notify";
			}
			return "auto";
		}

		/// <summary>
		/// The upgrade policy represented by a string.
		/// </summary>
		/// <returns>The upgrade policy.</returns>
		/// <param name="upgrade">The string representation.</param>
		public static UpgradePolicy StringToUpgrade(string upgrade)
		{
			switch (upgrade)
			{
			case "auto":
				return UpgradePolicy.Automatic;

			case "manual":
				return UpgradePolicy.Manual;

			case "notify":
				return UpgradePolicy.Notify;
			}
			return UpgradePolicy.Automatic;
		}

		/// <summary>
		/// String representation of the search policy.
		/// </summary>
		/// <returns>The string representation.</returns>
		/// <param name="search">Search policy.</param>
		public static string SearchToString(SearchPolicy search)
		{
			switch (search)
			{
			case SearchPolicy.Global:
				return "global";

			case SearchPolicy.Individual:
				return "individual";

			case SearchPolicy.Partial:
				return "partial";
			}
			return "individual";
		}

		/// <summary>
		/// The search policy represented by a string.
		/// </summary>
		/// <returns>The search policy.</returns>
		/// <param name="search">The string representation.</param>
		public static SearchPolicy StringToSearch(string search)
		{
			switch (search)
			{
			case "global":
				return SearchPolicy.Global;

			case "individual":
				return SearchPolicy.Individual;

			case "partial":
				return SearchPolicy.Partial;
			}
			return SearchPolicy.Individual;
		}

		/// <summary>
		/// String representation of the OpenAdd policy.
		/// </summary>
		/// <returns>The string representation.</returns>
		/// <param name="openAdd">OpenAdd policy.</param>
		public static string OpenAddToString(OpenAddPolicy openAdd)
		{
			switch (openAdd)
			{
			case OpenAddPolicy.DoNotAdd:
				return "none";

			case OpenAddPolicy.Library:
				return "library";

			case OpenAddPolicy.LibraryAndPlaylist:
				return "playlist";
			}
			return "library";
		}

		/// <summary>
		/// The OpenAdd policy represented by a string.
		/// </summary>
		/// <returns>The OpenAdd policy.</returns>
		/// <param name="openAdd">The string representation.</param>
		public static OpenAddPolicy StringToOpenAdd(string openAdd)
		{
			switch (openAdd)
			{
			case "none":
				return OpenAddPolicy.DoNotAdd;

			case "library":
				return OpenAddPolicy.Library;

			case "playlist":
				return OpenAddPolicy.LibraryAndPlaylist;
			}
			return OpenAddPolicy.Library;
		}

		/// <summary>
		/// String representation of the OpenPlay policy.
		/// </summary>
		/// <returns>The string representation.</returns>
		/// <param name="openPlay">OpenPlay policy.</param>
		public static string OpenPlayToString(OpenPlayPolicy openPlay)
		{
			switch (openPlay)
			{
			case OpenPlayPolicy.BackOfQueue:
				return "back";

			case OpenPlayPolicy.DoNotPlay:
				return "none";

			case OpenPlayPolicy.FrontOfQueue:
				return "front";

			case OpenPlayPolicy.Play:
				return "play";
			}
			return "back";
		}

		/// <summary>
		/// The OpenPlay policy represented by a string.
		/// </summary>
		/// <returns>The OpenPlay policy.</returns>
		/// <param name="openPlay">The string representation.</param>
		public static OpenPlayPolicy StringToOpenPlay(string openPlay)
		{
			switch (openPlay)
			{
			case "back":
				return OpenPlayPolicy.BackOfQueue;

			case "none":
				return OpenPlayPolicy.DoNotPlay;

			case "front":
				return OpenPlayPolicy.FrontOfQueue;

			case "play":
				return OpenPlayPolicy.Play;
			}
			return OpenPlayPolicy.BackOfQueue;
		}

		/// <summary>
		/// String representation of an alignment.
		/// </summary>
		/// <returns>The string representation.</returns>
		/// <param name="alignment">Alignment.</param>
		public static string AlignmentToString(Alignment alignment)
		{
			return alignment.ToString ().ToLower ();
		}

		/// <summary>
		/// Get the Alignment represented by a string.
		/// </summary>
		/// <returns>The alignment.</returns>
		/// <param name="alignment">The string representation.</param>
		public static Alignment StringToAlignment(string alignment)
		{
			switch (alignment)
			{
			case "bottom":
				return Alignment.Bottom;

			case "center":
				return Alignment.Center;

			case "left":
				return Alignment.Left;

			case "middle":
				return Alignment.Middle;

			case "right":
				return Alignment.Right;

			case "top":
				return Alignment.Top;
			}
			throw new InvalidEnumArgumentException ("Alignment " + alignment + " not recognized");
		}

		/// <summary>
		/// String representation of a view mode.
		/// </summary>
		/// <returns>The string representation.</returns>
		/// <param name="viewMode">ViewMode.</param>
		public static string ViewModeToString(ViewMode viewMode)
		{
			return viewMode.ToString ().ToLower ();
		}

		/// <summary>
		/// Get the ViewMode represented by a string.
		/// </summary>
		/// <returns>The view mode.</returns>
		/// <param name="viewMode">The string representation.</param>
		public static ViewMode StringToViewMode(string viewMode)
		{
			switch (viewMode)
			{
			case "content":
				return ViewMode.Content;

			case "details":
				return ViewMode.Details;

			case "icons":
				return ViewMode.Icons;

			case "list":
				return ViewMode.List;

			case "tiles":
				return ViewMode.Tiles;
			}
			throw new InvalidEnumArgumentException ("View mode " + viewMode + " not recognized");
		}

		/// <summary>
		/// String representation of a source type.
		/// </summary>
		/// <returns>The string representation.</returns>
		/// <param name="sourceType">SourceType.</param>
		public static string SourceTypeToString(SourceType sourceType)
		{
			return sourceType.ToString ().ToLower ();
		}

		/// <summary>
		/// Get the SourceType represented by a string.
		/// </summary>
		/// <returns>The source type.</returns>
		/// <param name="sourceType">The string representation.</param>
		public static SourceType StringToSourceType(string sourceType)
		{
			switch (sourceType)
			{
			case "file":
				return SourceType.File;

			case "folder":
				return SourceType.Folder;

			case "library":
				return SourceType.Library;
			}
			throw new InvalidEnumArgumentException ("Source type " + sourceType + " not recognized");
		}

		#endregion

		#endregion

		#region Private

		/// <summary>
		/// Initializes the database connection.
		/// </summary>
		/// <param name="reset">If true the database file will be reset</param>
		private static void InitializeDatabase(bool reset = false)
		{
			var folder = Path.GetDirectoryName(U.FullPath);

			if (!Directory.Exists(folder))
				Directory.CreateDirectory(folder);

			var filename = "settings.s3db";
			var path = Path.Combine(folder, filename);

			if (reset && File.Exists(path))
				File.Delete(path);

			db = new Database(path);
			CreateDatabase();
			LoadDatabase();
		}

		/// <summary>
		/// Create all databases for storing settings.
		/// </summary>
		private static void CreateDatabase()
		{
			db.CreateTable ("config", new string[] { "key", "value" });

			db.CreateTable ("cloudLinks",
				new string[] { "provider", "picture", "names", "url", "connectUrl", "error" },
				new string[] { "cloudID", "connected", "canShare", "doShare", "canListen", "doListen", "canDonate", "doDonate", "canCreatePlaylist", "doCreatePlaylist", "identity" });
			db.CreateTable ("cloudIdentities", null, new string[] { "user", "configuration", "device", "synchronizeConfig", "synchronizePlaylists", "synchronizeQueue", "synchronizeFiles" });

			db.CreateTable ("shortcuts", new string[] { "name", "category", "keys", "profile" }, new string[] { "global" });
			db.CreateTable ("shortcutProfiles", new string[] { "name", "title" }, new string[] { "protected" });

			db.CreateTable ("equalizerProfiles", new string[] { "name", "bands" }, new string[] { "protected", "echo" });

			db.CreateTable ("files",
				new string[] { "path", "title", "album", "artist", "genre", "url", "artUrl", "originalArtUrl", "source", "codecs" },
				new string[] { "year", "bitrate", "track", "channels", "number", "lastWrite", "lastPlayed", "userPlayCount", "globalPlayCount", "sampleRate", "processed" },
				new string[] { "length" }
			);
			db.CreateTable ("radio",
				new string[] { "path", "title", "album", "artist", "genre", "url", "artUrl", "originalArtUrl", "source", "codecs" },
				new string[] { "year", "bitrate", "track", "channels", "number", "lastWrite", "lastPlayed", "userPlayCount", "globalPlayCount", "sampleRate", "processed" },
				new string[] { "length" }
			);
			db.CreateTable ("bookmarks", new string[] { "track", "type" }, null, new string[] { "pos" });
			db.CreateTable ("queue", new string[] { "path" }, new string[] { "number" });
			db.CreateTable ("history", new string[] { "path" }, new string[] { "lastPlayed" });
			db.CreateTable ("playlists",
				new string[] { "name", "ownerName", "filter" },
				new string[] { "cloudID", "ownerID", "ownerCacheTime", "listConfig" },
				new string[] { "length" });
			db.CreateTable ("playlistTracks", new string[] { "path", "playlist" });

			db.CreateTable ("listColumns",
				new string[] { "name", "text", "binding", "converter", "sortOn", "align" },
				new string[] { "alwaysVisible", "sortable", "visible", "config" },
				new string[] { "width" });
			db.CreateTable ("listConfigurations",
				new string[] { "selection", "sorting", "filter", "mode" },
				new string[] { "numberColumn", "allowNumber", "showNumber", "numberPos", "useIcons", "acceptDrop", "canDragSort", "canClickSort", "lockSortOnNumber" },
				new string[] { "horizontalOffset", "verticalOffset", "verticalOffsetWithoutSearch", "iconSize" }
			);

			db.CreateTable ("sources", new string[] { "type", "data" }, new string[] { "automatic", "ignore" });

			db.CreateTable ("pluginData", null, new string[] { "plugin", "enabled", "installed" });
			db.CreateTable ("pluginSettings", new string[] { "id", "type", "value", "min", "max" }, new string[] { "visible", "plugin" });
			db.CreateTable ("pluginSettingPossibleValues", new string[] { "value" }, new string[] { "setting" });
			db.CreateTable ("listenBuffer", new string[] { "url", "method", "track" });
		}

		#region Load from database

		/// <summary>
		/// Load the settings from the database.
		/// </summary>
		private static void LoadDatabase()
		{
			LoadTracks(files, db.Select ("select * from files;"));
			LoadTrackReferences (queue, db.Select ("select * from queue order by number;"));
			LoadTrackReferences (history, db.Select ("select * from history;"));
			LoadShortcutProfiles (shortcutProfiles, db.Select ("select rowid,* from shortcutProfiles"));
			LoadEqualizerProfiles (equalizerProfiles, db.Select ("select * from equalizerProfiles"));
			LoadCloudIdentities (cloudIdentities, db.Select ("select * from cloudIdentities"));
			LoadMetadata (pluginSettings, db.Select ("select * from pluginData"));
			LoadSources (scanSources, db.Select ("select * from sources"));
			LoadPlaylists (playlists, db.Select ("select rowid,* from playlists"));
			foreach (var p in playlists)
				foreach (var t in p.Tracks)
					t.Source = p.NavigationID;
			LoadConfig(db.Select ("select * from config;"));

			var bookmarks = LoadBookmarks (db.Select ("select * from bookmarks"));
			foreach (var bookmark in bookmarks)
			{
				var track = Media.Manager.GetTrack (bookmark.Item1);
				if (track == null)
					continue;
				track.Bookmarks.Add (new Tuple<string, double>(bookmark.Item2, bookmark.Item3));
			}
		}

		/// <summary>
		/// Load settings from the configuration table.
		/// </summary>
		/// <param name="data">Data.</param>
		private static void LoadConfig(DataTable data)
		{
			// we need to save these and load after we are sure the profiles have been loaded
			var curShortcutProfile = "";
			var curEqProfile = "";
			var listConfigs = LoadListConfigurations (db.Select ("select rowid,* from listConfigurations"));

			foreach (DataRow row in data.Rows) {
				var v = row ["value"].ToString ();
				var k = row ["key"].ToString ();
				try
				{
					switch (k) {

					#region GUI
					case "winWidth":
						winWidth = Convert.ToDouble (v);
						break;

					case "winHeight":
						winHeight = Convert.ToDouble (v);
						break;

					case "winTop":
						winTop = Convert.ToDouble (v);
						break;

					case "winLeft":
						winLeft = Convert.ToDouble (v);
						break;

					case "winState":
						winState = v;
						break;

					case "equalizerWidth":
						equalizerWidth = Convert.ToDouble (v);
						break;

					case "equalizerHeight":
						equalizerHeight = Convert.ToDouble (v);
						break;

					case "equalizerTop":
						equalizerTop = Convert.ToDouble (v);
						break;

					case "equalizerLeft":
						equalizerLeft = Convert.ToDouble (v);
						break;

					case "currentSelectedNavigation":
						currentSelectedNavigation = v;
						break;

					case "navigationPaneWidth":
						navigationPaneWidth = Convert.ToDouble (v);
						break;

					case "detailsPaneHeight":
						detailsPaneHeight = Convert.ToDouble (v);
						break;

					case "detailsPaneVisible":
						detailsPaneVisible = v == "1";
						break;

					case "menuBarVisible":
						menuBarVisible = v == "1";
						break;

					case "language":
						language = v;
						break;
						#endregion

						#region List configurations
					case "sourceListConfig":
						if (listConfigs.ContainsKey(Convert.ToInt32 (v)))
							sourceListConfig = listConfigs[Convert.ToInt32 (v)];
						break;

					case "pluginListConfig":
						if (listConfigs.ContainsKey(Convert.ToInt32 (v)))
							pluginListConfig = listConfigs[Convert.ToInt32 (v)];
						break;

					case "historyListConfig":
						if (listConfigs.ContainsKey(Convert.ToInt32 (v)))
							historyListConfig = listConfigs[Convert.ToInt32 (v)];
						break;

					case "queueListConfig":
						if (listConfigs.ContainsKey(Convert.ToInt32 (v)))
							queueListConfig = listConfigs[Convert.ToInt32 (v)];
						break;

					case "fileListConfig":
						if (listConfigs.ContainsKey(Convert.ToInt32 (v)))
							fileListConfig = listConfigs[Convert.ToInt32 (v)];
						break;

					case "radioListConfig":
						if (listConfigs.ContainsKey(Convert.ToInt32 (v)))
							radioListConfig = listConfigs[Convert.ToInt32 (v)];
						break;

					case "soundCloudListConfig":
						if (listConfigs.ContainsKey(Convert.ToInt32 (v)))
							SoundCloudListConfig = listConfigs[Convert.ToInt32 (v)];
						break;

					case "youTubeListConfig":
						if (listConfigs.ContainsKey(Convert.ToInt32 (v)))
							YouTubeListConfig = listConfigs[Convert.ToInt32 (v)];
						break;

					case "jamendoListConfig":
						if (listConfigs.ContainsKey(Convert.ToInt32 (v)))
							JamendoListConfig = listConfigs[Convert.ToInt32 (v)];
						break;

					case "discListConfig":
						if (listConfigs.ContainsKey(Convert.ToInt32 (v)))
							discListConfig = listConfigs[Convert.ToInt32 (v)];
						break;
						#endregion

						#region Application params
					case "id":
						id = Convert.ToInt32(v);
						break;
						#endregion

						#region Settings
					case "upgradePolicy":
						upgradePolicy = StringToUpgrade (v);
						break;

					case "searchPolicy":
						searchPolicy = StringToSearch (v);
						break;

					case "openAddPolicy":
						openAddPolicy = StringToOpenAdd (v);
						break;

					case "openPlayPolicy":
						openPlayPolicy = StringToOpenPlay (v);
						break;

					case "fastStart":
						fastStart = v == "1";
						break;

					case "showNotifications":
						showNotifications = v == "1";
						break;

					case "pauseWhenLocked":
						pauseWhenLocked = v == "1";
						break;

					case "pauseWhenSongEnds":
						pauseWhenSongEnds = v == "1";
						break;

					case "currentShortcutProfile":
						curShortcutProfile = v;
						break;

					case "youTubeFilter":
						youTubeFilter = v;
						break;

					case "youTubeQuality":
						youTubeQuality = v;
						break;
						#endregion

						#region Playback
					case "currentActiveNavigation":
						currentActiveNavigation = v;
						break;

					case "currentTrack":
						// assume songs have already been loaded
						currentTrack = Media.Manager.GetTrack (v);
						break;

					case "currentEqualizerProfile":
						curEqProfile = v;
						break;

					case "historyIndex":
						historyIndex = Convert.ToInt32 (v);
						break;

					case "shuffle":
						shuffle = StringToShuffle (v);
						break;

					case "repeat":
						repeat = StringToRepeat(v);
						break;

					case "volume":
						volume = Convert.ToDouble (v);
						break;

					case "seek":
						seek = Convert.ToDouble (v);
						break;

					case "mediaState":
						mediaState = StringToMediaState (v);
						break;
						#endregion

						#region Cloud
						#endregion

						#region Misc
					case "firstRun":
						firstRun = v == "1";
						break;
						#endregion

					}
				}
				catch (Exception e) {
					U.L (LogLevel.Warning, "Settings", "Could not load config " + k + ": " + e.Message);
				}
			}

			if (String.IsNullOrWhiteSpace (curShortcutProfile) && shortcutProfiles != null && shortcutProfiles.Count > 0)
				currentShortcutProfile = shortcutProfiles [0];
			else
				currentShortcutProfile = GetKeyboardShortcutProfile (shortcutProfiles, curShortcutProfile);

			if (String.IsNullOrWhiteSpace (curEqProfile) && equalizerProfiles != null && equalizerProfiles.Count > 0)
				currentEqualizerProfile = equalizerProfiles [0];
			else
				currentEqualizerProfile = GetEqualizerProfile (curEqProfile);
		}

		/// <summary>
		/// Load songs from a database table.
		/// </summary>
		/// <param name="collection">Collection where the songs will be stored.</param>
		/// <param name="data">The data from the database.</param>
		private static void LoadTracks(ObservableCollection<Track> collection, DataTable data)
		{
			if (data == null)
				return;
			if (collection == null)
				throw new Exception ("Database cannot be loaded since the variable holding the collection has not been initialized.");

			collection.CollectionChanged -= ObservableCollection_CollectionChanged;
			foreach (var song in data.Rows) {
				var track = LoadTrack (song as DataRow);
				if (track != null)
					collection.Add (track);
			}
			collection.CollectionChanged += ObservableCollection_CollectionChanged;
		}

		/// <summary>
		/// Load songs via references from a database table.
		/// </summary>
		/// <param name="collection">Collection where the songs will be stored.</param>
		/// <param name="data">The data from the database.</param>
		private static void LoadTrackReferences(ObservableCollection<Track> collection, DataTable data)
		{
			if (data == null)
				return;
			if (collection == null)
				throw new Exception ("Database cannot be loaded since the variable holding the collection has not been initialized.");

			collection.CollectionChanged -= ObservableCollection_CollectionChanged;
			foreach (DataRow row in data.Rows)
			{
				try
				{
					var path = row ["path"].ToString ();
					var track = Media.Manager.GetTrack(path);
					if (track != null)
					{
						var copy = new Track();
						Files.CopyTrackInfo(track, copy);

						if (row.Table.Columns.Contains("lastPlayed"))
						{
							var lastPlayed = Convert.ToInt64(row["lastPlayed"]);
							if (lastPlayed > 0)
								copy.LastPlayed = DateTime.FromFileTimeUtc(lastPlayed);
						}

						if (row.Table.Columns.Contains("number") && !String.IsNullOrWhiteSpace(row["number"].ToString()))
						{
							copy.Number = Convert.ToInt32(row["number"]);
						}

						collection.Add (copy);
					}
				}
				catch { }
			}
			collection.CollectionChanged += ObservableCollection_CollectionChanged;
		}

		/// <summary>
		/// Turn a database row into a song.
		/// </summary>
		/// <returns>The track.</returns>
		/// <param name="row">Database row.</param>
		private static Track LoadTrack(DataRow row)
		{
			var track = new Track ();
			try
			{
				track.Album = row["album"].ToString();
				track.Artist = row["artist"].ToString();
				track.ArtURL = row["artUrl"].ToString();
				track.Bitrate = Convert.ToInt32(row["bitrate"]);
				track.Channels = Convert.ToInt32(row["channels"]);
				track.Codecs = row["codecs"].ToString();
				track.Genre = row["genre"].ToString();
				track.Image = row["artUrl"].ToString();
				var lastPlayed = Convert.ToInt64(row["lastPlayed"]);
				if (lastPlayed > 0)
					track.LastPlayed = DateTime.FromFileTimeUtc(lastPlayed);
				track.LastWrite = Convert.ToInt64(row["lastWrite"]);
				track.Length = Convert.ToDouble(row["length"]);
				track.Number = Convert.ToInt32(row["number"]);
				track.OriginalArtURL = row["originalArtUrl"].ToString();
				track.Path = row["path"].ToString();
				track.PlayCount = Convert.ToUInt32(row["userPlayCount"]);
				track.Processed = row["processed"].ToString() != "0";
				track.SampleRate = Convert.ToInt32(row["sampleRate"]);
				track.Source = row["source"].ToString();
				track.Title = row["title"].ToString();
				track.TrackNumber = Convert.ToUInt32(row["track"]);
				track.URL = row["url"].ToString();
				track.Views = Convert.ToUInt64(row["globalPlayCount"]);
				track.Year = Convert.ToUInt32(row["year"]);
				return track;
			}
			catch (Exception e) {
				U.L (LogLevel.Warning, "Settings", "Could not parse track from database: " + e.Message);
				return null;
			}
		}

		/// <summary>
		/// Loads bookmarks from a database table.
		/// </summary>
		/// <param name="data">Data from the database.</param>
		/// <returns>A list with bookmarks (track path, type, position)</returns>
		private static List<Tuple<string,string,double>> LoadBookmarks(DataTable data)
		{
			var bookmarks = new List<Tuple<string,string,double>>();
			foreach (DataRow row in data.Rows)
			{
				var bookmark = LoadBookmark (row);
				if (bookmark != null)
					bookmarks.Add (bookmark);
			}
			return bookmarks;
		}

		/// <summary>
		/// Loads a single bookmark from a database row.
		/// </summary>
		/// <param name="row">Database row.</param>
		/// <returns>The bookmark.</returns>
		private static Tuple<string,string,double> LoadBookmark(DataRow row)
		{
			try
			{
				var pos = Convert.ToDouble(row["pos"]);
				var path = row["track"].ToString();
				var type = row["type"].ToString();
				return new Tuple<string, string, double>(path,type,pos);
			}
			catch (Exception e)
			{
				U.L (LogLevel.Warning, "Settings", "Could not parse bookmark from database: " + e.Message);
				return null;
			}
		}

		/// <summary>
		/// Loads list view configurations from a database table.
		/// </summary>
		/// <param name="data">Data from the database.</param>
		/// <returns>A dictionary with list configurations and their rowids</returns>
		private static Dictionary<int,ListConfig> LoadListConfigurations(DataTable data)
		{
			var listConfigs = new Dictionary<int,ListConfig> ();
			foreach (DataRow row in data.Rows)
			{
				var config = LoadListConfiguration (row);
				if (config != null)
					listConfigs.Add (Convert.ToInt32 (row ["rowid"]), config);
			}
			return listConfigs;
		}

		/// <summary>
		/// Loads a single list view configuration from a database row.
		/// </summary>
		/// <param name="row">Database row.</param>
		/// <returns>The configuration.</returns>
		private static ListConfig LoadListConfiguration(DataRow row)
		{
			var config = new ListConfig ();
			try
			{
				config.AcceptFileDrops = row["acceptDrop"].ToString() == "1";
				config.Filter = row["filter"].ToString();
				config.HasNumber = row["allowNumber"].ToString() == "1";
				config.HorizontalScrollOffset = Convert.ToDouble(row["horizontalOffset"]);
				config.IconSize = Convert.ToDouble(row["iconSize"]);
				config.IsClickSortable = row["canClickSort"].ToString() == "1";
				config.IsDragSortable = row["canDragSort"].ToString() == "1";
				config.IsNumberVisible = row["showNumber"].ToString() == "1";
				config.LockSortOnNumber = row["lockSortOnNumber"].ToString() == "1";
				config.Mode = StringToViewMode(row["mode"].ToString());
				config.NumberIndex = Convert.ToInt32(row["numberPos"]);
				config.Sorts = new ObservableCollection<string>(row["sorting"].ToString().Split(';'));
				config.UseIcons = row["useIcons"].ToString() == "1";
				config.VerticalScrollOffset = Convert.ToDouble(row["verticalOffset"]);
				config.VerticalScrollOffsetWithoutSearch = Convert.ToDouble(row["verticalOffsetWithoutSearch"]);

				if (!String.IsNullOrWhiteSpace(row["selection"].ToString()))
				{
					var selection = from i in row["selection"].ToString().Split(';') select Convert.ToUInt32(i);
					config.SelectedIndices = new ObservableCollection<uint>(selection);
				}

				var data = db.Select("select * from listColumns where config = "+row["rowid"].ToString() + ";");
				LoadListColumns(config.Columns, data);

				data = db.Select("select * from listColumns where rowid = "+row["numberColumn"] + ";");
				config.NumberColumn = LoadListColumn(data.Rows[0]);

				return config;
			}
			catch (Exception e)
			{
				U.L (LogLevel.Warning, "Settings", "Could not parse list config from database: " + e.Message);
				return null;
			}
		}

		/// <summary>
		/// Loads list columns from a database table.
		/// </summary>
		/// <param name="columns">Collection where the columns will be stored.</param>
		/// <param name="data">Data from the database.</param>
		private static void LoadListColumns(IList<ListColumn> columns, DataTable data)
		{
			foreach (DataRow row in data.Rows)
			{
				var col = LoadListColumn (row);
				if (col != null)
					columns.Add (col);
			}
		}

		/// <summary>
		/// Loads a single list column from a database row.
		/// </summary>
		/// <param name="row">Database row.</param>
		/// <returns>The column.</returns>
		private static ListColumn LoadListColumn(DataRow row)
		{
			var column = new ListColumn ();
			try
			{
				column.Alignment = StringToAlignment(row["align"].ToString());
				column.Binding = row["binding"].ToString();
				column.Converter = row["converter"].ToString();
				column.IsAlwaysVisible = row["alwaysVisible"].ToString() == "1";
				column.IsSortable = row["sortable"].ToString() == "1";
				column.IsVisible = row["visible"].ToString() == "1";
				column.Name = row["name"].ToString();
				column.SortField = row["sortOn"].ToString();
				column.Text = row["text"].ToString();
				column.Width = Convert.ToDouble(row["width"]);
				return column;
			}
			catch (Exception e)
			{
				U.L (LogLevel.Warning, "Settings", "Could not parse list column from database: " + e.Message);
				return null;
			}
		}

		/// <summary>
		/// Loads shortcut profiles from a database table.
		/// </summary>
		/// <param name="profiles">Collection where the profiles will be stored.</param>
		/// <param name="data">Data from the database.</param>
		private static void LoadShortcutProfiles(ObservableCollection<KeyboardShortcutProfile> profiles, DataTable data)
		{
			profiles.CollectionChanged -= ObservableCollection_CollectionChanged;
			foreach (DataRow row in data.Rows)
			{
				var profile = LoadShortcutProfile (row);
				if (profile != null)
					profiles.Add (profile);
			}
			profiles.CollectionChanged += ObservableCollection_CollectionChanged;
		}

		/// <summary>
		/// Loads a single shortcut profile from a database row.
		/// </summary>
		/// <param name="row">Database row.</param>
		/// <returns>The profile.</returns>
		private static KeyboardShortcutProfile LoadShortcutProfile(DataRow row)
		{
			var profile = new KeyboardShortcutProfile ();
			try
			{
				profile.IsProtected = row["protected"].ToString() != "0";
				profile.Name = row["name"].ToString();
				var data = db.Select("select * from shortcuts where profile = "+row["rowid"].ToString() + ";");
				LoadShortcuts(profile.Shortcuts, data);
				return profile;
			}
			catch (Exception e)
			{
				U.L (LogLevel.Warning, "Settings", "Could not parse shortcut profile from database: " + e.Message);
				return null;
			}
		}

		/// <summary>
		/// Loads shortcuts from a database table.
		/// </summary>
		/// <param name="shortcuts">Collection where the shortcuts will be stored.</param>
		/// <param name="data">Data from the database.</param>
		private static void LoadShortcuts(IList<KeyboardShortcut> shortcuts, DataTable data)
		{
			foreach (DataRow row in data.Rows)
			{
				var shortcut = LoadShortcut (row);
				if (shortcut != null)
					shortcuts.Add (shortcut);
			}
		}

		/// <summary>
		/// Loads a single shortcut from a database row.
		/// </summary>
		/// <param name="row">Database row.</param>
		/// <returns>The shortcut.</returns>
		private static KeyboardShortcut LoadShortcut(DataRow row)
		{
			var shortcut = new KeyboardShortcut ();
			try
			{
				shortcut.Category = row["category"].ToString();
				shortcut.IsGlobal = row["global"].ToString() == "1";
				shortcut.Keys = row["keys"].ToString();
				shortcut.Name = row["name"].ToString();
				return shortcut;
			}
			catch (Exception e)
			{
				U.L (LogLevel.Warning, "Settings", "Could not parse shortcut from database: " + e.Message);
				return null;
			}
		}

		/// <summary>
		/// Loads equalizer profiles from a database table.
		/// </summary>
		/// <param name="profiles">Collection where the profiles will be stored.</param>
		/// <param name="data">Data from the database.</param>
		private static void LoadEqualizerProfiles(ObservableCollection<EqualizerProfile> profiles, DataTable data)
		{
			profiles.CollectionChanged -= ObservableCollection_CollectionChanged;
			foreach (DataRow row in data.Rows)
			{
				var profile = LoadEqualizerProfile (row);
				if (profile != null)
					profiles.Add (profile);
			}
			profiles.CollectionChanged += ObservableCollection_CollectionChanged;
		}

		/// <summary>
		/// Loads a single equalizer profile from a database row.
		/// </summary>
		/// <param name="row">Database row.</param>
		/// <returns>The profile.</returns>
		private static EqualizerProfile LoadEqualizerProfile(DataRow row)
		{
			var profile = new EqualizerProfile ();
			try
			{
				profile.IsProtected = row["protected"].ToString() != "0";
				profile.Name = row["name"].ToString();
				profile.EchoLevel = (float)Convert.ToDouble(row["echo"]);
				var levels = from i in row["bands"].ToString().Split(';') select float.Parse(i);
				profile.Levels = new ObservableCollection<float>(levels);
				return profile;
			}
			catch (Exception e)
			{
				U.L (LogLevel.Warning, "Settings", "Could not parse equalizer profile from database: " + e.Message);
				return null;
			}
		}

		/// <summary>
		/// Loads cloud identitites from a database table.
		/// </summary>
		/// <param name="identities">Collection where the identities will be stored.</param>
		/// <param name="data">Data from the database.</param>
		private static void LoadCloudIdentities(ObservableCollection<Identity> identities, DataTable data)
		{
			identities.CollectionChanged -= ObservableCollection_CollectionChanged;
			foreach (DataRow row in data.Rows)
			{
				var identity = LoadCloudIdentity (row);
				if (identity != null)
					identities.Add (identity);
			}
			identities.CollectionChanged += ObservableCollection_CollectionChanged;
		}

		/// <summary>
		/// Loads a single cloud identity from a database row.
		/// </summary>
		/// <param name="row">Database row.</param>
		/// <returns>The identity.</returns>
		private static Identity LoadCloudIdentity(DataRow row)
		{
			var identity = new Identity ();
			try
			{
				identity.ConfigurationID = Convert.ToUInt32(row["configuration"]);
				identity.DeviceID = Convert.ToUInt32(row["device"]);
				identity.UserID = Convert.ToUInt32(row["user"]);
				identity.Synchronize = row["synchronize"].ToString() == "1";
				identity.SynchronizeConfig = row["synchronizeConfig"].ToString() == "1";
				identity.SynchronizeFiles = row["synchronizeFiles"].ToString() == "1";
				identity.SynchronizePlaylists = row["synchronizePlaylists"].ToString() == "1";
				identity.SynchronizeQueue = row["synchronizeQueue"].ToString() == "1";
				var data = db.Select("select * from cloudLinks where identity = "+row["rowid"] + ";");
				LoadCloudLinks(identity.Links, data);
				return identity;
			}
			catch (Exception e)
			{
				U.L (LogLevel.Warning, "Settings", "Could not parse cloud identity from database: " + e.Message);
				return null;
			}
		}

		/// <summary>
		/// Loads cloud links from a database table.
		/// </summary>
		/// <param name="links">Collection where the links will be stored.</param>
		/// <param name="data">Data from the database.</param>
		private static void LoadCloudLinks(IList<Link> links, DataTable data)
		{
			foreach (DataRow row in data.Rows)
			{
				var link = LoadCloudLink (row);
				if (link != null)
					links.Add (link);
			}
		}

		/// <summary>
		/// Loads a single cloud link from a database row.
		/// </summary>
		/// <param name="row">Database row.</param>
		/// <returns>The link.</returns>
		private static Link LoadCloudLink(DataRow row)
		{
			var link = new Link ();
			try
			{
				link.CanCreatePlaylist = row["canCreatePlaylist"].ToString() != "0";
				link.CanDonate = row["canDonate"].ToString() != "0";
				link.CanListen = row["canListen"].ToString() != "0";
				link.CanShare = row["canShare"].ToString() != "0";
				link.Connected = row["connected"].ToString() != "0";
				link.ConnectURL = row["connectUrl"].ToString();
				link.DoCreatePlaylist = row["doCreatePlaylist"].ToString() != "0";
				link.DoDonate = row["doDonate"].ToString() != "0";
				link.DoListen = row["doListen"].ToString() != "0";
				link.DoShare = row["doShare"].ToString() != "0";
				link.Error = row["error"].ToString();
				link.Picture = row["picture"].ToString();
				link.Provider = row["provider"].ToString();
				link.URL = row["url"].ToString();
				link.ID = Convert.ToUInt32(row["cloudID"]);
				link.Names = new ObservableCollection<string> (row["names"].ToString().Split('\n'));
				return link;
			}
			catch (Exception e)
			{
				U.L (LogLevel.Warning, "Settings", "Could not parse cloud link from database: " + e.Message);
				return null;
			}
		}

		/// <summary>
		/// Loads plugin meta data from a database table.
		/// </summary>
		/// <param name="pluginData">Collection where the meta data will be stored.</param>
		/// <param name="data">Data from the database.</param>
		private static void LoadMetadata(ObservableCollection<Metadata> pluginData, DataTable data)
		{
			pluginData.CollectionChanged -= ObservableCollection_CollectionChanged;
			foreach (DataRow row in data.Rows)
			{
				var pData = LoadMetadata (row);
				if (pData != null)
					pluginData.Add (pData);
			}
			pluginData.CollectionChanged += ObservableCollection_CollectionChanged;
		}

		/// <summary>
		/// Loads plugin meta data for a single plugin from a database row.
		/// </summary>
		/// <param name="row">Database row.</param>
		/// <returns>The meta data.</returns>
		private static Metadata LoadMetadata(DataRow row)
		{
			var settings = new Metadata ();
			try
			{
				settings.Enabled = row["enabled"].ToString() != "0";
				var installed = Convert.ToInt64(row["installed"]);
				if (installed > 0)
					settings.Installed = DateTime.FromFileTimeUtc(installed);
				settings.PluginID = row["plugin"].ToString();
				var data = db.Select("select * from pluginSettings where plugin = "+row["rowid"].ToString() + ";");
				LoadPluginSettings(settings.Settings, data);
				return settings;
			}
			catch (Exception e)
			{
				U.L (LogLevel.Warning, "Settings", "Could not parse plugin data from database: " + e.Message);
				return null;
			}
		}

		/// <summary>
		/// Loads plugin settings from a database table.
		/// </summary>
		/// <param name="settings">Collection where the settings will be stored.</param>
		/// <param name="data">Data from the database.</param>
		private static void LoadPluginSettings(IList<Stoffi.Plugins.Setting> settings, DataTable data)
		{
			foreach (DataRow row in data.Rows)
			{
				var setting = LoadPluginSetting (row);
				if (setting != null)
					settings.Add (setting);
			}
		}

		/// <summary>
		/// Loads a single plugin setting from a database row.
		/// </summary>
		/// <param name="row">Database row.</param>
		/// <returns>The setting.</returns>
		private static Stoffi.Plugins.Setting LoadPluginSetting(DataRow row)
		{
			var setting = new Stoffi.Plugins.Setting ();
			try
			{
				setting.ID = row["id"].ToString();
				setting.IsVisible = row["visible"].ToString() == "1";
				setting.Maximum = (object)row["max"].ToString();
				setting.Minimum = (object)row["min"].ToString();
				setting.SerializedType = row["type"].ToString();
				setting.SerializedValue = row["value"].ToString();

				var data = db.Select("select * from pluginSettingPossibleValues where plugin = " + row["rowid"].ToString() + ";");
				foreach (DataRow r in data.Rows)
				{
					try
					{
						setting.PossibleValues.Add((object)row["value"].ToString());
					}
					catch {}
				}

				return setting;
			}
			catch (Exception e)
			{
				U.L (LogLevel.Warning, "Settings", "Could not parse possible value for plugin setting from database: " + e.Message);
				return null;
			}
		}

		/// <summary>
		/// Loads scan sources from a database table.
		/// </summary>
		/// <param name="sources">Collection where the sources will be stored.</param>
		/// <param name="data">Data from the database.</param>
		private static void LoadSources(ObservableCollection<Location> sources, DataTable data)
		{
			sources.CollectionChanged -= ObservableCollection_CollectionChanged;
			foreach (DataRow row in data.Rows)
			{
				var source = LoadSource (row);
				if (source != null)
					sources.Add (source);
			}
			sources.CollectionChanged += ObservableCollection_CollectionChanged;
		}

		/// <summary>
		/// Loads a single scan source from a database row.
		/// </summary>
		/// <param name="row">Database row.</param>
		/// <returns>The source.</returns>
		private static Location LoadSource(DataRow row)
		{
			var source = new Location ();
			try
			{
				source.Automatic = row["automatic"].ToString() != "0";
				source.Data = row["data"].ToString();
				source.Ignore = row["ignore"].ToString() != "0";
				source.Type = StringToSourceType(row["type"].ToString());
				return source;
			}
			catch (Exception e)
			{
				U.L (LogLevel.Warning, "Settings", "Could not parse source from database: " + e.Message);
				return null;
			}
		}

		/// <summary>
		/// Loads playlists from a database table.
		/// </summary>
		/// <param name="playlists">Collection where the playlists will be stored.</param>
		/// <param name="data">Data from the database.</param>
		private static void LoadPlaylists(ObservableCollection<Playlist> playlists, DataTable data)
		{
			playlists.CollectionChanged -= ObservableCollection_CollectionChanged;
			foreach (DataRow row in data.Rows)
			{
				var playlist = LoadPlaylist (row);
				if (playlist != null)
					playlists.Add (playlist);
			}
			playlists.CollectionChanged += ObservableCollection_CollectionChanged;
		}

		/// <summary>
		/// Loads a single playlist from a database row.
		/// </summary>
		/// <param name="row">Database row.</param>
		/// <returns>The playlist.</returns>
		private static Playlist LoadPlaylist(DataRow row)
		{
			var playlist = new Playlist ();
			try
			{
				playlist.Filter = row["filter"].ToString();
				playlist.ID = Convert.ToUInt32(row["cloudID"]);
				playlist.Name = row["name"].ToString();
				var cacheTime = Convert.ToInt64(row["ownerCacheTime"]);
				if (cacheTime > 0)
					playlist.OwnerCacheTime = DateTime.FromFileTimeUtc(cacheTime);
				playlist.OwnerID = Convert.ToUInt32(row["ownerID"]);
				playlist.OwnerName = row["ownerName"].ToString();
				playlist.Time = Convert.ToUInt32(row["length"]);

				var data = db.Select("select * from playlistTracks where playlistTracks.playlist = "+row["rowid"].ToString() + ";");
				LoadTrackReferences(playlist.Tracks, data);

				data = db.Select("select rowid,* from listConfigurations where rowid = "+row["listConfig"].ToString() + ";");
				playlist.ListConfig = LoadListConfiguration(data.Rows[0]);

				return playlist;
			}
			catch (Exception e)
			{
				U.L (LogLevel.Warning, "Settings", "Could not load playlist from database: " + e.Message);
				return null;
			}
		}

		#endregion

		#region Save to database

		/// <summary>
		/// Save a property to the database.
		/// </summary>
		/// <param name="property">Property.</param>
		private static void Save(string property)
		{
			lock (saveLock)
			{
				int rowid;

				switch (property) {

				#region GUI
				case "WinWidth":
					SaveConfig ("config", "winWidth", DBEncode(winWidth));
					break;

				case "WinHeight":
					SaveConfig ("config", "winHeight", DBEncode(winHeight));
					break;

				case "WinTop":
					SaveConfig ("config", "winTop", DBEncode(winTop));
					break;

				case "WinLeft":
					SaveConfig ("config", "winLeft", DBEncode(winLeft));
					break;

				case "WinState":
					SaveConfig ("config", "winState", winState);
					break;

				case "EqualizerWidth":
					SaveConfig ("config", "equalizerWidth", DBEncode(equalizerWidth));
					break;

				case "EqualizerHeight":
					SaveConfig ("config", "equalizerHeight", DBEncode(equalizerHeight));
					break;

				case "EqualizerTop":
					SaveConfig ("config", "equalizerTop", DBEncode(equalizerTop));
					break;

				case "EqualizerLeft":
					SaveConfig ("config", "equalizerLeft", DBEncode(equalizerLeft));
					break;

				case "CurrentSelectedNavigation":
					SaveConfig ("config", "currentSelectedNavigation", currentSelectedNavigation);
					break;

				case "NavigationPaneWidth":
					SaveConfig ("config", "navigationPaneWidth", DBEncode(navigationPaneWidth));
					break;

				case "DetailsPaneHeight":
					SaveConfig ("config", "detailsPaneHeight", DBEncode(detailsPaneHeight));
					break;

				case "DetailsPaneVisible":
					SaveConfig ("config", "detailsPaneVisible", DBEncode(detailsPaneVisible));
					break;

				case "MenuBarVisible":
					SaveConfig ("config", "menuBarVisible", DBEncode(menuBarVisible));
					break;

				case "Language":
					SaveConfig ("config", "language", language);
					break;
					#endregion

					#region Lists
				case "SourceListConfig":
					if (sourceListConfig != null)
					{
						Update<ListConfig>("listConfigurations", sourceListConfig);
						sourceListConfig.PropertyChanged -= Object_PropertyChanged;
						sourceListConfig.PropertyChanged += Object_PropertyChanged;
					}
					break;

				case "Sources":
					db.Delete ("sources");
					if (scanSources != null) {
						SaveSources (scanSources, "sources");
						scanSources.CollectionChanged -= ObservableCollection_CollectionChanged;
						scanSources.CollectionChanged += ObservableCollection_CollectionChanged;
					}
					break;

				case "PluginListConfig":
					if (pluginListConfig != null)
					{
						Update<ListConfig>("listConfigurations", pluginListConfig);
						pluginListConfig.PropertyChanged -= Object_PropertyChanged;
						pluginListConfig.PropertyChanged += Object_PropertyChanged;
					}
					break;

				case "HistoryTracks":
					db.Delete ("history");
					if (history != null) {
						SaveTrackReferences (history, "history", new string[] { "lastPlayed" });
						history.CollectionChanged -= ObservableCollection_CollectionChanged;
						history.CollectionChanged += ObservableCollection_CollectionChanged;
					}
					break;

				case "HistoryListConfig":
					if (historyListConfig != null)
					{
						Update<ListConfig>("listConfigurations", historyListConfig);
						historyListConfig.PropertyChanged -= Object_PropertyChanged;
						historyListConfig.PropertyChanged += Object_PropertyChanged;
					}
					break;

				case "QueueTracks":
					db.Delete ("queue");
					if (queue != null) {
						SaveTrackReferences (queue, "queue", new string[] { "number" });
						queue.CollectionChanged -= ObservableCollection_CollectionChanged;
						queue.CollectionChanged += ObservableCollection_CollectionChanged;
					}
					break;

				case "QueueListConfig":
					if (queueListConfig != null)
					{
						Update<ListConfig>("listConfigurations", queueListConfig);
						queueListConfig.PropertyChanged -= Object_PropertyChanged;
						queueListConfig.PropertyChanged += Object_PropertyChanged;
					}
					break;

				case "FileTracks":
					db.Delete ("files");
					if (files != null) {
						SaveTracks (files, "files");
						files.CollectionChanged -= ObservableCollection_CollectionChanged;
						files.CollectionChanged += ObservableCollection_CollectionChanged;
					}
					break;

				case "FileListConfig":
					if (fileListConfig != null)
					{
						Update<ListConfig>("listConfigurations", fileListConfig);
						fileListConfig.PropertyChanged -= Object_PropertyChanged;
						fileListConfig.PropertyChanged += Object_PropertyChanged;
					}
					break;

				case "RadioTracks":
					db.Delete ("radio");
					if (radio != null) {
						SaveTracks (radio, "radio");
						radio.CollectionChanged -= ObservableCollection_CollectionChanged;
						radio.CollectionChanged += ObservableCollection_CollectionChanged;
					}
					break;

				case "RadioListConfig":
					if (radioListConfig != null)
					{
						Update<ListConfig>("listConfigurations", radioListConfig);
						radioListConfig.PropertyChanged -= Object_PropertyChanged;
						radioListConfig.PropertyChanged += Object_PropertyChanged;
					}
					break;

				case "SoundCloudListConfig":
					if (SoundCloudListConfig != null)
					{
						Update<ListConfig>("listConfigurations", SoundCloudListConfig);
						SoundCloudListConfig.PropertyChanged -= Object_PropertyChanged;
						SoundCloudListConfig.PropertyChanged += Object_PropertyChanged;
					}
					break;

				case "YouTubeListConfig":
					if (YouTubeListConfig != null)
					{
						Update<ListConfig>("listConfigurations", YouTubeListConfig);
						YouTubeListConfig.PropertyChanged -= Object_PropertyChanged;
						YouTubeListConfig.PropertyChanged += Object_PropertyChanged;
					}
					break;

				case "JamendoListConfig":
					if (JamendoListConfig != null)
					{
						Update<ListConfig>("listConfigurations", JamendoListConfig);
						JamendoListConfig.PropertyChanged -= Object_PropertyChanged;
						JamendoListConfig.PropertyChanged += Object_PropertyChanged;
					}
					break;

				case "DiscListConfig":
					if (discListConfig != null)
					{
						Update<ListConfig>("listConfigurations", discListConfig);
						discListConfig.PropertyChanged -= Object_PropertyChanged;
						discListConfig.PropertyChanged += Object_PropertyChanged;
					}
					break;
					#endregion

					#region Application params
				case "ID":
					SaveConfig ("config", "id", DBEncode(id));
					break;
					#endregion

					#region Settings
				case "UpgradePolicy":
					SaveConfig ("config", "upgradePolicy", UpgradeToString(upgradePolicy));
					break;

				case "SearchPolicy":
					SaveConfig ("config", "searchPolicy", SearchToString(searchPolicy));
					break;

				case "OpenAddPolicy":
					SaveConfig ("config", "openAddPolicy", OpenAddToString(openAddPolicy));
					break;

				case "OpenPlayPolicy":
					SaveConfig ("config", "openPlayPolicy", OpenPlayToString(openPlayPolicy));
					break;

				case "FastStart":
					SaveConfig ("config", "fastStart", DBEncode(fastStart));
					break;

				case "ShowOSD":
					SaveConfig ("config", "showNotifications", DBEncode(showNotifications));
					break;

				case "PauseWhenLocked":
					SaveConfig ("config", "pauseWhenLocked", DBEncode(pauseWhenLocked));
					break;

				case "PauseWhenSongEnds":
					SaveConfig ("config", "pauseWhenSongEnds", DBEncode(pauseWhenSongEnds));
					break;

				case "CurrentShortcutProfile":
					rowid = GetID<KeyboardShortcutProfile>("shortcutProfiles", currentShortcutProfile);
					if (rowid >= 0)
						SaveConfig("config", "currentShortcutProfile", DBEncode(rowid));
					break;

				case "ShortcutProfiles":
					db.Delete("shortcutProfiles");
					if (shortcutProfiles != null) {
						SaveShortcutProfiles(shortcutProfiles, "shortcutProfiles");
						shortcutProfiles.CollectionChanged -= ObservableCollection_CollectionChanged;
						shortcutProfiles.CollectionChanged += ObservableCollection_CollectionChanged;
					}
					break;

				case "PluginSettings":
					db.Delete ("pluginData");
					if (pluginSettings != null) {
						SaveMetadata (pluginSettings, "pluginData");
						pluginSettings.CollectionChanged -= ObservableCollection_CollectionChanged;
						pluginSettings.CollectionChanged += ObservableCollection_CollectionChanged;
					}
					break;

				case "YouTubeFilter":
					SaveConfig ("config", "youTubeFilter", youTubeFilter);
					break;

				case "YouTubeQuality":
					SaveConfig ("config", "youTubeQuality", youTubeQuality);
					break;
					#endregion

					#region Playback
				case "CurrentActiveNavigation":
					SaveConfig ("config", "currentActiveNavigation", currentActiveNavigation);
					break;

				case "CurrentTrack":
					SaveConfig ("config", "currentTrack", currentTrack == null ? "" : currentTrack.Path);
					break;

				case "CurrentEqualizerProfile":
					rowid = GetID<EqualizerProfile>("equalizerProfiles", currentEqualizerProfile);
					if (rowid >= 0)
						SaveConfig("config", "currentEqualizerProfile", DBEncode(rowid));
					break;

				case "EqualizerProfiles":
					db.Delete("equalizerProfiles");
					if (shortcutProfiles != null) {
						SaveEqualizerProfiles(equalizerProfiles, "equalizerProfiles");
						equalizerProfiles.CollectionChanged -= ObservableCollection_CollectionChanged;
						equalizerProfiles.CollectionChanged += ObservableCollection_CollectionChanged;
					}
					break;

				case "HistoryIndex":
					SaveConfig ("config", "historyIndex", DBEncode(historyIndex));
					break;

				case "Shuffle":
					SaveConfig ("config", "shuffle", ShuffleToString(shuffle));
					break;

				case "Repeat":
					SaveConfig ("config", "repeat", RepeatToString(repeat));
					break;

				case "Volume":
					SaveConfig ("config", "volume", DBEncode(volume));
					break;

				case "Seek":
					SaveConfig ("config", "seek", DBEncode(seek));
					break;

				case "MediaState":
					SaveConfig ("config", "mediaState", MediaStateToString(mediaState));
					break;
					#endregion

					#region Cloud
				case "DownloadAlbumArt":
					SaveConfig ("config", "downloadAlbumArt", DBEncode(downloadAlbumArt));
					break;

				case "DownloadAlbumArtSmall":
					SaveConfig ("config", "downloadAlbumArtSmall", DBEncode(downloadAlbumArtSmall));
					break;

				case "DownloadAlbumArtMedium":
					SaveConfig ("config", "downloadAlbumArtMedium", DBEncode(downloadAlbumArtMedium));
					break;

				case "DownloadAlbumArtLarge":
					SaveConfig ("config", "downloadAlbumArtLarge", DBEncode(downloadAlbumArtLarge));
					break;

				case "DownloadAlbumArtHuge":
					SaveConfig ("config", "downloadAlbumArtHuge", DBEncode(downloadAlbumArtHuge));
					break;

				case "CloudIdentities":
					db.Delete("cloudIdentities");
					if (cloudIdentities != null) {
						SaveCloudIdentities(cloudIdentities, "cloudIdentities");
						cloudIdentities.CollectionChanged -= ObservableCollection_CollectionChanged;
						cloudIdentities.CollectionChanged += ObservableCollection_CollectionChanged;
					}
					break;

				case "SubmitSongs":
					SaveConfig ("config", "submitSongs", DBEncode(submitSongs));
					break;
					#endregion

					#region Misc
				case "FirstRun":
					SaveConfig ("config", "firstRun", DBEncode(firstRun));
					break;

				case "UpgradeCheck":
					SaveConfig ("config", "lastUpgradeCheck", DBEncode(lastUpgradeCheck));
					break;

				case "Playlists":
					db.Delete("playlists");
					if (playlists != null) {
						SavePlaylists(playlists, "playlists");
						playlists.CollectionChanged -= ObservableCollection_CollectionChanged;
						playlists.CollectionChanged += ObservableCollection_CollectionChanged;
					}
					break;

				case "OAuthSecret":
					SaveConfig ("config", "oauthSecret", oauthSecret);
					break;

				case "OAuthToken":
					SaveConfig ("config", "oauthToken", oauthToken);
					break;

				case "CurrentVisualizer":
					SaveConfig ("config", "currentVisualizer", currentVisualizer);
					break;
					#endregion

				default:
					break;
				}
			}
		}

		/// <summary>
		/// Save a key-value pair to the database.
		/// </summary>
		/// <param name="table">Database table.</param>
		/// <param name="key">Configuration key.</param>
		/// <param name="value">Value.</param>
		private static void SaveConfig(string table, string key, string value)
		{
			var d = new Dictionary<string,string> ();
			d ["key"] = DBEncode (key);
			d ["value"] = DBEncode (value);

			// check if key exists
			var filter = String.Format ("key='{0}'", key);
			var result = db.Select(String.Format("select * from config where {0};", filter));	
			if (result.Rows.Count > 0)
				db.Update (table, d, filter);
			else
				db.Insert (table, d);
		}

		/// <summary>
		/// Save tracks to a database table.
		/// </summary>
		/// <param name="tracks">Track collection.</param>
		/// <param name="table">Database table.</param>
		private static void SaveTracks(IEnumerable<Track> tracks, string table)
		{
			while (true)
			{
				try
				{
					foreach (var track in tracks)
						SaveTrack(track, table);
					break;
				}
				catch (InvalidOperationException e) { } // collection was modified while trying to save
				catch { }
			}
		}

		/// <summary>
		/// Save a single track to a database table.
		/// </summary>
		/// <param name="track">Track.</param>
		/// <param name="table">Database table.</param>
		private static void SaveTrack(Track track, string table)
		{
			var data = CreateData (track);
			db.Insert (table, data);
		}

		/// <summary>
		/// Save track references to a database table.
		/// </summary>
		/// <param name="tracks">Track collection.</param>
		/// <param name="table">Database table.</param>
		/// <param name="fieldsToCopy">The fields to copy into the table in addition to the path.</param>
		private static void SaveTrackReferences(IEnumerable<Track> tracks, string table, string[] fieldsToCopy = null)
		{
			foreach (Track track in tracks)
				SaveTrackReference (track, table, fieldsToCopy);
		}

		/// <summary>
		/// Save a reference to a single track in a database table.
		/// </summary>
		/// <param name="track">Track.</param>
		/// <param name="table">Database table.</param>
		/// <param name="fieldsToCopy">The fields to copy into the table in addition to the path.</param>
		private static void SaveTrackReference(Track track, string table, string[] fieldsToCopy = null)
		{
			var data = new Dictionary<string,string> ();
			data ["path"] = DBEncode(track.Path);

			if (fieldsToCopy != null)
			{
				foreach (var field in fieldsToCopy)
				{
					switch (field)
					{
					case "lastPlayed":
						data [field] = DBEncode (track.LastPlayed);
						break;

					case "number":
						data [field] = DBEncode (track.Number);
						break;
					}
				}
			}

			db.Insert (table, data);
		}

		/// <summary>
		/// Save bookmarks to a database table.
		/// </summary>
		/// <param name="bookmarks">Bookmark collection.</param>
		/// <param name="table">Database table.</param>
		private static void SaveBookmarks(IList<Tuple<string,string,double>> bookmarks, string table)
		{
			foreach (var bookmark in bookmarks)
				SaveBookmark (bookmark, table);
		}

		/// <summary>
		/// Save a single bookmark to a database table.
		/// </summary>
		/// <param name="bookmark">A tuple with track path, type and position of bookmark.</param>
		/// <param name="table">Database table.</param>
		private static void SaveBookmark(Tuple<string,string,double> bookmark, string table)
		{
			var data = new Dictionary<string,string> ();
			data ["track"] = DBEncode (bookmark.Item1);
			data ["type"] = DBEncode (bookmark.Item2);
			data ["pos"] = DBEncode (bookmark.Item3);
			db.Insert (table, data);
		}

		/// <summary>
		/// Save list view configuration to a database table.
		/// </summary>
		/// <param name="configs">List view configuration collection.</param>
		/// <param name="table">Database table.</param>
		private static void SaveListConfigurations(IEnumerable<ListConfig> configs, string table)
		{
			foreach (var config in configs)
				SaveListConfiguration (config, table);
		}

		/// <summary>
		/// Save a single list view configuration to a database table.
		/// </summary>
		/// <param name="config">A list view configuration.</param>
		/// <param name="table">Database table.</param>
		private static void SaveListConfiguration(ListConfig config, string table)
		{
			var data = CreateData (config);
			SaveColumn (config.NumberColumn, "listColumns", -1);
			data ["numberColumn"] = DBEncode (db.LastID ("listColumns"));
			db.Insert (table, data);
			var id = db.LastID (table);
			SaveColumns (config.Columns, "listColumns", id);
		}

		/// <summary>
		/// Save list view columns to a database table.
		/// </summary>
		/// <param name="columns">Cloud identity collection.</param>
		/// <param name="table">Database table.</param>
		/// <param name="parentID">The ID of the config row.</param>
		private static void SaveColumns(IEnumerable<ListColumn> columns, string table, int parentID)
		{
			foreach (var column in columns)
				SaveColumn (column, table, parentID);
		}

		/// <summary>
		/// Save a single list view column to a database table.
		/// </summary>
		/// <param name="column">A list view column.</param>
		/// <param name="table">Database table.</param>
		/// <param name="parentID">The ID of the config row.</param>
		private static void SaveColumn(ListColumn column, string table, int parentID)
		{
			var data = CreateData (column);
			data ["config"] = DBEncode (parentID);
			db.Insert (table, data);
		}

		/// <summary>
		/// Save shortcut profiles to a database table.
		/// </summary>
		/// <param name="profiles">Shortcut profile collection.</param>
		/// <param name="table">Database table.</param>
		private static void SaveShortcutProfiles(IEnumerable<KeyboardShortcutProfile> profiles, string table)
		{
			foreach (var profile in profiles)
				SaveShortcutProfile (profile, table);
		}

		/// <summary>
		/// Save a single shortcut profile to a database table.
		/// </summary>
		/// <param name="profile">A shortcut profile.</param>
		/// <param name="table">Database table.</param>
		private static void SaveShortcutProfile(KeyboardShortcutProfile profile, string table)
		{
			var data = CreateData (profile);
			db.Insert (table, data);
			SaveShortcuts (profile.Shortcuts, "shortcuts", db.LastID(table));
		}

		/// <summary>
		/// Save shortcuts to a database table.
		/// </summary>
		/// <param name="shortcuts">Shortcut collection.</param>
		/// <param name="table">Database table.</param>
		/// <param name="parentID">The ID of the profile row.</param>
		private static void SaveShortcuts(IEnumerable<KeyboardShortcut> shortcuts, string table, int parentID)
		{
			foreach (var shortcut in shortcuts)
				SaveShortcut (shortcut, table, parentID);
		}

		/// <summary>
		/// Save a single shortcut to a database table.
		/// </summary>
		/// <param name="shortcut">A shortcut.</param>
		/// <param name="table">Database table.</param>
		/// <param name="parentID">The ID of the profile row.</param>
		private static void SaveShortcut(KeyboardShortcut shortcut, string table, int parentID)
		{
			var data = CreateData (shortcut);
			data ["profile"] = DBEncode (parentID);
			db.Insert (table, data);
		}

		/// <summary>
		/// Save equalizer profiles to a database table.
		/// </summary>
		/// <param name="profiles">Equalizer profile collection.</param>
		/// <param name="table">Database table.</param>
		private static void SaveEqualizerProfiles(IEnumerable<EqualizerProfile> profiles, string table)
		{
			foreach (var profile in profiles)
				SaveEqualizerProfile (profile, table);
		}

		/// <summary>
		/// Save a single equalizer profile to a database table.
		/// </summary>
		/// <param name="profile">A equalizer profile.</param>
		/// <param name="table">Database table.</param>
		private static void SaveEqualizerProfile(EqualizerProfile profile, string table)
		{
			var data = CreateData (profile);
			db.Insert (table, data);
		}

		/// <summary>
		/// Save cloud identities to a database table.
		/// </summary>
		/// <param name="identities">Cloud identity collection.</param>
		/// <param name="table">Database table.</param>
		private static void SaveCloudIdentities(IEnumerable<Identity> identities, string table)
		{
			foreach (var identity in identities)
				SaveCloudIdentity (identity, table);
		}

		/// <summary>
		/// Save a single cloud identity to a database table.
		/// </summary>
		/// <param name="identity">A cloud identity.</param>
		/// <param name="table">Database table.</param>
		private static void SaveCloudIdentity(Identity identity, string table)
		{
			var data = CreateData (identity);
			db.Insert (table, data);
			SaveCloudLinks (identity.Links, "cloudLinks", db.LastID(table));
		}

		/// <summary>
		/// Save cloud links to a database table.
		/// </summary>
		/// <param name="links">Cloud link collection.</param>
		/// <param name="table">Database table.</param>
		/// <param name="parentID">The ID of the cloud identity row.</param>
		private static void SaveCloudLinks(IEnumerable<Link> links, string table, int parentID)
		{
			foreach (var link in links)
				SaveCloudLink (link, table, parentID);
		}

		/// <summary>
		/// Save a single cloud link to a database table.
		/// </summary>
		/// <param name="link">A cloud link.</param>
		/// <param name="table">Database table.</param>
		/// <param name="parentID">The ID of the cloud identity row.</param>
		private static void SaveCloudLink(Link link, string table, int parentID)
		{
			var data = CreateData (link);
			data ["identity"] = DBEncode (parentID);
			db.Insert (table, data);
		}

		/// <summary>
		/// Save plugin meta data to a database table.
		/// </summary>
		/// <param name="pluginData">Meta data collection.</param>
		/// <param name="table">Database table.</param>
		private static void SaveMetadata(IEnumerable<Metadata> data, string table)
		{
			foreach (var d in data)
				SaveMetadata (d, table);
		}

		/// <summary>
		/// Save a single plugin to a database table.
		/// </summary>
		/// <param name="pluginData">The plugin meta data.</param>
		/// <param name="table">Database table.</param>
		private static void SaveMetadata(Metadata pluginData, string table)
		{
			var data = CreateData (pluginData);
			db.Insert (table, data);
			SavePluginSettings (pluginData.Settings, "pluginSettings", db.LastID(table));
		}

		/// <summary>
		/// Save settings to a database table.
		/// </summary>
		/// <param name="bookmarks">Setting collection.</param>
		/// <param name="table">Database table.</param>
		/// <param name="parentID">The ID of the plugin data row.</param>
		private static void SavePluginSettings(IEnumerable<Stoffi.Plugins.Setting> settings, string table, int parentID)
		{
			foreach (var setting in settings)
				SavePluginSetting (setting, table, parentID);
		}

		/// <summary>
		/// Save a single plugin setting to a database table.
		/// </summary>
		/// <param name="setting">The plugin setting.</param>
		/// <param name="table">Database table.</param>
		/// <param name="parentID">The ID of the plugin data row.</param>
		private static void SavePluginSetting(Stoffi.Plugins.Setting setting, string table, int parentID)
		{
			var data = CreateData (setting);
			data ["plugin"] = DBEncode (parentID);
			db.Insert (table, data);

			var id = db.LastID(table);
			foreach (var v in setting.PossibleValues)
			{
				data.Clear ();
				data ["plugin"] = DBEncode (id);
				data ["value"] = DBEncode (v.ToString ());
			}
		}

		/// <summary>
		/// Save sources to a database table.
		/// </summary>
		/// <param name="sources">Source collection.</param>
		/// <param name="table">Database table.</param>
		private static void SaveSources(IEnumerable<Location> sources, string table)
		{
			foreach (var source in sources)
				SaveSource (source, table);
		}

		/// <summary>
		/// Save a single source to a database table.
		/// </summary>
		/// <param name="source">A source where to scan for files.</param>
		/// <param name="table">Database table.</param>
		private static void SaveSource(Location source, string table)
		{
			var data = CreateData (source);
			db.Insert (table, data);
		}

		/// <summary>
		/// Save playlists to a database table.
		/// </summary>
		/// <param name="playlists">Playlist collection.</param>
		/// <param name="table">Database table.</param>
		private static void SavePlaylists(IEnumerable<Playlist> playlists, string table)
		{
			foreach (var playlist in playlists)
				SavePlaylist (playlist, table);
		}

		/// <summary>
		/// Save a single playlist to a database table.
		/// </summary>
		/// <param name="playlist">A playlist.</param>
		/// <param name="table">Database table.</param>
		private static void SavePlaylist(Playlist playlist, string table)
		{
			var data = CreateData (playlist);
			db.Insert (table, data);
			var rowid = db.LastID (table);

			data.Clear ();
			SaveListConfiguration (playlist.ListConfig, "listConfigurations");
			data ["listConfig"] = DBEncode (db.LastID("listConfigurations"));
			db.Update (table, data, String.Format("rowid={0}", rowid));

			foreach (var t in playlist.Tracks)
			{
				data.Clear ();
				data ["path"] = DBEncode(t.Path);
				data ["playlist"] = DBEncode (rowid);
				db.Insert ("playlistTracks", data);
			}
		}

		/// <summary>
		/// Save buffer of listen submissions to a database table.
		/// </summary>
		/// <param name="buffer">Listen submission buffer.</param>
		/// <param name="table">Database table.</param>
		private static void SaveListenBuffer(Dictionary<string,Tuple<string,string>> buffer, string table)
		{
			foreach (var listen in buffer)
			{
				var data = new Dictionary<string,string> ();
				data ["url"] = DBEncode (listen.Key);
				data ["method"] = DBEncode (listen.Value.Item1);
				data ["track"] = DBEncode (listen.Value.Item2);
				db.Insert (table, data);
			}
		}

		#endregion

		#region Update in database

		/// <summary>
		/// Update an object in the database.
		/// </summary>
		/// <param name="table">Database table where the object is stored.</param>
		/// <param name="obj">Object to update.</param>
		/// <typeparam name="T">The type of the object.</typeparam>
		private static void Update<T>(string table, T obj)
		{
			if (obj == null)
				return;
			var rowid = GetID<T> (table, obj);
			if (rowid < 0)
				return;
			db.Update(table, CreateData(obj), String.Format ("rowid={0}", rowid));

			if (typeof(T) == typeof(ListConfig))
			{
				var config = obj as ListConfig;
				db.Delete ("listColumns", String.Format ("config={0}", rowid));
				SaveColumns (config.Columns, "listColumns", rowid);
			}
			else if (typeof(T) == typeof(Playlist))
			{
				var playlist = obj as Playlist;
				db.Delete ("playlistTracks", String.Format ("playlist={0}", rowid));

				var data = new Dictionary<string,string> ();
				foreach (var t in playlist.Tracks)
				{
					data.Clear ();
					data ["path"] = DBEncode(t.Path);
					data ["playlist"] = DBEncode (rowid);
					db.Insert ("playlistTracks", data);
				}
			}
			// TODO: keyboard shortcuts, equalizer?, plugin meta data
		}

		#endregion

		#region Remove from database

		/// <summary>
		/// Remove tracks from a database table.
		/// </summary>
		/// <param name="tracks">Track collection.</param>
		/// <param name="table">Database table.</param>
		private static void DeleteTracks(IEnumerable<Track> tracks, string table)
		{
			foreach (var track in tracks)
				DeleteTrack (track, table);
		}

		/// <summary>
		/// Remove a single track from a database table.
		/// </summary>
		/// <param name="track">Track.</param>
		/// <param name="table">Database table.</param>
		private static void DeleteTrack(Track track, string table)
		{
			db.Delete(table, String.Format("path='{0}'", track.Path));
		}

		/// <summary>
		/// Remove shortcut profiles from a database table.
		/// </summary>
		/// <param name="profiles">Shortcut profile collection.</param>
		/// <param name="table">Database table.</param>
		private static void DeleteShortcutProfiles(IEnumerable<KeyboardShortcutProfile> profiles, string table)
		{
			foreach (var profile in profiles)
				db.Delete (table, String.Format ("name={0}", DBEncode (profile.Name)));
		}

		/// <summary>
		/// Remove equalizer profiles from a database table.
		/// </summary>
		/// <param name="profiles">Equalizer profile collection.</param>
		/// <param name="table">Database table.</param>
		private static void DeleteEqualizerProfiles(IEnumerable<EqualizerProfile> profiles, string table)
		{
			foreach (var profile in profiles)
				db.Delete (table, String.Format ("name={0}", DBEncode (profile.Name)));
		}

		/// <summary>
		/// Remove playlists from a database table.
		/// </summary>
		/// <param name="playlists">Playlist collection.</param>
		/// <param name="table">Database table.</param>
		private static void DeletePlaylists(IEnumerable<Playlist> playlists, string table)
		{
			foreach (var playlist in playlists)
				db.Delete (table, String.Format ("name={0} and cloudID={1}", DBEncode (playlist.Name), DBEncode (playlist.ID)));
		}

		/// <summary>
		/// Remove sources from a database table.
		/// </summary>
		/// <param name="sources">Source collection.</param>
		/// <param name="table">Database table.</param>
		private static void DeleteSources(IEnumerable<Location> sources, string table)
		{
			foreach (var source in sources)
				db.Delete (table, String.Format ("data={0} and type={1}", DBEncode (source.Data), DBEncode (SourceTypeToString (source.Type))));
		}

		#endregion

		#region Select from database

		/// <summary>
		/// Get the specified object's rowid in a given table.
		/// </summary>
		/// <param name="table">Table.</param>
		/// <param name="obj">Object to look for.</param>
		/// <typeparam name="T">The type of the object.</typeparam>
		/// <returns>The rowid if found, otherwise -1.</returns>
		private static int GetID<T>(string table, T obj)
		{
			if (obj == null)
				return -1;

			DataTable result;
			if (typeof(T) == typeof(ListConfig)) {
				var config = obj as ListConfig;
				var configKey = "";
				if (config == fileListConfig)
					configKey = "fileListConfig";
				else if (config == queueListConfig)
					configKey = "queueListConfig";
				else if (config == historyListConfig)
					configKey = "historyListConfig";
				else if (config == radioListConfig)
					configKey = "radioListConfig";
				else if (config == SoundCloudListConfig)
					configKey = "soundCloudListConfig";
				else if (config == YouTubeListConfig)
					configKey = "youTubeListConfig";
				else if (config == JamendoListConfig)
					configKey = "jamendoListConfig";

				if (configKey != "") {
					result = db.Select (String.Format ("select value from config where key='{0}';", configKey));
					if (result.Rows.Count > 0)
						return Convert.ToInt32 (result.Rows [0] ["value"]);
				} else {
					foreach (var playlist in playlists) {
						if (config == playlist.ListConfig) {
							result = db.Select (String.Format ("select listConfig from playlists where name='{0}' and cloudID={1};", playlist.Name, playlist.ID));
							if (result.Rows.Count > 0)
								return Convert.ToInt32 (result.Rows [0] ["listConfig"]);
						}
					}
				}
			}
			else if (typeof(T) == typeof(Track))
			{
				var track = obj as Track;
				result = db.Select (String.Format ("select rowid from {0} where path='{1}';", table, track.Path));
				if (result.Rows.Count > 0)
					return Convert.ToInt32 (result.Rows [0] ["rowid"]);
			}
			else if (typeof(T) == typeof(EqualizerProfile))
			{
				var profile = obj as EqualizerProfile;
				result = db.Select (String.Format ("select rowid from {0} where name='{1}';", table, profile.Name));
				if (result.Rows.Count > 0)
					return Convert.ToInt32 (result.Rows [0] ["rowid"]);
			}
			else if (typeof(T) == typeof(KeyboardShortcutProfile))
			{
				var profile = obj as KeyboardShortcutProfile;
				result = db.Select (String.Format ("select rowid from {0} where name='{1}';", table, profile.Name));
				if (result.Rows.Count > 0)
					return Convert.ToInt32 (result.Rows [0] ["rowid"]);
			}
			else if (typeof(T) == typeof(Playlist))
			{
				var playlist = obj as Playlist;
				result = db.Select (String.Format ("select rowid from {0} where name='{1}' and cloudID = {2};", table, playlist.Name, playlist.ID));
				if (result.Rows.Count > 0)
					return Convert.ToInt32 (result.Rows [0] ["rowid"]);
			}
			else if (typeof(T) == typeof(Location))
			{
				var source = obj as Location;
				result = db.Select (String.Format ("select rowid from {0} where type='{1}' and data='{2}';", table, SourceTypeToString(source.Type), source.Data));
				if (result.Rows.Count > 0)
					return Convert.ToInt32 (result.Rows [0] ["rowid"]);
			}
			return -1;
		}

		#endregion

		#region Encode for database

		/// <summary>
		/// Create data for an SQL update or insert query.
		/// </summary>
		/// <returns>The data.</returns>
		/// <param name="obj">Object.</param>
		/// <typeparam name="T">The type of the object.</typeparam>
		private static Dictionary<string,string> CreateData<T>(T obj)
		{
			var data = new Dictionary<string,string> ();

			if (typeof(T) == typeof(Track))
			{
				var track = obj as Track;
				data ["album"] = DBEncode (track.Album);
				data ["artist"] = DBEncode (track.Artist);
				data ["artUrl"] = DBEncode (track.ArtURL);
				data ["bitrate"] = DBEncode(track.Bitrate);
				data ["channels"] = DBEncode(track.Channels);
				data ["codecs"] = DBEncode (track.Codecs);
				data ["genre"] = DBEncode (track.Genre);
				data ["lastPlayed"] = DBEncode(track.LastPlayed);
				data ["lastWrite"] = DBEncode(track.LastWrite);
				data ["length"] = DBEncode(track.Length);
				data ["number"] = DBEncode(track.Number);
				data ["originalArtUrl"] = DBEncode (track.OriginalArtURL);
				data ["userPlayCount"] = DBEncode(track.PlayCount);
				data ["processed"] = DBEncode(track.Processed);
				data ["sampleRate"] = DBEncode(track.SampleRate);
				data ["source"] = DBEncode (track.Source);
				data ["title"] = DBEncode (track.Title);
				data ["track"] = DBEncode(track.TrackNumber);
				data ["url"] = DBEncode (track.URL);
				data ["globalPlayCount"] = DBEncode(track.Views);
				data ["year"] = DBEncode(track.Year);
				data ["path"] = DBEncode(track.Path);
			}
			else if (typeof(T) == typeof(ListConfig))
			{
				var config = obj as ListConfig;
				data ["acceptDrop"] = DBEncode (config.AcceptFileDrops);
				data ["filter"] = DBEncode (config.Filter);
				data ["allowNumber"] = DBEncode (config.HasNumber);
				data ["horizontalOffset"] = DBEncode (config.HorizontalScrollOffset);
				data ["iconSize"] = DBEncode (config.IconSize);
				data ["canClickSort"] = DBEncode (config.IsClickSortable);
				data ["canDragSort"] = DBEncode (config.IsDragSortable);
				data ["showNumber"] = DBEncode (config.IsNumberVisible);
				data ["lockSortOnNumber"] = DBEncode (config.LockSortOnNumber);
				data ["mode"] = DBEncode (ViewModeToString (config.Mode));
				data ["numberPos"] = DBEncode (config.NumberIndex);
				data ["selection"] = DBEncode (String.Join (";",config.SelectedIndices));
				data ["sorting"] = DBEncode (String.Join (";",config.Sorts));
				data ["useIcons"] = DBEncode (config.UseIcons);
				data ["verticalOffset"] = DBEncode (config.VerticalScrollOffset);
				data ["verticalOffsetWithoutSearch"] = DBEncode (config.VerticalScrollOffsetWithoutSearch);
			}
			else if (typeof(T) == typeof(ListColumn))
			{
				var column = obj as ListColumn;
				data ["align"] = DBEncode (AlignmentToString (column.Alignment));
				data ["binding"] = DBEncode (column.Binding);
				data ["converter"] = DBEncode (column.Converter);
				data ["alwaysVisible"] = DBEncode (column.IsAlwaysVisible);
				data ["sortable"] = DBEncode (column.IsSortable);
				data ["visible"] = DBEncode (column.IsVisible);
				data ["name"] = DBEncode (column.Name);
				data ["sortOn"] = DBEncode (column.SortField);
				data ["text"] = DBEncode (column.Text);
				data ["width"] = DBEncode (column.Width);
			}
			else if (typeof(T) == typeof(KeyboardShortcutProfile))
			{
				var profile = obj as KeyboardShortcutProfile;
				data ["protected"] = DBEncode (profile.IsProtected);
				data ["name"] = DBEncode (profile.Name);
			}
			else if (typeof(T) == typeof(KeyboardShortcut))
			{
				var shortcut = obj as KeyboardShortcut;
				data ["category"] = DBEncode (shortcut.Category);
				data ["global"] = DBEncode (shortcut.IsGlobal);
				data ["keys"] = DBEncode (shortcut.Keys);
				data ["name"] = DBEncode (shortcut.Name);
			}
			else if (typeof(T) == typeof(EqualizerProfile))
			{
				var eqProfile = obj as EqualizerProfile;
				data ["echo"] = DBEncode (eqProfile.EchoLevel);
				data ["protected"] = DBEncode (eqProfile.IsProtected);
				data ["name"] = DBEncode (eqProfile.Name);
				data ["bands"] = DBEncode (String.Join(";",eqProfile.Levels));
			}
			else if (typeof(T) == typeof(Identity))
			{
				var identity = obj as Identity;
				data ["configuration"] = DBEncode (identity.ConfigurationID);
				data ["device"] = DBEncode (identity.DeviceID);
				data ["synchronize"] = DBEncode (identity.Synchronize);
				data ["synchronizeConfig"] = DBEncode (identity.SynchronizeConfig);
				data ["synchronizeFiles"] = DBEncode (identity.SynchronizeFiles);
				data ["synchronizePlaylists"] = DBEncode (identity.SynchronizePlaylists);
				data ["synchronizeQueue"] = DBEncode (identity.SynchronizeQueue);
				data ["user"] = DBEncode (identity.UserID);
			}
			else if (typeof(T) == typeof(Link))
			{
				var link = obj as Link;
				data ["canCreatePlaylist"] = DBEncode (link.CanCreatePlaylist);
				data ["canDonate"] = DBEncode (link.CanDonate);
				data ["canListen"] = DBEncode (link.CanListen);
				data ["canShare"] = DBEncode (link.CanShare);
				data ["connected"] = DBEncode (link.Connected);
				data ["connectUrl"] = DBEncode (link.ConnectURL);
				data ["doCreatePlaylist"] = DBEncode (link.DoCreatePlaylist);
				data ["doDonate"] = DBEncode (link.DoDonate);
				data ["doListen"] = DBEncode (link.DoListen);
				data ["doShare"] = DBEncode (link.DoShare);
				data ["error"] = DBEncode (link.Error);
				data ["cloudID"] = DBEncode (link.ID);
				data ["names"] = DBEncode (String.Join("\n", link.Names));
				data ["picture"] = DBEncode (link.Picture);
				data ["provider"] = DBEncode (link.Provider);
				data ["url"] = DBEncode (link.URL);
			}
			else if (typeof(T) == typeof(Metadata))
			{
				var pluginData = obj as Metadata;
				data ["enabled"] = DBEncode (pluginData.Enabled);
				data ["installed"] = DBEncode (pluginData.Installed);
				data ["plugin"] = DBEncode (pluginData.PluginID);
			}
			else if (typeof(T) == typeof(Stoffi.Plugins.Setting))
			{
				var setting = obj as Stoffi.Plugins.Setting;
				data ["id"] = DBEncode (setting.ID);
				data ["visible"] = DBEncode (setting.IsVisible);
				data ["max"] = DBEncode (setting.Maximum.ToString());
				data ["min"] = DBEncode (setting.Minimum.ToString());
				data ["type"] = DBEncode (setting.SerializedType);
				data ["value"] = DBEncode (setting.SerializedValue);
			}
			else if (typeof(T) == typeof(Location))
			{
				var source = obj as Location;
				data ["automatic"] = DBEncode (source.Automatic);
				data ["data"] = DBEncode (source.Data);
				data ["ignore"] = DBEncode (source.Ignore);
				data ["type"] = DBEncode(SourceTypeToString (source.Type));
			}
			else if (typeof(T) == typeof(Playlist))
			{
				var playlist = obj as Playlist;
				data ["filter"] = DBEncode (playlist.Filter);
				data ["cloudID"] = DBEncode (playlist.ID);
				data ["name"] = DBEncode (playlist.Name);
				data ["ownerCacheTime"] = DBEncode (playlist.OwnerCacheTime);
				data ["ownerID"] = DBEncode (playlist.OwnerID);
				data ["ownerName"] = DBEncode (playlist.OwnerName);
				data ["length"] = DBEncode (playlist.Time);
			}

			return data;
		}

		/// <summary>
		/// Returns a string safe for storing in the database.
		/// </summary>
		/// <returns>A string representation safe for database storage.</returns>
		/// <param name="x">The value to store.</param>
		private static string DBEncode(string x)
		{
			x = x ?? "";
			return String.Format("'{0}'", x.Replace("\'", "\'\'"));
		}

		/// <summary>
		/// Returns a string safe for storing in the database.
		/// </summary>
		/// <returns>A string representation safe for database storage.</returns>
		/// <param name="x">The value to store.</param>
		private static string DBEncode(bool x)
		{
			return x ? "1" : "0";
		}

		/// <summary>
		/// Returns a string safe for storing in the database.
		/// </summary>
		/// <returns>A string representation safe for database storage.</returns>
		/// <param name="x">The value to store.</param>
		private static string DBEncode(ulong x)
		{
			return x.ToString("0", CultureInfo.GetCultureInfo("en-US"));
		}

		/// <summary>
		/// Returns a string safe for storing in the database.
		/// </summary>
		/// <returns>A string representation safe for database storage.</returns>
		/// <param name="x">The value to store.</param>
		private static string DBEncode(long x)
		{
			return x.ToString("0", CultureInfo.GetCultureInfo("en-US"));
		}

		/// <summary>
		/// Returns a string safe for storing in the database.
		/// </summary>
		/// <returns>A string representation safe for database storage.</returns>
		/// <param name="x">The value to store.</param>
		private static string DBEncode(uint x)
		{
			return x.ToString("0", CultureInfo.GetCultureInfo("en-US"));
		}

		/// <summary>
		/// Returns a string safe for storing in the database.
		/// </summary>
		/// <returns>A string representation safe for database storage.</returns>
		/// <param name="x">The value to store.</param>
		private static string DBEncode(int x)
		{
			return x.ToString("0", CultureInfo.GetCultureInfo("en-US"));
		}

		/// <summary>
		/// Returns a string safe for storing in the database.
		/// </summary>
		/// <returns>A string representation safe for database storage.</returns>
		/// <param name="x">The value to store.</param>
		private static string DBEncode(double x)
		{
			return x.ToString("0.0000", CultureInfo.GetCultureInfo("en-US"));
		}

		/// <summary>
		/// Returns a string safe for storing in the database.
		/// </summary>
		/// <returns>A string representation safe for database storage.</returns>
		/// <param name="x">The value to store.</param>
		private static string DBEncode(DateTime x)
		{
			long l = 0;
			try
			{
				if (x.Year > 1600)
					l = x.ToFileTimeUtc ();
			}
			catch {
			}
			return DBEncode (l);
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when the property of a ListViewConfig changes.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private static void Object_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var t = new Thread (delegate() {
				lock (saveToDatabaseLock)
				{
					propertyChangedBuffer.Add (new Tuple<PropertyChangedBase, PropertyChangedEventArgs> (sender as PropertyChangedBase, e));
				}
				if (propertyChangedTimer != null)
					propertyChangedTimer.Dispose ();
				propertyChangedTimer = new Timer (HandlePropertyChanged, null, 5000, Timeout.Infinite);
			});
			t.Name = "Reflect changed property in database";
			t.Priority = ThreadPriority.BelowNormal;
			t.Start ();
		}

		/// <summary>
		/// Handles a change of a property in a list view config.
		/// 
		/// Will iterate the propertyChangedBuffer and update the database
		/// according to those changes.
		/// </summary>
		/// <param name="state">State (not used).</param>
		private static void HandlePropertyChanged(object state)
		{
			lock (saveToDatabaseLock) {
				foreach (var item in propertyChangedBuffer) {
					if (item.Item1 == null || item.Item2 == null)
						continue;

					if (item.Item1 is Track)
					{
						var track = item.Item1 as Track;
						var type = track.Type;
						if (type == TrackType.File)
							Update<Track> ("files", item.Item1 as Track);
						else if (type == TrackType.WebRadio)
							Update<Track> ("radio", item.Item1 as Track);
					}
					else if (item.Item1 is Location)
					{
						Update<Location> ("sources", item.Item1 as Location);
					}
					else if (item.Item1 is ListConfig)
					{
						Update<ListConfig> ("listConfigurations", item.Item1 as ListConfig);
					}
					else if (item.Item1 is KeyboardShortcutProfile)
					{
						Update<KeyboardShortcutProfile> ("shortcutProfiles", item.Item1 as KeyboardShortcutProfile);
					}
					else if (item.Item1 is EqualizerProfile)
					{
						Update<EqualizerProfile> ("equalizerProfiles", item.Item1 as EqualizerProfile);
					}
					else if (item.Item1 is Playlist)
					{
						Update<Playlist> ("playlists", item.Item1 as Playlist);
					}
				}
				propertyChangedBuffer.Clear ();
			}
		}

		/// <summary>
		/// Invoked when a collection changes.
		/// Will reflect the change in the database.
		/// </summary>
		/// <param name="sender">Sender of the event.</param>
		/// <param name="e">The event data.</param>
		private static void ObservableCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			Func<object,int> method = x => {
				lock (saveToDatabaseLock)
				{
					collectionChangedBuffer.Add (new Tuple<IEnumerable<object>, NotifyCollectionChangedEventArgs> (sender as IEnumerable<object>, e));
				}
				if (collectionChangedTimer != null)
					collectionChangedTimer.Dispose ();
				collectionChangedTimer = new Timer (HandleCollectionChanged, null, 5000, Timeout.Infinite);
				return -1;
			};

			if (sender is ObservableCollection<Playlist>) {
				var t = new Thread (delegate() {
					method (null);
				});
				t.Name = "Reflect changed playlist collection in database";
				t.Priority = ThreadPriority.BelowNormal;
				t.Start ();
			} else
				method (null);
		}

		/// <summary>
		/// Handles a change in a track collection.
		/// 
		/// Will iterate the collectionChangedBuffer and update the database
		/// according to those changes.
		/// </summary>
		/// <param name="state">State (not used).</param>
		private static void HandleCollectionChanged(object state)
		{
			lock (saveToDatabaseLock) {
				foreach (var item in collectionChangedBuffer) {
					if (item.Item1 == null || item.Item2 == null)
						continue;

					var collection = item.Item1;

					var newItems = new List<object> ();
					var oldItems = new List<object> ();
					if (item.Item2.NewItems != null)
						foreach (object t in item.Item2.NewItems)
							newItems.Add (t);
					if (item.Item2.OldItems != null)
						foreach (object t in item.Item2.OldItems)
							oldItems.Add (t);

					if (collection == files) {
						switch (item.Item2.Action) {
						case NotifyCollectionChangedAction.Add:
							SaveTracks (from i in newItems select i as Track, "files");
							break;

						case NotifyCollectionChangedAction.Remove:
							DeleteTracks (from i in oldItems select i as Track, "files");
							break;

						case NotifyCollectionChangedAction.Replace:
							DeleteTracks (from i in oldItems select i as Track, "files");
							SaveTracks (from i in newItems select i as Track, "files");
							break;

						case NotifyCollectionChangedAction.Reset:
							db.Delete ("files");
							SaveTracks (files, "files");
							break;
						}
					} else if (collection == radio) {
						switch (item.Item2.Action) {
						case NotifyCollectionChangedAction.Add:
							SaveTracks (from i in newItems select i as Track, "radio");
							break;

						case NotifyCollectionChangedAction.Remove:
							DeleteTracks (from i in oldItems select i as Track, "radio");
							break;

						case NotifyCollectionChangedAction.Replace:
							DeleteTracks (from i in oldItems select i as Track, "radio");
							SaveTracks (from i in newItems select i as Track, "radio");
							break;

						case NotifyCollectionChangedAction.Reset:
							db.Delete ("radio");
							SaveTracks (radio, "radio");
							break;
						}
						foreach (var x in radio)
						{
						x.PropertyChanged -= Object_PropertyChanged;
						x.PropertyChanged += Object_PropertyChanged;
						}
					} else if (collection == queue) {
						db.Delete ("queue");
						SaveTrackReferences (queue, "queue", new string[] { "number" });
					} else if (collection == history) {
						switch (item.Item2.Action) {
						case NotifyCollectionChangedAction.Add:
							SaveTrackReferences (from i in newItems select i as Track, "history", new string[] { "lastPlayed" });
							break;

						case NotifyCollectionChangedAction.Remove:
							DeleteTracks (from i in oldItems select i as Track, "history");
							break;

						case NotifyCollectionChangedAction.Replace:
							DeleteTracks (from i in oldItems select i as Track, "history");
							SaveTrackReferences (from i in newItems select i as Track, "history", new string[] { "lastPlayed" });
							break;

						case NotifyCollectionChangedAction.Reset:
							db.Delete ("history");
							SaveTrackReferences (history, "history", new string[] { "lastPlayed" });
							break;
						}
					} else if (collection == scanSources) {
						switch (item.Item2.Action) {
						case NotifyCollectionChangedAction.Add:
							SaveSources (from i in newItems select i as Location, "sources");
							break;

						case NotifyCollectionChangedAction.Remove:
							DeleteSources (from i in oldItems select i as Location, "sources");
							break;

						case NotifyCollectionChangedAction.Replace:
							DeleteSources (from i in oldItems select i as Location, "sources");
							SaveSources (from i in newItems select i as Location, "sources");
							break;

						case NotifyCollectionChangedAction.Reset:
							db.Delete ("sources");
							SaveSources (scanSources, "sources");
							break;
						}
						foreach (var x in scanSources)
						{
						x.PropertyChanged -= Object_PropertyChanged;
						x.PropertyChanged += Object_PropertyChanged;
						}
					} else if (collection == shortcutProfiles) {
						switch (item.Item2.Action) {
						case NotifyCollectionChangedAction.Add:
							SaveShortcutProfiles (from i in newItems select i as KeyboardShortcutProfile, "shortcutProfiles");
							break;

						case NotifyCollectionChangedAction.Remove:
							DeleteShortcutProfiles (from i in oldItems select i as KeyboardShortcutProfile, "shortcutProfiles");
							break;

						case NotifyCollectionChangedAction.Replace:
							DeleteShortcutProfiles (from i in oldItems select i as KeyboardShortcutProfile, "shortcutProfiles");
							SaveShortcutProfiles (from i in newItems select i as KeyboardShortcutProfile, "shortcutProfiles");
							break;

						case NotifyCollectionChangedAction.Reset:
							db.Delete ("shortcutProfiles");
							SaveShortcutProfiles (shortcutProfiles, "shortcutProfiles");
							break;
						}
						foreach (var x in shortcutProfiles)
						{
						x.PropertyChanged -= Object_PropertyChanged;
						x.PropertyChanged += Object_PropertyChanged;
						}
					} else if (collection == equalizerProfiles) {
						switch (item.Item2.Action) {
						case NotifyCollectionChangedAction.Add:
							SaveEqualizerProfiles (from i in newItems select i as EqualizerProfile, "equalizerProfiles");
							break;

						case NotifyCollectionChangedAction.Remove:
							DeleteEqualizerProfiles (from i in oldItems select i as EqualizerProfile, "equalizerProfiles");
							break;

						case NotifyCollectionChangedAction.Replace:
							DeleteEqualizerProfiles (from i in oldItems select i as EqualizerProfile, "equalizerProfiles");
							SaveEqualizerProfiles (from i in newItems select i as EqualizerProfile, "equalizerProfiles");
							break;

						case NotifyCollectionChangedAction.Reset:
							db.Delete ("equalizerProfiles");
							SaveEqualizerProfiles (equalizerProfiles, "equalizerProfiles");
							break;
						}
						foreach (var x in EqualizerProfiles)
						{
						x.PropertyChanged -= Object_PropertyChanged;
						x.PropertyChanged += Object_PropertyChanged;
						}
					} else if (collection == playlists) {
						switch (item.Item2.Action) {
						case NotifyCollectionChangedAction.Add:
							SavePlaylists (from i in newItems select i as Playlist, "playlists");
							break;

						case NotifyCollectionChangedAction.Remove:
							DeletePlaylists (from i in oldItems select i as Playlist, "playlists");
							break;

						case NotifyCollectionChangedAction.Replace:
							DeletePlaylists (from i in oldItems select i as Playlist, "playlists");
							SavePlaylists (from i in newItems select i as Playlist, "playlists");
							break;

						case NotifyCollectionChangedAction.Reset:
							db.Delete ("playlists");
							SavePlaylists (playlists, "playlists");
							break;
						}
						foreach (var x in playlists)
						{
						x.PropertyChanged -= Object_PropertyChanged;
						x.PropertyChanged += Object_PropertyChanged;
						}
					}
				}
				collectionChangedBuffer.Clear ();
			}
		}

		#endregion

		#endregion

		#endregion

	}
}

