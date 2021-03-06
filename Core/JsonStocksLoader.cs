using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Invest.Core.Entities;
using Invest.Core.Enums;

namespace Invest.Core
{
	public interface IStocksProvider
	{
		List<BaseStock> Stocks { get; }
		List<BaseCompany> Companies { get; }
	}

    public class JsonStocksLoader : IStocksProvider
    {
		private readonly string _fileName;

		public JsonStocksLoader(string fileName)
		{
			_fileName = fileName;
			Load();
		}

		private void Load()
	    {
			if (!File.Exists(_fileName))
				throw new Exception($"Load(): file '{_fileName}' not found.");

            using(var fs = File.OpenText(_fileName))
            {
                var content = fs.ReadToEnd();
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject(content);
                if (data == null)
	                throw new Exception("Load(): stocks data not found.");
                
                //Instance.Companies = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Company>>(((Newtonsoft.Json.Linq.JObject)data)["companies"].ToString());
                Companies = new List<BaseCompany>();
                Stocks = new List<BaseStock>();

                // data
                foreach(var val in ((Newtonsoft.Json.Linq.JObject)data)["stocks"])
                {
	                var companyId = val["id"].ToString();
	                var company = new Company { Id = companyId, Name = val["name"].ToString(), DivName = val["divName"].ToString() };
					Companies.Add(company);

					foreach(var s in val["stocks"])
					{
						var stock = new Stock
						{
							Name = s["brokerName"].ToString(),
							Company = company,
							Currency = s["cur"] != null && s["cur"].ToString() != ""
								? (Currency)Enum.Parse(typeof(Currency), s["cur"].ToString(), true)
								: Currency.Rur,
							LotSize = s["lot"] != null && s["lot"].ToString() != "" ? int.Parse(s["lot"].ToString()) : 1,
							Ticker = s["t"].ToString(),
							Type = s["type"] != null && !string.IsNullOrEmpty(s["type"].ToString())
								? (StockType) Enum.ToObject(typeof(StockType), (int)s["type"])
								: StockType.Share
						};

						if (!string.IsNullOrEmpty(s["isin"].ToString()))	
						{
							var isins = s["isin"].ToString().Replace(" ", "");
							stock.Isin = isins.Split(new[]{','}, StringSplitOptions.RemoveEmptyEntries);
						}

						if (s["regNum"] != null && !string.IsNullOrEmpty(s["regNum"].ToString()))	
							stock.RegNum = s["regNum"].ToString();

						if (Stocks.FirstOrDefault(x => x.Ticker == stock.Ticker) != null)
							throw new Exception("Attempt to add a stock with a duplicate ticker.");

						Stocks.Add(stock);
					}
				}
            }
	    }

	    public List<BaseStock> Stocks { get; private set; }

	    public List<BaseCompany> Companies { get; private set; }
    }
}
