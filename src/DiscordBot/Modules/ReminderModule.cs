using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using DiscordBot;
using Newtonsoft.Json;
using MariBot.Models;


namespace MariBot.Modules
{
    public class ReminderModule : ModuleBase<SocketCommandContext>
    {

        public static Timer CheckTimer = new Timer {AutoReset = true, Enabled = true, Interval = 60000};

        public ReminderModule()
        {
            CheckTimer.Elapsed += HandleTimer;
        }

        public static void HandleTimer(object source, ElapsedEventArgs arg)
        {
            if (System.IO.File.Exists(Environment.CurrentDirectory + "\\cache\\reminders.json"))
            {
                List<ReminderObject> toSend = new List<ReminderObject>();
                RemindersStore myStore = JsonConvert.DeserializeObject<RemindersStore>(
                    System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\cache\\reminders.json"));
                foreach (var v in myStore.reminders)
                {
                    if (v.alarmTime < DateTime.Now)
                    {
                        toSend.Add(v);
                    }
                }

                foreach (var v in toSend)
                {
                    IGuild server = findServer(v.server);
                    ITextChannel ch = findTextChannel(server, v.channel);
                    string output = "";
                    output += "**Reminder**\n\n";
                    output += v.message + "\n\n";
                    var eb = new EmbedBuilder();
                    eb.WithDescription(output);
                    eb.Color = Color.Orange;
                    ch.SendMessageAsync("", false, eb.Build());
                    myStore.reminders.Remove(v);
                }
                System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\cache\\reminders.json",JsonConvert.SerializeObject(myStore));
            }
        }

        static IGuild findServer(ulong id)
        {
            foreach (IGuild server in Program._client.Guilds) // discord is your DiscorClient instance
            {
                if (server.Id == id)
                    return server;
            }
            return null;
        }

        static ITextChannel findTextChannel(IGuild server, ulong id)
        {
            foreach (ITextChannel channel in server.GetTextChannelsAsync().Result)
            {
                if (channel.Id == id)
                    return channel;
            }
            return null;
        }

        [Command("reminder")]
        public Task reminder([Remainder] string message)
        {
            string[] command = message.Split('&');
            if (command.Length != 2)
            {
                return ReplyAsync("Invalid input.");
            }
            RemindersStore myStore;
            if (!System.IO.File.Exists(Environment.CurrentDirectory + "\\cache\\reminders.json"))
            {
                myStore = new RemindersStore();
                myStore.reminders = new List<ReminderObject>();
            }
            else
            {
                myStore = JsonConvert.DeserializeObject<RemindersStore>(
                    System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\cache\\reminders.json"));
            }

            ReminderObject newr = new ReminderObject();
            newr.message = command[0];
            try
            {
                DateTime t = DateTime.Parse(command[1]);
                if (t < DateTime.Now)
                {
                    return ReplyAsync("Reminder time is in the past.");
                }
                newr.alarmTime = t;
            }
            catch (Exception ex)
            {
                return ReplyAsync("Unrecognized time format.");
            }

            newr.channel = Context.Channel.Id;
            newr.server = Context.Guild.Id;
            myStore.reminders.Add(newr);
            System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\cache\\reminders.json", JsonConvert.SerializeObject(myStore));
            return ReplyAsync("Reminder has been set.");
        }
    }

}