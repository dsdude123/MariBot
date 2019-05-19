using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace StarBot.Modules
{
    public class SonicModule : ModuleBase<SocketCommandContext>
    {
        [Command("sonicsays")]
        public Task sonicsays([Remainder] string text)
        {
            Image backing = Image.FromFile(Environment.CurrentDirectory + "\\Content\\sonicsaystemplate.png");
            Graphics canvas = Graphics.FromImage(backing);
            Rectangle r = new Rectangle(new Point(44,112),new Size(514,291) );
            StringFormat s = new StringFormat();
            s.Alignment = StringAlignment.Near;
            s.LineAlignment = StringAlignment.Center;
            canvas.DrawString(text,new Font(FontFamily.GenericSansSerif, 50),Brushes.White,r,s );
            MemoryStream outgoing = new MemoryStream();
            canvas.Save();
            backing.Save(outgoing,ImageFormat.Png);
            outgoing.Seek(0, SeekOrigin.Begin);
            return Context.Channel.SendFileAsync(outgoing, "sonicsays.png");
        }
    }
}
