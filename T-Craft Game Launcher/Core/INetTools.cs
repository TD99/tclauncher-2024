using System.Net;

namespace T_Craft_Game_Launcher.Core
{
    public static class INetTools
    {
        public static bool requestPage(string url)
        {
            string[] protocols = { "http://", "https://" };
            url = removeProtocol(url, protocols);
            HttpWebRequest request;

            foreach (string protocol in protocols)
            {
                try
                {
                    request = (HttpWebRequest)HttpWebRequest.Create(protocol + url);
                    request.AllowAutoRedirect = false;
                    request.Method = "HEAD";
                    request.GetResponse();
                    return true;
                }
                catch
                {
                    // ignore the exception and try the next protocol
                }
            }
            return false;
        }

        public static string removeProtocol(string url, string[] protocols = null)
        {
            if (protocols == null)
            {
                protocols = new string[] { "http://", "https://" };
            }

            foreach (var i in protocols)
            {
                if (url.StartsWith(i))
                {
                    return url.Substring(i.Length);
                }
            }

            return url;
        }
    }
}
