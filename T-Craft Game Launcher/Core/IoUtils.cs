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
        public static class File
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
        public static class Directory
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
        public static class AppData
        {
            /// <summary>
            /// Gets the path of the application data directory.
            /// </summary>
            /// <returns>The path of the application data directory.</returns>
            public static string GetPath()
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TCL");
            }

            /// <summary>
            /// Gets the size of the application data directory in GB.
            /// </summary>
            /// <returns>The size of the application data directory in GB.</returns>
            public static double GetSize()
            {
                return Directory.GetSize(GetPath());
            }

            /// <summary>
            /// Gets the drive TCL is installed on.
            /// </summary>
            /// <returns>The drive TCL is installed on.</returns>
            public static DriveInfo GetDrive()
            {
                return new DriveInfo(GetPath());
            }
        }

        /// <summary>
        /// A nested class for handling the file system.
        /// </summary>
        public static class FileSystem
        {
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
            /// Gets the total storage of the file system in GB. (drive is installation drive.)
            /// </summary>
            /// <returns>The total storage of the file system in GB.</returns>
            public static double GetTotalStorageInGb()
            {
                return GetTotalStorageInGb(AppData.GetDrive());
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
            /// Gets the free storage of the file system in GB. (drive is installation drive.)
            /// </summary>
            /// <returns>The free storage of the file system in GB.</returns>
            public static double GetFreeStorageInGb()
            {
                return GetFreeStorageInGb(AppData.GetDrive());
            }
        }
    }
}
