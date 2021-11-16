using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAT_Planner
{
    class Config
    {
        static readonly string[] defaultSettings = new string[]
        { 
            "Login",
            "Groups",
            "SubjectFromGroup",
            "ManualAdd",
            "ManualDelete"
        };
        Dictionary<string, string[]> settings = new Dictionary<string, string[]>();
        public Config(in string file)
        {
            if (!File.Exists(file))
            {
                File.Create(file).Close();
                Write(file, defaultSettings.ToList());
            }
            else
            {
                byte[] config = File.ReadAllBytes(file);
                List<string> lines = Encoding.UTF8.GetString(config).Split('\n').ToList();
                if (lines[lines.Count - 1].Length != 0) File.AppendAllText(file, Environment.NewLine);
                lines.ForEach(line =>
                {
                    line = line.Split("#", 2)[0];
                    if (line.Contains('='))
                    {
                        string[] seperated = line.Split('=', 2);
                        string[] values = seperated[1].Split(',');
                        for (int i = 0; i < values.Length; i++)
                            values[i] = String.Concat(values[i].Where(c => !Char.IsWhiteSpace(c)));
                        if (settings.ContainsKey(seperated[0])) return;
                        settings.Add(seperated[0], values);
                    }
                });
            }
            SettingsCheck(file);
        }
        void Write(in string filePath, List<string> settings)
        {
            for (int i = 0; i < settings.Count; i++)
                settings[i] += '=';
            File.AppendAllLines(filePath, settings);
        }
        void SettingsCheck(in string filePath)
        {
            List<string> write = new List<string>();
            foreach (string key in defaultSettings)
                if (!settings.ContainsKey(key)) write.Add(key);
            Write(filePath, write);
        }
        public string[] Get(string key)
        {
            if (settings.TryGetValue(key, out string[] result))
                return settings[key];
            else return null;
        }
        public string GetFirst(string key)
        {
            string[] result = Get(key);
            if (result != null)
                return result[0];
            else return null;
        }
        public override string ToString()
        {
            string result = String.Empty;
            foreach (string key in settings.Keys)
            {
                result += key + " = ";
                bool first = true;
                foreach (string value in settings[key])
                {
                    if (first)
                    {
                        first = false;
                        result += value;
                    }
                    else
                        result += ", " + value;
                }
                result += '\n';
            }
            return result;
        }
    }
}
