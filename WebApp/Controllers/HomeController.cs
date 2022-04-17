using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Invest.Core.Entities;
using Invest.Core.Enums;
using Invest.WebApp.Models;
using Microsoft.AspNetCore.Html;

namespace Invest.WebApp.Controllers
{
    public class HomeController : Controller
    {
		private readonly Core.Builder _builder;

		public HomeController(Core.Builder builder)
		{
			_builder = builder;
		}

        public IActionResult Index()
        {
            var operations = _builder.Operations
                .Where(x => x.Type == OperationType.Buy || x.Type == OperationType.Sell || x.Type == OperationType.Dividend || x.Type == OperationType.Coupon)
                .OrderByDescending(x => x.Date).ThenByDescending(x => x.Index)
                .ToList();

            var model = new OperationViewModel {
                Ticker = null,
				Stocks = _builder.Stocks,
                Operations = operations
            };

            return View(model);
        }

		public IActionResult TickerIndex()
		{
			var tickerId = Request.Query.ContainsKey("tickerId") ? Request.Query["tickerId"].ToString() : null;
			var stock = _builder.GetStock(tickerId);

			var operations = _builder.Operations
				.Where(
					x => (x.Type == OperationType.Buy || x.Type == OperationType.Sell || x.Type == OperationType.Coupon)
						&& (tickerId == null || x.Stock.Ticker == tickerId)
				)
				.OrderByDescending(x => x.Date).ThenByDescending(x => x.TransId).ToList(); // Index

			var model = new OperationViewModel
			{
				Ticker = tickerId,
				Stock = stock,
				Stocks = _builder.Stocks,
				Accounts = _builder.Accounts,
				Operations = operations,
				//FifoResults = _builder.FifoResults.Where(x => x.Key.Ticker == stock.Ticker).ToList()
			};

			return View(model);
		}


		public IActionResult TickerDetails()
		{
			var tickerId = Request.Query.ContainsKey("tickerId")
			 ? Request.Query["tickerId"].ToString()
			 : null;

			var stock = _builder.GetStock(tickerId);

			var operations = _builder.Operations
				.Where(
					x => (x.Type == OperationType.Buy || x.Type == OperationType.Sell)
						//&& (tickerId == null || x.Stock.Ticker == tickerId)
				)
				.OrderByDescending(x => x.Date).ThenByDescending(x => x.TransId) // Index
				.ToList();

			var model = new OperationViewModel
			{
				Ticker = tickerId,
				Stock = stock,
				Accounts = _builder.Accounts,
				Operations = operations,
				FifoResults = _builder.FifoResults.Where(x => x.Key.Ticker == stock.Ticker)
			};

			return View(model);
		}

		//public IActionResult Stocks(string country, string orderBy = "company")
		//{
		//	var list = new List<StockItem>();
		//	foreach (var s in Invest.WebApp.Core.Instance.Stocks.Where(x => x.Data.BuyQty > 0))
		//	{
		//		var item = new StockItem
		//		{
		//			Stock = s,
		//			BuyQty = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s && x.Type == OperationType.Buy).Sum(x => x.Qty),
		//			SellQty = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s && x.Type == OperationType.Sell).Sum(x => x.Qty)
		//		};

		//		//var qL = string.Format("{0}", q);
		//		//if (q >= 1000 && s.LotSize > 1)
		//		//	qL = string.Format("{0:N0}K", q / 1000);
		//		//else if (q == 0)				
		//		//	qL = null;				

		//		Currency cur = Currency.Rur;

		//		var opp = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s && x.Type == OperationType.Buy).ToList();
		//		if (opp.Any())
		//		{
		//			item.FirstBuy = opp.Min(x => x.Date);
		//			item.LastBuy = opp.Max(x => x.Date);
		//			cur = opp[0].Currency;
		//		}

		//		opp = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s && x.Type == OperationType.Sell).ToList();
		//		if (opp.Any())
		//		{
		//			item.FirstSell = opp.Max(x => x.Date);
		//			item.LastSell = opp.Max(x => x.Date);
		//		}

		//		item.BuySum = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s && x.Type == OperationType.Buy).Sum(x => x.Summa);
		//		item.SellSum = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s && x.Type == OperationType.Sell)?.Sum(x => x.Summa);
		//		item.TotalSum = item.BuySum + (item.SellSum ?? 0);
		//		item.TotalSumInRur = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s
		//			&& (x.Type == OperationType.Sell || x.Type == OperationType.Buy)).Sum(x => x.Summa);

		//		if (cur == Currency.Usd)
		//		{
		//			item.TotalSumInRur = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s
		//				&& (x.Type == OperationType.Sell || x.Type == OperationType.Buy)).Sum(x => x.RurSumma);
		//		}
		//		else
		//		{
		//			item.TotalSumInRur = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s
		//				&& (x.Type == OperationType.Sell || x.Type == OperationType.Buy)).Sum(x => x.Summa);
		//		}

		//		//notloss
		//		var notClosedOps = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s && !x.IsClosed).OrderByDescending(x => x.Date);
		//		if (notClosedOps.Count() == 0)
		//		{
		//			var lastOp = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s && x.PositionNum != null).OrderByDescending(x => x.Date).FirstOrDefault();
		//			if (lastOp != null)
		//			{
		//				//var posNum = lastOp.PositionNum;
		//				notClosedOps = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s && x.Date >= lastOp.Date).OrderByDescending(x => x.Date);
		//			}
		//		}

		//		var notClosedBuyOps = notClosedOps.Where(x => x.Type == OperationType.Buy).ToList();
		//		var notClosedSellOps = notClosedOps.Where(x => x.Type == OperationType.Sell).ToList();

