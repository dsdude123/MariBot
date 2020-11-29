using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Models.Google.KnowledgeGraph
{
    public class EntitySearchResult
    {
        public Entity Result { get; set; }
        public float ResultScore { get; set; }
    }
}
