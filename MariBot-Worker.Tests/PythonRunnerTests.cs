using System;
using System.IO;
using MariBot.Worker;
using Xunit;

namespace MariBot.Worker.Tests
{
    /// <summary>
    /// How the container image's Python environments are found and run.
    /// </summary>
    public class PythonRunnerTests : IDisposable
    {
        private readonly string root =
            Path.Combine(Path.GetTempPath(), $"maribot-venv-{Guid.NewGuid()}");

        [Fact]
        public void AnEnvironmentOfItsOwnWinsOverTheSharedOne()
        {
            if (SkipOnWindows()) return;

            Stub("ocr", "easyocr", "from the ocr environment");
            Stub(VirtualenvRunner.DefaultEnvironment, "easyocr", "from the shared environment");

            Assert.Contains("from the ocr environment", Run("ocr", "easyocr"));
        }

        [Fact]
        public void TheSharedEnvironmentIsTheFallback()
        {
            if (SkipOnWindows()) return;

            // The image ships only "default", so this is the normal case: all
            // three environment names the handlers ask for land in one venv.
            Stub(VirtualenvRunner.DefaultEnvironment, "python", "from the shared environment");

            Assert.Contains("from the shared environment", Run("ldm", "python"));
        }

        [Fact]
        public void AWorkerWithNoPythonSaysSoAndNamesTheFix()
        {
            // The CPU image's situation: OCR and image generation jobs should fail
            // with something an operator can act on rather than a Win32Exception
            // about a missing file.
            var error = Assert.Throws<FileNotFoundException>(() => Run("ocr", "easyocr"));

            Assert.Contains("CPU", error.Message);
            Assert.Contains(VirtualenvRunner.RootVariable, error.Message);
        }

        [Fact]
        public void ArgumentsArriveSeparatelyAndStderrIsCaptured()
        {
            if (SkipOnWindows()) return;

            Stub(VirtualenvRunner.DefaultEnvironment, "echoargs",
                script: "for a in \"$@\"; do echo \"[$a]\"; done\necho on-stderr >&2\n");

            // A language list and a path with a space: spliced into a command line
            // these would come out as three arguments and two.
            var output = Run("ocr", "echoargs", "ja", "en", "/tmp/a b.png");

            Assert.Contains("[ja]", output);
            Assert.Contains("[en]", output);
            Assert.Contains("[/tmp/a b.png]", output);
            // Where Python puts its tracebacks, so the handlers' error scanning
            // depends on it being in the same string.
            Assert.Contains("on-stderr", output);
        }

        private string Run(string environment, string tool, params string[] arguments) =>
            new VirtualenvRunner(root).RunTool(environment, tool, arguments);

        private void Stub(string environment, string name, string? echo = null, string? script = null)
        {
            var directory = Path.Combine(root, environment, "bin");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, name);
            File.WriteAllText(path, "#!/bin/sh\n" + (script ?? $"echo '{echo}'\n"));
            File.SetUnixFileMode(path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        /// <summary>
        /// The stubs are shell scripts. A Windows worker runs through CondaRunner,
        /// which has no venv layout at all, so there is nothing here to check.
        /// </summary>
        private static bool SkipOnWindows() => OperatingSystem.IsWindows();

        public void Dispose()
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
