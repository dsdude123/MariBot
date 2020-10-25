using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Models.FAPI._4chan
{
    public class Thread
    {
        public long id { get; set; }
        public long time { get; set; }
        [JsonProperty("last_update")]
        public long lastUpdate { get; set; }
    }
}
