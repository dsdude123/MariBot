using Discord.Commands;
using MariBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Modules
{
    [Group("statictext")]
    public class StaticTextManagerModule : ModuleBase<SocketCommandContext>
    {
        public StaticTextResponseService staticTextResponseService { get; set; }

        [Command("getall")]
        public async Task getAll()
        {
            var file = staticTextResponseService.getAllResponses(Context.Guild.Id);
            file.Seek(0, System.IO.SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(file, "textresponse.json");
        }

        [Command("getallglobal")]
        public async Task getAllGlobal()
        {
            var file = StaticTextResponseService.getAllGlobalResponses();
            file.Seek(0, System.IO.SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(file, "textresponse.json");
        }

        [RequireOwner]
        [Command("addglobal")]
        public Task addGlobal(string key, [Remainder] string text)
        {
            try
            {
                StaticTextResponseService.addGlobalResponse(key, text);
            } catch (InvalidOperationException ex)
            {
                return Context.Channel.SendMessageAsync(ex.Message);
            }
            return Context.Channel.SendMessageAsync("OK");
        }

        [Command("add")]
        public Task add(string key, [Remainder] string text)
        {
            try
            {
                staticTextResponseService.addResponse(Context.Guild.Id, key, text);
            }
            catch (InvalidOperationException ex)
            {
                return Context.Channel.SendMessageAsync(ex.Message);
            }
            return Context.Channel.SendMessageAsync("OK");
        }

        [RequireOwner]
        [Command("updateglobal")]
        public Task updateGlobal(string key, [Remainder] string text)
        {
            try
            {
                StaticTextResponseService.updateGlobalResponse(key, text);
            }
            catch (InvalidOperationException ex)
            {
                return Context.Channel.SendMessageAsync(ex.Message);
            }
            return Context.Channel.SendMessageAsync("OK");
        }

        [Command("update")]
        public Task update(string key, [Remainder] string text)
        {
            try
            {
                staticTextResponseService.updateResponse(Context.Guild.Id, key, text);
            }
            catch (InvalidOperationException ex)
            {
                return Context.Channel.SendMessageAsync(ex.Message);
            }
            return Context.Channel.SendMessageAsync("OK");
        }

        [RequireOwner]
        [Command("removeglobal")]
        public Task removeGlobal(string key)
        {
            try
            {
                StaticTextResponseService.removeGlobalResponse(key);
            }
            catch (InvalidOperationException ex)
            {
                return Context.Channel.SendMessageAsync(ex.Message);
            }
            return Context.Channel.SendMessageAsync("OK");
        }

        [Command("remove")]
        public Task remove(string key)
        {
            try
            {
                staticTextResponseService.removeResponse(Context.Guild.Id, key);
            }
            catch (InvalidOperationException ex)
            {
                return Context.Channel.SendMessageAsync(ex.Message);
            }
            return Context.Channel.SendMessageAsync("OK");
        }
    }
}
