using System;
using System.Linq;
using MariBot.Common.Model.GpuWorker;
using Xunit;

namespace MariBot.Common.Tests
{
    public class CommandRegistryTests
    {
        [Fact]
        public void EveryCommandHasADispatchProfile()
        {
            var missing = CommandRegistry.MissingProfiles();

            // A command with no profile cannot be routed at all, and the first
            // sign of it would be a user's request throwing. Catch it here.
            Assert.Empty(missing);
        }

        [Fact]
        public void FastCommandsAreOnTheHighPriorityLane()
        {
            // The radar command and every wrong-format fixup run this, and they
            // are re-encodes measured in milliseconds.
            Assert.Equal(JobPriority.High, CommandRegistry.For(Command.ConvertToDiscordFriendly).Priority);
        }

        [Fact]
        public void MostCommandsAreNormalPriority()
        {
            var high = Enum.GetValues<Command>()
                .Where(c => CommandRegistry.For(c).Priority == JobPriority.High)
                .ToArray();

            // High priority only means something while it stays scarce.
            Assert.True(high.Length < Enum.GetValues<Command>().Length / 2,
                $"Too many commands claim high priority: {string.Join(", ", high)}");
        }

        [Fact]
        public void CapabilitiesAreUnchangedFromTheOriginalMapping()
        {
            Assert.Equal(WorkerCapability.CPU, CommandRegistry.For(Command.Adidas).Capability);
            Assert.Equal(WorkerCapability.CPU, CommandRegistry.For(Command.DeepFry).Capability);
            Assert.Equal(WorkerCapability.ConsumerGPU, CommandRegistry.For(Command.Ocr).Capability);
            Assert.Equal(WorkerCapability.ConsumerGPU, CommandRegistry.For(Command.StableDiffusionWaifu).Capability);
            Assert.Equal(WorkerCapability.DatacenterGPU, CommandRegistry.For(Command.StableDiffusion).Capability);
        }

        [Fact]
        public void GenerationCommandsGetLongerThanTheDefaultTimeout()
        {
            var generation = CommandRegistry.For(Command.StableDiffusion).Timeout;

            Assert.NotNull(generation);
            Assert.True(generation > CommandRegistry.DefaultTimeout);
        }

        [Fact]
        public void QuickCommandsUseTheDefaultTimeout()
        {
            Assert.Null(CommandRegistry.For(Command.ConvertToDiscordFriendly).Timeout);
        }
    }
}
