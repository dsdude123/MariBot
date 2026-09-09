namespace MariBot.Worker
{
    /// <summary>
    /// The fonts <see cref="CommandHandlers.MagickImageHandler.GetBestFont"/>
    /// chooses between when rendering text onto an image.
    /// </summary>
    /// <remarks>
    /// The list is ordered: Latin first, then the CJK faces that cover what it
    /// cannot. Where those files live is the one thing that differs between a
    /// Windows worker and the Linux container image, so the defaults are
    /// per-platform and configuration overrides both.
    /// </remarks>
    public static class FontCatalog
    {
        /// <summary>
        /// Configuration section holding an ordered array of font file paths,
        /// e.g. the SupportedFonts__0 environment variable. Unset — the normal
        /// case — means the platform defaults below.
        /// </summary>
        public const string ConfigurationSection = "SupportedFonts";

        private static readonly string[] WindowsDefaults =
        {
            "C:\\Windows\\Fonts\\NotoSans-Regular.ttf",
            "C:\\Windows\\Fonts\\NotoSansJP-Regular.otf",
            "C:\\Windows\\Fonts\\NotoSansKR-Regular.otf",
            "C:\\Windows\\Fonts\\NotoSansSC-Regular.otf",
            "C:\\Windows\\Fonts\\NotoSansTC-Regular.otf"
        };

        // Where Debian's fonts-noto-core and fonts-noto-cjk packages install,
        // which is what MariBot.Worker/Dockerfile puts in the image. The CJK
        // faces ship as one collection rather than the four separate files
        // Windows has.
        private static readonly string[] LinuxDefaults =
        {
            "/usr/share/fonts/truetype/noto/NotoSans-Regular.ttf",
            "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc"
        };

        public static IReadOnlyList<string> Defaults =>
            OperatingSystem.IsWindows() ? WindowsDefaults : LinuxDefaults;

        public static IReadOnlyList<string> Resolve(IConfiguration? configuration)
        {
            var configured = configuration?
                .GetSection(ConfigurationSection)
                .GetChildren()
                .Select(entry => entry.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToArray();

            return configured is { Length: > 0 } ? configured : Defaults;
        }
    }
}
