using Newtonsoft.Json;
using System.Net;

namespace StatusBot
{
    public class Servers
    {
        const string serverListUri = "https://publicapi.battlebit.cloud/Servers/GetServerList";

        public static ServerList GetServerList()
        {
            string response = Get(serverListUri);
            ServerData[] serverList = JsonConvert.DeserializeObject<ServerData[]>(response);
            ServerList serverListInstance = new ServerList();
            serverListInstance.serverData = serverList.ToList();

            return serverListInstance;
        }
        private static string Get(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public class ServerData
    {
        public string Name;
        public string Map;
        public string Gamemode;
        public string Region;
        public int Players;
        public int QueuePlayers;
        public int MaxPlayers;
        public int Hz;
        public string DayNight;
        public bool IsOfficial;
        public bool HasPassword;
        public string AntiCheat;
        public string Build;
    }

    public class ServerList
    {
        public List<ServerData> serverData;
    }

}
