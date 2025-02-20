using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityModManagerNet;

namespace TwitchChat
{

    /// <summary>
    /// Core module for the Twitch Chat mod.
    /// Handles mod initialization, lifecycle management, settings persistence, logging, and provides
    /// centralized coordination for all mod components by integrating with Unity Mod Manager.
    /// This module manages debug levels, component initialization, and file logging systems.
    /// </summary>
    public static class Main
    {
        public static bool _dispatcherModDetected;
        public static UnityModManager.ModEntry ModEntry { get; private set; } = null!;
        public static string settingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "TwitchChat", "Settings.xml");
        public static string debugLog = string.Empty;
        private static string messageLog = string.Empty;

        /// <summary>
        /// Initializes the mod and sets up required components and dependencies.
        /// </summary>
        /// <param name="modEntry">The mod entry point provided by Unity Mod Manager.</param>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            ModEntry.Logger.Log("[Load] Load method started.");

            try
            {
                // Initialize settings
                Settings.Instance = UnityModManager.ModSettings.Load<Settings>(modEntry);
                if (Settings.Instance == null)
                {
                    Settings.Instance = new Settings();
                    Settings.Instance.debugLevel = DebugLevel.Minimal; // Set default debug level
                }
                Settings.Instance.authentication_status = "Unverified or not set";

                // Begin using LogEntry
                string methodName = MethodBase.GetCurrentMethod().Name;
                LogEntry(methodName, "Load method started.");

                // Initialize UnityMainThreadDispatcher Object
                GameObject dispatcherObject = new("UnityMainThreadDispatcher");
                dispatcherObject.AddComponent<UnityMainThreadDispatcher>();
                UnityEngine.Object.DontDestroyOnLoad(dispatcherObject);
                ModEntry.Logger.Log("[Load] UnityMainThreadDispatcher initialized.");
                LogEntry(methodName, "UnityMainThreadDispatcher initialized successfully.");

                // Initialize TwitchChat Menu Object
                GameObject menuObject = new("TwitchChatMenu");
                LogEntry(methodName, "TwitchChatMenu object created.");

                // Check for RemoteDispatch Mod
                var remoteDispatchMod = UnityModManager.modEntries.FirstOrDefault(mod => mod.Info.Id == "RemoteDispatch");
                if (remoteDispatchMod != null)
                {
                    ModEntry.Logger.Log("[Load] RemoteDispatch Mod detected.");
                    LogEntry(methodName, "RemoteDispatch Mod detected and verified.");
                    _dispatcherModDetected = true;
                }
                else
                {
                    ModEntry.Logger.Log("[Load] RemoteDispatch Mod not detected.");
                    LogEntry(methodName, "RemoteDispatch Mod not found - mod will operate in standalone mode.");
                }

                // Register the mod's toggle and update methods
                ModEntry.OnToggle = OnToggle;
                ModEntry.OnUpdate = OnUpdate;
                LogEntry(methodName, "Registered mod toggle and update methods.");
                
                // Load the mod's settings
                Settings.Instance = UnityModManager.ModSettings.Load<Settings>(modEntry);
                Settings.Instance.authentication_status = "Unverified or not set";
                LogEntry(methodName, "Settings loaded and initialized.");
                
                // Register the mod's GUI methods
                modEntry.OnGUI = OnGUI;
                modEntry.OnSaveGUI = OnSaveGUI;
                LogEntry(methodName, "GUI methods registered.");

                // Initialize the log files
                InitializeLogFiles();
                LogEntry(methodName, "Log files initialized successfully.");

                // Activate Menu Managers
                MenuManager.Instance.gameObject.SetActive(true);
                LogEntry(methodName, "Menu Manager activated.");

                ModEntry.Logger.Log("[Load] Load method completed successfully.");
                LogEntry(methodName, "Load method completed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                ModEntry.Logger.Log($"[Load] Critical error during mod initialization: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Handles the mod's GUI rendering in the Unity Mod Manager interface.
        /// </summary>
        /// <param name="modEntry">The mod entry point provided by Unity Mod Manager.</param>
        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            _ = MethodBase.GetCurrentMethod().Name;
            Settings.Instance.DrawButtons();

        }

        /// <summary>
        /// Saves the mod's GUI settings when changes are made.
        /// </summary>
        /// <param name="modEntry">The mod entry point provided by Unity Mod Manager.</param>
        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            _ = MethodBase.GetCurrentMethod().Name;
            Settings.Instance.Save(modEntry);
        }

        /// <summary>
        /// Handles the enabling and disabling of the mod.
        /// </summary>
        /// <param name="_">Unused mod entry parameter.</param>
        /// <param name="isEnabled">Boolean indicating whether the mod should be enabled or disabled.</param>
        /// <returns>True if the toggle operation was successful, false otherwise.</returns>
        private static bool OnToggle(UnityModManager.ModEntry _, bool isEnabled)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            if (ModEntry.Enabled)
            {
                NotificationManager.AttachNotification("The TwitchChat mod is now receiving message notifications from your channel.", "null");
                LogEntry(methodName, "Mod Enabled!");
                ModEntry.Enabled = true;
            }
            else
            {
                NotificationManager.AttachNotification("The TwitchChat mod is no longer receiving message notifications from your Channel.", "null");
                LogEntry(methodName, "Mod Disabled!");
                ModEntry.Enabled = false;
            }

