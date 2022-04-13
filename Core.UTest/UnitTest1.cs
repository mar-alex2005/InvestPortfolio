using System.Collections.Generic;
using Invest.Core.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Invest.Core.UTest
{
    [TestClass]
    public class UnitTest1
    {
		[TestInitialize]
	    public void Setup()
	    {
	    }

        [TestMethod]
        public void TestMethod1()
        {
			var builder = new Core.Builder();

			var accounts = builder.LoadAccountsFromJson(@"C:\\Users\\Alex\\Downloads\\stocks.json");
			builder.SetAccounts(accounts);

			var portolios = builder.LoadPortfoliosFromJson(@"C:\\Users\\Alex\\Downloads\\stocks.json");
			builder.SetPortfolios(portolios);
			
			builder.AddStocks(new JsonStocksLoader(@"C:\\Users\\Alex\\Downloads\\stocks.json"));

			builder.AddReport(new VtbBrokerReport(){});
			builder.AddReport(new SberBrokerReport(){});
			builder.AddReport(new AlfaBrokerReport(){});
			
			builder.Init();
        }
    }
}
