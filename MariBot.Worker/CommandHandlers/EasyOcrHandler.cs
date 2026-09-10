using System.Diagnostics;
using ImageMagick;
using MariBot.Common.Model.GpuWorker;
using MariBot.Common.Model.Ocr;
using MariBot.Common.Util;
using Newtonsoft.Json;

namespace MariBot.Worker.CommandHandlers
{
    public class EasyOcrHandler
    {
        private readonly ILogger<EasyOcrHandler> logger;
        private readonly IPythonRunner runner;

        private readonly string[] errorText = new string[]
            { "error", "exception", "traceback", "failed", "winerror", "not recognized", "NotFound" };

        private readonly string[] warnText = new string[] { "warn", "warning" };

        public EasyOcrHandler(ILogger<EasyOcrHandler> logger, IPythonRunner runner)
        {
            this.logger = logger;
            this.runner = runner;
        }

        public void ExecuteOcr()
        {
            ConvertAndWriteImage();

            var imagePath = WorkerPaths.Python($"{WorkerGlobals.Job.Id}.png");

            // --download_enabled=False: the recognition models are provisioned with
            // the worker rather than fetched per job, so a missing one is a
            // deployment problem to surface, not a download to wait on.
            var arguments = new List<string>
            {
                "--verbose=False",
                "--download_enabled=False",
                "--model_storage_directory", ModelStorageDirectory(),
                "--output_format", "json",
                "-f", imagePath,
                "-l"
            };

            // -l takes a list, and each language is its own argument. It used to be
            // one string only because a shell was splitting it on the way through.
            arguments.AddRange(GetLanguageCombo().Split(' ', StringSplitOptions.RemoveEmptyEntries));

            var consoleLogs = runner.RunTool("ocr", "easyocr", arguments.ToArray());

            LogAllConsoleText(consoleLogs);

            // easyocr writes its JSON to stdout, one object per line. It used to be
            // redirected to a file by the shell; there is no shell now, so the
            // console text is the result.
            string output = "```\n";
            foreach (var jsonLine in consoleLogs.Split('\n'))
            {
                try
                {
                    var result = JsonConvert.DeserializeObject<OcrResult>(jsonLine);
                    if (result?.text != null)
                    {
                        output += result.text;
                        output += '\n';
                    }
                } catch { }
            }

            output += "```";
           
            WorkerGlobals.Job.Result = new JobResult()
            {
                Message = output
            };

            File.Delete(imagePath);
        }

        /// <summary>
        /// Where easyocr's recognition models are kept. The container image points
        /// this at the mounted model cache; a Windows worker keeps using the
        /// ocr_cache directory beside the worker that already holds them.
        /// </summary>
        private static string ModelStorageDirectory()
        {
            var configured = Environment.GetEnvironmentVariable("MARIBOT_MODEL_CACHE");
            return string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(WorkerPaths.Root, "ocr_cache")
                : Path.Combine(configured, "easyocr");
        }

        public void ConvertAndWriteImage()
        {
            var inputImage = new MagickImage(WorkerGlobals.Job.SourceImage);
            inputImage.Write(WorkerPaths.Python($"{WorkerGlobals.Job.Id}.png"), MagickFormat.Png);
        }

        public string GetLanguageCombo()
        {
            if (WorkerGlobals.Job.SourceText == null)
            {
                return "en";
            }

            switch (WorkerGlobals.Job.SourceText.ToLower().Trim())
            {
                case "th":
                    return "th en";
                case "ch_tra":
                    return "ch_tra en";
                case "ch_sim":
                case "ch":
                    return "ch_sim en";
                case "ja":
                    return "ja en";
                case "ko":
                    return "ko en";
                case "ta":
                    return "ta en";
                case "te":
                    return "te en";
                case "kn":
                    return "kn tn";
                case "bn":
                case "as":
                    return "bn as en";
                case "ar":
                case "fa":
                case "ur":
                case "ug":
                    return "ar fa ur ug en";
                case "hi":
                case "mr":
                case "ne":
                    return "hi mr ne en";
                case "ru":
                case "rs":
                case "be":
                case "bg":
                case "uk":
                case "mn":
                    return "ru rs_cyrillic be bg uk mn en";
                default:
                    return "en";
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

            }
            else if (text.ContainsAny(warnText, StringComparison.OrdinalIgnoreCase))
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
