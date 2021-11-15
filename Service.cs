using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Topshelf;

namespace WAT_Planner
{
    class Service : ServiceControl
    {
        public bool Start(HostControl hostControl)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            new Thread(new ParameterizedThreadStart(Worker)).Start(hostControl);
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            return true;
        }
        (string, string) LoadPassword()
        {
            string login = Encoding.UTF8.GetString(Password.Load("login.wat"));
            string password;
            using (Password passwordReader = new Password())
            {
                password = passwordReader.decrypt(Password.Load("password.wat"));
            }
            return (login, password);
        }
        public async void Worker(object hostControl) 
        {
            //string[] groups = { "WCY19IG1S1", "WCY19IJ4S1", "WCY19KC1S1" };
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string[] groups = { "WCY19IJ4S1" };
            //Task<CalendarConnection[]> calendarsTask = Calendar();
            Schedule[] schedules;

            (string, string) credentials;
            try
            {
                credentials = LoadPassword();
            }
            catch (System.Security.Cryptography.CryptographicException e)
            {
                Debug.WriteLine("ERROR! " + e.Message);
                ((HostControl)hostControl).Stop();
                return;
            }
            Page page = new Page(credentials.Item1, credentials.Item2);
            credentials.Item1 = null;
            credentials.Item2 = null;
            schedules = new Schedule[] { await page.LoadSchedule(groups[0], 2021, 1, Encoding.GetEncoding("ISO-8859-2").GetString(Password.LoadFile("C:/Users/Michal/Desktop/J4.html"))) };
            //CalendarConnection[] calendars = await calendarsTask;
            Console.WriteLine(schedules.Length);
            foreach (Schedule schedule in schedules)
                Console.WriteLine(schedule.ToString(15, 11));
            /*foreach (Schedule schedule in schedules)
            {
                for (int i = 0; i < calendars.Length; i++)
                {
                    if (calendars[i].group == schedule.name)
                    {
                        calendars[i].Update(schedule);
                        break;
                    }
                }
            }*/
            ((HostControl)hostControl).Stop();
        }

        async Task<Schedule[]> LoadWat(Page page, string[] groups)
        {
            Schedule[] schedules = new Schedule[groups.Length];
            Task<Schedule>[] scheduleTasks = new Task<Schedule>[groups.Length];
            for (int i = 0; i < groups.Length; i++)
                scheduleTasks[i] = page.LoadSchedule(groups[i], 2021, 1);
            for(int i = 0; i < groups.Length; i++)
                schedules[i] = await scheduleTasks[i];
            return schedules;
        }
        async Task<CalendarConnection[]> Calendar()
        {
            await CalendarConnection.Connect();
            return await CalendarConnection.GetCalendars(new string[] { "WCY20IB1S4" });
        }
    }
}
