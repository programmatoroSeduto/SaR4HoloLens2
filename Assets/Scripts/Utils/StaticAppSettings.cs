using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Project.Scripts.Components;

namespace Project.Scripts.Utils
{
    public class StaticAppSettings
    {
        // ===== PUBLIC ===== //

#if WINDOWS_UWP
        public static bool IsEnvUWP = true;
#else
        public static bool IsEnvUWP = false;
#endif



        // ===== PRIVATE ===== //

        // global settings
        private static Dictionary<string, string> GlobalSettings = new Dictionary<string, string>();
        // global object references
        private static Dictionary<string, object> GlobalObjects = new Dictionary<string, object>();
        // name of this class (for logging purposes)
        private static readonly string logSource = "Static App Settings";



        // ===== GET AD SET GLOBAL SETTINGS ===== //

        public static string SetOpt(string key, string val, string defaultVal = null)
        {
            if (key == "") return null;
            if (val == "" && defaultVal != null) val = defaultVal;

            string res = "";
            if (StaticAppSettings.GlobalSettings.TryGetValue(key, out res))
                StaticAppSettings.GlobalSettings[key] = val;
            else
                StaticAppSettings.GlobalSettings.Add(key, val);

            StaticLogger.Info(logSource, $"SUCCESS SET PARAMETER key({key}) val({val})");

            return val;
        }

        public static string GetOpt(string key, string defaultVal = null)
        {
            if (key == "") return null;

            string res = "";
            if (StaticAppSettings.GlobalSettings.TryGetValue(key, out res))
                return res;
            else
                return defaultVal;
        }



        // ===== GET AD SET GLOBAL SETTINGS IN JSON FORMAT ===== //

        public static string SetOptJson<T>(string key, T val)
        {
            if (val == null) return null;
            if (key == "") return null;

            string jsonVal = "";
            try { jsonVal = JsonUtility.ToJson(val); }
            catch (System.Exception)
            {
                StaticLogger.Warn(logSource, $"FAILED SET JSON PARAMETER key({key})");
                return null;
            }

            string res = "";

            if (StaticAppSettings.GlobalSettings.TryGetValue(key, out res))
                StaticAppSettings.GlobalSettings[key] = jsonVal;
            else
                StaticAppSettings.GlobalSettings.Add(key, jsonVal);

            StaticLogger.Info(logSource, $"SUCCESS SET JSON PARAMETER key({key})");
            StaticLogger.Info(logSource, $"JSON PARAMETER key({key}) with code: \n{jsonVal}", logLayer: 1);

            return jsonVal;
        }

        public static T GetOptJson<T>(string key, T defaultVal = default(T))
        {
            if (key == "") return default(T);

            string res = "";
            if (StaticAppSettings.GlobalSettings.TryGetValue(key, out res))
                try
                {
                    return JsonUtility.FromJson<T>(res);
                }
                catch(System.Exception)
                {
                    return defaultVal;
                }
            else
                return defaultVal;
        }



        // ===== GET AD SET GLOBAL REFERENCES ===== //

        public static object GetObject(string key, object defaultVal = null)
        {
            if (key == "") return defaultVal;

            object res = defaultVal;
            if (StaticAppSettings.GlobalObjects.TryGetValue(key, out res))
                return res;
            else
                return defaultVal;
        }

        public static object SetObject(string key, object val, object defaultVal = null)
        {
            if (key == "") return null;
            if (val == null && defaultVal != null) val = defaultVal;

            object res = defaultVal;
            if (StaticAppSettings.GlobalObjects.TryGetValue(key, out res))
                StaticAppSettings.GlobalObjects[key] = val;
            else
                StaticAppSettings.GlobalObjects.Add(key, val);

            StaticLogger.Info(logSource, $"SUCCESS SET OBJECT REFERENCE PARAMETER key({key})", logLayer: 1);

            return val;
        }
    }
}
