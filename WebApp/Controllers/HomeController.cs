using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Invest.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Invest.Core.Enums;
using Invest.WebApp.Models;

namespace Invest.WebApp.Controllers
{
	public class HomeController : Controller
	{
		private readonly Invest.Core.Builder _builder;

		public HomeController(Invest.Core.Builder builder)
		{
			_builder = builder;
		}

		public IActionResult Index(DateTime? start, DateTime? end)
		{
			if (start == null)
				start = DateTime.Today.AddMonths(-6);
			if (end == null)
				end = DateTime.Now;

			var operations = _builder.Operations
				.Where(x => x.Type == OperationType.Buy || x.Type == OperationType.Sell || x.Type == OperationType.Dividend || x.Type == OperationType.Coupon)
				.Where(x => x.Date >= start && x.Date <= end)
				.OrderByDescending(x => x.Date).ThenByDescending(x => x.Index)
				.ToList();

			var model = new OperationViewModel {
				Ticker = null,
				Stocks = _builder.Stocks,
				Operations = operations,
				Start = start,
				End = end,
				Accounts = _builder.Accounts
			};

			return View(model);
		}


		public IActionResult OperationList(DateTime? start, DateTime? end, string accounts)
		{
			if (start == null)
				start = DateTime.Today.AddMonths(-6);
			if (end == null)
				end = DateTime.Now;
			if (string.IsNullOrEmpty(accounts))
				accounts = "";

			var accountsList = accounts.Split(',', StringSplitOptions.RemoveEmptyEntries);

			var operations = _builder.Operations
				.Where(x => x.Type == OperationType.Buy || x.Type == OperationType.Sell || x.Type == OperationType.Dividend || x.Type == OperationType.Coupon)
				.Where(x => x.Date >= start && x.Date <= end)
				.Where(x => accountsList.Contains(x.Account.BitCode.ToString()))
				.OrderByDescending(x => x.Date).ThenByDescending(x => x.Index)
				.ToList();

			var model = new OperationViewModel {
				Ticker = null,
				Stocks = _builder.Stocks,
				Operations = operations,
				Start = start,
				End = end,
				Accounts = _builder.Accounts
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
				VirtualAccounts = _builder.VirtualAccounts,
				Accounts = _builder.Accounts,
				Operations = operations,
				FifoResults = _builder.FifoResults.Where(x => x.Key.Ticker == stock.Ticker)
			};

			return View(model);
		}

        public IActionResult TickerList(int curId = 0, int typeId = 0, bool showZero = true)
        {
			var list = _builder.Stocks
				.Where(x => (curId == 0 || (int)x.Currency == curId)
                    && (typeId == 0 || (int)x.Type == typeId)
					&& (showZero || x.Data.QtyBalance != 0)
                )
				.OrderBy(x => !(x.Data.QtyBalance > 0))
                .ThenBy(x => x.Company.Name)
                .ToList();

			var model = new TickersViewModel
			{
                CurId = curId,
				Stocks = list
            };

            return View("Tickers", model);
        }

		public IActionResult TickerDetails()
		{
			var tickerId = Request.Query.ContainsKey("tickerId")
			 ? Request.Query["tickerId"].ToString()
			 : null;

			var stock = _builder.GetStock(tickerId);

			var operations = _builder.Operations
				.Where(
					x => (x.Type == OperationType.Buy || x.Type == OperationType.Sell || x.Type == OperationType.Dividend)
				//&& (tickerId == null || x.Stock.Ticker == tickerId)
				)
				.OrderByDescending(x => x.Date).ThenByDescending(x => x.TransId) // Index
				.ToList();

			var model = new OperationViewModel
			{
				Ticker = tickerId,
				Stock = stock,
				VirtualAccounts = _builder.VirtualAccounts,
				Accounts = _builder.Accounts,
				Operations = operations,
				FifoResults = _builder.FifoResults.Where(x => x.Key.Ticker == stock.Ticker)
			};

			return View(model);
		}

