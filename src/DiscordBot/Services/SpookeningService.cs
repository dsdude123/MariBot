using Discord;
using Discord.WebSocket;
using LiteDB;
using MariBot.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using ActionType = MariBot.Models.ActionType;

namespace MariBot.Services
{
    public class SpookeningService
    {

        private readonly SpookConfig config;

        private DiscordSocketClient discord;
        private LiteDatabase database;
        // private ILogger log;
        private Random random;
        private ILiteCollection<SpookedUser> SpookedUserCollection => database.GetCollection<SpookedUser>("SpookedUser");
        private ILiteCollection<SpookTask> SpookTaskCollection => database.GetCollection<SpookTask>("SpookTask");
        private ILiteCollection<UsageLog> SpookLogCollection => database.GetCollection<UsageLog>("SpookLog");
        private System.Timers.Timer timer;

        public SpookeningService(DiscordSocketClient discord, LiteDatabase database)
        {
            this.discord = discord;
            this.database = database;
            //this.log = log;
            random = new Random();
            timer = new System.Timers.Timer();

            if (!File.Exists("spook.json"))
            {
                throw new FileNotFoundException("Spook config is missing.");
            }

            config = JsonConvert.DeserializeObject<SpookConfig>(File.ReadAllText("spook.json"));

            timer.Interval = 60000;
            timer.Elapsed += Timer_Elapsed;

            discord.Ready += Discord_Ready;
            discord.MessageReceived += MimicSpookyEmojiWithReactions;
            timer.Start();
        }

        private async Task Discord_Ready()
        {
            await discord.GetGuild(config.TargetGuildId).DownloadUsersAsync();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (IsOctober)
            {
                if (IsMidnight && IsHaloween)
                {
                    OnHalloweenMidnight();
                }
                else if (IsMidnight)
                {
                    OnMidnight();
                }
            }
            else
            {
                if (DateTime.Now.Month == 11 && DateTime.Now.Day == 3 && DateTime.Now.Minute == 1 && DateTime.Now.Hour == 0)
                {
                    //log.LogInformation("Starting reset all names task.");
                    Task.Factory.StartNew(() => ResetAllNames());
                }
            }
        }

        public void ResetAllNames()
        {
            foreach (var item in SpookedUserCollection.FindAll())
            {
                Thread.Sleep(5000);
                ResetNickname(item.UserId);
            }
        }

        public bool CheckUserRateLimit(ActionType actionType, ulong userId)
        {

            int userCountLimit = 0;
            int globalCountLimit = 0;
            TimeSpan userCountSpan;
            TimeSpan globalCountSpan;


            switch (actionType)
            {
                case ActionType.Reroll:
                    userCountSpan = TimeSpan.FromHours(18);
                    globalCountSpan = TimeSpan.FromHours(2);
                    userCountLimit = 3;
                    globalCountLimit = 3;
                    break;
                case ActionType.Joke:
                    userCountSpan = TimeSpan.FromHours(18);
                    globalCountSpan = TimeSpan.FromHours(2);
                    userCountLimit = 5;
                    globalCountLimit = 3;
                    break;
                case ActionType.Doot:
                    userCountSpan = TimeSpan.FromHours(2);
                    globalCountSpan = TimeSpan.FromHours(2);
                    userCountLimit = 3;
                    globalCountLimit = 5;
                    break;
                default:
                    return false;
            }
            var userCount = GetRateLimitCount(actionType, userId, userCountSpan);
            var globalCount = GetRateLimitCount(actionType, null, globalCountSpan);
            var result = userCount < userCountLimit && globalCount < globalCountLimit;
            return result;
        }

        public int GetRateLimitCount(ActionType actionType, ulong? userId, TimeSpan timeRange)
        {
            return SpookLogCollection
                .Find(x =>
                    (x.UserId == userId || userId == null) && x.ActionType == actionType && x.Timestamp > DateTime.Now.Add(-timeRange))
                .Count();
        }

        public void RegisterAction(ActionType actionType, ulong userId)
        {
            SpookLogCollection
                .Insert(new UsageLog()
                {
                    ActionType = actionType,
                    UserId = userId,
                    Timestamp = DateTime.Now,
                });
        }

        private async Task MimicSpookyEmojiWithReactions(SocketMessage message)
        {
            if (message.Author.IsBot)
                return;

            if (IsOctober)
            {
                List<Emoji> toReact = new List<Emoji>();

                if (CanUserUseSpookyCommands(message.Author.Id))
                {
                    foreach (var emoji in config.SpookyEmojis)
                    {
                        if (message.Content.Contains(emoji))
                        {
                            toReact.Add(new Emoji(emoji));
                        }
                    }
                }
                var userMessage = message as SocketUserMessage;

                await Task.Factory.StartNew(async () =>
                {
                    foreach (var e in toReact)
                    {
                        await userMessage.AddReactionAsync(e);
                        // don't block other rate limits so delay this a bit too
                        await Task.Delay(500);
                    }
                });
            }
        }

