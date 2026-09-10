using System.Diagnostics;

namespace MariBot.Worker
{
    /// <summary>
    /// The Anaconda plumbing a Windows worker drives, by piping activate.bat and a
    /// command into a cmd.exe session.
    /// </summary>
    /// <remarks>
    /// Unchanged in behaviour from when this was CondaShell: the same activation
    /// script, the same environments, and commands still built as text because
    /// that is what a piped shell session takes.
    /// </remarks>
    public class CondaRunner : IPythonRunner
    {
        public const string ActivateScript = "C:\\ProgramData\\Anaconda3\\Scripts\\activate.bat";

        public string RunScript(string environment, string script, params string[] arguments)
        {
            // Relative to the worker's own directory, which is where StartInfo is
            // rooted, so the shipped Python directory resolves whatever directory
            // the service was started from.
            var scriptPath = Path.Combine("Python", script);
            return Run(environment, $"python \"{scriptPath}\" {Quote(arguments)}");
        }

        public string RunTool(string environment, string tool, params string[] arguments)
            => Run(environment, $"{tool} {string.Join(' ', arguments)}");

        private string Run(string environment, string command)
        {
            EnsureAvailable();

            var process = Process.Start(StartInfo())
                          ?? throw new InvalidOperationException("Could not start cmd.exe for a Python job.");

            return PythonRunner.Capture(process, input =>
            {
                input.WriteLine(ActivateScript);
                input.WriteLine("set PYTHONIOENCODING=utf-8");
                input.WriteLine($"activate {environment}");
                input.WriteLine(command);
            });
        }

        private static string Quote(IEnumerable<string> arguments)
            => string.Join(' ', arguments.Select(argument => $"\"{argument}\""));

        private static void EnsureAvailable()
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException(
                    "The Anaconda runner only exists on Windows.");
            }

            if (!File.Exists(ActivateScript))
            {
                throw new FileNotFoundException(
                    $"Anaconda's activation script was not found at '{ActivateScript}'. The OCR and image " +
                    "generation commands run in its environments; install it, or register this worker with " +
                    "the CPU capability only.", ActivateScript);
            }
        }

        /// <summary>
        /// A cmd.exe session with its three streams redirected, rooted at the
        /// worker's own directory so relative script paths resolve against the
        /// shipped Python directory.
        /// </summary>
        private static ProcessStartInfo StartInfo() => new()
        {
            FileName = "cmd.exe",
            WorkingDirectory = WorkerPaths.Root,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
    }
}
