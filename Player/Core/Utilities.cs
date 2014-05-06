/**
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
 **/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Reflection;
using System.Web;
using Tomers.WPF.Localization;
using Newtonsoft.Json.Linq;

namespace Stoffi
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
		/// Gets the full path of from the executable that is running.
		/// </summary>
		public static string FullPath
		{
			get
			{
				return Uri.UnescapeDataString(
					(new Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath
				).Replace("/", @"\");
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Creates a utility class with a "Stoffi.log" logfile in the TEMP folder and a Level of Warning
		/// </summary>
		static U()
		{
			LogFile = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "Stoffi.log");
			Level = LogLevel.Warning;
			initTime = DateTime.Now;
		}

		#endregion

		#region Methods

		#region Public

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
		/// Looks for a track with a specific path in a collection
		/// </summary>
		/// <param name="collection">A collection of tracks</param>
		/// <param name="path">A path to look for</param>
		/// <returns>True if any track has a path with <paramref name="path"/> as prefix, otherwise false.</returns>
		public static bool ContainsPath(ObservableCollection<TrackData> collection, string path)
		{
			foreach (TrackData t in collection)
				if (t.Path.StartsWith(path))
					return true;
			return false;
		}

		/// <summary>
		/// Looks for all tracks with a specific path in a collection
		/// </summary>
		/// <param name="collection">A collection of tracks</param>
		/// <param name="path">A path to look for</param>
		/// <returns>Any track that has a path with <paramref name="path"/> as prefix</returns>
		public static List<TrackData> GetTracks(ObservableCollection<TrackData> collection, string path)
		{
			List<TrackData> ret = new List<TrackData>();
			foreach (TrackData t in collection)
				if (t.Path.StartsWith(path))
					ret.Add(t);
			return ret;
		}

		/// <summary>
		/// Turns a size in bytes into a human readable string
		/// </summary>
		/// <param name="size">The size</param>
		/// <returns>A localized string describing the size</returns>
		public static string HumanSize(long size)
		{
			if (size > Math.Pow(10,12))
				return String.Format(U.T("SizeTb"), Math.Round((double)(size / Math.Pow(10, 12)), 2));

			else if (size > Math.Pow(10, 9))
				return String.Format(U.T("SizeGb"), Math.Round((double)(size / Math.Pow(10, 9)), 2));

			else if (size > Math.Pow(10, 6))
				return String.Format(U.T("SizeMb"), Math.Round((double)(size / Math.Pow(10, 6)), 2));

			else if (size > Math.Pow(10, 3))
				return String.Format(U.T("SizeKb"), Math.Round((double)(size / Math.Pow(10, 3)), 2));

			else
				return String.Format(U.T("SizeByte"), size);
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
				if ((n >= 9 && n <= 10) ||
					(n == 13) ||
					(n >= 32 && n <= 55295) ||
					(n >= 57344 && n <= 65533) ||
					(n >= 65536 && n <= 1114111))
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
			return d.ToString("0.00", CultureInfo.GetCultureInfo("en-US"));
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
			if (str.Length == 0)
				return "";
			else
			{
				str = str.ToLower();
				char[] a = str.ToCharArray();
				a[0] = Char.ToUpper(a[0]);
				return new String(a);
			}
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

			if (ts.Days > 0)
				ret += String.Format("{0} {1}, ", ts.Days, T("DateDays", numDay));

			if (ts.Hours > 0)
				ret += String.Format("{0} {1}, ", ts.Hours, T("DateHours", numHrs));

			ret += String.Format("{0} {1}, ", ts.Minutes, T("DateMinutes", numMin));
			ret += String.Format("{0} {1}", ts.Seconds, T("DateSeconds", numSec));

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
				ret += String.Format("{0:00}:", ts.Days);

			if (ts.Hours > 0)
				ret += String.Format("{0:00}:", ts.Hours);

			ret += String.Format("{0:00}:", ts.Minutes);
			ret += String.Format("{0:00}", ts.Seconds);

			return ret;
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
			LanguageDictionary dictionary = LanguageDictionary.GetDictionary(LanguageContext.Instance.Culture);
			return (string)dictionary.Translate(id, field, def, typeof(string));
		}

		/// <summary>
		/// Formats an integer using local culture
		/// </summary>
		/// <param name="n">The number to format</param>
		/// <returns>The number formatted according to localization</returns>
		public static string T(int n)
		{
			return n.ToString("N0", Thread.CurrentThread.CurrentCulture);
		}

		/// <summary>
		/// Formats an integer using local culture
		/// </summary>
		/// <param name="n">The number to format</param>
		/// <returns>The number formatted according to localization</returns>
		public static string T(ulong n)
		{
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
					return U.T("SourcesTypeFile");

				case SourceType.Folder:
					return U.T("SourcesTypeFolder");

				case SourceType.Library:
					return U.T("SourcesTypeLibrary");
			}
			return U.T("Unknown");
		}

		/// <summary>
		/// Translates a plugin type name.
		/// </summary>
		/// <param name="pluginType">The plugin type</param>
		/// <returns>A localized name of the plugin type</returns>
		public static string T(Plugins.PluginType pluginType)
		{
			switch (pluginType)
			{
				case Plugins.PluginType.Source:
					return U.T("AppTypeSource");

				case Plugins.PluginType.Filter:
					return U.T("AppTypeFilter");

				case Plugins.PluginType.Visualizer:
					return U.T("AppTypeVisualizer");
			}
			return U.T("Unknown");
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

			// split on - : ~
			foreach (string sep in new[] { "-", ":", "~" })
			{
				string[] variants = new[]
				{
					" " + sep + " ",
					" " + sep,
					sep + " "
				};
				foreach (string var in variants)
				{
					if (title.Contains(var))
					{
						string[] str = title.Split(new[] { var }, 2, StringSplitOptions.None);

						string[] prefixes = new[]
						{
							"by ",
							"ft ",
							"ft.",
							"feat ",
							"feat.",
							"with "
						};

						foreach (string pref in prefixes)
						{
							if (str[0].ToLower().StartsWith(pref))
								return new[] { str[0].Substring(pref.Length), str[1] };
							else if (str[1].ToLower().StartsWith(pref))
								return new[] { str[1].Substring(pref.Length), str[0] };
						}
						return new[] { str[0], str[1] };
					}
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

	#region Data structures

	/// <summary>
	/// Describes a collection of event arguments of a generic type.
	/// </summary>
	/// <typeparam name="T">The type of the arguments</typeparam>
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
		/// Creates a generic event data object.
		/// </summary>
		/// <param name="value">The value of the event data</param>
		public GenericEventArgs(T value) { this.value = value; }
	}

	#endregion
}