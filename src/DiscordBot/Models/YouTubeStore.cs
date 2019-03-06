using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarBot.Models
{
    public class YouTubeStore
    {
        public Dictionary<string,YouTubeObject> cache;
    }

    public class YouTubeObject
    {
        public string name;
        public List<int> duration;
    }
}
