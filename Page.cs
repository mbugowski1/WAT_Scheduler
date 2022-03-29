using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace WAT_Planner
{
    class Page
    {

        readonly HttpClient client = new HttpClient();
        public int[,] startHour = new int[2, 7];
        public int[,] endHour = new int[2, 7];

        public int weekCount;
        string session;

        public Page(string login, string password)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Work(login, password).Wait();
        }

        async Task Work(String login, String password)
        {
            session = await GetSession();
            await LogIn(login, password);
        }
        async Task<String> GetSession()
        {
            HttpResponseMessage response = await client.GetAsync("https://s1.wcy.wat.edu.pl/ed1/");
            if (response.IsSuccessStatusCode)
            {
                String text = await response.Content.ReadAsStringAsync();
                int index = text.IndexOf("index.php?sid=");
                text = text.Substring(index + 14);
                index = text.IndexOf('>');
                text = text.Substring(0, index);
                return text;
            }
            else
                throw new HttpRequestException();
        }
        async Task LogIn(String login, String password)
        {
            MultipartFormDataContent test = new MultipartFormDataContent();
            test.Add(new StringContent(login), "userid");
            test.Add(new StringContent(password), "password");
            test.Add(new StringContent("login"), "formname");
            HttpResponseMessage response = await client.PostAsync("https://s1.wcy.wat.edu.pl/ed1/index.php?sid=" + session, test);
        }
        public async Task<List<String>> LoadGroups(int year, int semester)
        {
            String group;
            List<String> groups = new List<String>();
            HttpResponseMessage response = await client.GetAsync($"https://s1.wcy.wat.edu.pl/ed1/logged_inc.php?sid={session}&mid=328&iid={year}{semester}");
            response.EnsureSuccessStatusCode();
            String text = await response.Content.ReadAsStringAsync();
            int tdIndex = text.IndexOf("tdGrayWhite");
            while (tdIndex > 0)
            {
                int checkIndex = text.IndexOf('>', tdIndex) + 1;
                text = text.Substring(checkIndex);
                if (text.Substring(0, 5) == "&nbsp")
                {
                    tdIndex = text.IndexOf("tdGrayWhite");
                    continue;
                }
                int aIndex = text.IndexOf("<a");
                int firstIndex = text.IndexOf('>', aIndex) + 1;
                int lastIndex = text.IndexOf('<', firstIndex);
                group = text.Substring(firstIndex, lastIndex - firstIndex);
                groups.Add(group);
                tdIndex = text.IndexOf("tdGrayWhite");
                Console.WriteLine(group);
            }
            return groups;

        }
        void LoadTime(string content)
        {
            int search = "tdFormList1DSheTeaGrpHTM1".Length;
            for (int i = 0; i < 7; i++)
            {
                int tdIndex = content.IndexOf("tdFormList1DSheTeaGrpHTM1");
                content = content.Substring(tdIndex + search);
                tdIndex = content.IndexOf("tdFormList1DSheTeaGrpHTM1");
                content = content.Substring(tdIndex + search);
                int cutting = content.IndexOf("<nobr>") + 6;
                string cut = content.Substring(cutting, 5);
                startHour[0, i] = Int32.Parse(cut.Substring(0, 2));
                startHour[1, i] = Int32.Parse(cut.Substring(3, 2));
                cutting = content.IndexOf("<br>", cutting) + 4;
                cut = content.Substring(cutting, 5);
                endHour[0, i] = Int32.Parse(cut.Substring(0, 2));
                endHour[1, i] = Int32.Parse(cut.Substring(3, 2));
            }
        }
        DateTime GetStartDate(string content, int year)
        {
            int tdIndex = content.IndexOf("thFormList1HSheTeaGrpHTM3");
            string cut = content.Substring(content.IndexOf("<nobr>", tdIndex) + 6, 8);
            int day = Int32.Parse(cut.Substring(0, 2));
            string monthString = cut.Substring(6, 2);
            if (monthString[1] == '<')
                monthString = monthString.Substring(0, 1);
            int month;
            switch (monthString)
            {
                case "I":
                    month = 1;
                    break;
                case "II":
                    month = 2;
                    break;
                case "III":
                    month = 3;
                    break;
                case "IV":
                    month = 4;
                    break;
                case "V":
                    month = 5;
                    break;
                case "VI":
                    month = 6;
                    break;
                case "VII":
                    month = 7;
                    break;
                case "VIII":
                    month = 8;
                    break;
                case "IX":
                    month = 9;
                    break;
                case "X":
                    month = 10;
                    break;
                case "XI":
                    month = 11;
                    break;
                case "XII":
                    month = 12;
                    break;
                default:
                    month = 1;
                    break;
            }
            return new DateTime(year, month, day);
        }
        public int LoadWeeks(string content)
        {
            int counter = -1;
            int pos = -1;
            do
            {
                pos = content.IndexOf("thFormList1HSheTeaGrpHTM3", pos + 1);
                counter++;
            } while (pos != -1);
            return counter;
        }
        public async Task<Schedule> LoadSchedule(string group, int year, int semester, string name, String strona)
        {
            //Ładowanie dat i czasów
            LoadTime(strona);
            var startDate = GetStartDate(strona, year);
            weekCount = LoadWeeks(strona);


            Schedule schedule = new Schedule(group, year, semester, name, startDate);

            int searchLength = "tdFormList1DSheTeaGrpHTM3".Length;
            int tdIndex = strona.IndexOf("tdFormList1DSheTeaGrpHTM3");
            int dayCounter = 0, hourCounter = 0, weekCounter = 0;
            while (tdIndex > 0)
            {
                //Odcięcie niepotrzebnej części strony
                strona = strona.Substring(tdIndex);

                //Obsłużenie sytuacji, w której komórka jest pusta
                int checkIndex = strona.IndexOf('>') + 1;
                if (strona.Substring(checkIndex, 5) == "&nbsp")
                {
                    tdIndex = strona.IndexOf("tdFormList1DSheTeaGrpHTM3", searchLength);
                    weekCounter++;
                    if (weekCounter == weekCount)
                    {
                        hourCounter += 1;
                        weekCounter = 0;
                    }
                    if (hourCounter == 7)
                    {
                        dayCounter += 1;
                        hourCounter = 0;
                    }
                    continue;
                }

                //Tworzenienie wydarzenia i ładowanie danych z nazwy klasy
                Entry entry = new Entry();
                int titleStart = strona.IndexOf("title") + 7;
                int titleEnd = strona.IndexOf('"', titleStart);
                string title = strona.Substring(titleStart, titleEnd - titleStart);
                int titleDivide = title.IndexOf('-');
                int typeStart = title.IndexOf('(');
                int typeStop = title.IndexOf(')');
                entry.type = title.Substring(typeStart + 1, typeStop - typeStart - 1);
                entry.longname = title.Substring(0, titleDivide - 1);
                entry.leader = title.Substring(titleDivide + 2, typeStart - titleDivide - 3);

                //Ładowanie danych dodatkowych
                int tdAdditionalIndex = strona.IndexOf("<b style");
                int start = strona.IndexOf('>', tdAdditionalIndex) + 1;
                int stop = strona.IndexOf('<', start + 1);
                entry.shortname = strona.Substring(start, stop - start);
                tdAdditionalIndex = strona.IndexOf("<b style", stop);
                start = strona.IndexOf('>', tdAdditionalIndex) + 1;
                stop = strona.IndexOf('<', start + 1);
                entry.shortType = strona.Substring(start, stop - start);
                tdAdditionalIndex = strona.IndexOf(")<br>") + 5;
                stop = strona.IndexOf('<', tdAdditionalIndex);
                entry.room = strona.Substring(tdAdditionalIndex, stop - tdAdditionalIndex);

                tdIndex = strona.IndexOf("tdFormList1DSheTeaGrpHTM3", searchLength);

                //Obliczanie czasu wydarzenia
                DateTime time = startDate.AddDays(weekCounter * 7 + dayCounter);
                entry.start = new DateTime(time.Year, time.Month, time.Day, startHour[0, hourCounter], startHour[1, hourCounter], 0);
                entry.timeIndex = hourCounter;
                entry.stop = new DateTime(time.Year, time.Month, time.Day, endHour[0, hourCounter], endHour[1, hourCounter], 0);
                weekCounter++;
                if (weekCounter == weekCount)
                {
                    hourCounter += 1;
                    weekCounter = 0;
                }
                if (hourCounter == 7)
                {
                    dayCounter += 1;
                    hourCounter = 0;
                }
                ScheduleDay day = schedule.Find(time.Date);
                day.events.Add(entry);
            }
            foreach (var day in schedule.days)
            {
                await day.Connect();
            }
            schedule.days.Sort(delegate (ScheduleDay a, ScheduleDay b)
            {
                if (a.date == b.date) return 0;
                else if (a.date > b.date) return 1;
                else return -1;
            });
            return schedule;
        }
        public async Task<Schedule> LoadSchedule(string group, int year, int semester, string name)
        {

            //Pobieranie strony z kalendarzem
            HttpResponseMessage response;
            if (semester == 1)
                response = await client.GetAsync($"https://s1.wcy.wat.edu.pl/ed1/logged_inc.php?sid={session}&mid=328&iid={year}{semester+3}&exv={group}");
            else
                response = await client.GetAsync($"https://s1.wcy.wat.edu.pl/ed1/logged_inc.php?sid={session}&mid=328&iid={year - 1}{semester + 3}&exv={group}");
            response.EnsureSuccessStatusCode();
            String text = await response.Content.ReadAsStringAsync();
            //String text = Program.strona;

            //Ładowanie dat i czasów
            LoadTime(text);
            var startDate = GetStartDate(text, year);
            weekCount = LoadWeeks(text);

            Schedule schedule = new Schedule(group, year, semester, name, startDate);

            int searchLength = "tdFormList1DSheTeaGrpHTM3".Length;
            int tdIndex = text.IndexOf("tdFormList1DSheTeaGrpHTM3");
            int dayCounter = 0, hourCounter = 0, weekCounter = 0;
            while (tdIndex > 0)
            {
                //Odcięcie niepotrzebnej części strony
                text = text.Substring(tdIndex);

                //Obsłużenie sytuacji, w której komórka jest pusta
                int checkIndex = text.IndexOf('>') + 1;
                if (text.Substring(checkIndex, 5) == "&nbsp")
                {
                    tdIndex = text.IndexOf("tdFormList1DSheTeaGrpHTM3", searchLength);
                    weekCounter++;
                    if(weekCounter == weekCount)
                    {
                        hourCounter += 1;
                        weekCounter = 0;
                    }
                    if(hourCounter == 7)
                    {
                        dayCounter += 1;
                        hourCounter = 0;
                    }
                    continue;
                }

                //Tworzenienie wydarzenia i ładowanie danych z nazwy klasy
                Entry entry = new Entry();
                int titleStart = text.IndexOf("title") + 7;
                int titleEnd = text.IndexOf('"', titleStart);
                string title = text.Substring(titleStart, titleEnd - titleStart);
                int titleDivide = title.IndexOf('-');
                int typeStart = title.IndexOf('(', titleDivide);
                int typeStop = title.IndexOf(')', typeStart);
                entry.type = title.Substring(typeStart + 1, typeStop - typeStart - 1);
                entry.longname = title.Substring(0, titleDivide - 1);
                entry.leader = title.Substring(titleDivide + 2, typeStart - titleDivide - 3);

                //Ładowanie danych dodatkowych
                int tdAdditionalIndex = text.IndexOf("<b style");
                int start = text.IndexOf('>', tdAdditionalIndex) + 1;
                int stop = text.IndexOf('<', start + 1);
                entry.shortname = text.Substring(start, stop - start);
                tdAdditionalIndex = text.IndexOf("<b style", stop);
                start = text.IndexOf('>', tdAdditionalIndex) + 1;
                stop = text.IndexOf('<', start + 1);
                entry.shortType = text.Substring(start, stop - start);
                tdAdditionalIndex = text.IndexOf(")<br>") + 5;
                stop = text.IndexOf('<', tdAdditionalIndex);
                entry.room = text.Substring(tdAdditionalIndex, stop - tdAdditionalIndex);

                tdIndex = text.IndexOf("tdFormList1DSheTeaGrpHTM3", searchLength);

                //Obliczanie czasu wydarzenia
                DateTime time = startDate.AddDays(weekCounter * 7 + dayCounter);
                entry.start = new DateTime(time.Year, time.Month, time.Day, startHour[0, hourCounter], startHour[1, hourCounter], 0);
                entry.timeIndex = hourCounter;
                entry.stop = new DateTime(time.Year, time.Month, time.Day, endHour[0, hourCounter], endHour[1, hourCounter], 0);
                weekCounter++;
                if (weekCounter == weekCount)
                {
                    hourCounter += 1;
                    weekCounter = 0;
                }
                if (hourCounter == 7)
                {
                    dayCounter += 1;
                    hourCounter = 0;
                }
                ScheduleDay day = schedule.Find(time.Date);
                day.events.Add(entry);
            }
            foreach(var day in schedule.days)
            {
                await day.Connect();
            }
            schedule.days.Sort(delegate (ScheduleDay a, ScheduleDay b)
            {
                if (a.date == b.date) return 0;
                else if (a.date > b.date) return 1;
                else return -1;
            });
            return schedule;
        }
        public void link()
        {
            Console.WriteLine($"https://s1.wcy.wat.edu.pl/ed1/logged_inc.php?sid={session}");
        }
        ~Page()
        {
            client.GetAsync($"https://s1.wcy.wat.edu.pl/ed1/logged_inc.php?sid={session}&lou=1").Wait();
            client.Dispose();
        }
    }
}
