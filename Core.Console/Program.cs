using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Core.Console;
using Invest.Common;
using Invest.Core.Enums;
using log4net;
using log4net.Config;
using Microsoft.Extensions.Configuration;

namespace Invest.Core.Console
{
    class Program
    {
	    private static readonly ILog Log = LogManager.GetLogger(typeof(Program));
	    private static ApiController _apiController;
		private static Builder _builder;

        static void Main(string[] args)
        {
	        var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(GetAppSettingsFile());

            var configuration = builder.Build();
            
            RegisterLog4Net();
            WriteTitle();

            var config = new Config();
            try
            {
                config.Parse(configuration);
            }
            catch(Exception ex)
            {
                Log.Error("Config(): Error at parsing config params", ex);
                MessageHappened($"Error at parsing config params. {ex.Message}. See logfile for details.", MessageType.Error, "red", false);
            }

            WriteConfigParams(config);

            //WriteCurrentDirectories(dataProvider);
            //WriteJobs("Jobs:", _service.DataProvider.GetJobs().ToList());
            
            var webHost = new HttpHost(config.ServiceApi);
            //webHost.OnStateChanged += WebHost_OnStateChanged;
            webHost.MessageHappened += MessageHappened;
            webHost.RequestSentHandler += WebHost_RequestSent;
            //new Thread(() => {
                webHost.Run();
            //}).Start();

			InitCore(config);

			_apiController = new ApiController(_builder /*dataProvider, _jobManager*/ /*, config.ServiceApi*/);
			//_apiController.OnCommandRecieved += ApiController_OnCommandRecieved;

            // console commands
            var consoleMgr = new CmdManager(webHost);
            try
            {
                consoleMgr.Loop();
            }
            catch(Exception ex)
            {
                Log.Error($"consoleMgr Error, {ex}");
                System.Console.WriteLine($"Input cmd error: {ex.Message}");
                consoleMgr.Loop();
            }
        }

        private static void InitCore(Config config)
        {
			WriteLoadProcessBegin();

	        var fileName = Path.Combine(config.Import.ReportRootDir, "stocksData.json");

	        _builder = new Builder();

	        var accounts = _builder.LoadAccountsFromJson(fileName);
	        _builder.SetAccounts(accounts);

	        System.Console.WriteLine($"\t- Accounts loaded ({accounts.Count})");

	        var portfolios = _builder.LoadPortfoliosFromJson(fileName);
	        _builder.SetPortfolios(portfolios);

	        System.Console.WriteLine($"\t- Portfolios loaded ({accounts.Count})");

	        _builder.AddCurRates(
		        //new CbrCurrencyRate(new[]{ Currency.Usd, Currency.Eur }){ StartDate = new DateTime(2019, 1,1), EndDate = DateTime.Today }
		        new FakeCurrencyRate(new[]{ Currency.Usd, Currency.Eur }){ StartDate = new DateTime(2019, 12,1) }
	        );
			
	        _builder.AddStocks(new JsonStocksLoader(fileName));

	        System.Console.WriteLine($"\t- Stocks loaded ({_builder.Stocks.Count})");
			
			foreach(var b in config.Import.Brokers) 
			{
				var reportDir = Path.Combine(config.Import.ReportRootDir, b.Dir);

				IBrokerImport importInstance;

				if (b.Id == "vtb")
					importInstance = new VtbBrokerReport(reportDir, _builder);
				else if (b.Id == "sber")
					importInstance = new VtbBrokerReport(reportDir, _builder);
				else if (b.Id == "ab")
					importInstance = new VtbBrokerReport(reportDir, _builder);
				else if (b.Id == "bks")
					importInstance = new VtbBrokerReport(reportDir, _builder);
				else 
					throw new Exception($"Import reports: undefined broker id ({b.Id})");

				_builder.Import(importInstance);
			}
			
			_builder.Calc();

	        WriteLoadProcessEnd();
        }

