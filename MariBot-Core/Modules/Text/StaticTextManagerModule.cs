using Discord.Commands;
using MariBot.Core.Services;

namespace MariBot.Core.Modules.Text
{
    [Group("statictext")]
    public class StaticTextManagerModule : ModuleBase<SocketCommandContext>
    {

        public StaticTextResponseService staticTextResponseService { get; set; }

        public StaticTextManagerModule(StaticTextResponseService staticTextResponseService)
        {
            this.staticTextResponseService = staticTextResponseService;
        }

        //[Command("getall")]
        //public async Task getAll()
        //{
        //    var file = staticTextResponseService.getAllResponses(Context.Guild.Id);
        //    file.Seek(0, System.IO.SeekOrigin.Begin);
        //    await Context.Channel.SendFileAsync(file, "textresponse.json");
        //}

        //[Command("getallglobal")]
        //public async Task getAllGlobal()
        //{
        //    var file = StaticTextResponseService.getAllGlobalResponses();
        //    file.Seek(0, System.IO.SeekOrigin.Begin);
        //    await Context.Channel.SendFileAsync(file, "textresponse.json");
        //}

        [RequireOwner]
        [Command("addglobal")]
        public Task addGlobal(string key, [Remainder] string text)
        {
            try
            {
                staticTextResponseService.AddNewResponse(key, text, Context.Guild.Id, true);
            }
            catch (InvalidOperationException ex)
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
                staticTextResponseService.AddNewResponse(key, text, Context.Guild.Id, false);
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
                staticTextResponseService.UpdateResponse(key, text, Context.Guild.Id, true);
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
                staticTextResponseService.UpdateResponse(key, text, Context.Guild.Id, false);
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
                staticTextResponseService.RemoveResponse(key, Context.Guild.Id, true);
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
                staticTextResponseService.RemoveResponse(key, Context.Guild.Id, false);
            }
            catch (InvalidOperationException ex)
            {
                return Context.Channel.SendMessageAsync(ex.Message);
            }
            return Context.Channel.SendMessageAsync("OK");
        }

        [Command("migrateglobal")]
        public Task MigrateGlobal()
        {
            return Context.Channel.SendMessageAsync(staticTextResponseService.MigrateResponses(0, true));
        }

        [Command("migrate")]
        public Task Migrate()
        {
            return Context.Channel.SendMessageAsync(staticTextResponseService.MigrateResponses(Context.Guild.Id, false));
        }
    }
}
