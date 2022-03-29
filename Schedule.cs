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
        public readonly DateTime startDate;
        public List<ScheduleDay> days = new List<ScheduleDay>();
        public Schedule(string group, int year, int semester, string calendarName, DateTime startDate)
        {
            this.group = group;
            this.semester = semester;
            this.year = year;
            this.calendarName = calendarName;
            this.startDate = startDate;
        }
        public ScheduleDay Find(DateTime date)
        {
            foreach(ScheduleDay singleDay in days)
            {
                if (singleDay.date == date)
                    return singleDay;
            }
            ScheduleDay created = new ScheduleDay(date);
            days.Add(created);
            return created;
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
        public string ToString(int day, int month, int year)
        {
            string result = String.Empty;
            days.Where(x => x.date.Day == day && x.date.Month == month && x.date.Year == year).ToList().ForEach(x => result += x.ToString() + "\n\n");
            return result;
        }
    }
}