        private static string GetAppSettingsFile()
        {
            var appFile = "appsettings.json";
#if DEBUG
            appFile = "appsettings.debug.json";
#endif

            if (!File.Exists(appFile))
            {
                var msg = $"Load(): appSettings file '{appFile}' not found";
                Log.Error(msg);
                throw new Exception(msg);
            }

            return appFile;
        }

        private static void RegisterLog4Net()
        {
            const string fileName = "log4net.config";
            var configFile = new FileInfo(fileName);
            if (!configFile.Exists)
                throw new Exception($"RegisterLog4Net(): {fileName} not found");

            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, configFile);
        }


        private static void WriteTitle()
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version;
            System.Console.Title = $"Invest.Core (ver.: {version?.Major ?? 0}.{version?.Minor ?? 0})";
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine($"Invest.Core started at {DateTime.Now}");
            System.Console.ResetColor();
        }

        private static void WriteConfigParams(Config config)
        {
	        System.Console.WriteLine();
	        System.Console.ForegroundColor = ConsoleColor.Blue;
	        System.Console.BackgroundColor = ConsoleColor.Yellow;
	        System.Console.WriteLine("Configurations:");
	        System.Console.ResetColor();

	        System.Console.ForegroundColor = ConsoleColor.White;
	        System.Console.WriteLine("====================================================================");
	        System.Console.WriteLine($"{"ServiceApi, url",26}: {config.ServiceApi.Url, 40}");
	        System.Console.WriteLine($"{"RootDirectory",26}: {config.Import.ReportRootDir,40}");
	        //System.Console.WriteLine($"{"JobDirectorStatusInterval",26}: {config.JobDirectorStatusInterval,40}");
	        
	        System.Console.WriteLine();
	        System.Console.ResetColor();
        }

        private static void WriteLoadProcessBegin()
        {
			System.Console.WriteLine();
			System.Console.ForegroundColor = ConsoleColor.Blue;
			System.Console.BackgroundColor = ConsoleColor.White;
			System.Console.WriteLine("Import porcessing:");
			System.Console.ResetColor();
		}

        private static void WriteLoadProcessEnd()
        {
	        System.Console.WriteLine();
	        System.Console.ResetColor();
		}


        private static void MessageHappened(string msg, MessageType type, string color, bool showTime)
        {
            var text = msg;

            if (type == MessageType.Warning)
            {
                System.Console.ForegroundColor = ConsoleColor.Magenta;
                text = " *: " + text;
            }
            else if (type == MessageType.Info)
            {
	            System.Console.ForegroundColor = ConsoleColor.White;
                text = " i: " + text;
            }
            else if (type == MessageType.Error)
            {
	            System.Console.BackgroundColor = ConsoleColor.Red;
	            System.Console.ForegroundColor = ConsoleColor.White;
                text = " E: " + text;
            }

            MapColor(color);

            if (showTime)
                text = $"{DateTime.Now:dd MMM, HH:mm:ss}: {msg}";

            System.Console.WriteLine(text);
            System.Console.ResetColor();
            System.Console.BackgroundColor = ConsoleColor.Black;
            // Console.WriteLine("");
        }

        private static void MapColor(string color)
        {
            if (!string.IsNullOrEmpty(color))
            {
                if (color == "green")
	                System.Console.ForegroundColor = ConsoleColor.Green;
                else if (color == "orange")
	                System.Console.ForegroundColor = ConsoleColor.Red;
            }
        }

        private static void WebHost_RequestSent(object sender, ApiEventArgs e)
        {
	        Debug.WriteLine($"WebHost_RequestSent(): '{e.Url}'");
	        var res = _apiController.Parse(e.Url, e.HttpMethod, e.Body, e.Params);

	        e.Callback?.Invoke(res);
        }
    }


    public delegate void MessageHandler(string msg, MessageType type = MessageType.Info, string color = "white", bool showTime = false);

	[Flags]
    public enum MessageType
    {
	    None = 0,
	    Info = 1,
	    Warning = 2,

	    Error = 64
    }
}
