using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Invest.Core.Entities;

namespace Invest.Core
{
   public class PriceManager
    {
        private readonly string _fileName = "prices.json";

        //public void Load()
        //{
        //    var prices = Read();
        //    if (prices != null && prices.Any())
        //    {
        //        foreach (var p in prices)
        //        {
        //            if (p.CurPrice != null)
        //            {
        //                var stock = Core.Instance.Stocks.FirstOrDefault(x => x.Ticker == p.Ticker);
        //                if (stock == null)
        //                    continue;

        //                stock.CurPrice = p.CurPrice;
        //                stock.CurPriceUpdatedOn = p.CurPriceUpdatedOn;
        //            }
        //        }
        //    }

        //    LoadPortfolioPrices();
        //    LoadMoexPrices();
        //    LoadPricesFromMarketStack();

        //    var curPrices = Core.Instance.Stocks.Select(
        //            x => new { x.Ticker, x.CurPrice, x.Currency, CurPriceUpdatedOn = string.Format("{0:yyyy-MM-dd}", DateTime.Today.Date) }
        //        )
        //        .OrderBy(x => x.Ticker);

        //    var json = Newtonsoft.Json.JsonConvert.SerializeObject(curPrices);

        //    Write(json);
        //}

        /// <summary>Load prices from moex</summary>
        //public void LoadMoexPrices()
        //{
        //    //var baseUrl = "https://iss.moex.com/iss/engines/stock/markets/shares/securities/{0}.json?iss.meta=off&iss.only=marketdata";
        //    foreach (var s in Core.Instance.Stocks)
        //    {
        //        if (s.Company == null || s.Ticker == null)
        //            continue;

        //        if (s.CurPriceUpdatedOn > DateTime.Now.AddHours(-1))
        //            continue;

        //        //var url = string.Format(baseUrl, s.Ticker);
        //        var t = "https://iss.moex.com/iss/engines/stock/markets/shares/boards/TQBR/securities.json?iss.only=marketdata";

        //        var req = (HttpWebRequest)WebRequest.Create(t);
        //        var resp = (HttpWebResponse)req.GetResponse();

        //        using (var stream = new StreamReader(resp.GetResponseStream()))
        //        {
        //            var response = stream.ReadToEnd();
        //            var data = Newtonsoft.Json.JsonConvert.DeserializeObject(response);
        //            var marketData = ((Newtonsoft.Json.Linq.JObject)data)["marketdata"]["data"];

        //            foreach (var d in marketData)
        //            {
        //                var stock = Core.Instance.GetStock(d[0].ToString());
        //                if (stock != null)
        //                {
        //                    //if (stock.Ticker == "MTSS") {var yy = 0;}
        //                    if (decimal.TryParse(d[12].ToString(), out var p))
        //                    {
        //                        if (p != 0)
        //                        {
        //                            stock.CurPrice = p;
        //                            stock.CurPriceUpdatedOn = DateTime.Today.Date;
        //                        }
        //                    }
        //                }
        //                //    Accounts.Add( new Account{ Id = acc["id"].ToString(), Name = acc["name"].ToString(), 
        //                //        Currency = (Currency)Enum.Parse(typeof(Currency), acc["cur"].ToString(), true) });
        //            }
        //        }

        //        //req = (HttpWebRequest)HttpWebRequest.Create(url);
        //        //resp = (HttpWebResponse)req.GetResponse();

        //        //using (StreamReader stream = new StreamReader(resp.GetResponseStream()))
        //        //{
        //        //    var response = stream.ReadToEnd();
        //        //    var data = Newtonsoft.Json.JsonConvert.DeserializeObject(response);

        //        //    var marketData = ((Newtonsoft.Json.Linq.JObject)data)["marketdata"]["data"];

        //        //    foreach(var d in marketData)
        //        //    {
        //        //    //    Accounts.Add( new Account{ Id = acc["id"].ToString(), Name = acc["name"].ToString(), 
        //        //    //        Currency = (Currency)Enum.Parse(typeof(Currency), acc["cur"].ToString(), true) });
        //        //    }
        //        //}
        //    }
        //}
		

        //private void LoadPricesFromMarketStack()
        //{
        //    //var baseUrl = "http://api.marketstack.com/v1/eod/latest?access_key=3be0727910405eaec4bd9e4d0aa48b6b&symbols=AAPL";
        //    foreach (var s in Core.Instance.Stocks)
        //    {
        //        if (s.Company == null || s.Ticker == null)
        //            continue;

