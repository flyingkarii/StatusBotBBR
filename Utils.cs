namespace StatusBot
{
    public class Utils
    {
        public static List<ServerData> GetOurServers()
        {
            ServerList serverList = Servers.GetServerList();
            List<ServerData> ourServers = new List<ServerData>();

            int index = 0;
            do
            {
                ServerData server = serverList.serverData.ElementAt(index);

                if (server.Name.StartsWith(Program.GetConfig().ServerNamePrefix))
                    ourServers.Add(server);

                index++;
            } while (index < serverList.serverData.Count);

            return ourServers;
        }

        public static List<ServerData> GetServers(string withGamemode = "", string withNamePrefix = "", string? withMap = "")
        {
            ServerList serverList = Servers.GetServerList();
            List<ServerData> result;

            result = serverList.serverData.Where((ServerData data) =>
                data.Gamemode.ToLower().StartsWith(withGamemode.ToLower())
                && data.Name.ToLower().StartsWith(withNamePrefix.ToLower())
                && data.Map.ToLower().StartsWith(withMap.ToLower())).ToList();

            return result;
        }

    }
}
