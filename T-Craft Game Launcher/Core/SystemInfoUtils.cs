using System.Management;
using System;
using System.Collections.Generic;

namespace T_Craft_Game_Launcher.Core
{
    /// <summary>
    /// A utility class for retrieving system information.
    /// </summary>
    public static class SystemInfoUtils
    {
        /// <summary>
        /// Retrieves the total memory of all GPUs in GB.
        /// </summary>
        /// <returns>A dictionary where the keys are the names of the GPUs and the values are their memory in GB.</returns>
        public static Dictionary<string, double> GetTotalAdapterMemoryInGb()
        {
            var gpuMemory = new Dictionary<string, double>();
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            var gpuIndex = 0;
            foreach (var o in searcher.Get())
            {
                var obj = (ManagementObject)o;
                var gpuName = obj["Name"].ToString();
                var adapterRamBytes = Convert.ToDouble(obj["AdapterRAM"]);
                // Check if VRAM > 4GB
                if (adapterRamBytes > 3.8 * 1024 * 1024 * 1024)
                {
                    try
                    {
                        // Try to get HardwareInformation.qwMemorySize from the registry
                        var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey($@"SYSTEM\ControlSet001\Control\Class\{{4d36e968-e325-11ce-bfc1-08002be10318}}\000{gpuIndex}");
                        var qwMemorySize = key?.GetValue("HardwareInformation.qwMemorySize");
                        var driverDesc = key?.GetValue("DriverDesc");
                        if (qwMemorySize != null)
                        {
                            adapterRamBytes = Convert.ToDouble(qwMemorySize);
                            gpuName = Convert.ToString(driverDesc);
                        }
                    }
                    catch
                    {
                        // If it doesn't work, fall back to <4GB method
                    }
                }
                var adapterRamGb = Math.Round(adapterRamBytes / (1024 * 1024 * 1024), 2);
                gpuMemory[gpuName] = adapterRamGb;
                gpuIndex++;
            }
            return gpuMemory;
        }



        /// <summary>
        /// Retrieves the total physical memory in GB.
        /// </summary>
        /// <returns>The total physical memory in GB.</returns>
        public static double GetTotalPhysicalMemoryInGb()
        {
            double totalPhysicalMemoryInGb = 0;
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            foreach (var o in searcher.Get())
            {
                var obj = (ManagementObject)o;
                var totalPhysicalMemory = Convert.ToDouble(obj["TotalPhysicalMemory"]);
                totalPhysicalMemoryInGb = Math.Round(totalPhysicalMemory / (1024 * 1024 * 1024), 2);
            }
            return totalPhysicalMemoryInGb;
        }

    }
}