        //        if (s.CurPriceUpdatedOn > DateTime.Today.Date.AddDays(-4))
        //            continue;

        //        var url = $"http://api.marketstack.com/v1/eod/latest?access_key=3be0727910405eaec4bd9e4d0aa48b6b&symbols={s.Ticker}";
        //        HttpWebResponse resp;

        //        try
        //        {
        //            var req = (HttpWebRequest)WebRequest.Create(url);
        //            resp = (HttpWebResponse)req.GetResponse();
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine("LoadPricesFromMarketStack(): {0}", ex.Message);
        //            //Log.Error(ex);
        //            continue;
        //        }

        //        using (var stream = new StreamReader(resp.GetResponseStream()))
        //        {
        //            var response = stream.ReadToEnd();
        //            var data = Newtonsoft.Json.JsonConvert.DeserializeObject(response);
        //            var marketData = ((Newtonsoft.Json.Linq.JObject)data)["data"];

        //            try
        //            {
        //                if (marketData != null && marketData.Any() && marketData[0]["close"] != null && decimal.TryParse(marketData[0]["close"].ToString(), out var p))
        //                {
        //                    if (p != 0)
        //                    {
        //                        s.CurPrice = p;
        //                        s.CurPriceUpdatedOn = DateTime.Today.Date;
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                Debug.WriteLine("LoadPricesFromMarketStack(): {0}", ex.Message);
        //                continue;
        //            }
        //        }
        //    }
        //}

        /// <summary>Load prices from Inv portfolio page</summary>
        //private static void LoadPortfolioPrices()
        //{
        //    // указываем путь к файлу csv
        //    string[] dirCsvFiles = { @"C:\Users\...\Downloads", @"C:\Users\Alex\Downloads" };

        //    var file = GetFile(dirCsvFiles, "Инвестпортфель_Watchlist_*");

        //    using (var reader = new StreamReader(file.FullName))
        //    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        //    {
        //        //csv.Configuration.HasHeaderRecord = true;
        //        var records = csv.GetRecords<CsvData>(); //var records = csv.GetRecords<CsvData>();
        //        foreach (var r in records)
        //        {
        //            if (r == null)
        //                continue;

        //            if (!string.IsNullOrEmpty(r.Ticker))
        //            {
        //                r.Ticker = r.Ticker.Replace("_", "").Replace(".MM", "");
        //                var stock = Core.Instance.GetStock(r.Ticker);
        //                if (stock == null)
        //                {
        //                    r.Ticker = r.Ticker.Replace(".O", "");   //"WDC.O"
        //                    stock = Core.Instance.GetStock(r.Ticker);
        //                }
        //                if (stock == null)
        //                {
        //                    r.Ticker = r.Ticker.Replace(".K", "");   //"BABA.K"
        //                    stock = Core.Instance.GetStock(r.Ticker);
        //                }

        //                if (stock != null)
        //                {
        //                    stock.CurPrice = decimal.Parse(r.Price.Replace(".", ""),
        //                        NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, new CultureInfo("fr-FR"));
        //                }
        //            }
        //            //r.Время "16:12:41"
        //            //r.Тикер "RTKM_p.MM"
        //            //r.Цена "80,20"                    
        //        }
        //    }
        //}

        private List<Stock> Read()
        {
            var root = "wwwroot";
            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                root,
                _fileName
            );

            if (!File.Exists(path))
                return null;

            string jsonResult;

            using (var streamReader = new StreamReader(path))
            {
                jsonResult = streamReader.ReadToEnd();
            }

            var sett = new Newtonsoft.Json.JsonSerializerSettings { DateFormatString = "yyyy-MM-dd" };
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Stock>>(jsonResult, sett);

            return data;
        }

        private void Write(string json = "")
        {
            var root = "wwwroot";
            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                root,
                _fileName
            );

            using (var streamWriter = File.CreateText(path))
            {
                streamWriter.Write(json);
            }
        }

        private static FileInfo GetFile(string[] paths, string patern)
        {
            // xlsx file
            var dir = new DirectoryInfo(paths[0]);
            if (!dir.Exists)
                dir = new DirectoryInfo(paths[1]);

            var files = dir.GetFiles(patern);
            if (files.Length == 0)
                return null;

            var xslFile = files[0];
            foreach (var f in files)
            {
                if (f.LastWriteTime >= xslFile.LastWriteTime)
                    xslFile = f;
            }

            return xslFile;
        }

        private class CsvData
        {
            [Name("Тикер")]
            public string Ticker { get; set; }
            [Name("Цена")]
            public string Price { get; set; }
        }
    }
}