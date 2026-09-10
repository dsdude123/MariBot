using System;
using System.Linq;
using MariBot.Common.Model.GpuWorker;
using MariBot.Core.Services;
using Xunit;

namespace MariBot.Core.Tests
{
    public class JobQueueTests
    {
        private static WorkerJob Job(Command command, JobPriority priority) => new()
        {
            Id = Guid.NewGuid(),
            Command = command,
            Priority = priority
        };

        [Fact]
        public void HighPriorityJobsAreOfferedBeforeNormalOnesAlreadyWaiting()
        {
            var queue = new JobQueue();
            var slowFirst = Job(Command.DeepFry, JobPriority.Normal);
            var slowSecond = Job(Command.Trump, JobPriority.Normal);
            var quick = Job(Command.ConvertToDiscordFriendly, JobPriority.High);

            queue.Enqueue(slowFirst);
            queue.Enqueue(slowSecond);
            queue.Enqueue(quick);

            // The radar frame jumps the two deepfries, which is the delay this
            // queue exists to remove.
            Assert.Equal(new[] { quick.Id, slowFirst.Id, slowSecond.Id },
                queue.PendingInOrder().Select(j => j.Id).ToArray());
        }

        [Fact]
        public void OrderIsFirstInFirstOutWithinALane()
        {
            var queue = new JobQueue();
            var first = Job(Command.ConvertToDiscordFriendly, JobPriority.High);
            var second = Job(Command.ConvertToDiscordFriendly, JobPriority.High);
            var third = Job(Command.ConvertToDiscordFriendly, JobPriority.High);

            queue.Enqueue(first);
            queue.Enqueue(second);
            queue.Enqueue(third);

            Assert.Equal(new[] { first.Id, second.Id, third.Id },
                queue.PendingInOrder().Select(j => j.Id).ToArray());
        }

        [Fact]
        public void RemovingTakesTheJobOutOfItsOwnLane()
        {
            var queue = new JobQueue();
            var quick = Job(Command.ConvertToDiscordFriendly, JobPriority.High);
            var slow = Job(Command.DeepFry, JobPriority.Normal);
            queue.Enqueue(quick);
            queue.Enqueue(slow);

            Assert.True(queue.Remove(quick));

            Assert.Equal(1, queue.Count);
            Assert.Equal(0, queue.CountOf(JobPriority.High));
            Assert.Equal(new[] { slow.Id }, queue.PendingInOrder().Select(j => j.Id).ToArray());
        }

        [Fact]
        public void RemovingAJobThatIsNotQueuedReportsFalse()
        {
            var queue = new JobQueue();

            Assert.False(queue.Remove(Job(Command.DeepFry, JobPriority.Normal)));
        }

        [Fact]
        public void CountsAreTrackedPerLane()
        {
            var queue = new JobQueue();
            queue.Enqueue(Job(Command.ConvertToDiscordFriendly, JobPriority.High));
            queue.Enqueue(Job(Command.DeepFry, JobPriority.Normal));
            queue.Enqueue(Job(Command.Trump, JobPriority.Normal));

            Assert.Equal(3, queue.Count);
            Assert.Equal(1, queue.CountOf(JobPriority.High));
            Assert.Equal(2, queue.CountOf(JobPriority.Normal));
        }

        [Fact]
        public void PendingSnapshotCanBeWalkedWhileTheQueueIsChanged()
        {
            var queue = new JobQueue();
            var jobs = Enumerable.Range(0, 3).Select(_ => Job(Command.DeepFry, JobPriority.Normal)).ToArray();
            foreach (var job in jobs)
            {
                queue.Enqueue(job);
            }

            // This is exactly what the dispatcher does: walk the snapshot and
            // remove each job as it is placed.
            foreach (var job in queue.PendingInOrder())
            {
                queue.Remove(job);
            }

            Assert.Equal(0, queue.Count);
        }
    }
}
