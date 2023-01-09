using System;
using System.Collections.Generic;
using System.Linq;
using Invest.Core.Enums;
using Newtonsoft.Json;

namespace Invest.Core.Entities
{
	public abstract class BaseAccount
	{
		public string Id;
		public string Name;
		public string Broker;
		public string BrokerName; //todo: delete in future
		public int BitCode;
		public int SortIndex;
		public AccountType Type;
		public VirtualAccount VirtualAccount;
	}

    public class Account : BaseAccount
    {
    }

	/// <summary>The virtual account that union any broker accounts</summary>
    public class VirtualAccount
    {
	    public string Id;
	    public string Name;
	    public int BitCode;
	    public int SortIndex;
    }


    public abstract class BaseCompany
    {
	    public string Id;
	    public string Name;
	    public string DivName;      // Имя для divs (from report)
	    public int SortIndex;
    }

    public class Company : BaseCompany
    {
    }

	public abstract class BasePortfolio
	{
		public string Id;
		public string Name;
		public int BitCode;
		public int SortIndex;
	}

    public class Portfolio : BasePortfolio
    {
    }



    public class Data
    {
		[JsonIgnore]
		public BaseStock Stock;
        public int BuyQty;
        public int SellQty;
        public DateTime? FirstBuy;
        public DateTime? LastBuy;
        public DateTime? FirstSell;
        public DateTime? LastSell;
        public decimal? BuyMin;
        public decimal? BuyMax;
        public decimal? BuyAvg;
        public decimal? SellMin;
        public decimal? SellMax;
        public decimal? SellAvg;
        public bool OpenPosExist;
        public decimal? BuySum;
        public decimal? SellSum;
        // last current position price
        public decimal? PosPrice;
		/// <summary>Market Summa</summary>
		public decimal? CurrentSum;
		public decimal? StockSum;
		public decimal? StockSumInRur;

        /// <summary>Saldo by qty = buys - sels</summary>
        public int QtyBalance => BuyQty - SellQty;

        public decimal? SumBalance => BuySum - SellSum ?? 0;

        public decimal? BuyCommission
		{
			get { 
				decimal? sum = Stock.AccountData.Values.Sum(x => x.BuyCommission);
				return sum;
			}
		}

		public decimal? SellCommission
		{
			get { 
				decimal? sum = Stock.AccountData.Values.Sum(x => x.SellCommission);
				return sum;
			}
		}
        public decimal? Commission {
            get { 
				return Stock.AccountData.Values.Sum(x => x.Commission);
				//return Core.SumValues(BuyCommission, SellCommission);				
			} 
        }
		                        
        // current result
        public decimal? ProfitInRur;
        public decimal? ProfitWithComm;
        public decimal? ProfitPercent;

        // result for closed position
        public decimal? FinResultForClosedPositions
		{
			get { 
				var sum = Stock.AccountData.Values.Sum(x => x.FinResultForClosedPositions);
				return sum;
			}
		}

		public decimal? FinResult
		{
			get { 
				var sum = Stock.AccountData.Values.Sum(x => x.TotalFinResult);
				return sum;
			}
		}
		public decimal? TotalFinResult
		{
			get { 
				var sum = Stock.AccountData.Values.Sum(x => x.TotalFinResult);
				return sum;
			}
		}

		public decimal? Profit
		{
			get { 
				var sum = Stock.AccountData.Values.Sum(x => x.Profit);
				return sum;
			}
		}

		public decimal? DivSummaRur {
			get { 
				var sum = Stock.AccountData.Values.Sum(x => x.DivSummaRur);
				return sum;
			}
		}

		public decimal? DivSummaUsd {
			get { 
				var sum = Stock.AccountData.Values.Sum(x => x.DivSummaUsd);
				return sum;
			}
		}
    }


    public class AccountData
    {        
        public int BuyQty;
        public int SellQty;
        public decimal? BuySum;
        public decimal? SellSum;

        [JsonIgnore]
        public BaseStock Stock;
        public BaseAccount Account;
		[Obsolete("use account")]
		public AccountType AccountType;

        // current result
        public decimal? Profit;
        public decimal? ProfitWithComm;
        public decimal? ProfitPercent;

        // result for closed position
        public decimal? FinResultForClosedPositions; 
		
		/// <summary>Market Summa</summary>
		public decimal? CurrentSum;
		public decimal? StockSum;
		public decimal? StockSumInRur;
        public decimal? BuyCommission, SellCommission;
		public decimal? DivSummaUsd, DivSummaRur;
		public decimal? NotLossPrice;
		public int CurrentQty;

		[JsonIgnore]
		public List<Operation> Operations;
		[JsonIgnore]
		public List<Position> Positions;

		public AccountData(){ }
		
