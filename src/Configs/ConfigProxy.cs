using System;
using System.Collections.Generic;
using System.Text;

namespace ProxyTor.Configs
{
    /// <summary>
    /// Configuration proxy server
    /// </summary>
    public class ConfigProxy
    {
        /// <summary>
        /// Port for listening
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Tors proxies
        /// </summary>
        public List<TorConfig> Tors { get; set; }

        /// <summary>
        /// Flag to use sometimes local ip address without nay proxies
        /// </summary>
        public bool UseLocalIp { get; set; }
    }
}
