namespace MariBot.Core.Models.Google.KnowledgeGraph
{
    public class Entity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string[] Type { get; set; }
        public string Description { get; set; }
        public Image Image { get; set; }
        public DetailedDescription DetailedDescription { get; set; }
        public string Url { get; set; }
    }
}
