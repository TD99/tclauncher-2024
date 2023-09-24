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
            foreach (var o in searcher.Get())
            {
                var obj = (ManagementObject)o;
                var gpuName = obj["Name"].ToString();
                var adapterRamBytes = Convert.ToDouble(obj["AdapterRAM"]);
                var adapterRamGb = Math.Round(adapterRamBytes / (1024 * 1024 * 1024), 2);
                gpuMemory[gpuName] = adapterRamGb;
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
