using System;
using System.Net.Http;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Project.Scripts.Components;
using Project.Scripts.Utils;
using Packages.SAR4HL2NetworkingServices.Components;

namespace Packages.SAR4HL2NetworkingServices.Utils
{
    /// <summary>
    /// This static class allows to communicate with the server. 
    /// It is meant to be unique for each HoloLens2 client
    /// </summary>
    public static class SarAPI
    {
        // ===== PUBLIC ===== //

        /// <summary>
        /// The URL of the server
        /// </summary>
        public static string ApiURL = "127.0.0.1";

        /// <summary>
        /// Variable used only for support (voice command or instance)
        /// </summary>
        public static SarHL2Client Client = null;

        /// <summary>
        /// Use this to check if a request is already in action
        /// </summary>
        public static bool InProgress
        {
            get => inProgress;
        }
        private static bool inProgress = false;

        /// <summary>
        /// Use this to check if the request has been completed.
        /// NOTE WELL: check this before checking the 'success' variable
        /// </summary>
        public static bool Completed
        {
            get => completed;
        }
        private static bool completed = true;

        /// <summary>
        /// Use this to check if the request completed successfully. 
        /// </summary>
        public static bool Success
        {
            get => success;
        }
        private static bool success = true;

        /// <summary>
        /// the URL used by the API object to contact the server in the last request
        /// </summary>
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

        /// <summary>
        /// This field contains the data from the server obtained in the last request. 
        /// </summary>
        public static string ResultFromServer
        {
            get
            {
                return result;
            }
        }
        private static string result = "";

        /// <summary>
        /// This field contains the result code from the server obtained in the last request. 
        /// </summary>

        public static int HTTPCode
        {
            get => resultCode;
        }
        private static int resultCode = 200;

        /// <summary>
        /// Flag used to check if a InternalServerError occurred for a request.
        /// </summary>
        public static bool InternalServerError
        {
            get => internalServerError;
        }



        // ===== PUBLIC : API ADDRESSES ===== //

        /// <summary>
        /// path of the service status call. If the service is online, it returns 418. 
        /// </summary>
        public static readonly string ApiAddress_ServerStatus = "/api";

        /// <summary>
        /// User Login API address. 
        /// </summary>
        public static readonly string ApiAddress_UserLogin = "/api/user/login";

        /// <summary>
        /// User logout API address. 
        /// </summary>
        public static readonly string ApiAddress_UserLogout = "/api/user/logout";

        /// <summary>
        /// device Login API address. 
        /// </summary>
        public static readonly string ApiAddress_DeviceLogin = "/api/device/login";

        /// <summary>
        /// device logout API address. 
        /// </summary>
        public static readonly string ApiAddress_DeviceLogout = "/api/device/logout";

        /// <summary>
        /// HoloLens2 integration Download API address.
        /// </summary>
        public static readonly string ApiAddress_Hl2Download = "/api/hl2/download";

        /// <summary>
        /// HoloLens2 integration Upload API address.
        /// </summary>
        public static readonly string ApiAddress_Hl2Upload = "/api/hl2/upload";



        // ===== PRIVATE ===== //

        // a special check to detect internal server error
        private static bool internalServerError = false;
        // service status
        private static bool serviceStatusCheck = false;
        // response from server for call service status
        private static api_base_response serviceStatusResponsePack = null;
        // user logged in?
        private static bool userLoggedIn = false;
        // user id
        private static string userID = "";
        // ...
        private static string deviceID = "";
        // user session token
        private static string userSessionToken = "";
        // reference position 
        private static string referencePosId = "";
        // device logged in?
        private static bool deviceLoggedIn = false;
        // user login response
        private static api_user_login_response userLoginResponsePack = null;
        // device login response pack
        private static api_device_login_response deviceLoginResponsePack = null;
        // ...
        private static api_hl2_download_response hl2DownloadResponsePack = null;
        // ...
        private static string hl2DownloadResponsePackJson = "";
        // ...
        private static string fakeToken = "";
        // ...
        private static api_hl2_upload_response hl2UploadResponsePack = null;
        // ...
        private static Dictionary<int, int> uploadAlignmentLookup = new Dictionary<int, int>();
        // ...
        private static int maxIdx = 0;
        // ...
        private static bool downloadSuccess = true;
        // ...
        private static bool uploadSuccess = true;



        // ===== API CALLS : SERVICE STATUS ===== //

