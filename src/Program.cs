using ProxyTor.Helpers;
using System;
using Titanium.Web.Proxy.Helpers;
using log4net.Config;
using Microsoft.Extensions.Configuration;
using System.IO;
using ProxyTor.Configs;
using log4net;

[assembly: XmlConfigurator(Watch = true, ConfigFile = "log4net.config")]
namespace ProxyTor
{
    class Program
    {
        private static IConfiguration Configuration { get; set; }
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));
        static void Main(string[] args)
        {
            if (RunTime.IsWindows)
            {
                // fix console hang due to QuickEdit mode
                ConsoleHelper.DisableQuickEditMode();
            }

            //get configuration
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                
                .Build();

            var config = GetConfig();
            if (config == null)
            {
                Console.WriteLine("Invalid configurations:\r\n" +
                    "Using d.json with parameters:\r\n" +
                    "Port - number of proxy port\r\n" +
                    "UseLocalIp - sometime using your IP address without any tor proxy\r\n" +
                    "Tors - settings for tors proxies");
                return;
            }

            using (var controller = new ProxyTorController(config))
            {
                // Start proxy controller
                controller.StartProxy();

                Console.WriteLine("Hit escape to exit..");

                // it's need only for docker
                var k = 0;
                do
                {
                    k = Console.Read();
                } while (k != (int)ConsoleKey.Escape);

                controller.Stop();
                
                Console.WriteLine("Exit from program...");
            }
        }

        private static ConfigProxy GetConfig()
        {
            try
            {
                var config = new ConfigProxy();

                config.Port = Configuration.GetValue<int>("Port");
                config.Tor = Configuration.GetSection("Tor").Get<TorConfig>();
                config.UseLocalIp = Configuration.GetValue<bool>("UseLocalIp");
                return config;
            }
            catch (Exception e)
            {
                Log.Error("Error reading config: ", e);
                Console.WriteLine("Error reading config");
                return null;
            }
        }
    }
}
