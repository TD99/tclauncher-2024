using System.Management;
using System;

namespace T_Craft_Game_Launcher.Core
{
    public static class SysInfo
    {
        public static string getGpuMem()
        {
            string output = "";

            ManagementClass c = new ManagementClass("Win32_VideoController");
            foreach (ManagementObject o in c.GetInstances())
            {
                string gpuTotalMem = String.Format("{0} ", o["AdapterRam"]);
                output += gpuTotalMem + " | ";
            }

            return output;
        }
    }
}