		//		var notClosedBuySum = notClosedBuyOps.Sum(x => x.Price * x.Qty);
		//		var notClosedSellSum = notClosedSellOps.Sum(x => x.Price * x.Qty);
		//		var notClosedSaldo = notClosedSellSum - notClosedBuySum;

		//		var qty = notClosedBuyOps.Sum(x => x.Qty) - notClosedSellOps.Sum(x => x.Qty);
		//		if (qty != 0)
		//			item.NotLossPrice = Math.Abs(notClosedSaldo.Value) / qty;

		//		//min, avg (at  months)
		//		var ops = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s && x.Type == OperationType.Buy && x.Date >= DateTime.Today.AddMonths(-6));
		//		if (ops.Count() == 0 || ops.Count() < 2)
		//			ops = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s && x.Type == OperationType.Buy).OrderByDescending(x => x.Date).Take(6);

		//		item.LastMinBuyPrice = ops.Min(x => x.Price);

		//		qty = ops.Sum(x => x.Qty);
		//		if (qty.HasValue && qty != 0)
		//			item.LastAvgBuyPrice = Math.Abs(ops.Sum(x => x.Price.Value * x.Qty.Value)) / qty.Value;


		//		//foreach (var a in Core.Instance.Accounts)
		//		//{
		//		//	notClosedOps = Core.Instance.Operations.Where(x => !x.IsClosed && x.AccountType == a.Type).OrderByDescending(x => x.Date);

		//		//	notClosedBuyOps = notClosedOps.Where(x => x.Type == OperationType.Buy).ToList();
		//		//	notClosedSellOps = notClosedOps.Where(x => x.Type == OperationType.Sell).ToList();

		//		//	notClosedBuySum = notClosedBuyOps.Sum(x => x.Price * x.Qty);
		//		//	notClosedSellSum = notClosedSellOps.Sum(x => x.Price * x.Qty);
		//		//	notClosedSaldo = notClosedSellSum - notClosedBuySum;

		//		//	item.NotLossPrice = notClosedSaldo / s.Data.QtyBalance;
		//		//}

		//		if (string.IsNullOrEmpty(country) || (country == "Rur" && cur == Currency.Rur) || (country == "Usd" && cur == Currency.Usd))
		//			list.Add(item);
		//	}

		//	var model = new StockViewModel
		//	{
		//		Country = country
		//	};

		//	if (!string.IsNullOrEmpty(orderBy))
		//	{
		//		if (orderBy.ToLower() == "company")
		//			model.StockItems = list.OrderBy(x => x.Stock.Company.Name);
		//		else if (orderBy.ToLower() == "ticker")
		//			model.StockItems = list.OrderBy(x => x.Stock.Ticker);
		//		else if (orderBy.ToLower() == "qty")
		//			model.StockItems = list.OrderBy(x => x.Stock.Data.QtyBalance);
		//		else
		//			model.StockItems = list.OrderBy(x => x.Stock.Ticker);
		//	}
		//	else
		//		model.StockItems = list.OrderBy(x => x.Stock.Ticker);

		//	return View(model);
		//}

		//public IActionResult Profit(Portfolio? portfolio, bool isJson = false, string column = "ticker", string orderBy = "asc")
		//{
		//	var model = new ProfitViewModel
		//	{
		//		Portfolio = portfolio,
		//		Tickers = new List<string>()
		//	};

		//	var list = new List<ProfitViewModel.StockItem>();
		//	var stocks = Invest.WebApp.Core.Instance.Stocks
		//		.Where(x => portfolio == null
		//				|| (portfolio == Entities.Portfolio.IIs && Invest.WebApp.Core.Instance.Operations.Any(o => o.Stock == x && o.AccountType == AccountType.Iis))
		//				|| (portfolio == Entities.Portfolio.Vbr && Invest.WebApp.Core.Instance.Operations.Any(o => o.Stock == x && o.AccountType == AccountType.VBr))
		//				|| (portfolio == Entities.Portfolio.Usd && x.Currency == Currency.Usd)
		//				|| (portfolio == Entities.Portfolio.Rur && x.Currency == Currency.Rur)
		//				|| (portfolio == Entities.Portfolio.BlueUs && x.Currency == Currency.Usd
		//					&& (Invest.WebApp.Core.Instance.BlueUsList.Contains(x.Ticker))
		//				|| (portfolio == Entities.Portfolio.BlueRu && x.Currency == Currency.Rur
		//					&& (Invest.WebApp.Core.Instance.BlueRuList.Contains(x.Ticker))))
		//				|| (portfolio == Entities.Portfolio.BlueEur && x.Currency == Currency.Eur
		//					&& (Invest.WebApp.Core.Instance.BlueEurList.Contains(x.Ticker))))
		//		.ToList();

		//	foreach (var s in stocks)
		//	{
		//		var item = new ProfitViewModel.StockItem
		//		{
		//			Stock = s,
		//			BuyQty = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s && x.Type == OperationType.Buy).Sum(x => x.Qty),
		//			SellQty = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s && x.Type == OperationType.Sell).Sum(x => x.Qty)
		//		};

		//		var cur = Currency.Rur;

		//		var opp = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s && x.Type == OperationType.Buy).ToList();
		//		if (opp.Any())
		//		{
		//			item.FirstBuy = opp.Min(x => x.Date);
		//			item.LastBuy = opp.Max(x => x.Date);
		//			cur = opp[0].Currency;
		//		}

		//		opp = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s && x.Type == OperationType.Sell).ToList();
		//		if (opp.Any())
		//		{
		//			item.FirstSell = opp.Max(x => x.Date);
		//			item.LastSell = opp.Max(x => x.Date);
		//		}

