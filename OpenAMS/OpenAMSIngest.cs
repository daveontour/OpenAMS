using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;

namespace OpenAMS {

    public class RestResponse {
        public HttpStatusCode StatusCode { get; set; }
        public string Content { get; set; }
    }

    internal class OpenAMSIngest {
        public static readonly Logger logger = LogManager.GetLogger("consoleLogger");
        public static readonly Logger arrLogger = LogManager.GetLogger("arrivalLogger");
        public static readonly Logger depLogger = LogManager.GetLogger("depLogger");

        private System.Timers.Timer updateTimer;
        private DateTime lastUpdate;
        public string HomeAirport { get; set; }
        public string HomeAirportSub { get; set; }
        public string InitTemplate { get; set; }
        public string UpdateTemplate { get; set; }
        public string AMSToken { get; set; }
        public string AMSRequestQueue { get; set; }
        public string FLIFOToken { get; set; }
        public int UpdateInterval { get; set; }

        public List<Tuple<string, string>> arrivalFields = new List<Tuple<string, string>>();
        public List<Tuple<string, string>> departureFields = new List<Tuple<string, string>>();

        public OpenAMSIngest(string executeFile, string server) {
            ExecuteFile = executeFile;
            Server = server;

            XmlDocument doc = new XmlDocument();
            doc.Load("widget.config.xml");

            HomeAirport = doc.SelectSingleNode(".//homeAirport").InnerText;
            HomeAirportSub = doc.SelectSingleNode(".//homeAirportSub")?.InnerText;
            InitTemplate = doc.SelectSingleNode(".//initURL").InnerText;
            UpdateTemplate = doc.SelectSingleNode(".//updateURL").InnerText;
            AMSToken = doc.SelectSingleNode(".//AMSToken").InnerText;
            FLIFOToken = doc.SelectSingleNode(".//FLIFOToken").InnerText;
            UpdateInterval = Int32.Parse(doc.SelectSingleNode(".//UpdateInterval").InnerText);
            AMSRequestQueue = doc.SelectSingleNode(".//AMSRequestQueue")?.InnerText;

            foreach (XmlNode node in doc.SelectNodes(".//departureMapping")) {
                departureFields.Add(new Tuple<string, string>(node.Attributes["property"].Value, node.Attributes["externalName"].Value));
            }

            foreach (XmlNode node in doc.SelectNodes(".//arrivalMapping")) {
                arrivalFields.Add(new Tuple<string, string>(node.Attributes["property"].Value, node.Attributes["externalName"].Value));
            }
        }

        public string ExecuteFile { get; }
        public string Server { get; }

        public void OnStart() {
            var t = new Task(() => {
                Init();
            });
            t.Start();

            updateTimer = new Timer {
                Interval = UpdateInterval * 1000,
                AutoReset = true,
                Enabled = true
            };
            updateTimer.Elapsed += CheckUpdateFromFLIFO;
        }

        public void Init() {
            // ProcessFile(@"C:\Users\dave_\Desktop\input\FRA Arrivals.json");
            // ProcessFile(@"C:\Users\dave_\Desktop\input\FRA Departure.json");

            InitFromFlifo(HomeAirport, "4", "4", "A", FLIFOToken);
            InitFromFlifo(HomeAirport, "4", "4", "D", FLIFOToken);

            lastUpdate = DateTime.Now;
        }

        private void InitFromFlifo(string apt, string pastWindow, string futurWindow, string direction, string token) {
            string url = String.Format(InitTemplate, apt, direction, pastWindow, futurWindow);

            RestResponse resp = GetRestURI(url, token).Result;
            if (resp.StatusCode == HttpStatusCode.OK) {
                ProcessResponse(resp.Content);
            } else {
                Console.WriteLine(resp.StatusCode);
            }
        }

        private void CheckUpdateFromFLIFO(object sender, ElapsedEventArgs e) {
            arrLogger.Info("Checking for Arrival Updates");
            UpdateFromFLIFO(HomeAirport, "A", FLIFOToken);
            depLogger.Info("Checking for Departure Updates");
            UpdateFromFLIFO(HomeAirport, "D", FLIFOToken);

            lastUpdate = DateTime.Now;
        }

        private void UpdateFromFLIFO(string apt, string direction, string token) {
            string to = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            string from = lastUpdate.AddSeconds(-10).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

            string url = String.Format(UpdateTemplate, apt, direction, from, to);

            logger.Trace(url);

            RestResponse resp = GetRestURI(url, token).Result;
            if (resp.StatusCode == HttpStatusCode.OK) {
                ProcessResponse(resp.Content);
            } else {
                Console.WriteLine(resp.StatusCode);
            }
        }

        public void OnStop() {
        }

        public static string FormatXML(string xml) {
            try {
                StringBuilder sb = new StringBuilder();
                TextWriter tr = new StringWriter(sb);
                XmlTextWriter wr = new XmlTextWriter(tr) {
                    Formatting = System.Xml.Formatting.Indented
                };

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                doc.Save(wr);
                wr.Close();
                return sb.ToString();
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return xml;
            }
        }

        private void ProcessResponse(string response) {
            JsonReader reader = new JsonTextReader(new StringReader(response));
            reader.DateParseHandling = DateParseHandling.None;
            JObject o = JObject.Load(reader);

            var flightRecords = o["flightRecords"];

            if (flightRecords == null) {
                logger.Warn("No Updates");
                return;
            }

            foreach (var flight in flightRecords) {
                Flight flt = new Flight((JObject)flight, HomeAirport, arrivalFields, departureFields, HomeAirportSub);
                string xml = FormatXML(flt.GetAMSFlightCreate(AMSToken));
                if (flt.IsArrival) {
                    arrLogger.Info($"Updating Arrival {flt.Info}");
                    arrLogger.Trace(xml);
                } else {
                    depLogger.Info($"Updating Departure  {flt.Info}");
                    depLogger.Trace(xml);
                }

                if (AMSRequestQueue != null && xml != null) {
                    SendToAMS(xml);
                }
            }
        }

        private void SendToAMS(string xml) {
            try {
                using (MessageQueue msgQueue = new MessageQueue(AMSRequestQueue)) {
                    try {
                        var body = Encoding.ASCII.GetBytes(xml);
                        Message myMessage = new Message(body, new ActiveXMessageFormatter());
                        msgQueue.Send(myMessage);
                    } catch (Exception ex) {
                        logger.Error(ex.Message);
                        logger.Error(ex.StackTrace);
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"MSMQ Error Sending to {AMSRequestQueue}");
            }
        }

        public async Task<RestResponse> GetRestURI(string uri, string token) {
            RestResponse response = new RestResponse() { StatusCode = HttpStatusCode.NoContent };

            try {
                HttpClient _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.Add("X-apiKey", token);

                using (var result = await _httpClient.GetAsync(uri)) {
                    response.StatusCode = result.StatusCode;
                    response.Content = result.Content.ReadAsStringAsync().Result;

                    return response;
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return response;
            }
        }
    }
}