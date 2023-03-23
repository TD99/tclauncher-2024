using System.Management;
using System;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Windows;

namespace T_Craft_Game_Launcher.Core
{
    public static class SysInfo
    {
        //public static List<int> getGpuMem()
        //{
        //    List<int> output = new List<int>();

        //    ManagementClass c = new ManagementClass("Win32_VideoController");
        //    foreach (ManagementObject o in c.GetInstances())
        //    {
        //        var ram = o.Properties["AdapterRAM"].Value as ulong?;
        //        if (ram.HasValue)
        //        {
        //            output.Add((int)(ram / 1048576));
        //        }
        //    }

        //    return output;
        //}
        public static double GetTotalPhysicalMemoryInGB()
        {
            double totalPhysicalMemoryInGB = 0;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                double totalPhysicalMemory = Convert.ToDouble(obj["TotalPhysicalMemory"]);
                totalPhysicalMemoryInGB = Math.Round(totalPhysicalMemory / (1024 * 1024 * 1024), 2);
            }
            return totalPhysicalMemoryInGB;
        }

    }
}