            return true;
        }

        /// <summary>
        /// Updates the mod's state each frame when enabled.
        /// </summary>
        /// <param name="modEntry">The mod entry point provided by Unity Mod Manager.</param>
        /// <param name="delta">Time in seconds since the last update.</param>
        private static void OnUpdate(UnityModManager.ModEntry modEntry, float delta)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                if (ModEntry.Enabled)
                {
                    Settings.Instance.Update();
                }
            }
            catch (Exception e)
            {
                LogEntry(methodName, $"Exception: {e}");
            }
        }

        /// <summary>
        /// Initializes and manages the mod's log files, maintaining only the last 3 debug log files.
        /// Creates necessary directories and log files with appropriate headers.
        /// </summary>
        private static void InitializeLogFiles()
        {
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "TwitchChat", "Logs");
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        
            // Create the directory if it does not exist
            if (!Directory.Exists(logDirectory))
            {
                try
                {
                    Directory.CreateDirectory(logDirectory);
                    ModEntry.Logger.Log($"Created directory: {logDirectory}");
                }
                catch (Exception ex)
                {
                    ModEntry.Logger.Log($"Failed to create directory: {logDirectory}. Exception: {ex.Message}");
                    return;
                }
            }
        
            // Handle Debug Logs - Keep only last 3
            var debugLogFiles = Directory.GetFiles(logDirectory, "Debug_*.txt")
                                       .OrderByDescending(f => File.GetCreationTime(f))
                                       .ToList();
        
            // Remove excess debug log files (keep only 3 most recent)
            for (int i = 2; i < debugLogFiles.Count; i++)
            {
                try
                {
                    File.Delete(debugLogFiles[i]);
                }
                catch (Exception ex)
                {
                    ModEntry.Logger.Log($"Failed to delete old debug log: {debugLogFiles[i]}. Exception: {ex.Message}");
                }
            }
        
            // Create new log files with timestamp
            debugLog = Path.Combine(logDirectory, $"Debug_{timestamp}.txt");
            messageLog = Path.Combine(logDirectory, $"Messages_{timestamp}.txt");
        
            // Create new files with headers
            try
            {
                File.WriteAllText(debugLog, $"Debug log initialized on {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
                File.WriteAllText(messageLog, $"Message log initialized on {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
                
                ModEntry.Logger.Log($"Initialized new debug log: {debugLog}");
                ModEntry.Logger.Log($"Initialized new message log: {messageLog}");
            }
            catch (Exception ex)
            {
                ModEntry.Logger.Log($"Failed to initialize log files. Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Writes a log entry to either the debug or message log file based on the source.
        /// Implements retry logic for handling file access conflicts.
        /// </summary>
        /// <param name="source">The source (method) of the log entry, also determines which log file to use.</param>
        /// <param name="message">The message to be logged.</param>
        public static void LogEntry(string source, string message)  // TODO: Refactor for better level use and general cleanup across entire mod
        {
            // Ensure Settings.Instance is initialized
            if (Settings.Instance == null)
            {
                ModEntry?.Logger.Log($"[{source}] Cannot log: Settings.Instance is null");
                return;
            }

            string selected_log = (source == "ReceivedMessage" || source == "SentMessage") ? messageLog : debugLog;

            // Early return if this is a debug message and debug logging is off
            if (selected_log == debugLog)
            {
                if (Settings.Instance.debugLevel == DebugLevel.Off)
                    return;
                if (Settings.Instance.debugLevel == DebugLevel.Minimal && 
                    !MinimalDebug.Contains(source))
                    return;
                if (Settings.Instance.debugLevel == DebugLevel.Reduced &&
                    ReducedDebug.Contains(source))
                    return;
            }

            if (string.IsNullOrEmpty(selected_log))
            {
                ModEntry.Logger.Log($"[{source}] Log file path is not initialized.");
                return;
            }

            int retryCount = 3;
            int delay = 1000; // 1 second

            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    using StreamWriter writer = new(selected_log, true);
                    string logMessage = selected_log == debugLog ? $"[{source}] {message}" : message;
                    writer.WriteLine($"{DateTime.Now:HH:mm}: {logMessage}");
                    return;
                }
                catch (IOException ex) when (i < retryCount - 1)
                {
                    ModEntry.Logger.Log($"[{source}] Failed to write to log file: {selected_log}. Exception: {ex.Message}. Retrying in {delay}ms...");
                    Thread.Sleep(delay);
                }
                catch (Exception ex)
                {
                    ModEntry.Logger.Log($"[{source}] Failed to write to log file: {selected_log}. Exception: {ex.Message}");
                    return;
                }
            }
        }

        /// <summary>
        /// Lists of debug sources used to control logging verbosity.
        /// MinimalDebug contains sources that should be logged at Minimal level.
        /// ReducedDebug contains sources that should be excluded at Reduced level.
        /// These lists help optimize logging performance and output clarity.
        /// </summary>
        private static readonly HashSet<string> MinimalDebug =
        [
            "Load",
            "OnToggle",
            "OnUpdate",
            "RegisterWebbSocketChatEvent"
        ];

        /// <summary>
        /// List of source/method names that should be excluded from logging when debug level is set to Reduced.
        /// </summary>
        private static readonly HashSet<string> ReducedDebug =
        [
            "ReceiveMessages",
            "AttachNotification"
        ];
    }
}