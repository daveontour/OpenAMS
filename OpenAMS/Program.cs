using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Topshelf;

namespace OpenAMS {

    internal class Program {

        private static void Main(string[] args) {
            NLog.Logger logger = NLog.LogManager.GetLogger("LoadInjectorClient");

            try {
                var exitCode = HostFactory.Run(x => {
                    string executeFile = null;
                    string server = null;
                    x.AddCommandLineDefinition("execute", f => { executeFile = f; });
                    x.AddCommandLineDefinition("server", srv => { server = srv; });
                    x.ApplyCommandLine();
                    try {
                        x.Service<OpenAMSIngest>(s => {
                            s.ConstructUsing(core => new OpenAMSIngest(executeFile, server));
                            s.WhenStarted(core => core.OnStart());
                            s.WhenStopped(core => core.OnStop());
                        });
                    } catch (Exception ex) {
                        logger.Info($"Starting Load Injector Runtime Client Failed, Error {ex.Message}");
                    }
                    x.RunAsLocalSystem();

                    x.SetServiceName($"AMSOpenFLIFOIngest");
                    x.SetDisplayName("AMS Open FLIFO Ingestor");
                    x.SetDescription($"AMS Open FLIFO Ingestor. AMSOPen to AMS");
                });

                int exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
                Environment.ExitCode = exitCodeValue;
            } catch (Exception e) {
                logger.Info($"Starting HOST Factory - failed (Outer), Error {e.Message}");
            }
        }
    }
}