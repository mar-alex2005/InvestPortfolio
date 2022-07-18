using System;
using System.Collections.Generic;
using System.IO;
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
	    public void FillPeriods_Test()
	    {
		    //Builder target = new Builder();
		    //PrivateObject obj = new PrivateObject(target);
		    //var retVal = obj.Invoke("PrivateMethod");
		    //Assert.AreEqual(retVal);
		}

        [TestMethod]
        public void InitTestMethod()
        {
			// @"C:\\Users\\Alex\\Downloads\\stocks.json"
	        var docFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			var fileName = Path.Combine(docFolder, "Downloads\\inv\\stocksData.json");

			var builder = new Builder();

			var accounts = builder.LoadAccountsFromJson(fileName);
			builder.SetAccounts(accounts);

			var portolios = builder.LoadPortfoliosFromJson(fileName);
			builder.SetPortfolios(portolios);
			
			builder.AddStocks(new JsonStocksLoader(fileName));

			Assert.IsNotNull(builder.Companies);
			Assert.IsNotNull(builder.Stocks);

			Assert.IsTrue(builder.Companies.Count != 0);
			Assert.IsTrue(builder.Stocks.Count != 0);

			var dir = Path.Combine(Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.Personal)), "Downloads");
			builder
				.AddReport(new VtbBrokerReport(dir, builder));

			//builder.AddReport(new SberBrokerReport(){});
			//builder.AddReport(new AlfaBrokerReport(){});
			
			builder.Calc();
        }
    }
}