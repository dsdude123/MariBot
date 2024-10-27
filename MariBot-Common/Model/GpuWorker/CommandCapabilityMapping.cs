using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Common.Model.GpuWorker
{
    public class CommandCapabilityMapping
    {
        public static Dictionary<Command, WorkerCapability> NeededCapabilityDictionary =
            new()
            {
                { Command.Adidas, WorkerCapability.CPU},
                { Command.AdminWalk, WorkerCapability.CPU},
                { Command.AEW, WorkerCapability.CPU},
                { Command.Ajit, WorkerCapability.CPU},
                { Command.America, WorkerCapability.CPU},
                { Command.Analysis, WorkerCapability.CPU},
                { Command.Andrew, WorkerCapability.CPU},
                { Command.Asuka, WorkerCapability.CPU},
                { Command.Austin, WorkerCapability.CPU},
                { Command.Banner, WorkerCapability.CPU},
                { Command.Bernie, WorkerCapability.CPU},
                { Command.Biden, WorkerCapability.CPU},
                { Command.Binoculars, WorkerCapability.CPU},
                { Command.BobRoss, WorkerCapability.CPU},
                { Command.ChangeMyMind, WorkerCapability.CPU},
                { Command.Condom, WorkerCapability.CPU},
                { Command.ConvertToDiscordFriendly, WorkerCapability.CPU},
                { Command.Daryl, WorkerCapability.CPU},
                { Command.Dave, WorkerCapability.CPU},
                { Command.DeepFry, WorkerCapability.CPU},
                { Command.DkOldies, WorkerCapability.CPU},
                { Command.DsKoopa, WorkerCapability.CPU},
                { Command.Edges2Hentai, WorkerCapability.ConsumerGPU},
                { Command.Expert, WorkerCapability.CPU },
                { Command.Herschel, WorkerCapability.CPU},
                { Command.JohnRiggs, WorkerCapability.CPU},
                { Command.Kamala, WorkerCapability.CPU},
                { Command.Kevin, WorkerCapability.CPU},
                { Command.KingPortrait, WorkerCapability.CPU},
                { Command.Kurisu, WorkerCapability.CPU},
                { Command.NineGag, WorkerCapability.CPU},
                { Command.Makoto, WorkerCapability.CPU},
                { Command.Miyamoto, WorkerCapability.CPU},
                { Command.Mugi, WorkerCapability.CPU},
                { Command.Nuke, WorkerCapability.CPU},
                { Command.Ocr, WorkerCapability.ConsumerGPU},
                { Command.Obama, WorkerCapability.CPU},
                { Command.Pence, WorkerCapability.CPU},
                { Command.Popcorn, WorkerCapability.CPU},
                { Command.Queen, WorkerCapability.CPU},
                { Command.RadicalReggie, WorkerCapability.CPU},
                { Command.Reagan, WorkerCapability.CPU},
                { Command.RGT, WorkerCapability.CPU},
                { Command.Scarecrow, WorkerCapability.CPU},
                { Command.SonicSays, WorkerCapability.CPU},
                { Command.Spawnwave, WorkerCapability.CPU},
                { Command.StableDiffusion, WorkerCapability.DatacenterGPU},
                { Command.StableDiffusionPokemon, WorkerCapability.ConsumerGPU},
                { Command.StableDiffusionWaifu, WorkerCapability.ConsumerGPU},
                { Command.Trump, WorkerCapability.CPU},
                { Command.TransactionDenied, WorkerCapability.CPU }
            };
    }
}