        public async Task ProcessSpooking()
        {
            var guild = discord.GetGuild(config.TargetGuildId);
            await guild.DownloadUsersAsync();

            // process spookenings that have been issued earlier that day
            foreach (var spook in SpookTaskCollection.Find(x => !x.TaskComplete))
            {
                // double check that the user isn't already spooked
                if (IsUserSpooked(spook.UserId))
                    continue;

                var user = guild.GetUser(spook.UserId);
                var by = guild.GetUser(spook.SpookedByUserId);


                if (user == null)
                {
                    //log.LogWarning($"User `user` [{spook.UserId}] was null.");

                    // could potentially result in this repeating a lot?
                    continue;
                }

                if (by == null)
                {
                    //log.LogWarning($"User `by` [{spook.SpookedByUserId}] was null.");
                    continue;
                }

                SpookUser(user, by);

                // mark this item as expired
                spook.TaskComplete = true;
                // update the database
                SpookTaskCollection.Update(spook);
            }
        }

        public async void RespookUser(ulong userId)
        {
            // ensure user is already spooked
            if (IsUserSpooked(userId))
            {
                // get their original name
                var user = SpookedUserCollection.FindOne(x => x.UserId == userId);
                // get the discord user to modify
                var discordUser = discord.GetGuild(config.TargetGuildId).GetUser(userId);

                // the original nickname may be null, if the user didn't already have a nickname
                var originalName = string.IsNullOrWhiteSpace(user.OriginalNickname) ? discordUser.Username : user.OriginalNickname;
                var newName = string.Format(GetRandomNicknameFormatter, originalName, ReverseString(originalName));

                var safeOriginalName = SanitizeNickname(originalName);
                var safeNewName = SanitizeNickname(newName);

                var message =
                    $"Uh-oh! **{safeOriginalName}** has been re-spooked and is now **{safeNewName}**! SpooOoOoooKy!";

                var _ = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        // just in case their nickname is too long
                        await discordUser.ModifyAsync(x => x.Nickname = Truncate(newName, 32));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });

                user.SpookedNickname = newName;
                SpookedUserCollection.Update(user);

