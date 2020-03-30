using System;
using System.Collections.Generic;
using System.Xml;

namespace AUH_PTM_Widget
{
    class ArrivalChangeClassifier
    {

        static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();


        public ArrivalChangeClassifier() { }

        public Tuple<bool, FlightNode, XmlNode> Classify(XmlNode xmlRoot)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlRoot.OwnerDocument.NameTable);
            nsmgr.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");
            nsmgr.AddNamespace("amsmess", "http://www.sita.aero/ams6-xml-api-messages");

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

            FlightNode flight = new FlightNode(xmlRoot, nsmgr);

            // The Flight is not an arrival, so stop processing
            if (!flight.IsArrival())
            {
                logger.Trace("Flight was not an arrival");
                return new Tuple<bool, FlightNode, XmlNode>(false, flight, null);
            }

            logger.Trace("Arrival flight with transfer changes " + flight.ToString());

            return new Tuple<bool, FlightNode, XmlNode>(true, flight, transferChanges);
        }

        internal Tuple<List<PTMRow>, List<PTMRow>, List<PTMRow>> ClassifyEntries(XmlNode transferChanges)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(transferChanges.OwnerDocument.NameTable);
            nsmgr.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");
            nsmgr.AddNamespace("amsmess", "http://www.sita.aero/ams6-xml-api-messages");

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

            List<PTMRow> oldList = new List<PTMRow>();
            List<PTMRow> newList = new List<PTMRow>();

            try
            {

                logger.Trace("Old Values:");
                foreach (XmlNode row in oldNode.SelectNodes(".//ams:Row", nsmgr))
                {
                    PTMRow ptmRow = new PTMRow(row, nsmgr);
                    logger.Trace(ptmRow.ToString);
                    oldList.Add(ptmRow);
                }
            }
            catch (Exception e)
            {
                logger.Trace(e.Message);
            }

            try
            {
                logger.Trace("New Values:");
                foreach (XmlNode row in newNode.SelectNodes(".//ams:Row", nsmgr))
                {
                    PTMRow ptmRow = new PTMRow(row, nsmgr);
                    logger.Trace(ptmRow.ToString);
                    newList.Add(ptmRow);
                }
            }
            catch (Exception e)
            {
                logger.Trace(e.Message);
            }

            List<PTMRow> additionsList = GetAdditions(oldList, newList);
            List<PTMRow> updateList = GetChanges(oldList, newList);
            List<PTMRow> deleteList = GetDeletions(oldList, newList);


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


            return new Tuple<List<PTMRow>, List<PTMRow>, List<PTMRow>>(additionsList, updateList, deleteList);

        }

        private List<PTMRow> GetAdditions(List<PTMRow> oldList, List<PTMRow> newList)
        {
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

                if (!found)
                {
                    additionsList.Add(newPTM);
                }
            }

            return additionsList;

        }

        private List<PTMRow> GetChanges(List<PTMRow> oldList, List<PTMRow> newList)
        {
            List<PTMRow> changeList = new List<PTMRow>();
            foreach (PTMRow newPTM in newList)
            {
                bool change = false;
                foreach (PTMRow oldPTM in oldList)
                {
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

                if (!found)
                {
                    deleteList.Add(oldPTM);
                }
            }

            return deleteList;

        }
    }
}
