/**
 * UpgradeManager.cs
 * 
 * Takes care of upgrading Stoffi to a new version by probing the
 * upgrade servers at regular intervals, or doing a probe on user
 * request when mode is set to manual.
 * 
 * Downloads the file via HTTPS and examines it to determine if
 * the file is a pure text response from the server or a tar.bz2
 * package containing the different packages for each new version.
 * 
 * If the downloaded file contains version package each is unpacked
 * and the containing files are copied to the program folder.
 * If the settings migrator library is found it is hooked into and
 * a migration of the user.config file is performed.
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
using System.Collections.Specialized;
using System.Configuration;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.BZip2;

namespace Stoffi
{
	/// <summary>
	/// Represents the upgrade manager which check for upgrades and installs them
	/// </summary>
	public static class UpgradeManager
	{
		#region Members

		private static string upgradeServer = "upgrade.stoffiplayer.com";
		private static string upgradeFolder = "UpgradeFolder/";
		private static string baseFolder = U.BasePath;
		private static string downloadFilename = "";
		private static TimeSpan interval;
		private static int pendingUpgradeVersion = -1;
		private static Timer prober = null;
		private static bool haveID = false;
		private static WebClient client = null;

		#endregion

		#region Properties

		/// <summary>
		/// Gets whether there's a upgrade in progress
		/// </summary>
		public static bool InProgress { get; private set; }

		/// <summary>
		/// Gets whether there's a upgrade pending, waiting for a restart of Stoffi
		/// </summary>
		public static bool Pending { get; private set; }

		/// <summary>
		/// Gets whether there's a upgrade available
		/// </summary>
		public static bool Found { get; private set; }

		/// <summary>
		/// Sets whether the upgrade should download the upgrades in case the policy is set to manual
		/// </summary>
		public static bool ForceDownload { get; set; }

		/// <summary>
		/// Gets or sets the policy regarding upgrading
		/// </summary>
		public static UpgradePolicy Policy
		{
			get { return SettingsManager.UpgradePolicy; }
			set { SettingsManager.UpgradePolicy = value; }
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Initialize the upgrade manager
		/// </summary>
		/// <param name="interv">How often to check for new upgrades</param>
		public static void Initialize(TimeSpan interv)
		{
			interval = interv;
			Pending = false;
			ForceDownload = false;
			InProgress = false;
			Clear();

			if (Directory.Exists(Path.Combine(baseFolder, upgradeFolder)))
				Directory.Delete(Path.Combine(baseFolder, upgradeFolder), true);

			if (Policy != UpgradePolicy.Manual)
			{
				Start();
				Probe(null);
			}

			SettingsManager.PropertyChanged += new PropertyChangedWithValuesEventHandler(SettingsManager_PropertyChanged);
		}

		/// <summary>
		/// Start the prober
		/// </summary>
		public static void Start()
		{
#if (DEBUG)
			U.L(LogLevel.Warning, "UPGRADE", "Running debug build, no upgrade checks");
#else
			Stop();
			if (SettingsManager.UpgradePolicy != UpgradePolicy.Manual)
				prober = new Timer(Probe, null, (long)interval.TotalMilliseconds, (long)interval.TotalMilliseconds);
#endif
		}

		/// <summary>
		/// Stop the prober
		/// </summary>
		public static void Stop()
		{
			if (prober != null)
				prober.Dispose();
			prober = null;
			ForceDownload = false;
			if (client != null)
				client.CancelAsync();
		}

		/// <summary>
		/// Check if there's any upgrades available.
		/// This is usually called from the timer.
		/// </summary>
		/// <param name="state">The timer state</param>
		public static void Probe(object state)
		{
			Found = false;
			InProgress = false;

			// ignore SSL cert error
			ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(
				delegate { return true; }
			);
			ServicePointManager.ServerCertificateValidationCallback +=
				delegate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
				{
					return true;
				};
#if (DEBUG)
			U.L(LogLevel.Warning, "UPGRADE", "Running debug build, no upgrade checks");
			if (SettingsManager.UpgradePolicy == UpgradePolicy.Manual)
			{
				DispatchErrorOccured(U.T("UpgradeDebug"));
			}
			
#else
			if (Pending)
			{
				if (SettingsManager.UpgradePolicy == UpgradePolicy.Manual)
				{
					DispatchErrorOccured(U.T("UpgradePending"));
				}
				U.L(LogLevel.Debug, "UPGRADE", "Waiting for restart");
				Stop();
				return;
			}

			U.L(LogLevel.Debug, "UPGRADE", "Probe");

			// create temporary folder if it doesn't already exist
			if (!Directory.Exists(Path.Combine(baseFolder, upgradeFolder)))
				Directory.CreateDirectory(Path.Combine(baseFolder, upgradeFolder));

			String URL = String.Format("https://{0}?version={1}&channel={2}&arch={3}",
				upgradeServer,
				SettingsManager.Version,
				SettingsManager.Channel,
				SettingsManager.Architecture);

			U.L(LogLevel.Debug, "UPGRADE", "Probing " + URL);

			// send unique ID if we have one
			haveID = true;
			try { URL += String.Format("&id={0}", SettingsManager.ID); }
			catch { haveID = false; }
			if (SettingsManager.UpgradePolicy != UpgradePolicy.Automatic && !ForceDownload)
				URL += "&p=1";

			DispatchProgressChanged(0, new ProgressState(U.T("UpgradeDownloading"), false));

			if (client != null)
				client.CancelAsync();
			else
			{
				client = new WebClient();
				client.Headers.Add("User-Agent", String.Format("Stoffi/{0}", SettingsManager.Version));
				client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(Client_ProgressChanged);
				client.DownloadFileCompleted += new AsyncCompletedEventHandler(Examine);
			}

			// generate a random file name
			String Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			StringBuilder Filename = new StringBuilder();
			Random random = new Random();
			for (int i = 0; i < 20; i++)
				Filename.Append(Chars[(int)(random.NextDouble() * Chars.Length)]);
			downloadFilename = Filename.ToString();

			// download upgrade file
			U.L(LogLevel.Debug, "UPGRADE", "Start download");
			try
			{
				//InProgress = true;
				client.DownloadFileAsync(new Uri(URL), baseFolder + "/" + upgradeFolder + downloadFilename);
			}
			catch (Exception exc)
			{
				InProgress = false;
				U.L(LogLevel.Warning, "UPGRADE", "Could not contact Upgrade Server: " + exc.Message);
			}
#endif
		}

		/// <summary>
		/// Examines downloaded file to see.
		/// 
		/// The method will try to uncompress and unpack the file, assuming that it's a tar.bz2 package containing
		/// all upgrade packages.
		/// If that fails it will treat the file as a text file which contains the response from the upgrade server.
		/// Either the text repsonse is an error message or a message telling the client that no upgrades are available.
		/// If the attempt does not fail and the downloaded file is indeed a package then it will be unpacked to a
		/// temporary folder and Examine() will look for a textfile called "data" which should contain basic information
		/// about the versions included and the ID of the client. The client will change its ID to this number unconditionally.
		/// 
		/// This method is called from the WebClient when download is complete.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="eventArgs">The event data</param>
		public static void Examine(object sender, AsyncCompletedEventArgs eventArgs)
		{
			if (eventArgs.Error != null)
			{
				InProgress = false;
				if (eventArgs.Cancelled)
				{
					return;
				}

				U.L(LogLevel.Error, "UPGRADE", eventArgs.Error.Message);
				DispatchErrorOccured(eventArgs.Error.Message);

				ForceDownload = false;
				return;
			}
			ForceDownload = false;

			U.L(LogLevel.Debug, "UPGRADE", "Examine downloaded file");

			// try to extract file
			try
			{
				U.L(LogLevel.Debug, "UPGRADE", "Try to extract files");
				Stream inStream = File.OpenRead(baseFolder + "/" + upgradeFolder + downloadFilename);
				TarArchive archive = TarArchive.CreateInputTarArchive(inStream, TarBuffer.DefaultBlockFactor);
				System.IO.Directory.CreateDirectory(baseFolder + "/" + upgradeFolder + downloadFilename + "_tmp/");
				archive.ExtractContents(baseFolder + "/" + upgradeFolder + downloadFilename + "_tmp/");
				archive.Close();
				inStream.Close();

				// set unique ID that we got from server
				if (!haveID)
				{
					if (File.Exists(baseFolder + "/" + upgradeFolder + downloadFilename + "_tmp/outgoing/data"))
					{
						String line;
						StreamReader file = new StreamReader(baseFolder + "/" + upgradeFolder + downloadFilename + "_tmp/outgoing/data");
						while ((line = file.ReadLine()) != null)
						{
							string[] field = line.Split(new[] { ':' }, 2);
							if (field[0] == "client id")
							{
								SettingsManager.ID = Convert.ToInt32(field[1].Trim());
								SettingsManager.Save();
							}
						}
						file.Close();
					}
					else
						U.L(LogLevel.Warning, "UPGRADE", "No data file found in Upgrade Package");
				}
				Unpack();
				U.L(LogLevel.Debug, "UPGRADE", "Upgrade installed");
				Pending = true;
				DispatchChecked();
				DispatchUpgraded();
				client.CancelAsync();
				InProgress = false;
				return;
			}
			catch (Exception e)
			{
				// invalid checksum means not an archive,
				// so it must be a text file
				InProgress = false;
				U.L(LogLevel.Debug, "UPGRADE", "Got message from server");
				if (e.Message == "Header checksum is invalid")
				{
					String line;
					StreamReader file = new StreamReader(baseFolder + "/" + upgradeFolder + downloadFilename);
					try
					{
						while ((line = file.ReadLine()) != null) // TODO: check if closed?
						{
							U.L(LogLevel.Debug, "UPGRADE", "Message from server: " + line);
							if (line.Length > 23 + 25 && (line.Substring(0, 23) == "No upgrade needed, mr. " && line.Substring(line.Length - 25, 25) == ". You're already awesome!"))
							{
								String id = line.Substring(23, line.Length - 48);
								SettingsManager.ID = Convert.ToInt32(id);
								SettingsManager.Save();
								file.Close();
								Clear();
								U.L(LogLevel.Information, "UPGRADE", "No new versions found");
								DispatchChecked();
								return;
							}
							else if (line.Length > 20 && line.Substring(0, 20) == "Versions available: ")
							{
								String[] versions = line.Substring(20, line.Length - 20).Split(new[] { ':' }, 2);
								Found = true;
								Notify(versions);
							}
							else
							{
								DispatchErrorOccured("The upgrade server complained:\n" + line);
								U.L(LogLevel.Debug, "UPGRADE", "Server says: " + line);
							}
						}
					}
					catch (Exception exc)
					{
						DispatchErrorOccured(exc.Message);
						U.L(LogLevel.Warning, "UPGRADE", "Could not read downloaded file: " + exc.Message);
					}
					file.Close();
					Clear();
					DispatchChecked();
					return;
				}
				else
				{
					U.L(LogLevel.Warning, "UPGRADE", e.Message);
					Clear();
					DispatchErrorOccured(e.Message);
					return;
				}
			}
		}

		/// <summary>
		/// Unpacks the different upgrade packages contained inside the downloaded file.
		/// 
		/// Each upgrade of Stoffi is to be packaged inside a file called N.tar.bz2 (where N
		/// is the version number) file containing the following:
		/// 1) All files that should be copied to the program folder
		/// 2) Optional: "Settings Migrator.dll", this will be used later to migrate the settings in SettingsManager
		/// 
		/// The method will call Propare() on each version as they are decompressed and unpacked, this will copy the files to a
		/// folder called "Queue". From here the content will be copied to the program folder at Finish().
		/// </summary>
		public static void Unpack()
		{
			string folder = downloadFilename + "_tmp/packages/";

			U.L(LogLevel.Debug, "UPGRADE", "Unpack versions");
			DirectoryInfo folderInfo = new DirectoryInfo(baseFolder + "/" + upgradeFolder + folder);
			FileInfo[] files = folderInfo.GetFiles("*.tar.bz2", SearchOption.AllDirectories);
			List<int> versions = new List<int>();

			if (!Directory.Exists(baseFolder + "/" + upgradeFolder + "Queue")) Directory.CreateDirectory(baseFolder + "/" + upgradeFolder + "Queue");

			// check each file inside the folder and add the version number to our list
			foreach (FileInfo file in files)
			{
				U.L(LogLevel.Debug, "UPGRADE", "Version file @ " + file.FullName);
				versions.Add(Convert.ToInt32(file.Name.Substring(0, file.Name.Length - 8)));
			}

			versions.Sort();
			foreach (int version in versions)
			{
				DispatchProgressChanged(0, new ProgressState(U.T("UpgradeProcessing"), true));
				U.L(LogLevel.Debug, "UPGRADE", "Processing version " + version.ToString());

				String bz2File = baseFolder + "/" + upgradeFolder + folder + SettingsManager.Channel + "/" + SettingsManager.Architecture + "/" + version.ToString() + ".tar.bz2";
				String tarFile = baseFolder + "/" + upgradeFolder + folder + SettingsManager.Channel + "/" + SettingsManager.Architecture + "/" + version.ToString() + ".tar";
				String tmpFold = baseFolder + "/" + upgradeFolder + folder + SettingsManager.Channel + "/" + SettingsManager.Architecture + "/" + version.ToString() + "_tmp/";

				U.L(LogLevel.Debug, "UPGRADE", "Decompressing");
				BZip2.Decompress(File.OpenRead(bz2File), File.Create(tarFile), true);

				Stream inStream = File.OpenRead(tarFile);
				TarArchive archive = TarArchive.CreateInputTarArchive(inStream, TarBuffer.DefaultBlockFactor);
				Directory.CreateDirectory(tmpFold);
				archive.ExtractContents(tmpFold);
				archive.Close();
				inStream.Close();

				if (File.Exists(Path.Combine(tmpFold, "Settings Migrator.dll")))
					File.Move(Path.Combine(tmpFold, "Settings Migrator.dll"), Path.Combine(tmpFold, String.Format("Migrator.{0}.dll", version.ToString())));

				U.L(LogLevel.Debug, "UPGRADE", "Prepare version " + version.ToString());
				Prepare(tmpFold, baseFolder + "/" + upgradeFolder + "Queue/");
				pendingUpgradeVersion = version;
			}

			DispatchUpgraded();
			U.L(LogLevel.Debug, "UPGRADE", "Upgrade completed");
		}

		/// <summary>
		/// Copy all files from a given folder to a given destination, keeping the filestructure of the
		/// source folder.
		/// </summary>
		/// <param name="folder">The folder to copy into</param>
		/// <param name="dest">The folder to copy from</param>
		public static void Prepare(String folder, String dest)
		{
			U.L(LogLevel.Debug, "UPGRADE", "Preparing " + folder + " with destination " + dest);
			DirectoryInfo folderInfo = new DirectoryInfo(folder);

			U.L(LogLevel.Debug, "UPGRADE", "Getting directories of " + folder + " and creating equivalents");
			DirectoryInfo[] dirs = folderInfo.GetDirectories("*", SearchOption.AllDirectories);
			foreach (DirectoryInfo dir in dirs)
				if (!Directory.Exists(dest + dir.FullName.Remove(0, folder.Length)))
					Directory.CreateDirectory(dest + dir.FullName.Remove(0, folder.Length));

			U.L(LogLevel.Debug, "UPGRADE", "Getting files of " + folder + " and moving them to destination");
			FileInfo[] files = folderInfo.GetFiles("*", SearchOption.AllDirectories);
			foreach (FileInfo file in files)
			{
				U.L(LogLevel.Debug, "UPGRADE", String.Format("Moving {0} to {1}", file.FullName, dest + file.FullName.Remove(0, folder.Length)));
				File.Copy(file.FullName, dest + file.FullName.Remove(0, folder.Length), true);
			}
		}

		/// <summary>
		/// Goes through the "Queue" folder inside the upgrade folder and copies each file into the
		/// program folder, backing up files if they already exist.
		/// 
		/// A special case is the file called "Settings Migrator.dll". This file will be loaded into the
		/// assembly as an external library. It should declare an interface named "Stoffi.IMigrator" which
		/// contains a method called Migrate() that takes two arguments:
		/// 1) The existing settings file to read from
		/// 2) The new file to write the migrated settings to
		/// This method will be called using the current user.config file and the new settings file will
		/// be copied both over the old file as well as into a new folder (we do this since we don't know
		/// if Windows will use the old or the new folder to look for user.config, but the one not used will
		/// be removed later anyway).
		/// 
		/// This method is called when the application shuts down.
		/// </summary>
		public static void Finish()
		{
			if (prober != null)
				prober.Dispose();
			prober = null;
			if (!Directory.Exists(baseFolder + "/" + upgradeFolder + "Queue")) return;
			if (!Pending) return;
			U.L(LogLevel.Debug, "UPGRADE", "Finishing upgrade");

			string src = baseFolder + "/" + upgradeFolder + "Queue";
			string dst = baseFolder;
			DirectoryInfo folderInfo = new DirectoryInfo(src);

			U.L(LogLevel.Debug, "UPGRADE", "Creating destination folders");
			DirectoryInfo[] dirs = folderInfo.GetDirectories("*", SearchOption.AllDirectories);
			foreach (DirectoryInfo dir in dirs)
				if (!Directory.Exists(dst + dir.FullName.Remove(0, src.Length)))
					Directory.CreateDirectory(dst + dir.FullName.Remove(0, src.Length));

			U.L(LogLevel.Debug, "UPGRADE", "Copying files");
			FileInfo[] files = folderInfo.GetFiles("*", SearchOption.AllDirectories);

			Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
			string configCompanyFolder = new DirectoryInfo(config.FilePath).Parent.Parent.Parent.Name;
			string configBaseFolder = new DirectoryInfo(config.FilePath).Parent.Parent.Name;
			string configVersionFolder = new DirectoryInfo(config.FilePath).Parent.Name;
			string newVersion = pendingUpgradeVersion.ToString();
			newVersion = String.Format("{0}.{1}.{2}.{3}",
				Convert.ToInt32(newVersion.Substring(0, 1)),
				Convert.ToInt32(newVersion.Substring(1, 2)),
				Convert.ToInt32(newVersion.Substring(3, 3)),
				Convert.ToInt32(newVersion.Substring(6, 4)));
			string newConfigFolder = Path.Combine(new DirectoryInfo(config.FilePath).Parent.Parent.FullName, newVersion);

			foreach (FileInfo file in files.OrderBy(f => f.Name))
			{
				Match m = Regex.Match(file.Name, @"Migrator\.([0-9]+)\.dll", RegexOptions.IgnoreCase);
				if (m.Success)
				{
					U.L(LogLevel.Debug, "UPGRADE", "Migrating user settings");

					// load dll file
					U.L(LogLevel.Debug, "UPGRADE", "Loading migrator library");
					Assembly assembly = Assembly.LoadFrom(file.FullName);

					foreach (Type type in assembly.GetTypes())
					{
						if (type.GetInterface("Stoffi.IMigrator") == null)
							continue;

						object migratorObject = Activator.CreateInstance(type);

						object[] arguments = new object[] { config.FilePath, config.FilePath + ".new" };

						U.L(LogLevel.Debug, "UPGRADE", "Invoking DLL procedure");
						U.L(LogLevel.Debug, "UPGRADE", String.Format("From: {0}", config.FilePath));
						U.L(LogLevel.Debug, "UPGRADE", String.Format("To: {0}", config.FilePath + ".new"));
						type.InvokeMember("Migrate", BindingFlags.Default | BindingFlags.InvokeMethod, null, migratorObject, arguments);

						U.L(LogLevel.Debug, "UPGRADE", String.Format("Taking backup and replacing current settings file"));
						File.Copy(config.FilePath + ".new", config.FilePath + "." + file.Name + ".config");
						if (File.Exists(config.FilePath + ".old"))
							File.Delete(config.FilePath + ".old");
						if (File.Exists(config.FilePath))
							File.Move(config.FilePath, config.FilePath + ".old");
						File.Move(config.FilePath + ".new", config.FilePath);

						U.L(LogLevel.Debug, "UPGRADE", String.Format("Settings have been migrated"));
					}
				}
				else
				{
					String oldFile = file.FullName;
					String newFile = dst + file.FullName.Remove(0, src.Length);

					String bakFile = Path.GetDirectoryName(newFile) + "/bak." + Path.GetFileName(newFile);

					U.L(LogLevel.Debug, "UPGRADE", String.Format("Renaming {0} to {1}", newFile, bakFile));
					if (File.Exists(bakFile))
						File.Delete(bakFile);

					if (File.Exists(newFile))
					{
						try { File.Move(newFile, bakFile); }
						catch { U.L(LogLevel.Error, "UPGRADE", String.Format("Failed to rename {0}", newFile)); }
					}

					U.L(LogLevel.Information, "UPGRADE", String.Format("Upgrading {0}", newFile));
					try { File.Copy(oldFile, newFile, true); }
					catch { U.L(LogLevel.Error, "UPGRADE", String.Format("Failed to upgrade {0}", newFile)); }
				}
			}

			// we need to move the settings with us during upgrade, in case .NET decides to use a new folder
			if (File.Exists(config.FilePath))
			{
				U.L(LogLevel.Debug, "UPGRADE", String.Format("Creating destination for settings file: " + newConfigFolder));
				if (!Directory.Exists(newConfigFolder))
					Directory.CreateDirectory(newConfigFolder);

				string newSettingsFile = Path.Combine(newConfigFolder, "user.config");
				U.L(LogLevel.Debug, "UPGRADE", String.Format("Move settings file to: " + newSettingsFile));
				if (File.Exists(newSettingsFile))
					File.Move(newSettingsFile, newSettingsFile + ".old");
				File.Copy(config.FilePath, newSettingsFile);
			}

			// remove temporary upgrade folder
			if (Directory.Exists(Path.Combine(baseFolder, upgradeFolder)))
			{
				U.L(LogLevel.Debug, "UPGRADE", "Removing temporary upgrade folder");
				try
				{
					Directory.Delete(Path.Combine(baseFolder, upgradeFolder), true);
				}
				catch (Exception e)
				{
					U.L(LogLevel.Debug, "UPGRADE", "Could not remove upgrade folder: " + e.Message);
				}
			}
		}

		/// <summary>
		/// Removes any temporary backup files and unused settings files.
		/// </summary>
		public static void Clear()
		{
			try
			{
				DirectoryInfo folderInfo = new DirectoryInfo(baseFolder);
				FileInfo[] files = folderInfo.GetFiles("bak.*", SearchOption.AllDirectories);
				foreach (FileInfo file in files)
				{
					U.L(LogLevel.Debug, "UPGRADE", String.Format("Removing temporary backup file: {0}", file.FullName));
					File.Delete(file.FullName);
				}

				Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
				string[] dirs = Directory.GetDirectories(Path.GetDirectoryName(Path.GetDirectoryName(config.FilePath)));
				foreach (string dir in dirs)
				{
					if (dir != Path.GetDirectoryName(config.FilePath))
					{
						U.L(LogLevel.Debug, "UPGRADE", String.Format("Removing unused settings folder: {0}", dir));
						Directory.Delete(dir, true);
					}
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Warning, "UPGRADE", "There was a problem cleaning up: " + e.Message);
			}
		}

		/// <summary>
		/// Completely removes the upgrade folder
		/// </summary>
		public static void Clean()
		{
			if (Directory.Exists(upgradeFolder))
			{
				U.L(LogLevel.Debug, "UPGRADE", "Removing temporary upgrade folder");
				try
				{
					Directory.Delete(upgradeFolder, true);
				}
				catch (Exception e)
				{
					U.L(LogLevel.Debug, "UPGRADE", "Could not remove upgrade folder: " + e.Message);
				}
			}
		}

		#endregion

		#region Private

		/// <summary>
		/// Called when the progress of the download is changed to update the progressbar.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void Client_ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			if (Policy != UpgradePolicy.Automatic && !ForceDownload)
				return;

			DispatchProgressChanged(e.ProgressPercentage, new ProgressState(String.Format("{0}%", e.ProgressPercentage), false));
		}

		/// <summary>
		/// Notifies the user that an upgrade is available.
		/// </summary>
		/// <param name="versions">A list of all available versions</param>
		private static void Notify(String[] versions)
		{
			if (Policy != UpgradePolicy.Automatic)
				DispatchUpgradeFound();
		}

		#endregion

		#region Event handlers


		/// <summary>
		/// Invoked when a property of the settings manager changes
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void SettingsManager_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "UpgradePolicy":
					if (SettingsManager.UpgradePolicy == UpgradePolicy.Manual)
						Stop();
					else
					{
						Start();
						Probe(null);
					}
					break;
			}
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// The dispatcher of the <see cref="ProgressChanged"/> event
		/// </summary>
		/// <param name="progressPercentage">The playlist that was modified</param>
		/// <param name="userState">The type of modification that occured</param>
		private static void DispatchProgressChanged(int progressPercentage, ProgressState userState)
		{
			if (ProgressChanged != null)
				ProgressChanged(null, new ProgressChangedEventArgs(progressPercentage, userState));
		}

		/// <summary>
		/// The dispatcher of the <see cref="ErrorOccured"/> event
		/// </summary>
		/// <param name="message">The error message</param>
		private static void DispatchErrorOccured(string message)
		{
			if (ErrorOccured != null)
				ErrorOccured(null, message);
		}

		/// <summary>
		/// The dispatcher of the <see cref="UpgradeFound"/> event
		/// </summary>
		private static void DispatchUpgradeFound()
		{
			if (UpgradeFound != null)
				UpgradeFound(null, null);
		}

		/// <summary>
		/// The dispatcher of the <see cref="Upgraded"/> event
		/// </summary>
		private static void DispatchUpgraded()
		{
			if (Upgraded != null)
				Upgraded(null, null);
		}

		/// <summary>
		/// The dispatcher of the <see cref="Checked"/> event
		/// </summary>
		private static void DispatchChecked()
		{
			if (Checked != null)
				Checked(null, null);
		}

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Occurs when the progress of an upgrade is changed
		/// </summary>
		public static event ProgressChangedEventHandler ProgressChanged;

		/// <summary>
		/// Occurs when an error is encountered
		/// </summary>
		public static event ErrorEventHandler ErrorOccured;

		/// <summary>
		/// Occurs when an upgrade is found
		/// </summary>
		public static event EventHandler UpgradeFound;

		/// <summary>
		/// Occurs when the application has been upgraded
		/// </summary>
		public static event EventHandler Upgraded;

		/// <summary>
		/// Occurs when a check for new upgrades has completed
		/// </summary>
		public static event EventHandler Checked;

		#endregion
	}

	#region Delegates

	/// <summary>
	/// Represents the method that will be called when an ErrorEvent occurs
	/// </summary>
	/// <param name="sender">The sender of the event</param>
	/// <param name="message">The error message</param>
	public delegate void ErrorEventHandler(object sender, string message);

	#endregion

	/// <summary>
	/// Represents the state of the progress
	/// </summary>
	public class ProgressState
	{
		/// <summary>
		/// Gets or sets the message to display
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// Gets or sets whether the progressbar is indetermined
		/// </summary>
		public bool IsIndetermined { get; set; }

		/// <summary>
		/// Creates an instance of the ProgressState class
		/// </summary>
		/// <param name="message">The message to display</param>
		/// <param name="isIndetermined">Whether the progressbar is indetermined</param>
		public ProgressState(string message, bool isIndetermined)
		{
			Message = message;
			IsIndetermined = isIndetermined;
		}
	}
}