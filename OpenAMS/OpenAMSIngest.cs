using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private System.Timers.Timer updateTimer;
        private DateTime lastUpdate;

        public OpenAMSIngest(string executeFile, string server) {
            ExecuteFile = executeFile;
            Server = server;
        }

        public string ExecuteFile { get; }
        public string Server { get; }

        public void OnStart() {
            var t = new Task(() => {
                Init();
            });
            t.Start();

            updateTimer = new Timer {
                Interval = 6000,
                AutoReset = true,
                Enabled = true
            };
            updateTimer.Elapsed += CheckUpdateFromFLIFO;
        }

        public void Init() {
            // ProcessFile(@"C:\Users\dave_\Desktop\input\FRA Arrivals.json");
            // ProcessFile(@"C:\Users\dave_\Desktop\input\FRA Departure.json");

            InitFromFlifo("FRA", "4", "4", "A", "SZpvZxtHHeyo3mwhmsGhMKcZGLtEnRG2");
            InitFromFlifo("FRA", "4", "4", "D", "SZpvZxtHHeyo3mwhmsGhMKcZGLtEnRG2");

            lastUpdate = DateTime.Now;
        }

        private void InitFromFlifo(string apt, string pastWindow, string futurWindow, string direction, string token) {
            string urlTemplate = "https://flifo-qa.api.aero/flifo/flightinfo/v1/flights/airport/{0}/direction/{1}?pastWindow={2}&futureWindow={3}&searchByUTC=true&showCargo=false&groupMarketingCarriers=true&view=full";
            string url = String.Format(urlTemplate, apt, direction, pastWindow, futurWindow);

            RestResponse resp = GetRestURI(url, token).Result;
            if (resp.StatusCode == HttpStatusCode.OK) {
                ProcessResponse(resp.Content);
            } else {
                Console.WriteLine(resp.StatusCode);
            }
        }

        private void CheckUpdateFromFLIFO(object sender, ElapsedEventArgs e) {
            Console.WriteLine("Updating Arrivals");
            UpdateFromFLIFO("FRA", "A", "SZpvZxtHHeyo3mwhmsGhMKcZGLtEnRG2");
            Console.WriteLine("Updating Departures");
            UpdateFromFLIFO("FRA", "D", "SZpvZxtHHeyo3mwhmsGhMKcZGLtEnRG2");

            lastUpdate = DateTime.Now;
        }

        private void UpdateFromFLIFO(string apt, string direction, string token) {
            string to = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            string from = lastUpdate.AddSeconds(-10).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

            string urlTemplate = "https://flifo-qa.api.aero/flifo/flightinfo/v1/flights/updates/airport/{0}?from={2}&to={3}&showCargo=false&direction={1}&view=full";
            string url = String.Format(urlTemplate, apt, direction, from, to);

            Console.WriteLine(url);
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

        private static void ProcessResponse(string response) {
            JsonReader reader = new JsonTextReader(new StringReader(response));
            reader.DateParseHandling = DateParseHandling.None;
            JObject o = JObject.Load(reader);

            var flightRecords = o["flightRecords"];

            if (flightRecords == null) {
                Console.WriteLine("No Updates");
                return;
            }

            foreach (var flight in flightRecords) {
                Flight flt = new Flight((JObject)flight, "FRA");
                Console.WriteLine(FormatXML(flt.FlightIDXML));
            }
        }

        private static void ProcessFile(string filename) {
            JsonReader reader = new JsonTextReader(new StringReader(File.ReadAllText(filename)));
            reader.DateParseHandling = DateParseHandling.None;
            JObject o = JObject.Load(reader);

            var flightRecords = o["flightRecords"];

            foreach (var flight in flightRecords) {
                Flight flt = new Flight((JObject)flight, "FRA");
                Console.WriteLine(FormatXML(flt.FlightIDXML));
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