using System;
using System.Configuration;

//Version RC 1.0

namespace Departure_PTM_Widget
{

    /*
     * Class to make the configuration parameters available. 
     * The static constructor makes sure the parameters are initialised the first time the 
     * class is accessed
     * 
     * 
     */
    internal class Parameters
    {

        static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        internal static string TOKEN;
        internal static string BASE_URI;
        internal static string RECVQ;
        internal static string HOME_AIRPORT_IATA;

        internal static int LISTENQUEUE_RETRY_INTERVAL;
        internal static int MIN_SEPERATION = 10;
        internal static int DISPATCHER_LOOP_INTERVAL = 1000;

        internal static string VERSION = "Version RC 1.0, 20200404";
        internal static bool DEEPTRACE;

        internal static bool PROCESS_ADDS;
        internal static bool PROCESS_UPDATES;
        internal static bool PROCESS_DELETES;
        internal static bool STO_DATETIME;

        internal static int WAIT_FOR_MESSAGE_INTERVAL;

        static Parameters()
        {
            try
            {
                TOKEN = (string)ConfigurationManager.AppSettings["Token"];
                HOME_AIRPORT_IATA = (string)ConfigurationManager.AppSettings["HOME_AIRPORT_IATA"];
                BASE_URI = (string)ConfigurationManager.AppSettings["BaseURI"];
                RECVQ = (string)ConfigurationManager.AppSettings["NotificationQueue"];
                LISTENQUEUE_RETRY_INTERVAL = Int32.Parse((string)ConfigurationManager.AppSettings["ResetServerRetryInterval"]);
                WAIT_FOR_MESSAGE_INTERVAL = Int32.Parse((string)ConfigurationManager.AppSettings["WaitForMessageInterval"]);

                MIN_SEPERATION = Int32.Parse((string)ConfigurationManager.AppSettings["MIN_SEPERATION"]);

                DISPATCHER_LOOP_INTERVAL = Int32.Parse((string)ConfigurationManager.AppSettings["DISPATCHER_LOOP_INTERVAL"]);

                try
                {
                    DEEPTRACE = bool.Parse((string)ConfigurationManager.AppSettings["DeepTrace"]);
                }
                catch (Exception)
                {
                    DEEPTRACE = false;
                }

                try
                {
                    PROCESS_ADDS = bool.Parse((string)ConfigurationManager.AppSettings["PROCESS_ADDS"]);
                }
                catch (Exception)
                {
                    PROCESS_ADDS = false;
                }

                try
                {
                    PROCESS_UPDATES = bool.Parse((string)ConfigurationManager.AppSettings["PROCESS_UPDATES"]);
                }
                catch (Exception)
                {
                    PROCESS_UPDATES = false;
                }

                try
                {
                    PROCESS_DELETES = bool.Parse((string)ConfigurationManager.AppSettings["PROCESS_DELETES"]);
                }
                catch (Exception)
                {
                    PROCESS_DELETES = false;
                }
                try
                {
                    STO_DATETIME = bool.Parse((string)ConfigurationManager.AppSettings["STO_DATETIMES"]);
                }
                catch (Exception)
                {
                    STO_DATETIME = true;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
        }
    }
}
