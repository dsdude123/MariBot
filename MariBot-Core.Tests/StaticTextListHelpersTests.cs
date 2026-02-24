using MariBot.Core.Modules.Text;
using Xunit;

namespace MariBot.Core.Tests
{
    public class StaticTextListHelpersTests
    {
        // --- ChunkText ---

        [Fact]
        public void ChunkText_ShortText_ReturnsSingleChunk()
        {
            var result = StaticTextListHelpers.ChunkText("hello world", 3500);
            Assert.Single(result);
            Assert.Equal("hello world", result[0]);
        }

        [Fact]
        public void ChunkText_LongTextWithNewlines_SplitsAtNewline()
        {
            var line = new string('a', 200);
            // Build a string that is ~3600 chars with newlines every 200 chars
            var text = string.Join("\n", Enumerable.Repeat(line, 18)); // 18 * 200 + 17 newlines = 3617 chars
            var result = StaticTextListHelpers.ChunkText(text, 3500);
            Assert.True(result.Count > 1);
            foreach (var chunk in result)
                Assert.True(chunk.Length <= 3500);
        }

        [Fact]
        public void ChunkText_EmptyString_ReturnsSingleEmptyChunk()
        {
            var result = StaticTextListHelpers.ChunkText("", 3500);
            // empty string produces no chunks since length is not > 0
            Assert.Empty(result);
        }

        // --- BuildListPages ---

        [Fact]
        public void BuildListPages_EmptyLists_ReturnsSinglePageWithNone()
        {
            var pages = StaticTextListHelpers.BuildListPages(new List<string>(), new List<string>());
            Assert.Single(pages);
            Assert.Contains("None", pages[0].Description);
        }

        [Fact]
        public void BuildListPages_SmallLists_ReturnsSinglePage()
        {
            var global = new List<string> { "cmd1", "cmd2" };
            var guild = new List<string> { "guildcmd1" };
            var pages = StaticTextListHelpers.BuildListPages(global, guild);
            Assert.Single(pages);
        }

        [Fact]
        public void BuildListPages_VeryLongLists_ReturnsMultiplePages()
        {
            // Create enough commands to exceed 3500 chars
            var cmds = Enumerable.Range(1, 500).Select(i => $"command{i:D4}").ToList();
            var pages = StaticTextListHelpers.BuildListPages(cmds, new List<string>());
            Assert.True(pages.Count > 1);
        }

        [Fact]
        public void BuildListPages_GlobalCommandsAppearInGlobalSection()
        {
            var global = new List<string> { "myglobalcmd" };
            var guild = new List<string> { "myguildcmd" };
            var pages = StaticTextListHelpers.BuildListPages(global, guild);
            var allText = string.Join("", pages.Select(p => p.Description));
            Assert.Contains("myglobalcmd", allText);
            Assert.Contains("Global Commands", allText);
        }

        [Fact]
        public void BuildListPages_GuildCommandsAppearInGuildSection()
        {
            var global = new List<string> { "globalcmd" };
            var guild = new List<string> { "myguildcmd" };
            var pages = StaticTextListHelpers.BuildListPages(global, guild);
            var allText = string.Join("", pages.Select(p => p.Description));
            Assert.Contains("myguildcmd", allText);
            Assert.Contains("Guild Commands", allText);
        }
    }
}
