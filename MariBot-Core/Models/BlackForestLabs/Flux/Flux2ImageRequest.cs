namespace MariBot.Core.Models.BlackForestLabs.Flux
{
    /// <summary>
    /// Request body for the FLUX.2 endpoints.
    /// </summary>
    /// <remarks>
    /// A separate shape from <see cref="ImageRequest"/> rather than a shared one
    /// with extra fields, because FLUX.2 renamed the prompt-upsampling control
    /// and inverted it: FLUX 1.1 takes prompt_upsampling (off unless asked),
    /// FLUX.2 takes disable_pup (on unless refused). Sending one API the other's
    /// field name is how you end up quietly not getting the behaviour you set.
    /// </remarks>
    public class Flux2ImageRequest
    {
        public string prompt { get; set; } = string.Empty;

        public int width { get; set; }

        public int height { get; set; }

        /// <summary>
        /// Turns off automatic prompt enhancement. False — the default — leaves
        /// it on, which is what prompt_upsampling = true asked for on FLUX 1.1.
        /// </summary>
        public bool disable_pup { get; set; }

        /// <summary>Moderation strictness, 0 (strictest) to 5 (least strict).</summary>
        public int safety_tolerance { get; set; }

        public string output_format { get; set; } = "jpeg";
    }
}
