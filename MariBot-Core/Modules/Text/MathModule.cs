using Discord.Commands;
using MariBot.Core.Services;

namespace MariBot.Core.Modules.Text
{
    /// <summary>
    /// Collection of commands relating to math
    /// </summary>
    public class MathModule : ModuleBase<SocketCommandContext>
    {
        private readonly ImageService imageService;

        public MathModule(ImageService imageService)
        {
            this.imageService = imageService;
        }

        [Command("latex")]
        public async Task Latex([Remainder] string latex)
        {
            var image = imageService.GenerateLatexImage(latex);
            image.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(image, "latex.png");
        }

        [Command("solve")]
        public Task Solve([Remainder] string equation)
        {
            var expression = new org.mariuszgromada.math.mxparser.Expression(equation);
            return Context.Channel.SendMessageAsync(expression.calculate().ToString());
        }
    }
}
