using System.Diagnostics;
using MariBot.Common.Model.GpuWorker;
using MariBot.Worker.Model;
using Newtonsoft.Json;

namespace MariBot.Worker.CommandHandlers
{
    public class StableDiffusionTextVariantHandler
    {
        private readonly ILogger<StableDiffusionTextVariantHandler> logger;
        private readonly IConfiguration configuration;

        private readonly float moderationThreshold = 0.5f;

        public StableDiffusionTextVariantHandler(ILogger<StableDiffusionTextVariantHandler> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
        }

        public void ExecuteStableDiffusion(string provider)
        {
            File.WriteAllText($".\\Python\\{WorkerGlobals.Job.Id}.txt", $"{WorkerGlobals.Job.SourceText}");

            var contentModeration = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                WorkingDirectory = Environment.CurrentDirectory,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            contentModeration.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    logger.LogInformation(e.Data);
                }
            });
            contentModeration.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    logger.LogError(e.Data);
                }
            });
            contentModeration.BeginOutputReadLine();
            contentModeration.BeginErrorReadLine();

            using (var sw = contentModeration.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine("C:\\ProgramData\\Anaconda3\\Scripts\\activate.bat");
                    sw.WriteLine("activate detoxify");
                    sw.WriteLine($"python .\\Python\\moderation.py \"{WorkerGlobals.Job.Id}\"");
                }
            }

            contentModeration.WaitForExit();

            var moderationResult = JsonConvert.DeserializeObject<ToxicityResult>(File.ReadAllText($".\\Python\\{WorkerGlobals.Job.Id}-moderation.json"));

            if (moderationResult.toxicity.Any(t => t >= moderationThreshold)) {
                WorkerGlobals.Job.Result = new JobResult()
                {
                    Message = "Input prompt failed safety check."
                };
            } else
            {
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
                        sw.WriteLine($"python .\\Python\\{provider}.py \"{WorkerGlobals.Job.Id}\" \"{configuration["HuggingFaceToken"]}\"");
                    }
                }

                generator.WaitForExit();

                WorkerGlobals.Job.Result = new JobResult()
                {
                    FileName = "result.png",
                    Data = File.ReadAllBytes($".\\Python\\{WorkerGlobals.Job.Id}.png")
                };

                File.Delete($".\\Python\\{WorkerGlobals.Job.Id}.png");
            }

            File.Delete($".\\Python\\{WorkerGlobals.Job.Id}.txt");
            File.Delete($".\\Python\\{WorkerGlobals.Job.Id}-moderation.json");
        }
    }
}
