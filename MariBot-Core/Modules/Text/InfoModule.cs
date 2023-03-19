using Discord.Commands;

namespace MariBot.Core.Modules.Text
{
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        [Command("info")]
        public Task Info()
            => ReplyAsync(
                $"Hello, I am a bot called {Context.Client.CurrentUser.Username} written in Discord.Net\n");

        [Command("error")]
        public Task Error()
            => throw new ApplicationException("I failed intentionally");
    }
}
