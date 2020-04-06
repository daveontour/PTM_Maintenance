using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Messaging;
using Topshelf;

//Release Candidate 2.0

namespace Departure_PTM_Widget
{
    class Program
    {

        // Primarily skeleton code for defining the ConsoleApp/Service to be managed by TopShelf
        static void Main()
        {
            if (!MessageQueue.Exists(Parameters.RECVQ))
            {
                Console.WriteLine("======================================");
                Console.WriteLine("= Departure Flight PTM Updater Error ");
                Console.WriteLine($"= The configured notification queue, {Parameters.RECVQ}, does not exist");
                Console.WriteLine("=");
                Console.WriteLine("= Service Not Started ");
                Console.WriteLine("======================================");

                Environment.ExitCode = -1;
                return;
            }

            var exitCode = HostFactory.Run(x =>
            {
                /*
                 * The 'PTMController' class is the class that provides the actual functionality.
                 * The two key methods that Exchange has to implement are "Start()" and "Stop()"
                 */

                x.Service<PTMController>(s =>
                {
                    s.ConstructUsing(core => new PTMController());
                    s.WhenStarted(core => core.Start());
                    s.WhenStopped(core => core.Stop());
                });

                x.RunAsLocalSystem();
                x.StartAutomatically();
                x.EnableServiceRecovery(rc =>
                {
                    rc.RestartService(1); // restart the service after 1 minute
                });

                /*
                 * Get any customisation for the Service Name and description from the configuration file
                 * This is useful is multiple instance of the service are run from different directories
                 */
                NameValueCollection appSettings = ConfigurationManager.AppSettings;
                string serviceName = string.IsNullOrEmpty(appSettings["ServiceName"]) ? $"SITA MEIA - Departure PTM {Parameters.VERSION}" : appSettings["ServiceName"];
                string serviceDisplayName = string.IsNullOrEmpty(appSettings["ServiceDisplayName"]) ? $"SITA MEIA - Departure PTM Service ({Parameters.VERSION})" : appSettings["ServiceDisplayName"];
                string serviceDescription = string.IsNullOrEmpty(appSettings["ServiceDescription"]) ? "Updates passenger transfer details of departing flights based on the PTMs applied to the arriving flight" : appSettings["ServiceDescription"];

                x.SetServiceName(serviceName);
                x.SetDisplayName(serviceDisplayName);
                x.SetDescription(serviceDescription);
            });

            int exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
            Environment.ExitCode = exitCodeValue;
        }

    }
}
