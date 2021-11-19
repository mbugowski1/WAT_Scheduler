using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAT_Planner
{
    static class Program
    {
        static Config config = new Config(Data.ConfigPath);
        public static void Run()
        {
            string login, password;
            string[] groups;
            Entry[] entriesToAdd;
            if (!LoadCredentials(out login, out password)) return;
            config.GetString("Groups", out groups);
            config.GetEntry("ManualAdd", entriesToAdd, )

        }
        static bool LoadCredentials(out string login, out string password)
        {
            bool done;
            if (done = config.GetFirstString("Login", out login))
            {
                //Obsługa wyjątku z brakiem hasła
                byte[] encryptedPassword = Password.Load(Data.PasswordPath);
                using Password pass = new();
                password = pass.decrypt(encryptedPassword);
            }
            else
            {
                password = null;
                Log("Warning", "No Login");
            }
            return done;

        }
        static void Log(string type, string text)
        {
            File.AppendText($"{DateTime.Now.ToString()} {type}: {text}");
        }
    }
}
