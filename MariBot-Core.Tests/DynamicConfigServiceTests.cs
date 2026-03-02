using MariBot.Core.Models.Config;
using MariBot.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;

namespace MariBot.Core.Tests
{
    public class DynamicConfigServiceTests : IDisposable
    {
        private readonly string configPath = Path.Combine(Environment.CurrentDirectory, "dynamic-config.json");
        private readonly ILogger<DynamicConfigService> logger = NullLogger<DynamicConfigService>.Instance;

        private DynamicConfigService CreateServiceWithConfig(DynamicConfig? config)
        {
            var json = config != null ? JsonConvert.SerializeObject(config) : "null";
            File.WriteAllText(configPath, json);
            return new DynamicConfigService(logger);
        }

        public void Dispose()
        {
            if (File.Exists(configPath))
                File.Delete(configPath);

            var tempPath = Path.Combine(Environment.CurrentDirectory, "dynamic-config-temp.json");
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }

        // --- GetGrokChatModel ---

        [Fact]
        public void GetGrokChatModel_NullConfig_ReturnsDefault()
        {
            var service = CreateServiceWithConfig(null);
            Assert.Equal("grok-4-1-fast-reasoning", service.GetGrokChatModel());
        }

        [Fact]
        public void GetGrokChatModel_ConfigWithoutOverride_ReturnsDefault()
        {
            var service = CreateServiceWithConfig(new DynamicConfig());
            Assert.Equal("grok-4-1-fast-reasoning", service.GetGrokChatModel());
        }

        [Fact]
        public void GetGrokChatModel_CustomValue_ReturnsCustom()
        {
            var service = CreateServiceWithConfig(new DynamicConfig { GrokChatModel = "custom-chat-model" });
            Assert.Equal("custom-chat-model", service.GetGrokChatModel());
        }

        // --- GetGrokImageModel ---

        [Fact]
        public void GetGrokImageModel_NullConfig_ReturnsDefault()
        {
            var service = CreateServiceWithConfig(null);
            Assert.Equal("grok-imagine-image", service.GetGrokImageModel());
        }

        [Fact]
        public void GetGrokImageModel_ConfigWithoutOverride_ReturnsDefault()
        {
            var service = CreateServiceWithConfig(new DynamicConfig());
            Assert.Equal("grok-imagine-image", service.GetGrokImageModel());
        }

        [Fact]
        public void GetGrokImageModel_CustomValue_ReturnsCustom()
        {
            var service = CreateServiceWithConfig(new DynamicConfig { GrokImageModel = "custom-image-model" });
            Assert.Equal("custom-image-model", service.GetGrokImageModel());
        }

        // --- GetGrokVideoModel ---

        [Fact]
        public void GetGrokVideoModel_NullConfig_ReturnsDefault()
        {
            var service = CreateServiceWithConfig(null);
            Assert.Equal("grok-imagine-video", service.GetGrokVideoModel());
        }

        [Fact]
        public void GetGrokVideoModel_ConfigWithoutOverride_ReturnsDefault()
        {
            var service = CreateServiceWithConfig(new DynamicConfig());
            Assert.Equal("grok-imagine-video", service.GetGrokVideoModel());
        }

        [Fact]
        public void GetGrokVideoModel_CustomValue_ReturnsCustom()
        {
            var service = CreateServiceWithConfig(new DynamicConfig { GrokVideoModel = "custom-video-model" });
            Assert.Equal("custom-video-model", service.GetGrokVideoModel());
        }

        // --- GetGuildConfig ---

        [Fact]
        public void GetGuildConfig_NullConfig_ReturnsNull()
        {
            var service = CreateServiceWithConfig(null);
            Assert.Null(service.GetGuildConfig(123));
        }

        [Fact]
        public void GetGuildConfig_GuildNotFound_ReturnsNull()
        {
            var config = new DynamicConfig
            {
                Guilds = new[] { new GuildConfig { Id = 999, Name = "Other" } }
            };
            var service = CreateServiceWithConfig(config);
            Assert.Null(service.GetGuildConfig(123));
        }

        [Fact]
        public void GetGuildConfig_GuildFound_ReturnsCorrectConfig()
        {
            var config = new DynamicConfig
            {
                Guilds = new[]
                {
                    new GuildConfig { Id = 111, Name = "First" },
                    new GuildConfig { Id = 222, Name = "Second" }
                }
            };
            var service = CreateServiceWithConfig(config);
            var result = service.GetGuildConfig(222);
            Assert.NotNull(result);
            Assert.Equal("Second", result.Name);
        }

        // --- CheckFeatureEnabled ---

        [Fact]
        public void CheckFeatureEnabled_NullConfig_ReturnsFalse()
        {
            var service = CreateServiceWithConfig(null);
            Assert.False(service.CheckFeatureEnabled(123, "someFeature"));
        }

