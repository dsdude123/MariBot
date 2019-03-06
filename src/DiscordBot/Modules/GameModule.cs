using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using StarBot.Models;

namespace StarBot.Modules
{
    [Group("game")]
    public class GameModule : ModuleBase<SocketCommandContext>
    {
        public static readonly ulong DAILY_BONUS = 20;

        [Command("daily")]
        public Task daily()
        {
            var guild = Context.Guild.Id;
            var user = Context.User.Id;
            GameStore myStore;
            if (System.IO.File.Exists(Environment.CurrentDirectory + "\\cache\\" + guild + "\\game.json"))
            {
                myStore = JsonConvert.DeserializeObject<GameStore>(
                    System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\cache\\" + guild + "\\game.json"));
            }
            else
            {
                myStore = new GameStore();
                myStore.profiles = new Dictionary<ulong, UserProfile>();
            }

            UserProfile u = null;
            if (myStore.profiles.ContainsKey(user))
            {
                u = myStore.profiles[user];
            }
            else
            {
                u = new UserProfile();
                myStore.profiles.Add(user, u);
            }

            string username = Context.User.Username;

            if (u.lastDailyCredit < DateTime.Now.AddDays(-1))
            {
                u.credits += DAILY_BONUS;
                u.lastDailyCredit = DateTime.Now;
                myStore.profiles[user] = u;
                System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\cache\\" + guild + "\\game.json",
                    JsonConvert.SerializeObject(myStore));
                return ReplyAsync(Context.User.Mention + " has received their daily bonus of " + DAILY_BONUS + ". New balance is " +
                                  u.credits + " credits.");
            }
            else
            {
                return ReplyAsync("You can only claim your daily credits once per day " + Context.User.Mention);
            }
        }

        [Command("balance")]
        public Task balance()
        {
            var guild = Context.Guild.Id;
            var user = Context.User.Id;
            GameStore myStore;
            if (System.IO.File.Exists(Environment.CurrentDirectory + "\\cache\\" + guild + "\\game.json"))
            {
                myStore = JsonConvert.DeserializeObject<GameStore>(
                    System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\cache\\" + guild + "\\game.json"));
            }
            else
            {
                myStore = new GameStore();
                myStore.profiles = new Dictionary<ulong, UserProfile>();
            }

            UserProfile u = null;
            if (myStore.profiles.ContainsKey(user))
            {
                u = myStore.profiles[user];
            }
            else
            {
                u = new UserProfile();
                myStore.profiles.Add(user, u);
            }

            myStore.profiles[user] = u;
            System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\cache\\" + guild + "\\game.json",
                JsonConvert.SerializeObject(myStore));
            return ReplyAsync("Balance for " + Context.User.Mention + " is " +
                              u.credits + " credits.");
        }

    }
}
