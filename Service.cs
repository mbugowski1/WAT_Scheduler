using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WAT_Planner
{
    class Service
    {
        public static readonly int weekCount = 22;
        public static readonly string homeName = "wat_plan";
        public static readonly string keyName = "key.wat";
        public static readonly string passwordFile = "password.wat";
        public static readonly string loginFile = "login.wat";
        static string password = "";
        static string login = "";

        public static string strona;
        //public static string[] groups = { "WCY19IJ4S1", "WCY19IG1S1", "WCY19KC1S1" };
        public static string[] groups = { "WCY20IB1S4" };
        Schedule[] schedules;
        CalendarConnection[] calendars;
        public void Start()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Thread t = new Thread(new ThreadStart(Sync));
            t.Start();
            //Encoding en = EncodingProvider.GetEncoding("iso-8859-2");
        }
        public void Sync()
        {
            Load().Wait();
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
            Console.WriteLine("Done");
            Console.Read();
        }
        async Task Load()
        {
            Password pass = new Password();
            login = Encoding.UTF8.GetString(Password.Load(loginFile));
            password = pass.decrypt(Password.Load(passwordFile));
            strona = Encoding.GetEncoding("iso-8859-2").GetString(Password.LoadA("C:/Users/Michal/Desktop/edziekanat.html"));
            List<Schedule> scheduleList = new List<Schedule>();
            Task wat = Task.Run(async () =>
            {
                Page page = new Page();
                await page.Work(login, password);
                List<Task<Schedule>> scheduleTask = new List<Task<Schedule>>();
                foreach(string group in groups)
                    scheduleTask.Add(page.LoadSchedule(group, 2021, 1));
                while(scheduleTask.Count > 0)
                {
                    Task<Schedule> finished = await Task.WhenAny<Schedule>(scheduleTask.ToArray());
                    scheduleList.Add(finished.Result);
                    scheduleTask.Remove(finished);
                }

            });
            Task google = Task.Run(async () =>
            {
                await CalendarConnection.Connect();
                calendars = await CalendarConnection.GetCalendars(groups);
            });
            await Task.WhenAll(wat, google);
            schedules = scheduleList.ToArray();
        }
        static void LoadPassword()
        {
            using (Password test = new Password())
            {
                byte[] loginByte = Password.Load(loginFile);
                if (loginByte == null)
                {
                    Console.WriteLine("Proszę wprowadzić login do konta e-dziekanat WCY WAT: ");
                    login = Console.ReadLine();
                    if (login == "") return;
                    Password.Write(Encoding.ASCII.GetBytes(login), loginFile);
                }
                else
                    login = Encoding.ASCII.GetString(loginByte);
                byte[] passwordEncrypted = Password.Load(passwordFile);
                if (passwordEncrypted == null)
                {
                    Console.WriteLine("Proszę wprowadzić hasło do konta e-dziekanat WCY WAT: ");
                    password = Console.ReadLine();
                    if (password == "") return;
                    byte[] encrypted = test.encrypt(password);
                    Password.Write(encrypted, passwordFile);
                }
                else
                    password = test.decrypt(passwordEncrypted);
            }
        }
    }
}