		//		item.BuySum = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s && x.Type == OperationType.Buy).Sum(x => x.Summa);
		//		item.SellSum = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s && x.Type == OperationType.Sell)?.Sum(x => x.Summa);
		//		item.TotalSum = item.BuySum + (item.SellSum ?? 0);
		//		item.TotalSumInRur = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s
		//			&& (x.Type == OperationType.Sell || x.Type == OperationType.Buy))?.Sum(x => x.Summa);

		//		item.Commission = Invest.WebApp.Core.Instance.FinIndicators.Where(x => x.Key.Ticker == s.Ticker).Sum(x => x.Value.Commission);
		//		item.FifoUsd = Invest.WebApp.Core.Instance.FifoResults.Where(x => x.Key.Ticker == s.Ticker && x.Key.Cur == Currency.Usd).Sum(x => x.Value.Summa);
		//		item.FifoRur = Invest.WebApp.Core.Instance.FifoResults.Where(x => x.Key.Ticker == s.Ticker && x.Key.Cur == Currency.Rur).Sum(x => x.Value.Summa);
		//		item.FifoInRur = Invest.WebApp.Core.Instance.FifoResults.Where(x => x.Key.Ticker == s.Ticker).Sum(x => x.Value.RurSumma);
		//		item.FifoRurComm = Invest.WebApp.Core.Instance.FifoResults.Where(x => x.Key.Ticker == s.Ticker).Sum(x => x.Value.RurCommission);
		//		item.FifoBaseRur = item.FifoInRur - item.FifoRurComm;

		//		var divs = Invest.WebApp.Core.Instance.FinIndicators.Where(x => x.Value.DivSumma != null);
		//		item.DivUsd = divs.Where(x => x.Key.Ticker == item.Stock.Ticker && x.Key.Cur == Currency.Usd).Sum(x => x.Value.DivSumma);
		//		item.DivRur = divs.Where(x => x.Key.Ticker == item.Stock.Ticker && x.Key.Cur == Currency.Rur).Sum(x => x.Value.DivSumma);

		//		if (cur == Currency.Usd)
		//		{
		//			item.TotalSumInRur = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s
		//				&& (x.Type == OperationType.Sell || x.Type == OperationType.Buy))?.Sum(x => x.RurSumma);
		//		}
		//		else
		//		{
		//			item.TotalSumInRur = Invest.WebApp.Core.Instance.Operations.Where(x => x.Stock == s
		//				&& (x.Type == OperationType.Sell || x.Type == OperationType.Buy))?.Sum(x => x.Summa);
		//		}

		//		list.Add(item);
		//		model.Tickers.Add(item.Stock.Ticker);
		//	}

		//	if (!string.IsNullOrEmpty(column))
		//	{
		//		if (column.ToLower() == "ticker")
		//			model.StockItems = orderBy == "asc" ? list.OrderBy(x => x.Stock.Ticker) : list.OrderByDescending(x => x.Stock.Ticker);
		//		else if (column.ToLower() == "qty")
		//			model.StockItems = orderBy == "asc" ? list.OrderBy(x => x.Stock.Data.QtyBalance) : list.OrderByDescending(x => x.Stock.Data.QtyBalance);
		//		else if (column.ToLower() == "FifoUsd".ToLower())
		//			model.StockItems = orderBy == "asc" ? list.OrderBy(x => x.FifoUsd) : list.OrderByDescending(x => x.FifoUsd);
		//		else if (column.ToLower() == "FifoRur".ToLower())
		//			model.StockItems = orderBy == "asc" ? list.OrderBy(x => x.FifoRur) : list.OrderByDescending(x => x.FifoRur);
		//		else if (column.ToLower() == "FifoInRur".ToLower())
		//			model.StockItems = (orderBy == "asc") ? list.OrderBy(x => x.FifoInRur) : list.OrderByDescending(x => x.FifoInRur);
		//		else if (column.ToLower() == "DivUsd".ToLower())
		//			model.StockItems = (orderBy == "asc") ? list.OrderBy(x => x.DivUsd) : list.OrderByDescending(x => x.DivUsd);
		//		else if (column.ToLower() == "DivRur".ToLower())
		//			model.StockItems = (orderBy == "asc") ? list.OrderBy(x => x.DivRur) : list.OrderByDescending(x => x.DivRur);
		//		else
		//			model.StockItems = (orderBy == "asc") ? list.OrderBy(x => x.Stock.Ticker) : list.OrderByDescending(x => x.Stock.Ticker);
		//	}
		//	else
		//		model.StockItems = list.OrderBy(x => x.Stock.Ticker);

		//	return View(model);
		//}



		//       public IActionResult Portfolio(Portfolio? portfolio, bool isJson = false, string column = "ticker", string orderBy = "asc")
		//       {            
		//		ViewBag.Portfolio = portfolio != null ? (Portfolio?)Enum.ToObject(typeof(Portfolio), portfolio) : null;

