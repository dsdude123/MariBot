using System.Diagnostics;
using MariBot.Common.Model.GpuWorker;
using MariBot.Common.Util;
using MariBot.Worker.Model;
using Newtonsoft.Json;

namespace MariBot.Worker.CommandHandlers
{
    public class StableDiffusionTextVariantHandler
    {
        private readonly ILogger<StableDiffusionTextVariantHandler> logger;
        private readonly IConfiguration configuration;

        private readonly string[] errorText = new string[]
            { "error", "exception", "traceback", "failed", "winerror", "not recognized", "NotFound" };

        private readonly string[] warnText = new string[] { "warn", "warning" };
        private readonly float moderationThreshold = 0.7f;

        public StableDiffusionTextVariantHandler(ILogger<StableDiffusionTextVariantHandler> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
        }

        public void ExecuteStableDiffusion(string provider)
        {
            string consoleLogs = "";

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
                    consoleLogs += e.Data;
                    if (!e.Data.EndsWith('\n'))
                    {
                        consoleLogs += '\n';
                    }
                }
            });
            contentModeration.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    consoleLogs += e.Data;
                    if (!e.Data.EndsWith('\n'))
                    {
                        consoleLogs += '\n';
                    }
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
                LogAllConsoleText(consoleLogs);
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
                        consoleLogs += e.Data;
                        if (!e.Data.EndsWith('\n'))
                        {
                            consoleLogs += '\n';
                        }
                    }
                });
                generator.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        consoleLogs += e.Data;
                        if (!e.Data.EndsWith('\n'))
                        {
                            consoleLogs += '\n';
                        }
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

                LogAllConsoleText(consoleLogs);

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

        public void LogAllConsoleText(string text)
        {
            string[] eventLogParts = text.Chunk(31839).Select(s => new string(s)).ToArray();
            if (text.ContainsAny(errorText, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var log in eventLogParts)
                {
                    logger.LogError(log);
                }

            } else if (text.ContainsAny(warnText, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var log in eventLogParts)
                {
                    logger.LogWarning(log);
                }
            }
            else
            {
                foreach (var log in eventLogParts)
                {
                    logger.LogInformation(log);
                }
            }
        }
    }
}
