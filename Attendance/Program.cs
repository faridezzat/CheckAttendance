using System;
using Topshelf;

namespace Attendance
{
    class Program
    {

        static void Main(string[] args)
        {
            var exitCode = HostFactory.Run(x =>
            {

                x.Service<Service>(s =>
                {

                    s.ConstructUsing(service => new Service());
                    s.WhenStarted(service => service.start());
                    s.WhenStopped(service => service.stop());
                });


                x.RunAsLocalSystem();
                x.SetServiceName("AttendanceService");
                x.SetDisplayName("Attendance Service");
                x.SetDescription("Automatic Attendance logs for ZK created by Farid Ezzat");
            });

            int exitCodevalue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
            Environment.ExitCode = exitCodevalue;

            //  Service s = new Service();
            // Console.WriteLine(s.response());
            //   s.deserialize(s.response());
            Console.ReadLine();

        }
    }
}