        [Fact]
        public void CheckFeatureEnabled_FeatureMissing_ReturnsFalse()
        {
            var config = new DynamicConfig
            {
                Guilds = new[] { new GuildConfig { Id = 123, EnabledFeatures = new[] { "other" } } }
            };
            var service = CreateServiceWithConfig(config);
            Assert.False(service.CheckFeatureEnabled(123, "someFeature"));
        }

        [Fact]
        public void CheckFeatureEnabled_FeaturePresent_ReturnsTrue()
        {
            var config = new DynamicConfig
            {
                Guilds = new[] { new GuildConfig { Id = 123, EnabledFeatures = new[] { "someFeature" } } }
            };
            var service = CreateServiceWithConfig(config);
            Assert.True(service.CheckFeatureEnabled(123, "someFeature"));
        }

        [Fact]
        public void CheckFeatureEnabled_NullEnabledFeatures_ReturnsFalse()
        {
            var config = new DynamicConfig
            {
                Guilds = new[] { new GuildConfig { Id = 123, EnabledFeatures = null } }
            };
            var service = CreateServiceWithConfig(config);
            Assert.False(service.CheckFeatureEnabled(123, "someFeature"));
        }

        // --- IsTextCommandBlocked ---

        [Fact]
        public void IsTextCommandBlocked_NullConfig_ReturnsFalse()
        {
            var service = CreateServiceWithConfig(null);
            Assert.False(service.IsTextCommandBlocked(123, "ban"));
        }

        [Fact]
        public void IsTextCommandBlocked_Blocked_ReturnsTrue()
        {
            var config = new DynamicConfig
            {
                Guilds = new[] { new GuildConfig { Id = 123, BlockedTextCommands = new[] { "ban" } } }
            };
            var service = CreateServiceWithConfig(config);
            Assert.True(service.IsTextCommandBlocked(123, "ban"));
        }

        [Fact]
        public void IsTextCommandBlocked_NotBlocked_ReturnsFalse()
        {
            var config = new DynamicConfig
            {
                Guilds = new[] { new GuildConfig { Id = 123, BlockedTextCommands = new[] { "kick" } } }
            };
            var service = CreateServiceWithConfig(config);
            Assert.False(service.IsTextCommandBlocked(123, "ban"));
        }

        // --- IsSlashCommandBlocked ---

        [Fact]
        public void IsSlashCommandBlocked_NullConfig_ReturnsFalse()
        {
            var service = CreateServiceWithConfig(null);
            Assert.False(service.IsSlashCommandBlocked(123, "ban"));
        }

        [Fact]
        public void IsSlashCommandBlocked_Blocked_ReturnsTrue()
        {
            var config = new DynamicConfig
            {
                Guilds = new[] { new GuildConfig { Id = 123, BlockedSlashCommands = new[] { "ban" } } }
            };
            var service = CreateServiceWithConfig(config);
            Assert.True(service.IsSlashCommandBlocked(123, "ban"));
        }

        [Fact]
        public void IsSlashCommandBlocked_NotBlocked_ReturnsFalse()
        {
            var config = new DynamicConfig
            {
                Guilds = new[] { new GuildConfig { Id = 123, BlockedSlashCommands = new[] { "kick" } } }
            };
            var service = CreateServiceWithConfig(config);
            Assert.False(service.IsSlashCommandBlocked(123, "ban"));
        }

        // --- GetReactionConfig ---

        [Fact]
        public void GetReactionConfig_NullConfig_ReturnsEmpty()
        {
            var service = CreateServiceWithConfig(null);
            Assert.Empty(service.GetReactionConfig(123));
        }

        [Fact]
        public void GetReactionConfig_Configured_ReturnsCorrectReactions()
        {
            var reactions = new[]
            {
                new ReactionConfig
                {
                    TriggerWords = new[] { "hello" },
                    TriggerUsers = new ulong[] { 456 },
                    Emoji = "👋"
                }
            };
            var config = new DynamicConfig
            {
                Guilds = new[] { new GuildConfig { Id = 123, AutoReactions = reactions } }
            };
            var service = CreateServiceWithConfig(config);
            var result = service.GetReactionConfig(123);
            Assert.Single(result);
            Assert.Equal("👋", result[0].Emoji);
            Assert.Contains("hello", result[0].TriggerWords);
        }

        [Fact]
        public void GetReactionConfig_NullAutoReactions_ReturnsEmpty()
        {
            var config = new DynamicConfig
            {
                Guilds = new[] { new GuildConfig { Id = 123, AutoReactions = null } }
            };
            var service = CreateServiceWithConfig(config);
            Assert.Empty(service.GetReactionConfig(123));
        }
    }
}