		public IActionResult Stocks(string country, string orderBy = "company")
		{
			var list = new List<StockItem>();
			foreach (var s in _builder.Stocks.Where(x => x.Data.BuyQty > 0))
			{
				var item = new StockItem
				{
					Stock = s,
					BuyQty = _builder.Operations.Where(x => x.Stock == s && x.Type == OperationType.Buy).Sum(x => x.Qty),
					SellQty = _builder.Operations.Where(x => x.Stock == s && x.Type == OperationType.Sell).Sum(x => x.Qty)
				};

				//var qL = string.Format("{0}", q);
				//if (q >= 1000 && s.LotSize > 1)
				//	qL = string.Format("{0:N0}K", q / 1000);
				//else if (q == 0)				
				//	qL = null;				

				Currency cur = Currency.Rur;

				var opp = _builder.Operations.Where(x => x.Stock == s && x.Type == OperationType.Buy).ToList();
				if (opp.Any())
				{
					item.FirstBuy = opp.Min(x => x.Date);
					item.LastBuy = opp.Max(x => x.Date);
					cur = opp[0].Currency;
				}

				opp = _builder.Operations.Where(x => x.Stock == s && x.Type == OperationType.Sell).ToList();
				if (opp.Any())
				{
					item.FirstSell = opp.Max(x => x.Date);
					item.LastSell = opp.Max(x => x.Date);
				}

				item.BuySum = _builder.Operations.Where(x => x.Stock == s && x.Type == OperationType.Buy).Sum(x => x.Summa);
				item.SellSum = _builder.Operations.Where(x => x.Stock == s && x.Type == OperationType.Sell)?.Sum(x => x.Summa);
				item.TotalSum = item.BuySum + (item.SellSum ?? 0);
				item.TotalSumInRur = _builder.Operations.Where(x => x.Stock == s
				                            && (x.Type == OperationType.Sell || x.Type == OperationType.Buy)).Sum(x => x.Summa);

				if (cur == Currency.Usd)
				{
					item.TotalSumInRur = _builder.Operations.Where(x => x.Stock == s
						&& (x.Type == OperationType.Sell || x.Type == OperationType.Buy)).Sum(x => x.RurSumma);
				}
				else
				{
					item.TotalSumInRur = _builder.Operations.Where(x => x.Stock == s
						&& (x.Type == OperationType.Sell || x.Type == OperationType.Buy)).Sum(x => x.Summa);
				}

				//notloss
				var notClosedOps = _builder.Operations.Where(x => x.Stock == s && !x.IsClosed).OrderByDescending(x => x.Date);
				if (notClosedOps.Count() == 0)
				{
					var lastOp = _builder.Operations.Where(x => x.Stock == s && x.PositionNum != null).OrderByDescending(x => x.Date).FirstOrDefault();
					if (lastOp != null)
					{
						//var posNum = lastOp.PositionNum;
						notClosedOps = _builder.Operations.Where(x => x.Stock == s && x.Date >= lastOp.Date).OrderByDescending(x => x.Date);
					}
				}

				var notClosedBuyOps = notClosedOps.Where(x => x.Type == OperationType.Buy).ToList();
				var notClosedSellOps = notClosedOps.Where(x => x.Type == OperationType.Sell).ToList();

				var notClosedBuySum = notClosedBuyOps.Sum(x => x.Price * x.Qty);
				var notClosedSellSum = notClosedSellOps.Sum(x => x.Price * x.Qty);
				var notClosedSaldo = notClosedSellSum - notClosedBuySum;

				var qty = notClosedBuyOps.Sum(x => x.Qty) - notClosedSellOps.Sum(x => x.Qty);
				if (qty != 0)
					item.NotLossPrice = Math.Abs(notClosedSaldo.Value) / qty;

				//min, avg (at  months)
				var ops = _builder.Operations.Where(x => x.Stock == s && x.Type == OperationType.Buy && x.Date >= DateTime.Today.AddMonths(-6));
				if (ops.Count() == 0 || ops.Count() < 2)
					ops = _builder.Operations.Where(x => x.Stock == s && x.Type == OperationType.Buy).OrderByDescending(x => x.Date).Take(6);

				item.LastMinBuyPrice = ops.Min(x => x.Price);

				qty = ops.Sum(x => x.Qty);
				if (qty.HasValue && qty != 0)
					item.LastAvgBuyPrice = Math.Abs(ops.Sum(x => x.Price.Value * x.Qty.Value)) / qty.Value;


				//foreach (var a in Core.Instance.Accounts)
				//{
				//	notClosedOps = Core.Instance.Operations.Where(x => !x.IsClosed && x.AccountType == a.Type).OrderByDescending(x => x.Date);

				//	notClosedBuyOps = notClosedOps.Where(x => x.Type == OperationType.Buy).ToList();
				//	notClosedSellOps = notClosedOps.Where(x => x.Type == OperationType.Sell).ToList();

				//	notClosedBuySum = notClosedBuyOps.Sum(x => x.Price * x.Qty);
				//	notClosedSellSum = notClosedSellOps.Sum(x => x.Price * x.Qty);
				//	notClosedSaldo = notClosedSellSum - notClosedBuySum;

				//	item.NotLossPrice = notClosedSaldo / s.Data.QtyBalance;
				//}

				if (string.IsNullOrEmpty(country) || country == "Rur" && cur == Currency.Rur || country == "Usd" && cur == Currency.Usd)
					list.Add(item);
			}

			var model = new StockViewModel
			{
				Country = country
			};

			if (!string.IsNullOrEmpty(orderBy))
			{
				if (orderBy.ToLower() == "company")
					model.StockItems = list.OrderBy(x => x.Stock.Company.Name);
				else if (orderBy.ToLower() == "ticker")
					model.StockItems = list.OrderBy(x => x.Stock.Ticker);
				else if (orderBy.ToLower() == "qty")
					model.StockItems = list.OrderBy(x => x.Stock.Data.QtyBalance);
				else
					model.StockItems = list.OrderBy(x => x.Stock.Ticker);
			}
			else
				model.StockItems = list.OrderBy(x => x.Stock.Ticker);

			return View(model);
		}




