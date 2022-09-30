using Discord.WebSocket;
using LiteDB;
using MariBot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MariBot.Services
{
    public class SpookeningService
    {

        private readonly SpookConfig config;

        private DiscordSocketClient discord;
        private LiteDatabase database;
        private LogService log;
        private Random random;
        private ILiteCollection<SpookedUser> SpookedUserCollection => database.GetCollection<SpookedUser>("SpookedUser");
        private ILiteCollection<SpookTask> SpookTaskCollection => database.GetCollection<SpookTask>("SpookTask");
        private Timer timer;

        public SpookeningService(DiscordSocketClient discord, LiteDatabase database, LogService log)
        {
            this.discord = discord;
            this.database = database;
            this.log = log;
            random = new Random();

            if (!File.Exists("spook.json"))
            {
                throw new FileNotFoundException("Spook config is missing.");
            }

            config = JsonConvert.DeserializeObject<SpookConfig>(File.ReadAllText("spook.json"));

            timer.Interval = 60000;
            timer.Elapsed += Timer_Elapsed;

            discord.Ready += Discord_Ready;
        }

        private async Task Discord_Ready()
        {
            await discord.GetGuild(config.TargetGuild).DownloadUsersAsync();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (IsOctober)
            {
                if (IsMidnight && IsHaloween)
                {

                }
                else if (IsMidnight)
                {

                }
            }

            // TODO: Cleanup after Oct code
        }

        public string GetRandomJoke => config.SpookyJokes[random.Next(0, config.SpookyJokes.Count - 1)];

        public bool IsOctober => DateTime.Now.Month == 10;

        public bool IsHaloween => (IsOctober && DateTime.Now.Day == 31);

        public bool IsMidnight => (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0);
    }
}
