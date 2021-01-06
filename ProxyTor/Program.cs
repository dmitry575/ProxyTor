using ProxyTor.Helpers;
using System;
using Titanium.Web.Proxy.Helpers;

namespace ProxyTor
{
    class Program
    {
        private static readonly ProxyTestController controller = new ProxyTestController();
        static void Main(string[] args)
        {
            if (RunTime.IsWindows)
            {
                // fix console hang due to QuickEdit mode
                ConsoleHelper.DisableQuickEditMode();
            }

            // Start proxy controller
            controller.StartProxy();

            Console.WriteLine("Hit any key to exit..");
            Console.WriteLine();
            Console.Read();

            controller.Stop();
        }
    }
}
