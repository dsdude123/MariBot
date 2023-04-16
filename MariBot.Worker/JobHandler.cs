using System.Reflection.Metadata;
using System.Text;
using ImageMagick;
using MariBot.Common.Model.GpuWorker;
using MariBot.Worker.CommandHandlers;
using Newtonsoft.Json;

namespace MariBot.Worker
{
    public class JobHandler
    {
        // TODO: Make this configurable
        public static Uri returnAddress = new("http://tokyo-2.nerv.jpn.com:8091");

        public static void HandleJob()
        {
            try
            {
                switch (WorkerGlobals.Job.Command)
                {
                    case Command.Adidas:
                        MagickImageHandler.OverlayImage("adidas");
                        break;
                    case Command.AdminWalk:
                        MagickImageHandler.OverlayImage("adw", 379, 113, 513, 113, 379, 245, 513, 245);
                        break;
                    case Command.AEW:
                        MagickImageHandler.OverlayImage("aew", 285, 0, 685, 14, 265, 308, 668, 330);
                        break;
                    case Command.Ajit:
                        MagickImageHandler.OverlayImage("ajit", 1, 1);
                        break;
                    case Command.America:
                        MagickImageHandler.OverlayImage("america", 1, 1, true);
                        break;
                    case Command.Analysis:
                        MagickImageHandler.AppendFooter("analysis");
                        break;
                    case Command.Andrew:
                        HandleRandomOverlay(new List<Tuple<string, int[]>>()
                        {
                            new("andrew", new []{335, 172, 951, 172, 357, 1098, 974, 1072}),
                            new("andrew2", new []{ 569, 566, 1078, 552, 577, 1034, 1061, 1039})
                        });
                        break;
                    case Command.Asuka:
                        MagickImageHandler.OverlayImage("asuka", 143, 98, 651, 101, 138, 359, 649, 363);
                        break;
                    case Command.Austin:
                        MagickImageHandler.OverlayImage("austin", 525, 366, 706, 360, 529, 475, 712, 466);
                        break;
                    case Command.Banner:
                        var bannerReadSettings = new MagickReadSettings
                        {
                            TextEncoding = Encoding.Unicode,
                            FontFamily = MagickImageHandler.GetBestFont(WorkerGlobals.Job.SourceText),
                            FontStyle = FontStyleType.Bold,
                            FontWeight = FontWeight.ExtraBold,
                            FillColor = MagickColors.White,
                            BackgroundColor = MagickColors.Black,
                            TextGravity = Gravity.Center,
                            Width = 800,
                            Height = 800
                        };

                        MagickImageHandler.AnnotateImage("banner", bannerReadSettings, MagickColors.Black, 86, 297, 412, 330, 85, 575, 413, 573);
                        break;
                    case Command.Bernie:
                        MagickImageHandler.OverlayImage("bernie", 294, 93, 638, 67, 306, 374, 649, 349);
                        break;
                    case Command.Biden:
                        HandleRandomOverlay(new List<Tuple<string, int[]>>()
                        {
                            new("biden", new []{50, 72, 341, 117, 42, 627, 339, 587}),
                            new("biden2", new []{ 66, 255, 442, 284, 75, 830, 446, 641})
                        });
                        break;
                    case Command.Binoculars:
                        MagickImageHandler.OverlayImage("binoculars", 36, 458, 769, 458, 36, 894, 769, 894);
                        break;
                    case Command.BobRoss:
                        MagickImageHandler.OverlayImage("bobross", 22, 71, 453, 88, 22, 389, 453, 403);
                        break;
                    case Command.ChangeMyMind:
                        var cmmReadSettings = new MagickReadSettings
                        {
                            TextEncoding = Encoding.Unicode,
                            FontFamily = MagickImageHandler.GetBestFont(WorkerGlobals.Job.SourceText),
                            FontStyle = FontStyleType.Bold,
                            FillColor = MagickColors.Black,
                            BackgroundColor = MagickColors.White,
                            TextGravity = Gravity.Center,
                            Width = 980,
                            Height = 624
                        };

                        MagickImageHandler.AnnotateImage("cmm", cmmReadSettings, MagickColors.White, 242, 352, 526, 236, 292, 474, 576, 358);
                        break;
                    case Command.Condom:
                        MagickImageHandler.OverlayImage("condom", 0, 381, 320, 381, 0, 702, 320, 702);
                        break;
                    case Command.ConvertToDiscordFriendly:
                        MagickImageHandler.ConvertToDiscordFriendly();
                        break;
                    case Command.Daryl:
                        HandleRandomOverlay(new List<Tuple<string, int[]>>()
                        {
                            new("daryl", new []{ 1043, 565, 1515, 492, 1040, 792, 1517, 801}),
                            new("daryl2", new []{ 1145, 233, 1941, 151, 1128, 622, 1881, 678}),
                            new("daryl3", new []{ 223, 483, 525, 502, 181, 1121, 483, 1140}),
                            new("daryl4", new []{275, 376, 343, 414, 203, 499, 271, 538}),
                            new("daryl5", new []{94, 15, 770, 0, 248, 530, 842, 437}),
                            new("daryl6", new []{822, 1347, 882, 1338, 829, 1389, 890, 1379}),
                            new("daryl7", new []{1648, 1528, 2426, 1495, 1655, 2288, 2542, 2264}),
                            new("daryl8", new []{1126, 501, 1940, 311, 1135, 801, 1914, 862}),
                            new("daryl9", new []{1098, 448, 1903, 268, 1100, 754, 1846, 809})
                        });
                        break;
                    case Command.Dave:
                        MagickImageHandler.OverlayImage("dave", 895, 258, 1234, 234, 894, 441, 1234, 447);
                        break;
                    case Command.DeepFry:
                        MagickImageHandler.DeepfryImage();
                        break;
                    case Command.DkOldies:
                        MagickImageHandler.OverlayImage("dkoldies", 190, 334, 264, 332, 193, 450, 265, 448);
                        break;
                    case Command.DsKoopa:
                        MagickImageHandler.OverlayImage("dskoopa", 60, 47, 262, 59, 126, 425, 336, 387);
                        break;
                    case Command.Edges2Hentai:
                        // TODO: Implement
                        throw new NotImplementedException();
                    case Command.Herschel:
                        MagickImageHandler.OverlayImage("herschel", 597, 215, 1279, 228, 563, 696, 1279, 748);
                        break;
                    case Command.Kevin:
                        MagickImageHandler.OverlayImage("kevin", 1119, 363, 1960, 163, 1123, 674, 1831, 749);
                        break;
                    case Command.Kurisu:
                        var kurisuSettings = new MagickReadSettings
                        {
                            TextEncoding = Encoding.Unicode,
                            FontFamily = MagickImageHandler.GetBestFont(WorkerGlobals.Job.SourceText),
                            FontStyle = FontStyleType.Bold,
                            FillColor = MagickColors.Black,
                            BackgroundColor = MagickColors.White,
                            Width = 980,
                            Height = 980
                        };

                        MagickImageHandler.AnnotateImage("kurisu", kurisuSettings, MagickColors.White, 32, 74, 241, 74, 32, 280, 241, 280);
                        break;
                    case Command.Makoto:
                        MagickImageHandler.OverlayImage("makoto", 50, 332, 258, 246, 124, 505, 311, 402);
                        break;
                    case Command.Miyamoto:
                        MagickImageHandler.OverlayImage("miyamoto", 257, 281, 689, 356, 209, 553, 643, 624);
                        break;
                    case Command.NineGag:
                        // TODO: This command has it's own implementation. Need to look into it more.
                        throw new NotImplementedException();
                    case Command.Nuke:
                        MagickImageHandler.DeepfryImage(10);
                        break;
                    case Command.Obama:
                        HandleRandomOverlay(new List<Tuple<string, int[]>>()
                        {
                            new("obama", new []{501, 86, 829, 80, 503, 270, 831, 273}),
                            new("obama2", new []{416, 54, 899, 51, 414, 313, 887, 329})
                        });
                        break;
                    case Command.Pence:
                        MagickImageHandler.OverlayImage("pence", 615, 254, 663, 261, 566, 379, 618, 389);
                        break;
                    case Command.Queen:
                        var queenSettings = new MagickReadSettings
                        {
                            TextEncoding = Encoding.Unicode,
                            FontFamily = MagickImageHandler.GetBestFont(WorkerGlobals.Job.SourceText),
                            FontStyle = FontStyleType.Bold,
                            FillColor = MagickColors.Black,
                            BackgroundColor = MagickColors.White,
                            Width = 980,
                            Height = 624
                        };

                        MagickImageHandler.AnnotateImage("queen", queenSettings, MagickColors.White, 86, 175, 408, 177, 86, 342, 404, 353);
                        break;
                    case Command.RadicalReggie:
                        HandleRandomOverlay(new List<Tuple<string, int[]>>()
                        {
                            new("radical", new []{658, 85, 1319, 85, 654, 719, 1249, 719}),
                            new("radical2", new []{0, 0, 691, 0, 0, 374, 691, 374})
                        });
                        break;
                    case Command.Reagan:
                        MagickImageHandler.OverlayImage("reagan", 46, 136, 547, 226, 100, 603, 614, 581);
                        break;
                    case Command.RGT:
                        MagickImageHandler.OverlayImage("rgt", 0, 66, 727, 26, 25, 531, 753, 490);
                        break;
                    case Command.Scarecrow:
                        HandleRandomOverlay(new List<Tuple<string, int[]>>()
                        {
                            new("scarecrow", new []{1168, 278, 1897, 37, 1159, 567, 1773, 585}),
                            new("scarecrow2",new []{1021, 408, 1957, 291, 1003, 757, 1806, 909}),
                            new("scarecrow3", new []{226, 1330, 1164, 1378, 216, 2185, 1087, 2284})
                        });
                        break;
                    case Command.SonicSays:
                        var readSettings = new MagickReadSettings
                        {
                            TextEncoding = Encoding.Unicode,
                            FontFamily = MagickImageHandler.GetBestFont(WorkerGlobals.Job.SourceText),
                            FontStyle = FontStyleType.Bold,
                            FillColor = MagickColors.White,
                            BackgroundColor = MagickColors.Black,
                            Width = 980,
                            Height = 624
                        };
                        MagickImageHandler.AnnotateImage("sonicsaystemplate", readSettings, MagickColors.Black, 41, 93, 539, 93, 41, 406, 539, 406);
                        break;
                    case Command.Spawnwave:
                        MagickImageHandler.OverlayImage("spawnwave", 0, 0, 984, 0, 0, 719, 984, 719);
                        break;
                    case Command.StableDiffusion:
                        StableDiffusionTextVariantHandler.ExecuteStableDiffusion("stablediffusion-text");
                        break;
                    case Command.StableDiffusionPokemon:
                        StableDiffusionTextVariantHandler.ExecuteStableDiffusion("pokemon");
                        break;
                    case Command.StableDiffusionWaifu:
                        StableDiffusionTextVariantHandler.ExecuteStableDiffusion("waifudiffusion");
                        break;
                    case Command.Trump:
                        HandleRandomOverlay(new List<Tuple<string, int[]>>()
                        {
                            new("trump", new []{49, 472, 651, 472, 26, 767, 667, 767}),
                            new("trump2", new []{352, 59, 732, 80, 349, 276, 738, 290}),
                            new("trump3", new []{1188, 399, 1667, 473, 1119, 1024, 1627, 1103}),
                            new("trump4", new []{1433, 234, 1755, 248, 1413, 567, 1737, 577})
                        });
                        break;
                    default:
                        throw new ArgumentException("Command type does not have a handler.");
                }
            }
            catch (Exception ex)
            {
                WorkerGlobals.Job.Result = new JobResult
                {
                    Message = ex.Message
                };
            }

            try
            {
                var http = new HttpClient();
                http.BaseAddress = returnAddress;
                var json = JsonConvert.SerializeObject(WorkerGlobals.Job);
                http.PostAsync("/job", new StringContent(json));
            }
            catch
            {
                // TODO: Log this
            }

            WorkerGlobals.WorkerStatus = WorkerStatus.Ready;

        }

        public static void HandleRandomOverlay(List<Tuple<string, int[]>> files)
        {
            var pick = new Random().Next(0, files.Count);
            var source = files[pick];
            MagickImageHandler.OverlayImage(source.Item1, source.Item2[0], source.Item2[1], source.Item2[2], source.Item2[3], source.Item2[4], source.Item2[5], source.Item2[6], source.Item2[7]);
        }
    }
}
