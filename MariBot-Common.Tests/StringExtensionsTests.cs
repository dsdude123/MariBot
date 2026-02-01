using System;
using MariBot.Common.Util;
using Xunit;

namespace MariBot.Common.Tests
{
    public class StringExtensionsTests
    {
        [Fact]
        public void ContainsReturnsTrueWhenSubstringPresentOrdinal()
        {
            var source = "Hello World";

            var result = source.Contains("World", StringComparison.Ordinal);

            Assert.True(result);
        }

        [Fact]
        public void ContainsReturnsFalseWhenSubstringNotPresent()
        {
            var source = "Hello World";

            var result = source.Contains("planet", StringComparison.OrdinalIgnoreCase);

            Assert.False(result);
        }

        [Fact]
        public void ContainsReturnsFalseWhenSourceIsNull()
        {
            string source = null;

            var result = StringExtensions.Contains(source, "any", StringComparison.Ordinal);

            Assert.False(result);
        }

        [Fact]
        public void ContainsThrowsWhenToCheckIsNull()
        {
            var source = "Hello";

            Assert.Throws<ArgumentNullException>(() => source.Contains(null, StringComparison.Ordinal));
        }

        [Fact]
        public void ContainsAnyReturnsTrueWhenAnyElementMatchesCaseInsensitive()
        {
            var source = "The quick brown fox";
            var candidates = new[] { "cat", "FOX", "dog" };

            var result = source.ContainsAny(candidates, StringComparison.OrdinalIgnoreCase);

            Assert.True(result);
        }

        [Fact]
        public void ContainsAnyReturnsFalseWhenArrayEmpty()
        {
            var source = "abc";
            var candidates = Array.Empty<string>();

            var result = source.ContainsAny(candidates, StringComparison.Ordinal);

            Assert.False(result);
        }

        [Fact]
        public void ContainsAnyReturnsFalseWhenSourceIsNull()
        {
            string source = null;
            var candidates = new[] { "a", "b" };

            var result = source.ContainsAny(candidates, StringComparison.Ordinal);

            Assert.False(result);
        }

        [Fact]
        public void ContainsAnyThrowsWhenCandidatesContainNull()
        {
            var source = "hello";
            var candidates = new string[] { null, "h" };

            Assert.Throws<ArgumentNullException>(() => source.ContainsAny(candidates, StringComparison.Ordinal));
        }
    }
}
