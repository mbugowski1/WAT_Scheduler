/*using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Topshelf;
using System.Linq;
using System.Collections.Generic;

namespace WAT_Planner
{
    class Service : ServiceControl
    {
        Config config = new Config(Data.ConfigPath);
        public bool Start(HostControl hostControl)
        {
            new Thread(new ParameterizedThreadStart(Worker)).Start(hostControl);
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            return true;
        }
        (string, string) LoadPassword()
        {
            if (!config.GetFirstString("Login", out string login))
                throw new ArgumentNullException("Login was not defined");
            string password;
            using (Password passwordReader = new Password())
            {
                password = passwordReader.decrypt(Password.Load(Data.PasswordPath));
            }
            return (login, password);
        }
        public async void Worker(object hostControl) 
        {
            /*if(!config.GetString("Groups", out string[] groups))
            {
                //TODO LOG
                ((HostControl)hostControl).Stop();
            }*/
            string[] groups = { "WCY19IG1S1", "WCY19IJ4S1", "WCY19KC1S1" };
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Task<CalendarConnection[]> calendarsTask = Calendar(groups);
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
            schedules = new Schedule[] { await page.LoadSchedule(groups[0], 2021, 1) };
            //WRONG PASSWORD EXCEPTION
            CalendarConnection[] calendars = await calendarsTask;
            foreach (Schedule schedule in schedules)
            {
                for (int i = 0; i < calendars.Length; i++)
                {
                    if (calendars[i].group == schedule.name)
                    {
                        calendars[i].Update(schedule);
                        break;
                    }
                }
            }
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
        async Task<CalendarConnection[]> Calendar(string[] groups)
        {
            try
            {
                await CalendarConnection.Connect();
            }catch(Google.Apis.Auth.OAuth2.Responses.TokenResponseException)
            {
                throw new NotImplementedException();
            }
            return await CalendarConnection.GetCalendars(groups);
        }
    }
}
*/