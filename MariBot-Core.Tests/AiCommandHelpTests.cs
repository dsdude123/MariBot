using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Discord.Commands;
using MariBot.Core.Modules.Text;
using Xunit;

namespace MariBot.Core.Tests
{
    /// <summary>
    /// Every AI command answers `z help &lt;command&gt;`.
    /// </summary>
    /// <remarks>
    /// The help text lives in a file per command name, looked up by the name
    /// that was typed, so a new command or a new alias silently has no help
    /// until someone remembers to add the file. Adding `flux1` was exactly that
    /// case. This is scoped to <see cref="AiModule"/> rather than every module
    /// because the older modules have gaps that are not this change's to close.
    /// </remarks>
    public class AiCommandHelpTests
    {
        public static TheoryData<string> AiCommandNames()
        {
            var data = new TheoryData<string>();

            var names = typeof(AiModule)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .SelectMany(m => m.GetCustomAttributes<CommandAttribute>()
                    .Select(c => c.Text)
                    .Concat(m.GetCustomAttributes<AliasAttribute>().SelectMany(a => a.Aliases)))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.ToLowerInvariant())
                .Distinct()
                .OrderBy(n => n);

            foreach (var name in names)
            {
                data.Add(name);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(AiCommandNames))]
        public void EveryAiCommandHasAHelpFile(string command)
        {
            // The path InfoModule.Help builds, from the same base directory.
            var path = Path.Combine(AppContext.BaseDirectory, "Resources", "Help", $"{command}.md");

            Assert.True(File.Exists(path), $"No help file for `{command}`; expected {path}.");
            Assert.False(string.IsNullOrWhiteSpace(File.ReadAllText(path)));
        }
    }
}
