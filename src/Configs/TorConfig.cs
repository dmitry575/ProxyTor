namespace ProxyTor.Configs
{
    /// <summary>
    /// Configuration Tors proxy
    /// </summary>
    public class TorConfig
    {
        /// <summary>
        /// Host that listening Tor
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// Port that listening Tor
        /// </summary>
        public int PortFrom { get; set; }

        /// <summary>
        /// Port that listening Tor
        /// </summary>
        public int PortTo { get; set; }
    }
}
