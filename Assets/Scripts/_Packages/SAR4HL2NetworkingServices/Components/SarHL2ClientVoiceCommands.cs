using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Project.Scripts.Utils;
using Project.Scripts.Components;
using Packages.SAR4HL2NetworkingServices.Utils;

namespace Packages.SAR4HL2NetworkingServices.Components
{
    public class SarHL2ClientVoiceCommands : MonoBehaviour
    {
        // ===== PRIVATE ===== //

        // only for logging
        private string sourceLogClass = "SaHL2ClientVoiceCommands";
        


        // ===== CONNECT ===== //

        public void VOICE_Connect()
        {
            string sourceLog = $"{sourceLogClass}:VOICE_Connect";

            SarHL2Client client = SarAPI.Client;
            if (client == null)
            {
                StaticLogger.Info(sourceLog, "No registered client", logLayer: 2);
                return;
            }

            if(SarAPI.UserLoggedIn)
            {
                if(SarAPI.DeviceLoggedIn)
                {
                    StaticLogger.Info(sourceLog, "Already connected", logLayer: 2);
                    return;
                }
                else
                {
                    StaticLogger.Warn(sourceLog, "Login done but device is not logged in!", logLayer: 1);
                    StaticLogger.Info(sourceLog, "Trying to disconnect ... ", logLayer: 1);
                    if (!client.Disconnect())
                    {
                        StaticLogger.Err(sourceLog, "Trying to disconnect ... ERROR: can't log out from the service!", logLayer: 0);
                        return;
                    }
                    StaticLogger.Info(sourceLog, "Trying to disconnect ... OK ready to redo connection", logLayer: 1);
                }
            }
            
            StaticLogger.Info(sourceLog, "Connection request ... ", logLayer: 1);
            client.InitConnection();
            StaticLogger.Info(sourceLog, "Connection request ... connection process started", logLayer: 1);
        }



        // ===== DISCONNECT ===== //

        public void VOICE_Disconnect()
        {
            string sourceLog = $"{sourceLogClass}:VOICE_Disconnect";

            SarHL2Client client = SarAPI.Client;
            if (client == null)
            {
                StaticLogger.Info(sourceLog, "No registered client", logLayer: 2);
                return;
            }

            if (!SarAPI.UserLoggedIn)
            {
                StaticLogger.Info(sourceLog, "Not connected, skip", logLayer: 2);
                return;
            }

            StaticLogger.Info(sourceLog, "Logout request ... ", logLayer: 1);
            if(!client.Disconnect())
            {
                StaticLogger.Err(sourceLog, "Logout request ... ERROR: can't logout", logLayer: 0);
                return;
            }
            StaticLogger.Info(sourceLog, "Logout request ... OK logout success", logLayer: 1);
        }
    }
}
