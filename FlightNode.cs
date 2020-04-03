using System;
using System.Xml;

//Version RC 3.7

namespace AUH_PTM_Widget
{

    // Class for holding the flight information that is contained in the Towing message
    class FlightNode
    {
        public string flightKind;
        public string airlineDesignatorIATA;
        public string airlineDesignatorICAO;

        public string flightNumber;
        public string scheduledDate;

        public string airportCodeIATA;
        public string airportCodeICAO;

        public string flightKey;

        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public FlightNode(PTMRow flight, string kind, string airport)
        {
            this.flightKind = kind;
            this.airlineDesignatorIATA = flight.airline;
            this.scheduledDate = flight.sto;
            this.flightNumber = flight.flight;
            this.airportCodeIATA = airport;

            this.flightKey = this.airlineDesignatorIATA + this.flightNumber + this.scheduledDate.Substring(0, 10);
        }
        public FlightNode(XmlNode node, XmlNamespaceManager nsmgr)
        {


            try
            {
                this.flightKind = node.SelectSingleNode(".//amsmess:Flight/ams:FlightId/ams:FlightKind", nsmgr).InnerText;
            }
            catch (Exception)
            {
                logger.Trace("FlightKind Not Found");
            }

            try
            {
                this.airlineDesignatorIATA = node.SelectSingleNode(".//amsmess:Flight/ams:FlightId/ams:AirlineDesignator[@codeContext='IATA']", nsmgr).InnerText;
            }
            catch (Exception)
            {
                logger.Trace("IATA Airline Not Found");
            }

            try
            {
                this.airlineDesignatorICAO = node.SelectSingleNode(".//amsmess:Flight/ams:FlightId/ams:AirlineDesignator[@codeContext='ICAO']", nsmgr).InnerText;
            }
            catch (Exception)
            {
                logger.Trace("ICAO Airline Not Found");
            }

            try
            {
                this.flightNumber = node.SelectSingleNode(".//amsmess:Flight/ams:FlightId/ams:FlightNumber", nsmgr).InnerText;
            }
            catch (Exception)
            {
                logger.Trace("Flight Number Not Found");
            }

            try
            {
                this.scheduledDate = node.SelectSingleNode(".//amsmess:Flight/ams:FlightId/ams:ScheduledDate", nsmgr).InnerText;
            }
            catch (Exception)
            {
                logger.Trace("Sched Date Not Found");
            }

            try
            {
                this.airportCodeIATA = node.SelectSingleNode(".//amsmess:Flight/ams:FlightId/ams:AirportCode[@codeContext='IATA']", nsmgr).InnerText;
            }
            catch (Exception)
            {
                logger.Trace("IATA Airport Not Found");
            }

            try
            {
                this.airportCodeICAO = node.SelectSingleNode(".//amsmess:Flight/ams:FlightId/ams:AirportCode[@codeContext='ICAO']", nsmgr).InnerText;
            }
            catch (Exception)
            {
                logger.Trace("ICAO Airport Not Found");
            }

            this.flightKey = this.airlineDesignatorIATA + this.flightNumber + this.scheduledDate.Substring(0, 10);
        }

        public bool IsArrival()
        {
            return this.flightKind == "Arrival";
        }

        // Is the supplied node referring to the same flight as this node?
        public bool Equals(FlightNode node)
        {
            if (node.flightKind == this.flightKind
                && node.airlineDesignatorIATA == this.airlineDesignatorIATA
                && node.flightNumber == this.flightNumber
                && node.scheduledDate == this.scheduledDate)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public new string ToString()
        {
            return $"AirlineCode: {airlineDesignatorIATA}, Flight Number: {flightNumber}, Flight Kind: {flightKind}, Scheduled Date: {scheduledDate}";

        }
    }
}