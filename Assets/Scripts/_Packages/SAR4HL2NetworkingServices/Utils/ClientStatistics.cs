using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Packages.SAR4HL2NetworkingServices.Utils
{ 
    [Serializable]
    public class ClientStatistics
    {
        public static readonly float CharDimKb = 0.001f;
        public static readonly List<string> CsvSchema = new List<string>
        {
            "api_type",
            "api_url",
            "http_type",
            "http_response_status",
            "request_len",
            "request_kb",
            "request_at_ms",
            "response_len",
            "response_kb",
            "response_at_ms",
            "latency_ms"
        };

        public enum ApiOperationTypeEnum
        {
            Undefined,
            ServerStatus,
            UserLogin,
            UserLogout,
            DeviceLogin,
            DeviceLogout,
            Upload,
            Download
        }
        public ClientStatistics.ApiOperationTypeEnum ApiOperationType = ApiOperationTypeEnum.Undefined;
        public string ApiURL = "";

        public enum CallTypeEnum
        {
            GET, // response only
            POST // request and response
        }
        public ClientStatistics.CallTypeEnum HttpType;
        public int HTTPStatusCode = 500;

        public string RequestJSON = "";
        public string ResponseJSON = "";
        public DateTime sendTimestamp = DateTime.Now;
        public DateTime receiveTimestamp = DateTime.Now;

        public int wpSentToServer = 0;
        public int wpReceivedFromServer = 0;
        public int renamingFromServer = 0;

        public List<string> ToCsvList()
        {
            List<string> res = new List<string>();

            // api_type
            res.Add(getApiType());
            // api_url
            res.Add(ApiURL);
            // http_type
            res.Add(getHttpType());
            // http_response_status
            res.Add(HTTPStatusCode.ToString());
            // request_len
            res.Add(RequestJSON.Length.ToString());
            // request_kb
            res.Add((RequestJSON.Length * CharDimKb).ToString("0.0000"));
            // request_at_ms
            res.Add(sendTimestamp.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString());
            // response_len
            res.Add(ResponseJSON.Length.ToString());
            // response_kb
            res.Add((ResponseJSON.Length * CharDimKb).ToString());
            // response_at_ms
            res.Add(receiveTimestamp.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString());
            // latency_ms
            float latency = (float) receiveTimestamp.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds - (float) sendTimestamp.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            res.Add(latency.ToString("0.000000"));

            return res;
        }

        private string getApiType()
        {
            switch (ApiOperationType)
            {
                case ApiOperationTypeEnum.Undefined:
                    return "UNKNOWN";
                case ApiOperationTypeEnum.ServerStatus:
                    return "STATUS";
                case ApiOperationTypeEnum.UserLogin:
                    return "LOGIN";
                case ApiOperationTypeEnum.UserLogout:
                    return "LOGOUT";
                case ApiOperationTypeEnum.DeviceLogin:
                    return "DEV_LOGIN";
                case ApiOperationTypeEnum.DeviceLogout:
                    return "DEV_LOGOUT";
                case ApiOperationTypeEnum.Upload:
                    return "UPLOAD";
                case ApiOperationTypeEnum.Download:
                    return "DOWNLOAD";
                default:
                    return "";
            }
        }

        private string getHttpType()
        {
            switch (HttpType)
            {
                case CallTypeEnum.GET:
                    return "GET";
                case CallTypeEnum.POST:
                    return "POST";
                default:
                    return "";
            }
        }
    }
}