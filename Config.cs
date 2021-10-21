﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAT_Planner
{
    class Config
    {
        private struct Setting
        {
            public string key;
            public string[] values;
        }
        static readonly Setting[] defaultSettings = {
            new Setting { key = "Login", values = new string[] { String.Empty }  },
            new Setting { key = "Groups", values = new string[] { String.Empty } },
            new Setting { key = "SubjectFromGroup", values = new string[] { String.Empty } },
            new Setting { key = "ManualAdd", values = new string[] { String.Empty } },
            new Setting { key = "ManualDelete", values = new string[] { String.Empty } }
        };
        List<Setting> settings = new List<Setting>();
        public Config(in string file)
        {
            if (!File.Exists(file))
            {
                File.Create(file).Close();
                Write(file, defaultSettings.ToList());
                settings.AddRange(defaultSettings);
            }
            else
            {
                using (StreamReader reader = new StreamReader(file, Encoding.ASCII))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Split(" #", 2)[0];
                        if(line.Contains('='))
                        {
                            string[] seperated = line.Split('=', 2);
                            string[] values = seperated[1].Split(',');
                            for(int i = 0; i < values.Length; i++)
                                values[i] = String.Concat(values[i].Where(c => !Char.IsWhiteSpace(c)));
                            Setting add = new Setting()
                            {
                                key = seperated[0],
                                values = values
                            };
                            settings.Add(add);
                        }
                    }
                }
                SettingsCheck(file);
            }
        }
        void Write(in string filePath, List<Setting> settings)
        {
            List<string> lines = new List<string>();
            foreach (Setting set in settings)
            {
                lines.Add(set.key + "=");
            }
            File.AppendAllLines(filePath, lines);
        }
        void SettingsCheck(in string filePath)
        {
            List<Setting> write = new List<Setting>();
            foreach(Setting defaultKey in defaultSettings)
            {
                bool exists = false;
                foreach(Setting fileKey in settings)
                {
                    if (defaultKey.key == fileKey.key) exists = true;
                }
                if (!exists) write.Add(new Setting() { key = defaultKey.key });
            }
            Write(filePath, write);
        }
        public override string ToString()
        {
            string result = String.Empty;
            foreach (Setting set in settings)
            {
                result += set.key + " = ";
                bool first = true;
                foreach (string value in set.values)
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