using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace SaR4Hololens2.Scripts.Components
{
    public class StaticAppSettings
    {
        private static Dictionary<string, string> Settings = new Dictionary<string, string>();

        public static string SetOpt(string key, string val, string defaultVal = null)
        {
            if (key == "") return null;
            if (val == "" && defaultVal != null) val = defaultVal;

            string res = "";
            if (StaticAppSettings.Settings.TryGetValue(key, out res))
                StaticAppSettings.Settings[key] = val;
            else
                StaticAppSettings.Settings.Add(key, val);

            return val;
        }

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

            if (StaticAppSettings.Settings.TryGetValue(key, out res))
                StaticAppSettings.Settings[key] = jsonVal;
            else
                StaticAppSettings.Settings.Add(key, jsonVal);

            return jsonVal;
        }

        public static string GetOpt(string key, string defaultVal = null)
        {
            if (key == "") return null;

            string res = "";
            if (StaticAppSettings.Settings.TryGetValue(key, out res))
                return res;
            else
                return defaultVal;
        }

        public static T GetOptJson<T>(string key, T defaultVal = default(T))
        {
            if (key == "") return default(T);

            string res = "";
            if (StaticAppSettings.Settings.TryGetValue(key, out res))
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
    }
}
