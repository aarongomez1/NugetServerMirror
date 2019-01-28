using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NuGetServerMirror
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
           

            if(args.Length == 1 && args[0] == "-local")
            {
                var service = new MirrorService();
                service.Go();

                while(true)
                {
                    Thread.Sleep(1);
                }
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new MirrorService()
                };

                if (args.Length == 1 && args[0] == "-i")
                {
                    MirrorService.Install();
                    return;
                }

                ServiceBase.Run(ServicesToRun);
            }
            
            

        }
    }
}
