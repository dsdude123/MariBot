using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarBot.Models
{
    public class WebcamStore
    {
        public List<Webcam> cameras;
    }

    public class Webcam
    {
        public String command;
        public String description;
        public String url;
    }


}