		public AccountData(BaseAccount account, BaseStock stock)
		{
			Stock = stock;
			Account = account;
			AccountType = account.Type;
			//FinResult = 0;
			FinResultForClosedPositions = 0;
		}

		public int QtyBalance => BuyQty - SellQty;

		public decimal? Commission {
            get {
	            if (Positions == null || Positions.Count == 0)
					return null;

	            decimal comm = 0;
	            foreach(var p in Positions) {
		            comm += p.Commission;
	            }

	            return comm; 
				//return Core.SumValues(BuyCommission, SellCommission);
			} 
        }

        //public decimal? FinResult;

        public decimal? TotalFinResult {
	        get {
		        if (Positions != null && Positions.Count != 0 && Positions.Any(x => x.TotalFinResult != null))
					return Positions.Where(p => p.TotalFinResult != null).Sum(p => p.TotalFinResult.Value);
				
				return null;
	        } 
        }

		public decimal? PosPrice
		{
			get
			{
				var position = Positions?.LastOrDefault();
				return position?.Items[position.Items.Count-1].PosPrice;
			}
		}

		public decimal? SumBalance => BuySum - SellSum ?? 0;

		internal void AddCommission(Operation o)
		{
			if (o.Type == OperationType.Buy)
				BuyCommission = (BuyCommission ?? 0) + o.Commission;
            if (o.Type == OperationType.Sell)
				SellCommission = (SellCommission ?? 0) + o.Commission;
		}

		public Position GetPositionData(int num)
		{
			foreach (var t in Positions)
			{
				if (t.Num == num)
					return t;
			}

			throw new Exception($"Position not found by num {num}");
		}
	}

	/// <summary>Fin result by stock by year period</summary>
	public class FifoResult
	{
		public decimal? Summa;
		public decimal? RurSumma;
		public Currency Cur;
		public decimal Commission;
		public decimal RurCommission;
		public decimal? TotalSumma;
        /// <summary>Base suuma for calc ndfl</summary>
        public decimal BaseSumma => (RurSumma ?? 0) - RurCommission;
    }

	public class FinIndicator
	{
		//public FifoResult Fifo;
		public decimal? DivSumma;
		public decimal? CouponSumma;
		public decimal? Commission;
	}

	public class Period
	{
		public string Name => End.ToString("MMM, yy"); 
		public readonly int Month;
		public readonly int Year;
		public DateTime Start => new DateTime(Year, Month, 1);
		public DateTime End => new DateTime(Year, Month, DateTime.DaysInMonth(Year, Month));

		public Period(DateTime date)
		{
			Year = date.Year;
			Month = date.Month;
		}

		public override int GetHashCode()
		{
			return int.Parse(string.Format("{0:yyyy}{0:MM}", Start));
		}
        public override bool Equals(object obj)
        {
            var period = obj as Period;
            if (period != null)
                return period.GetHashCode() == GetHashCode();

            return false;
        }
    }


    /// <summary>Set of Analytics (for fin result FIFO)</summary>
    public class Analytics
    {
        public int Month;
        public int Year;
        public BaseAccount Account;
        public VirtualAccount VirtualAccount;
        public Currency Cur;
        public string Ticker;
        
        public Analytics()
        { }

        public Analytics(string ticker, BaseAccount account, Currency cur, DateTime date)
        {
            Ticker = ticker;
			Account = account;
            VirtualAccount = account.VirtualAccount;
            Cur = cur;
            Year = date.Year;
            Month = date.Month;
        }
        public Analytics(string ticker, VirtualAccount vAccount, Currency cur, DateTime date)
        {
	        Ticker = ticker;
	        VirtualAccount = vAccount;
	        Cur = cur;
	        Year = date.Year;
	        Month = date.Month;
        }

        public override int GetHashCode()
        {
            return Ticker.GetHashCode() + int.Parse(string.Format("{0}{1}{2}{3}", 
                    Month, Year, Account != null ? Account.Type : 0, (int)Cur));
        }

        public override bool Equals(object obj)
        {
            if (obj is Analytics a)
                return a.Ticker == Ticker && a.Account == Account && a.Cur == Cur && a.Year == Year && a.Month == Month;

            return false;
        }
    }

	public class History
    {
		public History(){}
		
		public History(Stock stock)
		{
			Ticker = stock.Ticker;
			LastDate = new DateTime(2019,12,1);
			Items = new Dictionary<DateTime, HistoryItem>();
		}

		public string Ticker;
		public DateTime LastDate;
		public Dictionary<DateTime, HistoryItem> Items;
	}

	public class HistoryItem
    {
		[JsonProperty("c")]
		public decimal Close;
	}


	//public class CurRates
	//{
	//	public Currency Currency;
	//	public decimal? Rate;
	//}
}