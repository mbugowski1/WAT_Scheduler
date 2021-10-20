using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace WAT_Planner
{
    class Program
    {
        static void Main()
        {
            var service = HostFactory.Run(x =>
            {
                x.Service<Service>(s =>
                {
                    s.ConstructUsing(ser => new Service());
                    s.WhenStarted(ser => ser.Start());
                    s.WhenStopped(ser => { });
                });
                x.RunAsLocalSystem();
            });
            int exitCodeValue = (int)Convert.ChangeType(service, service.GetTypeCode());
            Environment.ExitCode = exitCodeValue;
        }
    }
}
