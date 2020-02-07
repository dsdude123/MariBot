using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarBot.Models
{
    public class FapiRequest
    {
        public List<String> images;
        public Arguments args;
    }

    public class Arguments
    {
        public string text;
    }
}
