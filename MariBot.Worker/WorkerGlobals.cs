using MariBot.Common.Model.GpuWorker;

namespace MariBot.Worker
{
    /// <summary>
    /// The one job this worker is running, and whether it is free to take another.
    /// </summary>
    /// <remarks>
    /// <see cref="Job"/> is ambient rather than a plain static, which matters
    /// once a job can be abandoned: the thread running an abandoned job keeps
    /// going — ImageMagick and OpenCV calls cannot be cancelled — and a plain
    /// static would have it write its eventual result over whatever job the
    /// worker picked up in the meantime. An <see cref="AsyncLocal{T}"/> gives
    /// that thread its own view, so it can only ever scribble on the job object
    /// it was handed, which by then nobody reads.
    /// </remarks>
    public class WorkerGlobals
    {
        private static readonly AsyncLocal<WorkerJob?> CurrentJob = new();
        private static readonly object Gate = new();
        private static Guid acceptedJobId;

        public static WorkerStatus WorkerStatus { get; set; }

        /// <summary>
        /// The job being processed on this logical call context. Handlers read
        /// their input and write their result through here.
        /// </summary>
        public static WorkerJob Job
        {
            get => CurrentJob.Value!;
            set => CurrentJob.Value = value;
        }

        /// <summary>The job this worker has told Core it is running, if any.</summary>
        public static Guid AcceptedJobId
        {
            get
            {
                lock (Gate)
                {
                    return acceptedJobId;
                }
            }
        }

        /// <summary>
        /// Claims the worker for a job.
        /// </summary>
        /// <returns>False if it is already busy or held, in which case the caller must refuse the job.</returns>
        public static bool TryAccept(WorkerJob job)
        {
            lock (Gate)
            {
                if (WorkerStatus != WorkerStatus.Ready)
                {
                    return false;
                }

                WorkerStatus = WorkerStatus.Working;
                acceptedJobId = job.Id;
                return true;
            }
        }

        /// <summary>
        /// Whether <paramref name="jobId"/> is still the job this worker is
        /// running. False once it has been abandoned or superseded, which is the
        /// signal to throw its result away rather than post it.
        /// </summary>
        public static bool StillOwns(Guid jobId)
        {
            lock (Gate)
            {
                return acceptedJobId == jobId;
            }
        }

        /// <summary>
        /// Frees the worker after finishing <paramref name="jobId"/>.
        /// </summary>
        /// <returns>False if the worker had already moved on, so the caller should stay quiet.</returns>
        public static bool Release(Guid jobId)
        {
            lock (Gate)
            {
                if (acceptedJobId != jobId)
                {
                    return false;
                }

                acceptedJobId = Guid.Empty;
                WorkerStatus = WorkerStatus.Ready;
                return true;
            }
        }

        /// <summary>
        /// Drops a job at Core's request, freeing the worker immediately.
        /// </summary>
        /// <remarks>
        /// The work itself carries on to completion in the background — there is
        /// no way to stop it — but the worker stops being unavailable, which is
        /// the whole point. Its result is discarded when it finishes and finds
        /// it no longer owns the slot.
        /// </remarks>
        /// <returns>False if that job was not the one running.</returns>
        public static bool Abandon(Guid jobId) => Release(jobId);
    }
}
