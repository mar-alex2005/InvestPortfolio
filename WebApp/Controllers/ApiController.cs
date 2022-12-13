using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Invest.Core.Enums;
using Newtonsoft.Json.Linq;

namespace Invest.WebApp.Controllers
{
	public class ApiController : Controller
	{
		private readonly Invest.Core.Builder _builder;

		public ApiController(Invest.Core.Builder builder)
		{
			_builder = builder;
		}

		public JsonResult Bonds()
		{
			var companies = _builder.Companies
				//.Where(x => x.Type == StockType.Bond)
				//&& (x.Ticker == "MTSS" || x.Ticker == "NVTK" || x.Ticker == "TDC" || x.Ticker == "AAL" || x.Ticker == "CLF" )
				//&& (currency == null || (int)x.Currency == currency)
				//&& (account == null || Core.Instance.Operations.Any(o => o.Stock == x && (int)o.AccountType == account))
				.ToList();

			var items = new List<object>();

			foreach (var c in companies)
			{
				var ops = _builder.Operations
					.Where(x => x.Stock != null && x.Stock.Type == StockType.Bond && x.Stock.Company == c
			            //&& s.Data.QtyBalance > 0
			            && (x.Type == OperationType.Buy || x.Type == OperationType.Sell || x.Type == OperationType.Coupon)
					)
					.ToList();

				if (!ops.Any())
					continue;

				var jArr = new JArray();

				var t = new JObject(
					new JProperty("company", c.Name),
					new JProperty("value", ops.Where(x => x.Type == OperationType.Buy).Sum(x => x.Summa) 
							            - ops.Where(x => x.Type == OperationType.Sell).Sum(x => x.Summa)),
					new JProperty("qty", ops.Where(x => x.Type == OperationType.Buy).Sum(x => x.Qty) 
				                       - ops.Where(x => x.Type == OperationType.Sell).Sum(x => x.Qty)),
					new JProperty("coupon", ops.Where(x => x.Type == OperationType.Coupon).Sum(x => x.Summa))
					
					//new JProperty("mskTime", e.MskTime.ToString("yyyy-MM-ddTHH:mm:ss.fff")),
					//new JProperty("ott", new JArray()), // not used
					//new JProperty("serverTime", srvTime.ToString("yyyy-MM-ddTHH:mm:ss.fff"))
				);
				
				items.Add(t);
			}

			return new JsonResult(items);
		}

		public JsonResult Divs(int? accountCode, Currency? cur, string tickerId, int? year, int? month)
		{
			var ops = _builder.Operations
				.Where(x => x.Type == OperationType.Dividend
					&& (accountCode == null || x.Account.BitCode == accountCode)
					&& (cur == null || x.Stock.Currency == cur)
					&& (tickerId == null || x.Stock.Ticker == tickerId)					
					&& (year == null || x.Date.Year == year)					
					&& (month == null || x.Date.Month == month)
				)
				.ToList();

			var items = new List<object>();

			foreach (var o in ops)
			{
				var jArr = new JArray();

				var t = new JObject(
					new JProperty("date", o.Date),
					new JProperty("summa", ops.Where(x => x.Type == OperationType.Buy).Sum(x => x.Summa) 
					                       - ops.Where(x => x.Type == OperationType.Sell).Sum(x => x.Summa)),
					new JProperty("qty", ops.Where(x => x.Type == OperationType.Buy).Sum(x => x.Qty) 
					                     - ops.Where(x => x.Type == OperationType.Sell).Sum(x => x.Qty)),
					new JProperty("coupon", ops.Where(x => x.Type == OperationType.Coupon).Sum(x => x.Summa))
					
					//new JProperty("mskTime", e.MskTime.ToString("yyyy-MM-ddTHH:mm:ss.fff")),
					//new JProperty("ott", new JArray()), // not used
					//new JProperty("serverTime", srvTime.ToString("yyyy-MM-ddTHH:mm:ss.fff"))
				);
				
				items.Add(t);
			}

			return new JsonResult(items);
		}
	}
}