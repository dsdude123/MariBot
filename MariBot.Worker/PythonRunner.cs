using System.Diagnostics;
using System.Text;

namespace MariBot.Worker
{
    /// <summary>
    /// Runs the worker's Python scripts and tools, and hands back everything they
    /// wrote to the console.
    /// </summary>
    /// <remarks>
    /// There are two ways this happens and they have nothing in common. A Windows
    /// worker pipes an Anaconda activation and a command into cmd.exe, because
    /// that is how those environments were built. The container image has no
    /// Anaconda and no cmd.exe: it runs an interpreter out of a virtualenv
    /// directly, which also means arguments are passed as an argument list rather
    /// than spliced into a command line.
    /// </remarks>
    public interface IPythonRunner
    {
        /// <summary>Runs a script from the shipped Python directory.</summary>
        string RunScript(string environment, string script, params string[] arguments);

        /// <summary>Runs a console entry point installed in the environment, e.g. easyocr.</summary>
        string RunTool(string environment, string tool, params string[] arguments);
    }

    public static class PythonRunner
    {
        /// <summary>
        /// The runner for this machine. Windows means Anaconda; anything else means
        /// the virtualenvs the container image builds.
        /// </summary>
        public static IPythonRunner ForThisPlatform() =>
            OperatingSystem.IsWindows() ? new CondaRunner() : new VirtualenvRunner();

        /// <summary>
        /// Collects stdout and stderr into one string, interleaved as they arrive,
        /// which is what the handlers scan for error text.
        /// </summary>
        internal static string Capture(Process process, Action<StreamWriter>? writeInput = null)
        {
            var console = new StringBuilder();

            void Append(string? data)
            {
                if (string.IsNullOrEmpty(data))
                {
                    return;
                }

                lock (console)
                {
                    console.AppendLine(data);
                }
            }

            process.OutputDataReceived += (_, e) => Append(e.Data);
            process.ErrorDataReceived += (_, e) => Append(e.Data);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (writeInput != null)
            {
                using var input = process.StandardInput;
                if (input.BaseStream.CanWrite)
                {
                    writeInput(input);
                }
            }

            process.WaitForExit();

            lock (console)
            {
                return console.ToString();
            }
        }
    }
}
