using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Discord.Commands;
using MariBot.Core.Models.Config;
using MariBot.Core.Modules.Text;
using Newtonsoft.Json;
using Xunit;

namespace MariBot.Core.Tests
{
    /// <summary>
    /// The guilds that block third-party AI block all of it.
    /// </summary>
    /// <remarks>
    /// <see cref="DynamicConfigService.IsTextCommandBlocked"/> matches the
    /// literal word typed — not the resolved command, and not its aliases — so
    /// a new command or a new alias walks straight past a blocklist that was
    /// meant to cover it. `flux1` did exactly that. This asserts the policy
    /// these two servers actually have, against the commands
    /// <see cref="AiModule"/> actually exposes, so the next one cannot be added
    /// without the blocklist being considered.
    /// <para>
    /// If a server is meant to allow one of these after all, remove it from
    /// <see cref="AiCommandsBlockedEverywhereTheyAreBlocked"/>'s expectation
    /// rather than deleting the test — the point is that the decision is
    /// deliberate.
    /// </para>
    /// </remarks>
    public class GuildAiBlocklistTests
    {
        /// <summary>Servers where every third-party AI command is blocked.</summary>
        private static readonly ulong[] GuildsBlockingAi = { 297485054836342786, 1004662899756769371 };

        /// <summary>
        /// Every command name and alias in <see cref="AiModule"/>. All of them
        /// reach OpenAI, xAI or Black Forest Labs; the Stable Diffusion and
        /// Edges2Hentai commands in <see cref="ImageModule"/> run on our own
        /// worker, so they are a different question and not covered here.
        /// </summary>
        public static IEnumerable<string> AiCommandNames()
        {
            return typeof(AiModule)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .SelectMany(m => m.GetCustomAttributes<CommandAttribute>()
                    .Select(c => c.Text)
                    .Concat(m.GetCustomAttributes<AliasAttribute>().SelectMany(a => a.Aliases)))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.ToLowerInvariant())
                .Distinct();
        }

        private static DynamicConfig ShippedConfig()
        {
            // The file the bot ships and then re-fetches from master at runtime,
            // read from the source tree rather than a test output directory —
            // DynamicConfigServiceTests writes its own copy of that filename.
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "MariBot-Core", "dynamic-config.json");
                if (File.Exists(candidate))
                {
                    return JsonConvert.DeserializeObject<DynamicConfig>(File.ReadAllText(candidate))!;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException("Could not find MariBot-Core/dynamic-config.json above " + AppContext.BaseDirectory);
        }

        [Fact]
        public void AiCommandsBlockedEverywhereTheyAreBlocked()
        {
            var config = ShippedConfig();
            var expected = AiCommandNames().ToList();

            Assert.NotEmpty(expected);

            foreach (var guildId in GuildsBlockingAi)
            {
                var guild = config.Guilds.SingleOrDefault(g => g.Id == guildId);
                Assert.True(guild != null, $"No config entry for guild {guildId}.");

                var blocked = guild!.BlockedTextCommands ?? Array.Empty<string>();
                var missing = expected.Where(c => !blocked.Contains(c)).ToList();

                Assert.True(missing.Count == 0,
                    $"Guild {guildId} blocks third-party AI but not: {string.Join(", ", missing)}.");
            }
        }
    }
}
