using Newtonsoft.Json;
using System.Reflection;

namespace StatusBot
{
    public class JsonManager
    {
        public BotConfig GetBotConfig()
        {
            string botPath = FixPath(Assembly.GetEntryAssembly().Location);
            string configPath = botPath + "\\Config.json";

            if (!File.Exists(configPath))
            {
                string jsonString = JsonConvert.SerializeObject(new BotConfig(), Formatting.Indented);
                File.WriteAllText(configPath, jsonString);
            }

            return OpenBotConfig();
        }

        private BotConfig? OpenBotConfig()
        {
            string botPath = FixPath(Assembly.GetExecutingAssembly().Location);
            string configPath = botPath + "\\Config.json";

            using StreamReader reader = new(configPath);
            string json = reader.ReadToEnd();

            return JsonConvert.DeserializeObject<BotConfig>(json);
        }

        private static string FixPath(string path)
        {
            int endPos = path.LastIndexOf("\\");
            return path.Substring(0, endPos);
        }
    }

    public class BotConfig
    {
        public string ServerNamePrefix = "yourserversprefix";
        public string BotToken = "yourtoken";
    }
}
