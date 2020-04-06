using System;
using System.Xml;

//Release Candidate 2.0

namespace Departure_PTM_Widget
{

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

        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public FlightNode(PTMRow flight, string kind, string airport)
        {
            flightKind = kind;
            airlineDesignatorIATA = flight.airline;
            scheduledDate = flight.sto;
            flightNumber = flight.flight;
            airportCodeIATA = airport;

            flightKey = airlineDesignatorIATA + flightNumber + scheduledDate.Substring(0, 10);
        }
        public FlightNode(XmlNode node, XmlNamespaceManager nsmgr)
        {


            try
            {
                this.flightKind = node.SelectSingleNode(".//amsmess:Flight/ams:FlightId/ams:FlightKind", nsmgr).InnerText;
            }
            catch (Exception)
            {
                logger.Error("FlightKind Not Found");
            }

            try
            {
                this.airlineDesignatorIATA = node.SelectSingleNode(".//amsmess:Flight/ams:FlightId/ams:AirlineDesignator[@codeContext='IATA']", nsmgr).InnerText;
            }
            catch (Exception)
            {
                logger.Error("IATA Airline Not Found");
            }

            try
            {
                this.airlineDesignatorICAO = node.SelectSingleNode(".//amsmess:Flight/ams:FlightId/ams:AirlineDesignator[@codeContext='ICAO']", nsmgr).InnerText;
            }
            catch (Exception)
            {
                logger.Error("ICAO Airline Not Found");
            }

            try
            {
                this.flightNumber = node.SelectSingleNode(".//amsmess:Flight/ams:FlightId/ams:FlightNumber", nsmgr).InnerText;
            }
            catch (Exception)
            {
                logger.Error("Flight Number Not Found");
            }

            try
            {
                this.scheduledDate = node.SelectSingleNode(".//amsmess:Flight/ams:FlightId/ams:ScheduledDate", nsmgr).InnerText;
            }
            catch (Exception)
            {
                logger.Error("Sched Date Not Found");
            }

            try
            {
                this.airportCodeIATA = node.SelectSingleNode(".//amsmess:Flight/ams:FlightId/ams:AirportCode[@codeContext='IATA']", nsmgr).InnerText;
            }
            catch (Exception)
            {
                logger.Error("IATA Airport Not Found");
            }

            try
            {
                this.airportCodeICAO = node.SelectSingleNode(".//amsmess:Flight/ams:FlightId/ams:AirportCode[@codeContext='ICAO']", nsmgr).InnerText;
            }
            catch (Exception)
            {
                logger.Error("ICAO Airport Not Found");
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