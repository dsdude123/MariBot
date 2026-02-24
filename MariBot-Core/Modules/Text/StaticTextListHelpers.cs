using Discord;

namespace MariBot.Core.Modules.Text
{
    public static class StaticTextListHelpers
    {
        private const int MaxDescriptionLength = 3500;

        public static List<Embed> BuildListPages(List<string> globalCmds, List<string> guildCmds)
        {
            var pages = new List<Embed>();

            var globalSection = globalCmds.Count > 0
                ? "**Global Commands:**\n" + string.Join(", ", globalCmds)
                : "**Global Commands:**\nNone";
            var guildSection = guildCmds.Count > 0
                ? "**Guild Commands:**\n" + string.Join(", ", guildCmds)
                : "**Guild Commands:**\nNone";

            var fullText = globalSection + "\n\n" + guildSection;
            var chunks = ChunkText(fullText, MaxDescriptionLength);
            int pageNum = 1;

            foreach (var chunk in chunks)
            {
                var eb = new EmbedBuilder()
                    .WithTitle($"Static Text Commands (Page {pageNum++} of {chunks.Count})")
                    .WithDescription(chunk)
                    .WithColor(Color.Blue);
                pages.Add(eb.Build());
            }

            return pages;
        }

        public static List<string> ChunkText(string text, int maxLength)
        {
            var chunks = new List<string>();
            while (text.Length > maxLength)
            {
                int cut = text.LastIndexOf('\n', maxLength);
                if (cut < 0) cut = maxLength;
                chunks.Add(text[..cut]);
                text = text[cut..].TrimStart('\n');
            }
            if (text.Length > 0) chunks.Add(text);
            return chunks;
        }
    }
}
