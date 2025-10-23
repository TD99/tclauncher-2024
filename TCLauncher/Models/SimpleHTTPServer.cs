// ReSharper disable MemberCanBePrivate.Global

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace TCLauncher.Models
{
    /// <summary>
    /// Represents a simple HTTP server.
    /// </summary>
    public class SimpleHttpServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly Func<HttpListenerRequest, string> _responderMethod;
        private readonly bool _useDefaultCorsConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleHttpServer"/> class.
        /// </summary>
        /// <param name="prefixes">The prefixes to add to the listener.</param>
        /// <param name="method">The method to respond to HTTP requests.</param>
        /// <param name="useDefaultCorsConfiguration">Whether to use the default CORS configuration.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public SimpleHttpServer(IReadOnlyCollection<string> prefixes, Func<HttpListenerRequest, string> method, bool useDefaultCorsConfiguration = false)
        {
            _useDefaultCorsConfiguration = useDefaultCorsConfiguration;

            if (!HttpListener.IsSupported)
                throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");

            if (prefixes == null || prefixes.Count == 0)
                throw new ArgumentException("prefixes");

            foreach (var s in prefixes)
                _listener.Prefixes.Add(s);

            _responderMethod = method ?? throw new ArgumentException("method");
            _listener.Start();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleHttpServer"/> class.
        /// </summary>
        /// <param name="method">The method to respond to HTTP requests.</param>
        /// <param name="prefixes">The prefixes to add to the listener.</param>
        public SimpleHttpServer(Func<HttpListenerRequest, string> method, params string[] prefixes)
            : this(prefixes, method) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleHttpServer"/> class.
        /// </summary>
        /// <param name="method">The method to respond to HTTP requests.</param>
        /// <param name="useCors">Whether to use CORS.</param>
        /// <param name="prefixes">The prefixes to add to the listener.</param>
        public SimpleHttpServer(Func<HttpListenerRequest, string> method, bool useCors, params string[] prefixes)
            : this(prefixes, method, useCors) { }

        /// <summary>
        /// Starts the HTTP server.
        /// </summary>
        public void Run()
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Console.WriteLine(@"Webserver running...");
                try
                {
                    while (_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                if (ctx == null) return;
                                var rstr = _responderMethod(ctx.Request);
                                var buf = Encoding.UTF8.GetBytes(rstr);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.ContentType = "application/json";
                                if (_useDefaultCorsConfiguration)
                                {
                                    ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");
                                    ctx.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept");
                                    ctx.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                                }
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                            catch
                            {
                                // ignored
                            }
                            finally
                            {
                                ctx?.Response.OutputStream.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch
                {
                    // ignored
                }
            });
        }

        /// <summary>
        /// Stops the HTTP server.
        /// </summary>
        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }

    }
}
