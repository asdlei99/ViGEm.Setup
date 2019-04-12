﻿using System;
using System.Runtime.InteropServices;

namespace ViGEm.Setup.CustomAction.Util
{
    /// <summary>
    ///     Managed wrapper for common SetupAPI operations.
    /// </summary>
    internal static partial class Devcon
    {
        /// <summary>
        ///     Invokes the installation of a driver via provided .INF file.
        /// </summary>
        /// <param name="fullInfPath">An absolute path to the .INF file to install.</param>
        /// <param name="rebootRequired">True if a machine reboot is required, false otherwise.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool InstallDriver(string fullInfPath, out bool rebootRequired)
        {
            return DiInstallDriver(IntPtr.Zero, fullInfPath, DIIRFLAG_FORCE_INF, out rebootRequired);
        }

        /// <summary>
        ///     Creates a virtual device node (hardware ID) in the provided device class.
        /// </summary>
        /// <param name="className">The device class name.</param>
        /// <param name="classGuid">The GUID of the device class.</param>
        /// <param name="node">The node path terminated by two null characters.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool CreateDeviceNode(string className, Guid classGuid, string node)
        {
            var deviceInfoSet = (IntPtr) (-1);
            var deviceInfoData = new SP_DEVINFO_DATA();

            try
            {
                deviceInfoSet = SetupDiCreateDeviceInfoList(ref classGuid, IntPtr.Zero);

                if (deviceInfoSet == (IntPtr) (-1)) return false;

                deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);

                if (!SetupDiCreateDeviceInfo(
                    deviceInfoSet,
                    className,
                    ref classGuid,
                    null,
                    IntPtr.Zero,
                    DICD_GENERATE_ID,
                    ref deviceInfoData
                ))
                    return false;

                if (!SetupDiSetDeviceRegistryProperty(
                    deviceInfoSet,
                    ref deviceInfoData,
                    SPDRP_HARDWAREID,
                    node,
                    node.Length * 2
                ))
                    return false;

                if (!SetupDiCallClassInstaller(
                    DIF_REGISTERDEVICE,
                    deviceInfoSet,
                    ref deviceInfoData
                ))
                    return false;
            }
            finally
            {
                if (deviceInfoSet != (IntPtr) (-1))
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }


            return true;
        }

        /// <summary>
        ///     Instructs the system to re-enumerate hardware devices.
        /// </summary>
        /// <returns>True on success, false otherwise.</returns>
        public static bool RefreshDevices()
        {
            if (CM_Locate_DevNode_Ex(
                    out var devRoot,
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero
                ) != CR_SUCCESS)
                return false;

            return CM_Reenumerate_DevNode_Ex(devRoot, 0, IntPtr.Zero) == CR_SUCCESS;
        }

        /// <summary>
        ///     Given an INF file and a hardware ID, this function installs updated drivers for devices that match the hardware ID.
        /// </summary>
        /// <param name="hardwareId">A string that supplies the hardware identifier to match existing devices on the computer.</param>
        /// <param name="fullInfPath">A string that supplies the full path file name of an INF file.</param>
        /// <param name="rebootRequired">A variable that indicates whether a restart is required and who should prompt for it.</param>
        /// <returns>
        ///     The function returns TRUE if a device was upgraded to the specified driver.
        ///     Otherwise, it returns FALSE and the logged error can be retrieved with a call to GetLastError.
        /// </returns>
        public static bool UpdateDeviceDriver(string hardwareId, string fullInfPath, out bool rebootRequired)
        {
            return UpdateDriverForPlugAndPlayDevices(
                IntPtr.Zero,
                hardwareId,
                fullInfPath,
                INSTALLFLAG_FORCE | INSTALLFLAG_NONINTERACTIVE,
                out rebootRequired
            );
        }
    }
}