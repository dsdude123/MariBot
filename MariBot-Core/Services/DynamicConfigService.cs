using MariBot.Core.Models.Config;
using Newtonsoft.Json;
using System.Net;
using System.Timers;
using Timer = System.Timers.Timer;

namespace MariBot.Core.Services
{
    /// <summary>
    /// Configuration that is updated on a periodic basis from the MariBot GitHub repo.
    /// </summary>
    public class DynamicConfigService
    {
        private DynamicConfig dynamicConfig;
        private ILogger<DynamicConfigService> logger;

        private readonly string configPath = Environment.CurrentDirectory + "\\dynamic-config.json";

        public DynamicConfigService(ILogger<DynamicConfigService> logger)
        {
            this.logger = logger;
            LoadOrGetConfig();

            Timer dynamicConfigUpdate = new Timer(3300000);
            dynamicConfigUpdate.Elapsed += UpdateConfig;
            dynamicConfigUpdate.AutoReset = true;
            dynamicConfigUpdate.Enabled = true;
        }

        /// <summary>
        /// Checks if a feature is enabled for a guild.
        /// </summary>
        /// <param name="id">Guild ID</param>
        /// <param name="feature">Feature to check if enabled.</param>
        /// <returns>True if enabled.</returns>
        public bool CheckFeatureEnabled(ulong id, string feature)
        {
            var guildConfig = GetGuildConfig(id);
            
            if (guildConfig != null && guildConfig.EnabledFeatures != null)
            {
                return guildConfig.EnabledFeatures.Contains(feature);
            }
            return false;

        }

        /// <summary>
        /// Check if a text command is blocked for a guild.
        /// </summary>
        /// <param name="id">Guild ID</param>
        /// <param name="command">Command name</param>
        /// <returns>True if command is blocked.</returns>
        public bool IsTextCommandBlocked(ulong id, string command)
        {
            var guildConfig = GetGuildConfig(id);

            if (guildConfig != null && guildConfig.BlockedTextCommands != null)
            {
                return guildConfig.BlockedTextCommands.Contains(command);
            }
            return false;
        }

        /// <summary>
        /// Check if a slash command is blocked for a guild.
        /// </summary>
        /// <param name="id">Guild ID</param>
        /// <param name="command">Command name</param>
        /// <returns>True if command is blocked.</returns>
        public bool IsSlashCommandBlocked(ulong id, string command)
        {
            var guildConfig = GetGuildConfig(id);

            if (guildConfig != null && guildConfig.BlockedSlashCommands != null)
            {
                return guildConfig.BlockedSlashCommands.Contains(command);
            }
            return false;
        }

        /// <summary>
        /// Get the auto reaction config for a guild.
        /// </summary>
        /// <param name="id">Guild ID</param>
        /// <returns>Auto reaction config array or empty array</returns>
        public ReactionConfig[] GetReactionConfig(ulong id)
        {
            var guildConfig = GetGuildConfig(id);

            if (guildConfig != null && guildConfig.AutoReactions != null)
            {
                return guildConfig.AutoReactions;
            }
            return Array.Empty<ReactionConfig>();
        }

        /// <summary>
        /// Get the dynamic config for a specified guild ID.
        /// </summary>
        /// <param name="id">Guild ID</param>
        /// <returns>Configuration for the guild.</returns>
        public GuildConfig GetGuildConfig(ulong id)
        {
            if (dynamicConfig != null && dynamicConfig.Guilds != null)
            {
                foreach (var guildConfig in dynamicConfig.Guilds)
                {
                    if (id.Equals(guildConfig.Id))
                    {
                        return guildConfig;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Loads the dynamic config from disk or triggers an update if the config is invalid or missing.
        /// </summary>
        private void LoadOrGetConfig()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    dynamicConfig = JsonConvert.DeserializeObject<DynamicConfig>(
                            File.ReadAllText(configPath));
                } catch (Exception ex)
                {
                    logger.LogCritical("Failed to load dynamic config. {}", ex.Message);
                    UpdateConfig();
                }
            } else
            {
                logger.LogWarning("Dynamic config is missing.");
                UpdateConfig();
            }
            
        }

        /// <summary>
        /// Pulls the latest dynamic config from the MariBot GitHub repo and saves to disk.
        /// </summary>
        private void UpdateConfig(object sender = null, ElapsedEventArgs e = null)
        {
            using (var client = new WebClient())
            {
                try
                {
                    // Try to download latest config
                    logger.LogInformation("Updating dynamic config.");
                    client.DownloadFile("https://raw.githubusercontent.com/dsdude123/MariBot/master/src/DiscordBot/dynamic-config.json", "dynamic-config-temp.json");
                    dynamicConfig = JsonConvert.DeserializeObject<DynamicConfig>(
                        File.ReadAllText(Environment.CurrentDirectory + "\\dynamic-config-temp.json"));
                }
                catch (Exception ex)
                {
                    // Download failed or config bad, rollback
                    logger.LogWarning("Failed to update dynamic config. {}", ex.Message);
                    logger.LogWarning("Dynamic config rollback in progress.");
                    try
                    {
                        dynamicConfig = JsonConvert.DeserializeObject<DynamicConfig>(
                            File.ReadAllText(Environment.CurrentDirectory + "\\dynamic-config.json"));
                        return;
                    } catch (Exception rollbackEx)
                    {
                        logger.LogCritical("Dynamic config rollback failed. {}", rollbackEx.Message);
                        dynamicConfig = null;
                        return;
                    }
                }

                // Update file on disk
                if (File.Exists("dynamic-config-temp.json") && File.Exists("dynamic-config.json"))
                {
                    File.Delete("dynamic-config.json");
                    File.Move("dynamic-config-temp.json", "dynamic-config.json");
                } else if (File.Exists("dynamic-config-temp.json"))
                {
                    File.Move("dynamic-config-temp.json", "dynamic-config.json");
                }
                
            }
        }
    }
}
