using log4net;
using ProxyTor.Configs;
using ProxyTor.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ProxyTor.Models;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Exceptions;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.StreamExtended.Network;

namespace ProxyTor
{
    public class ProxyTorController : IDisposable
    {
        private readonly ProxyServer _proxyServer;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken _cancellationToken => _cancellationTokenSource.Token;
        private readonly ConcurrentQueue<Tuple<ConsoleColor?, string>> _consoleMessageQueue
            = new ConcurrentQueue<Tuple<ConsoleColor?, string>>();

        // List of tors proxies
        private readonly List<TorInfo> _tors;
        private readonly object _syncObj = new object();
        private int _currProxy = 0;

        private static readonly ILog Log = LogManager.GetLogger(typeof(ProxyTorController));



        public ProxyTorController(ConfigProxy config)
        {
            Task.Run(() => listenToConsole());

            _proxyServer = new ProxyServer();


            _proxyServer.ExceptionFunc = async exception =>
            {
                if (exception is ProxyHttpException phex)
                {
                    WriteToConsole(exception.Message + ": " + phex.InnerException?.Message, ConsoleColor.Red);
                }
                else
                {
                    WriteToConsole(exception.Message, ConsoleColor.Red);
                }
            };

            _proxyServer.TcpTimeWaitSeconds = 60;
            _proxyServer.ConnectionTimeOutSeconds = 65;
            _proxyServer.ReuseSocket = false;
            _proxyServer.EnableConnectionPool = true;
            _proxyServer.ForwardToUpstreamGateway = true;

            _proxyServer.CertificateManager.SaveFakeCertificates = true;


            var explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, config.Port, false);
            _proxyServer.AddEndPoint(explicitEndPoint);

            _tors = new List<TorInfo>();

            for (var i = config.Tor.PortFrom; i <= config.Tor.PortTo;i++)
            {
                _tors.Add(new TorInfo
                {
                    HostName = config.Tor.HostName,
                    Port = i
                });
            }
        }

        public void StartProxy()
        {
            _proxyServer.BeforeRequest += OnRequest;
            _proxyServer.BeforeResponse += OnResponse;
            _proxyServer.AfterResponse += OnAfterResponse;
            _proxyServer.CertificateManager.EnsureRootCertificate(false, false, false);
            _proxyServer.Start();


            foreach (var endPoint in _proxyServer.ProxyEndPoints)
            {
                Console.WriteLine("Listening on '{0}' endpoint at Ip {1} and port: {2} ", endPoint.GetType().Name,
                    endPoint.IpAddress, endPoint.Port);
            }

            _proxyServer.GetCustomUpStreamProxyFunc = OnGetCustomUpStreamProxyFunc;
        }

        public void Stop()
        {

            _proxyServer.BeforeRequest -= OnRequest;
            _proxyServer.BeforeResponse -= OnResponse;

            _proxyServer.Stop();
        }

        private async Task<IExternalProxy> OnGetCustomUpStreamProxyFunc(SessionEventArgsBase arg)
        {
            arg.GetState().PipelineInfo.AppendLine(nameof(OnGetCustomUpStreamProxyFunc));

            TorInfo torInfo;

            lock (_syncObj)
            {
                torInfo = _tors[_currProxy];
                _currProxy++;
                if (_currProxy >= _tors.Count)
                {
                    _currProxy = 0;
                }
            }


            // this is just to show the functionality, provided values are junk
            return new ExternalProxy
            {
                BypassLocalhost = false,
                ProxyType = ExternalProxyType.Socks5,
                HostName = torInfo.HostName,
                Port = torInfo.Port,
                UseDefaultCredentials = false
            };
        }


        private void WebSocket_DataSent(object sender, DataEventArgs e)
        {
            var args = (SessionEventArgs)sender;
            WebSocketDataSentReceived(args, e, true);
        }

        private void WebSocket_DataReceived(object sender, DataEventArgs e)
        {
            var args = (SessionEventArgs)sender;
            WebSocketDataSentReceived(args, e, false);
        }

        private void WebSocketDataSentReceived(SessionEventArgs args, DataEventArgs e, bool sent)
        {
            var color = sent ? ConsoleColor.Green : ConsoleColor.Blue;

            foreach (var frame in args.WebSocketDecoder.Decode(e.Buffer, e.Offset, e.Count))
            {
                if (frame.OpCode == WebsocketOpCode.Binary)
                {
                    var data = frame.Data.ToArray();
                    string str = string.Join(",", data.ToArray().Select(x => x.ToString("X2")));
                    WriteToConsole(str, color);
                }

                if (frame.OpCode == WebsocketOpCode.Text)
                {
                    WriteToConsole(frame.GetText(), color);
                }
            }
        }


        // intercept & cancel redirect or update requests
        private async Task OnRequest(object sender, SessionEventArgs e)
        {
            e.GetState().PipelineInfo.AppendLine(nameof(OnRequest) + ":" + e.HttpClient.Request.RequestUri);

            var clientLocalIp = e.ClientLocalEndPoint.Address;
            if (!clientLocalIp.Equals(IPAddress.Loopback) && !clientLocalIp.Equals(IPAddress.IPv6Loopback))
            {
                e.HttpClient.UpStreamEndPoint = new IPEndPoint(clientLocalIp, 0);
            }

            WriteToConsole("Active Client Connections:" + ((ProxyServer)sender).ClientConnectionCount + " " + e.HttpClient.Request.Url);
        }

        // Modify response
        private async Task MultipartRequestPartSent(object sender, MultipartRequestPartSentEventArgs e)
        {
            e.GetState().PipelineInfo.AppendLine(nameof(MultipartRequestPartSent));

            var session = (SessionEventArgs)sender;
            WriteToConsole("Multipart form data headers:");
            foreach (var header in e.Headers)
            {
                WriteToConsole(header.ToString());
            }
        }

        private async Task OnResponse(object sender, SessionEventArgs e)
        {
            e.GetState().PipelineInfo.AppendLine(nameof(OnResponse));

            if (e.HttpClient.ConnectRequest?.TunnelType == TunnelType.Websocket)
            {
                e.DataSent += WebSocket_DataSent;
                e.DataReceived += WebSocket_DataReceived;
            }

            WriteToConsole("Active Server Connections:" + ((ProxyServer)sender).ServerConnectionCount + " " + e.HttpClient.Request.RequestUri);
        }

        private async Task OnAfterResponse(object sender, SessionEventArgs e)
        {
            WriteToConsole($"Pipelineinfo: {e.GetState().PipelineInfo}", ConsoleColor.Yellow);
        }

        private void WriteToConsole(string message, ConsoleColor? consoleColor = null)
        {
            _consoleMessageQueue.Enqueue(new Tuple<ConsoleColor?, string>(consoleColor, message));
            Log.Info(message);
        }

        /// <summary>
        /// Listening console
        /// </summary>
        private async Task listenToConsole()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                while (_consoleMessageQueue.TryDequeue(out var item))
                {
                    var consoleColor = item.Item1;
                    var message = item.Item2;

                    if (consoleColor.HasValue)
                    {
                        ConsoleColor existing = Console.ForegroundColor;
                        Console.ForegroundColor = consoleColor.Value;
                        Console.WriteLine(message);
                        Console.ForegroundColor = existing;
                    }
                    else
                    {
                        Console.WriteLine(message);
                    }
                }

                //reduce CPU usage
                await Task.Delay(50);
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
            _proxyServer?.Dispose();
        }
    }
}