                // send a message
                var _2 = Task.Factory.StartNew(async () => await SendSpookMessage(message));
            }
        }

        public void OnMidnight()
        {
            //log.LogInformation("Midnight trigger has occured.");
            for (int i = 0; i < 2; i++)
            {
                // get a random user
                var userId = GetRandomUser();
                if (userId == null)
                {
                    //log.LogWarning("Get random user was null");
                }

                var user = discord.GetGuild(config.TargetGuildId).GetUser(userId.Value);

                // if no users left, then just do nothing
                if (user == null)
                {
                   // log.LogWarning("Could not find a random user");
                    return;
                }

                //log.LogDebug($"Spooking user {user?.Id.ToString() ?? "NULL"}");

                SpookUser(user);
            }

           // log.LogDebug($"Processing spooking");
            // process the queue of spooked people
            ProcessSpooking()
                .GetAwaiter()
                .GetResult();

            //log.LogDebug($"Sending info message");

            Task.Factory.StartNew(async () =>
            {
                Thread.Sleep(1000);
                await SendSpookMessage(
@"Spooked Users have access to the following commads:
```
/spook <@User> - Spooks a user the following night.
/doot - Doot.
/spoop - Spoop.
/spookyjoke - Tells a spooky joke.
/thankmrskeletal - Chooses a new (spooky) nickname.
```
"
);
            });
        }

        private void OnHalloweenMidnight()
        {
            var message = "🎃 **It's Halloween** 🎃\nNow everyone has access to the spooky commands, just for today. Nicknames will reset in a few days.\nhttps://www.youtube.com/watch?v=viMWnEOYN_U";

            Task.Factory.StartNew(async () => await SendSpookMessage(message));
        }

        public void QueueSpooking(ulong user, ulong by)
        {
            if (!SpookTaskCollection.Exists(x => x.UserId == user))
            {
                SpookTaskCollection.Insert(new SpookTask()
                {
                    SpookedByUserId = by,
                    UserId = user,
                    TaskComplete = false
                });
            }
        }

        private void SpookUser(SocketGuildUser user)
        {
            var message =
                $"Uh-oh! {user.Mention} has been spooked! AHH! {user.Mention} can now spook up to **{config.SpookUserLimit}** other people with `\\spook @User`.";

            // don't care how bad this code is
            var _ = Task.Factory.StartNew(async () =>
            {
                await AddUserRoleAsync(user, $"Spooked by bot");
            });

            RegisterSpookedUser(user.Id, user.Nickname);

            var _2 = Task.Factory.StartNew(async () => await SendSpookMessage(message));
        }

        private void SpookUser(SocketGuildUser user, SocketGuildUser by)
        {
            var message =
                $"Uh-oh! {user.Mention} has been spooked by {by.Mention}! Yikes!";

            // don't care
            var _ = Task.Factory.StartNew(async () =>
            {
                await AddUserRoleAsync(user, $"Spooked by {by.Mention}");
            });

            RegisterSpookedUser(user.Id, user.Nickname, by.Id);

            var _2 = Task.Factory.StartNew(async () => await SendSpookMessage(message));
        }

        private async Task SendSpookMessage(string message)
        {
            var channel = discord.GetGuild(config.TargetGuildId).GetTextChannel(config.MessageChannelId);
            await channel.SendMessageAsync(message);
        }

        private async Task AddUserRoleAsync(SocketGuildUser user, string auditLogReason = null)
        {
            var role = discord.GetGuild(config.TargetGuildId).GetRole(config.SpookyRoleId);

            string logReason = auditLogReason ?? "BOO!";

            try
            {
                await user.AddRoleAsync(role, options: new RequestOptions() { AuditLogReason = logReason });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void ResetNickname(ulong userId)
        {
            var item = SpookedUserCollection.FindOne(x => x.UserId == userId);
            var originalName = item.OriginalNickname;

            if (originalName == null) return;

            var guild = discord.GetGuild(config.TargetGuildId);

            Task.Factory.StartNew(async () =>
            {
                var user = guild.GetUser(userId);
                // Don't revert nickname if user has made a manual change
                if (GetNameFromUser(user).Equals(item.SpookedNickname))
                {
                    await user.ModifyAsync(x =>
                    {
                        x.Nickname = originalName;
                    });
                }
            });
        }

        private ulong? GetRandomUser()
        {
            var guildUsers = discord.GetGuild(config.TargetGuildId).GetUsersAsync().FlattenAsync()
                .GetAwaiter().GetResult()
                .ToList();

            //log.LogInformation($"Found {guildUsers.Count} users");

            // remove the bots
            guildUsers.RemoveAll(x => x.IsBot);
            // remove all override users, if they are server
            // owners then they cannot be spooked anyways
            guildUsers.RemoveAll(x => config.OverrideUserIds.Contains(x.Id));
            // remove all users who have been spooked already
            var collection = SpookedUserCollection;
            guildUsers.RemoveAll(x =>
            {
                return collection.Exists(y => y.UserId == x.Id);
            });

            // if none left, then skip operation
            if (guildUsers.Count == 0)
            {
            //    log.LogInformation("No users left to spook");
                return null;
            }

            // get a random user
            return guildUsers[random.Next(0, guildUsers.Count)].Id;
        }

        public void ForceSpookOverride(ulong userId, string originalNick)
        {
            // remove this user from being in the queue
            SpookTaskCollection.DeleteMany(x => x.UserId == userId);
            // and from the table if they are already for some reason
            SpookedUserCollection.DeleteMany(x => x.UserId == userId);

            SpookedUserCollection.Insert(new SpookedUser()
            {
                OriginalNickname = originalNick,
                UserId = userId,
                SpookedTime = DateTime.Now,
                SpookedByUserId = null
            });
        }

        public void RegisterSpookedUser(ulong user, string name, ulong? by = null)
        {
            // check that the user hasn't already been spooked
            // if so, do nothing
            if (IsUserSpooked(user))
                return;

            SpookedUserCollection.Insert(new SpookedUser()
            {
                SpookedByUserId = by,
                SpookedTime = DateTime.Now,
                UserId = user,
                OriginalNickname = name,
            });
        }

        private string GetNameFromUser(IGuildUser user)
    => string.IsNullOrWhiteSpace(user.Nickname) ? user.Username : user.Nickname;

        private static string ReverseString(string input)
        {
            char[] charArray = input.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        private string SanitizeNickname(string name)
    => name.Replace("@", $"@{ZeroWidthSpace}");

        private string Truncate(string value, int maxLength)
    => value.Length <= maxLength ? value : value.Substring(0, maxLength);

        private const char ZeroWidthSpace = '\x200b';
        public bool DoesUserHaveSpooksRemaining(ulong userId)
           => config.OverrideUserIds.Contains(userId) || SpookTaskCollection.Count(x => x.SpookedByUserId == userId) < config.SpookUserLimit;

        private string GetRandomNicknameFormatter
            => config.NicknameFormatters[random.Next(0, config.NicknameFormatters.Count - 1)];

        public string GetRandomSpookyJoke
            => config.SpookyJokes[random.Next(0, config.SpookyJokes.Count - 1)];

        public string GetRandomJoke => config.SpookyJokes[random.Next(0, config.SpookyJokes.Count - 1)];

        public bool IsOctober => DateTime.Now.Month == 10;

        public bool IsHaloween => (IsOctober && DateTime.Now.Day == 31);

        public bool IsMidnight => (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0);

        public bool IsUserSpooked(ulong userId) => SpookedUserCollection.Exists(x => x.UserId == userId);

        public bool IsUserAlreadyQueued(ulong userId) => SpookTaskCollection.Exists(x => x.UserId == userId && !x.TaskComplete);

        public bool CanUserUseSpookyCommands(ulong userId)
            => IsUserSpooked(userId) || config.OverrideUserIds.Contains(userId) || (IsHaloween);
    }
}
