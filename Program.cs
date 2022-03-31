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
            var groupsEnabled = config.GetGroups(out groups);
            var subjectsEnabled = config.GetSubjectFromGroup(out subjects);
            var manualAddsEnabled = config.GetManualAdds(out manualAdds);
            var manualDeletesEnabled = config.GetManualDelete(out manualDeletes);
            Debug.WriteLine("Groups: " + groupsEnabled);
            Debug.WriteLine("GetSubjectFromGroup: " + subjectsEnabled);
            Debug.WriteLine("GetManualAdds: " + manualAddsEnabled);
            Debug.WriteLine("GetManualDelete: " + manualDeletesEnabled);
            var watContent = new Page(login, password);
            var schedules = new List<Schedule>();
            var tempSchedules = new List<Schedule>();
            var calendars = new List<CalendarConnection>();
            //Downloading contents
            CalendarConnection.Connect().Wait();
            Config.Group grupa = new Config.SubjectFromGroup();
            if(groupsEnabled)
            {
                foreach (var group in groups)
                {
                    schedules.Add(watContent.LoadSchedule(group.group, group.year, group.semester, group.calendarName).Result);
                }
            }
            if(subjectsEnabled)
                subjects.Where(s => !groups.Exists(g => g.group == s.group)).ToList()
                    .ForEach(s => tempSchedules.Add(watContent.LoadSchedule(s.group, s.year, s.semester, s.group).Result));
            if (manualAddsEnabled)
            {
                foreach (var addon in manualAdds)
                {
                    if (!schedules.Exists(x => x.calendarName == addon.schedule))
                        groups.Add(new Config.Group
                        {
                            calendarName = addon.schedule,
                            group = addon.schedule,
                            semester = 0,
                            year = addon.entry.start.Year
                        });
                }
            }
            if(manualDeletesEnabled)
                RemoveEvents(manualDeletes, schedules);
            if(manualAddsEnabled)
                AddManualEvents(manualAdds, schedules);
            if (subjectsEnabled)
            {
                foreach (var s in subjects)
                {
                    var source = schedules.Find(x => x.group == s.group);
                    if (source == null)
                        source = tempSchedules.Find(x => x.group == s.group);
                    var destination = schedules.Find(x => x.calendarName == s.calendarName);
                    var export = source.ExportSubject(s);
                    if (destination == null)
                        schedules.Add(export);
                    else
                        destination.Merge(export);
                }
            }
            schedules.ForEach(schedule =>
            {
                calendars.Add(CalendarConnection.GetCalendars(schedule.calendarName).Result);
            });
            //Update
            schedules.ForEach(schedule => calendars.Where(x => x.name == schedule.calendarName).First().Update(schedule));
        }
        static void AddManualEvents(List<Config.ManualAdd> events, List<Schedule> schedules)
        {
            foreach(var e in events)
            {
                var schedule = schedules.Find(x => x.calendarName == e.schedule);
                if (schedule != null && schedule.StartDate.Date > e.entry.start.Date)
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
            if (done = config.GetFirstString(Config.loginTag, out login))
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