		//		var	stocks = Invest.WebApp.Core.Instance.Stocks
		//			.Where(x => x.Data.QtyBalance > 0
		//				//&& (x.Ticker == "MTSS" || x.Ticker == "NVTK" || x.Ticker == "TDC" || x.Ticker == "AAL" || x.Ticker == "CLF" )
		//				//&& (currency == null || (int)x.Currency == currency)
		//				//&& (account == null || Core.Instance.Operations.Any(o => o.Stock == x && (int)o.AccountType == account))
		//				&& (portfolio == null 
		//					|| (portfolio == Entities.Portfolio.IIs && Invest.WebApp.Core.Instance.Operations.Any(o => o.Stock == x && o.AccountType == AccountType.Iis))
		//					|| (portfolio == Entities.Portfolio.Vbr && Invest.WebApp.Core.Instance.Operations.Any(o => o.Stock == x && o.AccountType == AccountType.VBr))
		//					|| (portfolio == Entities.Portfolio.Usd && x.Currency == Currency.Usd)
		//					|| (portfolio == Entities.Portfolio.Rur && x.Currency == Currency.Rur)
		//					|| (portfolio == Entities.Portfolio.BlueUs && x.Currency == Currency.Usd 
		//						&& (Invest.WebApp.Core.Instance.BlueUsList.Contains(x.Ticker))))
		//					|| (portfolio == Entities.Portfolio.BlueRu && x.Currency == Currency.Rur 
		//						&& Invest.WebApp.Core.Instance.BlueRuList.Contains(x.Ticker))
		//		            || (portfolio == Entities.Portfolio.BlueEur && x.Currency == Currency.Eur 
		//		                && Invest.WebApp.Core.Instance.BlueEurList.Contains(x.Ticker)))
		//			.ToList();

		//		var model = new PortfolioViewModel {
		//               TotalRurSum = 1530000,
		//               Portfolio = portfolio,
		//               Stocks = stocks,
		//               Accounts = new List<AccountType?> { AccountType.Iis, AccountType.VBr, null }
		//           };

		//		decimal buyStockSumUsd = 0, buyStockSumRur = 0, buyStockSumInRur = 0;

		//		var list = new List<PortfolioViewModel.Item>();
		//           foreach (var s in stocks)
		//           {
		//			var ops = Invest.WebApp.Core.Instance.Operations
		//				.Where(x => x.Stock == s 
		//					&& s.Data.QtyBalance > 0
		//					&& s.Data.PosPrice != null
		//					&& (x.Type == OperationType.Buy || x.Type == OperationType.Sell)
		//                       && (portfolio == null 
		//						|| (portfolio == Entities.Portfolio.IIs && x.AccountType == AccountType.Iis)
		//						|| (portfolio == Entities.Portfolio.Vbr && x.AccountType == AccountType.VBr)
		//						|| (portfolio == Entities.Portfolio.Usd || portfolio == Entities.Portfolio.Rur 
		//							|| portfolio == Entities.Portfolio.BlueRu || portfolio == Entities.Portfolio.BlueUs
		//						)
		//					)
		//                   )
		//                   .ToList();

		//			if (!ops.Any())
		//				continue;

		//			var item = new PortfolioViewModel.Item { Stock = s };

		//			decimal curStockSum = 0, posPriceStockSum = 0, comission = 0, buyTotalSum = 0, posPriceStockSumInRur;

		//			// calc sum not salled stocks	
		//			var opBuys = ops.Where(x => x.Stock == s && x.Type == OperationType.Buy && x.QtySaldo > 0);
		//			foreach (var b in opBuys) {
		//				buyTotalSum += (b.QtySaldo * b.Price.Value);					
		//				curStockSum += (b.QtySaldo * (s.CurPrice ?? 0));

		//				if (s.AccountData[b.AccountType].PosPrice != null)
		//					posPriceStockSum += b.QtySaldo * s.AccountData[b.AccountType].PosPrice.Value;
		//				comission += b.QtySaldo * b.Commission.Value;					
		//			}

		//			if (s.Currency == Currency.Usd || s.Currency == Currency.Eur) {
		//                   posPriceStockSumInRur = posPriceStockSum * Invest.WebApp.Core.Instance.GetCurRate(s.Currency);
		//				buyStockSumInRur += posPriceStockSumInRur;
		//               }
		//			else {
		//                   posPriceStockSumInRur = posPriceStockSum;
		//				buyStockSumInRur += posPriceStockSumInRur;
		//               }

		//               item.BuyQty = ops.Where(x => x.Stock == s && x.Type == OperationType.Buy).Sum(x => x.Qty) ?? 0;
		//               item.SellQty = ops.Where(x => x.Stock == s && x.Type == OperationType.Sell).Sum(x => x.Qty) ?? 0;
		//               item.BuyTotalSum += buyTotalSum;
		//               //item.Commission += s.GetCommission(a) ?? 0;

		//			item.Qty = item.BuyQty - item.SellQty;
		//               item.CloseFinResult = s.Data.FinResultForClosedPositions ?? 0;
		//			item.FinResult = s.Data.FinResult ?? 0;
		//               item.TotalFinResult = item.CloseFinResult + item.FinResult;

		//               item.StockSum = posPriceStockSum; //s.AccountData[a].StockSum ?? 0;
		//			item.Profit = curStockSum - posPriceStockSum;

		//               item.CurStockSum = curStockSum; //s.AccountData[a].CurrentSum ?? 0;
		//			item.FirstBuy = s.FirstBuy();
		//			item.LastBuy = s.LastBuy();
		//			item.LastSell = s.LastSell();
		//			item.MinBuyPrice = s.MinBuyPrice();
		//			item.MaxSellPrice = s.MaxSellPrice();

		//               if (s.Currency == Currency.Usd || s.Currency == Currency.Eur)
		//               {
		//                   item.ProfitInRur = item.Profit * Invest.WebApp.Core.Instance.GetCurRate(s.Currency, DateTime.Today);
		//                   //model.TotalCloseFinResultUsd += s.AccountData[a].FinResultForClosedPositions ?? 0;
		//                   //model.TotalFinResultUsd += s.AccountData[a].FinResult ?? 0;
		//                   model.TotalProfitInRur += item.Profit * Invest.WebApp.Core.Instance.GetCurRate(s.Currency, DateTime.Today);

