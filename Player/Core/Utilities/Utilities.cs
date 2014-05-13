/***
 * Utilities.cs
 * 
 * The swiss armyknife of Stoffi containing, for example, the
 * log function.
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
 ***/

#if (!__MonoCS__)
#define Windows
#endif

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Xml.Serialization;
using Newtonsoft.Json.Linq;
#if (Windows)
using Tomers.WPF.Localization;
#endif

using Stoffi.Core.Media;
using Stoffi.Core.Settings;
using Stoffi.Core.Services;

namespace Stoffi.Core
{
	/// <summary>
	/// This is the utility class containing all helper methods
	/// </summary>
	public static partial class U
	{
		#region Members

		/// <summary>
		/// Contains the time when the class was first initialized
		/// </summary>
		public static DateTime initTime;

		private static Dictionary<string,object> locks = new Dictionary<string, object>();
		private static object logLock = new object();

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the path to the logfile
		/// </summary>
		public static string LogFile { get; set; }

		/// <summary>
		/// Gets or sets the minimum level of messages to print/write
		/// </summary>
		public static LogLevel Level { get; set; }

		/// <summary>
		/// Gets or sets whether the main window should listen for keyboard shortcuts
		/// </summary>
		public static bool ListenForShortcut { get; set; }

		/// <summary>
		/// Gets the directory path of from where the executable is running.
		/// </summary>
		public static string BasePath
		{
			get
			{
				return Path.GetDirectoryName(FullPath);
			}
		}

		/// <summary>
		/// Gets the GUI thread's context.
		/// </summary>
		/// <value>The GUI context.</value>
		public static SynchronizationContext GUIContext { get; set; }

		/// <summary>
		/// Gets or sets the full path of the executable that is running.
		/// </summary>
		public static string FullPath { get; set; }

