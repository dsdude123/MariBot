namespace MariBot.Worker.Tests
{
    // Static assembly-wide holder to initialize TestOutputsFixture once per test assembly load.
    public static class AssemblyTestOutputs
    {
        public static readonly TestOutputsFixture Instance = new TestOutputsFixture();
    }
}

