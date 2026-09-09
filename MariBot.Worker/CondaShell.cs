using System.Diagnostics;

namespace MariBot.Worker
{
    /// <summary>
    /// The Anaconda plumbing the OCR and Stable Diffusion handlers drive, by
    /// piping activate.bat and a python invocation into a cmd.exe session.
    /// </summary>
    /// <remarks>
    /// None of it has an equivalent on Linux, and it deliberately has not been
    /// given one: those commands are mapped to a GPU capability in
    /// CommandCapabilityMapping, and the container image registers as a
    /// CPU-only worker, so Core never dispatches them to it. The guard here is
    /// for the case where a worker-config.json entry claims a capability its
    /// worker does not have — an operator gets a sentence they can act on
    /// rather than a Win32Exception about a missing cmd.exe.
    /// </remarks>
    public static class CondaShell
    {
        public const string ActivateScript = "C:\\ProgramData\\Anaconda3\\Scripts\\activate.bat";

        public static void EnsureAvailable(string command)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException(
                    $"{command} runs in an Anaconda environment on a Windows worker, and this " +
                    "worker is not one. List only the CPU capability for it in worker-config.json.");
            }
        }

        /// <summary>
        /// A cmd.exe session with its three streams redirected, rooted at the
        /// worker's own directory so the .\Python\ paths in the piped commands
        /// resolve against the shipped scripts.
        /// </summary>
        public static ProcessStartInfo StartInfo() => new()
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