		public IActionResult Profit(PortfolioType? portfolio, bool isJson = false, string column = "ticker", string orderBy = "asc")
		{
			var model = new ProfitViewModel
			{
				Portfolio = portfolio,
				Tickers = new List<string>(),
				//VirtualAccounts = _builder.VirtualAccounts,
				Accounts = _builder.Accounts,
				Operations = _builder.Operations, 
				Stocks = _builder.Stocks,
				FifoResults = _builder.FifoResults, 
				FinIndicators = _builder.FinIndicators

			};

			var list = new List<ProfitViewModel.StockItem>();
			var stocks = _builder.Stocks
				.Where(x => x.Type == StockType.Share
				        && portfolio == null
				            || (portfolio == PortfolioType.IIs && _builder.Operations.Any(o => o.Stock == x && o.AccountType == AccountType.Iis))
				            || (portfolio == PortfolioType.Vbr && _builder.Operations.Any(o => o.Stock == x && o.AccountType == AccountType.VBr))
				            || (portfolio == PortfolioType.Usd && x.Currency == Currency.Usd)
				            || (portfolio == PortfolioType.Rur && x.Currency == Currency.Rur))
						//|| (portfolio == PortfolioType.BlueUs && x.Currency == Currency.Usd
						//    && (Invest.Core..BlueUsList.Contains(x.Ticker))
						//|| (portfolio == Entities.Portfolio.BlueRu && x.Currency == Currency.Rur
						//	&& (Invest.WebApp.Core.Instance.BlueRuList.Contains(x.Ticker))))
						//|| (portfolio == Entities.Portfolio.BlueEur && x.Currency == Currency.Eur
						//	&& (Invest.WebApp.Core.Instance.BlueEurList.Contains(x.Ticker))))
				.ToList();

			foreach (var s in stocks)
			{
				var item = new ProfitViewModel.StockItem
				{
					Stock = s,
					BuyQty = _builder.Operations.Where(x => x.Stock == s && x.Type == OperationType.Buy).Sum(x => x.Qty),
					SellQty = _builder.Operations.Where(x => x.Stock == s && x.Type == OperationType.Sell).Sum(x => x.Qty)
				};

				var cur = Currency.Rur;

				var opp = _builder.Operations.Where(x => x.Stock == s && x.Type == OperationType.Buy).ToList();
				if (opp.Any())
				{
					item.FirstBuy = opp.Min(x => x.Date);
					item.LastBuy = opp.Max(x => x.Date);
					cur = opp[0].Currency;
				}

				opp = _builder.Operations.Where(x => x.Stock == s && x.Type == OperationType.Sell).ToList();
				if (opp.Any())
				{
					item.FirstSell = opp.Max(x => x.Date);
					item.LastSell = opp.Max(x => x.Date);
				}

				item.BuySum = _builder.Operations.Where(x => x.Stock == s && x.Type == OperationType.Buy).Sum(x => x.Summa);
				item.SellSum = _builder.Operations.Where(x => x.Stock == s && x.Type == OperationType.Sell)?.Sum(x => x.Summa);
				item.TotalSum = item.BuySum + (item.SellSum ?? 0);
				item.TotalSumInRur = _builder.Operations.Where(x => x.Stock == s
				                                                    && (x.Type == OperationType.Sell || x.Type == OperationType.Buy))?.Sum(x => x.Summa);

				item.Commission = _builder.FinIndicators.Where(x => x.Key.Ticker == s.Ticker).Sum(x => x.Value.Commission);
				item.FifoUsd = _builder.FifoResults.Where(x => x.Key.Ticker == s.Ticker && x.Key.Cur == Currency.Usd).Sum(x => x.Value.Summa);
				item.FifoRur = _builder.FifoResults.Where(x => x.Key.Ticker == s.Ticker && x.Key.Cur == Currency.Rur).Sum(x => x.Value.Summa);
				item.FifoInRur = _builder.FifoResults.Where(x => x.Key.Ticker == s.Ticker).Sum(x => x.Value.RurSumma);
				item.FifoRurComm = _builder.FifoResults.Where(x => x.Key.Ticker == s.Ticker).Sum(x => x.Value.RurCommission);
				item.FifoBaseRur = item.FifoInRur - item.FifoRurComm;

				var divs = _builder.FinIndicators.Where(x => x.Value.DivSumma != null);
				item.DivUsd = divs.Where(x => x.Key.Ticker == item.Stock.Ticker && x.Key.Cur == Currency.Usd).Sum(x => x.Value.DivSumma);
				item.DivRur = divs.Where(x => x.Key.Ticker == item.Stock.Ticker && x.Key.Cur == Currency.Rur).Sum(x => x.Value.DivSumma);

				if (cur == Currency.Usd)
				{
					item.TotalSumInRur = _builder.Operations.Where(x => x.Stock == s
					                                                    && (x.Type == OperationType.Sell || x.Type == OperationType.Buy))?.Sum(x => x.RurSumma);
				}
				else
				{
					item.TotalSumInRur = _builder.Operations.Where(x => x.Stock == s
					                                                    && (x.Type == OperationType.Sell || x.Type == OperationType.Buy))?.Sum(x => x.Summa);
				}

				list.Add(item);
				model.Tickers.Add(item.Stock.Ticker);
			}

			if (!string.IsNullOrEmpty(column))
			{
				if (column.ToLower() == "ticker")
					model.StockItems = orderBy == "asc" ? list.OrderBy(x => x.Stock.Ticker) : list.OrderByDescending(x => x.Stock.Ticker);
				else if (column.ToLower() == "qty")
					model.StockItems = orderBy == "asc" ? list.OrderBy(x => x.Stock.Data.QtyBalance) : list.OrderByDescending(x => x.Stock.Data.QtyBalance);
				else if (column.ToLower() == "FifoUsd".ToLower())
					model.StockItems = orderBy == "asc" ? list.OrderBy(x => x.FifoUsd) : list.OrderByDescending(x => x.FifoUsd);
				else if (column.ToLower() == "FifoRur".ToLower())
					model.StockItems = orderBy == "asc" ? list.OrderBy(x => x.FifoRur) : list.OrderByDescending(x => x.FifoRur);
				else if (column.ToLower() == "FifoInRur".ToLower())
					model.StockItems = (orderBy == "asc") ? list.OrderBy(x => x.FifoInRur) : list.OrderByDescending(x => x.FifoInRur);
				else if (column.ToLower() == "DivUsd".ToLower())
					model.StockItems = (orderBy == "asc") ? list.OrderBy(x => x.DivUsd) : list.OrderByDescending(x => x.DivUsd);
				else if (column.ToLower() == "DivRur".ToLower())
					model.StockItems = (orderBy == "asc") ? list.OrderBy(x => x.DivRur) : list.OrderByDescending(x => x.DivRur);
				else
					model.StockItems = (orderBy == "asc") ? list.OrderBy(x => x.Stock.Ticker) : list.OrderByDescending(x => x.Stock.Ticker);
			}
			else
				model.StockItems = list.OrderBy(x => x.Stock.Ticker);

			return View(model);
		}



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



