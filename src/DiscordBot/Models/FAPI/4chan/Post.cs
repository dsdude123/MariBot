using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Models.FAPI._4chan
{
    public class Post
    {
        public long id { get; set; }
        public String user { get; set; }
        [JsonProperty("time_string")]
        public String timeAsString { get; set; }
        public long time { get; set; }
        public String text { get; set; }
        public String file { get; set; }
    }
}