		//				item.ProfitUsd = item.Profit;
		//				model.TotalProfitUsd += item.Profit;	
		//               }
		//               else if (s.Currency == Currency.Rur) {
		//                   item.ProfitInRur = item.Profit;
		//                   //model.TotalCloseFinResultRur += s.AccountData[a].FinResultForClosedPositions ?? 0;;
		//                   //model.TotalFinResultRur += s.AccountData[a].FinResult ?? 0;
		//                   model.TotalProfitInRur += item.Profit;

		//				item.ProfitRur = item.Profit;
		//                   model.TotalProfitRur += item.Profit;
		//               }

		//               //% in portfoliio of Total summa
		//               item.PortfPercent = 100 * posPriceStockSumInRur / model.TotalRurSum;

		//               item.BuySum = ops.Where(x => x.Type == OperationType.Buy).Sum(x => x.Summa);
		//			item.SellSum = ops.Where(x => x.Type == OperationType.Sell)?.Sum(x => x.Summa);
		//			item.TotalSum = item.BuySum + (item.SellSum ?? 0);
		//			item.TotalSumInRur = ops.Where(x => x.Type == OperationType.Sell || x.Type == OperationType.Buy)?.Sum(x => x.Summa);

		//			//var divs = Core.Instance.FinIndicators.Where(x => x.Value.DivSumma != null);
		//			//item.DivUsd = divs.Where(x => x.Key.Ticker == item.Stock.Ticker && x.Key.Cur == Currency.Usd).Sum(x => x.Value.DivSumma);
		//			//item.DivRur = divs.Where(x => x.Key.Ticker == item.Stock.Ticker && x.Key.Cur == Currency.Rur).Sum(x => x.Value.DivSumma);

		//               if (s.Currency == Currency.Usd) {
		//				model.TotalStockSum[s.Currency] += posPriceStockSum; //buyTotalSum;
		//				model.TotalCurStockSum[s.Currency] += item.CurStockSum;
		//                   buyStockSumUsd += item.StockSum;
		//               }
		//               else if (s.Currency == Currency.Rur) {
		//				model.TotalStockSum[s.Currency] += posPriceStockSum; //buyTotalSum;
		//				model.TotalCurStockSum[s.Currency] += item.CurStockSum;
		//                   buyStockSumRur += item.StockSum;
		//               }				

		//               if (item.Stock != null && item.Stock.Currency == Currency.Usd)
		//                   item.ProfitPercent += item.ProfitUsd / item.StockSum * 100;
		//               else if (item.StockSum != 0 && item.Stock.Currency == Currency.Rur)
		//                   item.ProfitPercent += item.ProfitRur / item.StockSum * 100;				

		//               if (item.Stock != null)
		//                   list.Add(item);
		//           }

		//		if (buyStockSumUsd != 0)
		//			model.TotalProfitPercentUsd = model.TotalProfitUsd / buyStockSumUsd * 100;
		//		if (buyStockSumRur != 0)
		//			model.TotalProfitPercentRur = model.TotalProfitRur / buyStockSumRur * 100;

		//		if (buyStockSumInRur != 0)
		//			model.TotalProfitPercent = model.TotalProfitInRur / buyStockSumInRur * 100;

		//           if (!string.IsNullOrEmpty(column))
		//           {
		//               if (column.ToLower() == "ticker")
		//                   model.Items = (orderBy == "asc") ? list.OrderBy(x => x.Stock.Ticker) : list.OrderByDescending(x => x.Stock.Ticker);
		//               else if (column.ToLower() == "qty")
		//                   model.Items = (orderBy == "asc") ? list.OrderBy(x => x.Stock.Data.QtyBalance) : list.OrderByDescending(x => x.Stock.Data.QtyBalance);
		//			else if (column.ToLower() == "account")
		//                   model.Items = (orderBy == "asc") ? list.OrderBy(x => x.Stock.Data.QtyBalance) : list.OrderByDescending(x => x.Stock.Data.QtyBalance);

		//			else if (column.ToLower() == "DivUsd".ToLower())
		//                   model.Items = (orderBy == "asc") ? list.OrderBy(x => x.DivUsd): list.OrderByDescending(x => x.DivUsd);
		//			else if (column.ToLower() == "DivRur".ToLower())
		//                   model.Items = (orderBy == "asc") ? list.OrderBy(x => x.DivRur): list.OrderByDescending(x => x.DivRur);
		//			else if (column.ToLower() == "StockSum".ToLower())
		//                   model.Items = (orderBy == "asc") ? list.OrderBy(x => x.DivRur): list.OrderByDescending(x => x.DivRur);
		//			else if (column.ToLower() == "profitUsd".ToLower())
		//                   model.Items = (orderBy == "asc") ? list.OrderBy(x => x.ProfitUsd): list.OrderByDescending(x => x.ProfitUsd);
		//			else if (column.ToLower() == "profitRur".ToLower())
		//                   model.Items = (orderBy == "asc") ? list.OrderBy(x => x.ProfitRur): list.OrderByDescending(x => x.ProfitRur);
		//			else if (column.ToLower() == "profitInRur".ToLower())
		//                   model.Items = (orderBy == "asc") ? list.OrderBy(x => x.ProfitInRur): list.OrderByDescending(x => x.ProfitInRur);
		//			//else if (column.ToLower() == "profitPercent".ToLower())
		//               //    model.Items = (orderBy == "asc") ? list.OrderBy(x => x.ProfitPercent): list.OrderByDescending(x => x.ProfitPercent);
		//               else if (column.ToLower() == "profitPercent".ToLower())
		//                   model.Items = orderBy == "asc" 
		//                       ? list.OrderBy(l => GetPropertyValue(l, column))
		//                       : list.OrderByDescending(l => GetPropertyValue(l, column));
		//               else
		//                   model.Items = (orderBy == "asc") ? list.OrderBy(x => x.Stock.Ticker): list.OrderByDescending(x => x.Stock.Ticker);
		//           }
		//           else
		//               model.Items = list.OrderBy(x => x.Stock.Ticker);

