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

        //TODO: Figure out how we can have optional text if we have attachments. Discord.NET built in Optional class is broken in ASP.NET. 

        [RequireOwner]
        [Command("addglobal", RunMode = RunMode.Async)]
        public async Task addGlobal(string key, [Remainder] string text = "")
        {
            try
            {
                var attachmentsList = Context.Message.Attachments != null ? Context.Message.Attachments.ToList() : null;
                var result = staticTextResponseService.AddNewResponse(key, text, Context.Guild.Id, attachmentsList, true);
                await Context.Channel.SendMessageAsync(result.Result);
            }
            catch (InvalidOperationException ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message);
            }
        }

        [Command("add", RunMode = RunMode.Async)]
        public async Task add(string key, [Remainder] string text = "")
        {
            try
            {
                var attachmentsList = Context.Message.Attachments != null ? Context.Message.Attachments.ToList() : null;
                var result = staticTextResponseService.AddNewResponse(key, text, Context.Guild.Id, attachmentsList, false);
               await Context.Channel.SendMessageAsync(result.Result);
            }
            catch (InvalidOperationException ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message);
            }
            
        }

        [RequireOwner]
        [Command("updateglobal", RunMode = RunMode.Async)]
        public async Task updateGlobal(string key, [Remainder] string text)
        {
            try
            {
                var attachmentsList = Context.Message.Attachments != null ? Context.Message.Attachments.ToList() : null;
                var result = staticTextResponseService.UpdateResponse(key, text, Context.Guild.Id, attachmentsList, true);
                await Context.Channel.SendMessageAsync(result.Result);
            }
            catch (InvalidOperationException ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message);
            }
        }

        [Command("update", RunMode = RunMode.Async)]
        public async Task update(string key, [Remainder] string text)
        {
            try
            {
                var attachmentsList = Context.Message.Attachments != null ? Context.Message.Attachments.ToList() : null;
                var result = staticTextResponseService.UpdateResponse(key, text, Context.Guild.Id, attachmentsList, false);
                await Context.Channel.SendMessageAsync(result.Result);
            }
            catch (InvalidOperationException ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message);
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

        [Command("upgrade", RunMode = RunMode.Async)]
        [RequireOwner]
        public async Task Upgrade()
        {
            await Context.Channel.SendMessageAsync(staticTextResponseService.UpgradeAttachments().Result);
        }
    }
}
