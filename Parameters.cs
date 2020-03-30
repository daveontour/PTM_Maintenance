using System;
using System.Configuration;

//Version RC 1.0

namespace AUH_PTM_Widget
{

    /*
     * Class to make the configuration parameters available. 
     * The static constructor makes sure the parameters are initialised the first time the 
     * class is accessed
     * 
     * 
     */
    public class Parameters
    {

        static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static string TOKEN;
        public static string BASE_URI;
        //public static string APT_CODE;
        public static string RECVQ;
        //public static string ALERT_FIELD;
        //public static string APT_CODE_ICAO;
        //public static int REFRESH_INTERVAL;
        //public static int GRACE_PERIOD;
        //public static double FROM_HOURS;
        //public static double TO_HOURS;
        public static int RESTSERVER_RETRY_INTERVAL;
        //public static bool STARTUP_FLIGHT_PROCESSING;
        public static string VERSION = "Version 1.0, 20200323";
        public static bool DEEPTRACE;

        public static bool PROCESS_ADDS;
        public static bool PROCESS_UPDATES;
        public static bool PROCESS_DELETES;

        static Parameters()
        {
            try
            {
                TOKEN = (string)ConfigurationManager.AppSettings["Token"];
                BASE_URI = (string)ConfigurationManager.AppSettings["BaseURI"];
                RECVQ = (string)ConfigurationManager.AppSettings["NotificationQueue"];
                //ALERT_FIELD = (string)ConfigurationManager.AppSettings["AlertField"];
                //GRACE_PERIOD = Int32.Parse((string)ConfigurationManager.AppSettings["GracePeriod"]);
                //REFRESH_INTERVAL = Int32.Parse((string)ConfigurationManager.AppSettings["RefreshInterval"]);
                RESTSERVER_RETRY_INTERVAL = Int32.Parse((string)ConfigurationManager.AppSettings["ResetServerRetryInterval"]);
                //FROM_HOURS = double.Parse((string)ConfigurationManager.AppSettings["FromHours"]);
                //TO_HOURS = double.Parse((string)ConfigurationManager.AppSettings["ToHours"]);
                //STARTUP_FLIGHT_PROCESSING = bool.Parse((string)ConfigurationManager.AppSettings["StartUpFlightProcessing"]);
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
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }


    }
}
