using System.Diagnostics;
using MariBot.Common.Model.GpuWorker;

namespace MariBot.Worker.CommandHandlers
{
    public class StableDiffusionTextVariantHandler
    {
        public static ILogger<StableDiffusionTextVariantHandler> logger = new LoggerFactory()
            .CreateLogger<StableDiffusionTextVariantHandler>();

        public static void ExecuteStableDiffusion(string provider)
        {
            File.WriteAllText($".\\Python\\{WorkerGlobals.Job.Id}.txt", $"{WorkerGlobals.Job.SourceText}");
            
            var generator = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                WorkingDirectory = Environment.CurrentDirectory,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            generator.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    logger.LogInformation(e.Data);
                }
            });
            generator.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    logger.LogError(e.Data);
                }
            });
            generator.BeginOutputReadLine();
            generator.BeginErrorReadLine();

            using (var sw = generator.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine("C:\\ProgramData\\Anaconda3\\Scripts\\activate.bat");
                    sw.WriteLine("activate ldm");
                    sw.WriteLine($"python .\\Python\\{provider}.py \"{WorkerGlobals.Job.Id}\"");
                }
            }

            generator.WaitForExit();

            WorkerGlobals.Job.Result = new JobResult()
            {
                FileName = "result.png",
                Data = File.ReadAllBytes($".\\Python\\{WorkerGlobals.Job.Id}.png")
            };

            File.Delete($".\\Python\\{WorkerGlobals.Job.Id}.txt");
            File.Delete($".\\Python\\{WorkerGlobals.Job.Id}.png");
        }
    }
}