        /// <summary>
        /// It returns the status of the service. It is false by default if ApiCall_ServiceStatus() is not called. 
        /// </summary>
        public static bool ServiceStatus
        {
            get => serviceStatusCheck;
        }

        /// <summary>
        /// It returns the class representation of the response from server for the ServiceStatus API call.
        /// </summary>
        public static api_base_response ServiceStatusResponse
        {
            get => serviceStatusResponsePack;
        }

        /// <summary>
        /// Get the status of the service. Check the call ServiceStatus after called this coroutine. 
        /// </summary>
        /// <param name="timeout">the number of seconds to wait before considering the request failed. (default: -1, unused)</param>
        /// <returns> coroutine </returns>
        /// <example>
        /// <code>
        /// StartCoroutine(SarAPI.ApiCall_ServiceStatus(timeout: 10));
        /// while(SarAPI.InProgress)
        /// {
        ///     // ... wait ...
        /// }
        /// if(!SarAPI.ServiceStatus)
        /// {
        ///     // ... Ops! Something bad happened during the service call ...
        /// }
        /// </code>
        /// </example>
        public static IEnumerator ApiCall_ServiceStatus(int timeout = -1)
        {
            string sourceLog = "SarAPI:ApiCall_ServiceStatus";
            yield return null;
            if(inProgress)
            {
                StaticLogger.Err(sourceLog, "can't handle more than one request per time; not allowed", logLayer: 1);
                yield break;
            }

            StaticLogger.Info(sourceLog, "STARTING REQUEST", logLayer: 2);
            serviceStatusResponsePack = null;
            inProgress = true;
            completed = false;

            string requestURL = GetAPIUrl(
                ApiAddress_ServerStatus
            );
            yield return BSCOR_PerformRequestGet(
                requestURL, 
                timeout
            );

            serviceStatusResponsePack = JsonUtility.FromJson<api_base_response>(result);
            serviceStatusCheck = (resultCode == 418);
            if (resultCode == 418)
                resultCode = 200;

            inProgress = false;
            completed = true;
            StaticLogger.Info(sourceLog, "CLOSING REQUEST", logLayer: 2);
        }



        // ===== API CALLS : USER LOGIN LOGOUT ===== //

        /// <summary>
        /// Either the user login succeeded or not. Obtaining true for this GET is required for the next steps. 
        /// </summary>
        public static bool UserLoggedIn
        {
            get => userLoggedIn;
        }

        /// <summary>
        /// user login call. 
        /// </summary>
        /// <param name="userID">the user ID code of the user wearing hololens2 now</param>
        /// <param name="userAccessKey">the access key of the user wearing hololens2 now</param>
        /// <param name="timeout">the number of seconds to wait before considering the request failed. (default: -1, unused)</param>
        /// <returns>coroutine</returns>
        /// <remarks>The API call will fail if the server is not online!</remarks>
        /// <example>
        /// <code>
        /// 
        /// // check the service status first!
        /// yield StartCoroutine(SarAPI.ApiCall_ServiceStatus(timeout: 10));
        /// if(!SarAPI.ServiceStatus)
        ///     yield break; // server is not online
        /// 
        /// // user login request
        /// string userID = "SARHL2_ID..._USER"; // from settings
        /// string userAccessKey = "..."; // from settings
        /// yield StartCoroutine(SarAPI.ApiCall_UserLogin(userID, userAccessKey, timeout:10));
        /// if(!SarAPI.UserLoggiedIn)
        ///     yield break; // authentication failed, check UserLoginResponse pack from server
        /// 
        /// // ... the next step is to handle the device login request ...
        /// 
        /// </code>
        /// </example>
        public static IEnumerator ApiCall_UserLogin(string userID, string userApproverID, string userAccessKey, int timeout = -1, bool forceReLogin = false)
        {
            string sourceLog = "SarAPI:ApiCall_UserLogin";
            yield return null;
            if(!forceReLogin && userLoggedIn && userSessionToken != "")
            {
                StaticLogger.Info(sourceLog,
                    "Trying to login again; user already logged in", logLayer: 2);
                yield break;
            }
            else if(forceReLogin)
            {
                StaticLogger.Info(sourceLog,
                    "forceReLogin option: repeating login", logLayer: 2);
            }
            if (inProgress)
            {
                StaticLogger.Err(sourceLog, 
                    "can't handle more than one request per time; not allowed", logLayer: 1);
                yield break;
            }
            else if (!SarAPI.ServiceStatus)
            {
                StaticLogger.Err(sourceLog,
                    "rying to call the server, but the server seems not online", logLayer: 0);
                StaticLogger.Info(sourceLog,
                    "Did you check the service status before calling the API?", logLayer: 1);
                yield break;
            }

            StaticLogger.Info(sourceLog, "STARTING REQUEST", logLayer: 2);
            SarAPI.userLoginResponsePack = null;
            SarAPI.userSessionToken = "";
            inProgress = true;
            completed = false;

            string requestURL = GetAPIUrl(
                ApiAddress_UserLogin
            );
            api_user_login_request payload = new api_user_login_request();
            payload.user_id = userID;
            payload.approver_id = userApproverID;
            payload.access_key = userAccessKey;

            yield return BSCOR_PerformRequestPost(
                requestURL,
                JsonUtility.ToJson(payload),
                timeout
            );

            userLoginResponsePack = JsonUtility.FromJson<api_user_login_response>(result);
            

            if(resultCode == 200)
            {
                userLoggedIn = true;
                SarAPI.userID = payload.user_id;
                SarAPI.userSessionToken = userLoginResponsePack.session_token;
                StaticLogger.Info(sourceLog, $"OK User Logged in", logLayer: 0);
            }

            inProgress = false;
            completed = true;
            StaticLogger.Info(sourceLog, "CLOSING REQUEST", logLayer: 2);
        }

