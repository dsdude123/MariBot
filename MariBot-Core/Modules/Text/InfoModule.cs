using Discord.Commands;

namespace MariBot.Core.Modules.Text
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        [Command("info")]
        public Task Info()
            => ReplyAsync(
                $"Hello, I am a bot called {Context.Client.CurrentUser.Username} written in Discord.Net\n");

        [Command("help")]
        public Task Help()
        {
            // TODO: Rewrite this to not rely on web-hosted docs
            if (Context.Guild.Id == 297485054836342786) // Server is prohibited from using some commands
            {
                return ReplyAsync("https://dsdude123.github.io/MariBot/297485054836342786/commands.html");
            }
            else if (Context.Guild.Id == 564645677586710548)
            {
                return ReplyAsync("https://dsdude123.github.io/MariBot/564645677586710548/commands.html");
            }
            else
            {
                return ReplyAsync("https://dsdude123.github.io/MariBot/commands.html");
            }
        }
    }
}
