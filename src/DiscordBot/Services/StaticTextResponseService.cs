﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MariBot.Services
{
    public class StaticTextResponseService
    {
        private static string globalPath = Environment.CurrentDirectory + "\\data\\global\\textresponse.json";
        private static Dictionary<string, string> globalResponseList = null;

        public static string getGlobalResponse(string key)
        {
            if (globalResponseList == null) loadGlobalResponseList();
            if (globalResponseList.ContainsKey(key.ToLower()))
            {
                return globalResponseList[key.ToLower()];
            } else
            {
                return null;
            }
        }

        public static Stream getAllGlobalResponses()
        {
            if (File.Exists(globalPath))
            {
                return File.OpenRead(globalPath);
            } else
            {
                globalResponseList = new Dictionary<string, string>();
                Directory.CreateDirectory(Environment.CurrentDirectory + "\\data\\global");
                File.WriteAllText(globalPath, JsonConvert.SerializeObject(globalResponseList));
                return getAllGlobalResponses();
            }
        }

        public static void addGlobalResponse(string key, string text)
        {
            Regex userIdCheck = new Regex(@"<@![0-9]+>", RegexOptions.Compiled);
            if (userIdCheck.IsMatch(key.ToLower()))
            {
                key = key.Replace("!", string.Empty);
            }
            if (globalResponseList == null) loadGlobalResponseList();

            if (globalResponseList.ContainsKey(key.ToLower()))
            {
                throw new InvalidOperationException("The global response list already contains an entry for: " + key);
            } else
            {
                globalResponseList.Add(key.ToLower(), text);
                File.WriteAllText(globalPath, JsonConvert.SerializeObject(globalResponseList));
            }
        }

        public static void updateGlobalResponse(string key, string text)
        {
            Regex userIdCheck = new Regex(@"<@![0-9]+>", RegexOptions.Compiled);
            if (userIdCheck.IsMatch(key.ToLower()))
            {
                key = key.Replace("!", string.Empty);
            }
            if (globalResponseList == null) loadGlobalResponseList();

            if (globalResponseList.ContainsKey(key.ToLower()))
            {
                globalResponseList[key.ToLower()] = text;
                File.WriteAllText(globalPath, JsonConvert.SerializeObject(globalResponseList));
            }
            else
            {
                throw new InvalidOperationException("The global response list does not contain an entry for: " + key);
            }
        }

        public static void removeGlobalResponse(string key)
        {
            Regex userIdCheck = new Regex(@"<@![0-9]+>", RegexOptions.Compiled);
            if (userIdCheck.IsMatch(key.ToLower()))
            {
                key = key.Replace("!", string.Empty);
            }
            if (globalResponseList == null) loadGlobalResponseList();

            if (globalResponseList.ContainsKey(key.ToLower()))
            {
                globalResponseList.Remove(key.ToLower());
                File.WriteAllText(globalPath, JsonConvert.SerializeObject(globalResponseList));
            }
            else
            {
                throw new InvalidOperationException("The global response list does not contain an entry for: " + key);
            }
        }

        public string getResponse(ulong guild, string key)
        {
            Dictionary<string, string> serverResponses = loadResponseList(guild);

            if (serverResponses.ContainsKey(key.ToLower()))
            {
                return serverResponses[key.ToLower()];
            } else
            {
                return null;
            }
        }

        public Stream getAllResponses(ulong guild)
        {
            string path = Environment.CurrentDirectory + "\\data\\" + guild + "\\textresponse.json";
            if (File.Exists(path))
            {
                return File.OpenRead(path);
            } else
            {
                Dictionary<string, string>  serverResposnes = new Dictionary<string, string>();
                Directory.CreateDirectory(Environment.CurrentDirectory + "\\data\\" + guild);
                File.WriteAllText(path, JsonConvert.SerializeObject(serverResposnes));
                return getAllResponses(guild);
            }
        }

        public void addResponse(ulong guild, string key, string text)
        {
            Regex userIdCheck = new Regex(@"<@![0-9]+>", RegexOptions.Compiled);
            if (userIdCheck.IsMatch(key.ToLower()))
            {
                key = key.Replace("!", string.Empty);
            }
            string path = Environment.CurrentDirectory + "\\data\\" + guild + "\\textresponse.json";
            Dictionary<string, string> serverResponses = loadResponseList(guild);

            if (globalResponseList != null && globalResponseList.ContainsKey(key.ToLower()))
            {
                throw new InvalidOperationException("The selected key conflicts with an entry in the global response list");
            }
            else if (serverResponses.ContainsKey(key.ToLower()))
            {
                throw new InvalidOperationException("The response list already contains an entry for: " + key);
            }
            else
            {
                serverResponses.Add(key.ToLower(), text);
                File.WriteAllText(path, JsonConvert.SerializeObject(serverResponses));
            }
        }

        public void updateResponse(ulong guild, string key, string text)
        {
            Regex userIdCheck = new Regex(@"<@![0-9]+>", RegexOptions.Compiled);
            if (userIdCheck.IsMatch(key.ToLower()))
            {
                key = key.Replace("!", string.Empty);
            }
            string path = Environment.CurrentDirectory + "\\data\\" + guild + "\\textresponse.json";
            Dictionary<string, string> serverResponses = loadResponseList(guild);

            if (serverResponses.ContainsKey(key.ToLower()))
            {
                serverResponses[key.ToLower()] = text;
                File.WriteAllText(path, JsonConvert.SerializeObject(serverResponses));
            }
            else
            {
                throw new InvalidOperationException("The response list does not contain an entry for: " + key);
            }
        }

        public void removeResponse(ulong guild, string key)
        {
            Regex userIdCheck = new Regex(@"<@![0-9]+>", RegexOptions.Compiled);
            if (userIdCheck.IsMatch(key.ToLower()))
            {
                key = key.Replace("!", string.Empty);
            }
            string path = Environment.CurrentDirectory + "\\data\\" + guild + "\\textresponse.json";
            Dictionary<string, string> serverResponses = loadResponseList(guild);

            if (serverResponses.ContainsKey(key.ToLower()))
            {
                serverResponses.Remove(key.ToLower());
                File.WriteAllText(path, JsonConvert.SerializeObject(serverResponses));
            }
            else
            {
                throw new InvalidOperationException("The response list does not contain an entry for: " + key);
            }
        }

        private static void loadGlobalResponseList()
        {
            if (File.Exists(globalPath))
            {
                globalResponseList = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                        System.IO.File.ReadAllText(globalPath));
            }
            else
            {
                globalResponseList = new Dictionary<string, string>();
                Directory.CreateDirectory(Environment.CurrentDirectory + "\\data\\global");
                File.WriteAllText(globalPath, JsonConvert.SerializeObject(globalResponseList));
            }
        }

        private Dictionary<string, string> loadResponseList(ulong guild)
        {
            string path = Environment.CurrentDirectory + "\\data\\" + guild + "\\textresponse.json";
            if (File.Exists(path))
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(
                        System.IO.File.ReadAllText(path));
            }
            else
            {
                Dictionary<string, string> serverResponses = new Dictionary<string, string>();
                Directory.CreateDirectory(Environment.CurrentDirectory + "\\data\\" + guild);
                File.WriteAllText(path, JsonConvert.SerializeObject(serverResponses));
                return serverResponses;
            }
        }
    }
}