        /// <summary>
        /// "Logout" is not a coroutine as the other methods, since to be sure that the device session
        /// is correctly closed, the request can't be handled on many frames. This method can be used also
        /// in the OnDestroy methods: that's the main reason why his function is not a coroutine but a oneshot
        /// call. 
        /// </summary>
        /// <remarks>This call also releases the device, since it is associated to the user.</remarks>
        /// <returns> request success or not </returns>
        public static bool ApiCall_UserLogout()
        {
            string sourceLog = "SarAPI:ApiCall_UserLogout";
            if(!userLoggedIn || userID == "" || userSessionToken == "")
            {
                StaticLogger.Warn(sourceLog,
                    "Unable to log out: user not logged in", logLayer: 1);
                return false;
            }

            StaticLogger.Info(sourceLog, "STARTING REQUEST", logLayer: 2);
            inProgress = true;
            completed = false;
            success = false;

            string logoutUrl = GetAPIUrl(SarAPI.ApiAddress_UserLogout);
            api_user_logout_request classPayload = new api_user_logout_request();
            classPayload.user_id = SarAPI.userID;
            classPayload.session_token = SarAPI.userSessionToken;
            string jsonPayload = JsonUtility.ToJson(classPayload);

            string textResponse = "";
            int responseCode = 200;

            // ===== UNITY ONLY??? ===== //
            HttpClient cl = new HttpClient();
            StringContent httpPayload = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage res = cl.PostAsync(logoutUrl, httpPayload).GetAwaiter().GetResult();
            textResponse = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            responseCode = (int) res.StatusCode;
            // ===== UNITY ONLY??? ===== //

            result = textResponse;
            if (!handleRequestResult(responseCode, "POST", url, sourceLog))
            {
                StaticLogger.Info(sourceLog, $"server returned pack: \n\t{textResponse} \n\twith code {responseCode}");
                StaticLogger.Err(sourceLog, "Cannot log out");
                return false;
            }

            userID = "";
            userSessionToken = "";
            fakeToken = "";
            userLoggedIn = false;

            inProgress = false;
            completed = true;
            success = true;
            StaticLogger.Info(sourceLog, "CLOSING REQUEST - logout success", logLayer: 2);
            return true;
        }



        // ===== API CALLS : DEVICE LOGIN ===== //

        public static bool DeviceLoggedIn
        {
            get => deviceLoggedIn;
        }

