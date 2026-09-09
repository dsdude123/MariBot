using MariBot.Common.Model.GpuWorker;

namespace MariBot.Core.Services
{
    /// <summary>
    /// Jobs waiting for a worker, in two strictly ordered lanes.
    /// </summary>
    /// <remarks>
    /// Strict priority, FIFO within a lane: every waiting high-priority job is
    /// offered a worker before any normal one. That is deliberate rather than
    /// weighted — the high lane holds work measured in milliseconds, so it
    /// cannot meaningfully delay the normal lane, while the reverse (a radar
    /// frame stuck behind a deepfry) is the delay worth removing.
    /// <para>
    /// Strict priority can starve the lower lane in principle. It would take a
    /// sustained arrival rate of high-priority jobs exceeding what the workers
    /// can clear, which for a Discord bot means someone deliberately flooding
    /// it; the queue depth on the metrics page is where that would show.
    /// </para>
    /// </remarks>
    public class JobQueue
    {
        private readonly LinkedList<WorkerJob> high = new();
        private readonly LinkedList<WorkerJob> normal = new();
        private readonly object gate = new();

        public int Count
        {
            get
            {
                lock (gate)
                {
                    return high.Count + normal.Count;
                }
            }
        }

        public int CountOf(JobPriority priority)
        {
            lock (gate)
            {
                return Lane(priority).Count;
            }
        }

        public void Enqueue(WorkerJob job)
        {
            lock (gate)
            {
                Lane(job.Priority).AddLast(job);
            }
        }

        /// <summary>
        /// A snapshot of everything waiting, high lane first and oldest first
        /// within each. A copy, so the dispatcher can remove jobs as it walks it.
        /// </summary>
        public IReadOnlyList<WorkerJob> PendingInOrder()
        {
            lock (gate)
            {
                var pending = new List<WorkerJob>(high.Count + normal.Count);
                pending.AddRange(high);
                pending.AddRange(normal);
                return pending;
            }
        }

        public bool Remove(WorkerJob job)
        {
            lock (gate)
            {
                return Lane(job.Priority).Remove(job);
            }
        }

        private LinkedList<WorkerJob> Lane(JobPriority priority) =>
            priority == JobPriority.High ? high : normal;
    }
}
