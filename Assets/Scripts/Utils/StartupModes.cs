using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Project.Scripts.Utils
{
    public enum StartupModes
    {
        /// <summary>
        /// Startup mode not speccified; each scene is free to choose its default
        /// </summary>
        Undefined,

        /// <summary>
        /// The mode used while developing on Unity Edtor; it doesn't include the calibration
        /// </summary>
        PcDevelopment,

        /// <summary>
        /// Used for testing the app on the device, with calibration included
        /// </summary>
        DeviceDevelopment,

        /// <summary>
        /// The mode used while developing on Unity Edtor; it includes the initial calibration
        /// </summary>
        PcDevelopmentWithCalibration,

        /// <summary>
        /// Used for testing the app on the device, without calibration step
        /// </summary>
        DeviceDevelopmentNoCalibration,

        /// <summary>
        /// Used for product release
        /// </summary>
        DeviceProduction
    }
}
