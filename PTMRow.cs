using System;
using System.Collections.Generic;
using System.Xml;

//Release Candidate 1.0

namespace Departure_PTM_Widget
{


    class PTMRow
    {
        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public Dictionary<string, string> valueMap = new Dictionary<string, string>();

        public string airline;
        public string flight;
        public string sto;
        public string flightKey;

        public PTMRow(XmlNode node)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(node.OwnerDocument.NameTable);
            nsmgr.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");
            nsmgr.AddNamespace("amsmess", "http://www.sita.aero/ams6-xml-api-messages");

            airline = node.SelectSingleNode(".//ams:Value[@propertyName='Sl--_AirlineIATA']", nsmgr).InnerText;
            flight = node.SelectSingleNode(".//ams:Value[@propertyName='Sl--_FlightNumber']", nsmgr).InnerText;
            sto = node.SelectSingleNode(".//ams:Value[@propertyName='dl--_STO']", nsmgr).InnerText;
            flightKey = airline + flight + sto.Substring(0, 10);

            //Create a dictionary of values from the PTM record
            XmlNodeList values = node.SelectNodes(".//ams:Value", nsmgr);
            foreach (XmlNode value in values)
            {
                string key = value.Attributes["propertyName"].InnerText;
                string v = value.InnerText;
                try
                {
                    valueMap.Add(value.Attributes["propertyName"].InnerText, value.InnerText);
                }
                catch (Exception e)
                {
                    logger.Trace($"\nError adding PTM field to dictionary");
                    logger.Trace(e.Message);
                    logger.Trace($"{key}:{value}\n");
                }
            }
        }


        // Is the supplied node referring to the same flight as this node?
        public bool FlightEquals(PTMRow node)
        {
            return node.flightKey == this.flightKey;
        }

        public bool PTMEquals(PTMRow node)
        {
            try
            {
                foreach (string key in valueMap.Keys)
                {
                    if (valueMap[key] != node.valueMap[key])
                    {
                        return false;
                    }
                }

                // Just in case a new element has been added to the record
                foreach (string key in node.valueMap.Keys)
                {
                    if (node.valueMap[key] != valueMap[key])
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Trace(ex.Message);
                return false;
            }

            return true;
        }

        public new string ToString()
        {
            return flightKey;
        }
    }
}