namespace TCLauncher.Models
{
    public class McServerAddress
    {
        public string IP { get; set; }
        public int? Port { get; set; }

        public McServerAddress(string ip, int port)
        {
            IP = ip;
            Port = port;
        }

        public McServerAddress()
        {
        }
    }
}
