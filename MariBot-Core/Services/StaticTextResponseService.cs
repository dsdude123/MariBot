using MariBot.Core.Models;
using Newtonsoft.Json;

namespace MariBot.Core.Services
{
    /// <summary>
    /// Service providing static text response retrieval and management. 
    /// </summary>
    public class StaticTextResponseService
    {
        public static readonly string LegacyGlobalPath = Environment.CurrentDirectory + "\\data\\global\\textresponse.json";

        private readonly DataService dataService;

        public StaticTextResponseService(DataService dataService)
        {
            this.dataService = dataService;
        }

        /// <summary>
        /// Gets a static text response
        /// </summary>
        /// <param name="command">Command</param>
        /// <param name="guild">Guild ID</param>
        /// <returns>Static text response or null if not found</returns>
        public string? GetResponse(string command, ulong guild)
        {
            var globalId = GenerateId(command);
            var guildId = GenerateId(command, guild);

            var response = dataService.GetStaticTextResponse(globalId) ?? dataService.GetStaticTextResponse(guildId);

            return response?.Message;
        }

        /// <summary>
        /// Adds a new static text response
        /// </summary>
        /// <param name="command">Command name</param>
        /// <param name="text">Response text</param>
        /// <param name="guild">Guild ID</param>
        /// <param name="isGlobal">Should the command be registered as global</param>
        /// <returns>Status message</returns>
        public string AddNewResponse(string command, string text, ulong guild, bool isGlobal)
        {
            var newResponse = new StaticTextResponse
            {
                Command = command,
                Message = text
            };

            // Set values depending if global or not

            if (!isGlobal)
            {
                newResponse.GuildId = guild;
                newResponse.IsGlobal = false;
            }
            else
            {
                newResponse.IsGlobal = true;
            }

            // Make sure no global command exists if we are adding a non-global command.

            var noExistingGlobalResponse = true;

            if (!isGlobal)
            {
                noExistingGlobalResponse = dataService.GetStaticTextResponse(GenerateId(command)) != null;
            }

            // Write to DB

            if (noExistingGlobalResponse)
            {
                var isSuccess = dataService.InsertStaticTextResponse(newResponse);
                return isSuccess ? "OK" : "Failed to add command. It may already exist or there is a problem with the database.";
            }
            else
            {
                return "Failed to add command. It already exists in the global responses.";
            }
        }

        /// <summary>
        /// Updates an existing static text response or adds a new one if not found
        /// </summary>
        /// <param name="command">Command</param>
        /// <param name="text">New response value</param>
        /// <param name="guild">Guild ID</param>
        /// <param name="isGlobal">Is the existing command global</param>
        /// <returns>Status message</returns>
        public string UpdateResponse(string command, string text, ulong guild, bool isGlobal)
        {
            var newResponse = new StaticTextResponse
            {
                Command = command,
                Message = text
            };

            // Set values depending if global or not

            if (!isGlobal)
            {
                newResponse.GuildId = guild;
                newResponse.IsGlobal = false;
            }
            else
            {
                newResponse.IsGlobal = true;
            }

            // Make sure no global response exists if we are updating a non-global command.

            var noExistingGlobalResponse = true;

            if (!isGlobal)
            {
                noExistingGlobalResponse = dataService.GetStaticTextResponse(GenerateId(command)) != null;
            }

            // Write to DB

            if (noExistingGlobalResponse)
            {
                var isSuccess = dataService.UpdateStaticTextResponse(newResponse);
                return isSuccess ? "OK" : "Failed to update command. There is a problem with the database.";
            }
            else
            {
                return "Can't update. This command is in the global responses and the requested type is not global";
            }
        }

        /// <summary>
        /// Removes a static text response
        /// </summary>
        /// <param name="command">Command name</param>
        /// <param name="guild">Guild ID</param>
        /// <param name="isGlobal">Is the command global</param>
        /// <returns>Status message</returns>
        public string RemoveResponse(string command, ulong guild, bool isGlobal)
        {
            var newResponse = new StaticTextResponse
            {
                Command = command
            };

            // Set values depending if global or not

            if (!isGlobal)
            {
                newResponse.GuildId = guild;
                newResponse.IsGlobal = false;
            }
            else
            {
                newResponse.IsGlobal = true;
            }

            // Delete from DB

            var isSuccess = dataService.DeleteStaticTextResponse(newResponse);
            return isSuccess ? "OK" : "Failed to delete command. It may not exist or there is a problem with the database.";
        }

        /// <summary>
        /// Migrates static text responses from legacy data format to the DB.
        /// </summary>
        /// <param name="guild">Guild ID</param>
        /// <param name="isGlobal">Should migrate global responses</param>
        /// <returns>Status message</returns>
        public string MigrateResponses(ulong guild, bool isGlobal)
        {
            if (isGlobal)
            {
                if (!File.Exists(LegacyGlobalPath)) return "No source file.";
                var globalResponses = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(LegacyGlobalPath));

                foreach (var response in globalResponses)
                {
                    var newFormat = new StaticTextResponse
                    {
                        Command = response.Key,
                        Message = response.Value,
                        IsGlobal = true
                    };

                    dataService.InsertStaticTextResponse(newFormat);
                }

                return "Migration complete.";
            }
            else
            {
                var path = $"{Environment.CurrentDirectory}\\data\\{guild}\\textresponse.json";
                if (!File.Exists(path)) return "No source file.";
                var guildResponses = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));

                foreach (var response in guildResponses)
                {
                    var newFormat = new StaticTextResponse
                    {
                        Command = response.Key,
                        Message = response.Value,
                        GuildId = guild,
                        IsGlobal = false
                    };

                    dataService.InsertStaticTextResponse(newFormat);
                }

                return "Migration complete.";
            }

        }

        /// <summary>
        /// Generates an ID for a guild-specific static text response
        /// </summary>
        /// <param name="key">Command</param>
        /// <param name="guildId">Guild ID</param>
        /// <returns>Generated ID</returns>
        private static string GenerateId(string key, ulong guildId)
        {
            return $"{key}:{guildId}";
        }

        /// <summary>
        /// Generates an ID for a global static text response
        /// </summary>
        /// <param name="key">Command</param>
        /// <returns>Generated ID</returns>
        private static string GenerateId(string key)
        {
            return $"{key}:global";
        }
    }
}
