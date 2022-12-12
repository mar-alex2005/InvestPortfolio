using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using Core.Console;
using Invest.Common;
using log4net;

namespace Invest.Core.Console
{
    public class HttpHost : IHttpHost
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HttpHost));
        private HttpListener _listener;
        public event MessageHandler MessageHappened;
        private readonly Config.ServiceAPI _serviceApi;
        
        //public delegate void StateChangedHandler(Service.States state);
        //public event StateChangedHandler OnStateChanged;

        public event EventHandler<ApiEventArgs> RequestSentHandler;

        public HttpHost(Config.ServiceAPI serviceApi)
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");

            if (string.IsNullOrEmpty(serviceApi.Url))
                throw new ArgumentException("HttpHost(): URI host are required");

            _serviceApi = serviceApi;
            CreateListener();

            Log.Debug($"HttpHost. ctor(), {_serviceApi.Url}");
        }

        private void CreateListener()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(_serviceApi.Url);
        }

        public void Run()
        {
            if (IsRunning) {
                MessageHappened?.Invoke("Api webHost already started", MessageType.Warning, null);
                return;
            }

            if (_listener == null)
                CreateListener();

            _listener.Start();

            Log.Debug($"HttpHost. Api webhost running.., {_serviceApi.Url}");
            //OnStateChanged?.Invoke(Service.States.Started);

            new Thread(() => {
                Loop();
            }).Start();
        }

        private void Loop()
        {
            //ThreadPool.QueueUserWorkItem(o => {
            try
            {
                while (_listener.IsListening)
                {
                    ThreadPool.QueueUserWorkItem(c =>
                    {
                        var ctx = c as HttpListenerContext;

                        try
                        {
                            if (ctx == null) {
                                Debug.WriteLine("HttpHost, Loop(), ctx == null");
                                return;
                            }

                            var result = Response(ctx.Request);
                            var buf = Encoding.UTF8.GetBytes(result.ToJson());
                            
                            AddContentType(ctx, buf.Length);
                            AddHeaders(ctx);

                            if (result.ErrCode == 0)
                            {
                                ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                                ctx.Response.StatusDescription = "Status OK";
                            }
                            else if (result.ErrCode == ApiResult.ErrorCode.UncorrectUrl || result.ErrCode == ApiResult.ErrorCode.InvalidInputParams)
                            {
                                ctx.Response.StatusCode = 400;
                                ctx.Response.StatusDescription = "Bad Request";
                            }
                            else
                            {
                                ctx.Response.StatusCode = 500;
                                ctx.Response.StatusDescription = "Internal Server Error";
                            }

                            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"HttpHost, Loop(): {ex.Message}, {ex}");
                        }
                        finally
                        {
                            // always close the stream
                            if (ctx != null)
                                ctx.Response.OutputStream.Close();
                        }
                    }, _listener.GetContext());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HttpHost Run(), error: {ex.Message}, {ex}");
            }
            //});
        }

        public void Stop()
        {
            if (_listener == null || !_listener.IsListening) {
                MessageHappened?.Invoke("Api webHost already stopped", MessageType.Warning, null);
                return;
            }

            if (_listener != null) {
                _listener.Stop();
                _listener.Close();
                _listener = null;
            }

            Log.Debug("HttpHost. Api webhost stopped");
            //OnStateChanged?.Invoke(Service.States.Stopped);
        }

        public bool IsRunning { 
            get { 
                return _listener != null && _listener.IsListening; 
            }
        }

        private ApiResult Response(HttpListenerRequest req)
        {
            Debug.WriteLine($"HttpHost, response: url: {req.Url}");
            Log.Debug($"HttpHost, response: url: {req.Url}");

            if (!CheckSecretKey(req.Headers.Get("x-api-key"), _serviceApi.Key))
                return new ApiResult { ErrCode = ApiResult.ErrorCode.InvalidSecretKey, Error = "Secret key is incorrect or empty" };

            var queryParams = FillQueryParams(req.QueryString);
            var ea = new ApiEventArgs(req.Url.LocalPath, req.HttpMethod, queryParams) {
                Request = req
            };

            if (req.HttpMethod == "GET") {
                // nothing_todo
            }
            else if (req.HttpMethod == "POST" || req.HttpMethod == "PUT") {
                var body = req.InputStream;
                var encoding = req.ContentEncoding;
                var reader = new System.IO.StreamReader(body, encoding);
                ea.Body = reader.ReadToEnd();
            }

            RequestSent(ea);    

            return ea.ContentResult;
        }

        private static bool CheckSecretKey(string inputKey, string configKey)
        {
            Log.Debug("CheckSecretKey():");
#if DEBUG
            return true;
#endif
            if (string.IsNullOrEmpty(configKey))
                return true;

            if (string.IsNullOrEmpty(inputKey)) {
                Log.Debug("Secret key is null or empty");
                return false;
            }

            if (configKey.Equals(inputKey))
                return true;

            return false;
        }

        private static Dictionary<string, string> FillQueryParams(NameValueCollection dict)
        {
            var queryParams = new Dictionary<string, string>();

            foreach (var key in dict.Keys)
            {
                if (key != null)
                    queryParams.Add(key.ToString()?.ToLower(), dict[key.ToString()]);
            }

            return queryParams;
        }

        private static void AddHeaders(HttpListenerContext ctx)
        {
            ctx.Response.Headers["Server"] = "Service Web Host";
        }

        private static void AddContentType(HttpListenerContext ctx, long bufSize)
        {
            ctx.Response.ContentEncoding = Encoding.UTF8;
            ctx.Response.ContentLength64 = bufSize;
            ctx.Response.ContentType = "application/json";
        }

        private void RequestSent(ApiEventArgs e)
        {
            RequestSentHandler?.Invoke(this, e);
        }
    }
}