		public IActionResult Cache()
		{
			//var endDate = new DateTime(2021, 12, 01);

			var model = new CacheViewModel {
				VirtualAccounts = _builder.VirtualAccounts,
				Accounts = _builder.Accounts, 
				Operations = _builder.Operations, // _builder.Operations.Where(x => x.Date.Year == 2021 && x.Date < endDate).ToList(),
				Currencies = new List<Currency> { Currency.Usd, Currency.Eur, Currency.Cny },
				//NotExecutedOperations = _builder.Operations
			};

			var allCurOps = _builder.Operations
				.Where(x => x.Type == OperationType.CurBuy || x.Type == OperationType.CurSell)
				.ToList();
			
			foreach (var a in _builder.Accounts)
			{
				foreach (var cur in model.Currencies)
				{
					model.CurBuyOps.Add(new CacheViewModel.Item(cur, a),
						allCurOps.Where(x => x.Type == OperationType.CurBuy && x.Account == a && x.Currency == cur));
					model.CurSellOps.Add(new CacheViewModel.Item(cur, a),
						allCurOps.Where(x => x.Type == OperationType.CurSell && x.Account == a && x.Currency == cur));
				
					model.BuysOps.Add(new CacheViewModel.Item(cur, a),
						_builder.Operations.Where(x => x.Type == OperationType.Buy
						                               && x.Account == a && x.Stock != null && x.Currency == cur));

					model.SellOps.Add(new CacheViewModel.Item(cur, a),
						_builder.Operations.Where(x => x.Type == OperationType.Sell
						                               && x.Account == a && x.Stock != null && x.Currency == cur));

					model.Divs.Add(new CacheViewModel.Item(cur, a),
						_builder.Operations.Where(x => x.Type == OperationType.Dividend
						                               && x.Account == a && x.Stock != null && x.Currency == cur));

					var saldo = 0;
					var totalComm = .0m;
					var opers = allCurOps.Where(x => x.Account == a && x.Currency == cur);
					foreach (var o in opers)
					{
						saldo += o.Type == OperationType.CurBuy ? o.Qty.Value : -o.Qty.Value;
						totalComm += o.Commission ?? 0;
						var item = new CacheViewModel.CurOperItem(a, cur, o) { Saldo = saldo, TotalComm = totalComm };
						model.CurOperations.Add(item);
					}
				}
			}

			return View(model);
		}



