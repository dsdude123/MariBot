using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Models.FAPI.DuckDuckGo
{
    public class ApiResponse
    {
        public Result[] results { get; set; }
        public String[] images { get; set; }
    }
}
