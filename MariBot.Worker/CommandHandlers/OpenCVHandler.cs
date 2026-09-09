using OpenCvSharp;

namespace MariBot.Worker.CommandHandlers
{
    public class OpenCVHandler
    {
        /// <summary>
        /// Configuration key overriding where the frontal-face cascade lives,
        /// e.g. the HaarCascadePath environment variable.
        /// </summary>
        public const string HaarCascadeConfigurationKey = "HaarCascadePath";

        // Both defaults are just where the respective OpenCV distribution drops
        // its cascade XML: a manual C:\opencv extraction on the Windows worker,
        // Debian's opencv-data package in the container image.
        private const string WindowsHaarCascade =
            "C:\\opencv\\build\\etc\\haarcascades\\haarcascade_frontalface_default.xml";
        private const string LinuxHaarCascade =
            "/usr/share/opencv4/haarcascades/haarcascade_frontalface_default.xml";

        public static string DefaultHaarCascade =>
            OperatingSystem.IsWindows() ? WindowsHaarCascade : LinuxHaarCascade;

        private readonly string haarCascade;

        /// <remarks>
        /// <paramref name="configuration"/> is optional so tests can build a
        /// handler without standing up a configuration root; the container
        /// resolves the one-argument form.
        /// </remarks>
        public OpenCVHandler(IConfiguration? configuration = null)
        {
            var configured = configuration?[HaarCascadeConfigurationKey];
            haarCascade = string.IsNullOrWhiteSpace(configured) ? DefaultHaarCascade : configured;
        }

        public Rect[] FindFaces()
        {
            // Checked before CascadeClassifier gets a chance to fail on it:
            // when its constructor throws, the finalizer follows it into the
            // native layer and the unhandled exception there kills the worker
            // rather than the job.
            if (!File.Exists(haarCascade))
            {
                throw new FileNotFoundException(
                    $"Haar cascade not found at '{haarCascade}'. Install the OpenCV data files or " +
                    $"point {HaarCascadeConfigurationKey} at them.", haarCascade);
            }

            var scratchFile = WorkerPaths.Temp($"{WorkerGlobals.Job.Id}.tmp");
            File.WriteAllBytes(scratchFile, WorkerGlobals.Job.SourceImage);
            using var haarCascadeClassifier = new CascadeClassifier(haarCascade);
            using var src = new Mat(scratchFile);
            using var gray = new Mat();

            Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

            // Detect faces
            var faces = haarCascadeClassifier.DetectMultiScale(
                gray, 1.08, 2, HaarDetectionTypes.ScaleImage, new Size(30, 30));

            File.Delete(scratchFile);
            return faces;
        }
    }
}
