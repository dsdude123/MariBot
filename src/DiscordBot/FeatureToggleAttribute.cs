using Discord.Commands;
using MariBot.Services;
using System;
using System.Threading.Tasks;

namespace MariBot
{
    class FeatureToggleAttribute : PreconditionAttribute
    {
        private string featureName;
        private FeatureToggleService featureService = new FeatureToggleService();

        public FeatureToggleAttribute(string featureName)
        {
            this.featureName = featureName;
        }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {          
            if(featureService.CheckFeature(featureName, context.Guild.Id.ToString()))
            {
                return PreconditionResult.FromSuccess();
            }

            return PreconditionResult.FromError("This feature has not been enabled.");
        }
    }
}