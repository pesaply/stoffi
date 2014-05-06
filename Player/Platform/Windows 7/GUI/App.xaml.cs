/**
 * App.xaml.cs
 * 
 * The first code that runs during startup.
 * Checks for other running instances and takes
 * care of communication with other instances.
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
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using Un4seen.Bass;
using BASSSync = Un4seen.Bass.BASSSync;
using BASSInput = Un4seen.Bass.BASSInput;
using BassActive = Un4seen.Bass.BASSActive;

using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Win32;
using GlassLib;
using Tomers.WPF.Localization;

namespace Stoffi
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		#region Members

		private KeyboardListener kListener = new KeyboardListener();
		private string identifier = "165de6c3da87d45d5f5a3c2d75";
		private string languageFolder = @"Languages\";

		#endregion

		#region Constructor

		/// <summary>
		/// Creates an instance of the App.
		/// </summary>
		App()
		{
			U.Level = LogLevel.Debug;
			U.L(LogLevel.Information, "APP", "Starting up");

			string a = Assembly.GetExecutingAssembly().CodeBase;
			Uri b = new Uri(a);
			string c = b.AbsolutePath;
			string d = Uri.EscapeDataString(c);

			string baseFolder = U.BasePath;
			languageFolder = Path.Combine(baseFolder, languageFolder);

			U.L(LogLevel.Debug, "APP", "Loading languages from " + languageFolder);

			if (!Directory.Exists(languageFolder))
				languageFolder = Path.Combine(baseFolder, @"..\..\Platform\Windows 7\GUI\Languages");

			LanguageDictionary.RegisterDictionary(
				CultureInfo.GetCultureInfo("en-US"),
				new XmlLanguageDictionary(Path.Combine(languageFolder, "en-US.xml")));

			LanguageDictionary.RegisterDictionary(
				CultureInfo.GetCultureInfo("nb-NO"),
				new XmlLanguageDictionary(Path.Combine(languageFolder, "nb-NO.xml")));

			LanguageDictionary.RegisterDictionary(
				CultureInfo.GetCultureInfo("sv-SE"),
				new XmlLanguageDictionary(Path.Combine(languageFolder, "sv-SE.xml")));

			LanguageDictionary.RegisterDictionary(
				CultureInfo.GetCultureInfo("zh-CN"),
				new XmlLanguageDictionary(Path.Combine(languageFolder, "zh-CN.xml")));

			LanguageDictionary.RegisterDictionary(
				CultureInfo.GetCultureInfo("pt-BR"),
				new XmlLanguageDictionary(Path.Combine(languageFolder, "pt-BR.xml")));

			LanguageDictionary.RegisterDictionary(
				CultureInfo.GetCultureInfo("de-DE"),
				new XmlLanguageDictionary(Path.Combine(languageFolder, "de-DE.xml")));

			LanguageDictionary.RegisterDictionary(
				CultureInfo.GetCultureInfo("it-IT"),
				new XmlLanguageDictionary(Path.Combine(languageFolder, "it-IT.xml")));

			LanguageDictionary.RegisterDictionary(
				CultureInfo.GetCultureInfo("hu-HU"),
				new XmlLanguageDictionary(Path.Combine(languageFolder, "hu-HU.xml")));

			string lang = Stoffi.Properties.Settings.Default.Language;
			if (lang == null)
				lang = Thread.CurrentThread.CurrentUICulture.IetfLanguageTag;
			CultureInfo ci = CultureInfo.GetCultureInfo(lang);
			U.L(LogLevel.Debug, "APP", String.Format("Setting culture: {0} ({1})", ci.TwoLetterISOLanguageName, ci.IetfLanguageTag));
			LanguageContext.Instance.Culture = ci;
			Thread.CurrentThread.CurrentUICulture = ci;

			// check arguments
			string[] arguments = Environment.GetCommandLineArgs();

			// restart
			bool restarting = arguments.Length > 1 && arguments.Contains<string>("--restart");

			// register associations
			if (arguments.Length == 3 && arguments[1] == "--associate")
			{
				U.L(LogLevel.Information, "APP", "Associating file types and URI handles");
				SetAssociations(arguments[2] == "true");
				Application.Current.Shutdown();
				return;
			}

			// uninstall
			else if (arguments.Length == 2 && arguments[1] == "--uninstall")
			{
				Uninstall();
				Application.Current.Shutdown();
				return;
			}

			// find out if Stoffi is already running
			if (!restarting)
			{
				Process ThisProcess = Process.GetCurrentProcess();
				Process[] SameProcesses = Process.GetProcessesByName(ThisProcess.ProcessName);
				U.L(LogLevel.Debug, "APP", "Checking for processes named: " + ThisProcess.ProcessName);
				if (SameProcesses.Length > 1) // Stoffi is already running!
				{
					U.L(LogLevel.Information, "APP", "Another instance is already running");

					// pass arguments to first instance
					try
					{
						using (NamedPipeClientStream client = new NamedPipeClientStream(identifier.ToString()))
						{
							using (StreamWriter writer = new StreamWriter(client))
							{
								U.L(LogLevel.Debug, "APP", "Sending arguments");
								client.Connect(200);

								foreach (String argument in arguments)
									writer.WriteLine(argument);

								if (arguments.Count() == 1)
								{
									U.L(LogLevel.Debug, "APP", "Trying to raise window");
									SetForegroundWindow(SameProcesses[0].MainWindowHandle);
									ShowWindow(SameProcesses[0].MainWindowHandle, 9);
								}

								// shut down
								U.L(LogLevel.Information, "APP", "Dying gracefully after sending data to running instance");
								Application.Current.Shutdown();
								return;
							}
						}
					}
					catch (TimeoutException exc)
					{
						U.L(LogLevel.Error, "APP", "Couldn't connect to server: " + exc.Message);
					}
					catch (IOException exc)
					{
						U.L(LogLevel.Error, "APP", "Pipe was broken: " + exc.Message);
					}
					catch (Exception exc)
					{
						U.L(LogLevel.Error, "APP", "Couldn't send arguments: " + exc.Message);
					}
					U.L(LogLevel.Warning, "APP", "Couldn't contact the other instance; I declare it dead and take its place");
				}
			}

			if (!Microsoft.WindowsAPICodePack.Taskbar.TaskbarManager.IsPlatformSupported)
			{
				MessageBox.Show(U.T("MessageOldWindows", "Message"), U.T("MessageOldWindows", "Title"), MessageBoxButton.OK, MessageBoxImage.Error);
				Application.Current.Shutdown();
				return;
			}
		}

		#endregion

		#region Methods

		#region Private

		/// <summary>
		/// Method which listen for arguments from other instances
		/// </summary>
		/// <param name="state">TODO</param>
		private void ListenForArguments(Object state)
		{
			U.L(LogLevel.Debug, "APP", "Listening for arguments");
			try
			{
				using (NamedPipeServerStream server = new NamedPipeServerStream(identifier))
				using (StreamReader reader = new StreamReader(server))
				{
					server.WaitForConnection();

					List<String> arguments = new List<String>();
					while (server.IsConnected)
						arguments.Add(reader.ReadLine());

					U.L(LogLevel.Debug, "APP", "Doing argument receivement");
					ArgumentsReceived((object)arguments.ToArray());
					//ThreadPool.QueueUserWorkItem(new WaitCallback(ArgumentsReceived), arguments.ToArray());
					U.L(LogLevel.Debug, "APP", "Continuing with listening");
				}
			}
			catch (Exception e)
			{
				U.L(LogLevel.Error, "APP", "Couldn't listen for arguments: " + e.Message);
			}

			finally
			{
				ListenForArguments(null);
			}
		}

		/// <summary>
		/// Receives the arguments from ListenForArgument() and sends them to the MainWindow.
		/// </summary>
		/// <param name="state">The arguments converted from a String[] type</param>
		private void ArgumentsReceived(Object state)
		{
			U.L(LogLevel.Information, "APP", "Received arguments");
			String argument = "";
			try
			{
				String[] arguments = new String[((String[])state).Length - 1];
				Array.Copy((String[])state, 1, arguments, 0, ((String[])state).Length - 1);
				foreach (String a in arguments) argument += a + " ";
				argument = argument.Substring(0, argument.Length - 2);
			}
			catch (Exception e)
			{
				U.L(LogLevel.Debug, "APP", "Couldn't parse arguments: " + e.Message + " (" + e.StackTrace[0] + ")");
			}
			try
			{
				U.L(LogLevel.Debug, "APP", "Invoking CallFromSecondInstance with argument: " + argument);
				Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate() { ((StoffiWindow)MainWindow).CallFromSecondInstance(argument); }));
			}
			catch { }
		}

		/// <summary>
		/// Clean up the filesystem and registry from traces of Stoffi.
		/// </summary>
		private void Uninstall()
		{
			try
			{
				var filetypes = new Stoffi.Associations().FullFileList;
				UnregisterApplication(filetypes);
			}
			catch (Exception e) { MessageBox.Show(e.Message); }
			try
			{
				File.Delete(U.LogFile);
			}
			catch { }

			try
			{
				var folder = Path.GetDirectoryName(U.FullPath);
				//Directory.Delete(folder, true);
			}
			catch { }

			try
			{
				var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
				var configFolder = new DirectoryInfo(config.FilePath).Parent.Parent.FullName;
				//Directory.Delete(configFolder, true);
			}
			catch { }
		}

		/// <summary>
		/// Sets file associations in the registry.
		/// </summary>
		/// <param name="files">A list of all file types that should be associated with Stoffi.</param>
		private void SetAssociations(bool all)
		{
			var filetypes = new Stoffi.Associations().FullFileList;
			try
			{
				RegisterApplication(filetypes);
			}
			catch { }

			try
			{
				var type = Type.GetTypeFromCLSID(Shell.CLSID_ApplicationAssociationRegistration);
				var typeUI = Type.GetTypeFromCLSID(Shell.CLSID_ApplicationAssociationRegistrationUI);
				var comobj = Activator.CreateInstance(type);
				var comobjUI = Activator.CreateInstance(typeUI);
				var reg = (Shell.IApplicationAssociationRegistration)comobj;
				var regUI = (Shell.IApplicationAssociationRegistrationUI)comobjUI;

				if (all && reg != null)
					reg.SetAppAsDefaultAll("Stoffi");
				else if (regUI != null)
					regUI.LaunchAdvancedAssociationUI("Stoffi");
				Shell.SHChangeNotify(Shell.SHCNE_ASSOCCHANGED, Shell.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// Register the application in the registry.
		/// </summary>
		/// <param name="filetypes">The filetypes that are supported</param>
		private void RegisterApplication(List<string> filetypes)
		{
			var path = U.FullPath;
			var location = @"Software\Clients\Media\Stoffi\Capabilities";
			var key = Registry.LocalMachine.OpenSubKey(@"Software\RegisteredApplications", true);
			key.SetValue("Stoffi", location);
			key.Close();

			key = Registry.LocalMachine.OpenSubKey(@"Software\Clients\Media", true).CreateSubKey("Stoffi");
			key.SetValue("", U.T("Application", "Title"));
			using (var capKey = key.CreateSubKey("Capabilities"))
			{
				capKey.SetValue("ApplicationDescription", U.T("Application", "Tagline"));
				capKey.SetValue("ApplicationName", U.T("Application", "Title"));
				using (var assKey = capKey.CreateSubKey("FileAssociations"))
				{
					foreach (var filetype in filetypes)
					{
						assKey.SetValue("." + filetype, "Stoffi.AssocFile." + filetype);
						try
						{
							RegisterFileAssociation(filetype);
						}
						catch { }
					}
				}
				using (var assKey = capKey.CreateSubKey("UrlAssociations"))
				{
					foreach (var protocol in new string[] { "stoffi" })
					{
						assKey.SetValue(protocol, "Stoffi.Url." + protocol);
						try
						{
							RegisterUrlAssociation(protocol);
						}
						catch { }
					}
				}
			}
			key.Close();
		}

		/// <summary>
		/// Register the support for a given file type.
		/// </summary>
		/// <param name="filetype">The supporte file type</param>
		private void RegisterFileAssociation(string filetype)
		{
			string[] playlists = new string[] { "pls", "m3u", "m3u8" };
			string[] other = new string[] { "spp", "scx" };
			string path = U.FullPath;
			string verb = U.T("FileAssociationOpen");
			bool isPlaylist = playlists.Contains<string>(filetype);
			bool isOther = other.Contains<string>(filetype);
			var icon = Path.Combine(Path.GetDirectoryName(path), @"Icons\Win7\FileAudio.ico");
			var keyLocation = Registry.LocalMachine.OpenSubKey(@"Software\Classes", true);

			// determine name and verb
			string name;
			if (isPlaylist)
				name = String.Format(U.T("FileAssociationPlaylist"), filetype);

			else if (isOther)
			{
				name = U.T(String.Format("FileAssociation{0}", filetype.ToUpper()));
				icon = Path.Combine(Path.GetDirectoryName(path), String.Format(@"Icons\Win7\{0}.ico", filetype));
				if (filetype == "spp")
					verb = U.T("FileAssociationInstall");
				else if (filetype == "scx")
					verb = U.T("FileAssociationLoad");
			}
			else
				name = String.Format(U.T("FileAssociationSong"), filetype);
			
			// create ProgId
			var key = keyLocation.CreateSubKey(String.Format("Stoffi.AssocFile.{0}", filetype));

			// set icon
			var iconKey = key.CreateSubKey("DefaultIcon");
			iconKey.SetValue("", icon);
			iconKey.Close();

			// create shell commands
			RegistryKey shellKey = key.CreateSubKey("shell");

			// open 
			RegistryKey openKey = shellKey.CreateSubKey("Open");
			RegistryKey cmdKey = openKey.CreateSubKey("command");
			cmdKey.SetValue("", String.Format("\"{0}\" \"%1\"", path));
			cmdKey.Close();
			openKey.SetValue("", String.Format("&{0}", verb));
			openKey.Close();

			// play
			if (!isOther)
			{
				RegistryKey playKey = shellKey.CreateSubKey("PlayWithStoffi");
				cmdKey = playKey.CreateSubKey("command");
				cmdKey.SetValue("", String.Format("\"{0}\" \"%1\"", path));
				cmdKey.Close();
				playKey.SetValue("", U.T("FileAssociationPlay"));
				playKey.Close();
			}

			shellKey.SetValue("", "Open");
			shellKey.Close();

			key.SetValue("", name);
			key.Close();
		}

		/// <summary>
		/// Register the support of a given URL protocol.
		/// </summary>
		/// <param name="protocol">The supported protocol</param>
		private void RegisterUrlAssociation(string protocol)
		{
			string path = U.FullPath;
			var icon = Path.Combine(Path.GetDirectoryName(path), @"Icons\Win7\FileAudio.ico");
			var keyLocation = Registry.LocalMachine.OpenSubKey(@"Software\Classes", true);

			var key = keyLocation.CreateSubKey("Stoffi.Url." + protocol);
			key.SetValue("", String.Format("URL:{0} Protocol", U.Capitalize(protocol)));
			key.SetValue("URL Protocol", "");

			var iconKey = key.CreateSubKey("DefaultIcon");
			iconKey.SetValue("", icon);
			iconKey.Close();

			// create shell commands
			RegistryKey shellKey = key.CreateSubKey("shell");
			RegistryKey openKey = shellKey.CreateSubKey("Open");
			RegistryKey cmdKey = openKey.CreateSubKey("command");
			cmdKey.SetValue("", String.Format("\"{0}\" \"%1\"", path));
			cmdKey.Close();
			openKey.Close();
			shellKey.Close();

			key.Close();
		}

		/// <summary>
		/// Unregister Stoffi from the registry.
		/// </summary>
		/// <param name="filetypes">The supported filetypes</param>
		private void UnregisterApplication(List<string> filetypes)
		{
			// remove pointer
			RegistryKey key = null;
			try
			{
				key = Registry.LocalMachine.OpenSubKey(@"Software\RegisteredApplications", true);
				key.DeleteValue("Stoffi");
				key.Close();
			}
			catch { }

			// remove application
			try
			{
				key = Registry.LocalMachine.OpenSubKey(@"Software\Clients\Media", true);
				key.DeleteSubKeyTree("Stoffi");
				key.Close();
			}
			catch { }

			// remove associations
			try
			{
				foreach (var filetype in filetypes)
					UnregisterFileAssociation(filetype);
				foreach (var protocol in new string[] { "stoffi" })
					UnregisterUrlAssociation(protocol);
				Shell.SHChangeNotify(Shell.SHCNE_ASSOCCHANGED, Shell.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
			}
			catch { }
		}

		/// <summary>
		/// Remove the ProgId for a given filetype.
		/// </summary>
		/// <param name="filetype">The supported filetype to remove</param>
		private void UnregisterFileAssociation(string filetype)
		{
			// remove ProgId
			try
			{
				var key = Registry.LocalMachine.OpenSubKey(@"Software\Classes", true);
				key.DeleteSubKeyTree("Stoffi.AssocFile." + filetype);
				key.Close();
			}
			catch { }

			// restore previous association
			try
			{
				var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\."+filetype, true);
				var choiceKey = key.OpenSubKey("UserChoice");
				bool shouldDelete = (string)choiceKey.GetValue("Progid") == "Stoffi.AssocFile."+filetype;
				choiceKey.Close();
				if (shouldDelete)
					key.DeleteSubKey("UserChoice");
				key.Close();
			}
			catch { }
		}

		/// <summary>
		/// Remove the ProgId for a given URL protocol.
		/// </summary>
		/// <param name="filetype">The supported URL protocol to remove</param>
		private void UnregisterUrlAssociation(string protocol)
		{
			try
			{
				var key = Registry.LocalMachine.OpenSubKey(@"Software\Classes", true);
				key.DeleteSubKeyTree("." + protocol);
				key.DeleteSubKeyTree("Stoffi.Url." + protocol);
				key.Close();
			}
			catch { }
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// Called when the application is started
		/// </summary>
		/// <param name="sender">Sender of the event</param>
		/// <param name="e">Arguments of the event</param>
		private void App_Startup(object sender, StartupEventArgs e)
		{
			U.L(LogLevel.Information, "APP", "Starting Stoffi Music Player");
		}

		/// <summary>
		/// Called when the application is closed
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void App_Exit(object sender, ExitEventArgs e)
		{
			U.L(LogLevel.Information, "APP", "Shutting down");
		}

		/// <summary>
		/// Event handler for when the application crashes due to an unhandled exception
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The event data</param>
		private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			U.L(LogLevel.Error, "APP", "Crashing due to unforseen problems: " + e.Exception.Message);
			U.L(LogLevel.Error, "APP", e.Exception.StackTrace);
			U.L(LogLevel.Error, "APP", e.Exception.Source);
			Stoffi.SettingsManager.Save();
			if (Application.Current != null)
			{
				StoffiWindow stoffi = Application.Current.MainWindow as StoffiWindow;
				if (stoffi != null && stoffi.trayIcon != null)
					stoffi.trayIcon.Visibility = Visibility.Collapsed;
			}
		}

		#endregion

		#endregion

		#region Overrides

		/// <summary>
		/// Looks for another instance of Stoffi already running and sends arguments if such can
		/// be found.
		/// </summary>
		/// <param name="e">The startup arguments</param>
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			U.L(LogLevel.Information, "APP", "Started up");

			// create pipe server to listen for command line arguments
			U.L(LogLevel.Debug, "APP", "Starting to listen for arguments from second instances");
			ThreadPool.QueueUserWorkItem(new WaitCallback(ListenForArguments));
		}

		#endregion

		#region Imported

		[DllImport("user32.dll")]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

		[DllImport("user32.dll")]
		static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		#endregion
	}

	#region Imported shell
	/// <summary>
	/// 
	/// </summary>
	public class Shell
	{
		public enum NotifyInfoFlags { Error = 0x03, Info = 0x01, None = 0x00, Warning = 0x02 }
		public enum NotifyCommand { Add = 0x00, Delete = 0x02, Modify = 0x01 }
		public enum NotifyFlags { Message = 0x01, Icon = 0x02, Tip = 0x04, Info = 0x10, State = 0x08 }

		[ComVisible(false)]
		public enum ASSOCIATIONTYPE
		{
			AT_FILEEXTENSION,
			AT_URLPROTOCOL,
			AT_STARTMENUCLIENT,
			AT_MIMETYPE
		}

		[ComVisible(false)]
		public enum ASSOCIATIONLEVEL
		{
			AL_MACHINE,
			AL_EFFECTIVE,
			AL_USER
		}

		// CLSID_ApplicationAssociationRegistration
		public static readonly Guid CLSID_ApplicationAssociationRegistration = new Guid("591209c7-767b-42b2-9fba-44ee4615f2c7");

		// CLSID_ApplicationAssociationRegistrationUI
		public static readonly Guid CLSID_ApplicationAssociationRegistrationUI = new Guid("1968106d-f3b5-44cf-890e-116fcb9ecef1");

		#region Structures
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct NotifyIconData
		{
			public int cbSize; // DWORD
			public IntPtr hWnd; // HWND
			public int uID; // UINT
			public NotifyFlags uFlags; // UINT
			public int uCallbackMessage; // UINT
			public IntPtr hIcon; // HICON
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string szTip; // char[128]
			public int dwState; // DWORD
			public int dwStateMask; // DWORD
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string szInfo; // char[256]
			public int uTimeoutOrVersion; // UINT
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
			public string szInfoTitle; // char[64]
			public NotifyInfoFlags dwInfoFlags; // DWORD
		}
		#endregion

		#region Funtions
		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		public static extern int Shell_NotifyIcon(NotifyCommand cmd, ref NotifyIconData data);

		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		public static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);

		#endregion

		#region Defines
		public const int SHCNE_RENAMEITEM = 0x00000001;
		public const int SHCNE_CREATE = 0x00000002;
		public const int SHCNE_DELETE = 0x00000004;
		public const int SHCNE_MKDIR = 0x00000008;
		public const int SHCNE_RMDIR = 0x00000010;
		public const int SHCNE_MEDIAINSERTED = 0x00000020;
		public const int SHCNE_MEDIAREMOVED = 0x00000040;
		public const int SHCNE_DRIVEREMOVED = 0x00000080;
		public const int SHCNE_DRIVEADD = 0x00000100;
		public const int SHCNE_NETSHARE = 0x00000200;
		public const int SHCNE_NETUNSHARE = 0x00000400;
		public const int SHCNE_ATTRIBUTES = 0x00000800;
		public const int SHCNE_UPDATEDIR = 0x00001000;
		public const int SHCNE_UPDATEITEM = 0x00002000;
		public const int SHCNE_SERVERDISCONNECT = 0x00004000;
		public const int SHCNE_UPDATEIMAGE = 0x00008000;
		public const int SHCNE_DRIVEADDGUI = 0x00010000;
		public const int SHCNE_RENAMEFOLDER = 0x00020000;
		public const int SHCNE_FREESPACE = 0x00040000;
		public const int SHCNE_EXTENDED_EVENT = 0x04000000;

		public const int SHCNE_ASSOCCHANGED = 0x08000000;

		public const int SHCNE_DISKEVENTS = 0x0002381F;
		public const int SHCNE_GLOBALEVENTS = 0x0C0581E0;
		public const int SHCNE_ALLEVENTS = 0x7FFFFFFF;
		public const uint SHCNE_INTERRUPT = 0x80000000;

		// Flags
		public const int SHCNF_IDLIST = 0x0000;        // LPITEMIDLIST
		public const int SHCNF_PATHA = 0x0001;        // path name
		public const int SHCNF_PRINTERA = 0x0002;        // printer friendly name
		public const int SHCNF_DWORD = 0x0003;        // DWORD
		public const int SHCNF_PATHW = 0x0005;        // path name
		public const int SHCNF_PRINTERW = 0x0006;        // printer friendly name
		public const int SHCNF_TYPE = 0x00FF;
		public const int SHCNF_FLUSH = 0x1000;
		public const int SHCNF_FLUSHNOWAIT = 0x2000;

		public const int SHCNF_PATH = SHCNF_PATHW;
		public const int SHCNF_PRINTER = SHCNF_PRINTERW;

		#endregion

		#region Interfaces

		[ComVisible(true), ComImport,
		GuidAttribute("4e530b0a-e611-4c77-a3ac-9031d022281b"),
		InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IApplicationAssociationRegistration
		{
			[PreserveSig]
			int QueryCurrentDefault([In, MarshalAs(UnmanagedType.LPWStr)] string pszQuery,
															ASSOCIATIONTYPE atQueryType,
															ASSOCIATIONLEVEL alQueryLevel,
															[Out, MarshalAs(UnmanagedType.LPWStr)] out string ppszAssociation);

			[PreserveSig]
			int QueryAppIsDefault([In, MarshalAs(UnmanagedType.LPWStr)] string pszQuery,
													ASSOCIATIONTYPE atQueryType,
													ASSOCIATIONLEVEL alQueryLevel,
													[In, MarshalAs(UnmanagedType.LPWStr)] string pszAppRegistryName,
													out bool pfDefault);

			[PreserveSig]
			int QueryAppIsDefaultAll(ASSOCIATIONLEVEL alQueryLevel,
															[In, MarshalAs(UnmanagedType.LPWStr)] string pszAppRegistryName,
															out bool pfDefault);

			[PreserveSig]
			int SetAppAsDefault([In, MarshalAs(UnmanagedType.LPWStr)] string pszAppRegistryName,
													[In, MarshalAs(UnmanagedType.LPWStr)] string pszSet,
													ASSOCIATIONTYPE atSetType);

			[PreserveSig]
			int SetAppAsDefaultAll([In, MarshalAs(UnmanagedType.LPWStr)] string pszAppRegistryName);

			[PreserveSig]
			int ClearUserAssociations();
		}

		[ComVisible(true), ComImport,
		GuidAttribute("1f76a169-f994-40ac-8fc8-0959e8874710"),
		InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IApplicationAssociationRegistrationUI
		{
			int LaunchAdvancedAssociationUI([In, MarshalAs(UnmanagedType.LPWStr)] string pszAppRegistryName);
		}

		#endregion
	}
	#endregion
}