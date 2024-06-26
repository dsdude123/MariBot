﻿using OpenCvSharp;

namespace MariBot.Worker.CommandHandlers
{
    public class OpenCVHandler
    {
        public const string HaarCascade = "C:\\opencv\\build\\etc\\haarcascades\\haarcascade_frontalface_default.xml";
        public Rect[] FindFaces()
        {
            if (!Directory.Exists("temp"))
            {
                Directory.CreateDirectory("temp");
            }
            File.WriteAllBytes($"temp\\{WorkerGlobals.Job.Id}.tmp", WorkerGlobals.Job.SourceImage);
            using var haarCascade = new CascadeClassifier(HaarCascade);
            using var src = new Mat($"temp\\{WorkerGlobals.Job.Id}.tmp");
            using var gray = new Mat();

            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

            // Detect faces
            var faces = haarCascade.DetectMultiScale(
                gray, 1.08, 2, HaarDetectionTypes.ScaleImage, new Size(30, 30));

            File.Delete($"temp\\{WorkerGlobals.Job.Id}.tmp");
            return faces;
        }
    }
}
