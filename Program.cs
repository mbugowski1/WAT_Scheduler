using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAT_Planner
{
    public static class Program
    {
        static readonly Config config = new(Data.ConfigPath);
        public static void Run()
        {
            string login, password;
            List<Config.Group> groups;
            List<Config.SubjectFromGroup> subjects;
            List<Config.ManualAdd> manualAdds;
            List<Config.ManualDelete> manualDeletes;

            if (!LoadCredentials(out login, out password)) return;
            Debug.WriteLine("Groups: " + config.GetGroups(out groups));
            Debug.WriteLine("GetSubjectFromGroup: " + config.GetSubjectFromGroup(out subjects));
            Debug.WriteLine("GetManualAdds: " + config.GetManualAdds(out manualAdds));
            Debug.WriteLine("GetManualDelete: " + config.GetManualDelete(out manualDeletes));
            var watContent = new Page(login, password);
            var schedules = new List < Schedule > ();
            var calendars = new List<CalendarConnection>();
            //Downloading contents
            CalendarConnection.Connect().Wait();
            groups.ForEach(group =>
            {
                schedules.Add(watContent.LoadSchedule(group.group, group.year, group.semester, group.calendarName).Result);
                calendars.Add(CalendarConnection.GetCalendars(group.group).Result);
            });
            //Update
            schedules.ForEach(schedule => calendars.Where(x => x.name == schedule.calendarName).First().Update(schedule, watContent.StartDate));
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
        public static void Log(string type, string text)
        {
            File.AppendText($"{DateTime.Now.ToString()} {type}: {text}");
        }
    }
}
