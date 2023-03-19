using Discord;
using Discord.Commands;
using MariBot.Core.Services;

namespace MariBot.Core.Modules.Text
{
    /// <summary>
    /// Module for commands related to looking up information on 
    /// services such as Google, Wikipedia, etc.
    /// 
    /// </summary>
    public class LookupModule : ModuleBase<SocketCommandContext>
    {
        private PricechartingService pricechartingService { get; set; }

        public LookupModule(PricechartingService pricechartingService)
        {
            this.pricechartingService = pricechartingService;
        }

        [Command("pricecharting", RunMode = RunMode.Async)]
        public async Task Pricecharting([Remainder]string game)
        {
            var result = await pricechartingService.SearchPricechartingDataAsTable(game);
            await ReplyAsync($"```\n{result}\n```", messageReference: new MessageReference(Context.Message.Id));
        }
    }
}
