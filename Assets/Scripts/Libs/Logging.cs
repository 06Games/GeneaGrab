using System.IO;
using UnityEngine;

public static class Logging
{
    public static System.Action<LogType, string> NewMessage { get; set; }

    static FileInfo logFile;
    static string logs = "";
    public static void Log(System.Exception e, LogType type = LogType.Exception) { Log(e.Message, type, e.StackTrace); }
    public static void Log(string logString, LogType type = LogType.Log, string stackTrace = null)
    {
        if (!IsInitialised) Debug.LogError("Not Initialised");
        UnityThread.executeInUpdate(() => NewMessage?.Invoke(type, logString));
        if (logs != "") logs += "\n\n";
        logs += "[" + System.DateTime.Now.ToString("HH:mm:ss") + "] " + type + ": " + (logString + (stackTrace != null ? "\nStack Trace:\n" : "") + stackTrace).TrimEnd('\n', '\t', '\r').Replace("\n", "\n\t\t\t\t");

        UnityThread.executeInUpdate(() => File.WriteAllText(logFile.FullName, logs));
    }

    public static bool IsInitialised { get { return logFile != null; } }
    public static void Initialise()
    {
        if (IsInitialised) return;
        Application.logMessageReceived += (logString, stackTrace, type) => Log(logString, type, stackTrace);

        string DT = (System.DateTime.Now - System.TimeSpan.FromSeconds(Time.realtimeSinceStartup)).ToString("yyyy-MM-dd HH-mm-ss");
        string path = Application.persistentDataPath + "/logs/";
        logFile = new FileInfo(path + DT + ".log");
        if (!logFile.Directory.Exists) Directory.CreateDirectory(logFile.DirectoryName);
        Log("The app started", LogType.Log);
    }

    public static void DeleteLogs()
    {
        string log = File.ReadAllText(logFile.FullName);
        Directory.Delete(Application.persistentDataPath + "/logs/", true);
        Directory.CreateDirectory(Application.persistentDataPath + "/logs/");
        File.WriteAllText(logFile.FullName, log);
    }
}

