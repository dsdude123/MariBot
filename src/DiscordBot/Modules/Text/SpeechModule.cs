using Discord.Commands;
using MariBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Modules.Text
{
    public class SpeechModule : ModuleBase<SocketCommandContext>
    {
        public TalkHubService TalkHubService { get; set; }

        [Command("jfk", RunMode = RunMode.Async)]
        public async Task JFKTTS([Remainder] string text)
        {
            TalkHubService.GetTextToSpeech(Context, "jfk", text);
        }

        [Command("madden", RunMode = RunMode.Async)]
        public async Task Madden([Remainder] string text)
        {
            TalkHubService.GetTextToSpeech(Context, "john-madden", text);
        }

        [Command("obama-tts", RunMode = RunMode.Async)]
        public async Task ObamaTTS([Remainder] string text)
        {
            TalkHubService.GetTextToSpeech(Context, "barack-obama", text);
        }

        [Command("reagan-tts", RunMode = RunMode.Async)]
        public async Task ReaganTTS([Remainder] string text)
        {
            TalkHubService.GetTextToSpeech(Context, "ronald-reagan", text);
        }

        [Command("stephenasmith", RunMode = RunMode.Async)]
        public async Task SmephenASmith([Remainder] string text)
        {
            TalkHubService.GetTextToSpeech(Context, "stephenasmith", text);
        }

        [Command("ye", RunMode = RunMode.Async)]
        public async Task Ye([Remainder] string text)
        {
            TalkHubService.GetTextToSpeech(Context, "ye", text);
        }

        [Command("yoda", RunMode = RunMode.Async)]
        public async Task Yoda([Remainder] string text)
        {
            TalkHubService.GetTextToSpeech(Context, "yoda", text);
        }
    }
}
