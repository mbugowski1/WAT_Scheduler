using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace WAT_Planner
{
    class Program
    {
        public static void Main(String[] args)
        {
            HostFactory.New(serviceConf =>
            {
                serviceConf.Service<Service>(service =>
                {
                    service.ConstructUsing(() => new Service());
                    service.WhenStarted((body, control) => body.Start(control));
                    service.WhenStopped((body, control) => body.Stop(control));
                });
                serviceConf.RunAsLocalSystem();
            }).Run();
        }
    }
}