		public IActionResult Divs(int? cur = null, int? year = null)
		{
			var model = new DivsViewModel
			{
				Cur = (Currency?)cur,
				Year = year,

				VirtualAccounts = _builder.VirtualAccounts,
				Accounts = _builder.Accounts, 
				Operations = _builder.Operations.Where(x => x.Type == OperationType.Dividend).ToList(),
				Currencies = new List<Currency> { Currency.Rur, Currency.Usd, Currency.Eur }
			};

			return View(model);
		}


		//       public IActionResult AmChart()
		//       {
		//           return View();
		//       }

		//       public JsonResult CacheInData(AccountType? accountType)
		//       {
		//           var ops = Invest.WebApp.Core.Instance.Operations
		//               .Where(x => x.Type == OperationType.CacheIn 
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

		public JsonResult StockChartData(Currency cur)
		{
			//var ss = _builder.Stocks.Where(x => x.Data.QtyBalance > 0).OrderBy(x => x.SortIndex);

			var divsStocksRur = _builder.Operations.Where(x => x.Type == OperationType.Dividend)
				.Where(x => x.Currency == cur)
				.GroupBy(x => x.Stock)
				.Select(g => new { Stock = g.Key, Sum = g.Sum(x1 => x1.Summa) })
				.ToList();

			return new JsonResult(divsStocksRur);
		}

		//       public JsonResult LoadPrices()
		//       {
		//           var priceMgr = new PriceManager();
		//           priceMgr.LoadMoexPrices();
		//           return new JsonResult(null);
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
				.OrderBy(x => x.Company.Name).ThenBy(x => x.Ticker)
				.ToList();

			var model = new BondsViewModel
			{
				Stocks = stocks,
				VirtualAccounts = _builder.VirtualAccounts,
				Accounts = _builder.Accounts,
				Operations = _builder.Operations.Where(x => x.Type == OperationType.Coupon
					|| (x.Type == OperationType.Sell && x.Stock != null && x.Stock.Type == StockType.Bond)).ToList()
			};

			model.Items = new List<BondsViewModel.Item>();
			model.TotalSaldo = 0;
			
