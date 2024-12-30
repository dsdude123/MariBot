namespace MariBot.Core.Models.BlackForestLabs.Flux
{
    public class ImageResponse
    {
        public string id { get; set; }
        public string status { get; set; }
        public Result result { get; set; }

            public class Result
            {
                public string sample { get; set; }
                public string prompt { get; set; }
                public long seed { get; set; }
                public float start_time { get; set; }
                public float end_time { get; set; }
                public float duration { get; set; }
            }
        }
    }
