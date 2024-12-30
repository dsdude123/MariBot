namespace MariBot.Core.Models.BlackForestLabs.Flux
{
    public class ImageRequest
    {
        public string prompt { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public bool prompt_upsampling { get; set; }
        public int safety_tolerance { get; set; }
        public string output_format { get; set; }
    }
}
