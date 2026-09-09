namespace MariBot.Worker
{
    /// <summary>
    /// Locations of the files the worker ships with and the scratch files it
    /// writes while running a job.
    /// </summary>
    /// <remarks>
    /// Every one of these used to be spelled out at the call site with a
    /// literal "\" separator, which is the single reason this project only ran
    /// on Windows. They are also anchored to <see cref="AppContext.BaseDirectory"/>
    /// rather than the current directory: Content and Python are copied next to
    /// the assembly at build time, so that is where they actually are no matter
    /// what directory the process happens to be started from.
    /// </remarks>
    public static class WorkerPaths
    {
        /// <summary>
        /// Directory the worker's assembly and its copied content live in.
        /// </summary>
        public static string Root => AppContext.BaseDirectory;

        /// <summary>
        /// A file in the shipped Content directory, e.g. Content/trump.png.
        /// </summary>
        public static string Content(string name, string extension)
            => Path.Combine(Root, "Content", name + extension);

        /// <summary>
        /// A file in the shipped Python directory, which doubles as the scratch
        /// area the Python handlers exchange files through.
        /// </summary>
        public static string Python(string name) => Path.Combine(Root, "Python", name);

        /// <summary>
        /// A scratch file, creating the temp directory if this is the first one.
        /// </summary>
        public static string Temp(string name)
        {
            var directory = Path.Combine(Root, "temp");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, name);
        }
    }
}
