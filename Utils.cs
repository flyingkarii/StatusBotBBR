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

    }
}
