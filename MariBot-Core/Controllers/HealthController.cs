using Discord;
using MariBot_Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace MariBot_Core.Controllers
{
    [ApiController]
    [Route("status")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> logger;
        private DiscordBotService discordBotService;

        public HealthController(ILogger<HealthController> logger, DiscordBotService discordBotService)
        {
            this.logger = logger;
            this.discordBotService = discordBotService;
        }

        [HttpGet(Name = "GetDiscordConnectionStatus")]
        public string GetDiscordConnectionStatus()
        {
            return discordBotService.GetClientStatus().ToString();
        }
    }
}