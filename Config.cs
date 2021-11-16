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
        Dictionary<string, Entry[]> manual = new Dictionary<string, Entry[]>();
        public Config(in string file)
        {
            if (!File.Exists(file))
            {
                File.Create(file).Close();
            }
            else
            {
                string config = Encoding.UTF8.GetString(File.ReadAllBytes(file));
                config = removeNextLineFromBrackets(config, new char[] { ' ', '\t', '\r' });
                List<string> lines = config.Split('\n').ToList();
                if (lines[lines.Count - 1].Length != 0) File.AppendAllText(file, Environment.NewLine);
                lines.ForEach(line =>
                {
                    line = line.Split("#", 2)[0];
                    if (line.Contains('='))
                    {
                        string[] seperated = line.Split('=', 2);
                        if (seperated[1].Contains('{') && seperated[1].Contains('}'))
                        {
                            if (seperated[0] != "ManualDelete" && seperated[0] != "ManualAdd") return;
                            int stop = 0;
                            List<Entry> values = new List<Entry>();
                            while(seperated[1].IndexOf('{', stop) != -1)
                            {
                                int start = seperated[1].IndexOf('{', stop) + 1;
                                stop = seperated[1].IndexOf('}', start);
                                values.Add(GetEntry(seperated[1].Substring(start, stop - start)));
                            }
                            if (settings.ContainsKey(seperated[0])) return;
                            manual.Add(seperated[0], values.ToArray());
                        }
                        else
                        {
                            string[] values = seperated[1].Split(',');
                            //for (int i = 0; i < values.Length; i++)
                            //    values[i] = String.Concat(values[i].Where(c => !Char.IsWhiteSpace(c)));
                            if (settings.ContainsKey(seperated[0])) return;
                            settings.Add(seperated[0], values);
                        }
                    }
                });
            }
            SettingsCheck(file);
        }
        string removeNextLineFromBrackets(in string text, char[] excluded)
        {
            int startIndex = 0;
            StringBuilder modifier = new StringBuilder(text);
            for(int i = 0; i < modifier.Length; i++)
            {
                if (modifier[i] == '{')
                {
                    startIndex = i;
                }
                else if (modifier[i] == '}')
                {
                    modifier.Replace("\n", "", startIndex, i - startIndex);
                }
            }
            for (int i = 0; i < modifier.Length; i++)
            {
                if(excluded.Contains(modifier[i]))
                {
                    modifier.Remove(i--, 1);
                }
            }
            return modifier.ToString();
        }
        DateTime GetDate(string date)
        {
            string[] dateStrings = date.Split('-', 3);
            return new DateTime(Int32.Parse(dateStrings[0]), Int32.Parse(dateStrings[1]), Int32.Parse(dateStrings[2]));
        }
        DateTime GetTime(DateTime date, string time)
        {
            string[] timeStrings = time.Split(':', 2);
            DateTime result = new DateTime(date.Year, date.Month, date.Day, Int32.Parse(timeStrings[0]), Int32.Parse(timeStrings[1]), 0);
            return result;
        }
        Entry GetEntry(string text)
        {
            string shortName = String.Empty, longName = String.Empty, leader = String.Empty, type = String.Empty, startTime = null, stopTime = null;
            DateTime start = DateTime.Now, stop = DateTime.Now;
            DateTime? date = null;
            string[] options = text.Split(',');
            foreach(string option in options)
            {
                if (!option.Contains('=')) continue;
                string[] values = option.Split('=', 2);
                string key = values[0];
                string value = values[1];
                switch (key)
                {
                    case "short_name":
                        shortName = value;
                        break;
                    case "long_name":
                        longName = value;
                        break;
                    case "leader":
                        leader = value;
                        break;
                    case "type":
                        type = value;
                        break;
                    case "date":
                        date = GetDate(value);
                        break;
                    case "start_time":
                        startTime = value;
                        break;
                    case "stop_time":
                        stopTime = value;
                        break;
                }
            }
            if(date != null)
            {
                if (startTime != null)
                    start = GetTime((DateTime)date, startTime);
                if (stopTime != null)
                    stop = GetTime((DateTime)date, stopTime);
            }
            Entry result = new Entry
            {
                shortname = shortName,
                longname = longName,
                leader = leader,
                type = type,
                start = start,
                stop = stop,
                shortType = type.Substring(0, 1)
            };
            return result;
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
        public string[] GetString(string key)
        {
            if (settings.TryGetValue(key, out string[] result))
                return result;
            else return null;
        }
        public string GetStringFirst(string key)
        {
            string[] result = GetString(key);
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
            foreach (string key in manual.Keys)
            {
                result += key + " = ";
                foreach(Entry entry in manual[key])
                {
                    result += $"{{ {entry.longname}, {entry.type}, {entry.leader}, {entry.start} }}\n";
                }
            }
            return result;
        }
    }
}
