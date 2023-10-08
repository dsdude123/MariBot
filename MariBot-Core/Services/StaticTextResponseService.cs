using Discord;
using MariBot.Core.Models;
using Newtonsoft.Json;
using RestSharp.Extensions;

namespace MariBot.Core.Services
{
    /// <summary>
    /// Service providing static text response retrieval and management. 
    /// </summary>
    public class StaticTextResponseService
    {
        public static readonly string LegacyGlobalPath = Environment.CurrentDirectory + "\\data\\global\\textresponse.json";

        private readonly DataService dataService;

        private static readonly string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4414.0 Safari/537.36 Edg/90.0.803.0";

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
        public StaticTextResponse? GetResponse(string command, ulong guild)
        {
            var globalId = GenerateId(command);
            var guildId = GenerateId(command, guild);

            var response = dataService.GetStaticTextResponse(globalId) ?? dataService.GetStaticTextResponse(guildId);

            return response;
        }

        /// <summary>
        /// Adds a new static text response
        /// </summary>
        /// <param name="command">Command name</param>
        /// <param name="text">Response text</param>
        /// <param name="guild">Guild ID</param>
        /// <param name="isGlobal">Should the command be registered as global</param>
        /// <returns>Status message</returns>
        public async Task<string> AddNewResponse(string command, string text, ulong guild, List<Attachment>? discordAttachments, bool isGlobal)
        {
            var stringParts = text.Split(" ");
            var attachments = new Dictionary<string, byte[]>();
            var partsToRemove = new List<string>();

            foreach (var part in stringParts)
            {
                try
                {
                    var uri = new Uri(part);
                    if (uri.Host.EndsWith("cdn.discordapp.com", StringComparison.InvariantCultureIgnoreCase) || uri.Host.EndsWith("media.discordapp.com", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var filename = uri.AbsolutePath.Split("/").Last();
                        var client = new HttpClient();
                        var request = new HttpRequestMessage()
                        {
                            RequestUri = uri,
                            Method = HttpMethod.Get
                        };
                        request.Headers.Add("User-Agent", UserAgent);
                        var response = await client.SendAsync(request);
                        response.EnsureSuccessStatusCode();
                        var file =  await response.Content.ReadAsByteArrayAsync();
                        attachments[filename] = file;
                        partsToRemove.Add(part);
                    }
                } catch {}
            }

            foreach (var part in partsToRemove)
            {
                text = text.Replace(part, "");
            }

            if (discordAttachments != null)
            {
                foreach (var discordFile in discordAttachments)
                {
                    var client = new HttpClient();
                    var request = new HttpRequestMessage()
                    {
                        RequestUri = new Uri(discordFile.Url),
                        Method = HttpMethod.Get
                    };
                    request.Headers.Add("User-Agent", UserAgent);
                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    var file = await response.Content.ReadAsByteArrayAsync();
                    attachments.Add(discordFile.Filename, file);
                }
            }


            var newResponse = new StaticTextResponse
            {
                Command = command,
                Message = text,
                Attachments = attachments
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
                noExistingGlobalResponse = dataService.GetStaticTextResponse(GenerateId(command)) == null;
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
        public async Task<string> UpdateResponse(string command, string text, ulong guild, List<Attachment>? discordAttachments, bool isGlobal)
        {
            var stringParts = text.Split(" ");
            var attachments = new Dictionary<string, byte[]>();
            var partsToRemove = new List<string>();

            foreach (var part in stringParts)
            {
                try
                {
                    var uri = new Uri(part);
                    if (uri.Host.EndsWith("cdn.discordapp.com", StringComparison.InvariantCultureIgnoreCase) || uri.Host.EndsWith("media.discordapp.com", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var filename = uri.AbsolutePath.Split("/").Last();
                        var client = new HttpClient();
                        var request = new HttpRequestMessage()
                        {
                            RequestUri = uri,
                            Method = HttpMethod.Get
                        };
                        request.Headers.Add("User-Agent", UserAgent);
                        var response = await client.SendAsync(request);
                        response.EnsureSuccessStatusCode();
                        var file = await response.Content.ReadAsByteArrayAsync();
                        attachments[filename] = file;
                        partsToRemove.Add(part);
                    }
                }
                catch { }
            }

            foreach (var part in partsToRemove)
            {
                text = text.Replace(part, "");
            }

            if (discordAttachments != null)
            {
                foreach (var discordFile in discordAttachments)
                {
                    var client = new HttpClient();
                    var request = new HttpRequestMessage()
                    {
                        RequestUri = new Uri(discordFile.Url),
                        Method = HttpMethod.Get
                    };
                    request.Headers.Add("User-Agent", UserAgent);
                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    var file = await response.Content.ReadAsByteArrayAsync();
                    attachments.Add(discordFile.Filename, file);
                }
            }

            var newResponse = new StaticTextResponse
            {
                Command = command,
                Message = text,
                Attachments = attachments
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
                noExistingGlobalResponse = dataService.GetStaticTextResponse(GenerateId(command)) == null;
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

        public async Task<string> UpgradeAttachments()
        {
            try
            {
                var existingResponses = dataService.GetStaticTextResponse();

                foreach (var newResponse in existingResponses)
                {
                    if (newResponse.Attachments != null && newResponse.Attachments.Count > 0)
                    {
                        continue;
                    }
                    var stringParts = newResponse.Message.Split(" ");
                    var attachments = new Dictionary<string, byte[]>();
                    var partsToRemove = new List<string>();

                    foreach (var part in stringParts)
                    {
                        try
                        {
                            var uri = new Uri(part);
                            if (uri.Host.EndsWith("cdn.discordapp.com", StringComparison.InvariantCultureIgnoreCase) || uri.Host.EndsWith("media.discordapp.net", StringComparison.InvariantCultureIgnoreCase))
                            {
                                var filename = uri.AbsolutePath.Split("/").Last();
                                var client = new HttpClient();
                                var request = new HttpRequestMessage()
                                {
                                    RequestUri = uri,
                                    Method = HttpMethod.Get
                                };
                                request.Headers.Add("User-Agent", UserAgent);
                                var response = await client.SendAsync(request);
                                response.EnsureSuccessStatusCode();
                                var file = await response.Content.ReadAsByteArrayAsync();
                                attachments[filename] = file;
                                partsToRemove.Add(part);
                            }
                        }
                        catch { }
                    }

                    foreach (var part in partsToRemove)
                    {
                        newResponse.Message = newResponse.Message.Replace(part, "");
                    }

                    newResponse.Attachments = attachments;

                    dataService.UpdateStaticTextResponse(newResponse);
                }

                return "Upgrade complete.";
            }
            catch (Exception ex)
            {
                return ex.Message + "\n" + ex.StackTrace;
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