        public static IEnumerator ApiCall_DeviceLogin(string deviceID = "", int timeout = -1)
        {
            string sourceLog = "SarAPI:ApiCall_DeviceLogin";
            yield return null;

            if(deviceLoggedIn)
            {
                StaticLogger.Info(sourceLog,
                    "already logged in", logLayer: 2);
            }

            if (inProgress)
            {
                StaticLogger.Err(sourceLog,
                    "can't handle more than one request per time; not allowed", logLayer: 1);
                yield break;
            }
            else if (!userLoggedIn || userSessionToken == "")
            {
                StaticLogger.Err(sourceLog,
                    "Cannot register the device: missing login");
                yield break;
            }
            else if (!SarAPI.ServiceStatus)
            {
                StaticLogger.Info(sourceLog,
                    "Did you check the service status before calling the API?", logLayer: 1);
                StaticLogger.Err(sourceLog,
                    "rying to call the server, but the server seems not online", logLayer: 0);
                yield break;
            }

            StaticLogger.Info(sourceLog, "STARTING REQUEST", logLayer: 2);
            deviceLoginResponsePack = null;
            inProgress = true;
            completed = false;

            string requestURL = GetAPIUrl(
                ApiAddress_DeviceLogin
            );
            api_device_login_request payload = new api_device_login_request();
            payload.user_id = userID;
            payload.device_id = deviceID;
            payload.session_token = userSessionToken;

            yield return BSCOR_PerformRequestPost(
                requestURL,
                JsonUtility.ToJson(payload, true),
                timeout
            );

            deviceLoginResponsePack = JsonUtility.FromJson<api_device_login_response>(result);

            if (resultCode == 200)
            {
                deviceLoggedIn = true;
                SarAPI.deviceID = deviceID;
                StaticLogger.Info(sourceLog, $"OK Device Logged in", logLayer: 0);
            }

            inProgress = false;
            completed = true;
            StaticLogger.Info(sourceLog, "CLOSING REQUEST", logLayer: 2);
        }



        // ===== API CALLS : HL2 DOWNLOAD ===== //

        /// <summary>
        /// ...
        /// </summary>
        public static bool DownloadSuccess
        {
            get => downloadSuccess;
        }

        /// <summary>
        /// ...
        /// </summary>
        public static api_hl2_download_response Hl2DownloadResponse
        {
            get => hl2DownloadResponsePack;
        }

        /// <summary>
        /// raw JSON response from server
        /// </summary>
        public static string Hl2DownloadResponseJson
        {
            get => hl2DownloadResponsePackJson;
        }

        public static IEnumerator ApiCall_Hl2Download(string referencePositionId, Vector3 currentPosition, float radius, bool calibrating = false, int timeout = -1)
        {
            string sourceLog = "SarAPI:ApiCall_Hl2Download";
            yield return null;
            downloadSuccess = false;

            if (inProgress)
            {
                StaticLogger.Err(sourceLog,
                    "can't handle more than one request per time; not allowed", logLayer: 1);
                yield break;
            }
            else if (!userLoggedIn || userSessionToken == "")
            {
                StaticLogger.Err(sourceLog,
                    "Cannot download from server: missing login");
                yield break;
            }
            else if (!SarAPI.ServiceStatus)
            {
                StaticLogger.Info(sourceLog,
                    "Did you check the service status before calling the API?", logLayer: 1);
                StaticLogger.Err(sourceLog,
                    "rying to call the server, but the server seems not online", logLayer: 0);
                yield break;
            }
            else if (!calibrating && fakeToken == "")
            {
                StaticLogger.Err(sourceLog,
                    "Trying to get data (no calibration option enabled) with not yet set fake token", logLayer: 0);
                yield break;
            }
            else if (referencePositionId == "")
            {
                StaticLogger.Err(sourceLog,
                    "Reference Position ID cannot be null", logLayer: 0);
                yield break;
            }

            StaticLogger.Info(sourceLog, "STARTING REQUEST", logLayer: 2);
            hl2DownloadResponsePack = null;
            inProgress = true;
            completed = false;

            string requestURL = GetAPIUrl(
                ApiAddress_Hl2Download
            );
            api_hl2_download_request payload = new api_hl2_download_request();
            payload.user_id = userID;
            payload.device_id = deviceID;
            payload.session_token = userSessionToken;
            payload.based_on = fakeToken;
            payload.ref_id = referencePositionId;
            payload.center = Vector3ToList(calibrating ? Vector3.zero : currentPosition);
            payload.radius = radius;

            yield return BSCOR_PerformRequestPost(
                requestURL,
                JsonUtility.ToJson(payload, true),
                timeout
            );

            hl2DownloadResponsePackJson = result;
            hl2DownloadResponsePack = JsonUtility.FromJson<api_hl2_download_response>(result);

            if (resultCode == 200)
            {
                fakeToken = hl2DownloadResponsePack.based_on;
                if (referencePosId == "")
                    referencePosId = referencePositionId;
                maxIdx = hl2DownloadResponsePack.max_id;
                StaticLogger.Info(sourceLog, $"OK download done", logLayer: 0);
                downloadSuccess = true;
            }
            else
                downloadSuccess = false;

            inProgress = false;
            completed = true;
            StaticLogger.Info(sourceLog, "CLOSING REQUEST", logLayer: 2);
        }



