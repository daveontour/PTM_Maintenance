using System;
using System.Collections.Generic;
using System.Xml;

//Release Candidate 2.0

namespace Departure_PTM_Widget
{
    class ArrivalChangeClassifier
    {

        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public ArrivalChangeClassifier() { }

        public Tuple<bool, FlightNode, XmlNode> ClassifyFlightAndGetTransferChanges(XmlNode xmlRoot)
        {

            /*
             * Takes a FlightUpdated Notification messages and determines if there are any changes to the
             * Tl--TransferLoads table
             */

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlRoot.OwnerDocument.NameTable);
            nsmgr.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");
            nsmgr.AddNamespace("amsmess", "http://www.sita.aero/ams6-xml-api-messages");


            // Firstly, we are only interested in Arrival Flight Changes.
            FlightNode flight = new FlightNode(xmlRoot, nsmgr);

            // The Flight is not an arrival, so stop processing
            if (!flight.IsArrival())
            {
                logger.Trace("Flight was not an arrival");
                return new Tuple<bool, FlightNode, XmlNode>(false, flight, null);
            }


            XmlNode transferChanges;

            // Does the notification contain transfer changes?
            try
            {
                transferChanges = xmlRoot.SelectSingleNode(".//ams:FlightChanges/ams:TableValueChange[@propertyName='Tl--_TransferLoads']", nsmgr);
            }
            catch (Exception)
            {
                logger.Trace("No Transfer Changes Found");
                return new Tuple<bool, FlightNode, XmlNode>(false, null, null);
            }

            if (transferChanges == null)
            {
                logger.Trace("No Transfer Changes Found");
                return new Tuple<bool, FlightNode, XmlNode>(false, null, null);
            }

            logger.Trace("Arrival flight with transfer changes " + flight.ToString());

            return new Tuple<bool, FlightNode, XmlNode>(true, flight, transferChanges);
        }

        public Tuple<List<PTMRow>, List<PTMRow>, List<PTMRow>> ClassifyTransferChanges(XmlNode transferChanges)
        {
            /* 
             * Determine the entries which are additions, updates and deletions
             */

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(transferChanges.OwnerDocument.NameTable);
            nsmgr.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");
            nsmgr.AddNamespace("amsmess", "http://www.sita.aero/ams6-xml-api-messages");

            // Tne XML node containing the old entries
            XmlNode oldNode;

            try
            {
                oldNode = transferChanges.SelectSingleNode(".//ams:OldValue[@propertyName='Tl--_TransferLoads']", nsmgr);
            }
            catch (Exception)
            {
                logger.Trace("Old Transfer Values Not Found");
                return null;
            }

            // The XML node containing the new entries
            XmlNode newNode;

            try
            {
                newNode = transferChanges.SelectSingleNode(".//ams:NewValue[@propertyName='Tl--_TransferLoads']", nsmgr);
            }
            catch (Exception)
            {
                logger.Trace("New Transfer Values Not Found");
                return null;
            }

            //Create a list of the Old entries
            List<PTMRow> oldList = new List<PTMRow>();
            try
            {

                foreach (XmlNode row in oldNode.SelectNodes(".//ams:Row", nsmgr))
                {
                    PTMRow ptmRow = new PTMRow(row);
                    if (Parameters.DEEPTRACE) logger.Trace($"Old Values Entry {ptmRow.ToString()}");
                    oldList.Add(ptmRow);
                }
            }
            catch (Exception e)
            {
                logger.Trace(e.Message);
                return null;
            }

            // Create a list of the new entries
            List<PTMRow> newList = new List<PTMRow>();
            try
            {

                foreach (XmlNode row in newNode.SelectNodes(".//ams:Row", nsmgr))
                {
                    PTMRow ptmRow = new PTMRow(row);
                    if (Parameters.DEEPTRACE) logger.Trace($"New Values Entry {ptmRow.ToString()}");
                    newList.Add(ptmRow);
                }
            }
            catch (Exception e)
            {
                logger.Trace(e.Message);
                return null;
            }

            // Examine the list to create a new list of the adds, updates and deletes
            List<PTMRow> additionsList = GetAdditions(oldList, newList);
            List<PTMRow> updateList = GeUpdates(oldList, newList);
            List<PTMRow> deleteList = GetDeletions(oldList, newList);

            //Print out the individual lists of logging is enabled.
            if (logger.IsTraceEnabled)
            {
                logger.Trace("Addition List:");
                foreach (PTMRow row in additionsList)
                {
                    logger.Trace(row.ToString);
                }

                logger.Trace("Updates List:");
                foreach (PTMRow row in updateList)
                {
                    logger.Trace(row.ToString);
                }

                logger.Trace("Delete List:");
                foreach (PTMRow row in deleteList)
                {
                    logger.Trace(row.ToString);
                }
            }


            return new Tuple<List<PTMRow>, List<PTMRow>, List<PTMRow>>(additionsList, updateList, deleteList);

        }

        private List<PTMRow> GetAdditions(List<PTMRow> oldList, List<PTMRow> newList)
        {
            // List to hold the new entries
            List<PTMRow> additionsList = new List<PTMRow>();

            foreach (PTMRow newPTM in newList)
            {
                bool found = false;
                foreach (PTMRow oldPTM in oldList)
                {
                    if (oldPTM.FlightEquals(newPTM))
                    {
                        found = true;
                        continue;
                    }
                }

                //The entry wasn't found in the oldlist, so it is an addtion. Add it to the list
                if (!found)
                {
                    additionsList.Add(newPTM);
                }
            }

            return additionsList;

        }

        private List<PTMRow> GeUpdates(List<PTMRow> oldList, List<PTMRow> newList)
        {
            List<PTMRow> changeList = new List<PTMRow>();
            foreach (PTMRow newPTM in newList)
            {
                bool change = false;
                foreach (PTMRow oldPTM in oldList)
                {
                    // if the flight is the same, but the contents have changed, then add it to the changes list
                    if (oldPTM.FlightEquals(newPTM) & !oldPTM.PTMEquals(newPTM))
                    {
                        change = true;
                        continue;
                    }
                }

                if (change)
                {
                    changeList.Add(newPTM);
                }
            }

            return changeList;

        }

        private List<PTMRow> GetDeletions(List<PTMRow> oldList, List<PTMRow> newList)
        {
            List<PTMRow> deleteList = new List<PTMRow>();
            foreach (PTMRow oldPTM in oldList)
            {
                bool found = false;
                foreach (PTMRow newPTM in newList)
                {
                    if (oldPTM.FlightEquals(newPTM))
                    {
                        found = true;
                        continue;
                    }
                }

                // An old entry wasn't found in the set of new entries, so it has to be removed. Add it to the delete list
                if (!found)
                {
                    deleteList.Add(oldPTM);
                }
            }

            return deleteList;

        }
    }
}
