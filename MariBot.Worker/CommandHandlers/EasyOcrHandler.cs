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

        private readonly string[] errorText = new string[]
            { "error", "exception", "traceback", "failed", "winerror", "not recognized", "NotFound" };

        private readonly string[] warnText = new string[] { "warn", "warning" };

        public EasyOcrHandler(ILogger<EasyOcrHandler> logger)
        {
            this.logger = logger;
        }

        public void ExecuteOcr()
        {
            string consoleLogs = "";

            ConvertAndWriteImage();

            var easyOcr = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                WorkingDirectory = Environment.CurrentDirectory,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            easyOcr.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
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
            easyOcr.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
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
            easyOcr.BeginOutputReadLine();
            easyOcr.BeginErrorReadLine();

            using (var sw = easyOcr.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine("C:\\ProgramData\\Anaconda3\\Scripts\\activate.bat");
                    sw.WriteLine("set PYTHONIOENCODING=utf-8");
                    sw.WriteLine("activate ocr");
                    sw.WriteLine($"easyocr --verbose=False --download_enabled=False --model_storage_directory .\\ocr_cache --output_format json -f .\\Python\\{WorkerGlobals.Job.Id}.png -l {GetLanguageCombo()} > .\\Python\\{WorkerGlobals.Job.Id}.json ");
                }
            }

            easyOcr.WaitForExit();
            LogAllConsoleText(consoleLogs);

            string output = "```\n";
            foreach (var jsonLine in File.ReadAllLines($".\\Python\\{WorkerGlobals.Job.Id}.json"))
            {
                try
                {
                    var result = JsonConvert.DeserializeObject<OcrResult>(jsonLine);
                    output += result.text;
                    output += '\n';
                } catch { }
            }

            output += "```";
           
            WorkerGlobals.Job.Result = new JobResult()
            {
                Message = output
            };

            File.Delete($".\\Python\\{WorkerGlobals.Job.Id}.png");
            File.Delete($".\\Python\\{WorkerGlobals.Job.Id}.json");
        }

        public void ConvertAndWriteImage()
        {
            var inputImage = new MagickImage(WorkerGlobals.Job.SourceImage);
            inputImage.Write($".\\Python\\{WorkerGlobals.Job.Id}.png", MagickFormat.Png);
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
