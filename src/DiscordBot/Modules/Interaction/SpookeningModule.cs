using Discord;
using Discord.Interactions;
using DiscordBot.Services;
using MariBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Modules.Interaction
{
    public class SpookeningModule : InteractionModuleBase<IInteractionContext>
    {
        public SpookeningService spookening { get; set; }
        public LogService log { get; set; }

        [SlashCommand("doot", "Doot.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ViewChannel | GuildPermission.SendMessages)]
        public async Task doot()
        {
            log.LogInfo("DOOT");
            if (IsOctober)
            {
                log.LogInfo("It is October.");
                if (spookening.CanUserUseSpookyCommands(Context.User.Id))
                {
                    log.LogInfo("User is spooky.");
                    if (spookening.CheckUserRateLimit(Models.ActionType.Doot, Context.User.Id))
                    {
                        log.LogInfo("User within limits.");
                        await RespondAsync("doot doot\nhttps://www.youtube.com/watch?v=eVrYbKBrI7o");
                    }
                    else
                    {
                        log.LogInfo("Command rate limited.");
                        await RespondAsync("2doot5me, please try again later");
                    }
                }
                else
                {
                    log.LogInfo("GOING TO REJECT THE DOOT");
                    await RespondAsync("You aren't spooky enough to use this command.");
                    log.LogInfo("REJECTED THE DOOT");
                }
            }
            else
            {
                await RespondAsync("nah");
            }
        }

        [SlashCommand("spoop", "Spoopy.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ViewChannel | GuildPermission.SendMessages)]
        public async Task Spoop()
        {
            log.LogInfo("SPOOP");
            if (IsOctober)
            {
                if (spookening.CanUserUseSpookyCommands(Context.User.Id))
                {
                    if (spookening.CheckUserRateLimit(Models.ActionType.Doot, Context.User.Id))
                    {

                        await RespondAsync("so spoopy\nhttps://www.youtube.com/watch?v=n_qbGJuxCYY");
                    }
                    else
                    {
                        await RespondAsync("sorry but i just need a minute, that was too much for me");
                    }
                }
                else
                {
                    await RespondAsync("You aren't spooky enough to use this command.");
                }
            }
            else
            {
                await RespondAsync("nah");
            }
        }

        [SlashCommand("thankmrskeletal", "Chooses a new (spooky) nickname.")]
        public async Task RespookMePlease()
        {
            log.LogInfo("RESPOOK");
            if (IsOctober)
            {
                if (spookening.CanUserUseSpookyCommands(Context.User.Id))
                {
                    // if the user is spooked, then allow them to respook themselves
                    if (spookening.IsUserSpooked(Context.User.Id))
                    {
                        if (spookening.CheckUserRateLimit(Models.ActionType.Reroll, Context.User.Id))
                        {
                            spookening.RespookUser(Context.User.Id);
                        }
                        else
                        {
                            await RespondAsync("Uhhhhhhhhhhhhhhhh I'm out of names. I'll think of more tomorrow.");
                        }
                    }
                }
                else
                {
                    await RespondAsync("You aren't spooky enough to use this command.");
                }
            }
            else
            {
                await RespondAsync("Nah.");
            }
        }

        [SlashCommand("spook", "Spooks a user the following night.")]
        [RequireContext(ContextType.Guild)]
        public async Task Spook(IGuildUser user)
        {
            log.LogInfo("SPOOK");
            if (IsOctober)
            {
                if (spookening.CanUserUseSpookyCommands(Context.User.Id))
                {
                    // check if the user they are spooking can be spooked
                    if (!user.IsBot && !spookening.IsUserSpooked(user.Id))
                    {
                        if (spookening.IsUserAlreadyQueued(user.Id))
                        {
                            await RespondAsync("👻 This person is already going to be spooked.");
                        }
                        else if (spookening.DoesUserHaveSpooksRemaining(Context.User.Id))
                        {
                            // spook this user
                            spookening.QueueSpooking(user.Id, Context.User.Id);
                            await RespondAsync("Beware! A spookening may happen tonight!");
                        }
                        else
                        {
                            await RespondAsync("You can't spook any more people.");
                        }
                    }
                    else
                    {
                        await RespondAsync("Uh oh! This user cannot be spooked.");
                    }
                }
                else
                {
                    await RespondAsync("You aren't spooky enough to use this command.");
                }
            }
            else
            {
                await RespondAsync("Nah.");
            }
        }

        [SlashCommand("spookyjoke", "Tells a spooky joke.")]
        [RequireContext(ContextType.Guild)]
        public async Task SpookyJoke()
        {
            log.LogInfo("JOKE");
            if (IsOctober)
            {

                if (spookening.CanUserUseSpookyCommands(Context.User.Id))
                {
                    if (spookening.CheckUserRateLimit(Models.ActionType.Joke, Context.User.Id))
                    {
                        await RespondAsync(spookening.GetRandomSpookyJoke);
                    }
                    else
                    {
                        await RespondAsync("What's the deal with airline food?\n\nnah but actually just try again in a bit.");
                    }
                }
                else
                {
                    await RespondAsync("You aren't spooky enough to use this command.");
                }
            }
            else
            {
                await RespondAsync("uhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh nahhhhhhhhhhhhhhhhhhhhhh");
            }
        }

        [SlashCommand("adminarocessspooks", "Process spooks now.", runMode: RunMode.Async)]
        [RequireOwner]
        public async Task ManuallySpookUsers()
        {
            await spookening.ProcessSpooking();
            await RespondAsync("Wow, that was spooky");
        }

        [SlashCommand("adminspooknow", "Spook immediately.")]
        [RequireOwner]
        public async Task HeyAdminSpookThisPeopleRightNow(IGuildUser user)
        {
            spookening.ForceSpookOverride(user.Id, user.Nickname);
            await RespondAsync("ugh. fine. try now.");
        }

        [SlashCommand("forcemidnight", "Whoa did you change timezones?", runMode: RunMode.Async)]
        [RequireOwner]
        public async Task ForceOnMidnight()
        {
            spookening.OnMidnight();
            await ReplyAsync("oops");
        }

        public bool IsOctober => DateTime.Now.Month == 10;
    }
}
