namespace MariBot.Common.Model.GpuWorker
{
    /// <summary>
    /// The dispatch profile of every <see cref="Command"/>.
    /// </summary>
    /// <remarks>
    /// This is where a command declares how it should be routed. Adding a
    /// command means adding a row here; a command with no row cannot be
    /// dispatched, which <see cref="MissingProfiles"/> exists to catch in a
    /// test rather than at runtime.
    /// </remarks>
    public static class CommandRegistry
    {
        /// <summary>
        /// Applied to any command whose profile does not name a timeout of its
        /// own, unless the deployment overrides it.
        /// </summary>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

        // Stable Diffusion generates on a GPU for minutes at a time; the shared
        // default would abandon every one of them.
        private static readonly TimeSpan GenerationTimeout = TimeSpan.FromMinutes(10);

        private static readonly IReadOnlyDictionary<Command, CommandProfile> Profiles =
            new Dictionary<Command, CommandProfile>
            {
                { Command.Adidas, new(WorkerCapability.CPU) },
                { Command.AdminWalk, new(WorkerCapability.CPU) },
                { Command.AEW, new(WorkerCapability.CPU) },
                { Command.Ajit, new(WorkerCapability.CPU) },
                { Command.America, new(WorkerCapability.CPU) },
                { Command.Analysis, new(WorkerCapability.CPU) },
                { Command.Andrew, new(WorkerCapability.CPU) },
                { Command.Asuka, new(WorkerCapability.CPU) },
                { Command.Austin, new(WorkerCapability.CPU) },
                { Command.Banner, new(WorkerCapability.CPU) },
                { Command.Bernie, new(WorkerCapability.CPU) },
                { Command.Biden, new(WorkerCapability.CPU) },
                { Command.Binoculars, new(WorkerCapability.CPU) },
                { Command.Blagblare, new(WorkerCapability.CPU) },
                { Command.BobRoss, new(WorkerCapability.CPU) },
                { Command.ChangeMyMind, new(WorkerCapability.CPU) },
                { Command.Classic, new(WorkerCapability.CPU) },
                { Command.Condom, new(WorkerCapability.CPU) },

                // High priority: this is what the radar command and every
                // "your image was the wrong format" path runs, and it is a
                // re-encode measured in milliseconds. Queueing it behind a
                // deepfry is the delay this priority exists to remove.
                { Command.ConvertToDiscordFriendly, new(WorkerCapability.CPU, JobPriority.High) },

                { Command.Daryl, new(WorkerCapability.CPU) },
                { Command.Dave, new(WorkerCapability.CPU) },
                { Command.DeepFry, new(WorkerCapability.CPU) },
                { Command.DkOldies, new(WorkerCapability.CPU) },
                { Command.DsKoopa, new(WorkerCapability.CPU) },
                { Command.Edges2Hentai, new(WorkerCapability.ConsumerGPU, JobPriority.Normal, GenerationTimeout) },
                { Command.Expert, new(WorkerCapability.CPU) },
                { Command.Herschel, new(WorkerCapability.CPU) },
                { Command.JohnRiggs, new(WorkerCapability.CPU) },
                { Command.Kamala, new(WorkerCapability.CPU) },
                { Command.Kevin, new(WorkerCapability.CPU) },
                { Command.KingPortrait, new(WorkerCapability.CPU) },
                { Command.Kurisu, new(WorkerCapability.CPU) },
                { Command.NineGag, new(WorkerCapability.CPU) },
                { Command.Makoto, new(WorkerCapability.CPU) },
                { Command.Miyamoto, new(WorkerCapability.CPU) },
                { Command.Mugi, new(WorkerCapability.CPU) },
                { Command.Nuke, new(WorkerCapability.CPU) },
                { Command.Ocr, new(WorkerCapability.ConsumerGPU, JobPriority.Normal, GenerationTimeout) },
                { Command.Obama, new(WorkerCapability.CPU) },
                { Command.Pence, new(WorkerCapability.CPU) },
                { Command.Popcorn, new(WorkerCapability.CPU) },
                { Command.Queen, new(WorkerCapability.CPU) },
                { Command.RadicalReggie, new(WorkerCapability.CPU) },
                { Command.Reagan, new(WorkerCapability.CPU) },
                { Command.RGT, new(WorkerCapability.CPU) },
                { Command.Scarecrow, new(WorkerCapability.CPU) },
                { Command.SonicSays, new(WorkerCapability.CPU) },
                { Command.Spawnwave, new(WorkerCapability.CPU) },
                { Command.StableDiffusion, new(WorkerCapability.DatacenterGPU, JobPriority.Normal, GenerationTimeout) },
                { Command.StableDiffusionPokemon, new(WorkerCapability.ConsumerGPU, JobPriority.Normal, GenerationTimeout) },
                { Command.StableDiffusionWaifu, new(WorkerCapability.ConsumerGPU, JobPriority.Normal, GenerationTimeout) },
                { Command.Trump, new(WorkerCapability.CPU) },
                { Command.TransactionDenied, new(WorkerCapability.CPU) }
            };

        /// <summary>
        /// The profile for <paramref name="command"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The command has no profile, so nothing can be decided about routing
        /// it. Add a row above.
        /// </exception>
        public static CommandProfile For(Command command)
        {
            if (!Profiles.TryGetValue(command, out var profile))
            {
                throw new ArgumentOutOfRangeException(nameof(command), command,
                    "Command has no dispatch profile in CommandRegistry.");
            }

            return profile;
        }

        /// <summary>
        /// Commands declared in <see cref="Command"/> with no profile here.
        /// Empty in a healthy build.
        /// </summary>
        public static IReadOnlyCollection<Command> MissingProfiles() =>
            Enum.GetValues<Command>().Where(c => !Profiles.ContainsKey(c)).ToArray();
    }
}
