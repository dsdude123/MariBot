using System.Diagnostics;

namespace MariBot.Worker
{
    /// <summary>
    /// Runs Python out of a virtualenv, which is how the GPU container image
    /// provides what Anaconda provides on a Windows worker.
    /// </summary>
    /// <remarks>
    /// Environments are directories under <see cref="RootVariable"/>. The image
    /// ships a single "default" environment holding every dependency, so the three
    /// names the handlers ask for all land in the same place; a per-environment
    /// directory is looked for first, so a deployment that needs to keep, say,
    /// detoxify's transformers apart from diffusers' can add one without a code
    /// change.
    /// <para>
    /// No shell is involved: arguments go across as an argument list, so a prompt
    /// file path with a space or a quote in it cannot turn into another command.
    /// </para>
    /// </remarks>
    public class VirtualenvRunner : IPythonRunner
    {
        /// <summary>Where the environments live. The GPU image sets it.</summary>
        public const string RootVariable = "MARIBOT_PYTHON_ROOT";

        public const string DefaultEnvironment = "default";

        private readonly string root;

        public VirtualenvRunner(string? root = null)
        {
            this.root = root
                        ?? Environment.GetEnvironmentVariable(RootVariable)
                        ?? "/opt/maribot/venvs";
        }

        public string RunScript(string environment, string script, params string[] arguments)
        {
            var arguments_ = new List<string> { WorkerPaths.Python(script) };
            arguments_.AddRange(arguments);
            return Run(Executable(environment, "python"), arguments_);
        }

        public string RunTool(string environment, string tool, params string[] arguments)
            => Run(Executable(environment, tool), arguments);

        /// <summary>
        /// The environment's own copy of <paramref name="name"/>, falling back to
        /// the shared default environment.
        /// </summary>
        private string Executable(string environment, string name)
        {
            var specific = Path.Combine(root, environment, "bin", name);
            if (File.Exists(specific))
            {
                return specific;
            }

            var shared = Path.Combine(root, DefaultEnvironment, "bin", name);
            if (File.Exists(shared))
            {
                return shared;
            }

            // The CPU-only image is the normal way to end up here: it ships no
            // Python at all, and the commands that need it are mapped to GPU
            // capabilities it should not have claimed.
            throw new FileNotFoundException(
                $"No Python environment on this worker: looked for '{specific}' and '{shared}'. The OCR and " +
                "image generation commands need the GPU worker image. Register this worker with the CPU " +
                $"capability only, or set {RootVariable} to an environment that has them.", shared);
        }

        private static string Run(string executable, IEnumerable<string> arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = WorkerPaths.Root,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            // Unbuffered, so a job that dies mid-generation still has the output
            // that led up to it rather than an empty log.
            startInfo.Environment["PYTHONUNBUFFERED"] = "1";
            startInfo.Environment["PYTHONIOENCODING"] = "utf-8";

            var process = Process.Start(startInfo)
                          ?? throw new InvalidOperationException($"Could not start '{executable}'.");

            return PythonRunner.Capture(process);
        }
    }
}
