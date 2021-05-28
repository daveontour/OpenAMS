using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAMS {

    internal class Flight {

        private readonly string flightIDTemplate = @"<FlightId xmlns:{0}=""http://www.sita.aero/ams6-xml-api-messages"">
<{0}:FlightKind>{1}</{0}:FlightKind>
<{0}:AirlineDesignator codeContext=""IATA"">{2}</{0}:AirlineDesignator>
<{0}:FlightNumber>{3}</{0}:FlightNumber>
<{0}:ScheduledDate>{4}</{0}:ScheduledDate>
<{0}:AirportCode codeContext=""IATA"">{5}</{0}:AirportCode>
</FlightId>";

        private readonly string propertyTemplate = @"<{0}:Update propertyName=""{1}"" >{2}</{0}:Update>";

        public Flight(JObject flight, string apt) {
            this.flight = flight;
            this.HomeAirport = apt;
        }

        public string HomeAirport { get; set; }
        public JObject flight { get; set; }
        private JToken arr { get { return flight["arrival"]; } }
        private JToken dep { get { return flight["departure"]; } }
        private JToken flightIdentifier { get { return flight["flightIdentifier"]; } }

        public string ServiceType { get { return flight["serviceType"]?.ToString(); } }
        public string Duration { get { return flight["duration"]?.ToString(); } }

        public string AirLine { get { return flightIdentifier["operatingCarrier"]["iataCode"]?.ToString(); } }
        public string FltNumber { get { return flightIdentifier["operatingCarrier"]["flightNumber"]?.ToString(); } }
        public string ACType { get { return flightIdentifier["aircraft"]?["iataCode"]?.ToString(); } }
        public string ACReg { get { return flightIdentifier["aircraft"]?["registration"]?.ToString(); } }

        public string Airport_Arr { get { return arr["airport"]["iataCode"]?.ToString(); } }
        public string SCHA { get { return arr["scheduled"]?.ToString(); } }
        public DateTime? OpDateTime_Arr { get { return (DateTime?)arr["scheduled"]; } }
        public string ETA { get { return arr["estimated"]?.ToString(); } }
        public string ATA { get { return arr["actual"]?.ToString(); } }
        public string Carousel_Arr { get { return arr["carousel"]?.ToString(); } }
        public string Gate_Arr { get { return arr["gate"]?.ToString(); } }
        public string Terminal_Arr { get { return arr["terminal"]?.ToString(); } }
        public string StatusText_Arr { get { return arr["statusText"]?.ToString(); } }

        public string Airport_Dep { get { return dep["airport"]["iataCode"]?.ToString(); } }
        public string SCHD { get { return dep["scheduled"]?.ToString(); } }
        public DateTime? OpDateTime_Dep { get { return (DateTime?)dep["scheduled"]; } }
        public string ETD { get { return dep["estimated"]?.ToString(); } }
        public string ATD { get { return dep["actual"]?.ToString(); } }
        public string Gate_Dep { get { return dep["gate"]?.ToString(); } }
        public string Terminal_Dep { get { return dep["terminal"]?.ToString(); } }
        public string StatusText_Dep { get { return dep["statusText"]?.ToString(); } }

        private string SDO {
            get {
                if (HomeAirport == Airport_Arr) {
                    return OpDateTime_Arr.Value.ToString("yyyy-MM-dd");
                } else {
                    return OpDateTime_Dep.Value.ToString("yyyy-MM-dd");
                }
            }
        }

        public string STO {
            get {
                if (HomeAirport == Airport_Arr) {
                    return SCHA;
                } else {
                    return SCHD;
                }
            }
        }

        public string Gate {
            get {
                if (HomeAirport == Airport_Arr) {
                    return Gate_Arr;
                } else {
                    return Gate_Dep;
                }
            }
        }

        public string Terminal {
            get {
                if (HomeAirport == Airport_Arr) {
                    return Terminal_Arr;
                } else {
                    return Terminal_Dep;
                }
            }
        }

        public string StatusText {
            get {
                if (HomeAirport == Airport_Arr) {
                    return StatusText_Arr;
                } else {
                    return StatusText_Dep;
                }
            }
        }

        public string Carousel {
            get {
                if (HomeAirport == Airport_Arr) {
                    return Carousel_Arr;
                } else {
                    return null;
                }
            }
        }

        public string Info {
            get {
                return $"{AirLine}{FltNumber} {STO} {SDO} {Airport_Dep}->{Airport_Arr}";
            }
        }

        public string FlightIDXML {
            get {
                if (HomeAirport == Airport_Arr) {
                    return String.Format(flightIDTemplate, "ams", "Arrival", AirLine, FltNumber, SDO, HomeAirport);
                } else {
                    return String.Format(flightIDTemplate, "ams", "Departure", AirLine, FltNumber, SDO, HomeAirport);
                }
            }
        }

        public string GetPropValue(string propName) {
            return this.GetType().GetProperty(propName).GetValue(this, null)?.ToString();
        }

        public string GetFieldXML(string ns, string name, string prop) {
            string value = GetPropValue(prop);
            if (value == null) {
                return null;
            }
            return String.Format(this.propertyTemplate, ns, name, GetPropValue(prop));
        }

        public string GetAMSFlightCreate(List<Tuple<string, string>> fields) {
            StringBuilder sb = new StringBuilder();

            return sb.ToString();
        }
    }
}