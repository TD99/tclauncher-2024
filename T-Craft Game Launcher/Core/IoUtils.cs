using System.IO;

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
    }
}
