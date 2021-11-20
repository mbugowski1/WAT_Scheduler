using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WAT_Planner
{
    class Config
    {
        static readonly string loginTag = "Login";
        static readonly string groupsTag = "Groups";
        static readonly string subjectsTag = "SubjectFromGroup";
        static readonly string addsTag = "ManualAdd";
        static readonly string deletesTag = "ManualDelete";


        public struct ManualAdd
        {
            public Entry entry;
            public string schedule;
            public override string ToString()
            {
                return $"ManualAdd {{\n\tentry={entry}\n\tschedule={schedule}\n}}";
            }
        }
        public struct SubjectFromGroup
        {
            public string shortname;
            public string leader;
            public string type;
            public int year;
            public int semester;
            public string scheduleFrom;
            public string scheduleTo;
            public override string ToString()
            {
                return $"SubjectFromGroup {{\n\tshortname={shortname}\n\tleader={leader}\n\ttype={type}\n\tyear={year}\n\tsemester={semester}\n\t" +
                    $"scheduleFrom={scheduleFrom}\n\tscheduleTo={scheduleTo}\n}}";
            }
        }
        public struct ManualDelete
        {
            public DateTime start;
            public string schedule;
            public override string ToString()
            {
                return $"ManualDelete {{\n\tstart={start}\n\tschedule={schedule}\n}}";
            }
        }
        public struct Group
        {
            public string group;
            public int year;
            public int semester;
            public string calendarName;
            public override string ToString()
            {
                return $"Group {{\n\tgroup={group}\n\tyear={year}\n\tsemester={semester}\n\tcalendarName={calendarName}\n}}";
            }
        }

        private class Setting
        {
            public bool IsDictionary { private set; get; }
            public object Value { private set; get; }
            public Setting(object value, bool brackets)
            {
                Value = value;
                IsDictionary = brackets;
            }
            public void SetValue(object value, bool brackets)
            {
                Value = value;
                IsDictionary = brackets;
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
        Dictionary<string, List<Setting>> settings = new Dictionary<string, List<Setting>>();
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
                    List<Setting> sets = new List<Setting>();
                    if (seperated[1].Contains('{') && seperated[1].Contains('}'))
                    {
                        int stop = 0;
                        List<Dictionary<string, string>> pairs = new List<Dictionary<string, string>>();
                        while (seperated[1].IndexOf('{', stop) != -1)
                        {
                            int start = seperated[1].IndexOf('{', stop) + 1;
                            stop = seperated[1].IndexOf('}', start);
                            string textToEdit = seperated[1].Substring(start, stop - start);
                            pairs.Add(CreateDictionary(textToEdit));
                        }
                        pairs.ForEach(x =>
                        {
                            sets.Add(new Setting(x, true));
                        });
                    }
                    else
                    {
                        string[] values = seperated[1].Split(',');
                        for (int i = 0; i < values.Length; i++)
                            sets.Add(new Setting(values[i], false));
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
        DateTime CreateDate(string date)
        {
            string[] dateStrings = date.Split('-', 3);
            return new DateTime(Int32.Parse(dateStrings[0]), Int32.Parse(dateStrings[1]), Int32.Parse(dateStrings[2]));
        }
        DateTime CreateTime(DateTime date, string time)
        {
            string[] timeStrings = time.Split(':', 2);
            DateTime result = new DateTime(date.Year, date.Month, date.Day, Int32.Parse(timeStrings[0]), Int32.Parse(timeStrings[1]), 0);
            return result;
        }
        Dictionary<string, string> CreateDictionary(string text)
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
        public bool GetDictionary(string setting, out List<Dictionary<string, string>> result)
        {
            if (!settings.TryGetValue(setting, out List<Setting> sets))
            {
                result = null;
                return false;
            }
            if(sets.Exists(x => x.IsDictionary == false))
            {
                //log
                result = null;
                return false;
            }
            result = new();
            foreach(Setting x in sets)
            {
                result.Add((Dictionary<string, string>)x.Value);
            }
            return true;
        }
        public bool GetManualAdds(out List<ManualAdd> result)
        {
            if (!GetDictionary(addsTag, out List<Dictionary<string, string>> sets))
            {
                result = null;
                return false;
            }
            string[] tags =
            {
                "short_name",
                "long_name",
                "leader",
                "type",
                "date",
                "start_time",
                "stop_time",
                "schedule"
            };
            foreach (string tag in tags)
            {
                foreach (var set in sets)
                {
                    if (!set.ContainsKey(tag))
                    {
                        result = null;
                        return false;
                    }
                }
            }

            result = new();
            foreach (var set in sets)
            {
                ManualAdd newer = new ManualAdd
                {
                    entry = new(),
                    schedule = null
                };
                newer.entry.shortname = set["short_name"];
                newer.entry.longname = set["long_name"];
                newer.entry.leader = set["leader"];
                newer.entry.type = set["type"];
                string dateText = set["date"];
                string startTime = set["start_time"];
                string stopTime = set["stop_time"];
                newer.schedule = set["schedule"];

                DateTime date = CreateDate(dateText);
                newer.entry.start = CreateTime(date, startTime);
                newer.entry.stop = CreateTime(date, stopTime);
                newer.entry.shortType = newer.entry.type.Substring(0, 1);
                result.Add(newer);
            }
            return true;
        }
        public bool GetSubjectFromGroup(out List<SubjectFromGroup> result)
        {
            if (!GetDictionary(subjectsTag, out List<Dictionary<string, string>> sets))
            {
                result = null;
                return false;
            }
            string[] tags =
            {
                "short_name",
                "leader",
                "type",
                "scheduleFrom",
                "scheduleTo"
            };
            foreach (string tag in tags)
            {
                foreach (var set in sets)
                {
                    if (!set.ContainsKey(tag))
                    {
                        result = null;
                        return false;
                    }
                }
            }
            result = new();
            foreach (var set in sets)
            {
                SubjectFromGroup subject = new SubjectFromGroup
                {
                    shortname = set["short_name"],
                    leader = set["leader"],
                    type = set["type"],
                    scheduleFrom = set["scheduleFrom"],
                    scheduleTo = set["scheduleTo"],
                };
                if(!Int32.TryParse(set["year"], out subject.year) || !Int32.TryParse(set["semester"], out subject.semester))
                {
                    //log
                    result = null;
                    return false;
                }
                result.Add(subject);
            }
            return true;
        }
        public bool GetGroups(out List<Group> result)
        {
            if (!GetDictionary(groupsTag, out List<Dictionary<string, string>> sets))
            {
                result = null;
                return false;
            }
            string[] tags =
            {
                "groupName",
                "year",
                "semester",
                "calendarName"
            };
            foreach (string tag in tags)
            {
                foreach (var set in sets)
                {
                    if (!set.ContainsKey(tag))
                    {
                        result = null;
                        return false;
                    }
                }
            }
            result = new();
            foreach(var set in sets)
            {
                Group group = new Group
                {
                    group = set["groupName"],
                    calendarName = set["calendarName"]
                };
                if(!Int32.TryParse(set["year"], out group.year) || !Int32.TryParse(set["semester"], out group.semester))
                {
                    //log
                    result = null;
                    return false;
                }
                result.Add(group);
            }
            return true;
        }
        public bool GetManualDelete(out List<ManualDelete> result)
        {
            if (!GetDictionary(deletesTag, out List<Dictionary<string, string>> sets))
            {
                result = null;
                return false;
            }
            string[] tags =
            {
                "date",
                "start_time",
                "schedule"
            };
            foreach (string tag in tags)
            {
                foreach (var set in sets)
                {
                    if (!set.ContainsKey(tag))
                    {
                        result = null;
                        return false;
                    }
                }
            }
            result = new();
            foreach (var set in sets)
            {
                ManualDelete group = new ManualDelete
                {
                    start = CreateTime(CreateDate(set["date"]), set["start_time"]),
                    schedule = set["schedule"]
                };
                result.Add(group);
            }
            return true;
        }
        public bool GetString(string setting, out List<string> result)
        {
            if (!settings.TryGetValue(setting, out List<Setting> sets))
            {
                result = null;
                return false;
            }
            if(sets.Exists(x => x.IsDictionary == true))
                throw new ArgumentException("Tried to read from brackets setting");
            result = new();
            foreach (var set in sets)
            {
                result.Add((string)set.Value);
            }
            return true;
        }
        public bool GetFirstString(string setting, out string result)
        {
            bool notFaulty = GetString(setting, out List<string> array);
            result = array[0];
            return notFaulty;
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
            foreach (KeyValuePair<string, List<Setting>> setting in settings)
            {
                result += setting.Key + " = ";
                bool first = true;
                foreach (Setting settingValue in setting.Value)
                {
                    if (!first)
                        result += ", ";
                    else
                        first = false;
                    if (settingValue.IsDictionary)
                    {
                        var values = (Dictionary<string, string>)settingValue.Value;
                        result += "{\n";
                            foreach (KeyValuePair<string, string> pair in values)
                                result += $"\t{pair.Key}={pair.Value}{Environment.NewLine}";
                        result += "}";
                    }
                    else
                        result += settingValue.Value;
                }
                result += '\n';
            }
            return result;
        }
    }
}
