using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpTalk;

namespace TTSGen
{
    class Program
    {
        static void Main(string[] args)
        {
            string toSpeak = "";
            for (int i = 0; i < args.Length; i++)
            {
                toSpeak += args[i] + " ";
            }
            Console.WriteLine(toSpeak);
            FonixTalkEngine tts = new FonixTalkEngine();
            tts.SpeakToWavFile(Environment.CurrentDirectory+"\\tts.wav",toSpeak);
        }
    }
}