		//		if (isJson)
		//			 return new JsonResult(model.Items.OrderByDescending(x => x.ProfitInRur));

		//           return View(model);
		//       }



		//       public IActionResult Cache()
		//       {
		//        var model = new CacheViewModel();

		//        var allUsdOps = Invest.WebApp.Core.Instance.Operations
		//	        .Where(x => x.Type == OperationType.UsdRubBuy || x.Type == OperationType.UsdRubSell 
		//				|| x.Type == OperationType.EurRubBuy || x.Type == OperationType.EurRubSell).ToList();

		//        //
		//		foreach (var a in Invest.WebApp.Core.Instance.Accounts)
		//		{
		//			model.CurBuyOps.Add(new CacheViewModel.Item(Currency.Usd, a.Type),
		//				allUsdOps.Where(x => x.Type == OperationType.UsdRubBuy && x.AccountType == a.Type));
		//			model.CurSellOps.Add(new CacheViewModel.Item(Currency.Usd, a.Type),
		//				allUsdOps.Where(x => x.Type == OperationType.UsdRubSell && x.AccountType == a.Type));

		//			model.CurBuyOps.Add(new CacheViewModel.Item(Currency.Eur, a.Type),
		//				allUsdOps.Where(x => x.Type == OperationType.EurRubBuy && x.AccountType == a.Type));
		//			model.CurSellOps.Add(new CacheViewModel.Item(Currency.Eur, a.Type),
		//				allUsdOps.Where(x => x.Type == OperationType.EurRubSell && x.AccountType == a.Type));

		//			foreach (Currency curId in Enum.GetValues(typeof(Currency)))
		//			{
		//				if (curId == Currency.Rur)
		//					continue;

		//				model.BuysOps.Add(new CacheViewModel.Item(curId, a.Type),
		//			        Invest.WebApp.Core.Instance.Operations.Where(x => x.Type == OperationType.Buy
		//			                                                          && x.AccountType == a.Type && x.Stock != null && x.Stock.Currency == curId));

		//		        model.SellOps.Add(new CacheViewModel.Item(curId, a.Type),
		//			        Invest.WebApp.Core.Instance.Operations.Where(x => x.Type == OperationType.Sell
		//			                                                          && x.AccountType == a.Type && x.Stock != null && x.Stock.Currency == curId));

		//		        model.Divs.Add(new CacheViewModel.Item(curId, a.Type),
		//			        Invest.WebApp.Core.Instance.Operations.Where(x => x.Type == OperationType.Dividend
		//			                                                          && x.AccountType == a.Type && x.Stock != null && x.Currency == curId));

		//		        //if (a.Type == AccountType.Iis && curId == Currency.Usd)
		//		        //{
		//			       // var tt = Core.Instance.Operations.Where(x => x.Type == OperationType.Dividend
		//			       //     && x.AccountType == a.Type && x.Stock != null && x.Currency == curId).ToList();
		//		        //}
		//			}
		//		}

		//        return View(model);
		//       }


		//       public IActionResult AmChart()
		//       {
		//           return View();
		//       }

		//       public JsonResult CacheInData(AccountType? accountType)
		//       {
		//           var ops = Invest.WebApp.Core.Instance.Operations
		//               .Where(x => x.Type == OperationType.BrokerCacheIn 
		//					&& (accountType == null || x.AccountType == AccountType.Iis)
		//					&& x.Currency == Currency.Rur)
		//               .OrderByDescending(x => x.Date)
		//               .GroupBy(x => x.Date.ToString("MMM, yy"))
		//               //.Join(periods, g => g.Key, p => p, (g, p) => new { M = p, S = g.Sum(x1 => x1.Summa) });
		//               .Select( g => new { M = g.Key, S = g.Sum(x1 => x1.Summa)});				

		//           var data = new List<Invest.WebApp.Core.CacheView>();
		//           var d = DateTime.Now;
		//           while(d >= new DateTime(2019,12,1))
		//           { 
		//               var op = ops.FirstOrDefault(x => x.M == d.ToString("MMM, yy"));
		//               data.Add(new Invest.WebApp.Core.CacheView{ Period = d.ToString("MMM, yy"), Summa = op?.S });
		//               d = d.AddMonths(-1);  
		//           }            

		//           return new JsonResult(data);
		//       }

		//       public JsonResult DivsTotalData(int? curId = 1)
		//       {
		//           var list = Invest.WebApp.Core.Instance.Operations
		//               .Where(x => x.Type == OperationType.Dividend 
		//                           && (curId == null || (int)x.Currency == curId.Value)
		//						&& x.Summa != null)
		//               .GroupBy(x => x.Stock)
		//               .Select(g => new { Stock = g.Key, Sum = g.Sum(x1 => x1.Summa) })
		//            .ToList();

		//           return new JsonResult(list);
		//       }

		//       public JsonResult StockChartData()
		//       {
		//           var ss = Invest.WebApp.Core.Instance.Stocks.Where(x => x.Data.QtyBalance > 0).OrderBy(x => x.SortIndex);

		//           return new JsonResult(ss);
		//       }

		//       public JsonResult LoadPrices()
		//       {
		//           var priceMgr = new PriceManager();
		//           priceMgr.LoadMoexPrices();
		//           return new JsonResult(null);
		//       } 

