using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Invest.Core.Entities
{
	public abstract class BaseAccount
	{
		public string Id;
		public string Name;
		public string BrokerName;
		public AccountType Type;
	}

    public class Account : BaseAccount
    {
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

		public DateTime LastHistotyDate;

        public Data Data;   
        public Dictionary<AccountType, AccountData> AccountData;

        public BaseStock() { }
        public BaseStock(string ticker) {
            Ticker = ticker;
        }

        public decimal? TotalFinResult(AccountType? account, Currency? cur) 
        {
            if (account != null && cur != null)
            { 
                var v = Invest.Core.Core.Instance.Operations
                    .Where(x => x.Stock == this 
                        && !x.IsClosed
                        && (x.Type == OperationType.Buy || x.Type == OperationType.Sell)
                        && x.AccountType == account 
                        && x.Stock.Currency == cur);

                return v.Sum(x => x.FinResult);
            }

            return Data.FinResultForClosedPositions;    
        }

        public decimal? TotalFinResultForClosedPositions(AccountType? account, Currency? cur) 
        {
            var v = Invest.Core.Core.Instance.Operations
                .Where(x => x.Stock == this 
                    && x.IsClosed
                    && (x.Type == OperationType.Buy || x.Type == OperationType.Sell)
                    && (account == null || x.AccountType == account)
                    && (cur == null || x.Stock.Currency == cur));
                
            return v.Sum(x => x.FinResult);

            //return Data.TotalFinResultForClosedPositions;    
        }

		public decimal? GetCommission(AccountType? type = null)
		{
			if (type != null)
				return AccountData[type.Value].Commission;
			else
				return Data.Commission;
		}

		public DateTime? FirstBuy(AccountType? type = null)
		{
			var r = Invest.Core.Core.Instance.Operations.Where(x => x.Type == OperationType.Buy
			                                                          && x.Stock.Ticker == Ticker
			                                                          && (type == null || x.AccountType == type)
				);
			if (r.Any())
				return r.Min(x => x.Date);

            return null;
        }

		public DateTime? LastBuy(AccountType? type = null)
		{
			var r = Invest.Core.Core.Instance.Operations.Where(x => x.Type == OperationType.Buy
			                                                          && x.Stock.Ticker == Ticker
			                                                          && (type == null || x.AccountType == type)
				);
				
			if (r.Any())
				return r.Max(x => x.Date);
            
            return null;
        }

		public DateTime? LastSell(AccountType? type = null)
		{
			var r = Invest.Core.Core.Instance.Operations.Where(x => x.Type == OperationType.Sell
			                                                          && x.Stock.Ticker == Ticker
			                                                          && (type == null || x.AccountType == type)
				);

			if (r.Any())
				return r.Max(x => x.Date);
            
            return null;
        }

		public decimal? MinBuyPrice(AccountType? type = null)
		{
			var r = Invest.Core.Core.Instance.Operations.Where(x => x.Type == OperationType.Buy
			                                                          && x.Stock.Ticker == Ticker
			                                                          && (type == null || x.AccountType == type)
				);

			return r.Any() 
                ? r.Min(x => x.Price) 
                : null;
        }

		public decimal? MaxSellPrice(AccountType? type = null)
        {
            var r = Invest.Core.Core.Instance.Operations.Where(x => x.Type == OperationType.Sell
                                                                      && x.Stock.Ticker == Ticker
                                                                      && (type == null || x.AccountType == type));

            return r.Any() ? r.Max(x => x.Price) : null;
        }
	}

    public class Stock : BaseStock
    {
	    
    }


    public class Data
    {
		[JsonIgnore]
		public Stock Stock;
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

	public class PositionData
	{
		public PositionData(){}

		public PositionData(int num, Operation op) 
			: this(num, op.Date, op.Type == OperationType.Sell ? PositionType.Short : PositionType.Long) 
		{}

		public PositionData(int num, DateTime startDate, PositionType type = PositionType.Long)
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
		internal int CurentQty;   // temprory qty for calc procedure
		
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

		public void CalcFifoResult(AccountData accData)
		{
			var stock = accData.Stock;
	        var notClosedOps = new Queue<PositionItem>();
			PositionItem lastItem = null;

	        foreach(var item in Items)
	        {
		        var lots = item.Qty / stock.LotSize;

		        if (!item.ForCalc)
		        {
			        for (var i = 1; i <= lots; i++)
				        notClosedOps.Enqueue(item);
		        }

		        //if (stock.Ticker == "PLZL" && pos.StartDate.Date == new DateTime(2022,2,21) && pos.Type == PositionType.Short) { var q = 0;}

		        if (item.ForCalc)
		        {
					lots = item.Qty / stock.LotSize;
					while (lots > 0)
					{
						var opBuy = notClosedOps.Dequeue();
						if (opBuy == null)
							throw new Exception("CalcPositionFifoResult(): opBuy in Queue == null");

						var op = item.Operation;
						var profit = stock.LotSize * (item.Operation.Price - opBuy.Operation.Price).Value;
						if (Type == PositionType.Short)
							profit = stock.LotSize * (opBuy.Operation.Price - item.Operation.Price).Value;

						var commission = (item.Commission / (item.Qty / stock.LotSize)) + (opBuy.Commission / (opBuy.Qty));

						if (stock.Ticker == "PLZL" && StartDate.Date == new DateTime(2022,2,21) && Type == PositionType.Short) { var q = 0;}

						// Fifo result						
						var result = new FifoResult
						{
							Summa = profit,
							Cur = op.Currency,
							// buys comm. + sell comm.
							Commission = Math.Round(commission, 2)
						};

						// доход (и налог) считается с Summa - Commission!! (без учета коммисии)
						if (op.Currency == Currency.Usd || op.Currency == Currency.Eur)
						{
							var buyRate = Invest.Core.Core.Instance.GetCurRate(op.Currency, opBuy.Operation.DeliveryDate.Value);
							var sellRate = Invest.Core.Core.Instance.GetCurRate(op.Currency, op.DeliveryDate.Value);

							var delta = stock.LotSize * ((op.Price.Value * sellRate) - (opBuy.Operation.Price.Value * buyRate));
							result.RurSumma = Math.Round(delta, 2);
							result.RurCommission =
								Math.Round((opBuy.Commission / (opBuy.Qty / stock.LotSize)) * buyRate + (item.Commission / (item.Qty / stock.LotSize)) * sellRate, 2);
						}
						else
						{
							result.RurSumma = result.Summa;
							result.RurCommission = Math.Round(result.Commission, 2);
						}

						var analitic = new Analytics(op.Stock.Ticker, accData.AccountType, op.Currency, op.DeliveryDate.Value);

						if (!Invest.Core.Core.Instance.FifoResults.ContainsKey(analitic))
						{
							Invest.Core.Core.Instance.FifoResults.Add(analitic, result);
						}
						else
						{
							var r = Invest.Core.Core.Instance.FifoResults[analitic];
							r.Summa += result.Summa;
							r.Commission += result.Commission;
							r.RurSumma += result.RurSumma;
							r.RurCommission += result.RurCommission;
						}

						if (stock.Ticker == "OZONDR" && op.TransId == "S3715404120" /* && op.Date >= new DateTime(2021,3,12)*/)
						{
							var R = 0;
						}

						if (item.FifoResult == null)
						{
							item.FifoResult = new FifoResult
							{
								Summa = profit,
								Cur = op.Currency,
								// buys comm. + sell comm.
								Commission = Math.Round(((item.Commission) / item.Qty) + ((opBuy.Commission) / opBuy.Qty), 2),
								RurSumma = result.RurSumma,
								RurCommission = Math.Round(result.RurCommission, 2),
								TotalSumma = (lastItem?.FifoResult?.TotalSumma ?? 0) + profit
							};
							lastItem = item;
						}
						else
						{
							item.FifoResult.Summa += profit;
							item.FifoResult.Commission += Math.Round(commission, 2);
							item.FifoResult.RurSumma += result.RurSumma;
							item.FifoResult.RurCommission += Math.Round(result.RurCommission, 2);
							item.FifoResult.TotalSumma += profit;
						}

						lots--;
					}
		        }
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
        public Stock Stock;
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
		public List<PositionData> Positions;
		
		public AccountData(AccountType aType, Stock stock)
		{
			Stock = stock;
			AccountType = aType;
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

   //     public bool ExistOpenPosition
   //     {
			//get {
			//	return Positions.Any(x => !x.IsClosed);
			//}
   //     }

		public decimal? PosPrice
		{
			get
			{
				var position = Positions.LastOrDefault();
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

		public PositionData GetPositionData(int num)
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
        public AccountType AccountType;
        public DateTime Date;
		public DateTime? DeliveryDate; // плановая дата поставки
        public OperationType Type;
		[JsonIgnore]
        public Stock Stock;
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
        public decimal? Commission => Invest.Core.Core.SumValues(BankCommission1, BankCommission2);
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
        public Currency Cur;
        public string Ticker;
        
        public Analytics()
        { }

        public Analytics(string ticker, AccountType accountType, Currency cur, DateTime date)
        {
            Ticker = ticker;
            AccountType = accountType;
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


	[Flags]
	public enum PortfolioType
	{
		IIs = 1,
		Vbr = 2,
		Usd = 4,
		Rur = 8,
		BlueRu = 16,
		BlueUs = 32,
		BlueEur = 64
	}


    [Flags]
    public enum OperationType
    {
        //CasheIn = 1,   // not Used
        Dividend = 2,
        Buy = 4,
        Sell = 8,
        //CasheOut = 16,
        BrokerCacheIn = 32,
        BrokerCacheOut = 64,
        BrokerFee = 128,
        UsdExchange = 256,
		Ndfl = 512,
		UsdRubBuy = 1024,	// Operation: Завершенные в отчетном периоде сделки с иностранной валютой (обязательства прекращены)
		UsdRubSell = 2048,
		Coupon = 4096,		// купонный доход
		EurRubBuy = 8192,	// Operation: Завершенные в отчетном периоде сделки с иностранной валютой (обязательства прекращены)
		EurRubSell = 16384,
    }

    [Flags]
    public enum AccountType
    {
        Iis = 1,
        VBr = 2,
		SBr = 4
    }

    [Flags]
    public enum Currency
    {
        Rur = 1,
        Usd = 2,
        Eur = 4
    }

    [Flags]
    public enum StockType
    {
		Share =1,
		Bond = 2
	}

	///Type of position
    public enum PositionType
    {
	    Long = 1,
	    Short = 2
    }
}