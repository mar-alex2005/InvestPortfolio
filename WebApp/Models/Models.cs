using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Invest.Core.Entities;
using Invest.Core.Enums;

namespace Invest.WebApp.Models
{

	public class BaseViewModel
	{
		public HtmlString Value(decimal? value, bool useZnak = true, string css = null)
		{
			var znak = "";
			if (value > 0)
				znak = " +";
			if (value < 0)
				znak = " ";

			if (!useZnak)
				znak = "";

			var s = "font-weight: bold;";
			if (value > 0)
				s += "color: limegreen;";
			if (value < 0)
				s += "color: red;";
			if (value == 0)
				s += "color: grey;";

			var z = $"{znak}{value:N2}";

			return new HtmlString("<span style='" + s + ";" + css + "'>" + z + "</span>");
		}
	}

	public class OperationViewModel : BaseViewModel
	{
		public DateTime? Start;
		public DateTime? End;
		public string Ticker;
		public BaseStock Stock;
		public List<Operation> Operations;
		public List<BaseStock> Stocks;
		public List<BaseAccount> Accounts;
		public List<VirtualAccount> VirtualAccounts;
		public IEnumerable<KeyValuePair<Analytics, FifoResult>> FifoResults;
	}

    public class TickersViewModel : BaseViewModel
    {
		public int CurId;
        public List<BaseStock> Stocks;
    }


	public class BondsViewModel : BaseViewModel
	{
		//public Portfolio? Portfolio;
		public List<BaseStock> Stocks;
		public List<BaseAccount> Accounts;
		public List<VirtualAccount> VirtualAccounts;
		public decimal? TotalProfitUsd, TotalProfitRur, TotalProfitInRur, TotalSaldo;
		public Dictionary<AccountType, decimal> TotalProfits;
		public decimal TotalProfitPercentUsd, TotalProfitPercentRur, TotalProfitPercent;
		public List<Item> Items;
		public List<Operation> Operations;

		public BondsViewModel()
		{
			Items = new List<Item>();
			Operations = new List<Operation>();
			//TotalStockSum = new Dictionary<Currency, decimal> {{Currency.Usd, 0}, {Currency.Rur, 0}};
		}

		public class Item
		{
			public BaseStock Stock;
			public int Qty;
			public decimal BuyTotalSum;
			public int BuyQty;
			public int SellQty;
			public DateTime? FirstBuy, LastBuy;
			public DateTime? FirstSell, LastSell;
			public decimal? BuySum;
			public decimal? Coupon;
			public decimal? Nkd;
			public decimal? SellSum;
			public decimal? TotalSumInRur;
			public decimal Commission;
			public decimal CloseFinResult, FinResult, TotalFinResult, Profit, ProfitInRur, ProfitPercent;
			public decimal StockSum, CurStockSum;
			// % in portfolio of Total Sum
			public decimal SaldoPercent;

			public int CurrentQty
			{
				get { return BuyQty - SellQty; }
			}

			public decimal? Value 
			{ 
				get { return CurrentQty != 0 ? BuySum - SellSum : null; }
			}
		}
	}


	public class CacheViewModel : BaseViewModel
	{
		public Dictionary<Item, IEnumerable<Operation>> CurBuyOps;
		public Dictionary<Item, IEnumerable<Operation>> CurSellOps;
		public Dictionary<Item, IEnumerable<Operation>> BuysOps, SellOps, Divs;
		public BaseAccount Account;
		public Currency Cur;
		public List<Operation> Operations, NotExecutedOperations;
		public List<BaseAccount> Accounts;
		public List<VirtualAccount> VirtualAccounts;
		public List<Currency> Currencies;
		public List<CurOperItem> CurOperations;

		public CacheViewModel()
		{
			CurBuyOps = new Dictionary<Item, IEnumerable<Operation>>();
			CurSellOps = new Dictionary<Item, IEnumerable<Operation>>();

			BuysOps = new Dictionary<Item, IEnumerable<Operation>>();
			SellOps = new Dictionary<Item, IEnumerable<Operation>>();
			Divs = new Dictionary<Item, IEnumerable<Operation>>();
			CurOperations = new List<CurOperItem>();
		}

		public struct Item
		{
			public Currency Cur;
			public BaseAccount Account;

			public Item(Currency cur, BaseAccount acc)
			{
				Cur = cur;
				Account = acc;
			}
		}

		public class CurOperItem
		{
			public Currency Cur;
			public BaseAccount Account;
			public Operation Operation;
			public int Saldo;
			public decimal TotalComm;

			public CurOperItem(BaseAccount account, Currency cur, Operation operation)
			{
				Cur = cur;
				Account = account;
				Operation = operation;
			}
		}

		public decimal? GetSum(OperationType type, Currency? cur, BaseAccount acc = null)
		{ 
			var v = Operations.Where(x => x.Type == type 
                      && (cur == null || x.Currency == cur) 
                      && (acc == null || x.Account == acc))
				.Sum(x => x.Summa);

			//if (type == OperationType.Buy && cur == Currency.Usd && acc != null && acc.BitCode == 2) { var r=0; }

			return v != null ? Math.Abs(v.Value) : (decimal?)null;
		}

