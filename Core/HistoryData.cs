using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Invest.Core.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Invest.Core
{
    public class HistoryData
    {
		private string _fileName = "history.json";

		public void Load()
		{
			LoadFile();

			foreach(var s in Core.Instance.Stocks)
				//.Where(x => x.Ticker == "SBER" || x.Ticker == "GAZP" || x.Ticker == "NLMK" || x.Ticker == "YNDX" || x.Ticker == "BANEP"  || x.Ticker == "NVTK" || x.Ticker == "MTSS"))
			{
				if (s.Company == null || s.Ticker == null)
                    continue;

				var h = Core.Instance.History.FirstOrDefault(x => x.Ticker == s.Ticker);
				s.LastHistotyDate = h != null ? h.LastDate : DateTime.MinValue;

				if (h == null)					
				{
					h = new History(s);
					Core.Instance.History.Add(h);				
				}

				if (s.LastHistotyDate < DateTime.Today.Date.AddDays(-2))
				{
					GetMoexHistoryDate(s, h);
				}
			}

			Save();
		}

		public void GetMoexHistoryDate(Stock s, History h)
        {
			var hd = s.LastHistotyDate > new DateTime(2019, 12,1) ? s.LastHistotyDate.AddDays(1) : new DateTime(2019, 12,1);
            var url = string.Format("http://iss.moex.com/iss/history/engines/stock/markets/shares/boards/tqbr/securities/{0}.json?from={1:yyyy-MM-dd}&iss.meta=off&iss.only=history&history.columns=TRADEDATE,CLOSE", 
				s.Ticker, hd);

			var req = (HttpWebRequest)WebRequest.Create(url);
			var resp = (HttpWebResponse)req.GetResponse();

			using (var stream = new StreamReader(resp.GetResponseStream()))
			{
				var response = stream.ReadToEnd();
				var data = Newtonsoft.Json.JsonConvert.DeserializeObject(response);
				var historyData = ((Newtonsoft.Json.Linq.JObject)data)["history"]["data"];

				foreach (var d in historyData)
				{
					if (DateTime.TryParse(d[0].ToString(), out var p))
					{
						if (p > DateTime.MinValue && d[1] != null && d[1].ToString() != "")
						{
							var date = DateTime.Parse(d[0].ToString());
							h.Items.Add(date, new HistoryItem { Close = decimal.Parse(d[1].ToString()) } );
							h.LastDate = date;
						}

						s.CurPriceUpdatedOn = DateTime.Today.Date;
					}					
				}
			}
		}

		private void LoadFile()
        {
            var root = "wwwroot";
            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                root,
                _fileName
            );

            if (!File.Exists(path))
                return;

            string jsonResult;

            using (var streamReader = new StreamReader(path))
            {
                jsonResult = streamReader.ReadToEnd();
            }

            var sett = new JsonSerializerSettings { DateFormatString = "yyyy-MM-dd" };
            //var data = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonResult, sett);
			var  data = JToken.Parse(jsonResult);

			var childs = data.Children();
			foreach(var j in childs)
			{
				var item = new History() { 
					Ticker = j["Ticker"].ToString(), 
					LastDate = DateTime.Parse(j["LastDate"].ToString()), 
					Items = new Dictionary<DateTime, HistoryItem>() };				
				
				foreach(var i in j["Items"])
				{
					item.Items.Add(DateTime.Parse(i["d"].ToString()), new HistoryItem(){ Close = decimal.Parse(i["Value"]["c"].ToString()) } );
				}

				Core.Instance.History.Add(item);
			}
        }

		private void Save()
		{
			var list = Core.Instance.History
				.Select(x => new { x.Ticker, LastDate = string.Format("{0:yyyy-MM-dd}", x.LastDate), 
						Items = x.Items.Select(y => new {d = string.Format("{0:yyyy-MM-dd}", y.Key), y.Value})
					})
				;

			//DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Dictionary<int, List<int>>));
			var json = JsonConvert.SerializeObject(list, Formatting.Indented);

			var root = "wwwroot";
            var path = Path.Combine(Directory.GetCurrentDirectory(), root, _fileName );

            using (var streamWriter = File.CreateText(path))
            {
                streamWriter.Write(json);
            }
		}
    }
}