using System.Collections.Generic;
using System.Threading.Tasks;
using System;
namespace WAT_Planner
{
    class ScheduleDay
    {
        public DateTime date;
        public List<Entry> events = new List<Entry>();
        public ScheduleDay(DateTime date)
        {
            this.date = date;
        }
        public async Task Connect()
        {
            await Task.Run(() =>
            {
                for(int i = 1; i < events.Count; i++)
                {
                    if((events[i-1].shortname == events[i].shortname) && (events[i-1].timeIndex + 1 == events[i].timeIndex) && (events[i-1].type == events[i].type) && (events[i-1].leader == events[i].leader))
                    {
                        events[i - 1].stop = events[i].stop;
                        events[i - 1].timeIndex = events[i].timeIndex;
                        events.RemoveAt(i--);
                    }
                }
            });
        }
        public override string ToString()
        {
            string result = String.Empty;
            events.ForEach(x => result += x.longname + " | " + x.type + " | " + x.start.ToString() + "\n");
            return result;
        }
    }
}
