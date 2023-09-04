using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Timer = System.Timers.Timer;

namespace StatusBot
{
    public class Program
    {
        private static BotConfig _config;
        private DiscordSocketClient _client;
        private JsonManager _manager;
        private Timer _timer;
        private RestUserMessage _message;
        private List<ServerData> _servers;

        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            DiscordSocketConfig socketConfig = new DiscordSocketConfig();

            socketConfig.GatewayIntents = GatewayIntents.MessageContent | GatewayIntents.Guilds | GatewayIntents.GuildMessages;
            _client = new DiscordSocketClient(socketConfig);

            _client.Log += Log;
            _client.Ready += Client_Ready;
            _client.Disconnected += Disconnected;
            _client.SlashCommandExecuted += SlashCommandHandler;
            _manager = new JsonManager();
            _config = _manager.GetBotConfig();

            if (_config.BotToken == "yourtoken")
            {
                Console.WriteLine("Token is default! Please fill out the config and re-run.");
                await Task.Delay(-1);
                return;
            }

            _servers = Utils.GetOurServers();

            await _client.SetGameAsync(_servers.Count.ToString() + " servers.", null, ActivityType.Watching);
            await _client.LoginAsync(TokenType.Bot, _config.BotToken);
            await _client.StartAsync();

            await _client.SetStatusAsync(UserStatus.Idle);
            await Task.Delay(-1);
        }

        private void SetTimer()
        {
            Console.WriteLine($"Timer started for every {_config.DelayMilliseconds / 1000} seconds.");
            _timer = new Timer();
            _timer.Elapsed += async (s, o) => UpdatePresence();
            _timer.AutoReset = true;
            _timer.Interval = _config.DelayMilliseconds;
            _timer.Enabled = true;
        }

        public async Task Disconnected(Exception e)
        {
            if (_message != null)
                await _message.DeleteAsync();
        }

        public async Task Client_Ready()
        {
            IReadOnlyCollection<SocketApplicationCommand> commands = _client.GetGuild(1143675942535974993).GetApplicationCommandsAsync().Result;

            // Next, lets create our slash command builder. This is like the embed builder but for slash commands.
            var serversCommand = new SlashCommandBuilder();

            // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$
            serversCommand.WithName("servers");

            // Descriptions can have a max length of 100.
            serversCommand.WithDescription("A list of our servers.");
            serversCommand.AddOption(new SlashCommandOptionBuilder()
                .WithType(ApplicationCommandOptionType.String)
                .WithName("prefix")
                .WithDescription("Set to none to not filter by prefix. Default: Bot server prefix.")
                .WithRequired(false));
            serversCommand.AddOption(new SlashCommandOptionBuilder()
                .WithType(ApplicationCommandOptionType.String)
                .WithName("map")
                .WithDescription("Set to none to not filter by map. (Default)")
                .WithRequired(false));
            serversCommand.AddOption(new SlashCommandOptionBuilder()
                .WithType(ApplicationCommandOptionType.String)
                .WithName("gamemode")
                .WithDescription("Set to none to not filter by gamemode. (Default)")
                .WithRequired(false));

            _message = (RestUserMessage) await _client.GetGuild(_config.GuildId).GetTextChannel(_config.ChannelId).GetMessageAsync(_config.MessageToEdit);

            SetTimer();
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.CommandName == "servers")
            {
                string namePrefix = _config.ServerNamePrefix;
                string gamemode = "";
                string map = "";

                if (command.Data.Options.Count > 0)
                {
                    foreach (SocketSlashCommandDataOption option in command.Data.Options)
                    {
                        switch (option.Name)
                        {
                            case "gamemode":
                                gamemode = (string) option.Value;
                                break;
                            case "map":
                                map = (string) option.Value;
                                break;
                            case "prefix":
                                namePrefix = (string) option.Value;
                                break;
                        }

                    }
                }

                IMessageChannel channel = command.GetChannelAsync().Result;
                IDisposable typingDisposable = channel.EnterTypingState();

                if (namePrefix.StartsWith("none"))
                    namePrefix = "";

                List<ServerData> servers = Utils.GetServers(gamemode, namePrefix, map);
                string serversString = "## Servers\n";

                int players = 0;
                int queuePlayers = 0;
                int maxPlayers = 0;

                int i = 0;

                if (servers.Count > 0)
                {
                    do
                    {
                        ServerData server = servers.ElementAt(i);
                        if (servers.Count <= 10)
                        {
                            string queueString = "";
                            if (server.QueuePlayers > 0)
                                queueString = " (+" + server.QueuePlayers + ")";

                            serversString += "\n";
                            serversString += $"[``{server.Name}``]: {server.Players}{queueString}/{server.MaxPlayers}";
                        }

                        players += server.Players;
                        queuePlayers += server.QueuePlayers;
                        maxPlayers += server.MaxPlayers;

                        i++;
                    } while (i < servers.Count);
                } else
                {
                    serversString += $"\nNo servers found. {namePrefix}";
                }

                if (servers.Count > 5)
                    serversString += $"There are over **{servers.Count}** servers that matched your filters.\nPlayers: {players} (+{queuePlayers})/{maxPlayers}";

                await command.RespondAsync(serversString);
                typingDisposable.Dispose();
            }
        }

        public async void UpdatePresence()
        {
            List<ServerData> servers = Utils.GetOurServers();
            string queueString = "";

            int players = 0;
            int queuePlayers = 0;
            int maxPlayers = 0;

            foreach (ServerData server in servers)
            {
                players += server.Players;
                queuePlayers += server.QueuePlayers;
                maxPlayers += server.MaxPlayers;
            }

            string serversString = "## Servers\n";

            int i = 0;

            if (servers.Count > 0)
            {
                do
                {
                    queueString = "";
                    ServerData server = servers.ElementAt(i);

                    if (servers.Count <= 10)
                    {
                        if (server.QueuePlayers > 0)
                            queueString = " (+" + server.QueuePlayers + ")";

                        serversString += "\n";
                        serversString += $"[``{server.Name}``]: {server.Players}{queueString}/{server.MaxPlayers}";
                    }

                    i++;
                } while (i < servers.Count);
            }

            queueString = queuePlayers > 0 ? $" (+{queuePlayers})" : "";
            serversString += "\n";
            serversString += $"Total: {players}{queueString}/{maxPlayers}";
            serversString += $"\nUpdates every {_config.DelayMilliseconds / 1000} seconds.";

            await _message.ModifyAsync(m => { m.Content = serversString; });
            await _client.SetGameAsync($"{players}{queueString}/{maxPlayers} players.", null, ActivityType.Watching);


        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public static BotConfig GetConfig()
        {
            return _config;
        }
    }
}