using ByteSizeLib;
using Discord;
using Discord.Commands;
using ImageMagick;
using MariBot.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MariBot.Modules
{
    [Group("server")]
    public class ServerModule : ModuleBase<SocketCommandContext>
    {
        [Command("health", RunMode = RunMode.Async)]
        public async Task health()
        {
            PerformanceCounter cpuCounter;
            PerformanceCounter ramCounter;

            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            cpuCounter.NextValue();
            Thread.Sleep(1000);

            string description =
                "**CPU:** " + (int)cpuCounter.NextValue() + "%\n" +
                "**Memory:** " + ramCounter.NextValue() + "MB free\n\n**Drive Information:**\n";

            foreach(DriveInfo drive in DriveInfo.GetDrives())
            {
                if(drive.IsReady)
                {
                    description += drive.Name + "  " + (int)ByteSize.FromBytes(drive.TotalFreeSpace).GigaBytes 
                        + "GB free /" + (int)ByteSize.FromBytes(drive.TotalSize).GigaBytes + "GB total\n";
                }
            }

            var eb = new EmbedBuilder();
            eb.WithTitle("System Status");
            eb.WithAuthor(Environment.MachineName.ToString());
            eb.WithTimestamp(DateTimeOffset.Now);
            eb.WithColor(Color.Green);
            eb.WithDescription(description);
            Context.Channel.SendMessageAsync(null, false, eb.Build());
        }

        [Command("ping", RunMode = RunMode.Async)]
        public async Task ping()
        {          
            Context.Channel.SendMessageAsync("Pong! " + Context.Client.Latency + "ms");
        }

        [Command("opencl_status", RunMode = RunMode.Async)]
        public async Task OpenCLStatus()
        {
            string report = $"```\nOpenCL Enabled: {OpenCL.IsEnabled}\n";

            foreach (OpenCLDevice device in OpenCL.Devices)
            {
                report += $"{device.DeviceType}: {device.Name} | Enabled: {device.IsEnabled} | Score: {device.BenchmarkScore}\n";
            }

            report += "```";
            Context.Channel.SendMessageAsync(report);
        }
    }
}
