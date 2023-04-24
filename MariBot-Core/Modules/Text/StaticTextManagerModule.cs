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
                var result = staticTextResponseService.AddNewResponse(key, text, Context.Guild.Id, true);
                return Context.Channel.SendMessageAsync(result);
            }
            catch (InvalidOperationException ex)
            {
                return Context.Channel.SendMessageAsync(ex.Message);
            }
        }

        [Command("add")]
        public Task add(string key, [Remainder] string text)
        {
            try
            {
               var result = staticTextResponseService.AddNewResponse(key, text, Context.Guild.Id, false);
               return Context.Channel.SendMessageAsync(result);
            }
            catch (InvalidOperationException ex)
            {
                return Context.Channel.SendMessageAsync(ex.Message);
            }
            
        }

        [RequireOwner]
        [Command("updateglobal")]
        public Task updateGlobal(string key, [Remainder] string text)
        {
            try
            { 
                var result = staticTextResponseService.UpdateResponse(key, text, Context.Guild.Id, true);
                return Context.Channel.SendMessageAsync(result);
            }
            catch (InvalidOperationException ex)
            {
                return Context.Channel.SendMessageAsync(ex.Message);
            }
        }

        [Command("update")]
        public Task update(string key, [Remainder] string text)
        {
            try
            {
                var result = staticTextResponseService.UpdateResponse(key, text, Context.Guild.Id, false);
                return Context.Channel.SendMessageAsync(result);
            }
            catch (InvalidOperationException ex)
            {
                return Context.Channel.SendMessageAsync(ex.Message);
            }
        }

        [RequireOwner]
        [Command("removeglobal")]
        public Task removeGlobal(string key)
        {
            try
            {
                var result = staticTextResponseService.RemoveResponse(key, Context.Guild.Id, true);
                return Context.Channel.SendMessageAsync(result);
            }
            catch (InvalidOperationException ex)
            {
                return Context.Channel.SendMessageAsync(ex.Message);
            }
        }

        [Command("remove")]
        public Task remove(string key)
        {
            try
            {
                var result = staticTextResponseService.RemoveResponse(key, Context.Guild.Id, false);
                return Context.Channel.SendMessageAsync(result);
            }
            catch (InvalidOperationException ex)
            {
                return Context.Channel.SendMessageAsync(ex.Message);
            }
        }

        [Command("migrateglobal")]
        [RequireOwner]
        public Task MigrateGlobal()
        {
            return Context.Channel.SendMessageAsync(staticTextResponseService.MigrateResponses(0, true));
        }

        [Command("migrate")]
        [RequireOwner]
        public Task Migrate()
        {
            return Context.Channel.SendMessageAsync(staticTextResponseService.MigrateResponses(Context.Guild.Id, false));
        }
    }
}
