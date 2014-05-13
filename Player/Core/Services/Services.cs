/**
 * Services.cs
 * 
 * Communicates with the Stoffi Services.
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Xml;
using System.Web;

using Newtonsoft.Json.Linq;

using Stoffi.Core.Media;
using Stoffi.Core.Playlists;
using Stoffi.Core.Settings;
using Stoffi.Core.Sources;

namespace Stoffi.Core.Services
{
	/// <summary>
	/// Represents a manager that handles communication with
	/// the Stoffi services.
	/// </summary>
	public static class Manager
	{
		#region Members

		private static OAuth.Manager oauth = new OAuth.Manager();
		private static string oauth_request_token = "";
		private static string domain = "http://beta.stoffiplayer.com";
		private static bool connected = true;
		private static int failedPings = 0;
		private static object sendLocker = new object();
		private static object syncOutLocker = new object();

		/// <summary>
		/// Ensures that FetchLinkData is always called once RegisterDevice has finished.
		/// </summary>
		private static object initLocker = new object();

		private static string realtimeSessionID = null;

		/// <summary>
		/// The name of the currently linked user.
		/// </summary>
		private static string userName = "";

		/// <summary>
		/// The name of this device.
		/// </summary>
		private static string deviceName = "";

		/// <summary>
		/// The timer which sends all sync operations contained in syncOutBuffer.
		/// </summary>
		private static Timer syncOutDelay = null;

		/// <summary>
		/// The ID of the currently linked user. -1 indicates no link active.
		/// </summary>
		private static uint userID = 0;

		/// <summary>
		/// The language tag for use with Stoffi website.
		/// </summary>
		private static string lang = CultureInfo.CurrentUICulture.IetfLanguageTag.Split('-').Last<string>().ToLower();

		/// <summary>
		/// A list of all buffered sync operations scheduled to be sent to the server.
		/// </summary>
		private static List<SyncOperation> syncOutBuffer = new List<SyncOperation>();
		//private static Dictionary<KeyValuePair<string, string>, string> albumArtCache = new Dictionary<KeyValuePair<string, string>, string>();
		private static string artFolder = "Art";
		private static string defaultArt = "Default.png";
		private static string loadingArt = "Loading.png";

		/// <summary>
		/// A timer used for delaying listen requests so that if the user changes tracks
		/// rapidely, only the last track will be submitted.
		/// </summary>
		private static Timer listenDelay = null;

		/// <summary>
		/// The track that is the current object being listened to.
		/// </summary>
		private static Track currentListen = null;

		/// <summary>
		/// A cached ID of the listen object for the current track at the server.
		/// </summary>
		private static uint idOfCurrentListen = 0;

		/// <summary>
		/// The time the current listen was started.
		/// </summary>
		private static DateTime? stampOfCurrentListen = null;

		/// <summary>
		/// A list of all listens that has been sent to server but now yet received reply.
		/// Key is the path of the track, value is timestamp of send and whether to issue delete upon reply.
		/// </summary>
		private static Dictionary<string, Tuple<bool, DateTime>> listenQueue = new Dictionary<string, Tuple<bool, DateTime>>();

		/// <summary>
		/// The minimum amount of seconds a song most play. If the playtime is below this
		/// then the listen will be deleted from the server.
		/// </summary>
		private static uint minimumListenTime = 15;

		/// <summary>
		/// A timer used for retransmission of listen submissions which have failed.
		/// </summary>
		private static Timer listenRetry = null;

		/// <summary>
		/// The seconds to wait until next retry of transmitting failed listen submissions.
		/// </summary>
		private static int[] listenRetrySteps = new int[] { 10, 30, 60, 180, 300 };

		/// <summary>
		/// The index of listenRetrySteps, indicating how many seconds to wait for next retransmission of failed listen submissions.
		/// </summary>
		/// <remarks>
		/// -1 indicates no retransmissions scheduled. If index is above upper bound then the last value in listenRetrySteps will be used.
		/// </remarks>
		private static int listenRetryStep = -1;

		/// <summary>
		/// The timer for attempting to reconnect when the connection to the cloud server is lost.
		/// </summary>
		private static Timer reconnectTimer = null;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the loading album art image.
		/// </summary>
		public static string LoadingArt { get { return defaultArt; } }

		/// <summary>
		/// Gets whether or not the 
		/// </summary>
		public static bool Connected
		{
			get { return connected; }
			set
			{
				bool old = connected;
				connected = value;

				if (value)
				{
					if (reconnectTimer != null)
						reconnectTimer.Dispose();
					reconnectTimer = null;
					if (old != value)
						InitOAuth();
				}
				else
					InitiateReconnectTimer();

				if (old != value)
					DispatchPropertyChanged("Connected", old, value);
			}
		}

		/// <summary>
		/// Gets whether the client is linked to a service account or not
		/// </summary>
		public static bool Linked
		{
			get
			{
				return !String.IsNullOrWhiteSpace(Settings.Manager.OAuthToken) &&
					!String.IsNullOrWhiteSpace(Settings.Manager.OAuthSecret);
			}
		}

		/// <summary>
		/// Gets the URL for authorizing the application with OAuth
		/// </summary>
		public static string RequestURL
		{
			get
			{
				return String.Format("{0}/{1}/oauth/authorize?oauth_token={2}",
					domain, 
					lang, 
					oauth_request_token);
			}
		}

		/// <summary>
		/// Gets the URL for logging out
		/// </summary>
		public static string LogoutURL { get { return String.Format("{0}/{1}/logout", domain, lang); } }

		/// <summary>
		/// Gets the Identity representing the currently linked user.
		/// </summary>
		public static Identity Identity
		{
			get
			{
				return Settings.Manager.GetCloudIdentity(userID);
			}
		}

		/// <summary>
		/// Gets the callback URL for OAuth
		/// </summary>
		public static string CallbackURL
		{
			get { return String.Format("{0}/dashboard", domain); }
		}

		/// <summary>
		/// Gets the callback URL with parameters to authenticate 
		/// </summary>
		public static string CallbackURLWithAuthParams
		{
			get
			{
				return String.Format("{0}/{1}/dashboard?callback=stoffi&oauth_token={2}&oauth_secret_token={3}",
					domain,
					lang,
					Settings.Manager.OAuthToken,
					Settings.Manager.OAuthSecret);
			}
		}

		/// <summary>
		/// Gets the name of the currently linked user
		/// </summary>
		public static string UserName
		{
			get { return userName; }
			set
			{
				object old = userName;
				userName = value;
				DispatchPropertyChanged("UserName", old, value, true);
			}
		}

		/// <summary>
		/// Gets or sets the name of the device
		/// </summary>
		public static string DeviceName
		{
			get { return deviceName; }
			set
			{
				object old = deviceName;
				deviceName = value;
				DispatchPropertyChanged("DeviceName", old, value, true);

				if (old != value as object)
				{
					U.L(LogLevel.Debug, "SERVICE", "Changing device name");
					string url = String.Format("/devices/{0}.json", Identity.DeviceID);
					string query = String.Format("?device[name]={0}&device[version]={1}",
						OAuth.Manager.UrlEncode(value),
						OAuth.Manager.UrlEncode(Settings.Manager.Release));
					var response = SendRequest(url, "PUT", query);
					if (response == null || response.StatusCode != HttpStatusCode.NoContent)
					{
						U.L(LogLevel.Error, "SERVICE", "There was a problem changing device name");
						U.L(LogLevel.Error, "SERVICE", response);
					}
					else
					{
						U.L(LogLevel.Debug, "SERVICE", "Device name changed successfully");
					}
					if (response != null) response.Close();
				}
			}
		}

		/// <summary>
		/// Gets or sets whether or not to synchronize with the cloud.
		/// </summary>
		public static bool Synchronize
		{
			get { return Identity != null && Identity.Synchronize; }
			set
			{
				bool old = Synchronize;
				if (Identity != null)
				{
					Identity.Synchronize = value;
					if (value && !old)
					{
						SyncConfig();
						SyncPlaylists();
					}
				}
				DispatchPropertyChanged("Synchronize", old, value);
			}
		}

		/// <summary>
		/// Gets or sets whether or not to synchronize configuration.
		/// </summary>
		public static bool SynchronizeConfiguration
		{
			get
			{
				return Identity != null && Identity.Synchronize && Identity.SynchronizeConfig;
			}
			set
			{
				if (value)
					Synchronize = true;

				if (Identity != null)
					Identity.SynchronizeConfig = value;
				SyncConfig();
			}
		}

		/// <summary>
		/// Gets or sets whether or not to synchronize Playlists.Manager.
		/// </summary>
		public static bool SynchronizePlaylists
		{
			get
			{
				return Identity != null && Identity.Synchronize && Identity.SynchronizePlaylists;
			}
			set
			{
				if (value)
					Synchronize = true;

				if (Identity != null)
					Identity.SynchronizePlaylists = value;
				SyncPlaylists();
			}
		}

		/// <summary>
		/// Gets the domain name of the service.
		/// </summary>
		public static string Domain { get { return domain; } }

		/// <summary>
		/// Gets or sets the session ID of the juggernaut connection for real time communication.
		/// </summary>
		public static string RealTimeSessionID
		{
			get { return realtimeSessionID; }
			set
			{
				realtimeSessionID = value;
				if (!String.IsNullOrWhiteSpace(value))
				{
					lock (initLocker)
					{
						U.L(LogLevel.Information, "SERVICE", "Realtime communicaton channel is established.");
						Connected = true;
						RetrieveLinkData();
						OnRealtimeConnected();
					}
				}
				else
				{
					U.L(LogLevel.Information, "SERVICE", "Realtime communicaton channel was disconnected.");
					OnRealtimeDisconnected();
				}
			}
		}

		/// <summary>
		/// Gets whether or not the service manager has been initialized.
		/// </summary>
		public static bool IsInitialized { get; private set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of the Services class.
		/// </summary>
		static Manager()
		{
			IsInitialized = false;
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Initializes the manager.
		/// </summary>
		public static void Initialize()
		{
			ServicePointManager.DefaultConnectionLimit = 1000;
			artFolder = Path.Combine (U.BasePath, artFolder);
			defaultArt = Path.Combine (artFolder, defaultArt);
			loadingArt = Path.Combine (artFolder, loadingArt);

			ThreadStart initThread = delegate()
			{
				InitOAuth();
				Settings.Manager.PropertyChanged += Settings_PropertyChanged;
				Media.Manager.Started += Media_Started;
				Playlists.Manager.PlaylistModified += Playlists_PlaylistModified;
				Playlists.Manager.PlaylistRenamed += Playlists_PlaylistRenamed;

				foreach (var playlist in Settings.Manager.Playlists)
					playlist.PropertyChanged += new PropertyChangedEventHandler(Playlist_PropertyChanged);

				IsInitialized = true;
				DispatchInitialized();
			};
			Thread init_thread = new Thread(initThread);
			init_thread.Name = "Service Initializer thread";
			init_thread.Priority = ThreadPriority.Lowest;
			init_thread.Start();
		}

		/// <summary>
		/// Removes all album art from disk.
		/// </summary>
		public static void ClearArt()
		{
			try
			{
				Directory.Delete(artFolder);
				OnAlbumArtCacheChanged();
			}
			catch (Exception e)
			{
				U.L(LogLevel.Error, "SERVICE", "Could not clear album art: " + e.Message);
			}
		}

		/// <summary>
		/// Removes all album art of a given size from disk.
		/// </summary>
		/// <param name="size">The size to remove</param>
		public static void ClearArt(string size)
		{
			try
			{
				foreach (FileInfo f in new DirectoryInfo(artFolder).GetFiles(size + ".*"))
				{
					try
					{
						File.Delete(f.FullName);
					}
					catch (Exception innerE)
					{
						U.L(LogLevel.Error, "service", "Could not delete album art cache file " + f.FullName + ": " + innerE.Message);
					}
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Error, "service", "Could not clear album art of size " + size + ": " + e.Message);
			}
		}

		/// <summary>
		/// Fetches album art for all tracks in the background.
		/// </summary>
		public static void SetArt()
		{
			Thread aa_thread = new Thread(delegate()
			{
				SetArt(Settings.Manager.HistoryTracks);
				SetArt(Settings.Manager.QueueTracks);
				SetArt(Settings.Manager.FileTracks);
				foreach (Playlist p in Settings.Manager.Playlists)
					SetArt(p.Tracks);
			});
			aa_thread.Name = "Fetch album art";
			aa_thread.Priority = ThreadPriority.BelowNormal;
			aa_thread.Start();
		}

		/// <summary>
		/// Fetches album art for all set of tracks in the background.
		/// </summary>
		/// <param name="tracks">The tracks to fetch art for</param>
		public static void SetArt(ObservableCollection<Track> tracks)
		{
			foreach (Track t in tracks)
			{
				if (U.IsClosing) break;
				if (String.IsNullOrWhiteSpace(t.OriginalArtURL) || String.IsNullOrWhiteSpace(t.ArtURL) || t.ArtURL == LoadingArt || (t.ArtURL != defaultArt && !File.Exists(t.ArtURL) && !t.ArtURL.StartsWith("http")))
					SetArt(t);
			}
		}

		/// <summary>
		/// Fetches the album art of a given track and applies to the track.
		/// </summary>
		/// <param name="track">The track to fetch art for</param>
		public static void SetArt(Track track)
		{
			switch (track.Type)
			{
				case TrackType.File:
					track.ArtURL = Path.GetFullPath(GetArt(track));
					break;
			}
		}

		/// <summary>
		/// Set the art of a single track.
		/// The image will be copied to the cache folder.
		/// </summary>
		/// <param name="track">Track.</param>
		/// <param name="path">Path to the image.</param>
		public static void SetArt(Track track, string path)
		{
			if (!File.Exists (path))
				return;

			try
			{
				var ext = Path.GetExtension(path);
				var folder = ArtFolderForTrack(track);
				if (!Directory.Exists(folder))
					Directory.CreateDirectory(folder);
				var cache = Path.Combine(folder, track.Title + ext);
				File.Copy(path, cache);
				track.ArtURL = cache;
			}
			catch {
				track.ArtURL = path;
			}
		}

		/// <summary>
		/// Set the art of a single track.
		/// The image will be copied to the cache folder.
		/// </summary>
		/// <param name="track">Track.</param>
		/// <param name="img">The image.</param>
		public static void SetArt(Track track, Image img)
		{
			try
			{
				var format = ImageFormat.Png;
				var ext = ".png";
				var folder = ArtFolderForTrack(track);
				if (!Directory.Exists(folder))
					Directory.CreateDirectory(folder);
				var cache = Path.Combine(folder, track.Title + ext);
				img.Save(cache, format);
				track.ArtURL = cache;;
			}
			catch (Exception e) {
				U.L (LogLevel.Error, "Services", "Could not set track art: " + e.Message);
			}
		}

		/// <summary>
		/// Fetches the album art of a given track.
		/// </summary>
		/// <param name="track">The track to fetch art for</param>
		/// <returns>The first existing (depending on user's settings) size in the order: large, medium, small, huge</returns>
		public static string GetArt(Track track)
		{
			string ret = defaultArt;
			if (Settings.Manager.DownloadAlbumArtHuge)
				ret = GetArt(track, "huge");
			if (Settings.Manager.DownloadAlbumArtSmall)
				ret = GetArt(track, "small");
			if (Settings.Manager.DownloadAlbumArtMedium)
				ret = GetArt(track, "medium");
			if (Settings.Manager.DownloadAlbumArtLarge)
				ret = GetArt(track, "large");
			return ret;
		}

		/// <summary>
		/// Fetches the album art of a given track.
		/// </summary>
		/// <param name="track">The track to fetch art for</param>
		/// <param name="size">The prefered size to fetch</param>
		/// <returns>The path to the best matching album art or the default album art</returns>
		public static string GetArt(Track track, string size)
		{
			if (!Settings.Manager.DownloadAlbumArt) return defaultArt;
			string path;
			string[] ret = new string[] { defaultArt, "" };

			// check for single file
			if (!String.IsNullOrWhiteSpace(track.Album) && !String.IsNullOrWhiteSpace(track.Artist))
			{
				path = Path.Combine(artFolder, U.C(track.Artist), "Singles", U.C(track.Title));
				ret[0] = FetchCachedImages(path, size);
				if (ret[0] != defaultArt)
					return ret[0];
			}

			// check for album file
			if (!String.IsNullOrWhiteSpace(track.Album))
			{
				if (!String.IsNullOrWhiteSpace(track.Artist))
				{
					// check for artist's album
					path = Path.Combine(artFolder, U.C(track.Artist), U.C(track.Album));
					ret[0] = FetchCachedImages(path, size);
					if (ret[0] != defaultArt)
						return ret[0];

					// check for mix album
					path = Path.Combine(artFolder, "Mix", U.C(track.Album));
					ret[0] = FetchCachedImages(path, size);
					if (ret[0] != defaultArt)
						return ret[0];

				}
				else
				{
					// check all albums
					foreach (string folder in Directory.GetDirectories(artFolder))
					{
						path = Path.Combine(artFolder, folder, U.C(track.Album));
						ret[0] = FetchCachedImages(path, size);
						if (ret[0] != defaultArt)
							return ret[0];
					}
				}
			}

			// check for album URL on Last.fm
			if (!String.IsNullOrWhiteSpace(track.Album) && !String.IsNullOrWhiteSpace(track.Artist))
			{
				ret = FetchLastFMAlbumArt(track.Album, track.Artist, size);
				if (ret[0] != defaultArt)
				{
					track.OriginalArtURL = ret[1];
					return ret[0];
				}
			}

			// check for single URL on Last.fm
			if (!String.IsNullOrWhiteSpace(track.Title) && !String.IsNullOrWhiteSpace(track.Artist))
			{
				ret = FetchLastFMTrackArt(track.Title, track.Artist, size);
				if (ret[0] != defaultArt)
				{
					track.OriginalArtURL = ret[1];
					return ret[0];
				}
			}

			// check for artist URL on Last.fm
			if (!String.IsNullOrWhiteSpace(track.Artist))
			{
				ret = FetchLastFMArtistArt(track.Artist, size);
				if (ret[0] != defaultArt)
				{
					track.OriginalArtURL = ret[1];
					return ret[0];
				}
			}

			return defaultArt;
		}

		/// <summary>
		/// Gets the sizes of downloaded art on disk.
		/// </summary>
		/// <returns>A dictionary with the size (in bytes) for each image size (small, medium, large and huge)</returns>
		public static Dictionary<string, long> GetArtSizes()
		{
			Dictionary<string, long> ret = new Dictionary<string, long>();
			string[] sizes = new string[] { "small", "medium", "large", "huge" };
			foreach (string size in sizes)
				ret[size] = 0;

			try
			{
				foreach (string size in sizes)
					foreach (FileInfo f in new DirectoryInfo(artFolder).GetFiles(size + ".*", SearchOption.AllDirectories))
						ret[size] += f.Length;
			}
			catch (Exception e)
			{
				U.L(LogLevel.Error, "SERVICE", "Could not calculate album art cache sizes: " + e.Message);
			}

			return ret;
		}

		/// <summary>
		/// Shares a track.
		/// </summary>
		/// <param name="track">The track to share</param>
		public static void ShareSong(Track track)
		{
			ThreadStart shareThread = delegate()
			{
				string path = track.Path;
				if (track.Type == TrackType.File)
					path = Path.GetFileName(track.Path);

				string query = String.Format("?{0}&object=song",
						String.Join("&", EncodeTrack(track, "track")));

				var response = SendRequest("/shares.json", "POST", query);

				if (response == null || response.StatusCode != HttpStatusCode.Created)
				{
					U.L(LogLevel.Error, "SERVICE", "There was a problem sharing song " + track.Artist + " - " + track.Title);
					U.L(LogLevel.Error, "SERVICE", response);
				}
				else
				{
					U.L(LogLevel.Information, "SERVICE", "Shared song "+track.Artist + " - " + track.Title);
				}
				if (response != null) response.Close();
			};
			Thread sharethread = new Thread(shareThread);
			sharethread.Name = "Share thread";
			sharethread.Priority = ThreadPriority.Lowest;
			sharethread.Start();
		}

		/// <summary>
		/// Shares a playlist.
		/// </summary>
		/// <param name="playlist">The playlist to share</param>
		public static void SharePlaylist(Playlist playlist)
		{
			ThreadStart shareThread = delegate()
			{
				string query = String.Format("?playlist={0}&object=playlist",
						OAuth.Manager.UrlEncode(playlist.Name));
				var response = SendRequest("/shares.json", "POST", query);

				if (response == null || response.StatusCode != HttpStatusCode.Created)
				{
					U.L(LogLevel.Error, "SERVICE", "There was a problem sharing playlist " + playlist.Name);
					U.L(LogLevel.Error, "SERVICE", response);
				}
				else
				{
					U.L(LogLevel.Information, "SERVICE", "Shared playlist "+playlist.Name);
				}
				if (response != null) response.Close();
			};
			Thread sharethread = new Thread(shareThread);
			sharethread.Name = "Share thread";
			sharethread.Priority = ThreadPriority.Lowest;
			sharethread.Start();
		}

		/// <summary>
		/// Link Stoffi to an account
		/// </summary>
		/// <param name="url">The full callback URL (including parameters) where OAuth sent us after authorization</param>
		public static void Link(string url)
		{
			Dictionary<string,string> p = U.GetParams(U.GetQuery(url));
			string token = p["oauth_token"];
			string verifier = p["oauth_verifier"];
			var access_url = domain + "/oauth/access_token";
			try
			{
				OAuth.OAuthResponse accessToken = oauth.AcquireAccessToken(access_url, "POST", verifier);
				string[] tokens = accessToken.AllText.Split('&');
				Settings.Manager.OAuthToken = tokens[0].Split('=')[1];
				Settings.Manager.OAuthSecret = tokens[1].Split('=')[1];
				RetrieveUserData();
				U.L(LogLevel.Information, "SERVICE", "Cloud account has been linked");
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "SERVICE", "Problem linking with account: " + e.Message);
				Connected = false;
			}
		}

		/// <summary>
		/// Remove the current link if there is one
		/// </summary>
		public static void Delink()
		{
			if (!Linked) return;
			Settings.Manager.OAuthToken = null;
			Settings.Manager.OAuthSecret = null;
			try
			{
				InitOAuth();
				DispatchPropertyChanged("Linked", true, false);
				U.L(LogLevel.Information, "SERVICE", "Cloud account has been delinked");
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "SERVICE", "Problem delinking with account: " + e.Message);
				Connected = false;
			}
		}

		/// <summary>
		/// Download a playlist from the cloud.
		/// </summary>
		/// <param name="id">The cloud ID of the playlist</param>
		/// <returns>The playlist to object</returns>
		public static Playlist FetchPlaylist(uint id)
		{
			Playlist ret = null;
			try
			{
				string url = String.Format("/playlists/" + id.ToString() + ".json");
				var response = SendRequest(url, "GET");
				if (response == null || response.StatusCode != HttpStatusCode.OK)
				{
					U.L(LogLevel.Error, "SERVICE", "There was a problem fetching playlist " + id.ToString());
					U.L(LogLevel.Error, "SERVICE", response);
				}
				else
				{
					U.L(LogLevel.Information, "SERVICE", "Fetching playlist " + id.ToString());
					ret = MergePlaylistObject(ParseResponse(response) as JObject);
				}
				if (response != null) response.Close();
			}
			catch (Exception e)
			{
				U.L(LogLevel.Error, "SERVICE", "There was a problem fetching playlist " + id.ToString());
				U.L(LogLevel.Error, "SERVICE", e.Message);
			}
			return ret;
		}

		/// <summary>
		/// Retrieve the latest data on a playlist, owned by someone else, from the cloud and update it.
		/// </summary>
		/// <param name="playlist">The playlist to be udpated</param>
		public static void RefreshPlaylist(Playlist playlist)
		{
			if (playlist == null || !playlist.IsSomeoneElses)
				return;

			ThreadStart thread = delegate()
			{
				try
				{
					U.L(LogLevel.Debug, "SERVICE", "Retrieving playlist: " + playlist.Name + " ("+playlist.ID.ToString()+")");
					string url = String.Format("/playlists/"+playlist.ID.ToString()+".json");
					var response = SendRequest(url, "GET");
					if (response == null || response.StatusCode != HttpStatusCode.OK)
					{
						U.L(LogLevel.Error, "SERVICE", "There was a problem retrieving playlist");
						U.L(LogLevel.Error, "SERVICE", response);
						//PlaylistManager.RemovePlaylist(playlist.ID);
					}
					else
					{
						MergePlaylistObject(ParseResponse(response) as JObject);
						U.L(LogLevel.Information, "SERVICE", "Synchronized playlist " + playlist.Name);
					}
					if (response != null) response.Close();
				}
				catch (Exception e)
				{
					U.L(LogLevel.Error, "SERVICE", "There was a problem synchronizing playlist "+playlist.Name);
					U.L(LogLevel.Error, "SERVICE", e.Message);
				}
			};
			Thread th = new Thread(thread);
			th.Name = "Playlist sync thread";
			th.Priority = ThreadPriority.Lowest;
			th.Start();
		}

		/// <summary>
		/// Starts to follow a playlist, ensuring that we get any updates
		/// from the cloud.
		/// </summary>
		/// <param name="id">The ID of the playlist to follow</param>
		/// <returns>The playlist to follow</returns>
		public static Playlist FollowPlaylist(uint id)
		{
			var pl = Playlists.Manager.Get(id);
			if (pl == null)
				pl = FetchPlaylist(id);
			try
			{
				U.L(LogLevel.Debug, "SERVICE", "Following playlist: " + id.ToString());
				string url = String.Format("/playlists/" + id.ToString() + "/follow.json");
				var response = SendRequest(url, "PUT");
				if (response == null || response.StatusCode != HttpStatusCode.NoContent)
				{
					U.L(LogLevel.Error, "SERVICE", "There was a problem following playlist");
					U.L(LogLevel.Error, "SERVICE", response);
				}
				if (response != null) response.Close();
			}
			catch (Exception e)
			{
				U.L(LogLevel.Error, "SERVICE", "There was a problem following playlist "+id.ToString());
				U.L(LogLevel.Error, "SERVICE", e.Message);
			}
			return pl;
		}

		/// <summary>
		/// Update the values of an object.
		/// </summary>
		/// <remarks>
		/// Use this when an object is changed from outside the application (ie async call from server)
		/// in order to prevent the manager to send the update back to the server.
		/// </remarks>
		/// <param name="objectType">The name of the type of object</param>
		/// <param name="objectID">The object ID</param>
		/// <param name="updatedProperties">The updated properties of the object encoded in JSON</param>
		public static void UpdateObject(string objectType, uint objectID, string updatedProperties)
		{
			try
			{
				JObject o = JObject.Parse(updatedProperties);
				switch (objectType)
				{
					case "device":
						if (Identity == null) return;
						if (objectID == Identity.ConfigurationID)
						{
							if (o["name"] != null)
							{
								string name = U.UnescapeHTML((string)o["name"]);
								deviceName = name;
								DeviceName = name;
							}
						}
						break;

					case "configuration":
						if (Identity == null) return;
						if (objectID == Identity.ConfigurationID)
							SyncProfileUpdated(o);
						break;

					case "list_config":
						break;

					case "link":
						if (Identity == null) return;
						bool found = false;

						for (int i = 0; i < Identity.Links.Count; i++)
							if (Identity.Links[i].Connected && Identity.Links[i].ID == objectID)
							{
								UpdateLink(Identity.Links[i], true, o, false);
								found = true;
								break;
							}

						if (!found)
						{
							Link l = CreateLink(true, o);
							if (l != null)
								Identity.Links.Add(l);
						}

						DispatchPropertyChanged("Links", Identity.Links, Identity.Links, true);
						break;

					case "column":
						break;

					case "column_sort":
						break;

					case "equalizer_profile":
						break;

					case "keyboard_shortcut_profile":
						break;

					case "keyboard_shortcut":
						break;

					case "song":
						break;

					case "album":
						break;

					case "artist":
						break;

					case "user":
						break;

					case "playlist":
						var playlist = Playlists.Manager.Get(objectID);
						if (playlist != null && (SynchronizePlaylists || Identity == null || playlist.OwnerID != Identity.UserID))
						{
							string name = (string)o["name"];

							if (name != null)
							{
								Playlists.Manager.PlaylistRenamed -= Playlists_PlaylistRenamed;
								Playlists.Manager.Rename(objectID, (string)o["name"]);
								Playlists.Manager.PlaylistRenamed += Playlists_PlaylistRenamed;
							}

							JObject tracks = (JObject)o["songs"];

							if (tracks != null && playlist != null)
							{
								JArray added = (JArray)tracks["added"];
								JArray removed = (JArray)tracks["removed"];

								playlist.Tracks.CollectionChanged -= Playlist_CollectionChanged;

								List<Track> tracksToAdd = new List<Track>();
								List<Track> tracksToRemove = new List<Track>();
								foreach (JObject track in added)
								{
									Track t = JSONToTrack(track);
									if (t != null)
									{
										bool exists = false;
										foreach (Track tt in playlist.Tracks)
											if (tt.Path == t.Path)
											{
												exists = true;
												break;
											}
										if (!exists)
											tracksToAdd.Add(t);
									}
								}
								foreach (JObject track in removed)
								{
									Track t = JSONToTrack(track);
									if (t != null)
									{
										bool exists = false;
										foreach (Track tt in playlist.Tracks)
											if (tt.Path == t.Path)
											{
												exists = true;
												break;
											}
										if (exists)
											tracksToRemove.Add(t);
									}
								}
								DispatchModifyTracks(playlist.Tracks, new ModifiedEventArgs(ModifyType.Added, tracksToAdd));
								DispatchModifyTracks(playlist.Tracks, new ModifiedEventArgs(ModifyType.Removed, tracksToRemove));

								if (playlist.OwnerID == Identity.UserID)
									playlist.Tracks.CollectionChanged += Playlist_CollectionChanged;
							}
						}
						break;
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Error, "SERVICE", "Could not update object " + objectType + " with ID " + objectID.ToString() + ": " + e.Message);
			}
		}

		/// <summary>
		/// Creates a new object.
		/// </summary>
		/// <remarks>
		/// Use this when an object is changed from outside the application (ie async call from server)
		/// in order to prevent the manager to send the update back to the server.
		/// </remarks>
		/// <param name="objectType">The type of the object</param>
		/// <param name="objectJSON">The object encoded in JSON</param>
		public static void CreateObject(string objectType, string objectJSON)
		{
			try
			{
				JObject o = JObject.Parse(objectJSON);
				switch (objectType)
				{
					case "device":
						break;

					case "configuration":
						break;

					case "link":
						if (Identity == null) return;
						bool found = false;

						for (int i=0; i < Identity.Links.Count; i++)
							if (Identity.Links[i].Provider == (string)o["display"])
							{
								if (Identity.Links[i].Connected)
								{
									UpdateLink(Identity.Links[i], true, o);
									found = true;
									break;
								}
								else
								{
									Identity.Links.RemoveAt(i);
									break;
								}
							}

						if (!found)
						{
							Link l = CreateLink(true, o);
							if (l != null)
								Identity.Links.Add(l);
						}

						DispatchPropertyChanged("Links", Identity.Links, Identity.Links, true);
						break;

					case "list_config":
						break;

					case "column":
						break;

					case "column_sort":
						break;

					case "equalizer_profile":
						break;

					case "keyboard_shortcut_profile":
						break;

					case "keyboard_shortcut":
						break;

					case "song":
						break;

					case "album":
						break;

					case "artist":
						break;

					case "user":
						break;

					case "playlist":
						if (SynchronizePlaylists)
							MergePlaylistObject(o);
						break;
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Error, "SERVICE", "Could not create object " + objectType + ": " + e.Message);
			}
		}

		/// <summary>
		/// Deletes an object.
		/// </summary>
		/// <remarks>
		/// Use this when an object is changed from outside the application (ie async call from server)
		/// in order to prevent the manager to send the update back to the server.
		/// </remarks>
		/// <param name="objectType">The name of the type of object</param>
		/// <param name="objectID">The object ID</param>
		public static void DeleteObject(string objectType, uint objectID)
		{
			try
			{
				switch (objectType)
				{
					case "device":
						if (objectID == Identity.DeviceID)
						{
							lock (initLocker)
							{
								Identity.DeviceID = 0;
								RegisterDevice();
							}
						}
						break;

					case "configuration":
						if (objectID == Identity.ConfigurationID && Identity != null)
						{
							Identity.ConfigurationID = 0;
							Identity.SynchronizeConfig = false;
						}
						break;

					case "link":
						foreach (Link link in Identity.Links)
						{
							if (link.ID == objectID)
							{
								UpdateLink(link, false);
								break;
							}
						}
						DispatchPropertyChanged("Links", Identity.Links, Identity.Links, true);
						break;

					case "list_config":
						break;

					case "column":
						break;

					case "column_sort":
						break;

					case "equalizer_profile":
						break;

					case "keyboard_shortcut_profile":
						break;

					case "keyboard_shortcut":
						break;

					case "song":
						break;

					case "album":
						break;

					case "artist":
						break;

					case "user":
						if (Identity != null && objectID == Identity.UserID)
							Delink();
						break;

					case "playlist":
						var playlist = Playlists.Manager.Get(objectID);
						if (playlist != null && (SynchronizePlaylists || Identity == null || playlist.OwnerID != Identity.UserID))
						{
							Playlists.Manager.PlaylistModified -= Playlists_PlaylistModified;
							Playlists.Manager.Remove(objectID);
							Playlists.Manager.PlaylistModified += Playlists_PlaylistModified;
						}
						break;
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Error, "SERVICE", "Could not delete object " + objectType + " with ID " + objectID.ToString() + ": " + e.Message);
			}
		}

		/// <summary>
		/// Executes a command.
		/// </summary>
		/// <param name="command">The name of the command</param>
		/// <param name="objectType">The type of the object</param>
		/// <param name="objectID">The ID of the object</param>
		public static void ExecuteCommand(string command, string objectType, uint objectID)
		{
			try
			{
				var cmd = command.ToLower();

				switch (objectType.ToLower())
				{
					case "configuration":
						if (Identity.ConfigurationID != objectID)
							return;
						switch (cmd)
						{
							case "next":
								Media.Manager.Next(true, true);
								break;

							case "prev":
							case "previous":
								Media.Manager.Previous();
								break;

							case "play":
								Media.Manager.Play();
								break;

							case "pause":
								Media.Manager.Pause();
								break;

							case "play-pause":
								if (Settings.Manager.MediaState == MediaState.Playing)
									Media.Manager.Pause();
								else
									Media.Manager.Play();
								break;
						}
						break;

					case "playlist":
						switch (cmd)
						{
							case "follow":
								FetchPlaylist(objectID);
								break;
						}
						break;
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Error, "SERVICE", "Could not execute command " + command + ": " + e.Message);
			}
		}

		/// <summary>
		/// Synchronize the application with a given cloud configuration.
		/// </summary>
		public static void SyncConfig()
		{
			if (!SynchronizeConfiguration)
				return;

			ThreadStart thread = delegate()
			{
				try
				{
					U.L(LogLevel.Debug, "SERVICE", "Retrieving cloud sync profile");
					string url = String.Format("/configurations.json");
					var response = SendRequest(url, "GET");
					if (response == null || response.StatusCode != HttpStatusCode.OK)
					{
						U.L(LogLevel.Error, "SERVICE", "There was a problem retrieving sync profiles");
						U.L(LogLevel.Error, "SERVICE", response);
						SynchronizeConfiguration = false;
					}
					else
					{
						string query;
						JArray configs = ParseResponse(response) as JArray;
						if (configs.Count == 0)
						{
							U.L(LogLevel.Debug, "SERVICE", "Need to create configuration");
							url = String.Format("/configurations.json");
							query = String.Format("?configuration[name]=Default&device[version]={0}",
								OAuth.Manager.UrlEncode(Settings.Manager.Release));
							response = SendRequest(url, "POST", query);
							if (response == null || response.StatusCode != HttpStatusCode.Created)
							{
								U.L(LogLevel.Error, "SERVICE", "There was a problem creating configuration");
								U.L(LogLevel.Error, "SERVICE", response);
								SynchronizeConfiguration = false;
							}
							else
							{
								Stream s = response.GetResponseStream();
								StreamReader sr = new StreamReader(s);
								string str = sr.ReadToEnd();
								JObject o = JObject.Parse(str);
								Identity.ConfigurationID = (uint)o["id"];
								U.L(LogLevel.Debug, "SERVICE", "Configuration was created successfully");
							}
							if (response != null) response.Close();
						}
						else
						{
							Identity.ConfigurationID = (uint)configs[0]["id"];
							JObject config = configs[0] as JObject;

							if (Settings.Manager.MediaState == MediaState.Playing)
								config.Remove("current_track");

							SyncProfileUpdated(config);
							
							// send current track
							if (config != null && Settings.Manager.CurrentTrack != null && (config["current_track"] == null || config["current_track"].Type != JTokenType.Object))
								PushConfigUpdate("current_track", TrackToJSON(Settings.Manager.CurrentTrack));

							// send media state
							PushConfigUpdate("media_state", Settings.Manager.MediaState == MediaState.Playing ? "Playing" : "Paused");
						}

						U.L(LogLevel.Debug, "SERVICE", String.Format("Binding device to sync profile {0} in cloud", Identity.ConfigurationID));
						url = String.Format("/devices/{0}.json", Identity.DeviceID);
						query = String.Format("?device[configuration_id]={0}",
							OAuth.Manager.UrlEncode(Identity.ConfigurationID.ToString()));
						if (response != null) response.Close();

						response = SendRequest(url, "PUT", query);
						if (response == null || response.StatusCode != HttpStatusCode.NoContent)
						{
							U.L(LogLevel.Error, "SERVICE", "There was a problem updating device data");
							U.L(LogLevel.Error, "SERVICE", response);
						}
						else
							U.L(LogLevel.Debug, "SERVICE", "Successfully told server of our current sync profile.");

						if (response != null) response.Close();
					}
					if (response != null) response.Close();
				}
				catch (Exception e)
				{
					U.L(LogLevel.Error, "SERVICE", "There was a problem setting up synchronization");
					U.L(LogLevel.Error, "SERVICE", e.Message);
					SynchronizeConfiguration = false;
				}
			};
			Thread th = new Thread(thread);
			th.Name = "Service thread";
			th.Priority = ThreadPriority.Lowest;
			th.Start();
		}

		/// <summary>
		/// Synchronize the playlists with the cloud.
		/// </summary>
		public static void SyncPlaylists()
		{
			if (!SynchronizePlaylists)
				return;

			ThreadStart thread = delegate()
			{
				try
				{
					U.L(LogLevel.Debug, "SERVICE", "Retrieving playlists");
					string url = String.Format("/me/Playlists.Manager.json");
					var response = SendRequest(url, "GET");
					if (response == null || response.StatusCode != HttpStatusCode.OK)
					{
						U.L(LogLevel.Error, "SERVICE", "There was a problem retrieving playlists");
						U.L(LogLevel.Error, "SERVICE", response);
					}
					else
					{
						JArray playlists = ParseResponse(response) as JArray;

						Dictionary<string, uint> cloudPlaylists = new Dictionary<string, uint>();

						foreach (JObject playlist in playlists)
						{
							MergePlaylistObject(playlist);
							cloudPlaylists.Add(playlist["name"].ToString(), Convert.ToUInt32(playlist["id"].ToString()));
						}

						List<uint> playlistsToRemove = new List<uint>();
						foreach (Playlist playlist in Settings.Manager.Playlists)
						{
							var cloud = playlist.ID > 0;
							var missing = !cloudPlaylists.Values.Contains<uint>(playlist.ID);
							var mine = Identity != null && playlist.OwnerID == Identity.UserID;

							if (!cloud)
								UploadPlaylist(playlist);
							else if (cloud && !mine)
								FollowPlaylist(playlist.ID);
							else if (cloud && missing)
								playlistsToRemove.Add(playlist.ID);
						}

						Playlists.Manager.PlaylistModified -= Playlists_PlaylistModified;
						foreach (uint id in playlistsToRemove)
							Playlists.Manager.Remove(id);
						Playlists.Manager.PlaylistModified += Playlists_PlaylistModified;
						
					}
					if (response != null) response.Close();

					U.L(LogLevel.Debug, "SERVICE", "Retrieving followed playlists");
					response = SendRequest("/me/playlists.json", "GET", "?follows=1");
					if (response == null || response.StatusCode != HttpStatusCode.OK)
					{
						U.L(LogLevel.Error, "SERVICE", "There was a problem retrieving followed playlists");
						U.L(LogLevel.Error, "SERVICE", response);
					}
					else
					{
						JArray playlists = ParseResponse(response) as JArray;
						foreach (JObject playlist in playlists)
							MergePlaylistObject(playlist);
					}
				}
				catch (Exception e)
				{
					U.L(LogLevel.Error, "SERVICE", "There was a problem synchronizing playlists");
					U.L(LogLevel.Error, "SERVICE", e.Message);
				}
			};
			Thread th = new Thread(thread);
			th.Name = "Playlist sync thread";
			th.Priority = ThreadPriority.Lowest;
			th.Start();
		}

		/// <summary>
		/// Initializes the oauth manager and acquires a request token if needed
		/// </summary>
		public static void InitOAuth()
		{
			oauth = new OAuth.Manager();
			oauth["consumer_key"] = U.StoffiKey;
			oauth["consumer_secret"] = U.StoffiSecret;
			if (Linked)
			{
				oauth["token"] = Settings.Manager.OAuthToken;
				oauth["token_secret"] = Settings.Manager.OAuthSecret;
				RetrieveUserData();
			}
			else
			{
				try
				{
					oauth.AcquireRequestToken(domain + "/oauth/request_token", "POST");
					oauth_request_token = oauth["token"];
				}
				catch (Exception e)
				{
					U.L(LogLevel.Warning, "SERVICE", "Problem linking with account: " + e.Message);
					Connected = false;
				}
			}
		}

		/// <summary>
		/// Checks if a given URL works.
		/// If not it will go to disconnected mode.
		/// </summary>
		/// <param name="url">The URL to check</param>
		public static void Ping(Uri url)
		{
			ThreadStart thread = delegate()
			{
				bool failed = !U.Ping(url);
				if (!failed)
					Connected = true;
				else if (failed && failedPings < 3)
				{
					failedPings++;
					Ping(url);
				}
				else if (failed)
					Connected = false;
			};
			Thread th = new Thread(thread);
			th.Name = "Ping thread";
			th.Priority = ThreadPriority.Lowest;
			th.Start();
		}

		/// <summary>
		/// Retrieves a playlist object and either creates it or
		/// update an already existing playlist.
		/// </summary>
		/// <remarks>
		/// Called when a playlist is created in the cloud and need to be
		/// pushed to the local set of Playlists.Manager.
		/// </remarks>
		/// <param name="playlist">The playlist object</param>
		public static Playlist MergePlaylistObject(JObject playlist)
		{
			string name = U.UnescapeHTML((string)playlist["name"]);
			string filter = null;
			if (playlist["filter"].Type == JTokenType.String)
				filter = U.UnescapeHTML((string)playlist["filter"]);
			uint id = Convert.ToUInt32(playlist["id"].ToString());
			uint owner = Convert.ToUInt32(playlist["user_id"].ToString());

			Playlist p = Playlists.Manager.Get(id);

			if (p != null)
			{
				// found playlist with same ID
				p.OwnerID = owner;
				Playlists.Manager.PlaylistRenamed -= Playlists_PlaylistRenamed;
				Playlists.Manager.Rename(id, name);
				Playlists.Manager.PlaylistRenamed += Playlists_PlaylistRenamed;
			}
			else
			{
				p = Playlists.Manager.Get(name);
				if (p != null)
				{
					// found playlist with same name
					p.ID = id;
					p.OwnerID = owner;
				}
				else
				{
					// no playlist found
					Playlists.Manager.PlaylistModified -= Playlists_PlaylistModified;
					p = Playlists.Manager.Create(name, id, owner);
					Playlists.Manager.PlaylistModified += Playlists_PlaylistModified;
				}
			}

			if (p != null) // playlist created/found
			{
				p.Filter = filter;
				var type = p.Type;
				var tracks = (JArray)playlist["songs"];
				if (tracks == null)
					tracks = (JArray)playlist["paginated_songs"];

				List<Track> tracksToAdd = new List<Track>();

				// import tracks from cloud to local
				List<string> pathsInCloud = new List<string>();
				if (tracks != null && type == PlaylistType.Standard)
				{
					foreach (JObject track in tracks)
					{
						Track t = JSONToTrack(track);
						if (t != null)
						{
							pathsInCloud.Add(t.Path);
							bool exists = false;
							foreach (Track tt in p.Tracks)
								if (tt.Path == t.Path)
								{
									exists = true;
									break;
								}
							if (!exists)
								tracksToAdd.Add(t);
						}
					}
				}

				// upload tracks from local to cloud
				var localTracks = new List<Track>();
				if (type == PlaylistType.Standard)
				{
					JArray tracksToCloud = new JArray();
					foreach (Track track in p.Tracks)
					{
						if (!pathsInCloud.Contains<string>(track.Path))
							localTracks.Add(track);
					}
					if (localTracks.Count > 0)
					{
						// our own playlist, so we upload all tracks not in cloud
						if (Identity != null && p.OwnerID == Identity.UserID)
						{
							foreach (var track in localTracks)
							{
								JObject t = TrackToJSON(track);
								if (t != null)
									tracksToCloud.Add(t);
							}
							var json = new JObject();
							json["songs"] = new JObject();
							json["songs"]["added"] = tracksToCloud;
							lock (syncOutLocker)
								syncOutBuffer.Add(new SyncOperation("update", "playlists", p.ID, json));
							InitiateSyncTimer();
						}

						// someone else's playlist, so we remove tracks not in cloud
						else
						{
							foreach (var track in localTracks)
								p.Tracks.Remove(track);
						}
					}
				}

				if (tracksToAdd.Count > 0)
				{
					DispatchModifyTracks(p.Tracks, new ModifiedEventArgs(ModifyType.Added, tracksToAdd));
				}

				p.Tracks.CollectionChanged += Playlist_CollectionChanged;
			}

			return p;
		}

		/// <summary>
		/// Deletes a given link to a third party service provider.
		/// </summary>
		/// <param name="provider">The name of the provider</param>
		public static void DeleteLink(string provider)
		{
			try
			{
				foreach (Link l in Identity.Links)
				{
					if (l.Connected && l.Provider == provider)
					{
						DeleteLink(l);
						return;
					}
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "SERVICE", "Could not delete link: " + e.Message);
			}
		}

		/// <summary>
		/// Deletes a given link to a third party service provider.
		/// </summary>
		/// <param name="url">The URL to the link object</param>
		public static void DeleteLink(Uri url)
		{
			try
			{
				foreach (Link l in Identity.Links)
				{
					if (l.Connected && l.URL == url.AbsoluteUri)
					{
						DeleteLink(l);
						return;
					}
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "SERVICE", "Could not delete link: " + e.Message);
			}
		}

		/// <summary>
		/// Deletes a given link to a third party service provider.
		/// </summary>
		/// <param name="link">The link to be deleted</param>
		public static void DeleteLink(Link link)
		{
			try
			{
				var response = SendRequest(link.URL + ".json", "DELETE");
				if (response == null || response.StatusCode != HttpStatusCode.NoContent)
				{
					U.L(LogLevel.Error, "SERVICE", "There was a problem deleting link to " + link.Provider);
					U.L(LogLevel.Error, "SERVICE", response);
				}
				else
				{
					U.L(LogLevel.Information, "SERVICE", "Link to " + link.Provider + " deleted");
				}
				if (response != null) response.Close();
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "SERVICE", "Could not delete link to "+link.Provider+": " + e.Message);
			}
		}

		/// <summary>
		/// Updates the properties of a link object at the server.
		/// </summary>
		/// <param name="link">The link to update</param>
		public static void UpdateLink(Link link)
		{
			try
			{
				string url = String.Format("{0}.json", link.URL);
				string query = "?";
				query += U.CreateParam("do_share", link.DoShare, "link") + "&";
				query += U.CreateParam("do_listen", link.DoListen, "link") + "&";
				query += U.CreateParam("do_donate", link.DoDonate, "link") + "&";
				query += U.CreateParam("do_create_playlist", link.DoCreatePlaylist, "link");
				var response = SendRequest(url, "PUT", query);

				if (response == null || response.StatusCode != HttpStatusCode.OK)
				{
					U.L(LogLevel.Error, "SERVICE", "There was a problem updating link to " + link.Provider);
					U.L(LogLevel.Error, "SERVICE", response);
				}
				else
				{
					U.L(LogLevel.Debug, "SERVICE", "Updated link to " + link.Provider);
				}
				if (response != null) response.Close();
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "SERVICE", "Could not update link to "+link.Provider+": " + e.Message);
			}
		}

		/// <summary>
		/// Retrieves a given link object.
		/// </summary>
		/// <param name="provider">The name of the provider of the link</param>
		/// <returns>The link object if it could be found, otherwise null</returns>
		public static Link GetLink(string provider)
		{
			try
			{
				foreach (Link l in Identity.Links)
					if (l.Provider == provider)
						return l;
			}
			catch { }
			return null;
		}

		/// <summary>
		/// Retrieves a given link object.
		/// </summary>
		/// <param name="id">The ID of the link</param>
		/// <returns>The link object if it could be found, otherwise null</returns>
		public static Link GetLink(int id)
		{
			try
			{
				foreach (Link l in Identity.Links)
					if (l.ID == id)
						return l;
			}
			catch { }
			return null;
		}

		/// <summary>
		/// Updates the error message on a link.
		/// </summary>
		/// <param name="id">The ID of the link</param>
		/// <param name="message">The error message</param>
		public static void LinkError(int id, string message)
		{
			Link l = GetLink(id);
			if (l != null)
				l.Error = message;
		}
		
		#endregion

		#region Private

		/// <summary>
		/// Retrieves the data of the currently linked user.
		/// </summary>
		public static void RetrieveUserData()
		{
			if (!Linked) return;

			ThreadStart cloudThread = delegate()
			{
				U.L(LogLevel.Debug, "SERVICE", "Fetching user data");
				string url = String.Format("/me.json");
				var response = SendRequest(url, "GET", "");
				if (response == null || response.StatusCode != HttpStatusCode.OK)
				{
					U.L(LogLevel.Error, "SERVICE", "There was a problem retrieving user data");
					U.L(LogLevel.Error, "SERVICE", response);
					if (Connected || U.Ping(domain))
						Delink();
				}
				else
				{
					try
					{
						JObject o = ParseResponse(response) as JObject;
						userID = (uint)o["id"];
						UserName = (string)o["display"];
						if (!Settings.Manager.HasCloudIdentity(userID))
							Settings.Manager.CloudIdentities.Add(new Identity
							{
								UserID = userID,
								SynchronizeConfig = true,
								SynchronizePlaylists = true,
								Synchronize = true
							});
						RegisterDevice();
					}
					catch (Exception e)
					{
						U.L(LogLevel.Error, "SERVICE", "Could not retrieve user data: " + e.Message);
						Delink();
					}
				}
				if (response != null) response.Close();
			};
			Thread cl_thread = new Thread(cloudThread);
			cl_thread.Name = "Cloud user thread";
			cl_thread.Priority = ThreadPriority.Lowest;
			cl_thread.Start();
		}

		/// <summary>
		/// Retrieves the links to third parties for the currently linked user.
		/// </summary>
		public static void RetrieveLinkData()
		{
			if (!Linked) return;
			
			// wait for device to be registered
			for (int i = 0; i < 10; i++)
			{
				if (Identity != null && Identity.DeviceID > 0)
					break;
				Thread.Sleep(500);
			}
			if (Identity == null || Identity.DeviceID < 1)
				return;

			ThreadStart cloudThread = delegate()
			{
				U.L(LogLevel.Debug, "SERVICE", "Fetching link data");
				string url = String.Format("/links.json");
				var response = SendRequest(url, "GET", "");
				if (response == null || response.StatusCode != HttpStatusCode.OK)
				{
					U.L(LogLevel.Error, "SERVICE", "There was a problem retrieving link data");
					U.L(LogLevel.Error, "SERVICE", response);
					Delink();
				}
				else
				{
					try
					{
						JObject o = ParseResponse(response) as JObject;
						Identity.Links.Clear();

						JArray connected = (JArray)o["connected"];
						foreach (JObject link in connected)
							Identity.Links.Add(CreateLink(true, link));

						JArray notConnected = (JArray)o["not_connected"];
						foreach (JObject link in notConnected)
							Identity.Links.Add(CreateLink(false, link));

						DispatchPropertyChanged("Links", null, Identity.Links, true);
						SyncConfig();
						SyncPlaylists();
					}
					catch (Exception e)
					{
						U.L(LogLevel.Error, "SERVICE", "Could not retrieve link data for user: " + e.Message);
						Delink();
					}
				}
				if (response != null) response.Close();
			};
			Thread cl_thread = new Thread(cloudThread);
			cl_thread.Name = "Cloud link thread";
			cl_thread.Priority = ThreadPriority.Lowest;
			cl_thread.Start();
		}

		/// <summary>
		/// Registers the device with the server
		/// </summary>
		private static void RegisterDevice()
		{
			if (!Linked) return;

			ThreadStart cloudThread = delegate()
			{
				try
				{
					if (Identity != null && Identity.DeviceID > 0)
					{
						U.L(LogLevel.Debug, "SERVICE", "Device already registered, verifying ID");
						string url = String.Format("/devices/{0}.json", Identity.DeviceID);
						var response = SendRequest(url, "GET");
						if (response == null || response.StatusCode != HttpStatusCode.OK)
						{
							Identity.DeviceID = 0;
							U.L(LogLevel.Debug, "SERVICE", "Previous registered ID was invalid");
							RegisterDevice();
						}
						else
						{
							Stream s = response.GetResponseStream();
							StreamReader sr = new StreamReader(s);
							string str = sr.ReadToEnd();
							JObject o = JObject.Parse(str);
							string name = (string)o["name"];
							U.L(LogLevel.Debug, "SERVICE", "ID verified, device name is " + name);
							deviceName = name;
							DeviceName = name;
							DispatchPropertyChanged("Linked", false, true);
						}
						if (response != null) response.Close();
					}
					else
					{
						string name = U.Capitalize(Environment.MachineName);
						if (Settings.Manager.Channel != "Stable")
							name += " " + U.Capitalize(Settings.Manager.Channel);
#if (DEBUG)
						name += " Dev";
#endif

						U.L(LogLevel.Debug, "SERVICE", "Need to register the device");
						string url = String.Format("/devices.json");
						string query = String.Format("?device[name]={0}&device[version]={1}",
							OAuth.Manager.UrlEncode(name),
							OAuth.Manager.UrlEncode(Settings.Manager.Release));
						var response = SendRequest(url, "POST", query);
						if (response == null || response.StatusCode != HttpStatusCode.Created)
						{
							U.L(LogLevel.Error, "SERVICE", "There was a problem registering the device");
							U.L(LogLevel.Error, "SERVICE", response);
							Delink();
						}
						else
						{
							Stream s = response.GetResponseStream();
							StreamReader sr = new StreamReader(s);
							string str = sr.ReadToEnd();
							JObject o = JObject.Parse(str);
							Identity.DeviceID = (uint)o["id"];
							U.L(LogLevel.Debug, "SERVICE", "Device has been registered with ID " + Identity.DeviceID.ToString());
							deviceName = name;
							DeviceName = name;
							DispatchPropertyChanged("Linked", false, true);
						}
						if (response != null) response.Close();
					}
				}
				catch (Exception e)
				{
					U.L(LogLevel.Error, "SERVICE", "Could not register device: " + e.Message);
					Delink();
				}
			};
			Thread cl_thread = new Thread(cloudThread);
			cl_thread.Name = "Cloud register thread";
			cl_thread.Priority = ThreadPriority.Lowest;
			cl_thread.Start();
		}

		/// <summary>
		/// Uploads a playlist to the cloud.
		/// </summary>
		/// <param name="playlist">The playlist to upload</param>
		private static void UploadPlaylist(Playlist playlist)
		{
			if (playlist == null) return;


			JObject para = new JObject();
			para["name"] = playlist.Name;
			para["is_public"] = "0";

			switch (playlist.Type)
			{
				case PlaylistType.Standard:
					JArray tracks = new JArray();
					while (true)
					{
						try
						{
							tracks.Clear();
							foreach (var track in playlist.Tracks)
							{
								var t = TrackToJSON(track);
								if (t != null)
									tracks.Add(t);
							}
							break;
						}
						catch { }
					}
					para["songs"] = tracks;
					break;

				case PlaylistType.Dynamic:
					para["filter"] = playlist.Filter ?? "";
					break;
			}

			lock (syncOutLocker)
				syncOutBuffer.Add(new SyncOperation("create", "playlists", para));

			InitiateSyncTimer();
		}

		/// <summary>
		/// Will update Stoffi's current configuration according to the
		/// state of the synchronization profile in the cloud.
		/// </summary>
		/// <param name="updatedParams">The updated parameters of the configuration.</param>
		private static void SyncProfileUpdated(JObject updatedParams)
		{
			Settings.Manager.PropertyChanged -= Settings_PropertyChanged;

			#region Media state
			//if (updatedParams["media_state"] != null)
			//{
			//    try
			//    {
			//        switch ((string)updatedParams["media_state"])
			//        {
			//            case "Playing":
			//                Media.Manager.Play();
			//                break;

			//            case "Paused":
			//                Media.Manager.Pause();
			//                break;

			//            case "Stopped":
			//                Media.Manager.Stop();
			//                break;
			//        }
			//    }
			//    catch (Exception e)
			//    {
			//        U.L(LogLevel.Error, "SERVICE", "Problem synchronizing media state: " + e.Message);
			//    }
			//}
			#endregion

			#region Shuffle
			if (updatedParams["shuffle"] != null && updatedParams["shuffle"].Type == JTokenType.String)
			{
				try
				{
					switch (((string)updatedParams["shuffle"]).ToLower())
					{
						case "noshuffle":
						case "off":
							Settings.Manager.Shuffle = false;
							break;

						case "random":
						case "on":
							Settings.Manager.Shuffle = true;
							break;
					}
				}
				catch (Exception e)
				{
					U.L(LogLevel.Error, "SERVICE", "Problem synchronizing shuffle mode: " + e.Message);
				}
			}
			#endregion

			#region Repeat
			if (updatedParams["repeat"] != null && updatedParams["repeat"].Type == JTokenType.String)
			{
				try
				{
					switch (((string)updatedParams["repeat"]).ToLower())
					{
						case "norepeat":
							Settings.Manager.Repeat = RepeatState.NoRepeat;
							break;

						case "repeatall":
							Settings.Manager.Repeat = RepeatState.RepeatAll;
							break;

						case "repeatone":
							Settings.Manager.Repeat = RepeatState.RepeatOne;
							break;
					}
				}
				catch (Exception e)
				{
					U.L(LogLevel.Error, "SERVICE", "Problem synchronizing repeat mode: " + e.Message);
				}
			}
			#endregion

			#region Volume
			if (updatedParams["volume"] != null)
			{
				try
				{
					float vol = (float)updatedParams["volume"];
					Settings.Manager.Volume = Convert.ToDouble(vol);
				}
				catch (Exception e)
				{
					try
					{
						string vol = (string)updatedParams["volume"];
						Settings.Manager.Volume = Convert.ToDouble(vol);
					}
					catch (Exception ee)
					{
						U.L(LogLevel.Error, "SERVICE", "Problem synchronizing volume");
						U.L(LogLevel.Error, "SERVICE", e.Message);
						U.L(LogLevel.Error, "SERVICE", ee.Message);
					}
				}
			}
			#endregion

			#region Current track
			if (updatedParams["current_track"] != null && updatedParams["current_track"].Type == JTokenType.Object)
			{
				new Thread(delegate()
				{
					try
					{
						JObject json = updatedParams["current_track"] as JObject;
						if (json != null)
						{
							Track track = JSONToTrack(json);
							if (track != null && (Settings.Manager.CurrentTrack == null || track.Path != Settings.Manager.CurrentTrack.Path))
							{
								Media.Manager.Load(track);
								Settings.Manager.CurrentTrack = track;
							}
						}
					}
					catch (Exception e)
					{
						U.L(LogLevel.Error, "SERVICE", "Could not read current track from server");
						U.L(LogLevel.Error, "SERVICE", e.Message);
					}
				}) { Name = "Sync current track", Priority = ThreadPriority.Lowest }.Start();
			}
			#endregion

			Settings.Manager.PropertyChanged += Settings_PropertyChanged;
		}

		/// <summary>
		/// Sends a request to the server
		/// </summary>
		/// <param name="url">The base url</param>
		/// <param name="method">The HTTP method to use</param>
		/// <param name="query">The query string, encoded properly</param>
		/// <param name="body">The body of the request, encoded as JSON</param>
		/// <returns>The response from the server</returns>
		private static HttpWebResponse SendRequest(string url, string method, string query = "", string body = "")
		{
			if (Settings.Manager.HasCloudIdentity(userID))
			{
				if (query == null || query == "") query = "?";
				else query += "&";
				query += "device_id=" + Identity.DeviceID.ToString();
			}

			string fullUrl = url + query;
			if (!fullUrl.StartsWith("http") && !fullUrl.StartsWith("/"))
				fullUrl = domain + "/" + fullUrl;
			else if (!fullUrl.StartsWith("http"))
				fullUrl = domain + fullUrl;
			//U.L(LogLevel.Debug, "SERVICE", "Sending " + method + " request to " + fullUrl);

			try
			{
				var request = (HttpWebRequest)WebRequest.Create(fullUrl);

				lock (sendLocker)
				{
					var authzHeader = oauth.GenerateAuthzHeader(fullUrl, method);
					request.Method = method;
					request.PreAuthenticate = true;
					request.AllowWriteStreamBuffering = true;
					request.Headers.Add("Authorization", authzHeader);
					if (!String.IsNullOrWhiteSpace(RealTimeSessionID))
						request.Headers.Add("X-Session-ID", RealTimeSessionID);
					request.Timeout = 30000;
					request.KeepAlive = false;

					if (body != null && body != "" && body != "[]" && body != "{}")
					{
						ASCIIEncoding encoding = new ASCIIEncoding();
						byte[] data = encoding.GetBytes(body);

						//request.ContentType = "application/json";
						request.ContentLength = data.Length;

						Stream bodyStream = request.GetRequestStream();
						bodyStream.Write(data, 0, data.Length);
						bodyStream.Close();
					}
				}
				HttpWebResponse resp = (HttpWebResponse)request.GetResponse();
				return resp;
			}
			catch (WebException exc)
			{
				if (exc.Message == "The operation has timed out")
					Connected = false;
				U.L(LogLevel.Error, "SERVICE", "Error while contacting cloud server: " + exc.Message);
				return null;
			}
		}

		/// <summary>
		/// Parses a web response encoded in JSON into an object.
		/// </summary>
		/// <param name="response">The JSON web response.</param>
		/// <returns>A JSON object.</returns>
		private static JToken ParseResponse(HttpWebResponse response)
		{
			try
			{
				Stream s = response.GetResponseStream();
				StreamReader sr = new StreamReader(s);
				string str = sr.ReadToEnd();
				sr.Close();
				return JToken.Parse(str);
			}
			catch (Exception e)
			{
				U.L(LogLevel.Error, "SERVICE", "Could not parse response: " + e.Message);
				return null;
			}
		}

		/// <summary>
		/// Encodes a track object into HTTP parameters.
		/// </summary>
		/// <param name="track">The track to encode</param>
		/// <param name="prefix">An optional prefix to put on each property</param>
		/// <returns>The track encoded as title=X, artist=Y, genre=Z...</returns>
		private static List<string> EncodeTrack(Track track, string prefix = "")
		{
			List<string> paras = new List<string>();
			if (track != null)
			{
				paras.Add(U.CreateParam("title", track.Title, prefix));
				paras.Add(U.CreateParam("artist", track.Artist, prefix));
				paras.Add(U.CreateParam("art_url", track.OriginalArtURL, prefix));
				paras.Add(U.CreateParam("foreign_url", track.URL, prefix));
				paras.Add(U.CreateParam("genre", track.Genre, prefix));
				paras.Add(U.CreateParam("length", track.Length, prefix));

				switch (track.Type)
				{
					case TrackType.File:
						paras.Add(U.CreateParam("path", Path.GetFileName(track.Path), prefix));
						break;

					default:
						paras.Add(U.CreateParam("path", track.Path, prefix));
						break;
				}
			}
			return paras;
		}

		/// <summary>
		/// Encodes a track to a JSON object.
		/// </summary>
		/// <param name="track">The track to encode</param>
		/// <returns>A JSON object</returns>
		private static JObject TrackToJSON(Track track)
		{
			JObject json = new JObject();
			if (track != null)
			{
				json.Add("title", U.EscapeJSON(track.Title));
				json.Add("artist", U.EscapeJSON(track.Artist));
				json.Add("art_url", U.EscapeJSON(track.ArtURL));
				json.Add("foreign_url", U.EscapeJSON(track.URL));
				json.Add("genre", U.EscapeJSON(track.Genre));
				json.Add("length", U.EscapeJSON(track.Length));

				switch (track.Type)
				{
					case TrackType.File:
						json.Add("path", U.EscapeJSON(Path.GetFileName(track.Path)));
						break;

					default:
						json.Add("path", U.EscapeJSON(track.Path));
						break;
				}
			}
			return json;
		}

		/// <summary>
		/// Converts a JSON object to a track.
		/// </summary>
		/// <param name="track">The JSON object describing the track</param>
		/// <returns>A track if such can be created or found, otherwise null</returns>
		private static Track JSONToTrack(JObject track)
		{
			string path = U.UnescapeHTML(track["path"].ToString());
			double length = -1;
			try
			{
				length = Convert.ToDouble(track["length"].ToString());
			}
			catch { }
			
			Track t = null;
			if (path != null)
			{
				switch (Track.GetType(path))
				{
					case TrackType.File:
						t = Files.GetTrack(path, length);
						break;

					case TrackType.SoundCloud:
						t = Sources.Manager.SoundCloud.CreateTrack(path);
						break;

					case TrackType.Jamendo:
						t = Sources.Manager.Jamendo.CreateTrack(path);
						break;

					case TrackType.YouTube:
						t = Sources.Manager.YouTube.CreateTrack(path);
						break;

					case TrackType.WebRadio:
						t = Media.Manager.ParseURL(path);
						break;
				}
			}
			return t;
		}

		/// <summary>
		/// Pushes a configuration change to the synchronization buffer.
		/// </summary>
		/// <param name="key">The key that was changed.</param>
		/// <param name="value">The new value of the key.</param>
		private static void PushConfigUpdate(string key, string value)
		{
			JToken token = (JToken)OAuth.Manager.UrlEncode(value);
			PushConfigUpdate(key, token);
		}

		/// <summary>
		/// Pushes a configuration change to the synchronization buffer.
		/// </summary>
		/// <param name="key">The key that was changed.</param>
		/// <param name="value">The new value of the key.</param>
		private static void PushConfigUpdate(string key, JToken value)
		{
			lock (syncOutLocker)
			{
				bool found = false;
				for (int i = 0; i < syncOutBuffer.Count; i++)
				{
					SyncOperation op = syncOutBuffer[i];
					if (op.Command == "update" && op.ObjectType == "configurations" && op.ObjectID == Identity.ConfigurationID)
					{
						found = true;
						op.Params[key] = value;
					}
				}
				if (!found)
				{
					JObject para = new JObject();
					para[key] = value;
					syncOutBuffer.Add(new SyncOperation("update", "configurations", Identity.ConfigurationID, para));
				}
			}

			InitiateSyncTimer();
		}

		/// <summary>
		/// Starts or resets the synchronization timer.
		/// </summary>
		private static void InitiateSyncTimer()
		{
			if (syncOutDelay != null)
				syncOutDelay.Dispose();
			syncOutDelay = new Timer(PerformSyncOut, null, 500, Timeout.Infinite);
		}

		/// <summary>
		/// Check whether or not a given size of album art should be cached.
		/// </summary>
		/// <param name="size">The size of the art image (small, medium, large or huge)</param>
		/// <returns>True if the user has allowed caching of the size, otherwise false</returns>
		private static bool FetchArtSize(string size)
		{
			return (
				(size == "small" && Settings.Manager.DownloadAlbumArtSmall) ||
				(size == "medium" && Settings.Manager.DownloadAlbumArtMedium) ||
				(size == "large" && Settings.Manager.DownloadAlbumArtLarge) ||
				(size == "huge" && Settings.Manager.DownloadAlbumArtHuge));
		}

		/// <summary>
		/// Gets the folder for the album art of a single track.
		/// </summary>
		/// <returns>The folder for track's art.</returns>
		/// <param name="track">Track.</param>
		private static string ArtFolderForTrack(Track track)
		{
			return Path.Combine(artFolder, U.C(track.Artist), "Singles");
		}

		/// <summary>
		/// Looks for a cached album art image inside the path.
		/// </summary>
		/// <param name="path">The path in which to look for images</param>
		/// <param name="size">The prefered size of the image</param>
		/// <returns>The best matching image or the default album art</returns>
		private static string FetchCachedImages(string path, string size = "large")
		{
			if (Directory.Exists(path))
			{
				string ret = "";
				foreach (string file in Directory.GetFiles(path))
				{
					if (Path.GetFileNameWithoutExtension(file) == size)
						return file;
					ret = file;
				}
				if (ret != "")
					return ret;
			}
			return defaultArt;
		}

		/// <summary>
		/// Fetch the image of an artist on Last.fm.
		/// </summary>
		/// <param name="artist">The name of the artist</param>
		/// <param name="size">The prefered size</param>
		/// <param name="cache">If true then the images will be downloaded according to the cache settings</param>
		/// <returns>The best matching art path and url, or the default album art</returns>
		private static string[] FetchLastFMArtistArt(string artist, string size = "large", bool cache = true)
		{
			return FetchLastFMObjectArt(String.Format("http://ws.audioscrobbler.com/2.0?method=artist.info&format=json&api_key={0}&artist={1}",
				U.LastFMKey, OAuth.Manager.UrlEncode(artist)), size, cache);
		}

		/// <summary>
		/// Fetch the art for an album on Last.fm.
		/// </summary>
		/// <param name="album">The name of the album</param>
		/// <param name="artist">The name of the artist</param>
		/// <param name="size">The prefered size</param>
		/// <param name="cache">If true then the images will be downloaded according to the cache settings</param>
		/// <returns>The best matching art path and url, or the default album art</returns>
		private static string[] FetchLastFMAlbumArt(string album, string artist, string size = "large", bool cache = true)
		{
			return FetchLastFMObjectArt(String.Format("http://ws.audioscrobbler.com/2.0?method=album.info&format=json&api_key={0}&album={1}&artist={2}",
				U.LastFMKey, OAuth.Manager.UrlEncode(album), OAuth.Manager.UrlEncode(artist)), size, cache);
		}

		/// <summary>
		/// Fetch the art for a track on Last.fm.
		/// </summary>
		/// <param name="title">The title of the track</param>
		/// <param name="artist">The name of the artist</param>
		/// <param name="size">The prefered size</param>
		/// <param name="cache">If true then the images will be downloaded according to the cache settings</param>
		/// <returns>The best matching art path and url, or the default album art</returns>
		private static string[] FetchLastFMTrackArt(string title, string artist, string size = "large", bool cache = true)
		{
			return FetchLastFMObjectArt(String.Format("http://ws.audioscrobbler.com/2.0?method=track.info&format=json&api_key={0}&track={1}&artist={2}",
				U.LastFMKey, OAuth.Manager.UrlEncode(title), OAuth.Manager.UrlEncode(artist)), size, cache);
		}

		/// <summary>
		/// Fetch the album art for an artist, album or track on Last.fm.
		/// </summary>
		/// <param name="url">The full URL to the object</param>
		/// <param name="size">The prefered size</param>
		/// <param name="cache">If true then the images will be downloaded according to the cache settings</param>
		/// <returns>The best matching album art path and url, or the default album art</returns>
		private static string[] FetchLastFMObjectArt(string url, string size = "large", bool cache = true)
		{
			try
			{
				var request = (HttpWebRequest)WebRequest.Create(url);
				using (var response = (HttpWebResponse)request.GetResponse())
				using (var reader = new StreamReader(response.GetResponseStream()))
				{
					string[] ret = FetchLastFMImages(JObject.Parse(reader.ReadToEnd()), size, cache);
					if (ret[0] != defaultArt)
						return ret;
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Information, "SERVICE", "Could not fetch album art from Last.fm: " + e.Message);
			}
			return new string[] { defaultArt, "" };
		}

		/// <summary>
		/// Fetches the images from a JSON object representing an album, artist or track.
		/// The images will be downloaded into the cache and the path to the image best
		/// matching the prefered size will be returned.
		/// </summary>
		/// <param name="json">The artist, album or track</param>
		/// <param name="size">The prefered size to return</param>
		/// <param name="cache">If true then the images will be downloaded according to the cache settings</param>
		/// <returns>The path and url to the image best matching the size</returns>
		private static string[] FetchLastFMImages(JObject json, string size = "large", bool cache = true)
		{
			JArray images = null;
			string path = "";
			if (json["artist"] != null && json["artist"].Type == JTokenType.Object)
			{
				images = (JArray)json["artist"]["image"];
				path = Path.Combine(artFolder, U.C(json["artist"]["name"]));
			}
			else if (json["album"] != null && json["album"].Type == JTokenType.Object)
			{
				images = (JArray)json["album"]["image"];
				path = Path.Combine(artFolder, U.C(json["album"]["artist"]), U.C(json["album"]["name"]));
			}
			else if (json["track"] != null && json["track"].Type == JTokenType.Object)
			{
				if (json["track"]["album"] != null && json["track"]["album"].Type == JTokenType.Object)
				{
					images = (JArray)json["track"]["album"]["image"];
					path = Path.Combine(artFolder, U.C(json["track"]["album"]["artist"]), U.C(json["track"]["album"]["title"]));
				}
				else if (json["track"]["artist"] != null && json["track"]["artist"].Type == JTokenType.Object)
				{
					images = (JArray)json["track"]["artist"]["image"];
					path = Path.Combine(artFolder, U.C(json["track"]["artist"]["name"]), "Singles", U.C(json["track"]["name"]));
				}
			}

			if (images != null)
			{
				bool foundExact = false;
				WebClient client = new WebClient();
				string[] ret = new string[] { "", "" };
				foreach (JObject image in images)
				{
					try
					{
						string url = (string)image["#text"];
						if (url != "")
						{
							string ext = url.Substring(url.LastIndexOf('.'));
							string sz = (string)image["size"];
							if (sz == "mega") sz = "huge";
							Directory.CreateDirectory(path);
							string p = Path.Combine(path, sz + ext);
							if (cache && FetchArtSize(sz) && !File.Exists(p))
							{
								client.DownloadFile(url, p);
								OnAlbumArtCacheChanged();
							}
							if (!foundExact)
							{
								ret[0] = cache ? p : url;
								ret[1] = url;
							}
							if (sz == size)
								foundExact = true;
						}
					}
					catch (Exception e)
					{
						U.L(LogLevel.Information, "SERVICE", "Could not download album art from Last.fm: " + e.Message);
					}
				}
				if (!String.IsNullOrWhiteSpace(ret[0]))
					return ret;
			}

			return new string[] { defaultArt, "" };
		}

		/// <summary>
		/// Starts or resets the listen delay timer.
		/// </summary>
		private static void InitiateListenTimer()
		{
			if (listenDelay != null)
				listenDelay.Dispose();
			listenDelay = new Timer(PerformStartListen, null, 2000, Timeout.Infinite);
		}

		/// <summary>
		/// Starts or resets the reconnect timer.
		/// </summary>
		private static void InitiateReconnectTimer()
		{
			if (reconnectTimer != null)
				reconnectTimer.Dispose();
			reconnectTimer = new Timer(PerformReconnect, null, 15000, 15000);
		}

		/// <summary>
		/// Queue a submission of the current track as being listened to.
		/// </summary>
		private static void StartListen()
		{
			if (!Linked || !Settings.Manager.SubmitSongs) return;
			InitiateListenTimer();
		}

		/// <summary>
		/// Update the expected end time for the current listen.
		/// </summary>
		/// <param name="endTime">The time that the listen is expected to end</param>
		private static void UpdateListen(DateTime endTime)
		{
			if (!Linked || !Settings.Manager.SubmitSongs || idOfCurrentListen <= 0) return;
			uint id = idOfCurrentListen;

			ThreadStart listenThread = delegate()
			{
				try
				{
					string url = String.Format("/listens/{0}.json", id);
					string query = String.Format("?listen[ended_at]={0}", endTime.ToUniversalTime().ToString("yyyyMMddHHmmss"));
					var response = SendRequest(url, "PUT", query);

					if (response == null || response.StatusCode != HttpStatusCode.NoContent)
					{
						U.L(LogLevel.Error, "SERVICE", "There was a problem updating listen");
						if (response == null)
						{
							// overwrite previous PUTs but preserve previous DELETEs
							if (!Settings.Manager.ListenBuffer.ContainsKey(url) || Settings.Manager.ListenBuffer[url].Item1 != "DELETE")
							{
								Settings.Manager.ListenBuffer[url] = new Tuple<string, string>("PUT", query);
								CheckListenRetry();
							}
						}
						else
							U.L(LogLevel.Error, "SERVICE", response);
					}
					else
					{
						U.L(LogLevel.Debug, "SERVICE", "Updated listen successfully");
					}
					if (response != null) response.Close();
				}
				catch (Exception e)
				{
					U.L(LogLevel.Error, "SERVICE", "There was a problem updating listen");
					U.L(LogLevel.Error, "SERVICE", e.Message);
				}
			};
			Thread l_thread = new Thread(listenThread);
			l_thread.Name = "Listen thread";
			l_thread.Priority = ThreadPriority.Lowest;
			l_thread.Start();
		}

		/// <summary>
		/// End the listen with a given ID.
		/// </summary>
		/// <param name="id">The ID of the listen to end.</param>
		private static void EndListen(uint id)
		{
			if (!Linked || !Settings.Manager.SubmitSongs || id <= 0) return;

			ThreadStart listenThread = delegate()
			{
				try
				{
					string url = String.Format("/listens/{0}/end.json", id);
					var response = SendRequest(url, "POST");

					if (response == null || response.StatusCode != HttpStatusCode.Created)
					{
						U.L(LogLevel.Error, "SERVICE", "There was a problem ending listen");
						if (response == null)
						{
							Settings.Manager.ListenBuffer[url] = new Tuple<string, string>("POST", "");
							CheckListenRetry();
						}
						else
							U.L(LogLevel.Error, "SERVICE", response);
					}
					else
					{
						U.L(LogLevel.Debug, "SERVICE", "Ended listen successfully");
					}
					if (response != null) response.Close();
				}
				catch (Exception e)
				{
					U.L(LogLevel.Error, "SERVICE", "There was a problem ending listen");
					U.L(LogLevel.Error, "SERVICE", e.Message);
				}
			};
			Thread l_thread = new Thread(listenThread);
			l_thread.Name = "Listen thread";
			l_thread.Priority = ThreadPriority.Lowest;
			l_thread.Start();
		}

		/// <summary>
		/// Delete the listen with a given ID.
		/// </summary>
		/// <param name="id">The ID of the listen to delete</param>
		private static void DeleteListen(uint id)
		{
			if (!Linked || !Settings.Manager.SubmitSongs || id <= 0) return;

			ThreadStart listenThread = delegate()
			{
				try
				{
					string url = String.Format("/listens/{0}.json", id);
					var response = SendRequest(url, "DELETE");

					if (response == null || response.StatusCode != HttpStatusCode.NoContent)
					{
						U.L(LogLevel.Error, "SERVICE", "There was a problem deleting listen");
						if (response == null)
						{
							Settings.Manager.ListenBuffer[url] = new Tuple<string, string>("DELETE", "");
							CheckListenRetry();
						}
						else
							U.L(LogLevel.Error, "SERVICE", response);
					}
					else
					{
						U.L(LogLevel.Debug, "SERVICE", "Deleted listen successfully");
					}
					if (response != null) response.Close();
				}
				catch (Exception e)
				{
					U.L(LogLevel.Error, "SERVICE", "There was a problem deleting listen");
					U.L(LogLevel.Error, "SERVICE", e.Message);
				}
			};
			Thread l_thread = new Thread(listenThread);
			l_thread.Name = "Listen thread";
			l_thread.Priority = ThreadPriority.Lowest;
			l_thread.Start();
		}

		/// <summary>
		/// Starts the retransmission of failed listen submission if needed.
		/// </summary>
		private static void CheckListenRetry()
		{
			if (Settings.Manager.ListenBuffer.Count == 0)
				listenRetryStep = -1;
			else if (listenRetryStep < 0)
				listenRetryStep = 0;

			if (listenRetryStep < 0)
			{
				if (listenRetry != null)
					listenRetry.Dispose();
				listenRetry = null;
			}
			else if (listenRetry == null)
			{
				listenRetry = new Timer(PerformRetryListen, null, listenRetrySteps[listenRetryStep] * 1000, Int32.MaxValue);
			}
		}

		/// <summary>
		/// Creates a link according to a JSON object.
		/// </summary>
		/// <param name="connected">Whether or not the link is connected or not</param>
		/// <param name="link">The JSON object describing the link</param>
		/// <returns>The Link object corresponding to the JSON object</returns>
		private static Link CreateLink(bool connected, JObject link)
		{
			try
			{
			if (connected)
			{
				Link l = new Link();
				UpdateLink(l, true, link);
				l.ConnectURL = String.Format("{0}?origin=%2Fdashboard", link["connectURL"]);
				return l;
			}
			else
			{
				return new Link()
				{
					Provider = (string)link["display"],
					URL = (string)link["url"],
					ConnectURL = (string)link["url"],
					Connected = false
				};
			}
			}
			catch (Exception e)
			{
			    U.L(LogLevel.Warning, "SERVICE", "Could not create link object: " + e.Message);
			    return null;
			}
		}

		/// <summary>
		/// Updates a link object.
		/// </summary>
		/// <param name="link">The link object to be updated</param>
		/// <param name="connected">Whether the link is connected or not</param>
		/// <param name="json">A JSON object describing the link (cannot be null if connected = true)</param>
		/// <param name="forceDefaults">Whether or not the boolean properties should be set to default values if they do not appear in the JSON object</param>
		private static void UpdateLink(Link link, bool connected, JObject json = null, bool forceDefaults = true)
		{
			if (connected)
			{
				string[] props = new string[] { "share", "listen", "donate", "create_playlist" };
				string[] pref = new string[] { "do", "can" };
				Dictionary<string, bool> settings = new Dictionary<string, bool>();
				foreach (string prop in props)
				{
					foreach (string prefix in pref)
					{
						string key = prefix + "_" + prop;
						try
						{
							settings[key] = json[key] != null && json[key].ToString().ToLower() == "true";
						}
						catch
						{
							settings[key] = false;
						}
					}
				}
				if (json["display"] != null)
					link.Provider = (string)json["display"];
				if (json["url"] != null)
					link.URL = (string)json["url"];
				if (json["error"] != null)
					link.Error = (string)json["error"];

				try
				{
					if (json["id"] != null)
						link.ID = Convert.ToUInt32(String.Format("{0}", json["id"]));
				}
				catch { }

				if (json["can_share"] != null || forceDefaults)
					link.CanShare = settings["can_share"];
				if (json["do_share"] != null || forceDefaults)
					link.DoShare = settings["do_share"];
				if (json["can_listen"] != null || forceDefaults)
					link.CanListen = settings["can_listen"];
				if (json["do_listen"] != null || forceDefaults)
					link.DoListen = settings["do_listen"];
				if (json["can_donate"] != null || forceDefaults)
					link.CanDonate = settings["can_donate"];
				if (json["do_donate"] != null || forceDefaults)
					link.DoDonate = settings["do_donate"];
				if (json["can_create_playlist"] != null || forceDefaults)
					link.CanCreatePlaylist = settings["can_create_playlist"];
				if (json["do_create_playlist"] != null || forceDefaults)
					link.DoCreatePlaylist = settings["do_create_playlist"];
				link.Connected = true;
			}
			else
			{
				link.Connected = false;
			}
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when a property of the settings manager is changed
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void Settings_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			if (!Linked || Identity == null) return;

			// create query string
			switch (e.PropertyName)
			{
				case "Volume":
					if (SynchronizeConfiguration)
						PushConfigUpdate("volume", Convert.ToString(Settings.Manager.Volume));
					break;

				case "Repeat":
					if (SynchronizeConfiguration)
						PushConfigUpdate("repeat", Settings.Manager.Repeat.ToString());
					break;

				case "Shuffle":
					if (SynchronizeConfiguration)
						PushConfigUpdate("shuffle", Settings.Manager.Shuffle ? "Random" : "Off");
					break;

				case "MediaState":
					if (SynchronizeConfiguration)
						PushConfigUpdate("media_state", Settings.Manager.MediaState == MediaState.Playing ? "Playing" : "Paused");

					if (Settings.Manager.MediaState == MediaState.Stopped)
						currentListen = null;

					if (Settings.Manager.CurrentTrack != null)
					{
						string path = Settings.Manager.CurrentTrack.Path;
						if (listenQueue.ContainsKey(path))
						{
							// listen has been submitted but no reply yet
							listenQueue[path] = new Tuple<bool, DateTime>(false, listenQueue[path].Item2);
						}
						else
						{
							if (Settings.Manager.MediaState == MediaState.Playing)
							{
								if (idOfCurrentListen <= 0)
								{
									// no listen submitted yet
									stampOfCurrentListen = DateTime.Now;
									StartListen();
								}
								else
								{
									// update listen to reflect new exepcted end time
									double pos = Media.Manager.Position;
									double len = Media.Manager.Length;
									DateTime expectedEnd = DateTime.UtcNow.AddSeconds(len - pos);
									UpdateListen(expectedEnd);
								}
							}
							else if (idOfCurrentListen > 0)
							{
								// paused
								DateTime stamp = stampOfCurrentListen.Value;
								TimeSpan diff = DateTime.Now - stamp;
								if (diff.TotalSeconds >= minimumListenTime)
									UpdateListen(DateTime.Now);
								else
									DeleteListen(idOfCurrentListen);
							}
						}
					}
					else if (Settings.Manager.CurrentTrack != null)
					{
						string path = Settings.Manager.CurrentTrack.Path;
						if (listenQueue.ContainsKey(path))
						{
							// listen has been submitted but no reply yet
							DateTime stamp = listenQueue[path].Item2;
							TimeSpan diff = DateTime.Now - stamp;
							if (diff.TotalSeconds < minimumListenTime)
								listenQueue[path] = new Tuple<bool, DateTime>(true, stamp);
						}
						else
						{
							if (idOfCurrentListen <= 0) return;

							// update listen to reflect exepcted end time
							UpdateListen(DateTime.UtcNow);
						}
					}

					break;

				case "CurrentTrack":
					if (SynchronizeConfiguration)
						PushConfigUpdate("current_track", TrackToJSON(Settings.Manager.CurrentTrack));
					break;

				case "SyncPlaylists":
					SyncPlaylists();
					break;

				case "SyncConfig":
					SyncConfig();
					break;

				case "DownloadAlbumArt":
				case "DownloadAlbumArtSmall":
				case "DownloadAlbumArtMedium":
				case "DownloadAlbumArtLarge":
				case "DownloadAlbumArtHuge":
					if ((bool)e.NewValue == true)
						SetArt();
					break;

				default:
					return;
			}
		}

		/// <summary>
		/// Invoked when the media manager starts playing a track.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void Media_Started(object sender, EventArgs e)
		{
			if (Settings.Manager.MediaState == MediaState.Playing &&
				Settings.Manager.CurrentTrack != null &&
				(currentListen == null || Settings.Manager.CurrentTrack.Path != currentListen.Path))
			{
				uint oldListen = idOfCurrentListen;
				idOfCurrentListen = 0;

				if (oldListen > 0 && stampOfCurrentListen != null)
				{
					DateTime stamp = stampOfCurrentListen.Value;
					TimeSpan diff = DateTime.Now - stamp;
					if (diff.TotalSeconds >= minimumListenTime)
						EndListen(oldListen);
					else
						DeleteListen(oldListen);
				}

				stampOfCurrentListen = DateTime.Now;
				currentListen = Settings.Manager.CurrentTrack;
				StartListen();
			}
		}

		/// <summary>
		/// Invoked when a property of a playlist has changed.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void Playlist_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var playlist = sender as Playlist;
			if (playlist == null) return;

			try
			{
				if (e.PropertyName == "Filter")
				{
					if (Identity != null && playlist.OwnerID == Identity.UserID)
					{
						JObject para = new JObject();
						para["filter"] = playlist.Filter ?? "";

						lock (syncOutLocker)
							syncOutBuffer.Add(new SyncOperation("update", "playlists", playlist.ID, para));
						InitiateSyncTimer();
					}
				}
			}
			catch (Exception exc)
			{
				U.L(LogLevel.Warning, "Service", "Could not handle changed property of playlist: " + exc.Message);
			}
		}

		/// <summary>
		/// Invoked when a playlist has been modified.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void Playlists_PlaylistModified(object sender, ModifiedEventArgs e)
		{
			try
			{
				Playlist playlist = sender as Playlist;
				if (e.Type == ModifyType.Created)
					playlist.PropertyChanged += new PropertyChangedEventHandler(Playlist_PropertyChanged);

				if (!SynchronizePlaylists) return;

				if (Identity == null || playlist.OwnerID != Identity.UserID)
					return;

				Thread thread = new Thread(delegate()
				{
					switch (e.Type)
					{
						case ModifyType.Created:
							UploadPlaylist(playlist);
							break;

						case ModifyType.Removed:
							lock (syncOutLocker)
								syncOutBuffer.Add(new SyncOperation("delete", "playlists", playlist.ID));
							InitiateSyncTimer();
							break;
					}
				});
				thread.Name = "Playlist sync thread";
				thread.Priority = ThreadPriority.Lowest;
				thread.Start();
			}
			catch (Exception exc)
			{
				U.L(LogLevel.Warning, "Service", "Could not handle modified playlist: " + exc.Message);
			}
		}

		/// <summary>
		/// Invoked when a playlist has been modified.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void Playlists_PlaylistRenamed(object sender, RenamedEventArgs e)
		{
			if (!SynchronizePlaylists) return;

			Playlist playlist = sender as Playlist;

			JObject para = new JObject();
			para["name"] = e.Name;

			lock (syncOutLocker)
				syncOutBuffer.Add(new SyncOperation("update", "playlists", playlist.ID, para));
			InitiateSyncTimer();
		}

		/// <summary>
		/// Invoked when songs are added or removed from a playlist.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void Playlist_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			// find the playlist whose collection was modified
			ObservableCollection<Track> t = sender as ObservableCollection<Track>;
			if (t == null) return;
			Playlist playlist = null;
			foreach (Playlist p in Settings.Manager.Playlists)
				if (p.Tracks == t)
				{
					playlist = p;
					break;
				}
			if (playlist == null) return;

			// not synced, not our playlist, or dynamic?
			if (Identity == null || playlist.ID <= 0 || playlist.OwnerID != Identity.UserID || !String.IsNullOrWhiteSpace(playlist.Filter))
				return;

			lock (syncOutLocker)
			{
				SyncOperation op = null;
				foreach (SyncOperation o in syncOutBuffer)
				{
					if (o.Command == "update" && o.ObjectType == "playlists" && o.ObjectID == playlist.ID)
					{
						op = o;
						break;
					}
				}

				if (op == null)
				{
					JObject para = new JObject();
					para["songs"] = new JObject();
					para["songs"]["added"] = new JArray();
					para["songs"]["removed"] = new JArray();
					op = new SyncOperation("update", "playlists", playlist.ID, para);
					syncOutBuffer.Add(op);
				}

				if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count > 0)
				{
					foreach (Track track in e.NewItems)
					{
						JObject json = TrackToJSON(track);
						if (json != null)
							((JArray)op.Params["songs"]["added"]).Add(json);
					}
				}
				if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems.Count > 0)
				{
					foreach (Track track in e.OldItems)
					{
						JObject json = TrackToJSON(track);
						if (json != null)
							((JArray)op.Params["songs"]["removed"]).Add(json);
					}
				}
			}

			InitiateSyncTimer();
		}

		/// <summary>
		/// Sends requests to the server updating the properties stored in
		/// syncOutBuffer
		/// </summary>
		/// <param name="state">The state (not used)</param>
		private static void PerformSyncOut(object state)
		{
			if (syncOutDelay != null)
				syncOutDelay.Dispose();
			syncOutDelay = null;

			if (Identity == null || syncOutBuffer.Count == 0)
				return;

			ThreadStart thread = delegate()
			{
				U.L(LogLevel.Debug, "SERVICE", "Synchronizing with cloud");
				List<SyncOperation> tmpBuffer = new List<SyncOperation>();
				lock (syncOutLocker)
				{
					foreach (SyncOperation op in syncOutBuffer)
						tmpBuffer.Add(op);
					syncOutBuffer.Clear();
				}
				foreach (SyncOperation op in tmpBuffer)
				{
					HttpStatusCode expectedCode = HttpStatusCode.NoContent;
					string method = null;
					string url = null;
					string query = null;
					string body = null;
					
					string o = op.ObjectType.Substring(0, op.ObjectType.Length-1);

					switch (op.Command)
					{
						case "create":
							method = "POST";
							url = String.Format("/{0}.json", op.ObjectType);

							if (op.ObjectType == "playlists")
							{
								// remove "songs" and put in body
								if (op.Params["songs"] != null)
								{
									body = op.Params["songs"].ToString();
									op.Params.Remove("songs");
								}
							}

							foreach (string key in op.Params.Properties().Select(p => p.Name).ToList())
								query += U.CreateQuery(String.Format("{0}[{1}]", o, key), op.Params[key]) + "&";

							expectedCode = HttpStatusCode.Created;

							break;

						case "update":
							method = "PUT";
							url = String.Format("/{0}/{1}.json", op.ObjectType, op.ObjectID);

							if (op.ObjectType == "playlists")
							{
								// remove "songs" and put in body
								if (op.Params["songs"] != null)
								{
									body = op.Params["songs"].ToString();
									op.Params.Remove("songs");
								}
							}
							
							foreach (string key in op.Params.Properties().Select(p => p.Name).ToList())
								query += U.CreateQuery(String.Format("{0}[{1}]", o, key), op.Params[key]) + "&";
							break;

						case "delete":
							method = "DELETE";
							url = String.Format("/{0}/{1}.json", op.ObjectType, op.ObjectID);
							break;
					}

					if (query != null)
						query = "?" + query.Substring(0, query.Length-1);

					// send request to server
					var response = SendRequest(url, method, query, body);
					if (response == null || response.StatusCode != expectedCode)
					{
						U.L(LogLevel.Error, "SERVICE", "There was a problem synchronizing with cloud");
						U.L(LogLevel.Error, "SERVICE", response);
					}
					else
					{
						if (op.ObjectType == "playlists" && op.Command == "create")
						{
							Stream s = response.GetResponseStream();
							StreamReader sr = new StreamReader(s);
							string str = sr.ReadToEnd();
							JObject jo = JObject.Parse(str);
							Playlist playlist = Playlists.Manager.Get((string)jo["name"]);
							if (playlist != null)
							{
								playlist.ID = (uint)jo["id"];
								playlist.OwnerID = (uint)jo["user_id"];
								playlist.Tracks.CollectionChanged += Playlist_CollectionChanged;
								U.L(LogLevel.Debug, "SERVICE", "Playlist was uploaded successfully with cloud ID " + playlist.ID);
							}
						}
					}
					if (response != null) response.Close();
				}
			};
			Thread th = new Thread(thread);
			th.Name = "Sync thread";
			th.Priority = ThreadPriority.Lowest;
			th.Start();
		}

		/// <summary>
		/// Attempts to reconnect to the cloud server.
		/// </summary>
		/// <param name="state">The state (not used)</param>
		private static void PerformReconnect(object state)
		{
			Ping(new Uri(domain));
		}

		/// <summary>
		/// Sends request to the server submitting the CurrentTrack as a new Listen object.
		/// </summary>
		/// <param name="state">The state (not used)</param>
		private static void PerformStartListen(object state)
		{
			if (listenDelay != null)
				listenDelay.Dispose();
			listenDelay = null;

			if (Settings.Manager.CurrentTrack == null)
				return;

			ThreadStart thread = delegate()
			{
				try
				{
					Track track = Settings.Manager.CurrentTrack;

					Playlist playlist = null;
					if (Settings.Manager.CurrentActiveNavigation.StartsWith("playlist:"))
						playlist = Playlists.Manager.Get(Settings.Manager.CurrentActiveNavigation.Split(new char[] { ':' }, 2)[1]);

					string path = track.Path;
					if (track.Type == TrackType.File)
						path = Path.GetFileName(track.Path);

					string query = String.Format("?{0}", String.Join("&", EncodeTrack(track, "track")));
					if (playlist != null)
						query = String.Format("{0}&playlist={1}", query, playlist.ID > 0 ? playlist.ID.ToString() : playlist.Name);

					listenQueue[path] = new Tuple<bool, DateTime>(false, DateTime.Now);

					var response = SendRequest("/listens.json", "POST", query);

					if (response == null || response.StatusCode != HttpStatusCode.Created)
					{
						U.L(LogLevel.Error, "SERVICE", "There was a problem submitting song " + track.Artist + " - " + track.Title);
						if (response == null)
						{
							Settings.Manager.ListenBuffer["/listens.json" + query] = new Tuple<string, string>("POST", path);
							CheckListenRetry();
						}
						else
							U.L(LogLevel.Error, "SERVICE", response);
					}
					else
					{
						U.L(LogLevel.Information, "SERVICE", "Submitted listen of song " + track.Artist + " - " + track.Title);
						JObject listen = ParseResponse(response) as JObject;
						uint id = (uint)listen["id"];
						bool b = listenQueue[path].Item1;
						listenQueue.Remove(path);
						if (b)
							DeleteListen(id);
						else
							idOfCurrentListen = id;
					}
					if (response != null) response.Close();
				}
				catch (Exception e)
				{
					U.L(LogLevel.Error, "SERVICE", "There was a problem starting listen");
					U.L(LogLevel.Error, "SERVICE", e.Message);
					//Delink();
				}
			};
			Thread th = new Thread(thread);
			th.Name = "Listen submission thread";
			th.Priority = ThreadPriority.Lowest;
			th.Start();
		}

		/// <summary>
		/// Sends request to the server submitting the listens that has failed.
		/// </summary>
		/// <param name="state">The state (not used)</param>
		private static void PerformRetryListen(object state)
		{
			if (listenRetry != null)
				listenRetry.Dispose();

			if (Settings.Manager.ListenBuffer.Count == 0)
			{
				listenRetryStep = -1;
				return;
			}

			listenRetryStep++;

			try
			{
				List<Tuple<string,string,string>> buffer = new List<Tuple<string,string,string>>();
				for (int i = 0; i < Settings.Manager.ListenBuffer.Count; i++)
				{
					KeyValuePair<string,Tuple<string,string>> kv = Settings.Manager.ListenBuffer.ElementAt(i);
					buffer.Add(new Tuple<string,string,string>(kv.Key, kv.Value.Item1, kv.Value.Item2));
				}
				Settings.Manager.ListenBuffer.Clear();

				foreach (Tuple<string,string,string> tup in buffer)
				{
					string path = tup.Item1;
					string method = tup.Item2;
					string query = tup.Item3;
					ThreadStart thread = delegate()
					{
						try
						{
							string trackPath = "";

							// if POST then URL will contain the query and the track's path will be
							// the second item in the tuple.
							if (method == "POST")
							{
								string[] k = path.Split(new char[] { '?' }, 2);
								path = k[0];
								query = "?" + k[1];
								trackPath = tup.Item3;
								// if the listen is scheduled to be removed after creation (due to not
								// being played long enough) then we just skip creating it.
								if (listenQueue.ContainsKey(trackPath) && listenQueue[trackPath].Item1)
								{
									listenQueue.Remove(trackPath);
									return;
								}
							}
							var response = SendRequest(path, method, query);

							if (response == null || (response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.NoContent))
							{
								U.L(LogLevel.Error, "SERVICE", "There was a problem when retrying listen transmission");
								if (response == null)
									Settings.Manager.ListenBuffer[tup.Item1] = new Tuple<string, string>(tup.Item2, tup.Item3);
								else
									U.L(LogLevel.Error, "SERVICE", response);
							}
							else
							{
								U.L(LogLevel.Debug, "SERVICE", "Successfully retransmitted listen submission");
								if (response.StatusCode == HttpStatusCode.Created)
								{
									JObject listen = ParseResponse(response) as JObject;
									uint id = (uint)listen["id"];
									if (listenQueue.ContainsKey(trackPath))
									{
										bool b = listenQueue[trackPath].Item1;
										listenQueue.Remove(trackPath);
										if (b)
											DeleteListen(id);
										else
											idOfCurrentListen = id;
									}
									else
										idOfCurrentListen = id;
								}
							}
							if (response != null) response.Close();
						}
						catch (Exception e)
						{
							U.L(LogLevel.Error, "SERVICE", "There was a problem starting listen");
							U.L(LogLevel.Error, "SERVICE", e.Message);
						}
					};
					Thread th = new Thread(thread);
					th.Name = "Listen submission retry thread";
					th.Priority = ThreadPriority.Lowest;
					th.Start();
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Error, "SERVICE", "There was a problem starting listen");
				U.L(LogLevel.Error, "SERVICE", e.Message);
			}

			int nextStep = Math.Min(listenRetryStep, listenRetrySteps.Count() - 1);
			listenRetry = new Timer(PerformRetryListen, null, listenRetrySteps[nextStep] * 1000, Int32.MaxValue);
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// Trigger the PropertyChanged event
		/// </summary>
		/// <param name="name">The name of the property that was changed</param>
		/// <param name="oldValue">The value of the property before the change</param>
		/// <param name="newValue">The value of the property after the change</param>
		/// <param name="force">Dispatch even when no change occured between values</param>
		public static void DispatchPropertyChanged(string name, object oldValue, object newValue, bool force = false)
		{
			if (PropertyChanged != null && (oldValue != newValue || force))
				PropertyChanged(null, new PropertyChangedWithValuesEventArgs(name, oldValue, newValue));
		}

		/// <summary>
		/// Trigger the Initialized event.
		/// </summary>
		public static void DispatchInitialized()
		{
			if (Initialized != null)
				Initialized(null, new EventArgs());
		}

		/// <summary>
		/// Trigger the ModifyTracks event.
		/// </summary>
		/// <param name="collection">The collection that need to be modified</param>
		/// <param name="arguments">The arguments describing the modifications</param>
		public static void DispatchModifyTracks(ObservableCollection<Track> collection, ModifiedEventArgs arguments)
		{
			if (ModifyTracks != null)
				ModifyTracks(collection, arguments);
		}

		/// <summary>
		/// Trigger the RealtimeDisconnected event.
		/// </summary>
		public static void OnRealtimeDisconnected()
		{
			if (RealtimeDisconnected != null)
				RealtimeDisconnected(null, new EventArgs());
		}

		/// <summary>
		/// Trigger the RealtimeConnected event.
		/// </summary>
		public static void OnRealtimeConnected()
		{
			if (RealtimeConnected != null)
				RealtimeConnected(null, new EventArgs());
		}

		/// <summary>
		/// Trigger the AlbumArtCacheChanged event.
		/// </summary>
		public static void OnAlbumArtCacheChanged()
		{
			if (AlbumArtCacheChanged != null)
				AlbumArtCacheChanged(null, new EventArgs());
		}

		#endregion

		#endregion
		
		#region Events

		/// <summary>
		/// Occurs when a property has been changed
		/// </summary>
		public static event EventHandler<PropertyChangedWithValuesEventArgs> PropertyChanged;

		/// <summary>
		/// Occurs when the service manager has been fully initialized.
		/// </summary>
		public static event EventHandler Initialized;

		/// <summary>
		/// Occurs when the main window need to modify a collection in the UI thread.
		/// </summary>
		public static event EventHandler<ModifiedEventArgs> ModifyTracks;

		/// <summary>
		/// Occurs when the realtime communication channel has been disconnected.
		/// </summary>
		public static event EventHandler RealtimeDisconnected;

		/// <summary>
		/// Occurs when the realtime communication channel has been connected.
		/// </summary>
		public static event EventHandler RealtimeConnected;

		/// <summary>
		/// Occurs when the size of the cached album art changes.
		/// </summary>
		public static event EventHandler AlbumArtCacheChanged;

		#endregion
	}

	/// <summary>
	/// Describes the interface that the async javascript will call to update attributes
	/// </summary>
	[ComVisibleAttribute(true)]
	public class CloudSyncInterface
	{
		#region Members

		/// <summary>
		/// Values correpsond to arguments to <seealso cref="Buffer"/>.
		/// Key is in format: command, objectID, objectType
		/// Value is data
		/// </summary>
		private static Dictionary<Tuple<SyncCommand,uint,string>, string> syncInBuffer =
			new Dictionary<Tuple<SyncCommand, uint, string>, string>();
		private static Timer syncInDelay = null;
		#endregion

		#region Methods

		#region Private

		/// <summary>
		/// Places a sync command onto the buffer
		/// and resets the buffer timer.
		/// </summary>
		/// <param name="syncCommand">The command to be pushed onto the buffer</param>
		/// <param name="objectID">The ID of the object which to manipulate (not used for Create command)</param>
		/// <param name="objectType">The type of the object to act on (not used for Execute command).</param>
		/// <param name="data">
		/// Object structure if Update or Create, command if Execute, otherwise empty.
		/// </param>
		private void Buffer(SyncCommand syncCommand, uint objectID, string objectType, string data)
		{
			// create query string
			switch (syncCommand)
			{
				case SyncCommand.Execute:
					syncInBuffer[Tuple.Create<SyncCommand, uint, string>(syncCommand, objectID, objectType)] = data;
					break;

				case SyncCommand.Create:
					syncInBuffer[Tuple.Create<SyncCommand, uint, string>(syncCommand, 0, objectType)] = data;
					break;

				case SyncCommand.Delete:
					var updateTuple = Tuple.Create<SyncCommand, uint, string>(syncCommand, objectID, objectType);

					// remove any updates
					if (syncInBuffer.ContainsKey(updateTuple))
						syncInBuffer.Remove(updateTuple);

					syncInBuffer[Tuple.Create<SyncCommand, uint, string>(syncCommand, objectID, objectType)] = "";
					break;

				case SyncCommand.Update:
					syncInBuffer[Tuple.Create<SyncCommand, uint, string>(syncCommand, objectID, objectType)] = data;
					break;

				default:
					return;
			}

			if (syncInDelay != null)
				syncInDelay.Dispose();
			syncInDelay = new Timer(PerformSyncIn, null, 500, Timeout.Infinite);
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when an object changes.
		/// </summary>
		/// <param name="objectType">The name of the type of object</param>
		/// <param name="objectID">The object ID</param>
		/// <param name="properties">The updated properties encoded in JSON</param>
		public void UpdateObject(string objectType, uint objectID, string properties)
		{
			Buffer(SyncCommand.Update, objectID, objectType, properties);
		}

		/// <summary>
		/// Invoked when an object is created.
		/// </summary>
		/// <param name="objectType">The name of the type of object</param>
		/// <param name="objectJSON">The object encoded as JSON</param>
		public void CreateObject(string objectType, string objectJSON)
		{
			Buffer(SyncCommand.Create, 0, objectType, objectJSON);
		}

		/// <summary>
		/// Invoked when an object is deleted.
		/// </summary>
		/// <param name="objectType">The name of the type of object</param>
		/// <param name="objectID">The object id</param>
		public void DeleteObject(string objectType, uint objectID)
		{
			Buffer(SyncCommand.Delete, objectID, objectType, "");
		}

		/// <summary>
		/// Invoked when a command is to be executed.
		/// </summary>
		/// <param name="command">The name of the command</param>
		/// <param name="objectType">The name of the type of object</param>
		/// <param name="configID">The ID of the object</param>
		public void Execute(string command, string objectType, uint configID)
		{
			Buffer(SyncCommand.Execute, configID, objectType, command);
		}

		/// <summary>
		/// Sets the session ID of the real time communication channel.
		/// </summary>
		/// <param name="sessionID">The ID of the communication session</param>
		public void SetSessionID(string sessionID)
		{
			Services.Manager.RealTimeSessionID = sessionID;
		}

		/// <summary>
		/// Reports an error for a third party service link.
		/// </summary>
		/// <param name="id">The ID of the link</param>
		/// <param name="errorMessage">The error message</param>
		public void LinkError(int id, string errorMessage)
		{
			Services.Manager.LinkError(id, errorMessage);
		}

		/// <summary>
		/// Performs actions according to the commands stored in
		/// syncInBuffer.
		/// </summary>
		/// <param name="state">The state (not used)</param>
		private static void PerformSyncIn(object state)
		{
			if (syncInDelay != null)
				syncInDelay.Dispose();
			syncInDelay = null;

			ThreadStart thread = delegate()
			{
				Dictionary<Tuple<SyncCommand, uint, string>, string> buf = new Dictionary<Tuple<SyncCommand, uint, string>, string>();
				for (int i = 0; i < syncInBuffer.Count; i++)
					buf.Add(syncInBuffer.Keys.ElementAt(i), syncInBuffer.Values.ElementAt(i));
				syncInBuffer.Clear();
				foreach (KeyValuePair<Tuple<SyncCommand, uint, string>, string> p in buf)
				{
					switch (p.Key.Item1)
					{
						case SyncCommand.Execute:
							Services.Manager.ExecuteCommand(p.Value, p.Key.Item3, p.Key.Item2);
							break;

						case SyncCommand.Create:
							Services.Manager.CreateObject(p.Key.Item3, p.Value);
							break;

						case SyncCommand.Delete:
							Services.Manager.DeleteObject(p.Key.Item3, p.Key.Item2);
							break;

						case SyncCommand.Update:
							Services.Manager.UpdateObject(p.Key.Item3, p.Key.Item2, p.Value);
							break;
					}
				}
			};
			Thread th = new Thread(thread);
			th.Name = "Sync in thread";
			th.Priority = ThreadPriority.Lowest;
			th.Start();
		}

		#endregion

		#endregion

		#region Enums

		/// <summary>
		/// Describes a command of a synchronization operation.
		/// </summary>
		private enum SyncCommand
		{
			/// <summary>
			/// Create a new object.
			/// </summary>
			Create,

			/// <summary>
			/// Update an existing object.
			/// </summary>
			Update,

			/// <summary>
			/// Delete an existing object.
			/// </summary>
			Delete,

			/// <summary>
			/// Execute a command.
			/// </summary>
			Execute
		}

		#endregion
	}

	/// <summary>
	/// Describes a synchronization operation.
	/// </summary>
	public class SyncOperation
	{
		/// <summary>
		/// The command.
		/// </summary>
		/// <remarks>
		/// Possible values:
		/// create, delete, update
		/// </remarks>
		public string Command { get; private set; }

		/// <summary>
		/// The type of object.
		/// </summary>
		/// <remarks>
		/// Example values:
		/// playlists, configurations, devices, users
		/// </remarks>
		public string ObjectType { get; private set; }

		/// <summary>
		/// The ID of the object.
		/// </summary>
		/// <remarks>
		/// Not used for the create command.
		/// </remarks>
		public uint ObjectID { get; private set; }

		/// <summary>
		/// The parameters of the command.
		/// </summary>
		/// <remarks>
		/// Not used for the delete command.
		/// </remarks>
		public JObject Params { get; private set; }

		/// <summary>
		/// Create a synchronization operation.
		/// </summary>
		/// <param name="command">The sync command</param>
		/// <param name="objectType">The type of the object</param>
		/// <param name="objectID">The object ID</param>
		/// <param name="parameters">Parameters for the object</param>
		public SyncOperation(string command, string objectType, uint objectID = 0, JObject parameters = null)
		{
			Command = command;
			ObjectType = objectType;
			ObjectID = objectID;
			Params = parameters;
		}

		/// <summary>
		/// Create a synchronization operation.
		/// </summary>
		/// <param name="command">The sync command</param>
		/// <param name="objectType">The type of the object</param>
		/// <param name="parameters">Parameters for the object</param>
		/// <param name="objectID">The object ID</param>
		public SyncOperation(string command, string objectType, JObject parameters = null, uint objectID = 0) :
			this(command, objectType, objectID, parameters)
		{
		}
	}
}