using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoNoteMiner
{
    public static class INI
    {
        static string configFile = AppDomain.CurrentDomain.BaseDirectory + "config.ini";

        static INI()
        {
            if (!File.Exists(configFile))
            {
                File.CreateText(configFile).Close();
            }
        }

        public static void Config(params string[] keyValues)
        {

            if (keyValues.Length % 2 != 0)
                throw new ArgumentException("Must be even number of arguments as [key, value, key, value]");

            var configLines = File.ReadAllLines(configFile);
            var config = configLines.ToDictionary(l => l.Split('=')[0], l => l.Split('=')[1], StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < keyValues.Length; i += 2)
            {
                config[keyValues[i]] = keyValues[i + 1];
            }
            File.WriteAllLines(configFile, from p in config select p.Key.ToLower() + "=" + p.Value.ToLower());
        }

        public static string Value(string key)
        {
            var v = File.ReadAllLines(configFile).SingleOrDefault(s => s.Split('=')[0].ToLower() == key);
            if (v == null) return "";
            return v.Split('=')[1].ToLower();
        }
    }
}
