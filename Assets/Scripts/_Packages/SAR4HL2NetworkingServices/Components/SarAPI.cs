using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Project.Scripts.Components;
using Project.Scripts.Utils;
using Packages.SAR4HL2NetworkingSettings.Utils;

namespace Packages.SAR4HL2NetworkingSettings.Utils
{
    public static class SarAPI
    {
        public static string ApiURL = "127.0.0.1";
        public static int ApiPort = 5000;

        public static bool InProgress
        {
            get => inProgress;
        }
        private static bool inProgress = false;

        public static bool Completed
        {
            get => completed;
        }
        private static bool completed = true;

        public static bool Success
        {
            get => success;
        }
        private static bool success = true;

        public static string url
        {
            get
            {
                if (www != null)
                    return www.url;
                else
                    return "";
            }
        }
        private static UnityWebRequest www = null;

        public static string ResultFromServer
        {
            get
            {
                return result;
            }
        }
        private static string result = "";

        public static int HTTPCode
        {
            get => (int)resultCode;
        }
        private static long resultCode = 200;

        public static IEnumerator ServiceStatus(int timeout = -1)
        {
            yield return null;

            if(inProgress)
            {
                StaticLogger.Err("SarAPI", "can't handle more than one request per time; not allowed", logLayer: 1);
                yield break;
            }

            inProgress = true;
            completed = false;
            success = false;
            result = "";

            www = new UnityWebRequest();
            www.downloadHandler = new DownloadHandlerBuffer();

            www.method = UnityWebRequest.kHttpVerbGET;
            www.url = GetAPIUrl("/api"); ;
            if (timeout > 0)
                www.timeout = timeout;

            yield return www.SendWebRequest();
            resultCode = www.responseCode;
            
            switch (www.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                    StaticLogger.Err("SarAPI", "ConnectionError: " + www.error);
                    completed = true;
                    inProgress = false;
                    www = null;
                    yield break;
                case UnityWebRequest.Result.DataProcessingError:
                    StaticLogger.Err("SarAPI", "DataProcessingError: " + www.error);
                    completed = true;
                    inProgress = false;
                    www = null;
                    yield break;
                case UnityWebRequest.Result.ProtocolError:
                    if(resultCode != 418)
                    {
                        StaticLogger.Err("SarAPI", "ProtocolError: " + www.error);
                        completed = true;
                        inProgress = false;
                        www = null;
                        yield break;
                    }
                    else
                    {
                        StaticLogger.Info("SarAPI", "UnityWebRequest.Result.Success (I'm a Teapot)", logLayer: 2);
                    }
                    break;
                case UnityWebRequest.Result.Success:
                    StaticLogger.Info("SarAPI", "UnityWebRequest.Result.Success", logLayer: 2);
                    break;
            }

            result = www.downloadHandler.text;
            StaticLogger.Info("SarAPI", "from server:" + result, logLayer: 4);
            StaticLogger.Info("SarAPI", "server returned code:" + resultCode, logLayer: 4);
            success = true;
            completed = true;
            inProgress = false;
            www = null;
        }

        private static string GetAPIUrl(string apiPath = "/")
        {
            return $"http://{ApiURL}:{ApiPort}{apiPath}";
        }
    }
}