		public decimal? GetNkd(OperationType type, Currency? cur, BaseAccount acc = null)
		{ 
			var v = Operations.Where(x => x.Type == type 
                          && (cur == null || x.Currency == cur) 
                          && (acc == null || x.Account == acc)
                          && x.Nkd != null)
				.Sum(x => x.Nkd);

			return v != null ? Math.Abs(v.Value) : (decimal?)null;
		}
		
		public decimal? GetQty(OperationType type, Currency? cur, BaseAccount acc = null)
		{ 
			var v = Operations.Where(x => x.Type == type 
			                              && (cur == null || x.Currency == cur) 
			                              && (acc == null || x.Account == acc))
				.Sum(x => x.Qty);

			return v != null ? Math.Abs(v.Value) : (decimal?)null;
		}

		public decimal? GetComm(OperationType[] types, Currency? cur, BaseAccount acc = null)
		{ 
			var v = Operations
				.Where(x => (types == null || types.Contains(x.Type))
				            && (acc == null || x.Account == acc) 
				            && x.Currency == cur
                )
				.Sum(x => x.Commission ?? 0m);

			return v;
		}

		/// <summary>returns commis in RUB for currency operations</summary>
		/// <param name="types"></param>
		/// <param name="cur"></param>
		/// <param name="acc"></param>
		/// <returns></returns>
		public decimal? GetCurComm(OperationType[] types, Currency? cur = null, BaseAccount acc = null)
		{ 
			var v = Operations
				.Where(x => types.Contains(x.Type)
			            && (acc == null || x.Account == acc) 
			            && (cur == null || x.Currency == cur)
				)
				.Sum(x => x.Commission ?? 0m);

			return v;
		}
	}


	public class DivsViewModel : BaseViewModel
	{
		public Currency? Cur;
		public int? Year;
		public List<Operation> Operations;
		public List<BaseAccount> Accounts;

		public List<VirtualAccount> VirtualAccounts;
		public List<Currency> Currencies;
	}


	public class StockViewModel : BaseViewModel
	{
		public string Country;
		public IOrderedEnumerable<StockItem> StockItems;
	}

	public class ProfitViewModel : BaseViewModel
	{
		//public Portfolio? Portfolio;
		public IOrderedEnumerable<StockItem> StockItems;
		public List<string> Tickers;

		public class StockItem
		{
			public Stock Stock;
			public int? Qty;
			public int LotCount;
			public int? BuyQty;
			public int? SellQty;
			public DateTime FirstBuy, LastBuy;
			public DateTime? FirstSell, LastSell;
			public decimal? BuySum;
			public decimal? SellSum;
			public decimal? TotalSum;
			public decimal? TotalSumInRur;
			public decimal? Commission;
			public decimal? DivUsd, DivRur;
			public decimal? FifoUsd, FifoRur, FifoInRur, FifoBaseRur, FifoRurComm;
		}
	}

	public class PortfolioViewModel : BaseViewModel
	{
		//public Portfolio? Portfolio;
		public List<Stock> Stocks;
		public List<AccountType?> Accounts;
		public decimal TotalProfitUsd, TotalProfitRur, TotalProfitInRur;
		public Dictionary<AccountType, decimal> TotalProfits;
		public decimal TotalProfitPercentUsd, TotalProfitPercentRur, TotalProfitPercent;
		public decimal TotalCloseFinResultUsd, TotalCloseFinResultRur;
		public decimal TotalFinResultUsd, TotalFinResultRur;
		public IOrderedEnumerable<Item> Items;
		public Dictionary<Currency, decimal> TotalStockSum, TotalCurStockSum;
		public decimal TotalRurSum;

		public PortfolioViewModel()
		{
			TotalStockSum = new Dictionary<Currency, decimal> { { Currency.Usd, 0 }, { Currency.Rur, 0 } };
			TotalCurStockSum = new Dictionary<Currency, decimal> { { Currency.Usd, 0 }, { Currency.Rur, 0 } };
		}

		public class Item
		{
			public Stock Stock;
			public int Qty;
			public int LotCount;
			public decimal BuyTotalSum;
			public int BuyQty;
			public int SellQty;
			public int CurrentQty;
			public DateTime? FirstBuy, LastBuy;
			public DateTime? FirstSell, LastSell;
			public decimal? MinBuyPrice, MaxSellPrice;
			public decimal? BuySum;
			public decimal? SellSum;
			public decimal? TotalSum;
			public decimal? TotalSumInRur;
			public decimal ProfitUsd;
			public decimal ProfitRur;
			public decimal Commission;
			public decimal Divs;
			public decimal DivUsd, DivRur;
			public decimal CloseFinResult, FinResult, TotalFinResult, Profit, ProfitInRur, ProfitPercent;
			public decimal StockSum, CurStockSum;
			// % in portfolio of Total Sum
			public decimal PortfPercent;
		}
	}

	public class StockItem
	{
		public BaseStock Stock;
		public int? Qty;
		public int LotCount;
		public int? BuyQty;
		public int? SellQty;
		public DateTime FirstBuy, LastBuy;
		public DateTime? FirstSell, LastSell;
		public Decimal? BuySum;
		public Decimal? SellSum;
		public Decimal? TotalSum;
		public Decimal? TotalSumInRur;
		public Decimal? NotLossPrice;
		public Decimal? LastMinBuyPrice, LastAvgBuyPrice;
	}
}