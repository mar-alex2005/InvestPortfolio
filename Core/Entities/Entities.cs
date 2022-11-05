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


	public class BaseStock
	{
		[JsonProperty(PropertyName = "t")]
	    public string Ticker;
		public string[] Isin; // array of isin`s. In order to store some isins, if company changes own fin.params
		public string RegNum; // reg num for bonds
		[JsonProperty(PropertyName = "type")]
		public StockType Type;
        public Company Company;
        public int SortIndex;
        [JsonProperty(PropertyName = "lot")]
        public int LotSize;
        public decimal? CurPrice;
		public DateTime CurPriceUpdatedOn;
        // Name of stock in excel (brokerName)
        public string Name;
        public Currency Currency;
		/// <summary>for divs</summary>
        public Currency DivCurrency;

		public DateTime LastHistotyDate;

        public Data Data;   
        public Dictionary<int, AccountData> AccountData;

        public BaseStock() { }
        public BaseStock(string ticker) {
            Ticker = ticker;
        }

        public List<Position> Positions { get; set; }

        //public decimal? TotalFinResult(AccountType? account, Currency? cur) 
        //{
        //    if (account != null && cur != null)
        //    { 
        //        var v = Invest.Core.Core.Instance.Operations
        //            .Where(x => x.Stock == this 
        //                && !x.IsClosed
        //                && (x.Type == OperationType.Buy || x.Type == OperationType.Sell)
        //                && x.AccountType == account 
        //                && x.Stock.Currency == cur);

        //        return v.Sum(x => x.FinResult);
        //    }

        //    return Data.FinResultForClosedPositions;    
        //}

        //public decimal? TotalFinResultForClosedPositions(AccountType? account, Currency? cur) 
        //{
        //    var v = Invest.Core.Core.Instance.Operations
        //        .Where(x => x.Stock == this 
        //            && x.IsClosed
        //            && (x.Type == OperationType.Buy || x.Type == OperationType.Sell)
        //            && (account == null || x.AccountType == account)
        //            && (cur == null || x.Stock.Currency == cur));
                
        //    return v.Sum(x => x.FinResult);

        //    //return Data.TotalFinResultForClosedPositions;    
        //}

		public decimal? GetCommission(int? acCode = null)
		{
			if (acCode != null)
				return AccountData[acCode.Value].Commission;
			else
				return Data.Commission;
		}

		//public DateTime? FirstBuy(AccountType? type = null)
		//{
		//	var r = Invest.Core.Core.Instance.Operations.Where(x => x.Type == OperationType.Buy
		//	                                                          && x.Stock.Ticker == Ticker
		//	                                                          && (type == null || x.AccountType == type)
		//		);
		//	if (r.Any())
		//		return r.Min(x => x.Date);

  //          return null;
  //      }

		//public DateTime? LastBuy(AccountType? type = null)
		//{
		//	var r = Invest.Core.Core.Instance.Operations.Where(x => x.Type == OperationType.Buy
		//	                                                          && x.Stock.Ticker == Ticker
		//	                                                          && (type == null || x.AccountType == type)
		//		);
				
		//	if (r.Any())
		//		return r.Max(x => x.Date);
            
  //          return null;
  //      }

		//public DateTime? LastSell(AccountType? type = null)
		//{
		//	var r = Invest.Core.Core.Instance.Operations.Where(x => x.Type == OperationType.Sell
		//	                                                          && x.Stock.Ticker == Ticker
		//	                                                          && (type == null || x.AccountType == type)
		//		);

		//	if (r.Any())
		//		return r.Max(x => x.Date);
            
  //          return null;
  //      }

		//public decimal? MinBuyPrice(AccountType? type = null)
		//{
		//	var r = Invest.Core.Core.Instance.Operations.Where(x => x.Type == OperationType.Buy
		//	                                                          && x.Stock.Ticker == Ticker
		//	                                                          && (type == null || x.AccountType == type)
		//		);

		//	return r.Any() 
  //              ? r.Min(x => x.Price) 
  //              : null;
  //      }

		//public decimal? MaxSellPrice(AccountType? type = null)
  //      {
  //          var r = Invest.Core.Core.Instance.Operations.Where(x => x.Type == OperationType.Sell
  //                                                                    && x.Stock.Ticker == Ticker
  //                                                                    && (type == null || x.AccountType == type));

  //          return r.Any() ? r.Max(x => x.Price) : null;
  //      }
	}

    public class Stock : BaseStock
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

	public class Position
	{
		public Position(){}

		public Position(int num, Operation op) 
			: this(num, op.Date, op.Type == OperationType.Sell ? PositionType.Short : PositionType.Long) 
		{}

		public Position(int num, DateTime startDate, PositionType type = PositionType.Long)
		{
			Num = num;
			Type = type;
			StartDate = startDate;
			NotClosedOps = new Queue<Operation>();
			Items = new List<PositionItem>();
		}

		public int Num;
		public PositionType Type;
		public DateTime StartDate;
		public DateTime? CloseDate;
		public bool IsClosed;
		public Queue<Operation> NotClosedOps;
		public List<PositionItem> Items;
		public VirtualAccount VirtualAccount;
		public Currency Currency;
		public Exchange Exchange; 
		public int CurentQty;   // temprary qty for calc procedure and also save actual Qty for last open position
		
		public decimal? FinResult => Items.LastOrDefault(x => x.TotalFinResult != null)?.TotalFinResult;

		public decimal? BuySum {
			get {
				return Items.Where(x => x.Operation.Type == OperationType.Buy).Sum(item => item.Operation.Price * item.Qty);
			}
		}

		public decimal? SellSum {
			get
			{
				var s = Items.Where(x => x.Operation.Type == OperationType.Sell);
				return s.Any() 
					? Items.Where(x => x.Operation.Type == OperationType.Sell).Sum(item => item.Operation.Price * item.Qty) 
					: null;
			}
		}

		public decimal Commission
		{
			get {
				return Items.Sum(item => item.Commission);
			}
		}

		public decimal? TotalFinResult
		{
			get {
				var item = Items.LastOrDefault(x => x.ForCalc);
				return item?.TotalFinResult;
			}
		}

		public decimal? PosPrice
		{
			get {
				var item = Items.LastOrDefault();
				return item?.PosPrice;
			}
		}

		public FifoResult FifoResult
		{
			get {
				var result = new FifoResult();

				var item = Items.LastOrDefault(x => x.ForCalc);
				if (item != null && item.FifoResult != null)
				{
					result.Summa = item.FifoResult.Summa;
					result.Commission  = item.FifoResult.Commission;
					result.RurSumma = item.FifoResult.RurSumma;
					result.RurCommission = item.FifoResult.RurCommission;
					result.TotalSumma = item.FifoResult.TotalSumma;
				}

				return result;
			}
		}
		
		public void AddItem(Operation op, int qty)
		{
			var item = new PositionItem(op, qty);
			Items.Add(item);

			item.ForCalc = (Type == PositionType.Long && item.Operation.Type == OperationType.Sell)
				                || (Type == PositionType.Short && item.Operation.Type == OperationType.Buy);

			// divide commission per item
			if (op.Commission.HasValue && op.LotCount != 0)
				item.Commission = qty == op.Qty
					? op.Commission.Value
					: op.Commission.Value / (op.LotCount * qty);

			CurentQty += op.Type == OperationType.Buy ? qty : -qty;
		}

		public void CalcPosPrice()
		{
			var curentQty = 0;
			var list = new List<PositionItem>();

			for(var i=0; i < Items.Count; i++)
			{
				var item = Items[i];
				list.Add(item);

				curentQty += item.Operation.Type == OperationType.Buy
					? item.Qty
					: -item.Qty;

				if (list.Count == 1 || i == 0) {
					list[i].PosPrice = item.Operation.Price ?? 0;
					continue;
				}

				var recB = list.Where(x => x.Operation.Type == OperationType.Buy).Sum(x => x.Operation.Price * x.Qty) ?? 0;
				var recS = list.Where(x => x.Operation.Type == OperationType.Sell).Sum(x => x.PosPrice * x.Qty);

				if (Type == PositionType.Short)
				{
					recB = list.Where(x => x.Operation.Type == OperationType.Buy).Sum(x => x.PosPrice * x.Qty);
					recS = list.Where(x => x.Operation.Type == OperationType.Sell).Sum(x => x.Operation.Price * x.Qty) ?? 0;
				}

				item.PosPrice = !item.ForCalc 
					? (recB - recS) / curentQty 
					: list[i-1].PosPrice;
			}
		}

		public void Close(DateTime closeDate)
		{
			CloseDate = closeDate;
			IsClosed = true;
			CurentQty = 0;
		}

		public void CalcFinResult()
		{
			decimal totalFinResult = 0;
			foreach(var item in Items)
			{
				var op = item.Operation;

				if (op.Commission == null || op.Qty == null)
					throw new Exception("CalcPositionFinResult(): op.Commission == null || op.Qty == null");

				if (item.ForCalc)
				{
					item.FinResult = ((op.Price ?? 0) - item.PosPrice) * item.Qty;
					if (Type == PositionType.Short)
						item.FinResult *= -1;

					totalFinResult += item.FinResult.Value;
					item.TotalFinResult = totalFinResult;
				}

				item.Commission = item.Qty == item.Operation.Qty
					? op.Commission.Value
					: op.Commission.Value / (op.LotCount * item.Qty);
			}
		}
	}

	public class PositionItem
	{
		public PositionItem(Operation op, int qty)
		{
			Operation = op;
			Qty = qty;
		}

		public Operation Operation;
		public int Qty;
		public decimal PosPrice;
		public decimal? FinResult, TotalFinResult;
		public FifoResult FifoResult;
		public decimal Commission;
		public bool ForCalc;
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

		public List<Operation> Operations;
		public List<Position> Positions;
		
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

    public class Operation
    {
	    public BaseAccount Account;
        public AccountType AccountType;
        public DateTime Date;
		public DateTime? DeliveryDate; // плановая дата поставки
        public OperationType Type;
		[JsonIgnore]
        public BaseStock Stock;
        public int? Qty;               // кол-во акций
        public decimal? Price;
		public decimal? PriceInRur;  // operation price in RUR if currency == usd or eur
        public decimal? PosPrice;	 // Цена позиции (avg)
        public int Index;
        public decimal? Summa;
        public decimal? RurSumma;  // operation summa in RUR if currency == usd or eur
        public decimal FinResult;
        public decimal TotalFinResult; // нарастающий итог
        public bool IsClosed;
        public int ClosedCount;         // count of closed (sell) positions of asset
        //B3062822828
        public string TransId;
        public string OrderId;
        public Currency Currency;
        public string Comment;

        public decimal? BankCommission1; // Комиссия Банка за расчет по сделке
        public decimal? BankCommission2; // Комиссия Банка за заключение сделки
        public int? PositionNum;
		public decimal? Nkd;			 // coupon 

        // fifo saldo for sells and buys (default value = qty)
        public int QtySaldo;
        // fin result for sell position
		public FifoResult FifoResult;
		public Position Position;

		public int OffsetQty
		{
			get {
				return Qty.HasValue 
					? Type == OperationType.Buy ? Qty.Value : -Qty.Value 
					: 0;
			}
		}

        /// <summary>Кол-во лотов</summary>
        public int LotCount =>
	        Qty != null && Stock.LotSize != 0 
		        ? Qty.Value / Stock.LotSize 
		        : 0;

        /// <summary>BankCommission1 + BankCommission2</summary>
        public decimal? Commission => Builder.SumValues(BankCommission1, BankCommission2);
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
        public AccountType AccountType;
        public BaseAccount Account;
        public VirtualAccount VirtualAccount;
        public Currency Cur;
        public string Ticker;
        
        public Analytics()
        { }

        public Analytics(string ticker, BaseAccount account, Currency cur, DateTime date)
        {
            Ticker = ticker;
            AccountType = account.Type;
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
                    Month, Year, (int)AccountType, (int)Cur));
        }

        public override bool Equals(object obj)
        {
            if (obj is Analytics a)
                return a.Ticker == Ticker && a.AccountType == AccountType && a.Cur == Cur && a.Year == Year && a.Month == Month;

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