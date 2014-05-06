/**
 * PluginManager.cs
 * 
 * Loads and handles the visualizer plugins
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.BZip2;

using Stoffi.Plugins;

namespace Stoffi.Core
{
	/// <summary>
	/// Loads and manages plugins for the visualizer.
	/// </summary>
	public static class PluginManager
	{
		#region Members

		private static string baseFolder = U.BasePath;
		private static string pluginFolder = "Plugins";
		private static List<Plugin> plugins = new List<Plugin>();
		private static List<Plugin> activePlugins = new List<Plugin>();
		private static SortedList<string,string> pluginPaths = new SortedList<string,string>();
		private static Timer ticker = null;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the collection of visualizers used in a dropdown for selecting
		/// the active visualizer.
		/// </summary>
		/// <remarks>
		/// Includes a null visualizer as the first item for selecting no active visualizer.
		/// </remarks>
		public static ObservableCollection<PluginItem> VisualizerSelector { get; private set; }

		#endregion

		#region Constructor

		static PluginManager()
		{
			if (SettingsManager.Plugins == null)
				SettingsManager.Plugins = new ObservableCollection<PluginItem>();

			SettingsManager.Plugins.Clear();

            VisualizerSelector = new ObservableCollection<PluginItem>();
            VisualizerSelector.Add(new PluginItem()
			{
				ID = null,
				Name = U.T("NoVisualizer")
			});

			DispatchRefreshVisualizerSelector();
			SettingsManager.Plugins.CollectionChanged += new NotifyCollectionChangedEventHandler(Plugins_CollectionChanged);
			SettingsManager.PropertyChanged += new PropertyChangedWithValuesEventHandler(SettingsManager_PropertyChanged);
		}

		#endregion

		#region Methods

		#region Public

		/// <summary>
		/// Initializes the manager.
		/// </summary>
		/// <remarks>
		/// Will install all plugins in the plugin folder and activate the current visualizer if there is one.
		/// </remarks>
		public static void Initialize()
		{
            Clean();
			Install();
			ActivateVisualizer(SettingsManager.CurrentVisualizer);
		}

		/// <summary>
		/// Installs all available plugins.
		/// </summary>
		public static void Install()
		{
			string src = Path.Combine(baseFolder, pluginFolder);
			if (!Directory.Exists(src))
			{
				U.L(LogLevel.Debug, "PLUGINS", "Plugin folder not found");
				return;
			}

			U.L(LogLevel.Information, "PLUGINS", "Installing plugins from folder: " + src);

			DirectoryInfo folderInfo = new DirectoryInfo(src);
			FileInfo[] files = folderInfo.GetFiles("*.spp", SearchOption.AllDirectories);

			// iterate files in folder
			foreach (FileInfo file in files)
			{
				U.L(LogLevel.Information, "PLUGINS", "Installed plugin: " + file.FullName);
				Install(file.FullName);
			}			
		}

		/// <summary>
		/// Installs a single plugin.
		/// </summary>
		/// <param name="path">The path to the plugin to install</param>
		/// <param name="copy">Whether or not to copy the plugin into the plugin folder before loading it</param>
		public static void Install(string path, bool copy = false)
		{
			if (pluginPaths.ContainsKey(path))
			{
				U.L(LogLevel.Warning, "PLUGIN", "Plugin already installed: " + path);
			}
			else if (File.Exists(path))
			{
				string p = null;
				if (copy)
				{
					// we gracefully continue if copying was unsuccessful and load
					// the original path instead.
					p = Copy(path);
					if (p != null)
						path = p;
				}

				// we gracefully continue even if unpacking was unsuccessful.
				// this means that the path could actually point to the DLL
				// directly instead of the package, and it will still work.
				p = Unpack(path);
				string dllPath = path;
				if (p != null)
					dllPath = p;

				if (File.Exists(dllPath))
				{
					try
					{
						Assembly pluginAssembly = Assembly.LoadFrom(dllPath);

						// get the types of classes that are in this assembly.
						Type[] types = pluginAssembly.GetTypes();

						// loop through the types in the assembly until we find Plugin.
						foreach (Type type in types)
						{
							Console.WriteLine(pluginAssembly.CodeBase);
							Console.WriteLine(pluginAssembly.FullName);
							Version version = pluginAssembly.GetName().Version;
							string id = Path.GetFileNameWithoutExtension(dllPath);
							object[] args = new object[] { id, version };
							Plugin plugin = (Plugin)Activator.CreateInstance(type, args);

							if (plugin != null)
							{
								U.L(LogLevel.Debug, "PLUGINS", String.Format("Found type {0} in {1}", plugin.Type.ToString(), dllPath));
								if (plugin.Type == PluginType.Visualizer || plugin.Type == PluginType.Filter)
								{
									pluginPaths.Add(path, plugin.ID);
									LoadLanguages(plugin, Path.GetDirectoryName(dllPath));
									plugin.CurrentCulture = SettingsManager.Culture.IetfLanguageTag;
									Install(plugin);
								}
							}
						}
					}
					catch (Exception e)
					{
						U.L(LogLevel.Warning, "PLUGINS", "Could not install plugin: " + e.Message);
					}
				}
			}
		}

		/// <summary>
		/// Installs a single plugin.
		/// </summary>
		/// <param name="plugin">The plugin to install</param>
		public static void Install(Plugin plugin)
		{
			if (plugin.OnInstall())
			{
				plugins.Add(plugin);

                // load remembered settings
                PluginSettings set = GetSettings(plugin.ID);
                if (set != null)
                {
                    foreach (Setting savedSetting in set.Settings)
                    {
                        foreach (Setting pluginSetting in plugin.Settings)
                        {
                            if (pluginSetting.ID == savedSetting.ID &&
                                pluginSetting.Type == savedSetting.Type)
                            {
                                try
                                {
                                    pluginSetting.Value = savedSetting.Value;
                                }
                                catch (Exception e)
                                {
                                    U.L(LogLevel.Warning, "PLUGINS", String.Format("Could not load saved setting {0}: {1}", savedSetting.ID, e.Message));
                                }
                                break;
                            }
                        }
                    }
                    set.Settings = plugin.Settings;
                }

                if (set == null)
                {
                    SettingsManager.PluginSettings.Add(new PluginSettings
                    {
                        PluginID = plugin.ID,
                        Settings = plugin.Settings,
                        Installed = DateTime.Now,
                        Enabled = false
                    });
                }

                DispatchInstalled(plugin);

                if (set != null && set.Enabled && plugin.Type != PluginType.Visualizer)
                    Start(plugin);
			}
			else
                U.L(LogLevel.Warning, "PLUGIN", "Could not install plugin '" + plugin.ID + "'");
		}

		/// <summary>
		/// Starts a plugin.
		/// </summary>
		/// <param name="name">The name of the plugin to start</param>
		public static void Start(string name)
		{
			Plugin p = Get(name);
			if (p != null)
				Start(p);
			else
				U.L(LogLevel.Warning, "PLUGIN", "Trying to start non-existing plugin: " + name);
		}

		/// <summary>
		/// Starts a plugin.
		/// </summary>
		/// <param name="index">The index of the plugin to start</param>
		public static void Start(int index)
		{
			if (index >= 0 && index < plugins.Count)
				Start(plugins[index]);
			else
				U.L(LogLevel.Warning, "PLUGIN", "Trying to start non-existing plugin: #" + index);
		}

		/// <summary>
		/// Starts a plugin.
		/// </summary>
		/// <param name="plugin">The plugin to start</param>
		public static void Start(Plugin plugin)
		{
			if (!activePlugins.Contains(plugin))
			{
				if (plugin.OnStart())
				{
					DispatchStarted(plugin);
                    activePlugins.Add(plugin);
                    PluginSettings set = GetSettings(plugin.ID);
                    if (set != null)
                        set.Enabled = true;
                    PluginItem p = GetListItem(plugin.ID);
					if (p != null)
						p.Disabled = false;
					if (ticker == null)
						ticker = new Timer(Tick, null, 0, 30);

					if (plugin.Type == PluginType.Filter)
					{
						Plugins.Filter filter = plugin as Plugins.Filter;
						filter.VolumeChanged += Plugin_VolumeChanged;
					}

                    U.L(LogLevel.Information, "PLUGIN", "Started plugin '" + plugin.ID + "'");
				}
				else
                    U.L(LogLevel.Warning, "PLUGIN", "Could not start plugin '" + plugin.ID + "'");
			}
			else
                U.L(LogLevel.Warning, "PLUGIN", "Won't start running plugin '" + plugin.ID + "'");
		}

		/// <summary>
		/// Stops all plugins.
		/// </summary>
		public static void Stop()
		{
			foreach (Plugin p in plugins)
				if (activePlugins.Contains(p))
					Stop(p);
		}

		/// <summary>
		/// Stops a plugin.
		/// </summary>
		/// <param name="name">The name of the plugin to stop</param>
		public static void Stop(string name)
		{
			Plugin p = Get(name);
			if (p != null)
				Stop(p);
			else
				U.L(LogLevel.Warning, "PLUGIN", "Trying to stop non-existing plugin: " + name);
		}

		/// <summary>
		/// Stops a plugin.
		/// </summary>
		/// <param name="index">The index of the plugin to stop</param>
		public static void Stop(int index)
		{
			if (index >= 0 && index < plugins.Count)
				Stop(plugins[index]);
			else
				U.L(LogLevel.Warning, "PLUGIN", "Trying to stop non-existing plugin: #" + index);
		}

		/// <summary>
		/// Stops a plugin.
		/// </summary>
		/// <param name="plugin">The plugin to stop</param>
		public static void Stop(Plugin plugin)
		{
			if (activePlugins.Contains(plugin))
			{
				if (plugin.OnStart())
				{
					DispatchStopped(plugin);
                    activePlugins.Remove(plugin);

                    PluginSettings set = GetSettings(plugin.ID);
                    if (set != null)
                        set.Enabled = false;
                    PluginItem p = GetListItem(plugin.ID);
					if (p != null)
                        p.Disabled = true;

					if (activePlugins.Count == 0)
					{
						ticker.Dispose();
						ticker = null;
					}

					if (plugin.Type == PluginType.Filter)
					{
						Plugins.Filter filter = plugin as Plugins.Filter;
						filter.VolumeChanged -= Plugin_VolumeChanged;
					}

                    U.L(LogLevel.Information, "PLUGIN", "Stopped plugin '" + plugin.ID + "'");
				}
				else
                    U.L(LogLevel.Warning, "PLUGIN", "Could not stop plugin '" + plugin.ID + "'");
			}
			else
                U.L(LogLevel.Warning, "PLUGIN", "Won't stop not running plugin '" + plugin.ID + "'");
		}

		/// <summary>
		/// Uninstalls all installed plugins.
		/// </summary>
		public static void Uninstall()
		{
			foreach (Plugin p in plugins)
				Uninstall(p);
		}

		/// <summary>
		/// Uninstalls a plugin.
		/// </summary>
		/// <param name="name">The name of the plugin to uninstall</param>
		public static void Uninstall(string name)
		{
			Plugin p = Get(name);
			if (p != null)
				Uninstall(p);
			else
				U.L(LogLevel.Warning, "PLUGIN", "Trying to release non-existing plugin: " + name);
		}

		/// <summary>
		/// Uninstalls a plugin.
		/// </summary>
		/// <param name="index">The index of the plugin to uninstall</param>
		public static void Uninstall(int index)
		{
			if (index >= 0 && index < plugins.Count)
				Uninstall(plugins[index]);
			else
				U.L(LogLevel.Warning, "PLUGIN", "Trying to release non-existing plugin: #" + index);
		}

		/// <summary>
		/// Uninstalls a plugin.
		/// </summary>
		/// <param name="plugin">The plugin to uninstall</param>
		public static void Uninstall(Plugin plugin)
		{
            if (PluginManager.IsActive(plugin))
                Stop(plugin);
			if (!plugin.OnUninstall())
                U.L(LogLevel.Warning, "PLUGIN", String.Format("Plugin '{0}' was not uninstalled correctly", plugin.ID));
            if (pluginPaths.ContainsValue(plugin.ID))
            {
                string path = pluginPaths.Keys.ElementAt<string>(pluginPaths.Values.IndexOf(plugin.ID));
                string name = Path.GetFileNameWithoutExtension(path);
                pluginPaths.Remove(path);
                Remove(name);
			}

			// forget settings
			for (int i=0; i < SettingsManager.PluginSettings.Count; i++)
			{
				if (SettingsManager.PluginSettings[i].PluginID == plugin.ID)
				{
					SettingsManager.PluginSettings.RemoveAt(i);
					break;
				}
			}

			DispatchUninstalled(plugin);
			plugins.Remove(plugin);
		}

		/// <summary>
		/// Tries to get a plugin with a given ID.
		/// </summary>
		/// <param name="id">The ID of the plugin</param>
		/// <returns>The plugin if found, otherwise null</returns>
		public static Plugin Get(string id)
		{
			foreach (Plugin p in plugins)
                if (p.ID == id)
					return p;
			return null;
		}

		/// <summary>
		/// Tries to get a list item representing a plugin with a given ID.
		/// </summary>
		/// <param name="id">The ID of the plugin</param>
		/// <returns>The item if found, otherwise null</returns>
		public static PluginItem GetListItem(string id)
		{
			foreach (PluginItem p in SettingsManager.Plugins)
                if (p.ID == id)
					return p;
			return null;
		}

        public static PluginSettings GetSettings(string id)
        {
            foreach (PluginSettings s in SettingsManager.PluginSettings)
                if (s.PluginID == id)
                    return s;
            return null;
        }

        /// <summary>
        /// Gets whether or not a given plugin is running.
        /// </summary>
        /// <param name="plugin">The plugin to check</param>
        /// <returns>True if the plugin is running, otherwise false</returns>
        public static bool IsActive(Plugin plugin)
        {
            return activePlugins.Contains<Plugin>(plugin);
        }

		#endregion

		#region Private

		/// <summary>
		/// Unpacks a plugin package.
		/// </summary>
		/// <param name="path">The path to the package</param>
		/// <returns>The path to the unpacked DLL file of the plugin</returns>
		private static string Unpack(string path)
		{
			try
			{
				string suffix = Path.GetRandomFileName();
				suffix = "_" + suffix.Replace(".", "");
				string bz2File = path;
				string tarFile = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".tar");
				string dest = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + suffix);

				U.L(LogLevel.Debug, "PLUGIN", "Decompressing " + path);
				BZip2.Decompress(File.OpenRead(bz2File), File.Create(tarFile), true);

				Stream inStream = File.OpenRead(tarFile);
				TarArchive archive = TarArchive.CreateInputTarArchive(inStream, TarBuffer.DefaultBlockFactor);
				Directory.CreateDirectory(dest);
				archive.ExtractContents(dest);
				archive.Close();
				inStream.Close();
				File.Delete(tarFile);

				return Path.Combine(dest, Path.GetFileNameWithoutExtension(path) + ".dll");
			}
			catch (Exception e)
			{
				U.L(LogLevel.Error, "PLUGIN", "Could not unpack plugin package: " + e.Message);
				return null;
			}
		}

		/// <summary>
		/// Copies a plugin package to the application plugin folder.
		/// </summary>
		/// <param name="path">The path of the plugin to copy</param>
		/// <returns>The new path of the copied plugin</returns>
		private static string Copy(string path)
		{
			if (!Directory.Exists(Path.Combine(baseFolder, pluginFolder)))
			{
				try
				{
					Directory.CreateDirectory(Path.Combine(baseFolder, pluginFolder));
				}
				catch (Exception e)
				{
					U.L(LogLevel.Error, "PLUGIN", "Could not create plugin folder: " + e.Message);
					return null;
				}
			}
			try
			{
				string newPath = Path.Combine(baseFolder, pluginFolder, Path.GetFileName(path));
				File.Copy(path, newPath);
				return newPath;
			}
			catch (Exception e)
			{
				U.L(LogLevel.Error, "PLUGIN", "Could not copy plugin package: " + e.Message);
				return null;
			}
		}

        /// <summary>
        /// Remove the package and unpacked folder of a given plugin inside the plugin folder.
        /// </summary>
        /// <param name="name">The name of the plugin</param>
        /// <returns>True if the removal was successful, otherwise false</returns>
        private static bool Remove(string name)
        {
            string package = Path.Combine(baseFolder, pluginFolder, name + ".spp");
            try
            {
                if (File.Exists(package))
                    File.Delete(package);
                return true;
            }
            catch (Exception e)
            {
                U.L(LogLevel.Error, "PLUGIN", "Could not remove plugin from folder: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Cleans the plugin folder from temporary unpacked folders for non-existing packages.
        /// </summary>
        private static void Clean()
        {
            string src = Path.Combine(baseFolder, pluginFolder);
            if (!Directory.Exists(src))
                return;

            DirectoryInfo folderInfo = new DirectoryInfo(src);
            DirectoryInfo[] folders = folderInfo.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo folder in folders)
			{
				try
				{
					Directory.Delete(folder.FullName, true);
				}
				catch (Exception e)
				{
					U.L(LogLevel.Warning, "PLUGIN", "Could not remove temporary folder: " + e.Message);
				}
            }
        }

		/// <summary>
		/// Activates a given visualizer and disables all other.
		/// </summary>
		/// <remarks>
		/// Only one visualizer may be active at any given moment.
		/// </remarks>
		/// <param name="visualizer">The name of the visualizer to activate. If null then all visualizer will be stopped.</param>
		private static void ActivateVisualizer(string visualizer = null)
		{
			ActivateVisualizer(Get(visualizer) as Visualizer);
		}
	
		/// <summary>
		/// Activates a given visualizer and disables all other.
		/// </summary>
		/// <remarks>
		/// Only one visualizer may be active at any given moment.
		/// </remarks>
		/// <param name="visualizer">The visualizer to activate. If null then all visualizer will be stopped.</param>
		private static void ActivateVisualizer(Visualizer visualizer = null)
		{
			foreach (Plugin p in plugins)
			{
				if (p.Type == PluginType.Visualizer &&
                    p.ID == SettingsManager.CurrentVisualizer)
					Start(p);
				else if (p.Type == PluginType.Visualizer)
					Stop(p);
			}
		}

        /// <summary>
        /// Loads the language files of a plugin.
        /// </summary>
        /// <param name="plugin">The plugin onto which to put the loaded languages</param>
        /// <param name="folder">The folder in which to search for language files</param>
        /// <returns>Whether or not the operation was successful</returns>
        private static bool LoadLanguages(Plugin plugin, string folder)
        {
            try
            {
                string p = Path.Combine(folder, "Languages");

                if (Directory.Exists(p))
                {
                    DirectoryInfo d = new DirectoryInfo(p);
                    foreach (FileInfo f in d.GetFiles("*.xml", SearchOption.AllDirectories))
                    {
                        string ietf = Path.GetFileNameWithoutExtension(f.Name);
                        try
                        {
                            XmlSerializer ser = new XmlSerializer(typeof(List<Translation>));
                            FileStream fs = new FileStream(f.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                            List<Translation> t = (List<Translation>)ser.Deserialize(fs);
                            fs.Close();

                            plugin.Languages.Add(new Language
                            {
                                Culture = ietf,
                                Translations = t
                            });
                        }
                        catch (Exception e)
                        {
                            U.L(LogLevel.Warning, "PLUGIN",
                                String.Format("Problem loading language file {0} for plugin '{1}': {2}", f.Name, plugin.ID, e.Message));
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                U.L(LogLevel.Warning, "PLUGIN",
                    String.Format("Problem loading languages for plugin '{0}': {1}", plugin.ID, e.Message));
                return false;
            }
        }

		#endregion

		#region Event handlers

		/// <summary>
		/// Invoked when a property of the SettingsMananger is changed.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void SettingsManager_PropertyChanged(object sender, PropertyChangedWithValuesEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "Plugins":
					SettingsManager.Plugins.CollectionChanged += new NotifyCollectionChangedEventHandler(Plugins_CollectionChanged);
					break;

				case "CurrentVisualizer":
					ActivateVisualizer(SettingsManager.CurrentVisualizer);
					break;
			}
		}

		/// <summary>
		/// Invoked when the collection of plugins changes.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void Plugins_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			DispatchRefreshVisualizerSelector();
		}

		/// <summary>
		/// Called by the periodic timer for refreshing active plugins.
		/// </summary>
		/// <param name="state">The state (not used)</param>
		private static void Tick(object state)
		{
			if (activePlugins.Count == 0)
			{
				if (ticker != null)
					ticker.Dispose();
				ticker = null;
			}
			else
			{
				try
				{
					foreach (Plugin p in activePlugins)
					{
						switch (p.Type)
						{
							case PluginType.Visualizer:
								Plugins.Visualizer v = p as Plugins.Visualizer;
								MediaManager.FFTData.CopyTo(v.FFTData, 0);
								DispatchRefresh(v);
								break;

							case PluginType.Filter:
								p.Refresh();
								break;
						}
					}
				}
				catch (Exception e)
				{
					U.L(LogLevel.Warning, "PLUGIN", "Could not refresh plugins: " + e.Message);
				}
			}
		}

		/// <summary>
		/// Invoked when a plugin asks to change volume.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private static void Plugin_VolumeChanged(object sender, Plugins.GenericEventArgs<double> e)
		{
			SettingsManager.Volume = e.Value;
		}

		#endregion

		#region Dispatchers

		/// <summary>
		/// Dispatches the Started event.
		/// </summary>
		/// <param name="plugin">The plugin that was started</param>
		private static void DispatchStarted(Plugin plugin)
		{
			if (Started != null)
				Started(null, new PluginEventArgs(plugin));
		}

		/// <summary>
		/// Dispatches the Stopped event.
		/// </summary>
		/// <param name="plugin">The plugin that was stopped</param>
		private static void DispatchStopped(Plugin plugin)
		{
			if (Stopped != null)
				Stopped(null, new PluginEventArgs(plugin));
		}

		/// <summary>
		/// Dispatches the Installed event.
		/// </summary>
		/// <param name="plugin">The plugin that was installed</param>
		private static void DispatchInstalled(Plugin plugin)
		{
			if (Installed != null)
				Installed(null, new PluginEventArgs(plugin));
		}

		/// <summary>
		/// Dispatches the Uninstalled event.
		/// </summary>
		/// <param name="plugin">The plugin that was uninstalled</param>
		private static void DispatchUninstalled(Plugin plugin)
		{
			if (Uninstalled != null)
				Uninstalled(null, new PluginEventArgs(plugin));
		}

        /// <summary>
        /// Dispatches the Refresh event.
        /// </summary>
        /// <param name="plugin">The plugin that needs to be refreshed</param>
        private static void DispatchRefresh(Plugin plugin)
        {
            if (Refresh != null)
                Refresh(null, new PluginEventArgs(plugin));
        }

		/// <summary>
		/// Dispatches the RefreshVisualizerSelector event.
		/// </summary>
		private static void DispatchRefreshVisualizerSelector()
		{
			if (RefreshVisualizerSelector != null)
				RefreshVisualizerSelector(null, new EventArgs());
		}

		#endregion

		#endregion

		#region Events

		/// <summary>
		/// Occurs when a plugin has been started.
		/// </summary>
		public static event EventHandler<PluginEventArgs> Started;

		/// <summary>
		/// Occurs when a plugin has been stopped.
		/// </summary>
        public static event EventHandler<PluginEventArgs> Stopped;

		/// <summary>
		/// Occurs when a plugin has been loaded.
		/// </summary>
        public static event EventHandler<PluginEventArgs> Installed;

		/// <summary>
		/// Occurs when a plugin has been released.
		/// </summary>
        public static event EventHandler<PluginEventArgs> Uninstalled;

        /// <summary>
        /// Occurs when it is time to repaint the visualizer.
        /// </summary>
        public static event EventHandler<PluginEventArgs> Refresh;

		/// <summary>
		/// Occurs when there's a need to update the VisualizerSelector.
		/// </summary>
		/// <remarks>
		/// This cannot be done by the manager itself since the collection
		/// can only be modified by the dispatcher thread as it will propagate
		/// up to the UI.
		/// </remarks>
		public static event EventHandler RefreshVisualizerSelector;

		#endregion
	}

	#region Event args

	/// <summary>
	/// Provides data for a plugin related event.
	/// </summary>
	public class PluginEventArgs : EventArgs
	{
		/// <summary>
		/// The plugin.
		/// </summary>
		public Plugin Plugin { get; private set; }

		/// <summary>
		/// Creates an instance of plugin event arguments.
		/// </summary>
		/// <param name="plugin">The plugin</param>
		public PluginEventArgs(Plugin plugin)
		{
			Plugin = plugin;
		}
	}

	#endregion

	#region Data structures

	/// <summary>
	/// Describes a plugin item in a list view
	/// </summary>
	public class PluginItem : ViewDetailsItemData
	{
		#region Members

        private string id;
		private string name;
		private string description;
		private string author;
		private string url;
        private Version version;
		private DateTime installed;
		private PluginType type;

		#endregion

        #region Properties

        /// <summary>
        /// Gets or sets the identifier of the plugin
        /// </summary>
        public string ID
        {
            get { return id; }
            set { id = value; OnPropertyChanged("ID"); }
        }

		/// <summary>
		/// Gets or sets the name of the plugin
		/// </summary>
		public string Name
		{
			get { return name; }
			set { name = value; OnPropertyChanged("Name"); }
		}

		/// <summary>
		/// Gets or sets the description of the plugin
		/// </summary>
		public string Description
		{
			get { return description; }
			set { description = value; OnPropertyChanged("Description"); }
		}

		/// <summary>
		/// Gets or sets the author of the plugin
		/// </summary>
		public string Author
		{
			get { return author; }
			set { author = value; OnPropertyChanged("Author"); }
		}

		/// <summary>
		/// Gets or sets the URL of the author's or plugin's website
		/// </summary>
		public string URL
		{
			get { return url; }
			set { url = value; OnPropertyChanged("URL"); }
		}

        /// <summary>
        /// Gets or sets the version of the plugin.
        /// </summary>
        public Version Version
        {
            get { return version; }
            set { version = value; OnPropertyChanged("Version"); }
        }

		/// <summary>
		/// Gets or sets the time that the plugin was installed
		/// </summary>
		public DateTime Installed
		{
			get { return installed; }
			set { installed = value; OnPropertyChanged("Installed"); }
		}

		/// <summary>
		/// Gets or sets the type of the plugin
		/// </summary>
		public PluginType Type
		{
			get { return type; }
			set { type = value; OnPropertyChanged("Type"); OnPropertyChanged("HumanType"); }
		}

		#endregion
	}

	#endregion
}
