using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Project.Scripts.Utils
{
    public class StaticAppSettings
    {
        // ===== PRIVATE ===== //

        // global settings
        private static Dictionary<string, string> GlobalSettings = new Dictionary<string, string>();
        // global object references
        private static Dictionary<string, object> GlobalObjects = new Dictionary<string, object>();



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
                return null;
            }

            string res = "";

            if (StaticAppSettings.GlobalSettings.TryGetValue(key, out res))
                StaticAppSettings.GlobalSettings[key] = jsonVal;
            else
                StaticAppSettings.GlobalSettings.Add(key, jsonVal);

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

            return val;
        }
    }
}
