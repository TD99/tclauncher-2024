using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using System.Security.Principal;
using Newtonsoft.Json;
using TCLauncher.Models;

namespace TCLauncher.Core
{
    /// <summary>
    /// A utility class for handling I/O operations.
    /// </summary>
    public static class IoUtils
    {
        /// <summary>
        /// A nested class for handling specific file operations.
        /// </summary>
        public static class TclFile
        {
            /// <summary>
            /// Checks if a file is binary.
            /// </summary>
            /// <param name="filePath">The path of the file to check.</param>
            /// <returns>true if the file is binary; otherwise, false.</returns>
            public static bool IsBinary(string filePath)
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    int character;
                    while ((character = fileStream.ReadByte()) != -1)
                    {
                        if (character > 0 && character < 8 || character > 13 && character < 32)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public static bool CompareImages(string urlOrPath1, string urlOrPath2)
            {
                var image1 = GetImageBytes(urlOrPath1);
                var image2 = GetImageBytes(urlOrPath2);

                if (image1 == null || image2 == null)
                {
                    return false;
                }

                return image1.SequenceEqual(image2);
            }

            public static byte[] GetImageBytes(string urlOrPath)
            {
                if (!Uri.IsWellFormedUriString(urlOrPath, UriKind.Absolute))
                    return File.Exists(urlOrPath) ? File.ReadAllBytes(urlOrPath) : null;

                using (var webClient = new WebClient())
                {
                    try
                    {
                        return webClient.DownloadData(urlOrPath);
                    }
                    catch (WebException)
                    {
                        return null;
                    }
                }
            }

            public static bool DoesFileExistByUrlOrPath(string urlOrPath)
            {
                if (!Uri.IsWellFormedUriString(urlOrPath, UriKind.Absolute))
                    return File.Exists(urlOrPath);

                using (var webClient = new WebClient())
                {
                    try
                    {
                        webClient.DownloadData(urlOrPath);
                        return true;
                    }
                    catch (WebException)
                    {
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// A nested class for handling specific directory operations.
        /// </summary>
        public static class TclDirectory
        {
            /// <summary>
            /// Checks if a directory is empty.
            /// </summary>
            /// <param name="path">The path of the directory to check.</param>
            /// <returns>true if the directory is empty; otherwise, false.</returns>
            public static bool IsEmpty(string path)
            {
                try
                {
                    return Directory.GetFiles(path).Length == 0;
                }
                catch
                {
                    return true;
                }
            }

            /// <summary>
            /// Returns size of directory in GB.
            /// </summary>
            /// <param name="path">The path of the directory to check.</param>
            /// <returns>Size of directory in GB.</returns>
            public static double GetSize(string path)
            {
                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                var bytes = files.Select(name => new FileInfo(name)).Select(info => info.Length).Sum();
                var megabytes = (bytes / 1024f) / 1024f;
                var gigabytes = (megabytes / 1024f);
                return gigabytes;
            }
        }

        /// <summary>
        /// A nested class for handling application data.
        /// </summary>
        public static class Tcl
        {
            /// <summary>
            /// The path of the TCL root directory
            /// </summary>
            public static readonly string RootPath = Path.Combine(FileSystem.AppDataPath, "TCL");

            /// <summary>
            /// The path of the default TCL root directory
            /// </summary>
            public static readonly string DefaultRootPath = Path.Combine(FileSystem.RealAppDataPath, "TCL");
            
            /// <summary>
            /// The path of the cache directory
            /// </summary>
            public static readonly string CachePath = Path.Combine(RootPath, "Cache");
            
            /// <summary>
            /// The path of the instances directory
            /// </summary>
            public static readonly string InstancesPath = Path.Combine(RootPath, "Instances");
            
            /// <summary>
            /// The path of the udata directory
            /// </summary>
            public static readonly string UdataPath = Path.Combine(RootPath, "Udata");

            /// <summary>
            /// The default path for the application, located within the TCL root directory.
            /// </summary>
            public static readonly string DefaultPath = Path.Combine(RootPath, "Default");

            /// <summary>
            /// The shared path for the application, located within the TCL root directory.
            /// </summary>
            public static readonly string SharedPath = Path.Combine(RootPath, "Shared");

            /// <summary>
            /// Calculates the size of the directory at the specified path.
            /// </summary>
            /// <param name="path">The path of the directory. If null, the root path is used.</param>
            /// <returns>The size of the directory in bytes.</returns>
            public static double GetSize(string path = null)
            {
                return TclDirectory.GetSize(path ?? RootPath);
            }

            /// <summary>
            /// Retrieves information about the drive where the specified directory is located.
            /// </summary>
            /// <param name="path">The path of the directory. If null, the root path is used.</param>
            /// <returns>A DriveInfo object that provides information about the drive.</returns>
            public static DriveInfo GetDrive(string path = null)
            {
                return new DriveInfo(path ?? RootPath);
            }

            /// <summary>
            /// Creates the TCL directory structure.
            /// </summary>
            public static void CreateDirectries()
            {
                if (!Directory.Exists(RootPath)) Directory.CreateDirectory(RootPath);
                if (!Directory.Exists(CachePath)) Directory.CreateDirectory(CachePath);
                if (!Directory.Exists(InstancesPath)) Directory.CreateDirectory(InstancesPath);
                if (!Directory.Exists(UdataPath)) Directory.CreateDirectory(UdataPath);
                if (!Directory.Exists(DefaultPath)) Directory.CreateDirectory(DefaultPath);
                if (!Directory.Exists(SharedPath)) Directory.CreateDirectory(SharedPath);
            }

            /// <summary>
            /// Generates a temporary file name with an optional file type.
            /// </summary>
            /// <param name="fileType">The file type (e.g., ".txt"). Optional.</param>
            /// <returns>A string representing the full path of the temporary file.</returns>
            public static string GetTempFileName(string fileType = null)
            {
                string fileName = Path.GetRandomFileName();

                if (!string.IsNullOrEmpty(fileType))
                {
                    fileName = Path.ChangeExtension(fileName, fileType);
                }

                return Path.Combine(CachePath, fileName);
            }

            /// <summary>
            /// Gets the path of the specified instance.
            /// </summary>
            public static string GetInstancePath(Guid instanceGuid)
            {
                return Path.Combine(InstancesPath, instanceGuid.ToString());
            }

            /// <summary>
            /// Gets the path of the specified instance's data directory.
            /// </summary>
            public static string GetInstanceDataPath(Guid instanceGuid)
            {
                return Path.Combine(GetInstancePath(instanceGuid), "data");
            }

            /// <summary>
            /// Gets the path of the specified instance's config file.
            /// </summary>
            public static string GetInstanceConfigPath(Guid instanceGuid)
            {
                return Path.Combine(GetInstancePath(instanceGuid), "config.json");
            }

            /// <summary>
            /// Saves the specified instance's config file.
            /// </summary>
            public static string SaveInstalledInstanceConfig(InstalledInstance instance, string configFileOverride = null)
            {
                var path = configFileOverride ?? instance.ConfigFile;
                var json = JsonConvert.SerializeObject(instance);
                File.WriteAllText(path, json);
                return path;
            }
        }

        /// <summary>
        /// A nested class for handling the file system.
        /// </summary>
        public static class FileSystem
        {
            public static string RealAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            public static string AppDataPath = 
                Properties.Settings.Default.VirtualAppDataPath == ""
                    ? RealAppDataPath
                    : Properties.Settings.Default.VirtualAppDataPath;

            /// <summary>
            /// Gets the total storage of the file system in GB.
            /// </summary>
            /// <param name="driveInfo">The drive to check.</param>
            /// <returns>The total storage of the file system in GB.</returns>
            public static double GetTotalStorageInGb(DriveInfo driveInfo)
            {
                var bytes = driveInfo.TotalSize;
                var megabytes = (bytes / 1024f) / 1024f;
                var gigabytes = (megabytes / 1024f);
                return gigabytes;
            }

            /// <summary>
            /// Gets the total storage of the file system in GB. (drive is installation drive)
            /// </summary>
            /// <param name="path">The path of the directory. If null, the root path is used.</param>
            /// <returns>The total storage of the file system in GB.</returns>
            public static double GetTotalStorageInGb(string path = null)
            {
                return GetTotalStorageInGb(Tcl.GetDrive(path));
            }

            /// <summary>
            /// Gets the free storage of the file system in GB.
            /// </summary>
            /// <parm name="driveInfo">The drive to check.</parm>
            /// <returns>The free storage of the file system in GB.</returns>
            public static double GetFreeStorageInGb(DriveInfo driveInfo)
            {
                var bytes = driveInfo.TotalFreeSpace;
                var megabytes = (bytes / 1024f) / 1024f;
                var gigabytes = (megabytes / 1024f);
                return gigabytes;
            }

            /// <summary>
            /// Gets the free storage of the file system in GB. (drive is installation drive)
            /// </summary>
            /// <param name="path">The path of the directory. If null, the root path is used.</param>
            /// <returns>The free storage of the file system in GB.</returns>
            public static double GetFreeStorageInGb(string path = null)
            {
                return GetFreeStorageInGb(Tcl.GetDrive(path));
            }

            /// <summary>
            /// Checks if the current user has full access to a specified folder.
            /// </summary>
            /// <param name="folderPath">The path of the folder to check access for.</param>
            /// <returns>Returns true if the user has full access, false otherwise.</returns>
            public static bool HasFullAccess(string folderPath)
            {
                try
                {
                    var security = Directory.GetAccessControl(folderPath);
                    var accessRules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));

                    foreach (FileSystemAccessRule rule in accessRules)
                    {
                        var rights = rule.FileSystemRights;

                        if ((rights & FileSystemRights.Read) != FileSystemRights.Read)
                            continue;

                        if ((rights & FileSystemRights.ExecuteFile) != FileSystemRights.ExecuteFile)
                            continue;

                        if (rule.AccessControlType == AccessControlType.Allow)
                            return true;
                    }
                }
                catch
                {
                    // Exception handling here if needed
                }

                return false;
            }
        }
    }
}