			foreach (var s in stocks)
			{
				var ops = _builder.Operations
					.Where(x => x.Stock == s
						&& (x.Type == OperationType.Buy || x.Type == OperationType.Sell || x.Type == OperationType.Coupon)
					   )
					   .ToList();

				if (!ops.Any())
					continue;

				var item = new BondsViewModel.Item
				{
					Stock = s,
					BuyQty = ops.Where(x => x.Type == OperationType.Buy).Sum(x => x.Qty) ?? 0,
					SellQty = ops.Where(x => x.Type == OperationType.Sell).Sum(x => x.Qty) ?? 0,
					BuySum = ops.Where(x => x.Type == OperationType.Buy).Sum(x => x.Summa) ?? 0,
					Nkd = ops.Where(x => x.Type == OperationType.Buy).Sum(x => x.Nkd) ?? 0,
					SellSum = ops.Where(x => x.Type == OperationType.Sell).Sum(x => x.Summa + (x.Nkd ?? 0)) ?? 0,
					Coupon = ops.Where(x => x.Type == OperationType.Coupon).Sum(x => x.Summa) ?? 0,
					Commission = ops.Where(x => x.Type == OperationType.Buy).Sum(x => x.Commission) ?? 0
				};

				if (s.Ticker == "Татнфт1P1") { var t = 9; }

				model.TotalSaldo += item.CurrentQty != 0 ? (item.BuySum - item.SellSum) : 0;

				model.Items.Add(item);
			}

			//if (isJson)
			//	 return new JsonResult(model.Items.OrderByDescending(x => x.ProfitInRur));

			return View(model);
		}


		public JsonResult Tickers(int curId = 0)
		{
			var list = _builder.Stocks
				.Where(x => curId == 0 || (int)x.Currency == curId)
				.OrderBy(x => !(x.Data.QtyBalance > 0))
				.ThenBy(x => x.Company.Name)
                .Select(x => new { company = x.Company, x.Ticker, qty = x.Data.QtyBalance })
                .ToList();

			return new JsonResult(list);
		}

		public IActionResult Cur()
		{
			var model = new CurrencyViewModel {
				VirtualAccounts = _builder.VirtualAccounts,
				Accounts = _builder.Accounts, 
				Currencies = new List<Currency> { Currency.Usd, Currency.Eur, Currency.Cny },
				//NotExecutedOperations = _builder.Operations
			};

			var sellStack = new List<CurSellItem>();
			var sellsOps = _builder.Operations.Where(x => x.Type == OperationType.CurSell).ToList();
			foreach (var o in sellsOps)
				sellStack.Add(new CurSellItem { 
					SellOperation = o, VAccount = o.Account.VirtualAccount, Cur = o.Currency, 
					Qty = o.Qty.Value, Saldo = o.Qty.Value, BuyOperations = new List<CurBuytem>() });

			foreach (var cur in model.Currencies)
			{
				foreach (var va in _builder.VirtualAccounts)	
				{
					var buyStack = new Stack<CurBuytem>();
					foreach (var o in _builder.Operations
							.Where(x => x.Type == OperationType.CurBuy && x.Currency == cur && x.Account.VirtualAccount == va)
							.OrderByDescending(x => x.Date))
						buyStack.Push(new CurBuytem { BuyOperation = o, Qty = o.Qty.Value });

					//if (buyStack.Any(x => x.BuyOperation.Currency == Currency.Eur)) { var t =0;	}

					foreach (var sell in sellStack.Where(x => x.Cur == cur && x.VAccount == va))
					{
						//if (cur == Currency.Usd && va.Id == "IIS" && sell.SellOperation.DeliveryDate.Value.Year == 2022) { var r=0; }
						if (buyStack.Count == 0)
						{
							continue;
						}

						while(sell.Saldo != 0) 
						{
							if (buyStack.Count == 0)
								break;

							var bOper = buyStack.Peek();
							if (bOper.Qty <= sell.Saldo)
							{
								sell.Saldo -= bOper.Qty;
								sell.BuySumma += bOper.Qty * bOper.BuyOperation.Price;
								sell.BuyOperations.Add(new CurBuytem { BuyOperation = bOper.BuyOperation, Qty = bOper.Qty });
								buyStack.Pop();
							}
							else
							{
								var q = sell.Saldo;
								if (q < 0)
									throw new Exception($"Cur(): q < 0, {sell.SellOperation.Qty.Value}, {sell.Saldo}");
								
								sell.Saldo -= q;
								sell.BuySumma += q * bOper.BuyOperation.Price;
								sell.BuyOperations.Add(new CurBuytem { BuyOperation = bOper.BuyOperation, Qty = q });
								bOper.Qty -= q;
							}

							if (cur == Currency.Usd && va.Id == "VBr" &&  sell.SellOperation.DeliveryDate.Value.Year == 2020)
							{
								Debug.WriteLine($"bOper: {bOper.Qty}");
							}
						}
					}
				}
			}

			model.Operations = _builder.Operations.Where(x => x.Type == OperationType.CurSell || x.Type == OperationType.CurBuy).ToList();
			model.Items = sellStack;

			return View(model);
		}
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
}