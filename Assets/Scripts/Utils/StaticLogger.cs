using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Project.Scripts.Utils
{
    public static class StaticLogger
    {
        // ===== LOG LAYERS SUPPORT ===== //

        // every log with code smaller or equal than this is logged
        public static int CurrentLogLayer = int.MaxValue;
        // implicitly suppressed logs
        public static HashSet<string> SuppressedLogs = new HashSet<string>();



        // ===== LOG OPTIONS ===== //

        public static bool PrintInfo = true;
        public static bool PrintWarn = true;
        public static bool PrintErr = true;



        // ===== LOG INFO ===== //

        public static void Info(GameObject refObj, string text, bool pause = false, int logLayer = 0, bool SuppressLog = true)
        {
            if (logLayer > StaticLogger.CurrentLogLayer || !PrintInfo) return;

            if((SuppressLog && !SuppressedLogs.Contains(text)) || !SuppressLog)
                Debug.Log(WrapMessage(refObj, text));

            if (pause) BreakPoint();
        }
        public static void Info(MonoBehaviour refObj, string text, bool pause = false, int logLayer = 0, bool SuppressLog = true)
        {
            if (logLayer > StaticLogger.CurrentLogLayer || !PrintInfo) return;

            if ((SuppressLog && !SuppressedLogs.Contains(text)) || !SuppressLog)
                Debug.Log(WrapMessage(refObj, text));

            if (pause) BreakPoint();
        }
        public static void Info(string sourcext, string text, bool pause = false, int logLayer = 0, bool SuppressLog = true)
        {
            if (logLayer > StaticLogger.CurrentLogLayer || !PrintInfo) return;

            if ((SuppressLog && !SuppressedLogs.Contains(text)) || !SuppressLog)
                Debug.Log(WrapMessage(sourcext, text));

            if (pause) BreakPoint();
        }



        // ===== LOG WARNING ===== //

        public static void Warn(GameObject refObj, string text, bool pause = false, int logLayer = 0, bool SuppressLog = true)
        {
            if (logLayer > StaticLogger.CurrentLogLayer || !PrintWarn) return;

            if ((SuppressLog && !SuppressedLogs.Contains(text)) || !SuppressLog)
                Debug.LogWarning(WrapMessage(refObj, text));

            if (pause) BreakPoint();
        }
        public static void Warn(MonoBehaviour refObj, string text, bool pause = false, int logLayer = 0, bool SuppressLog = true)
        {
            if (logLayer > StaticLogger.CurrentLogLayer || !PrintWarn) return;

            if ((SuppressLog && !SuppressedLogs.Contains(text)) || !SuppressLog)
                Debug.LogWarning(WrapMessage(refObj, text));

            if (pause) BreakPoint();
        }
        public static void Warn(string sourcext, string text, bool pause = false, int logLayer = 0, bool SuppressLog = true)
        {
            if (logLayer > StaticLogger.CurrentLogLayer || !PrintWarn) return;

            if ((SuppressLog && !SuppressedLogs.Contains(text)) || !SuppressLog)
                Debug.LogWarning(WrapMessage(sourcext, text));

            if (pause) BreakPoint();
        }



        // ===== LOG ERROR ===== //

        public static void Err(GameObject refObj, string text, bool pause = false, int logLayer = 0, bool SuppressLog = true)
        {
            if (logLayer > StaticLogger.CurrentLogLayer || !PrintErr) return;

            if ((SuppressLog && !SuppressedLogs.Contains(text)) || !SuppressLog)
                Debug.LogError(WrapMessage(refObj, text));

            if (pause) BreakPoint();
        }
        public static void Err(MonoBehaviour refObj, string text, bool pause = false, int logLayer = 0, bool SuppressLog = true)
        {
            if (logLayer > StaticLogger.CurrentLogLayer || !PrintErr) return;

            if ((SuppressLog && !SuppressedLogs.Contains(text)) || !SuppressLog)
                Debug.LogError(WrapMessage(refObj, text));

            if (pause) BreakPoint();
        }
        public static void Err(string sourcext, string text, bool pause = false, int logLayer = 0, bool SuppressLog = true)
        {
            if (logLayer > StaticLogger.CurrentLogLayer || !PrintErr) return;

            if ((SuppressLog && !SuppressedLogs.Contains(text)) || !SuppressLog)
                Debug.LogError(WrapMessage(sourcext, text));

            if (pause) BreakPoint();
        }



        // ===== BREAKPOINT SUPPORT ===== //

        public static void BreakPoint(string msg = "", int logLayer = 0)
        {
            if (logLayer > StaticLogger.CurrentLogLayer) return;

            if (msg != "")
                Debug.Log($"BREAKPOINT : {msg}");

            Debug.Break();
        }



        // ===== UTILITIES ===== //

        public static string WrapMessage(GameObject refObject, string text)
        {
            return $"[FROM GameObject:{refObject.name}] {text}";
        }

        public static string WrapMessage(MonoBehaviour refComponent, string text)
        {
            return $"[FROM Component:{refComponent.name}] {text}";
        }

        public static string WrapMessage(string sourcext, string text)
        {
            return $"[FROM Component:{(sourcext != "" ? sourcext : "unknown!!!")}] {text}";
        }
    }
}
