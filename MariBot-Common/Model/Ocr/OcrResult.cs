using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Common.Model.Ocr
{

    public class OcrResult
    {
        public int[][] boxes { get; set; }
        public string text { get; set; }
        public float confident { get; set; }
    }

}
