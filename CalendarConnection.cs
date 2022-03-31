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
        List<Event> GetEvents(DateTime startTime, DateTime endTime)
        {
            var request = service.Events.List(calendarId);
            request.TimeMax = endTime;
            request.TimeMin = startTime;
            string token = String.Empty;
            List<Event> result = new();
            do
            {
                request.PageToken = token;
                Events events = request.Execute();
                result.AddRange(events.Items);
                token = events.NextPageToken;
            } while (token != null);
            return result;
        }
        public void ClearCalendar()
        {
            List<Event> events = GetEvents(DateTime.MinValue, DateTime.MaxValue);
            events.ForEach(e =>
            {
                Debug.WriteLine("Remove " + e.Summary + " at " + e.Start.DateTime);
                service.Events.Delete(calendarId, e.Id).Execute();
                Thread.Sleep(500);
            });
        }
        public void Update(Schedule schedule)
        {
            List<Event> events = GetEvents(schedule.StartDate, schedule.EndDate);
            events.Sort(delegate (Event a, Event b)
            {
                if (a.Start.DateTime == b.Start.DateTime) return 0;
                else if (a.Start.DateTime > b.Start.DateTime) return 1;
                else return -1;
            });
            List<Entry> sched = schedule.ToOneList();
            sched.ForEach(e =>
            {
                Event online = events.Find(x => x.Start.DateTime == e.start);
                if(online != null)
                {
                    Event local = CreateEvent(e);
                    if ((local.Description != online.Description) || (online.Summary != local.Summary) || (online.Location != local.Location) || (online.End.DateTime != local.End.DateTime))
                    {
                        Debug.WriteLine("Update " + e.longname + " " + e.start.ToString());
                        service.Events.Update(local, calendarId, online.Id).Execute();
                        Thread.Sleep(500);
                    }
                    events.Remove(online);
                }
                else
                {
                    Debug.WriteLine("Insert " + e.longname + " " + e.start.ToString());
                    service.Events.Insert(CreateEvent(e), calendarId).Execute();
                    Thread.Sleep(500);
                }
            });
            events.ForEach(e =>
            {
                Debug.WriteLine("Remove " + e.Summary + " at " + e.Start.DateTime);
                service.Events.Delete(calendarId, e.Id).Execute();
                Thread.Sleep(500);
            });
        }
        static Event CreateEvent(Entry inject)
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
            Debug.WriteLine("New Calendar: " + name);
            calendar = await service.Calendars.Insert(calendar).ExecuteAsync();
            return calendar.Id;
        }
    }
}