        // ===== API CALLS : HL2 UPLOAD ===== //

        /// <summary>
        /// ...
        /// </summary>
        public static bool UploadSuccess
        {
            get => uploadSuccess;
        }

        /// <summary>
        /// ...
        /// </summary>
        public static api_hl2_upload_response hl2UploadResponse
        {
            get => hl2UploadResponsePack;
        }

        /// <summary>
        /// ...
        /// </summary>
        public static Dictionary<int, int> AlignmentLookup
        {
            get => uploadAlignmentLookup;
        }

        /// <summary>
        /// ...
        /// </summary>
        public static int ServerPositionIndex
        {
            get => maxIdx;
        }

        public static IEnumerator ApiCall_Hl2Upload(List<data_hl2_waypoint> waypoints = null, List<data_hl2_path> paths = null, int timeout = -1)
        {
            string sourceLog = "SarAPI:ApiCall_Hl2Upload";
            yield return null;

            if (inProgress)
            {
                StaticLogger.Err(sourceLog,
                    "can't handle more than one request per time; not allowed", logLayer: 1);
                yield break;
            }
            else if (!userLoggedIn || userSessionToken == "")
            {
                StaticLogger.Err(sourceLog,
                    "Cannot download from server: missing login");
                yield break;
            }
            else if (!SarAPI.ServiceStatus)
            {
                StaticLogger.Info(sourceLog,
                    "Did you check the service status before calling the API?", logLayer: 1);
                StaticLogger.Err(sourceLog,
                    "rying to call the server, but the server seems not online", logLayer: 0);
                yield break;
            }
            else if (fakeToken == "")
            {
                StaticLogger.Info(sourceLog,
                    "Did you call API download before calling the UPLOAD API entry?", logLayer: 1);
                StaticLogger.Err(sourceLog,
                    "Trying to upload data (no calibration option enabled) with not yet set fake token", logLayer: 0);
                yield break;
            }
            else if (referencePosId == "")
            {
                StaticLogger.Info(sourceLog,
                    "Did you call API download before calling the UPLOAD API entry?", logLayer: 1);
                StaticLogger.Err(sourceLog,
                    "Trying to upload data with not yet set reference position ID", logLayer: 0);
                yield break;
            }

            StaticLogger.Info(sourceLog, "STARTING REQUEST", logLayer: 2);
            hl2UploadResponsePack = null;
            inProgress = true;
            completed = false;
            uploadSuccess = false;

            string requestURL = GetAPIUrl(
                ApiAddress_Hl2Upload
            );
            api_hl2_upload_request payload = new api_hl2_upload_request();
            payload.user_id = userID;
            payload.device_id = deviceID;
            payload.session_token = userSessionToken;
            payload.ref_id = referencePosId;
            payload.based_on = fakeToken;
            payload.waypoints = waypoints;
            payload.paths = paths;

            yield return BSCOR_PerformRequestPost(
                requestURL,
                JsonUtility.ToJson(payload, true),
                timeout
            );

            hl2UploadResponsePack = JsonUtility.FromJson<api_hl2_upload_response>(result);

            if (resultCode == 200)
            {
                foreach(data_hl2_align_item item in hl2UploadResponsePack.wp_alignment)
                {
                    if (!uploadAlignmentLookup.ContainsKey(item.request_position_id))
                        uploadAlignmentLookup.Add(
                            item.request_position_id,
                            item.aligned_position_id
                        );
                }
                maxIdx = hl2UploadResponsePack.max_id;
                StaticLogger.Info(sourceLog, $"OK upload done", logLayer: 0);
                uploadSuccess = true;
            }
            else
                uploadSuccess = false;

            inProgress = false;
            completed = true;
            StaticLogger.Info(sourceLog, "CLOSING REQUEST", logLayer: 2);
        }



        // ===== UTILITY URL BUILDER ===== //

        private static string GetAPIUrl(string apiPath = "/")
        {
            return $"{ApiURL}{apiPath}";
        }



        // ===== UTILITY REQUEST EXECUTION ===== //

