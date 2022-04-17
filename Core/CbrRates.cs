using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Xml;
using Invest.Core.Enums;

namespace Invest.Core
{
	public interface ICurrencyRate
	{
		Dictionary<DateTime, Dictionary<Currency, decimal>> Load();
	}

	/// <summary>
	/// Curensy rate from CBR
	/// </summary>
    public class CbrCurrencyRate : ICurrencyRate
    {
		private readonly Currency[] _currencyList;
		public DateTime StartDate, EndDate;

	    public CbrCurrencyRate(Currency[] currencyList)
	    {
		    _currencyList = currencyList;
		    EndDate = DateTime.Today;
	    }

	    public Dictionary<DateTime, Dictionary<Currency, decimal>> Load()
	    {
		    var rates = new Dictionary<DateTime, Dictionary<Currency, decimal>>();

		    foreach (var cur in _currencyList)
			    if (cur != Currency.Rur)
				    LoadCurrencyRates(cur, rates);

			return rates;
		}

	    private void LoadCurrencyRates(Currency cur, Dictionary<DateTime, Dictionary<Currency, decimal>> rates)
	    {
		    // https://cbr.ru/development/SXML/
	        // http://www.cbr.ru/scripts/XML_dynamic.asp?date_req1=02/03/2019&date_req2=14/03/2021&VAL_NM_RQ=R01235 usd
	        // http://www.cbr.ru/scripts/XML_dynamic.asp?date_req1=02/03/2019&date_req2=14/03/2021&VAL_NM_RQ=R01239 eur

	        var curCode = "1235";

	        if (cur == Currency.Usd)
				curCode = "1235";
			else if (cur == Currency.Eur)
				curCode = "1239";

            var url = $"http://www.cbr.ru/scripts/XML_dynamic.asp?date_req1={StartDate:dd/MM/yyyy}&date_req2={EndDate:dd/MM/yyyy}&VAL_NM_RQ=R0{curCode}";

            var req = (HttpWebRequest)WebRequest.Create(url);
            req.UseDefaultCredentials = true;
            req.UserAgent = "Chrome"; // error 403
            //req.Proxy.Credentials = CredentialCache.DefaultCredentials;
            var resp = (HttpWebResponse)req.GetResponse();

            using (var stream = new StreamReader(resp.GetResponseStream()))
            {
                var response = stream.ReadToEnd();
                var doc = new XmlDocument();
                doc.LoadXml(response);

                var nodes = doc.SelectNodes("//Record");
                if (nodes == null) 
                    return;

                foreach (XmlNode d in nodes)
                {
                    if (d.Attributes?["Date"] == null) 
                        continue;

                    var date = DateTime.Parse(d.Attributes?["Date"].Value);
                    var valueNode = d.ChildNodes[1];
                    if (valueNode != null)
                    {
                        var f = new CultureInfo("en-US", false).NumberFormat;
                        f.NumberDecimalSeparator = ",";
                        if (decimal.TryParse(valueNode.InnerText, NumberStyles.AllowDecimalPoint, f, out var v))
                        {
	                        Dictionary<Currency, decimal> rate;

							if (rates.ContainsKey(date))
								rate = rates[date];
							else 
							{
								rate = new Dictionary<Currency, decimal>();
								rates.Add(date, rate);
							}

							rate.Add(cur, v);
						}
                    }
                }
            }
	    }
    }
}