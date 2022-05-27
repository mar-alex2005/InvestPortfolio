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
		public List<AccountType?> Accounts;
		public List<VirtualAccount> VirtualAccounts;
		public decimal? TotalProfitUsd, TotalProfitRur, TotalProfitInRur, TotalSaldo;
		public Dictionary<AccountType, decimal> TotalProfits;
		public decimal TotalProfitPercentUsd, TotalProfitPercentRur, TotalProfitPercent;
		public List<Item> Items;

		public BondsViewModel()
		{
			Items = new List<Item>();
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
		public Account Account;
		public Currency Cur;
		public List<Operation> Operations;
		public List<BaseAccount> Accounts;

		public CacheViewModel()
		{
			CurBuyOps = new Dictionary<Item, IEnumerable<Operation>>();
			CurSellOps = new Dictionary<Item, IEnumerable<Operation>>();

			BuysOps = new Dictionary<Item, IEnumerable<Operation>>();
			SellOps = new Dictionary<Item, IEnumerable<Operation>>();
			Divs = new Dictionary<Item, IEnumerable<Operation>>();
		}

		public struct Item
		{
			public Currency Cur;
			public int AccCode;

			public Item(Currency cur, int bitCode)
			{
				Cur = cur;
				AccCode = bitCode;
			}
		}
	}
}