        private static IEnumerator BSCOR_PerformRequestGet(string requestURL, int timeout = -1)
        {
            string sourceLog = $"SarAPI:BSCOR_PerformRequestGet:{requestURL}";
            yield return null;

            inProgress = true;
            completed = false;
            success = false;
            result = "";

            www = new UnityWebRequest();
            www.downloadHandler = new DownloadHandlerBuffer();

            www.method = UnityWebRequest.kHttpVerbGET;
            www.url = requestURL;
            if (timeout > 0)
                www.timeout = timeout;

            StaticLogger.Info(sourceLog, $"Sending GET request with: \n\tURL: {requestURL}", logLayer: 4);
            yield return www.SendWebRequest();
            resultCode = (int) www.responseCode;
            StaticLogger.Info(sourceLog, $"Response from server: \n\tTEXT: {www.downloadHandler.text}\n\tSTATUS CODE: {resultCode}", logLayer: 4);

            if (!handleRequestResult(www, www.result, sourceLog))
                yield break;

            result = www.downloadHandler.text;
            
            inProgress = false;
            www = null;
        }

        private static IEnumerator BSCOR_PerformRequestPost(string requestURL, string payload, int timeout = -1)
        {
            string sourceLog = $"SarAPI:BSCOR_PerformRequestPost:{requestURL}";
            yield return null;

            inProgress = true;
            completed = false;
            success = false;
            result = "";
            
            www = new UnityWebRequest(requestURL);
            www.method = UnityWebRequest.kHttpVerbPOST;
            www.uploadHandler = new UploadHandlerRaw(Encoding.ASCII.GetBytes(payload));
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            if (timeout > 0)
                www.timeout = timeout;

            StaticLogger.Info(sourceLog, $"Sending to server: \n\tJSON: {payload}\n\t BINARY: {www.uploadedBytes}", logLayer: 4);
            yield return www.SendWebRequest();
            StaticLogger.Info(sourceLog, $"Response from server: \n\tTEXT: {www.downloadHandler.text}", logLayer: 4);

            result = www.downloadHandler.text;
            if (!handleRequestResult(www, www.result, sourceLog))
                yield break;

            inProgress = false;
            www = null;
        }

        private static bool handleRequestResult(UnityWebRequest requestHandle, UnityWebRequest.Result result, string sourceLog)
        {
            bool reqSuccess = false;
            resultCode = (int) requestHandle.responseCode;

            switch (result)
            {
                case UnityWebRequest.Result.ConnectionError:
                    StaticLogger.Warn(sourceLog, "ConnectionError: " + www.error, logLayer: 1);
                    inProgress = false;
                    www = null;
                    break;
                case UnityWebRequest.Result.DataProcessingError:
                    StaticLogger.Warn(sourceLog, "DataProcessingError: " + www.error, logLayer: 1);
                    inProgress = false;
                    www = null;
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    if (resultCode != 418)
                    {
                        StaticLogger.Warn(sourceLog, "ProtocolError: " + www.error, logLayer: 1);
                        inProgress = false;
                        www = null;
                        break;
                    }
                    else
                    {
                        StaticLogger.Info(sourceLog, "UnityWebRequest.Result.Success (I'm a Teapot)", logLayer: 2);
                        reqSuccess = true;
                        break;
                    }
                case UnityWebRequest.Result.Success:
                    StaticLogger.Info(sourceLog, "UnityWebRequest.Result.Success", logLayer: 2);
                    reqSuccess = true;
                    break;
            }

            internalServerError = (requestHandle.responseCode == 500);
            if (internalServerError)
            {
                StaticLogger.Err(sourceLog, $"Internal Server Error for '{requestHandle.method}' request '{requestHandle.url}'");
            }

            success = reqSuccess;
            return reqSuccess;
        }

        private static bool handleRequestResult(int httpCode, string httpMethod, string httpUrl, string sourceLog)
        {
            bool reqSuccess = false;
            resultCode = httpCode;

            if(httpCode != 200 && httpCode != 202 && httpCode != 418)
            { 
                StaticLogger.Warn(sourceLog, $"Request error! Server returned code {httpCode}", logLayer: 1);
                inProgress = false;
            }
            else
            {
                StaticLogger.Info(sourceLog, $"Request Success: server returned status code {httpCode}", logLayer: 2);
                reqSuccess = true;
            }

            internalServerError = (resultCode == 500);
            if (internalServerError)
            {
                StaticLogger.Err(sourceLog, $"Internal Server Error for '{httpMethod}' request '{httpUrl}'");
            }

            success = reqSuccess;
            return reqSuccess;
        }

        private static List<float> Vector3ToList(Vector3 v)
        {
            return new List<float>
            {
                v.x, v.y, v.z
            };
        }
    }
}
