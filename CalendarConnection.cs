using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2.Responses;
using System.Diagnostics;

namespace WAT_Planner
{
    class CalendarConnection
    {
        static CalendarService service;
        public readonly string calendarId;
        public readonly string name;
        CalendarConnection(string id, string group)
        {
            calendarId = service.Calendars.Get(id).Execute().Id;
            this.name = group;
        }
        public async static Task<CalendarConnection> GetCalendars(string group)
        {
            var groups = new List<string>();
            groups.Add(group);
            return (await GetCalendars(groups))[0];
        }
        public async static Task<List<CalendarConnection>> GetCalendars(List<string> groups)
        {
            List<string> nameList = new List<string>(groups);
            CalendarList calendarsList = await service.CalendarList.List().ExecuteAsync();
            List<CalendarConnection> calendars = new List<CalendarConnection>();
            foreach (CalendarListEntry entry in calendarsList.Items)
            {
                for (int i = 0; i < nameList.Count; i++)
                    if (entry.Summary == nameList[i])
                    {
                        calendars.Add(new CalendarConnection(entry.Id, entry.Summary));
                        nameList.RemoveAt(i);
                    }
            }
            for (int i = 0; i < nameList.Count; i++)
                calendars.Add(new CalendarConnection(await Create(nameList[i]), nameList[i]));
            return calendars;
        }
        public async static Task Connect()
        {
            UserCredential credential;
            string[] scopes = { CalendarService.Scope.Calendar };

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                try
                {
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        scopes,
                        "WAT Plan",
                        CancellationToken.None,
                        new FileDataStore(credPath, true));
                } catch (TokenResponseException e)
                {
                    throw new NotImplementedException();
                }
                Console.WriteLine("Credential file saved to: " + credPath);
            }
            // Create Google Calendar API service.
            service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "WAT Plan",
            });
        }
        IList<Event> GetEvents()
        {
            Events events = service.Events.List(calendarId).Execute();
            return events.Items;
        }
        public void Update(Schedule schedule)
        {
            List<Event> events = new List<Event>(GetEvents());
            events.Sort(delegate (Event a, Event b)
            {
                if (a.Start.DateTime == b.Start.DateTime) return 0;
                else if (a.Start.DateTime > b.Start.DateTime) return 1;
                else return -1;
            });
            List<Entry> sched = schedule.ToOneList();
            int j = 0;
            for (int i = 0; i < sched.Count; i++)
            {
                if (i >= events.Count)
                {
                    Debug.WriteLine("Insert " + sched[i].longname + " " + sched[i].start.ToString());
                    service.Events.Insert(CreateEvent(sched[i]), calendarId).Execute();
                    j++;
                    Thread.Sleep(500);
                }
                else if(sched[i].start == events[j].Start.DateTime)
                {
                    Event local = CreateEvent(sched[i]);
                    Event online = events[j];
                    if ((local.Description != online.Description) || (online.Summary != local.Summary) || (online.Location != local.Location))
                    {
                        Debug.WriteLine("Update " + sched[i].longname + " " + sched[i].start.ToString());
                        service.Events.Update(local, calendarId, online.Id).Execute();
                        Thread.Sleep(500);
                    }
                    j++;
                }
                else if (sched[i].start > events[j].Start.DateTime) //data w edziekanacie jest pozniej
                {
                    Debug.WriteLine("Remove " + events[j].Summary + " " + events[j].Start.ToString());
                    service.Events.Delete(calendarId, events[j].Id).Execute();
                    events.RemoveAt(j);
                    i--;
                    Thread.Sleep(500);
                    continue;
                }
                else //data w edziekanacie jest wczesniej
                {
                    Debug.WriteLine("Insert " + sched[i].longname + " " + sched[i].start.ToString());
                    service.Events.Insert(CreateEvent(sched[i]), calendarId).Execute();
                    Thread.Sleep(500);
                    continue;
                }
            }
            while (j < events.Count)
            {
                Debug.WriteLine("Remove " + events[j].Summary + " " + events[j].Start.ToString());
                service.Events.Delete(calendarId, events[j++].Id).Execute();
                Thread.Sleep(500);
            }
        }
        Event CreateEvent(Entry inject)
        {
            Event entry = new Event();
            entry.Summary = inject.shortname + " (" + inject.type + ")";
            if (inject.room != "-")
                entry.Location = inject.room;
            entry.Description = inject.longname + "\n" + inject.leader;
            EventDateTime start = new EventDateTime();
            start.DateTime = inject.start;
            start.TimeZone = "Europe/Warsaw";
            EventDateTime stop = new EventDateTime();
            stop.DateTime = inject.stop;
            stop.TimeZone = "Europe/Warsaw";
            entry.Start = start;
            entry.End = stop;
            return entry;
        }
        async static Task<string> Create(string name)
        {
            Calendar calendar = new Calendar();
            calendar.Summary = name;
            calendar = await service.Calendars.Insert(calendar).ExecuteAsync();
            return calendar.Id;
        }
    }
}
