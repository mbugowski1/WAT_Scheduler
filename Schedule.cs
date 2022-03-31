using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAT_Planner
{
    class Schedule
    {
        public readonly string group;
        public readonly int semester;
        public readonly int year;
        public readonly string calendarName;
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public List<ScheduleDay> days = new List<ScheduleDay>();
        public Schedule(string group, int year, int semester, string calendarName, DateTime startDate, DateTime endDate)
        {
            this.group = group;
            this.semester = semester;
            this.year = year;
            this.calendarName = calendarName;
            StartDate = startDate;
            EndDate = endDate;
        }
        public ScheduleDay FindDay(DateTime date)
        {
            foreach(ScheduleDay singleDay in days)
            {
                if (singleDay.date == date)
                    return singleDay;
            }
            return null;
        }
        public List<Entry> ToOneList()
        {
            List<Entry> list = new List<Entry>();
            foreach(ScheduleDay day in days)
            {
                list.AddRange(day.events);
            }
            return list;
        }
        public Schedule ExportSubject(Config.SubjectFromGroup subject)
        {
            var result = new Schedule(subject.group, year, semester, subject.calendarName, StartDate, EndDate);
            foreach(var day in days)
            {
                ScheduleDay newDay = null;
                foreach(var entry in day.events.Where(e => e.type == subject.type && e.shortname == subject.shortname))
                {
                    if (newDay == null)
                    {
                        newDay = new ScheduleDay(day.date);
                        result.days.Add(newDay);
                    }
                    newDay.events.Add(entry);
                }
            }
            return result;
        }
        public void Merge(Schedule other)
        {
            if(StartDate > other.StartDate) StartDate = other.StartDate;
            foreach(var day in other.days)
            {
                var commonDay = days.Find(x => x.date.Date == day.date.Date);
                if(commonDay == null) days.Add(day);
                else
                {
                    foreach(var e in day.events)
                    {
                        if (!commonDay.events.Exists(x => x.timeIndex == e.timeIndex && x.type == e.type &&
                             x.longname == e.longname && x.leader == e.leader))
                        {
                            commonDay.events.Add(e);
                        }
                    }
                }
            }
        }
        public string ToString(int day, int month, int year)
        {
            string result = String.Empty;
            days.Where(x => x.date.Day == day && x.date.Month == month && x.date.Year == year).ToList().ForEach(x => result += x.ToString() + "\n\n");
            return result;
        }
    }
}
