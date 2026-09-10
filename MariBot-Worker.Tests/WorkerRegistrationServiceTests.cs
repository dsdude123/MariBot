using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MariBot.Worker;
using Microsoft.Extensions.Logging;
using Xunit;

namespace MariBot.Worker.Tests
{
    /// <summary>
    /// The registration loop's behaviour when there is nothing to register with.
    /// </summary>
    public class WorkerRegistrationServiceTests
    {
        [Fact]
        public async Task AnUnconfiguredWorkerWarnsInsteadOfKillingTheHost()
        {
            var log = new RecordingLogger<WorkerRegistrationService>();
            var service = new WorkerRegistrationService(log, null!, new WorkerSettings(), null!);

            // BackgroundService hands StartAsync the task ExecuteAsync returned, so
            // anything thrown before its first await is a failed host start — which
            // is what a malformed message template here used to cause.
            await service.StartAsync(CancellationToken.None);

            var warning = Assert.Single(log.Messages);
            Assert.Contains(WorkerSettings.SectionName, warning);
        }

        /// <summary>
        /// Runs the formatter, which is the part that throws on a template whose
        /// placeholder count does not match its arguments. A logger that only
        /// stores the state — as the null logger does — would not notice.
        /// </summary>
        private sealed class RecordingLogger<T> : ILogger<T>
        {
            public List<string> Messages { get; } = new();

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                Messages.Add(formatter(state, exception));
            }

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new();
                public void Dispose() { }
            }
        }
    }
}
