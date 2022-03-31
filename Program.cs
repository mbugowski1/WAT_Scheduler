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
            foreach (var group in groups)
            {
                schedules.Add(watContent.LoadSchedule(group.group, group.year, group.semester, group.calendarName).Result);
            }
            foreach(var addon in manualAdds)
            {
                if (!groups.Exists(x => x.calendarName == addon.schedule))
                    groups.Add(new Config.Group
                    {
                        calendarName = addon.schedule,
                        group = addon.schedule,
                        semester = 0,
                        year = addon.entry.start.Year
                    });
            }
            AddManualEvents(manualAdds, schedules);
            RemoveEvents(manualDeletes, schedules);
            groups.ForEach(group =>
            {
                calendars.Add(CalendarConnection.GetCalendars(group.group).Result);
            });
            //Update
            schedules.ForEach(schedule => calendars.Where(x => x.name == schedule.calendarName).First().Update(schedule));
        }
        static void AddManualEvents(List<Config.ManualAdd> events, List<Schedule> schedules)
        {
            foreach(var e in events)
            {
                var schedule = schedules.Find(x => x.calendarName == e.schedule);
                if (schedule != null && schedule.startDate.Date > e.entry.start.Date)
                    continue;
                else if (schedule == null)
                {
                    schedule = new Schedule(e.schedule, e.entry.start.Year, 0, e.schedule, DateTime.MinValue);
                    schedules.Add(schedule);
                }
                var day = schedule.days.Find(x => x.date.Date == e.entry.start.Date);
                if(day != null)
                    day.events.Add(e.entry);
                else
                {
                    var insertDay = new ScheduleDay(e.entry.start.Date);
                    insertDay.events.Add(e.entry);
                    schedule.days.Add(insertDay);
                }
            };
        }
        static void RemoveEvents(List<Config.ManualDelete> events, List<Schedule> schedules)
        {
            foreach(var e in events)
            {
                var schedule = schedules.Find(x => x.calendarName == e.schedule);
                if (schedule == null) continue;
                var day = schedule.days.Find(x => x.date.Date == e.start.Date);
                if(day == null) continue;
                var removal = day.events.Find(x => x.start == e.start);
                if(removal == null) continue;
                day.events.Remove(removal);
            }
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
