using System;
using System.IO;

namespace MariBot.Worker.Tests
{
    // Collection fixture to run once per test collection. It performs a one-time cleanup of TestOutputs
    // and ensures directories exist for the test run. xUnit will instantiate this once per collection.
    public class TestOutputsFixture : IDisposable
    {
        public string TestOutputsDir { get; }
        public string BaseDir { get; }

        public TestOutputsFixture()
        {
            BaseDir = AppContext.BaseDirectory;
            TestOutputsDir = Path.Combine(BaseDir, "TestOutputs");

            try
            {
                if (Directory.Exists(TestOutputsDir))
                {
                    // Clear read-only attributes on files and directories so deletion succeeds
                    foreach (var file in Directory.GetFiles(TestOutputsDir, "*", SearchOption.AllDirectories))
                    {
                        try { File.SetAttributes(file, FileAttributes.Normal); } catch { }
                    }
                    foreach (var dir in Directory.GetDirectories(TestOutputsDir, "*", SearchOption.AllDirectories))
                    {
                        try { var attr = File.GetAttributes(dir); if ((attr & FileAttributes.ReadOnly) != 0) File.SetAttributes(dir, attr & ~FileAttributes.ReadOnly); } catch { }
                    }
                    Directory.Delete(TestOutputsDir, true);
                }
            }
            catch (Exception ex)
            {
                // If deletion fails, write to console as fixture cannot use ITestOutputHelper
                try { Console.WriteLine($"Warning: failed to delete existing TestOutputs directory '{TestOutputsDir}': {ex.Message}"); } catch { }
            }

            // Create directories used by tests
            try
            {
                Directory.CreateDirectory(Path.Combine(BaseDir, "temp"));
                Directory.CreateDirectory(TestOutputsDir);
            }
            catch (Exception ex)
            {
                try { Console.WriteLine($"Warning: failed to create test directories: {ex.Message}"); } catch { }
            }
        }

        public void Dispose()
        {
            // No-op, outputs should be manually inspected after test run
        }
    }
}
