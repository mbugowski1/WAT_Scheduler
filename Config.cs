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
        private class Setting
        {
            public bool Brackets { private set; get; }
            public object Value { private set; get; }
            public Setting(object value, bool brackets)
            {
                Value = value;
                Brackets = brackets;
            }
            public void SetValue(object value, bool brackets)
            {
                Value = value;
                Brackets = brackets;
            }
        }
        static readonly string[] defaultSettings = new string[]
        {
            "Login",
            "Groups",
            "SubjectFromGroup",
            "ManualAdd",
            "ManualDelete"
        };
        Dictionary<string, Setting[]> settings = new Dictionary<string, Setting[]>();
        //Dictionary<string, string[]> settings = new Dictionary<string, string[]>();
        //Dictionary<string, Dictionary<string, string>> settings = new Dictionary<string, Dictionary<string, string>>();
        //Dictionary<string, Entry[]> manual = new Dictionary<string, Entry[]>();
        public Config(in string file)
        {
            if (!File.Exists(file))
            {
                File.Create(file).Close();
            }
            else
            {
                //Remove white symbols and split brackets
                string config = Encoding.UTF8.GetString(File.ReadAllBytes(file));
                config = PullBrackets(RemoveChars(config, new char[] { '\t', '\r' }));
                //Seperate lines
                string[] lines = config.Split('\n');
                //Add newline at the end
                if (lines[lines.Length - 1].Length != 0)
                    File.AppendAllText(file, Environment.NewLine);

                LoadSettings(lines);
            }
            SettingsCheck(file);
        }
        void LoadSettings(string[] lines)
        {
            foreach (string edit in lines)
            {
                string line = edit.Split("#", 2)[0];
                if (line.Contains('='))
                {
                    string[] seperated = line.Split('=', 2);
                    if (settings.ContainsKey(seperated[0])) return;
                    Setting[] sets;
                    if (seperated[1].Contains('{') && seperated[1].Contains('}'))
                    {
                        int stop = 0;
                        List<Dictionary<string, string>> pairs = new List<Dictionary<string, string>>();
                        while (seperated[1].IndexOf('{', stop) != -1)
                        {
                            int start = seperated[1].IndexOf('{', stop) + 1;
                            stop = seperated[1].IndexOf('}', start);
                            string textToEdit = seperated[1].Substring(start, stop - start);
                            pairs.Add(GetDictionary(textToEdit));
                        }
                        sets = new Setting[pairs.Count];
                        for (int i = 0; i < sets.Length; i++)
                            sets[i] = new Setting(pairs.ToArray(), true);
                    }
                    else
                    {
                        string[] values = seperated[1].Split(',');
                        sets = new Setting[values.Length];
                        for (int i = 0; i < values.Length; i++)
                            sets[i] = new Setting(values[i], false);
                    }
                    settings.Add(seperated[0], sets);
                }
            }
        }
        string PullBrackets(in string text)
        {
            int startIndex = 0;
            StringBuilder modifier = new StringBuilder(text);
            bool inside = false;
            for (int i = 0; i < modifier.Length; i++)
            {
                if (modifier[i] == '{')
                {
                    startIndex = i;
                    inside = true;
                }
                else if (modifier[i] == '}')
                {
                    modifier.Replace("\n", "", startIndex, i - startIndex);
                }
                if (!inside)
                    if (modifier[i] == ' ')
                        modifier.Remove(i--, 1);
            }
            return modifier.ToString();
        }
        string RemoveChars(in string text, char[] excluded)
        {
            StringBuilder modifier = new StringBuilder(text);
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
        Dictionary<string, string> GetDictionary(string text)
        {
            string[] options = text.Split(',');
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (string option in options)
            {
                if (!option.Contains('=')) continue;
                string[] values = option.Split('=', 2);
                result.Add(values[0], values[1]);
            }
            return result;
        }
        public Entry[] GetEntry(string entry, out string schedule)
        {
            schedule = null;
            if (!settings.TryGetValue(entry, out Setting[] sets))
            {
                return null;
            }
            foreach (Setting set in sets)
                if (set.Brackets == false)
                    throw new ArgumentException("Tried to read from non brackets setting");
            Entry[] result = new Entry[sets.Length];
            for (int i = 0; i < sets.Length; i++)
            {
                Dictionary<string, string> pairs = (Dictionary<string, string>)sets[i].Value;
                schedule = null;
                string dateText = String.Empty, startTime = String.Empty, stopTime = String.Empty;
                result[i] = new Entry();
                bool done;
                if (done = pairs.TryGetValue("short_name", out result[i].shortname))
                    if (done = pairs.TryGetValue("long_name", out result[i].longname))
                        if (done = pairs.TryGetValue("leader", out result[i].leader))
                            if (done = pairs.TryGetValue("type", out result[i].type))
                                if (done = pairs.TryGetValue("date", out dateText))
                                    if (done = pairs.TryGetValue("start_time", out startTime))
                                        if (done = pairs.TryGetValue("stop_time", out stopTime))
                                            done = pairs.TryGetValue("schedule", out schedule);
                if (done)
                {
                    DateTime date = GetDate(dateText);
                    result[i].start = GetTime(date, startTime);
                    result[i].stop = GetTime(date, stopTime);
                    result[i].shortType = result[i].type.Substring(0, 1);
                }
                else
                    throw new ArgumentException("Error in config file");
            }
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
                if (!settings.ContainsKey(key))
                    write.Add(key);
            Write(filePath, write);
        }
        public override string ToString()
        {
            string result = String.Empty;
            foreach (string key in settings.Keys)
            {
                result += key + " = ";
                bool first = true;
                foreach (Setting setting in settings[key])
                {
                    if (!first)
                        result += ", ";
                    else
                        first = false;
                    if (setting.Brackets)
                    {
                        var values = (Dictionary<string, string>[])setting.Value;
                        foreach(Dictionary<string, string> dic in values)
                        {
                            result += "{ ";
                            foreach (KeyValuePair<string, string> pair in dic)
                                result += $"{pair.Key}={pair.Value}{Environment.NewLine}";
                            result += " }";
                        }
                    }
                    else
                        result += setting.Value;
                }
                result += '\n';
            }
            return result;
        }
    }
}
