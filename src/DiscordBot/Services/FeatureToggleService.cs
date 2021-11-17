using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MariBot.Services
{
    public class FeatureToggleService
    {
        public void EnableFeature(string featureName, string guild)
        {
            Dictionary<string, bool> features = new Dictionary<string, bool>();

            if (File.Exists(Environment.CurrentDirectory + "\\data\\" + guild + "\\features.json"))
            {
                features = JsonConvert.DeserializeObject<Dictionary<string, bool>>(
                    File.ReadAllText(Environment.CurrentDirectory + "\\data\\" + guild + "\\features.json"));
            }
            features[featureName] = true;
            Directory.CreateDirectory(Environment.CurrentDirectory + "\\data\\" + guild);
            File.WriteAllText(Environment.CurrentDirectory + "\\data\\" + guild + "\\features.json",
                JsonConvert.SerializeObject(features));
        }

        public void DisableFeature(string featureName, string guild)
        {
            Dictionary<string, bool> features = new Dictionary<string, bool>();

            if (File.Exists(Environment.CurrentDirectory + "\\data\\" + guild + "\\features.json"))
            {
                features = JsonConvert.DeserializeObject<Dictionary<string, bool>>(
                    File.ReadAllText(Environment.CurrentDirectory + "\\data\\" + guild + "\\features.json"));
            }
            features[featureName] = false;
            Directory.CreateDirectory(Environment.CurrentDirectory + "\\data\\" + guild);
            File.WriteAllText(Environment.CurrentDirectory + "\\data\\" + guild + "\\features.json",
                JsonConvert.SerializeObject(features));
        }

        public bool CheckFeature(string featureName, string guild)
        {
            Dictionary<string, bool> features = new Dictionary<string, bool>();

            if (File.Exists(Environment.CurrentDirectory + "\\data\\" + guild + "\\features.json"))
            {
                features = JsonConvert.DeserializeObject<Dictionary<string, bool>>(
                    File.ReadAllText(Environment.CurrentDirectory + "\\data\\" + guild + "\\features.json"));
            }
            if (!features.ContainsKey(featureName) || !features[featureName])
            {
                return false;
            }
            return true;
        }
    }
}
