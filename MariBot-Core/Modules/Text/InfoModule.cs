using Discord;
using Discord.Commands;
using MariBot.Core.Utils;

namespace MariBot.Core.Modules.Text
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _commands;

        public InfoModule(CommandService commands)
        {
            _commands = commands;
        }

        [Command("info")]
        public Task Info()
            => ReplyAsync(
                $"Hello, I am a bot called {Context.Client.CurrentUser.Username} written in Discord.Net\n");

        [Command("help")]
        public async Task Help()
        {
            var commands = _commands.Commands
                .SelectMany(c => c.Aliases)
                .Select(a => a.Split(' ')[0].ToLowerInvariant())
                .ToHashSet()
                .OrderBy(n => n)
                .ToList();

            var text = "**Available Commands:**\n" + string.Join(", ", commands);
            var chunks = PaginationHelpers.ChunkText(text, 3500);
            int page = 1;
            foreach (var chunk in chunks)
            {
                var eb = new EmbedBuilder()
                    .WithTitle($"Commands (Page {page++} of {chunks.Count})")
                    .WithDescription(chunk)
                    .WithColor(Color.Blue);
                await ReplyAsync(embed: eb.Build());
            }
        }

        [Command("help")]
        public async Task Help([Remainder] string commandName)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Resources", "Help", $"{commandName.ToLower().Trim()}.md");

            if (!File.Exists(path))
            {
                await ReplyAsync($"No help file found for command: `{commandName}`");
                return;
            }

            var content = File.ReadAllText(path);
            var chunks = PaginationHelpers.ChunkText(content, 3500);
            int page = 1;
            foreach (var chunk in chunks)
            {
                var eb = new EmbedBuilder()
                    .WithTitle($"{commandName} Help (Page {page++} of {chunks.Count})")
                    .WithDescription(chunk)
                    .WithColor(Color.Blue);
                await ReplyAsync(embed: eb.Build());
            }
        }
    }
}