		/// <summary>
		/// Gets or sets the indicator that the threads will listen to in order to know if they
		/// should die gracefully. To be used when the application wants to close.
		/// </summary>
		public static bool IsClosing { get; set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Creates a utility class.
		/// </summary>
		static U()
		{
			Check ();
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Initialize the utility class.
		/// </summary>
		/// <param name="fullPath">The absolute path to the executing assembly.</param>
		/// <param name="ctx">The synchronization context of the GUI thread</param>
		/// <param name="logLevel">The minimum level of log messages to write</param>
		public static void Initialize(string fullPath, SynchronizationContext ctx, LogLevel logLevel = LogLevel.Warning)
		{
			FullPath = fullPath;
			GUIContext = ctx;
			LogFile = Path.Combine(BasePath, "Stoffi.log");
			Level = logLevel;
			initTime = DateTime.Now;
			IsClosing = false;
		}

		/// <summary>
		/// Logs a message to file and/or console.
		/// </summary>
		/// <param name="level">The level of the message (if this is lower than Level the message will be ignored)</param>
		/// <param name="caller">The caller of the message</param>
		/// <param name="message">The message</param>
		public static void L(LogLevel level, string caller, string message)
		{
			if (LevelToInt(level) < LevelToInt(Level)) return;

			TimeSpan ts = (DateTime.Now - initTime);
			string logLine = String.Format("{0} {1}:{2:00}:{3:00}.{4:000} ({5:G}) [{6}] {7}: {8}",
				ts.Days, ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds, DateTime.Now,
				LevelToString(level), // #7
				caller.ToUpper(),
				message);

			if (Level == LogLevel.Debug)
				Console.WriteLine(logLine);

#if (!DEBUG)
			lock (logLock)
			{
				System.IO.StreamWriter sw = null;
				try
				{
					sw = System.IO.File.AppendText(LogFile);
					sw.WriteLine(logLine);
				}
				catch (Exception e)
				{
					Console.WriteLine("ERROR: Could not write to logfile: " + e.Message);
				}
				if (sw != null)
					sw.Close();
			}
#endif
		}

		/// <summary>
		/// Logs a HttpWebResponse to file and/or console.
		/// </summary>
		/// <param name="level">The level of the message (if this is lower than Level the message will be ignored)</param>
		/// <param name="caller">The caller of the message</param>
		/// <param name="response">The HttpWebResponse object.</param>
		public static void L(LogLevel level, string caller, System.Net.HttpWebResponse response)
		{
			if (response == null)
				U.L(level, caller, "Response was empty.");
			else
			{
				U.L(level, caller, String.Format("Content Encoding: {0}", response.ContentEncoding));
				U.L(level, caller, String.Format("Content Type: {0}", response.ContentType));
				U.L(level, caller, String.Format("Status Description: {0}", response.StatusDescription));
				StreamReader sr = new StreamReader(response.GetResponseStream());
				string str;
				while ((str = sr.ReadLine()) != null)
					U.L(level, caller, str);
				U.L(level, caller, String.Format("-- End of response. Total bytes: {0} --", response.ContentLength));
			}
		}

		/// <summary>
		/// Cleans a string to only contain alphanumerical and -_ characters.
		/// </summary>
		/// <param name="str">The string to clean</param>
		/// <returns>The string with all non-alphanumerical characters except whitespace and _ changed to -</returns>
		public static string C(string str)
		{
			if (String.IsNullOrWhiteSpace(str)) return "";
			Regex rgx = new Regex(@"[^\s\w_]");
			return rgx.Replace(str, "-");
		}

		/// <summary>
		/// Cleans a JSON string to only contain alphanumerical and -_ characters.
		/// </summary>
		/// <param name="json">The JSON object to clean</param>
		/// <returns>The JSON string with all non-alphanumerical characters except whitespace and _ changed to -</returns>
		public static string C(JToken json)
		{
			switch (json.Type)
			{
				case JTokenType.Integer:
				case JTokenType.Float:
				case JTokenType.String:
					return C(json.ToString());

				default:
					return "Unknown";
			}
		}

		/// <summary>
		/// Looks for a track with a specific path in a collection
		/// </summary>
		/// <param name="collection">A collection of tracks</param>
		/// <param name="path">A path to look for</param>
		/// <returns>True if any track has a path with <paramref name="path"/> as prefix, otherwise false.</returns>
		public static bool ContainsPath(IEnumerable<Track> collection, string path)
		{
			try
			{
				for (int i = 0; i < collection.Count(); i++)
				{
					if (collection.ElementAt(i).Path.StartsWith(path))
						return true;
				}
			}
			catch { }
			return false;
		}

		/// <summary>
		/// Looks for all tracks with a specific path in any collection.
		/// </summary>
		/// <param name="path">A path to look for</param>
		/// <param name="includeHistory">Whether or not to also look in the history collection</param>
		/// <returns>Any track that has a path with <paramref name="path"/> as prefix</returns>
		public static List<Track> GetTracks(string path, bool includeHistory = false)
		{
			var ret = new List<Track>();
			ret.Concat(GetTracks(Settings.Manager.FileTracks, path));
			ret.Concat(GetTracks(Settings.Manager.QueueTracks, path));
			ret.Concat(GetTracks(Settings.Manager.RadioTracks, path));
			foreach (var p in Settings.Manager.Playlists)
				ret.Concat(GetTracks(p.Tracks, path));
			if (includeHistory)
				ret.Concat(GetTracks(Settings.Manager.HistoryTracks, path));
			return ret;
		}

		/// <summary>
		/// Gets a lock with on-the-fly creation if the lock doesn't already exist.
		/// </summary>
		/// <returns>The lock.</returns>
		/// <param name="name">Name of the lock.</param>
		public static object GetLock(string name)
		{
			if (!locks.ContainsKey(name))
				locks.Add(name, new object());
			return locks[name];
		}

		/// <summary>
		/// Looks for all tracks with a specific path in a collection
		/// </summary>
		/// <param name="collection">A collection of tracks</param>
		/// <param name="path">A path to look for</param>
		/// <returns>Any track that has a path with <paramref name="path"/> as prefix</returns>
		public static List<Track> GetTracks(IEnumerable<Track> collection, string path)
		{
			var ret = new List<Track>();
			try
			{
				for (int i = 0; i < collection.Count(); i++)
				{
					var t = collection.ElementAt(i);
					if (t.Path.StartsWith(path))
						ret.Add(t);
				}
			}
			catch { }
			return ret;
		}

		/// <summary>
		/// Compares two strings and consider them equal if they are both null, empty or whitespace.
		/// </summary>
		/// <returns><c>true</c>, if strings are equal, <c>false</c> otherwise.</returns>
		/// <param name="str1">Str1.</param>
		/// <param name="str2">Str2.</param>
		public static bool Equal(string str1, string str2)
		{
			return ((String.IsNullOrWhiteSpace (str1) && String.IsNullOrWhiteSpace (str2)) || str1 == str2);
		}

		/// <summary>
		/// Remove all tracks with a given path from all collections.
		/// </summary>
		/// <param name="path">Path.</param>
		/// <param name="includeHistory">If set to <c>true</c> the track will also be removed from history.</param>
		public static void RemovePath(string path, bool includeHistory = false)
		{
			if (String.IsNullOrWhiteSpace (path))
				return;
			RemovePath (Settings.Manager.FileTracks, path);
			RemovePath (Settings.Manager.QueueTracks, path);
			RemovePath (Settings.Manager.RadioTracks, path);
			foreach (var p in Settings.Manager.Playlists)
				RemovePath (p.Tracks, path);
			if (includeHistory)
				RemovePath (Settings.Manager.HistoryTracks, path);
		}

		/// <summary>
		/// Remove all tracks from a collection with a given path.
		/// </summary>
		/// <param name="tracks">Tracks.</param>
		/// <param name="path">Path.</param>
		public static void RemovePath(ObservableCollection<Track> tracks, string path)
		{
			if (String.IsNullOrWhiteSpace (path))
				return;
			for (int i=0; i < tracks.Count; i++)
				if (tracks [i].Path == path)
					tracks.RemoveAt (i--);
		}
		
		/// <summary>
		/// Checks if a track matches a given query string.
		/// </summary>
		/// <returns><c>true</c>, if the track match, <c>false</c> otherwise.</returns>
		/// <param name="item">The track to check.</param>
		/// <param name="needles">The search query.</param>
		public static bool TrackMatchesQuery(object item, string needles)
		{
			if (needles == null || needles == "") return true;

			Track track = (Track)item;

			String artist = track.Artist == null ? "" : track.Artist.ToLower();
			String album = track.Album == null ? "" : track.Album.ToLower();
			String title = track.Title == null ? "" : track.Title.ToLower();
			String genre = track.Genre == null ? "" : track.Genre.ToLower();
			String year = track.Year.ToString().ToLower();
			String path = track.Path == null ? "" : track.Path.ToLower();

			foreach (String needle in needles.ToLower().Split(' '))
			{
				if (!artist.Contains(needle) &&
				    !album.Contains(needle) &&
				    !title.Contains(needle) &&
				    !genre.Contains(needle) &&
				    !year.Contains(needle) &&
				    !path.Contains(needle))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Calculates statistics on a collection of tracks.
		/// </summary>
		/// <returns>Four double values: average length (seconds), total length, average size (bytes), total size.</returns>
		/// <param name="tracks">Track collection.</param>
		public static double[] CalculateCollectionStatistics(IEnumerable<Track> tracks)
		{
			double avgLength = 0;
			double totLength = 0;
			double avgSize = 0;
			double totSize = 0;
			int numSize = 0;
			if (tracks != null)
			{
				for (int i=0; i < tracks.Count(); i++)
				{
					Track t = tracks.ElementAt<Track>(i);
					totLength += t.Length;
					try
					{
						if (t.Type == TrackType.File)
						{
							FileInfo fi = new FileInfo(t.Path);
							totSize += fi.Length;
							numSize++;
						}
					}
					catch { }
				}
				if (tracks.Count() > 0)
					avgLength = totLength / tracks.Count();
				if (numSize > 0)
					avgSize = totSize / numSize;
			}

			return new double[] { avgLength, totLength, avgSize, totSize };
		}

		/// <summary>
		/// Turns a size in bytes into a human readable string
		/// </summary>
		/// <param name="size">The size</param>
		/// <param name="binary">If true then report binary size, otherwise report decimal.</param>
		/// <returns>A localized string describing the size</returns>
		public static string HumanSize(long size, bool binary=true)
		{
			var b = binary ? 2 : 10;
			var exp = new int[] { 10, 20, 30, 40 };
			if (!binary)
				exp = new int[] { 3, 6, 9, 12 };

			if (size > Math.Pow(b,exp[3]))
				return String.Format(U.T("SizeTb", "Text", "{0} TB"), Math.Round((double)(size / Math.Pow(b, exp[3])), 2));

			else if (size > Math.Pow(b, exp[2]))
				return String.Format(U.T("SizeGb", "Text", "{0} GB"), Math.Round((double)(size / Math.Pow(b, exp[2])), 2));

			else if (size > Math.Pow(b, exp[1]))
				return String.Format(U.T("SizeMb", "Text", "{0} MB"), Math.Round((double)(size / Math.Pow(b, exp[1])), 2));

			else if (size > Math.Pow(b, exp[0]))
				return String.Format(U.T("SizeKb", "Text", "{0} KB"), Math.Round((double)(size / Math.Pow(b, exp[0])), 2));

			else if (size == 1)
				return String.Format(U.T("SizeByte", "Plural", "{0} byte"), size);

			else
				return String.Format(U.T("SizeByte", "Text", "{0} bytes"), size);
		}

		/// <summary>
		/// Removes characters that are not valid for an XML file.
		/// </summary>
		/// <param name="str">The string to be parsed</param>
		/// <returns>A copy of the string <paramref name="str"/> but with all invalid characters removed</returns>
		public static string CleanXMLString(string str)
		{
			if (str == null) return null;

			string r = "";
			foreach (char c in str)
			{
				int n = (int)c;
				if ((9 <= n && n <= 10) ||
					(n == 13) ||
					(32 <= n && n <= 55295) ||
					(57344 <= n && n <= 65533) ||
					(65536 <= n && n <= 1114111))
				{
					r += c;
				}
			}
			return r;
		}

		/// <summary>
		/// Converts back an escaped string passed via Rails CGI.escapeHTML method.
		/// </summary>
		/// <param name="str">The escaped string</param>
		/// <returns>An unescaped version of str</returns>
		public static string UnescapeHTML(string str)
		{
			if (str == null) return "";
			return WebUtility.HtmlDecode(str);
		}

		/// <summary>
		/// Converts a string so it fits inside a JSON object.
		/// </summary>
		/// <param name="str">The string to escape</param>
		/// <returns>An version of the string that can be used inside a JSON object</returns>
		public static string EscapeJSON(string str)
		{
			if (str == null) return "";
			str = WebUtility.HtmlEncode(str);

			// UTF characters (Mandarin for example) is not converted by HtmlEncode
			// so we do that manually
			string asciiString = "";
			foreach (var c in str)
			{
				if ((int)c > 255)
					asciiString += String.Format("&#{0};", (int)c);
				else
					asciiString += c;
			}
			return asciiString;
		}

		/// <summary>
		/// Converts a double so it fits inside a JSON object.
		/// </summary>
		/// <param name="d">The double to escape</param>
		/// <returns>An version of the double that can be used inside a JSON object</returns>
		public static string EscapeJSON(Double d)
		{
			return d.ToString("0.0000", CultureInfo.GetCultureInfo("en-US"));
		}

		/// <summary>
		/// Converts an integer so it fits inside a JSON object.
		/// </summary>
		/// <param name="d">The integer to escape</param>
		/// <returns>An version of the integer that can be used inside a JSON object</returns>
		public static string EscapeJSON(int d)
		{
			return d.ToString("0", CultureInfo.GetCultureInfo("en-US"));
		}

		/// <summary>
		/// Converts an integer so it fits inside a JSON object.
		/// </summary>
		/// <param name="d">The integer to escape</param>
		/// <returns>An version of the integer that can be used inside a JSON object</returns>
		public static string EscapeJSON(uint d)
		{
			return d.ToString("0", CultureInfo.GetCultureInfo("en-US"));
		}

		/// <summary>
		/// Converts an integer so it fits inside a JSON object.
		/// </summary>
		/// <param name="d">The integer to escape</param>
		/// <returns>An version of the integer that can be used inside a JSON object</returns>
		public static string EscapeJSON(long d)
		{
			return d.ToString("0", CultureInfo.GetCultureInfo("en-US"));
		}

		/// <summary>
		/// Converts an integer so it fits inside a JSON object.
		/// </summary>
		/// <param name="d">The integer to escape</param>
		/// <returns>An version of the integer that can be used inside a JSON object</returns>
		public static string EscapeJSON(ulong d)
		{
			return d.ToString("0", CultureInfo.GetCultureInfo("en-US"));
		}

		/// <summary>
		/// Parses HTTP query parameters
		/// </summary>
		/// <param name="query">The HTTP query</param>
		/// <returns>A dictionary with key-value pairs</returns>
		public static Dictionary<string, string> GetParams(string query)
		{
			string[] parameters = query.Split('&');
			Dictionary<string, string> d = new Dictionary<string, string>();
			foreach (string parameter in parameters)
			{
				string[] p = parameter.Split(new char[] { '=' }, 2);
				d.Add(p[0], p[1]);
			}
			return d;
		}

		/// <summary>
		/// Constructs a query string from a JSON object.
		/// </summary>
		/// <param name="key">The key of the parameter</param>
		/// <param name="value">The value of the parameter</param>
		/// <returns>A query string in HTTP format with encoded values</returns>
		public static string CreateQuery(string key, JToken value)
		{
			List<string> ret = new List<string>();
			switch (value.Type)
			{
				case JTokenType.String:
				case JTokenType.Integer:
				case JTokenType.Float:
				case JTokenType.Boolean:
					ret.Add(CreateParam(key, value.ToString(), ""));
					break;

				case JTokenType.Date:
					ret.Add(CreateParam(key, value.ToString(), "")); // TODO: format and make utc
					break;

				case JTokenType.Object:
					JObject o = value as JObject;
					if (o != null)
						foreach (JProperty prop in o.Properties())
							ret.Add(CreateQuery(String.Format("{0}[{1}]", key, prop.Name), o[prop.Name]));
					break;

				case JTokenType.Array:
					JArray a = value as JArray;
					if (a != null)
						for (int i=0; i < a.Count; i++)
							ret.Add(CreateQuery(String.Format("{0}[{1}]", key, i), a[i]));
					break;
			}
			for (int i = 0; i < ret.Count; i++)
				if (String.IsNullOrWhiteSpace(ret[i]))
					ret.RemoveAt(i--);
			return String.Join("&", ret);
		}

		/// <summary>
		/// Retrieves the query from a HTTP URL.
		/// Will clean up multiple question marks.
		/// </summary>
		/// <param name="url">The HTTP URL</param>
		/// <returns>A query string without any question marks</returns>
		public static string GetQuery(string url)
		{
			string[] tokens = url.Split('?');
			string ret = "";
			for (int i = 1; i < tokens.Count(); i++)
			{
				ret += "&" + tokens[i];
			}
			return ret.Substring(1);
		}

		/// <summary>
		/// Creates a HTTP parameter string from a key and a value.
		/// If value is null then it will return null instead of an
		/// empty assignment.
		/// All values and parameters will be encoded
		/// </summary>
		/// <param name="key">The parameter name</param>
		/// <param name="value">The value of the parameter</param>
		/// <param name="prefix">A prefix to be set on all parameter names</param>
		/// <returns>key=value if value is not null, otherwise null</returns>
		public static string CreateParam(string key, string value, string prefix = "")
		{
			if (String.IsNullOrWhiteSpace(key) || String.IsNullOrWhiteSpace(value))
				return null;
			else
			{
				if (String.IsNullOrWhiteSpace(prefix))
					return String.Format("{0}={1}",
						key,
						OAuth.Manager.UrlEncode(value));
				else
					return String.Format("{0}[{1}]={2}",
						prefix,
						OAuth.Manager.UrlEncode(key),
						OAuth.Manager.UrlEncode(value));
			}
		}

		/// <summary>
		/// Creates a HTTP parameter string from a key and a value.
		/// If value is null then it will return null instead of an
		/// empty assignment.
		/// All values and parameters will be encoded
		/// </summary>
		/// <param name="key">The parameter name</param>
		/// <param name="value">The value of the parameter</param>
		/// <param name="prefix">A prefix to be set on all parameter names</param>
		/// <returns>key=value if value is not null, otherwise null</returns>
		public static string CreateParam(string key, bool value, string prefix = "")
		{
			return CreateParam(key, value.ToString().ToLower(), prefix);
		}

		/// <summary>
		/// Creates a HTTP parameter string from a key and a value.
		/// If value is null then it will return null instead of an
		/// empty assignment.
		/// All values and parameters will be encoded
		/// </summary>
		/// <param name="key">The parameter name</param>
		/// <param name="value">The value of the parameter</param>
		/// <param name="prefix">A prefix to be set on all parameter names</param>
		/// <returns>key=value if value is not null, otherwise null</returns>
		public static string CreateParam(string key, int value, string prefix = "")
		{
			return CreateParam(key, value.ToString(), prefix);
		}

		/// <summary>
		/// Creates a HTTP parameter string from a key and a value.
		/// If value is null then it will return null instead of an
		/// empty assignment.
		/// All values and parameters will be encoded
		/// </summary>
		/// <param name="key">The parameter name</param>
		/// <param name="value">The value of the parameter</param>
		/// <param name="prefix">A prefix to be set on all parameter names</param>
		/// <returns>key=value if value is not null, otherwise null</returns>
		public static string CreateParam(string key, uint value, string prefix = "")
		{
			return CreateParam(key, value.ToString(), prefix);
		}

		/// <summary>
		/// Creates a HTTP parameter string from a key and a value.
		/// If value is null then it will return null instead of an
		/// empty assignment.
		/// All values and parameters will be encoded
		/// </summary>
		/// <param name="key">The parameter name</param>
		/// <param name="value">The value of the parameter</param>
		/// <param name="prefix">A prefix to be set on all parameter names</param>
		/// <returns>key=value if value is not null, otherwise null</returns>
		public static string CreateParam(string key, double value, string prefix)
		{
			return CreateParam(key, Convert.ToString(value), prefix);
		}

		/// <summary>
		/// Turns the first letter into uppercase and the rest into lowercase.
		/// </summary>
		/// <param name="str">The string to modify.</param>
		/// <returns>The string str with its first letter in uppercase and the rest in lowercase.</returns>
		public static String Capitalize(String str)
		{
			if (String.IsNullOrWhiteSpace(str))
				return "";
			str = str.ToLower();
			char[] a = str.ToCharArray();
			a[0] = Char.ToUpper(a[0]);
			return new String(a);
		}

		/// <summary>
		/// Capitalizes the words of a string.
		/// </summary>
		/// <param name="str">The string to titleize</param>
		/// <param name="onlyFirst">If true then only the first word will be capitalized</param>
		/// <param name="everyWord">If true then all words will be capitalized, if false then articles, prepositions and conjunctions will be lowercased</param>
		/// <returns></returns>
		public static String Titleize(String str, bool onlyFirst = false, bool everyWord = false)
		{
			if (String.IsNullOrWhiteSpace(str))
				return "";
			string[] toLower = { "a", "an", "the", "to", "at", "in", "with", "and", "but", "or" ,"of"};
			if (!onlyFirst)
			{
				string[] r = str.Split(' ');
				for (int i = 0; i < r.Count(); i++)
				{
					if (everyWord || !toLower.Contains<string>(r[i].ToLower()))
						r[i] = Capitalize(r[i]);
					else
						r[i] = r[i].ToLower();
				}
				return String.Join(" ", r);
			}

			return str[0].ToString().ToUpper() + str.Substring(1).ToLower();
		}

		/// <summary>
		/// Turns a timespan into a string in the format:
		/// X days, X hours, X minutes, X seconds
		/// </summary>
		/// <param name="ts">The timespan to turn into a string</param>
		/// <returns>The timespan printed out to a string</returns>
		public static String TimeSpanToLongString(TimeSpan ts)
		{
			String ret = "";
			string numDay = ts.Days == 1 ? "Singular" : "Plural";
			string numHrs = ts.Hours == 1 ? "Singular" : "Plural";
			string numMin = ts.Minutes == 1 ? "Singular" : "Plural";
			string numSec = ts.Seconds == 1 ? "Singular" : "Plural";

			string defDay = ts.Days == 1 ? "day" : "days";
			string defHrs = ts.Hours == 1 ? "hour" : "hours";
			string defMin = ts.Minutes == 1 ? "minute" : "minutes";
			string defSec = ts.Seconds == 1 ? "second" : "seconds";

			if (ts.Days > 0)
				ret += String.Format("{0} {1}, ", ts.Days, T("DateDays", numDay, defDay));

			if (ts.Hours > 0)
				ret += String.Format("{0} {1}, ", ts.Hours, T("DateHours", numHrs, defHrs));

			if (ts.Minutes > 0)
				ret += String.Format("{0} {1}, ", ts.Minutes, T("DateMinutes", numMin, defMin));

			ret += String.Format("{0} {1}", ts.Seconds, T("DateSeconds", numSec, defSec));

			return ret;
		}

		/// <summary>
		/// Turns a timespan to a short and compact string in the format:
		/// DD HH:MM:SS (days, hours, minutes, seconds and leading zeroes)
		/// </summary>
		/// <param name="ts">The timespan to turn into a string</param>
		/// <returns>The timespan printed out to a short string</returns>
		public static String TimeSpanToString(TimeSpan ts)
		{
			String ret = "";
			if (ts.Days > 0)
				ret += String.Format("{0}:", ts.Days);

			if (ts.Hours > 0)
				ret += String.Format("{0:00}:", ts.Hours);

			ret += String.Format("{0:00}:", ts.Minutes);
			ret += String.Format("{0:00}", ts.Seconds);

			return ret;
		}

		/// <summary>
		/// Turns a unix timestamp into a date string.
		/// </summary>
		/// <param name="timestamp">The unix timestamp</param>
		/// <returns>A formatted date and time string</returns>
		public static String Unix2Date(long timestamp)
		{
			DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
			dt = dt.AddSeconds(timestamp);
			dt = dt.AddSeconds((DateTime.Now - DateTime.UtcNow).TotalSeconds);
			return String.Format("{0:ddd, MMM d, yyy HH:mm}", dt);
		}

		/// <summary>
		/// Turns a version timestamp into a dotted string.
		/// </summary>
		/// <param name="timestamp">The unix timestamp of the version</param>
		/// <returns>A dotted version of the timestamp in the form A.BB.CCC.DDDD</returns>
		public static String Version2String(long timestamp)
		{
			int v1 = (int)(timestamp / 1000000000);
			int v2 = (int)(timestamp / 10000000) % 100;
			int v3 = (int)(timestamp / 10000) % 1000;
			int v4 = (int)(timestamp % 10000);
			return v1 + "." + v2 + "." + v3 + "." + v4;
		}

		/// <summary>
		/// Translates a string
		/// </summary>
		/// <param name="id">The id of the translation value</param>
		/// <param name="field">The field of the translation value</param>
		/// <param name="def">The default value (sets to field if empty)</param>
		/// <returns>The string associated with the translation value</returns>
		public static string T(string id, string field = "Text", string def = "")
		{
			if (def == "") def = field;
#if (Windows)
			LanguageDictionary dictionary = LanguageDictionary.GetDictionary(LanguageContext.Instance.Culture);
			return (string)dictionary.Translate(id, field, def, typeof(string));
#endif
			return def;
		}

		/// <summary>
		/// Formats an unsigned integer using local culture
		/// </summary>
		/// <param name="n">The number to format</param>
		/// <returns>The number formatted according to localization</returns>
		public static string T(uint n)
		{
			if (n == 0)
				return "0";
			return n.ToString("N0", Thread.CurrentThread.CurrentCulture);
		}

		/// <summary>
		/// Formats an integer using local culture
		/// </summary>
		/// <param name="n">The number to format</param>
		/// <returns>The number formatted according to localization</returns>
		public static string T(int n)
		{
			if (n == 0)
				return "0";
			return n.ToString("N0", Thread.CurrentThread.CurrentCulture);
		}

		/// <summary>
		/// Formats an integer using local culture
		/// </summary>
		/// <param name="n">The number to format</param>
		/// <returns>The number formatted according to localization</returns>
		public static string T(long n)
		{
			if (n == 0)
				return "0";
			return n.ToString("N0", Thread.CurrentThread.CurrentCulture);
		}

		/// <summary>
		/// Formats an integer using local culture
		/// </summary>
		/// <param name="n">The number to format</param>
		/// <returns>The number formatted according to localization</returns>
		public static string T(ulong n)
		{
			if (n == 0)
				return "0";
			return n.ToString("N0", Thread.CurrentThread.CurrentCulture);
		}

		/// <summary>
		/// Formats a double using local culture
		/// </summary>
		/// <param name="n">The number to format</param>
		/// <returns>The number formatted according to localization</returns>
		public static string T(double n)
		{
			return n.ToString("N", Thread.CurrentThread.CurrentCulture);
		}

		/// <summary>
		/// Formats a date using local culture
		/// </summary>
		/// <param name="dt">The date to format</param>
		/// <returns>The date formatted according to localization</returns>
		public static string T(DateTime dt)
		{
			if (dt.Year < 2)
				return T ("Never", "Text", "Never");
			return dt.ToString(Thread.CurrentThread.CurrentCulture);
		}

		/// <summary>
		/// Translates a source type name.
		/// </summary>
		/// <param name="sourceType">The source type</param>
		/// <returns>A localized name of the source type</returns>
		public static string T(SourceType sourceType)
		{
			switch (sourceType)
			{
			case SourceType.File:
				return U.T("SourcesTypeFile", "Text", "File");

			case SourceType.Folder:
				return U.T("SourcesTypeFolder", "Text", "Folder");

			case SourceType.Library:
				return U.T("SourcesTypeLibrary", "Text", "Library");
			}
			return U.T("Unknown", "Text", "Unknown");
		}

		/// <summary>
		/// Translates a plugin type name.
		/// </summary>
		/// <param name="pluginType">The plugin type</param>
		/// <returns>A localized name of the plugin type</returns>
		public static string T(Stoffi.Plugins.PluginType pluginType)
		{
			switch (pluginType)
			{
			case Stoffi.Plugins.PluginType.Source:
				return U.T("AppTypeSource", "Text", "Source");
		
			case Stoffi.Plugins.PluginType.Filter:
				return U.T("AppTypeFilter", "Text", "Filter");

			case Stoffi.Plugins.PluginType.Visualizer:
				return U.T("AppTypeVisualizer", "Text", "Visualizer");
			}
			return U.T("Unknown", "Text", "Unknown");
		}

		/// <summary>
		/// Separates a title by a list of separators
		/// and identifies artist and title.
		/// </summary>
		/// <param name="title">The title to split</param>
		/// <returns>An array holding artist and title</returns>
		public static string[] ParseTitle(string title)
		{
			string[] ret = SplitTitle(title);
			string[] enclosings = new[] { "\'.+\'", "\".+\"", "\\(.+\\)", "\\[.+\\]" };
			for (int i = 0; i < ret.Count(); i++)
			{
				ret[i] = ret[i].Trim(new[] { ' ', '\t', '-', '_' });
				foreach (string e in enclosings)
				{
					Match m = Regex.Match(ret[i], "^" + e + "$");
					if (m.Success)
						ret[i] = ret[i].Substring(1, ret[i].Length - 2);
				}
			}
			return ret;
		}

		/// <summary>
		/// Separates a title by a list of separators
		/// and identifies artist and title.
		/// </summary>
		/// <param name="title">The title to split</param>
		/// <returns>An array holding artist and title</returns>
		public static string[] SplitTitle(string title)
		{
			// remove all meta words
			List<string> meta = new List<string>();
			foreach (string m in new[] { "official video", "lyrics", "with lyrics", "hq", "hd", "official", "official audio", "alternate official video" })
				foreach (string e in new[] { "({0})", "[{0}]" })
					meta.Add(String.Format(e, m));
			meta.Add("official video");
			string lowTitle = title.ToLower();
			foreach (string m in meta)
			{
				string s = m.ToLower();
				int i = lowTitle.IndexOf(s);
				if (i >= 0)
				{
					title = title.Substring(0, i) + title.Substring(i + s.Length);
					lowTitle = title.ToLower();
				}
			}

			// split on - : ~ by
			// key is the separator, value is whether the left part is the artist
			var separators = new Dictionary<string,bool> ();

			foreach (string s in new[] { "-", ":", "~" }) {
				string[] variants = new[] {
					" " + s + " ", " " + s, s + " "
				};
				foreach (string v in variants) {
					separators.Add (v, true);
				}
			}
			separators.Add (", by ", false);
			separators.Add (" by ", false);
			foreach (var sep in separators)
			{
				var s = sep.Key;
				if (title.Contains(s))
				{
					string[] str = title.Split(new[] { s }, 2, StringSplitOptions.None);
					var artist = str [sep.Value?0:1];
					var name = str [sep.Value?1:0];

					// look for text which may hint that the string is the artist
					string[] artistTexts = new[]
					{
						"by ",
						"ft ",
						"ft.",
						"feat ",
						"feat.",
						"with "
					};
					foreach (var artistText in artistTexts)
					{
						var lowArtist = artist.ToLower ();
						var lowName = name.ToLower ();

						// remove prefix
						if (lowArtist.StartsWith (artistText))
							return new[] { artist.Substring (artistText.Length), name };

						// swap and remove prefix
						else if (lowName.StartsWith (artistText))
							return new[] { name.Substring (artistText.Length), artist };

						// swap
						else if (name.Contains (artistText))
							return new[] { name, artist };
					}
					return new[] { artist, name };
				}
			}

			// title in quotes
			// ex: Eminem "Not Afraid"
			string titlePattern = "(\'(?<title>.+)\'|\"(?<title>.+)\")";
			string artistPattern = "(?<artist>.+)";
			string pattern = String.Format("({0}\\s+{1}|{1}\\s+{0})", titlePattern, artistPattern);
			Match match = Regex.Match(title, pattern);
			if (match.Success)
			{
				return new[] { match.Groups["artist"].Value, match.Groups["title"].Value };
			}

			return new[] { "", title };
		}

		/// <summary>
		/// Prettifies a meta data tag of a track.
		/// </summary>
		/// <param name="tag">The tag to be prettified</param>
		/// <returns>The tag with special characters removed and properly titlelized</returns>
		public static string PrettifyTag(string tag)
		{
			if (String.IsNullOrWhiteSpace (tag))
				return "";
			tag = tag.Replace("_", " ").Replace("\t", "");
			tag = tag.Trim();
			while (tag.Contains("  "))
				tag = tag.Replace("  ", " ");
			string[] words = tag.Split(' ');
			for (int i = 0; i < words.Count(); i++)
				words[i] = Capitalize(words[i]);
			return String.Join(" ", words);
		}

		/// <summary>
		/// Checks if a given host of a URI is reachable.
		/// </summary>
		/// <param name="url">The URI to check (only checks the hostname)</param>
		public static bool Ping(string uri)
		{
			return Ping(new Uri(uri));
		}

		/// <summary>
		/// Checks if a given host of a URI is reachable.
		/// </summary>
		/// <param name="url">The URI to check (only checks the hostname)</param>
		public static bool Ping(Uri uri)
		{
			try
			{
				U.L(LogLevel.Debug, "Utilities", "Checking status of: " + uri.Host);
				Ping p = new Ping();
				PingReply r = p.Send(uri.Host, 5000);
				if (r.Status != IPStatus.Success)
				{
					U.L(LogLevel.Error, "Utilities", "Could not reach " + uri.Host + ": " + r.Status);
					return false;
				}
				else
				{
					U.L(LogLevel.Debug, "Utilities", "Successfully reached: " + uri.Host);
					return true;
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Error, "Utilities", "Error while trying to reach " + uri.Host + ": " + e.Message);
				return false;
			}
		}

		#endregion

		#region Private

		/// <summary>
		/// Converts a LogLevel to an integer
		/// </summary>
		/// <param name="level">The level to convert</param>
		/// <returns><paramref name="level"/> represented as an integer where Debug &lt; PropertiesWindow &lt; Warning &lt; Error</returns>
		private static int LevelToInt(LogLevel level)
		{
			if (level == LogLevel.Debug) return 1;
			else if (level == LogLevel.Information) return 2;
			else if (level == LogLevel.Warning) return 3;
			else if (level == LogLevel.Error) return 4;
			else return 0;
		}

		/// <summary>
		/// Converts a LogLevel to a string
		/// </summary>
		/// <param name="level">The level to convert</param>
		/// <returns><paramref name="level"/> represented as a string</returns>
		private static string LevelToString(LogLevel level)
		{
			if (level == LogLevel.Debug) return "DEBUG";
			else if (level == LogLevel.Information) return "INFO";
			else if (level == LogLevel.Warning) return "OOPS";
			else if (level == LogLevel.Error) return "SHIT";
			else return "HUH?";
		}

		#endregion

		#endregion
	}

	#region Enum

	/// <summary>
	/// Describes the level of a log message
	/// </summary>
	public enum LogLevel
	{
		/// <summary>
		/// Messages that are useful when debugging the application
		/// </summary>
		Debug,

		/// <summary>
		/// Messages that show general information about the application's actions
		/// </summary>
		Information,

		/// <summary>
		/// Messages that informs about something that have gone wrong
		/// </summary>
		Warning,

		/// <summary>
		/// Messages informing that something fatal has happened to the application
		/// </summary>
		Error
	}

	#endregion

	#region Delegates

	/// <summary>
	/// Represents the method that will be called when an ErrorEvent occurs
	/// </summary>
	/// <param name="sender">The sender of the event</param>
	/// <param name="message">The error message</param>
	public delegate void ErrorEventHandler(object sender, string message);

	#endregion

	#region Data structures

	/// <summary>
	/// Describes a generic event argument structure with arbitrary types.
	/// </summary>
	/// <typeparam name="T">The type of the class' single value</typeparam>
	public class GenericEventArgs<T> : EventArgs
	{
		/// <summary>
		/// The value of the event.
		/// </summary>
		T value;

		/// <summary>
		/// Gets the value of the event.
		/// </summary>
		public T Value { get { return value; } }

		/// <summary>
		/// Creates a generic event data structure.
		/// </summary>
		/// <param name="value">The value of the event data</param>
		public GenericEventArgs(T value) { this.value = value; }
	}

	/// <summary>
	/// List which can be sorted by pairs of strings (property name) and bools (whether ascending).
	/// </summary>
	public class SortableList<T> : List<T>
	{
		private string _propertyName;
		private bool _ascending;

		/// <summary>
		/// Sort the list in place by a given property.
		/// </summary>
		/// <param name="propertyName">Property name.</param>
		/// <param name="ascending">If set to <c>true</c> ascending, otherwise descending.</param>
		public void Sort(string propertyName, bool ascending)
		{
			if (_propertyName == propertyName && _ascending == ascending)
			{
				// don't do unneccessary work
				return;
			}
			else
			{
				_propertyName = propertyName;
				_ascending = ascending;
			}

			PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
			PropertyDescriptor propertyDesc = properties.Find(propertyName, true);

			// Apply and set the sort, if items to sort
			PropertyComparer<T> pc = new PropertyComparer<T>(propertyDesc, (_ascending) ? ListSortDirection.Ascending : ListSortDirection.Descending);

			// sort using merge sort (need stable sort, this.Sort is unstable)
			var sorted = MergeSort (this, pc);
			this.Clear ();
			this.AddRange (sorted);
		}

		/// <summary>
		/// Sort a list using the merge sort algorithm.
		/// </summary>
		/// <param name="list">List to sort.</param>
		/// <returns>The list 'list' sorted by the comparer 'comparer'.</returns>
		private List<T> MergeSort(List<T> list, PropertyComparer<T> comparer)
		{
			if (list == null || list.Count <= 1)
				return list;

			// divide
			var left = new List<T> ();
			var right = new List<T> ();
			var m = list.Count / 2;
			for (int i=0; i < list.Count; i++)
			{
				if (i < m)
					left.Add (list [i]);
				else
					right.Add (list [i]);
			}

			// sort
			left = MergeSort (left, comparer);
			right = MergeSort (right, comparer);

			// conquer
			return Merge (left, right, comparer);
		}

		/// <summary>
		/// Merge to lists in sorted order.
		/// </summary>
		/// <param name="left">Left list.</param>
		/// <param name="right">Right list.</param>
		/// <param name="comparer">Comparer to use to sort the lists.</param>
		private List<T> Merge(List<T> left, List<T> right, PropertyComparer<T> comparer)
		{
			var list = new List<T> ();

			while (left.Count > 0 || right.Count > 0)
			{
				if (left.Count > 0 && right.Count > 0)
				{
					var c = comparer.Compare (left[0], right[0]);
					if (c <= 0)
					{
						list.Add (left [0]);
						left.RemoveAt (0);
					}
					else
					{
						list.Add (right [0]);
						right.RemoveAt (0);
					}
				}
				else if (left.Count > 0)
				{
					list.Add (left [0]);
					left.RemoveAt (0);
				}
				else
				{
					list.Add (right [0]);
					right.RemoveAt (0);
				}
			}
			return list;
		}
	}

	/// <summary>
	/// Property comparer.
	/// </summary>
	public class PropertyComparer<T> : System.Collections.Generic.IComparer<T>
	{

		// The following code contains code implemented by Rockford Lhotka:
		// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnadvnet/html/vbnet01272004.asp

		private PropertyDescriptor _property;
		private ListSortDirection _direction;

		/// <summary>
		/// Initializes a new instance of the <see cref="Stoffi.Core.PropertyComparer`1"/> class.
		/// </summary>
		/// <param name="property">Property.</param>
		/// <param name="direction">Direction.</param>
		public PropertyComparer(PropertyDescriptor property, ListSortDirection direction)
		{
			_property = property;
			_direction = direction;
		}

		/// <summary>
		/// Determine order between two objects.
		/// </summary>
		/// <param name="xWord">X word.</param>
		/// <param name="yWord">Y word.</param>
		public int Compare(T xWord, T yWord)
		{
			// Get property values
			object xValue = GetPropertyValue(xWord, _property.Name);
			object yValue = GetPropertyValue(yWord, _property.Name);

			// Determine sort order
			if (_direction == ListSortDirection.Ascending)
			{
				return CompareAscending(xValue, yValue);
			}
			else
			{
				return CompareDescending(xValue, yValue);
			}
		}

		/// <summary>
		/// Determines whether two objects are equal.
		/// </summary>
		/// <param name="xWord">The first object to compare.</param>
		/// <param name="yWord">The second object to compare.</param>
		public bool Equals(T xWord, T yWord)
		{
			return xWord.Equals(yWord);
		}

		/// <summary>
		/// Generates a hash code for the speficied object.
		/// </summary>
		/// <returns>The hash code.</returns>
		/// <param name="obj">The object to get the hash code for.</param>
		public int GetHashCode(T obj)
		{
			return obj.GetHashCode();
		}

		/// <summary>
		/// Compares to object in ascending direction.
		/// </summary>
		/// <returns>-1 if x comes before y, 0 if equal, 1 otherwise.</returns>
		/// <param name="xValue">X value.</param>
		/// <param name="yValue">Y value.</param>
		private int CompareAscending(object xValue, object yValue)
		{
			int result;

			if (xValue == null && yValue != null) return -1;
			if (yValue == null && xValue != null) return 1;
			if (xValue == null && yValue == null) return 0;
			// If values implement IComparer
			if (xValue is IComparable)
			{
				result = ((IComparable)xValue).CompareTo(yValue);
			}
			// If values don't implement IComparer but are equivalent
			else if (xValue.Equals(yValue))
			{
				result = 0;
			}
			// Values don't implement IComparer and are not equivalent, so compare as string values
			else result = xValue.ToString().CompareTo(yValue.ToString());

			// Return result
			return result;
		}
		
		/// <summary>
		/// Compares to object in descending direction.
		/// </summary>
		/// <returns>1 if x comes before y, 0 if equal, -1 otherwise.</returns>
		/// <param name="xValue">X value.</param>
		/// <param name="yValue">Y value.</param>
		private int CompareDescending(object xValue, object yValue)
		{
			// Return result adjusted for ascending or descending sort order ie
			// multiplied by 1 for ascending or -1 for descending
			return CompareAscending(xValue, yValue) * -1;
		}

		/// <summary>
		/// Gets the value of a given property of a given object.
		/// </summary>
		/// <returns>The property value.</returns>
		/// <param name="value">The object to get the property value from.</param>
		/// <param name="property">The name of the property.</param>
		private object GetPropertyValue(T value, string property)
		{
			// Get property
			PropertyInfo propertyInfo = value.GetType().GetProperty(property);

			// Return value
			return propertyInfo.GetValue(value, null);
		}
	}

	#endregion
}