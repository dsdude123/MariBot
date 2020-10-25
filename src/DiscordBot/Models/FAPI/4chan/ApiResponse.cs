using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Models.FAPI._4chan
{
    public class ApiResponse
    {
        public Board board { get; set; }
        public Thread thread { get; set; }
        public Post[] posts { get; set; }
    }
}
