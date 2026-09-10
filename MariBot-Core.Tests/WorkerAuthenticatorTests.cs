using MariBot.Core.Models.Config;
using MariBot.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MariBot.Core.Tests
{
    public class WorkerAuthenticatorTests
    {
        private static WorkerAuthenticator Authenticator(string? key) =>
            new(NullLogger<WorkerAuthenticator>.Instance, new WorkerSettings { PreSharedKey = key });

        private static HttpRequest RequestWith(string? key)
        {
            var context = new DefaultHttpContext();
            if (key != null)
            {
                context.Request.Headers[WorkerAuthenticator.HeaderName] = key;
            }

            return context.Request;
        }

        [Fact]
        public void AcceptsTheConfiguredKey()
        {
            Assert.True(Authenticator("hunter2").IsAuthorised(RequestWith("hunter2")));
        }

        [Fact]
        public void RejectsAWrongKey()
        {
            Assert.False(Authenticator("hunter2").IsAuthorised(RequestWith("hunter3")));
        }

        [Fact]
        public void RejectsAKeyOfADifferentLength()
        {
            Assert.False(Authenticator("hunter2").IsAuthorised(RequestWith("hunter2longer")));
            Assert.False(Authenticator("hunter2").IsAuthorised(RequestWith("hunt")));
        }

        [Fact]
        public void RejectsARequestWithNoKeyAtAll()
        {
            Assert.False(Authenticator("hunter2").IsAuthorised(RequestWith(null)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("SET-ME")]
        public void FailsClosedWhenNoKeyIsConfigured(string? configured)
        {
            var authenticator = Authenticator(configured);

            // An unconfigured deployment registers nobody, rather than
            // registering everybody.
            Assert.False(authenticator.IsConfigured);
            Assert.False(authenticator.IsAuthorised(RequestWith("anything")));
            Assert.False(authenticator.IsAuthorised(RequestWith(configured)));
        }
    }
}
