using System;
using System.Collections.Generic;
using System.Text;
using Invest.Common;

namespace Invest.Core.Console
{
    public class CmdManager
    {
		//private readonly Service _service;
        private readonly IHttpHost _webHost;

        public CmdManager(IHttpHost webHost) 
        {
            //_service = service;
            _webHost = webHost;
        }

        private class Command
        {
            public readonly string Name;
            public readonly string Param;
            
            public Command(string line)
            {
                var arr = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (arr.Length == 1)
                {
                    Name = arr[0];
                }
                else if (arr.Length == 2)
                {
                    Name = arr[0].ToLower();
                    Param = arr[1];

                    if (Param.StartsWith("-"))
                    {
                        Param = Param.Substring(1);
                    }
                    else
                    {
                        Param = null;
                    }                
                }
            }
        }

        public void Loop()
        {
            while (true)
            {
                var literal = System.Console.ReadLine();
                var cmd = new Command(literal);
                if (cmd.Name == null)
                    continue;

                if (cmd.Name == "start")
                {
                    //_service?.Start();
                }
                else if (cmd.Name == "stop")
                {
                    //_service?.Stop();
                }
                else if (cmd.Name == "web")
                {
                    if (cmd.Param == "start")
                    {
                        _webHost?.Run();
                    }
                    else if (cmd.Param == "stop")
                    {
                        _webHost?.Stop();
                    }
                }
                else if (cmd.Name == "status")
                {
                    //System.Console.WriteLine("------------------------------------------------------------");
                    //System.Console.WriteLine("Status:");
                    //System.Console.WriteLine($"\tService started at: {_service?.StartedOn:dd.MMM.yy HH:mm:ss}");
                    //System.Console.WriteLine($"\tWork time: {DateTime.Now -_service?.StartedOn}");
                    //System.Console.WriteLine($"\tService status: {_service?.State}");
                    //System.Console.WriteLine("------------------------------------------------------------");
                }

                else if (cmd.Name == "exit")
                {
                    if (_webHost != null)
                        _webHost.Stop();
                    //if (_service != null)
                    //{
                    //    _service.Stop();
                    //    //isRead = false;
                    //    Environment.Exit(0);
                    //}
                }

                //else
                  //  _service?.NotifyUnsupportedCmd();
            }
        }
    }
}
