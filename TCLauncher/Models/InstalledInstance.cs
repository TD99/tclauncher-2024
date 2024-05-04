using System;
using System.Collections.Generic;
using TCLauncher.Core;

namespace TCLauncher.Models
{
    public class InstalledInstance : Instance
    {
        public string InstallationDir { get; set; }
        public string DataDir { get; set; }
        public string ConfigFile{ get; set; }
        public string LastServer { get; set; }

        public InstalledInstance(string name, string displayName, Guid guid, string version, bool upgradeable, string thumbnailUrl, string type, string mcVersion, bool? useFabric, bool? useForge, string workingDirZipUrl, List<Patch> patches, bool usePatch, bool? useIsolation, Dictionary<string, List<string>> workingDirDesc, string appletUrl, Dictionary<string, object> requirements, List<Server> servers, int minimumRamMb, int maximumRamMb, string[] jvmArguments)
            : base(name, displayName, guid, version, upgradeable, thumbnailUrl, type, mcVersion, useFabric, useForge, workingDirZipUrl, patches, usePatch, useIsolation, workingDirDesc, appletUrl, requirements, servers, minimumRamMb, maximumRamMb, jvmArguments)
        {
            InstallationDir = IoUtils.Tcl.GetInstancePath(guid);
            DataDir = IoUtils.Tcl.GetInstanceDataPath(guid);
            ConfigFile = IoUtils.Tcl.GetInstanceConfigPath(guid);
            Is_Installed = true;
        }
        
        public InstalledInstance(Instance instance)
            : base(instance.Name, instance.DisplayName, instance.Guid, instance.Version,
                   instance.Upgradeable, instance.ThumbnailURL, instance.Type, instance.McVersion, instance.UseFabric, instance.UseForge, instance.WorkingDirZipURL,
                   instance.Patches, instance.UsePatch, instance.UseIsolation, instance.WorkingDirDesc, instance.AppletURL, instance.Requirements,
                   instance.Servers, instance.MinimumRamMb, instance.MaximumRamMb, instance.JVMArguments)
        {
            InstallationDir = IoUtils.Tcl.GetInstancePath(instance.Guid);
            DataDir = IoUtils.Tcl.GetInstanceDataPath(instance.Guid);
            ConfigFile = IoUtils.Tcl.GetInstanceConfigPath(instance.Guid);
            Is_Installed = true;
        }

        public InstalledInstance()
        {
            Is_Installed = true;
        }
    }
}
