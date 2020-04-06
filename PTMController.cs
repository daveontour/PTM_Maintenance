using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

//Release Candidate 2.0

namespace Departure_PTM_Widget
{
    class PTMController : IDisposable
    {
        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private MessageQueue recvQueue;  // Queue to recieve update notifications on
        private bool startListenLoop = true;    // Flag controlling the execution of the update notificaiton listener

        private Thread startThread;
        private Thread receiveThread;           // Thread the notification listener runs in 
        private readonly Random random = new Random();

        private readonly ArrivalChangeClassifier arrivalClassifier = new ArrivalChangeClassifier();
        private readonly FlightUpdater flightUpdater = new FlightUpdater();

        public PTMController() { }

        /*
         * Method called by the TopShelf Skeleton to run the program
         */
        public bool Start()
        {
            logger.Info($"Departure PTM Service Starting ({Parameters.VERSION})");

            if (!MessageQueue.Exists(Parameters.RECVQ))
            {
                logger.Warn($"The configured notification queue, {Parameters.RECVQ}, does not exist");
                return false;
            }

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
            startListenLoop = false;
            FlightUpdater.OK_TO_RUN = false;
            logger.Info("Departure PTM Service Stopped");
        }

        private void StartThread()
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
        private bool StartMQListener()
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

            logger.Trace("Waiting for notification message");

            while (startListenLoop)
            {

                //Put it in a Try/Catch so on bad message or reading problem dont stop the system
                try
                {
                    using (Message msg = recvQueue.Receive(new TimeSpan(0, 0, Parameters.WAIT_FOR_MESSAGE_INTERVAL)))
                    {

                        logger.Trace("Message Received");
                        string xml;
                        using (StreamReader reader = new StreamReader(msg.BodyStream))
                        {
                            xml = reader.ReadToEnd();
                        }
                        _ = ProcessMessageAsync(xml, GenerateMessageID(10));
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
                    Thread.Sleep(Parameters.LISTENQUEUE_RETRY_INTERVAL);
                }
            }
            logger.Info("Queue Listener Stopped");
            receiveThread.Abort();
        }

        private async Task ProcessMessageAsync(string xml, string id)
        {

            logger.Info($"Processing Message  {id}");

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
                    Tuple<bool, FlightNode, XmlNode> tuple = arrivalClassifier.ClassifyFlightAndGetTransferChanges(xmlRoot);

                    // Item1 - bool. True if the flight is an arrival
                    // Item2 - FlightNode. Arrival Flight
                    // Item3 - XMLNode. .//ams:FlightChanges/ams:TableValueChange[@propertyName='Tl--_TransferLoads']

                    if (!tuple.Item1 || tuple.Item2 == null || tuple.Item3 == null)
                    {
                        logger.Trace($"No Data to process: Message ID {id}");
                        return;
                    }

                    logger.Info($"Processing Arrival Flight {tuple.Item2.flightKey}");

                    // Get a list for each of the additions, updates and deletions.
                    Tuple<List<PTMRow>, List<PTMRow>, List<PTMRow>> crud = arrivalClassifier.ClassifyTransferChanges(tuple.Item3);

                    // Item1 - List<PTMRow>. Additional PTMS
                    // Item2 - List<PTMRow>. Update PTMS
                    // Item3 - List<PTMRow>. Deleted PTMS

                    if (crud == null)
                    {
                        logger.Error($"Error Identifying Changes in Transfer Loads: Message ID {id}");
                        return;
                    }
                    else
                    {
                        // Process the added, updated and deleted PTM entries

                        if (Parameters.PROCESS_ADDS && crud.Item1.Count > 0) await ProcessAdditionsAsync(tuple.Item2, crud.Item1);
                        if (Parameters.PROCESS_UPDATES && crud.Item2.Count > 0) await ProcessUpdatesAsync(tuple.Item2, crud.Item2);
                        if (Parameters.PROCESS_DELETES && crud.Item3.Count > 0) await ProcessDeletesAsync(tuple.Item2, crud.Item3);


                    }
                    logger.Info($"Finished Processing : Message ID {id}\n");
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

        private async Task ProcessDeletesAsync(FlightNode arrFlight, List<PTMRow> deleteList)
        {

            logger.Trace(">>>>>>----------- Processing Deletes");
            logger.Info($"Processing deletes for arrival flight {arrFlight.flightKey}");

            foreach (PTMRow ptm in deleteList)
            {
                bool result = await flightUpdater.UpdateOrAddOrRemovePTMFromDepartureFlight(arrFlight, new FlightNode(ptm, "Departure", arrFlight.airportCodeIATA), null);
                if (result)
                {
                    logger.Trace($"PTM Record Removed Sucess {arrFlight.ToString()}, {ptm.flightKey}");
                }
                else
                {
                    logger.Trace($"PTM Record Removed Failure {arrFlight.ToString()}, {ptm.flightKey}");
                }
            }

            logger.Trace("<<<<<<<<<----------- Processing Deletes");
        }

        private async Task ProcessAdditionsAsync(FlightNode arrFlight, List<PTMRow> additionsList)
        {
            logger.Trace(">>>>>>----------- Processing Additions");
            logger.Info($"Processing additions for arrival flight {arrFlight.flightKey}");

            foreach (PTMRow ptm in additionsList)
            {
                bool result = await flightUpdater.UpdateOrAddOrRemovePTMFromDepartureFlight(arrFlight, new FlightNode(ptm, "Departure", arrFlight.airportCodeIATA), ptm);
                if (result)
                {
                    logger.Trace($"PTM Record Addition Sucess {arrFlight.ToString()}, {ptm.flightKey}");
                }
                else
                {
                    logger.Trace($"PTM Record Addition Failure {arrFlight.ToString()}, {ptm.flightKey}");
                }
            }

            logger.Trace("<<<<<<<----------- Processing Additions");
        }

        private async Task ProcessUpdatesAsync(FlightNode arrFlight, List<PTMRow> updateList)
        {

            logger.Trace(">>>>>>----------- Processing Updates");
            logger.Info($"Processing updates for arrival flight {arrFlight.flightKey}");

            foreach (PTMRow ptm in updateList)
            {
                bool result = await flightUpdater.UpdateOrAddOrRemovePTMFromDepartureFlight(arrFlight, new FlightNode(ptm, "Departure", arrFlight.airportCodeIATA), ptm);
                if (result)
                {
                    logger.Trace($"PTM Record Uppdate Sucess {arrFlight.ToString()}, {ptm.flightKey}");
                }
                else
                {
                    logger.Trace($"PTM Record Update Failure {arrFlight.ToString()}, {ptm.flightKey}");
                }
            }

            logger.Trace("<<<<<<<<<----------- Processing Updates");
        }

        private string GenerateMessageID(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public void Dispose()
        {
            ((IDisposable)recvQueue).Dispose();
        }
    }
}
