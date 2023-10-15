using System;
using System.IO;
using System.Linq;

namespace T_Craft_Game_Launcher.Core
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
                return System.IO.Directory.GetFiles(path).Length == 0;
            }

            /// <summary>
            /// Returns size of directory in GB.
            /// </summary>
            /// <param name="path">The path of the directory to check.</param>
            /// <returns>Size of directory in GB.</returns>
            public static double GetSize(string path)
            {
                var files = System.IO.Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
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
            }
        }

        /// <summary>
        /// A nested class for handling the file system.
        /// </summary>
        public static class FileSystem
        {
            public static string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

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
        }
    }
}
