using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace StatusBot
{
    public class Program
    {
        private static BotConfig _config;
        private DiscordSocketClient _client;
        private JsonManager _manager;
        private Thread _thread;
        private CancellationTokenSource _tokenSource;

        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            DiscordSocketConfig socketConfig = new DiscordSocketConfig();
            socketConfig.GatewayIntents = GatewayIntents.MessageContent;
            _client = new DiscordSocketClient(socketConfig);
            _tokenSource = new CancellationTokenSource();

            _client.Log += Log;
            _client.Ready += Client_Ready;
            _client.SlashCommandExecuted += SlashCommandHandler;
            _manager = new JsonManager();
            _config = _manager.GetBotConfig();

            await _client.LoginAsync(TokenType.Bot, _config.BotToken);
            await _client.StartAsync();

            BeginLoop();

            await Task.Delay(-1);
        }

        public async Task Client_Ready()
        {
            // Next, lets create our slash command builder. This is like the embed builder but for slash commands.
            var serversCommand = new SlashCommandBuilder();

            // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$
            serversCommand.WithName("servers");

            // Descriptions can have a max length of 100.
            serversCommand.WithDescription("A list of our servers.");

            try
            {
                // Now that we have our builder, we can call the CreateApplicationCommandAsync method to make our slash command.
                await _client.CreateGlobalApplicationCommandAsync(serversCommand.Build());
            }
            catch (ApplicationCommandException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.CommandName == "servers")
            {
                IMessageChannel channel = command.GetChannelAsync().Result;
                IDisposable typingDisposable = channel.EnterTypingState();

                List<ServerData> servers = Utils.GetOurServers();
                string serversString = "## Our Servers\n";

                foreach (ServerData server in servers)
                {
                    string queueString = "";
                    if (server.QueuePlayers > 0)
                        queueString = " (+" + server.QueuePlayers + ")";

                    serversString += "\n";
                    serversString += $"[**{server.Name}**]: {server.Players}{queueString}/{server.MaxPlayers}";
                }

                await command.RespondAsync(serversString);
                typingDisposable.Dispose();
            }
        }

        public void BeginLoop()
        {
            _thread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                do
                {
                    List<ServerData> ourServers = Utils.GetOurServers();

                    int players = 0;
                    int queuePlayers = 0;
                    int maxPlayers = 0;

                    foreach (ServerData server in ourServers)
                    {
                        players += server.Players;
                        queuePlayers += server.QueuePlayers;
                        maxPlayers += server.MaxPlayers;
                    }

                    string queueString = queuePlayers > 0 ? $" (+{queuePlayers})" : "";
                    _client.SetGameAsync($"{players}{queueString}/{maxPlayers} players.", null, ActivityType.Watching);
                    Thread.Sleep(5000);
                } while (!_tokenSource.Token.IsCancellationRequested);
            });

            _thread.Start();
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