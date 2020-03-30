using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Threading;
using System.Xml;

namespace AUH_PTM_Widget
{
    class PTMController
    {
        static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private static MessageQueue recvQueue;  // Queue to recieve update notifications on
        private bool startListenLoop = true;    // Flag controlling the execution of the update notificaiton listener

        public bool stopProcessing = false;
        public Thread startThread;
        private Thread receiveThread;           // Thread the notification listener runs in 
        private readonly static Random random = new Random();

        private ArrivalChangeClassifier arrivalClassifier = new ArrivalChangeClassifier();
        private FlightUpdater flightUpdater = new FlightUpdater();

        public PTMController() { }

        /*
         * Method called by the TopShelf Skeleton to run the program
         */
        public bool Start()
        {
            logger.Info($"Departure PTM Service Starting ({Parameters.VERSION})");
            stopProcessing = false;
            startThread = new Thread(new ThreadStart(StartThread));
            startThread.Start();
            logger.Info($"Departure PTM Service Started ({Parameters.VERSION})");
            return true;
        }


        /*
        * Method called by the TopShelf Skeleton to terminate the program
        */
        public void Stop()
        {
            logger.Info("Departure PTM Service Stopping");
            stopProcessing = true;
            startListenLoop = false;
            logger.Info("Departure PTM Service Stopped");
        }

        public void StartThread()
        {

            logger.Info($"Departure PTM Service Initialisation Starting ({Parameters.VERSION})");

            //Start Listener for incoming flight notifications
            recvQueue = new MessageQueue(Parameters.RECVQ);
            if (StartMQListener())
            {
                logger.Info($"Started Notification Queue Listener on queue: {Parameters.RECVQ}");
                logger.Info($"Departure PTM Service Initialisation Completed ({Parameters.VERSION})");
            }
            else
            {
                logger.Error($"Error starting the notification queue listener");
                logger.Error($"Departure PTM Service Initialisation NOT Started");
            }
        }

        // Start the thread to listen to incoming update notifications
        public bool StartMQListener()
        {

            try
            {
                if (!MessageQueue.Exists(Parameters.RECVQ))
                {
                    logger.Warn($"The configured notification queue, {Parameters.RECVQ}, does not exist");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.StackTrace);
                return false;
            }


            try
            {
                this.startListenLoop = true;
                receiveThread = new Thread(this.ListenToQueue)
                {
                    IsBackground = false
                };
                receiveThread.Start();
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Error starting notification queue listener");
                logger.Error(ex.Message);
                return false;
            }
        }

        private void ListenToQueue()
        {
            while (startListenLoop)
            {

                //Put it in a Try/Catch so on bad message or reading problem dont stop the system
                try
                {
                    logger.Trace("Waiting for notification message");
                    using (Message msg = recvQueue.Receive(new TimeSpan(0, 0, 5)))
                    {

                        logger.Trace("Message Received");
                        string xml;
                        using (StreamReader reader = new StreamReader(msg.BodyStream))
                        {
                            xml = reader.ReadToEnd();
                        }
                        ProcessMessage(xml, RandomString(10));
                    }
                }
                catch (MessageQueueException e)
                {
                    // Handle no message arriving in the queue.
                    if (e.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    {
                        if (Parameters.DEEPTRACE)
                        {
                            logger.Trace("DEEP TRACE ===>> No Message Recieved in Notification Queue <<==== DEEP TRACE");

                        }
                    }

                    // Handle other sources of a MessageQueueException.
                }
                catch (Exception e)
                {
                    logger.Error("Error in Recieving and Processing Notification Message");
                    logger.Error(e.Message);
                    Thread.Sleep(Parameters.RESTSERVER_RETRY_INTERVAL);
                }
            }
            logger.Info("Queue Listener Stopped");
            receiveThread.Abort();
        }

        public void ProcessMessage(string xml, string id)
        {

            logger.Trace($"Processing Message  {id}");

            try
            {

                if (Parameters.DEEPTRACE)
                {
                    logger.Trace("DEEP TRACE ===>>");
                    logger.Trace($"\n{xml}");
                    logger.Trace("<< ==== DEEP TRACE");
                }


                if (xml.Contains("FlightUpdatedNotification"))
                {

                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(xml);
                    XmlNode xmlRoot = doc.DocumentElement;
                    Tuple<bool, FlightNode, XmlNode> tuple = arrivalClassifier.Classify(xmlRoot);

                    if (!tuple.Item1 || tuple.Item2 == null || tuple.Item3 == null)
                    {
                        logger.Trace($"No Data to process: Message ID {id}");
                    }

                    // Get a list for each of the additions, updates and deletions.
                    Tuple<List<PTMRow>, List<PTMRow>, List<PTMRow>> crud = arrivalClassifier.ClassifyEntries(tuple.Item3);

                    if (crud == null)
                    {
                        logger.Trace($"Error Identifying Changes in Transfer Loads: Message ID {id}");
                    }
                    else
                    {
                        if (Parameters.PROCESS_ADDS) ProcessAdditions(tuple.Item2, crud.Item1);
                        if (Parameters.PROCESS_UPDATES) ProcessUpdates(tuple.Item2, crud.Item2);
                        if (Parameters.PROCESS_DELETES) ProcessDeletes(tuple.Item2, crud.Item3);
                    }

                    return;
                }

                logger.Trace($"Not a FlightUpdatedNotification Message: Message ID {id}");
            }
            catch (Exception e)
            {
                logger.Trace($"Message Processing Error {id}. See Contents Below");
                logger.Trace(e.Message);

                if (Parameters.DEEPTRACE)
                {
                    logger.Trace("DEEP TRACE ===>>");
                    logger.Trace($"\n{xml}");
                    logger.Trace("<< ==== DEEP TRACE");
                }
            }
        }

        private async void ProcessDeletes(FlightNode arrFlight, List<PTMRow> deleteList)
        {

            foreach (PTMRow ptm in deleteList)
            {
                bool result = await flightUpdater.RemovePTMFromDepartureFlight(arrFlight, new FlightNode(ptm, "Departure", arrFlight.airportCodeIATA));
                if (result)
                {
                    logger.Trace($"PTM Record Removed Sucess {arrFlight.ToString()}, {ptm.flightKey}");
                }
                else
                {
                    logger.Trace($"PTM Record Removed Failure {arrFlight.ToString()}, {ptm.flightKey}");
                }
            }
        }

        private void ProcessUpdates(FlightNode arrFlight, List<PTMRow> updateList)
        {
            return;
        }

        private async void ProcessAdditions(FlightNode arrFlight, List<PTMRow> additionsList)
        {
            foreach (PTMRow ptm in additionsList)
            {
                bool result = await flightUpdater.AddPTMToDepartureFlight(arrFlight, new FlightNode(ptm, "Departure", arrFlight.airportCodeIATA));
                if (result)
                {
                    logger.Trace($"PTM Record Removed Sucess {arrFlight.ToString()}, {ptm.flightKey}");
                }
                else
                {
                    logger.Trace($"PTM Record Removed Failure {arrFlight.ToString()}, {ptm.flightKey}");
                }
            }
        }



        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
}
