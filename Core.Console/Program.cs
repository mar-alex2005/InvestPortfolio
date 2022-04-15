using System;
using System.IO;
using Invest.Core;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
	        var docFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
	        var fileName = Path.Combine(docFolder, "stocksData.json");

	        var builder = new Builder();

	        var accounts = builder.LoadAccountsFromJson(fileName);
	        builder.SetAccounts(accounts);

	        var portolios = builder.LoadPortfoliosFromJson(fileName);
	        builder.SetPortfolios(portolios);
			
	        builder.AddStocks(new JsonStocksLoader(fileName));

	        //builder.AddReport(new VtbBrokerReport(){});
	        //builder.AddReport(new SberBrokerReport(){});
	        //builder.AddReport(new AlfaBrokerReport(){});
			
	        builder.Calc();

			var r = builder.GetCurRate(new DateTime(2022, 4,1));


            Console.ReadKey();
        }
    }
}