		//	public IActionResult Divs(int? cur = null, int? year = null)
		//       {
		//           var model = new DivsViewModel {
		//               Cur = (Currency?)cur,
		//               Year = year
		//           };

		//           return View(model);
		//       }

		//	public IActionResult Hist(bool isJson = false)
		//       {
		//		var model = new HistViewModel(){};
		//		model.Items = new List<HistViewModel.Item>();

		//		var opers = Invest.WebApp.Core.Instance.Operations
		//			.Where(x => x.Type == OperationType.Buy || x.Type == OperationType.Sell
		//				//&& (x.Stock.Ticker == "SBER" || x.Stock.Ticker == "NLMK" || x.Stock.Ticker == "YNDX")
		//				&& x.AccountType == AccountType.Iis
		//			)					
		//			.OrderBy(x => x.Date)
		//		;			

		//		DateTime d = new DateTime(2019, 12, 24);
		//		DateTime dEnd = new DateTime(2020, 1, 31);
		//		HistViewModel.Item prevItem = null;
		//		int qty = 0;

		//		while (d <= dEnd) 
		//		{
		//			d = d.AddDays(1);
		//			var item = new HistViewModel.Item {
		//				Date = d, 
		//				StockSum = prevItem != null ? prevItem.StockSum : 0m,
		//				CurStockSum = prevItem != null ? prevItem.CurStockSum : 0m,
		//				Profit = prevItem != null ? prevItem.Profit : 0m,
		//				ProfitPercent = prevItem != null ? prevItem.ProfitPercent : 0m,
		//			};

		//			var ops = opers.Where(x => x.Date.Date == d).OrderBy(x => x.Stock.Ticker);
		//			var stocks = ops.GroupBy(x => x.Stock).Select(g => new { Stock = g.Key, Ops = g.Select(x1 => x1) });			

		//			foreach(var s in stocks)
		//			{
		//				qty = 0;

		//				foreach(var o in s.Ops)	
		//				{
		//					if (o.Type == OperationType.Buy)
		//					{
		//						item.StockSum += o.Summa.Value;
		//						qty += o.Qty.Value;
		//					}
		//					if (o.Type == OperationType.Sell) {
		//						item.StockSum -= o.Summa.Value;
		//						qty -= o.Qty.Value;
		//					}									
		//				}				

		//				decimal closePrice = 0;
		//				var h = Invest.WebApp.Core.Instance.History.FirstOrDefault(x => x.Ticker == s.Stock.Ticker);
		//				if (h != null && h.Items.ContainsKey(d))
		//					closePrice = h.Items[d].Close;

		//				item.CurStockSum += closePrice * qty;
		//				item.Profit = item.CurStockSum - item.StockSum;
		//				item.ProfitPercent = item.Profit / item.StockSum * 100;
		//			}

		//			model.Items.Add(item);	
		//			prevItem = item;
		//		}

		//		if (isJson)
		//			return new JsonResult(model.Items.Select(x => new { Date = x.Date.ToShortDateString(), x.Profit }).OrderBy(x => x.Date));

		//		return View(model);
		//       }


		public IActionResult Bonds()
		{
			var stocks = _builder.Stocks
				.Where(x => x.Type == StockType.Bond)
				//&& (x.Ticker == "MTSS" || x.Ticker == "NVTK" || x.Ticker == "TDC" || x.Ticker == "AAL" || x.Ticker == "CLF" )
				//&& (currency == null || (int)x.Currency == currency)
				//&& (account == null || Core.Instance.Operations.Any(o => o.Stock == x && (int)o.AccountType == account))
				.ToList();

			var model = new BondsViewModel
			{
				Stocks = stocks,
				Accounts = new List<AccountType?> { AccountType.Iis, AccountType.VBr, null }
			};

			decimal buyStockSumUsd = 0, buyStockSumRur = 0;

			model.Items = new List<BondsViewModel.Item>();
			model.TotalSaldo = 0;

			foreach (var s in stocks)
			{
				var ops = _builder.Operations
					.Where(x => x.Stock == s
						//&& s.Data.QtyBalance > 0
						//&& s.Data.PosPrice != null
						&& (x.Type == OperationType.Buy || x.Type == OperationType.Sell || x.Type == OperationType.Coupon)
					   )
					   .ToList();

				if (!ops.Any())
					continue;

				var item = new BondsViewModel.Item
				{
					Stock = s,
					BuyQty = ops.Where(x => x.Type == OperationType.Buy).Sum(x => x.Qty) ?? 0,
					BuySum = ops.Where(x => x.Type == OperationType.Buy).Sum(x => x.Summa) ?? 0,
					Nkd = ops.Where(x => x.Type == OperationType.Buy).Sum(x => x.Nkd) ?? 0,
					SellSum = ops.Where(x => x.Type == OperationType.Sell).Sum(x => x.Summa) ?? 0,
					Coupon = ops.Where(x => x.Type == OperationType.Coupon).Sum(x => x.Summa) ?? 0,
					Commission = ops.Where(x => x.Type == OperationType.Buy).Sum(x => x.Commission) ?? 0
				};

				model.TotalSaldo += (item.BuySum - item.SellSum);

				model.Items.Add(item);
			}

			//if (isJson)
			//	 return new JsonResult(model.Items.OrderByDescending(x => x.ProfitInRur));

			return View(model);
		}





		//	//public JsonResult HistData()
		//	//{
		//	//          return new JsonResult("{}");
		//	//}

