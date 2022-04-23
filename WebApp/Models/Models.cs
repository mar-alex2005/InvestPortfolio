﻿using System;
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
			public int LotCount;
			public decimal BuyTotalSum;
			public int BuyQty;
			public int SellQty;
			public int CurrentQty;
			public DateTime? FirstBuy, LastBuy;
			public DateTime? FirstSell, LastSell;
			public decimal? BuySum;
			public decimal? Coupon;
			public decimal? Nkd;
			public decimal? SellSum;
			public decimal? TotalSum;
			public decimal? TotalSumInRur;
			public decimal ProfitUsd;
			public decimal ProfitRur;
			public decimal Commission;
			public decimal CloseFinResult, FinResult, TotalFinResult, Profit, ProfitInRur, ProfitPercent;
			public decimal StockSum, CurStockSum;
			// % in portfolio of Total Sum
			public decimal SaldoPercent;
		}
	}
}