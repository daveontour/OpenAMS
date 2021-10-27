using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAMS
{
    internal class Flight
    {
        public static readonly Logger logger = LogManager.GetLogger("consoleLogger");
        public static readonly Logger arrLogger = LogManager.GetLogger("arrivalLogger");
        public static readonly Logger depLogger = LogManager.GetLogger("depLogger");

        private readonly string topTemplate = @"<amsx-messages:Envelope
xmlns:amsx-messages=""http://www.sita.aero/ams6-xml-api-messages""
xmlns:amsx-datatypes=""http://www.sita.aero/ams6-xml-api-datatypes""
xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
apiVersion=""2.12"">
<amsx-messages:Content>
<amsx-messages:FlightCreateRequest>
<amsx-datatypes:Token>{0}</amsx-datatypes:Token>";

        private readonly string flightIDTemplate = @"<amsx-messages:FlightId>
<amsx-datatypes:FlightKind>{1}</amsx-datatypes:FlightKind>
<amsx-datatypes:AirlineDesignator codeContext=""IATA"">{2}</amsx-datatypes:AirlineDesignator>
<amsx-datatypes:FlightNumber>{3}</amsx-datatypes:FlightNumber>
<amsx-datatypes:ScheduledDate>{4}</amsx-datatypes:ScheduledDate>
<amsx-datatypes:AirportCode codeContext=""IATA"">{5}</amsx-datatypes:AirportCode>
</amsx-messages:FlightId>
<amsx-messages:FlightUpdates>
<amsx-messages:Update propertyName=""ScheduledTime"">{6}</amsx-messages:Update>";

        private readonly string bottomTemplate = @"</amsx-messages:FlightUpdates>
</amsx-messages:FlightCreateRequest>
</amsx-messages:Content>
</amsx-messages:Envelope>";

        private readonly string actypeTemplate = @"<amsx-messages:AircraftType>
	<amsx-messages:AircraftTypeId>
		<amsx-messages:AircraftTypeCode codeContext=""IATA"">{1}</AircraftTypeCode>
	</amsx-messages:AircraftTypeId>
</amsx-messages:AircraftType>";

        private readonly string propertyTemplate = @"<amsx-messages:Update propertyName=""{1}"" {3}>{2}</amsx-messages:Update>";

        public Flight(JObject flight, string apt, List<Tuple<string, string>> arrivalFields, List<Tuple<string, string>> departureFields, string homeAirportSub)
        {
            this.flight = flight;
            this.HomeAirport = apt;
            ArrivalFields = arrivalFields;
            DepartureFields = departureFields;
            this.HomeAirportSub = homeAirportSub;
        }

        public bool IsArrival
        {
            get
            {
                if (HomeAirport == Airport_Arr)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsDeparture
        {
            get
            {
                if (HomeAirport == Airport_Dep)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public string HomeAirport { get; set; }
        public string HomeAirportSub { get; set; }
        public JObject flight { get; set; }
        private JToken arr { get { return flight["arrival"]; } }
        private JToken dep { get { return flight["departure"]; } }
        private JToken flightIdentifier { get { return flight["flightIdentifier"]; } }

        public string ServiceType { get { return flight["serviceType"]?.ToString(); } }
        public string Duration { get { return flight["duration"]?.ToString(); } }

        public string AirLine
        {
            get
            {
                string airline = flightIdentifier["operatingCarrier"]["iataCode"]?.ToString();
                return airline;
            }
        }

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

        public string Route
        {
            get
            {
                if (HomeAirport == Airport_Arr)
                {
                    return Airport_Dep;
                }
                else
                {
                    return Airport_Arr;
                }
            }
        }

        private string SDO
        {
            get
            {
                if (HomeAirport == Airport_Arr)
                {
                    return OpDateTime_Arr.Value.ToString("yyyy-MM-dd");
                }
                else
                {
                    return OpDateTime_Dep.Value.ToString("yyyy-MM-dd");
                }
            }
        }

        public string STO
        {
            get
            {
                if (HomeAirport == Airport_Arr)
                {
                    return SCHA;
                }
                else
                {
                    return SCHD;
                }
            }
        }

        public string Gate
        {
            get
            {
                if (HomeAirport == Airport_Arr)
                {
                    return Gate_Arr;
                }
                else
                {
                    return Gate_Dep;
                }
            }
        }

        public string Terminal
        {
            get
            {
                if (HomeAirport == Airport_Arr)
                {
                    return Terminal_Arr;
                }
                else
                {
                    return Terminal_Dep;
                }
            }
        }

        public string StatusText
        {
            get
            {
                if (HomeAirport == Airport_Arr)
                {
                    return StatusText_Arr;
                }
                else
                {
                    return StatusText_Dep;
                }
            }
        }

        public string Carousel
        {
            get
            {
                if (HomeAirport == Airport_Arr)
                {
                    return Carousel_Arr;
                }
                else
                {
                    return null;
                }
            }
        }

        public string Info
        {
            get
            {
                return $"{AirLine}{FltNumber} {STO} {SDO} {Airport_Dep}->{Airport_Arr}";
            }
        }

        public string FlightIDXML
        {
            get
            {
                if (HomeAirportSub == null || HomeAirportSub == "")
                {
                    if (HomeAirport == Airport_Arr)
                    {
                        return String.Format(flightIDTemplate, "ams", "Arrival", AirLine, FltNumber, SDO, HomeAirport, STO);
                    }
                    else
                    {
                        return String.Format(flightIDTemplate, "ams", "Departure", AirLine, FltNumber, SDO, HomeAirport, STO);
                    }
                }
                else
                {
                    if (HomeAirport == Airport_Arr)
                    {
                        return String.Format(flightIDTemplate, "ams", "Arrival", AirLine, FltNumber, SDO, HomeAirportSub, STO);
                    }
                    else
                    {
                        return String.Format(flightIDTemplate, "ams", "Departure", AirLine, FltNumber, SDO, HomeAirportSub, STO);
                    }
                }
            }
        }

        public List<Tuple<string, string>> ArrivalFields { get; }
        public List<Tuple<string, string>> DepartureFields { get; }

        public string GetPropValue(string propName)
        {
            return this.GetType().GetProperty(propName).GetValue(this, null)?.ToString();
        }

        public string GetFieldXML(string ns, string name, string prop)
        {
            string value = GetPropValue(prop);
            if (value == null)
            {
                return null;
            }
            string codeContext = null;
            if (prop == "Route")
            {
                codeContext = @"codeContext=""IATA""";
            }
            return String.Format(this.propertyTemplate, ns, name, GetPropValue(prop), codeContext);
        }

        public string GetAMSFlightCreate(string amsToken)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(String.Format(topTemplate, amsToken));
            sb.AppendLine(FlightIDXML);

            if (ACType != null)
            {
                sb.AppendLine(String.Format(actypeTemplate, ACType));
            }

            if (IsArrival)
            {
                foreach (var pair in ArrivalFields)
                {
                    string field = GetFieldXML("ams", pair.Item2, pair.Item1);
                    if (field != null)
                    {
                        sb.AppendLine(field);
                    }
                }
            }

            if (IsDeparture)
            {
                foreach (var pair in DepartureFields)
                {
                    string field = GetFieldXML("ams", pair.Item2, pair.Item1);
                    if (field != null)
                    {
                        sb.AppendLine(field);
                    }
                }
            }

            sb.AppendLine(bottomTemplate);
            return sb.ToString();
        }
    }
}