		//       /// <summary>Возвращает свойство объекта</summary>
		//       /// <param name="obj">Объект</param>
		//       /// <param name="property">Имя свойства.</param>
		//       private static object GetPropertyValue(object obj, string property)
		//       {
		//           var field = obj.GetType().GetField(property);
		//           return field != null 
		//               ? field.GetValue(obj) 
		//               : null;
		//       }
		//   }


		//public class StockViewModel: BaseViewModel
		//{
		//	public string Country;
		//	public IOrderedEnumerable<StockItem> StockItems;		
		//}

		//   public class ProfitViewModel : BaseViewModel
		//   {
		//       public Portfolio? Portfolio;
		//       public IOrderedEnumerable<StockItem> StockItems;
		//	public List<string> Tickers;

		//       public class StockItem
		//       {
		//           public Stock Stock;
		//           public int? Qty;
		//           public int LotCount;
		//           public int? BuyQty;
		//           public int? SellQty;
		//           public DateTime FirstBuy, LastBuy;
		//           public DateTime? FirstSell, LastSell;
		//           public decimal? BuySum;
		//           public decimal? SellSum;
		//           public decimal? TotalSum;
		//           public decimal? TotalSumInRur;
		//		public decimal? Commission;
		//		public decimal? DivUsd, DivRur;
		//		public decimal? FifoUsd, FifoRur, FifoInRur, FifoBaseRur, FifoRurComm;
		//       }
		//   }

		//public class PortfolioViewModel : BaseViewModel
		//   {
		//       public Portfolio? Portfolio;
		//       public List<Stock> Stocks;
		//       public List<AccountType?> Accounts;
		//	public decimal TotalProfitUsd, TotalProfitRur, TotalProfitInRur;
		//	public Dictionary<AccountType, decimal> TotalProfits;
		//	public decimal TotalProfitPercentUsd, TotalProfitPercentRur, TotalProfitPercent;
		//	public decimal TotalCloseFinResultUsd, TotalCloseFinResultRur;
		//	public decimal TotalFinResultUsd, TotalFinResultRur;
		//       public IOrderedEnumerable<Item> Items;
		//	public Dictionary<Currency, decimal> TotalStockSum, TotalCurStockSum;
		//       public decimal TotalRurSum;

		//	public PortfolioViewModel()
		//	{
		//           TotalStockSum = new Dictionary<Currency, decimal> {{Currency.Usd, 0}, {Currency.Rur, 0}};
		//           TotalCurStockSum = new Dictionary<Currency, decimal> {{Currency.Usd, 0}, {Currency.Rur, 0}};
		//       }

		//       public class Item
		//       {			
		//           public Stock Stock;
		//           public int Qty;
		//           public int LotCount;
		//		public decimal BuyTotalSum;
		//           public int BuyQty;
		//           public int SellQty;
		//		public int CurrentQty;
		//           public DateTime? FirstBuy, LastBuy;
		//           public DateTime? FirstSell, LastSell;
		//		public decimal? MinBuyPrice, MaxSellPrice;
		//           public decimal? BuySum;
		//           public decimal? SellSum;
		//           public decimal? TotalSum;
		//           public decimal? TotalSumInRur;
		//           public decimal ProfitUsd;
		//           public decimal ProfitRur;
		//		public decimal Commission;
		//		public decimal Divs;
		//		public decimal DivUsd, DivRur;
		//		public decimal CloseFinResult, FinResult, TotalFinResult, Profit, ProfitInRur, ProfitPercent;
		//		public decimal StockSum, CurStockSum;
		//           // % in portfolio of Total Sum
		//           public decimal PortfPercent;
		//       }
		//   }

		//public class HistViewModel : BaseViewModel
		//{
		//	public List<Item> Items;

		//       public class Item
		//       {
		//		public DateTime Date;
		//           public decimal Profit, ProfitPercent;
		//		public decimal BuySum;
		//		public decimal SellSum;
		//		public decimal StockSum;
		//		public decimal CurStockSum;
		//	}
		//}



		//   public class StockItem
		//{
		//	public Stock Stock;
		//	public int? Qty;
		//	public int LotCount;
		//	public int? BuyQty;
		//	public int? SellQty;
		//	public DateTime FirstBuy, LastBuy;
		//	public DateTime? FirstSell, LastSell;
		//	public Decimal? BuySum;
		//	public Decimal? SellSum;
		//	public Decimal? TotalSum;
		//	public Decimal? TotalSumInRur;
		//	public Decimal? NotLossPrice;
		//	public Decimal? LastMinBuyPrice, LastAvgBuyPrice;
		//}

		//   public class DivsViewModel
		//   {
		//       public Currency? Cur;
		//       public int? Year;
		//   }

		//   public class CacheViewModel : BaseViewModel
		//   {
		//	public Dictionary<Item, IEnumerable<Operation>> CurBuyOps;
		//	public Dictionary<Item, IEnumerable<Operation>> CurSellOps;
		//	public Dictionary<Item, IEnumerable<Operation>> BuysOps, SellOps, Divs;
		//	public Account Account;
		//	public Currency Cur;

		//	public CacheViewModel()
		//	{
		//		CurBuyOps = new Dictionary<Item, IEnumerable<Operation>>();
		//		CurSellOps = new Dictionary<Item, IEnumerable<Operation>>();

		//		BuysOps = new Dictionary<Item, IEnumerable<Operation>>();
		//		SellOps = new Dictionary<Item, IEnumerable<Operation>>();
		//		Divs = new Dictionary<Item, IEnumerable<Operation>>();
		//	}

		//	public struct Item
		//	{
		//		public Currency Cur;
		//		public AccountType Account;

		//		public Item(Currency cur, AccountType acc)
		//		{
		//			Cur = cur;
		//			Account = acc;
		//		}
		//	}
		//   }

	}
}