using System.IO;

namespace T_Craft_Game_Launcher.Core
{
    public static class IOTools
    {
        public static class File
        {
            public static bool IsBinary(string filePath)
            {
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
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
