using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AUH_PTM_Widget
{
    class FlightUpdater
    {
        static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private string getFlightTemplate = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ams6=""http://www.sita.aero/ams6-xml-api-webservice"" xmlns:wor=""http://schemas.datacontract.org/2004/07/WorkBridge.Modules.AMS.AMSIntegrationAPI.Mod.Intf.DataTypes"">
   <soapenv:Header/>
   <soapenv:Body>
	  <ams6:GetFlight>
		 <!--Optional:-->
		 <ams6:sessionToken>@token</ams6:sessionToken>
		 <!--Optional:-->
		 <ams6:flightId>
			<wor:_hasAirportCodes>false</wor:_hasAirportCodes>
			<wor:_hasFlightDesignator>true</wor:_hasFlightDesignator>
			<wor:_hasScheduledTime>false</wor:_hasScheduledTime>
			<wor:airlineDesignatorField>
			   <!--Zero or more repetitions:-->
			   <wor:LookupCode>
				  <wor:codeContextField>IATA</wor:codeContextField>
				  <wor:valueField>@airlineIATA</wor:valueField>
			   </wor:LookupCode>
			</wor:airlineDesignatorField>
			<wor:airportCodeField>
			   <!--Zero or more repetitions:-->
			   <wor:LookupCode>
				  <wor:codeContextField>IATA</wor:codeContextField>
				  <wor:valueField>@airportIATA</wor:valueField>
			   </wor:LookupCode>
			</wor:airportCodeField>
			<wor:flightKindField>@kind</wor:flightKindField>
			<wor:flightNumberField>@flightNum</wor:flightNumberField>
			<wor:scheduledDateField>@schedDate</wor:scheduledDateField>
		 </ams6:flightId>
	  </ams6:GetFlight>
   </soapenv:Body>
</soapenv:Envelope>";



        private string updateFlightExtendedTop = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ams6=""http://www.sita.aero/ams6-xml-api-webservice"" xmlns:wor=""http://schemas.datacontract.org/2004/07/WorkBridge.Modules.AMS.AMSIntegrationAPI.Mod.Intf.DataTypes"">
   <soapenv:Header/>
   <soapenv:Body>
	  <ams6:UpdateFlightExtended>
		 <!--Optional:-->
		 <ams6:sessionToken>@token</ams6:sessionToken>
		 <!--Optional:-->
		 <ams6:flightIdentifier>
			<wor:_hasAirportCodes>true</wor:_hasAirportCodes>
			<wor:_hasFlightDesignator>true</wor:_hasFlightDesignator>
			<wor:_hasScheduledTime>true</wor:_hasScheduledTime>
			<wor:airlineDesignatorField>
			   <!--Zero or more repetitions:-->
			   <wor:LookupCode>
				  <wor:codeContextField>IATA</wor:codeContextField>
				  <wor:valueField>@iataAirline</wor:valueField>
			   </wor:LookupCode>
			   <wor:LookupCode>
				  <wor:codeContextField>ICAO</wor:codeContextField>
				  <wor:valueField>@icaoAirline</wor:valueField>
			   </wor:LookupCode>
			</wor:airlineDesignatorField>
			<wor:airportCodeField>
			   <!--Zero or more repetitions:-->
			   <wor:LookupCode>
				  <wor:codeContextField>IATA</wor:codeContextField>
				  <wor:valueField>@iataAirport</wor:valueField>
			   </wor:LookupCode>
			   <wor:LookupCode>
				  <wor:codeContextField>ICAO</wor:codeContextField>
				  <wor:valueField>@icaoAirport</wor:valueField>
			   </wor:LookupCode>
			</wor:airportCodeField>
			<wor:flightKindField>Departure</wor:flightKindField>
			<wor:flightNumberField>@fltNum</wor:flightNumberField>
			<wor:scheduledDateField>@sto</wor:scheduledDateField>
		 </ams6:flightIdentifier>
		 <!--Optional:-->
		 <ams6:updates>
		 <wor:activityUpdateField/>
		  <wor:eventUpdateField/>
			<wor:tableValueUpdateField>
			   <!--Zero or more repetitions:-->
			   <wor:TableValueUpdate>
				  <wor:propertyNameField>Tl--_TransferLoads</wor:propertyNameField>
				  <wor:rowField>";

        private string tableRowTemplateTop = @"<!--Zero or more repetitions:-->
					 <wor:TableRow>
						<wor:propertyNameField>Value</wor:propertyNameField>
						<wor:valueField>";

        private string tableRowPropertyTemplate = @"
						   <wor:PropertyValue>
							  <wor:codeContextField>IATA</wor:codeContextField>
							  <wor:codeContextFieldSpecified>false</wor:codeContextFieldSpecified>
							  <wor:propertyNameField>@field</wor:propertyNameField>
							  <wor:valueField>@value</wor:valueField>
						   </wor:PropertyValue>";

        private string tableRowTemplateBottom = @" </wor:valueField>
					 </wor:TableRow>";

        private string updateFlightExtendedBottomFixed = @"</wor:rowField>
			   </wor:TableValueUpdate>
			</wor:tableValueUpdateField>
			 <wor:updateField/>
		 </ams6:updates>
	  </ams6:UpdateFlightExtended>
   </soapenv:Body>
</soapenv:Envelope>";

        public FlightUpdater() { }

        public async Task<XmlNode> GetFlight(FlightNode flt)
        {
            try
            {
                string flightQuery = getFlightTemplate.Replace("@token", Parameters.TOKEN)
                    .Replace("@airlineIATA", flt.airlineDesignatorIATA)
                    .Replace("@airportIATA", flt.airportCodeIATA)
                    .Replace("@kind", flt.flightKind)
                    .Replace("@flightNum", flt.flightNumber)
                    .Replace("@schedDate", flt.scheduledDate);

                logger.Trace(flightQuery);



                try
                {
                    using (var client = new HttpClient())
                    {

                        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, Parameters.BASE_URI)
                        {
                            Content = new StringContent(flightQuery, Encoding.UTF8, "text/xml")
                        };
                        requestMessage.Headers.Add("SOAPAction", "http://www.sita.aero/ams6-xml-api-webservice/IAMSIntegrationService/GetFlight");

                        using (HttpResponseMessage response = await client.SendAsync(requestMessage))
                        {
                            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.NoContent)
                            {

                                XmlDocument doc = new XmlDocument();
                                doc.LoadXml(await response.Content.ReadAsStringAsync());
                                XmlNode xmlRoot = doc.DocumentElement;

                                return xmlRoot;
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex.StackTrace);
                    return null;
                }
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
                logger.Error(e);
                return null;
            }
        }

        public async Task<bool> RemovePTMFromDepartureFlight(FlightNode arrFlight, FlightNode depFlight)
        {
            // arrFlight - the flight node for the arriving flight
            // ptm - the PTM record that has to be removed from a departure flight


            XmlNode depFlightNode = await this.GetFlight(depFlight);

            if (depFlightNode == null)
            {
                logger.Trace($"Departure Flight Not Found: {depFlight.flightKey }");
                return false;
            }

            XmlNode departurePTMSNodes = GetTransfersFromFlight(depFlightNode);

            if (departurePTMSNodes == null)
            {
                logger.Trace($"No PTM Entries were found in departure flight: {depFlight.flightKey }");
                return true;
            }

            List<XmlNode> retainedPTMs = new List<XmlNode>();

            foreach (XmlNode node in departurePTMSNodes)
            {
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(node.OwnerDocument.NameTable);
                nsmgr.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");
                nsmgr.AddNamespace("amsmess", "http://www.sita.aero/ams6-xml-api-messages");

                PTMRow departurePTMEntry = new PTMRow(node, nsmgr);

                if (arrFlight.flightKey != departurePTMEntry.flightKey)
                {
                    retainedPTMs.Add(node);
                }
            }

            _ = UpdateDeprtaureFlightPTMEntriesAsync(retainedPTMs, depFlight);

            return true;
        }

        internal Task<bool> AddPTMToDepartureFlight(FlightNode arrFlight, FlightNode flightNode)
        {
            throw new NotImplementedException();
        }

        //public async Task<bool> AddPTMToDepartureFlight(PTMRow ptm, FlightNode depFlight)
        //{
        //    // arrFlight - the flight node for the arriving flight
        //    // ptm - the PTM record that has to be added a departure flight


        //    XmlNode depFlightNode = await this.GetFlight(depFlight);

        //    if (depFlightNode == null)
        //    {
        //        logger.Trace($"Departure Flight Not Found: {depFlight.flightKey }");
        //        return false;
        //    }

        //    XmlNode departurePTMSNodes = GetTransfersFromFlight(depFlightNode);

        //    List<XmlNode> retainedPTMs = new List<XmlNode>();

        //    if (departurePTMSNodes != null)
        //    {
        //        foreach (XmlNode node in departurePTMSNodes)
        //        {
        //            retainedPTMs.Add(node);
        //        }
        //    }

        //    retainedPTMs.Add(ptm);

        //    _ = UpdateDeprtaureFlightPTMEntriesAsync(retainedPTMs, depFlight);

        //    return true;
        //}

        private async Task UpdateDeprtaureFlightPTMEntriesAsync(List<XmlNode> retainedPTMs, FlightNode depFlight)
        {

            // The top part of the message which includes the departing flight info
            string soapUpdateMessage = updateFlightExtendedTop
                .Replace("@token", Parameters.TOKEN)
                .Replace("@iataAirline", depFlight.airlineDesignatorIATA)
                .Replace("@icaoAirline", depFlight.airlineDesignatorICAO)
                .Replace("@iataAirport", depFlight.airportCodeIATA)
                .Replace("@icaoAirport", depFlight.airportCodeICAO)
                .Replace("@fltNum", depFlight.flightNumber)
                .Replace("@sto", depFlight.scheduledDate);


            // The TableValue Construction for each of the PTMs
            string tableRowEntries = "";
            foreach (XmlNode node in retainedPTMs)
            {
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(node.OwnerDocument.NameTable);
                nsmgr.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");
                nsmgr.AddNamespace("amsmess", "http://www.sita.aero/ams6-xml-api-messages");
                XmlNodeList values = node.SelectNodes(".//ams:Value", nsmgr);

                string rowEntry = tableRowTemplateTop;
                foreach (XmlNode value in values)
                {
                    string key = value.Attributes["propertyName"].InnerText;
                    string v = value.InnerText;

                    rowEntry += tableRowPropertyTemplate.Replace("@field", key).Replace("@value", v);
                }

                rowEntry += tableRowTemplateBottom;
                tableRowEntries += rowEntry;

            }
            // Complete constructing the message by adding the bottom part of the message
            soapUpdateMessage += tableRowEntries + updateFlightExtendedBottomFixed;

            logger.Trace(soapUpdateMessage);

            // Send the message via the AMS WebServices endpoint
            try
            {
                using (var client = new HttpClient())
                {

                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, Parameters.BASE_URI)
                    {
                        Content = new StringContent(soapUpdateMessage, Encoding.UTF8, "text/xml")
                    };
                    requestMessage.Headers.Add("SOAPAction", "http://www.sita.aero/ams6-xml-api-webservice/IAMSIntegrationService/UpdateFlightExtended");

                    using (HttpResponseMessage response = await client.SendAsync(requestMessage))
                    {
                        if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.NoContent)
                        {
                            return;
                        }
                        else
                        {
                            logger.Error("Error Updating Departure Flight");
                            logger.Error(response.StatusCode);
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.StackTrace);
                return;
            }
        }

        public XmlNode GetTransfersFromFlight(XmlNode flight)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(flight.OwnerDocument.NameTable);
            nsmgr.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");
            nsmgr.AddNamespace("amsmess", "http://www.sita.aero/ams6-xml-api-messages");


            // Does the notification contain transfers?
            try
            {
                XmlNode transfers = flight.SelectSingleNode(".//ams:FlightState/ams:TableValue[@propertyName='Tl--_TransferLoads']", nsmgr);
                return transfers;
            }
            catch (Exception)
            {
                logger.Trace("Error Getting Transfers from Flight");
                return null;
            }
        }

    }
}
