using System;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityModManagerNet;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TwitchChat
{
    public static class Main
    {
        private static bool _isEnabled;
        public static bool _dispatcherModDetected;
        public static UnityModManager.ModEntry ModEntry { get; private set; } = null!;
        public static string settingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "TwitchChatMod", "Settings.xml");
        private static string debugLog = string.Empty;
        private static string messageLog = string.Empty;
        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            ModEntry = modEntry;

            ModEntry.Logger.Log("[Load] Load method started.");

            // Initialize UnityMainThreadDispatcher
            GameObject dispatcherObject = new("UnityMainThreadDispatcher");
            dispatcherObject.AddComponent<UnityMainThreadDispatcher>();
            UnityEngine.Object.DontDestroyOnLoad(dispatcherObject);
            ModEntry.Logger.Log("[Load] UnityMainThreadDispatcher initialized.");

            // Check for RemoteDispatch Mod
            var remoteDispatchMod = UnityModManager.modEntries.FirstOrDefault(mod => mod.Info.Id == "RemoteDispatch");
            if (remoteDispatchMod != null)
            {
                ModEntry.Logger.Log("[Load] RemoteDispatch Mod detected.");
                _dispatcherModDetected = true;
            }
            else
            {
                ModEntry.Logger.Log("[Load] RemoteDispatch Mod not detected.");
            }

            // Register the mod's toggle and update methods
            ModEntry.OnToggle = OnToggle;
            ModEntry.OnUpdate = OnUpdate;
            
            // Load the mod's settings
            Settings.Instance = UnityModManager.ModSettings.Load<Settings>(modEntry);
            
            // Register the mod's GUI methods
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            // Initialize the log files
            InitializeLogFiles();

            ModEntry.Logger.Log("[Load] Load method completed successfully.");
            LogEntry(methodName, "Load method completed successfullyy.");
            return true;
        }
        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            _ = MethodBase.GetCurrentMethod().Name;
            Settings.Instance.DrawButtons();

        }
        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            _ = MethodBase.GetCurrentMethod().Name;
            Settings.Instance.Save(modEntry);
        }
        private static bool OnToggle(UnityModManager.ModEntry _, bool isEnabled)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            if (isEnabled)
            {
                _isEnabled = true;
                MessageHandler.AttachNotification("TwitchChatMod is now receiving message notifications from your channel.", "null");
                LogEntry(methodName, "Mod Enabled!");
                ModEntry.Enabled = true;
            }
            else
            {
                _isEnabled = false;
                MessageHandler.AttachNotification("TwitchChatMod is no longer receiving message notifications from your Channel.", "null");
                LogEntry(methodName, "Mod Disabled!");
                ModEntry.Enabled = false;
            }

            return true;
        }
        private static void OnUpdate(UnityModManager.ModEntry mod, float delta)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;
            try
            {
                if (_isEnabled)
                {
                    Settings.Instance.Update();
                }
            }
            catch (Exception e)
            {
                LogEntry(methodName, $"Exception: {e}");
            }
        }
        private static void InitializeLogFiles()
        {
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "TwitchChatMod", "Logs");
            string date = DateTime.Now.ToString("yyyy-MM-dd");
        
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
        
            // Get all debug log files and sort by creation time
            var debugLogFiles = Directory.GetFiles(logDirectory, "Log - *.txt")
                                         .OrderByDescending(f => File.GetCreationTime(f))
                                         .ToList();
        
            // Keep only the last 3 log files
            for (int i = 3; i < debugLogFiles.Count; i++)
            {
                File.Delete(debugLogFiles[i]);
            }
        
            debugLog = Path.Combine(logDirectory, $"Log - {date}.txt");
            messageLog = Path.Combine(logDirectory, $"Messages - {date}.txt");
        
            ModEntry.Logger.Log($"Initialized log file: {debugLog}");
            ModEntry.Logger.Log($"Initialized message file: {messageLog}");
        
            // Open each of the logs and write a new header (not using logEntry because it will write to the log file)
            using StreamWriter logHeader = new(debugLog, true);
            logHeader.WriteLine($"Log file initialized on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            using StreamWriter messageHeader = new(messageLog, true);
            messageHeader.WriteLine($"Message file initialized on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        
            ModEntry.Logger.Log("Log files initialized.");
        }
        public static void LogEntry(string source, string message)
        {
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
        private static readonly HashSet<string> MinimalDebug =
        [
            "Load",
            "OnToggle",
            "OnUpdate",
            "RegisterWebbSocketChatEvent"
        ];
        private static readonly HashSet<string> ReducedDebug =
        [
            "ReceiveMessages",
            "AttachNotification"
        ];
    }
}