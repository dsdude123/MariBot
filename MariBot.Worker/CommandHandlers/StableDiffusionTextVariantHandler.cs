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
        private readonly IPythonRunner runner;

        private readonly string[] errorText = new string[]
            { "error", "exception", "traceback", "failed", "winerror", "not recognized", "NotFound" };

        private readonly string[] warnText = new string[] { "warn", "warning" };
        private readonly float moderationThreshold = 0.7f;

        public StableDiffusionTextVariantHandler(ILogger<StableDiffusionTextVariantHandler> logger,
            IConfiguration configuration, IPythonRunner runner)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.runner = runner;
        }

        public void ExecuteStableDiffusion(string provider)
        {
            var promptPath = WorkerPaths.Python($"{WorkerGlobals.Job.Id}.txt");
            var moderationPath = WorkerPaths.Python($"{WorkerGlobals.Job.Id}-moderation.json");
            var imagePath = WorkerPaths.Python($"{WorkerGlobals.Job.Id}.png");

            File.WriteAllText(promptPath, $"{WorkerGlobals.Job.SourceText}");

            try
            {
                var consoleLogs = runner.RunScript("detoxify", "moderation.py", promptPath, moderationPath);

                var moderationResult =
                    JsonConvert.DeserializeObject<ToxicityResult>(File.ReadAllText(moderationPath));

                if (moderationResult.toxicity.Any(t => t >= moderationThreshold))
                {
                    LogAllConsoleText(consoleLogs);
                    WorkerGlobals.Job.Result = new JobResult()
                    {
                        Message = "Input prompt failed safety check."
                    };

                    return;
                }

                // The token is only meaningful to the two Stable Diffusion scripts,
                // which take it as their third argument; the rest ignore it.
                consoleLogs += runner.RunScript(
                    "ldm", $"{provider}.py", promptPath, imagePath, configuration["HuggingFaceToken"] ?? string.Empty);

                LogAllConsoleText(consoleLogs);

                WorkerGlobals.Job.Result = new JobResult()
                {
                    FileName = "result.png",
                    Data = File.ReadAllBytes(imagePath)
                };
            }
            finally
            {
                // In a finally block because a job that fails partway used to leave
                // its prompt and moderation verdict behind on the worker.
                Delete(promptPath);
                Delete(moderationPath);
                Delete(imagePath);
            }
        }

        private static void Delete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
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
